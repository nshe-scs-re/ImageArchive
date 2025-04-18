import sys, os
import torch
from torchvision import transforms, models
from PIL import Image
from tqdm import tqdm
from db_connect import connect_to_database
import datetime

class Tee:
    def __init__(self, *files):
        self.files = files

    def write(self, obj):
        timestamped = self._add_timestamp(obj)
        for f in self.files:
            f.write(obj)
            f.flush()

    def flush(self):
        for f in self.files:
            f.flush()

    def _add_timestamp(self, text):
        lines = text.splitlines(keepends=True)
        now = datetime.datetime.now().strftime("[%Y-%m-%d %H:%M:%S] ")
        return ''.join((now + line if line.strip() else line) for line in lines)

log_file = open("initialize.log", "w")
sys.stdout = Tee(log_file)
sys.stderr = Tee(log_file)

# --- Model Definition ---
class MultiTaskResNet(torch.nn.Module):
    def __init__(self):
        super(MultiTaskResNet, self).__init__()
        self.base_model = models.resnet34(pretrained=False)

        for param in self.base_model.parameters():
            param.requires_grad = False
        for param in self.base_model.layer4.parameters():
            param.requires_grad = True

        self.features = torch.nn.Sequential(*list(self.base_model.children())[:-1])

        self.weather_classifier = torch.nn.Sequential(
            torch.nn.Dropout(0.5),
            torch.nn.Linear(512, 256),
            torch.nn.ReLU(),
            torch.nn.Dropout(0.3),
            torch.nn.Linear(256, 2)
        )

        self.snow_classifier = torch.nn.Sequential(
            torch.nn.Dropout(0.5),
            torch.nn.Linear(512, 256),
            torch.nn.ReLU(),
            torch.nn.Dropout(0.3),
            torch.nn.Linear(256, 2)
        )

    def forward(self, x):
        x = self.features(x)
        x = torch.flatten(x, 1)
        return self.weather_classifier(x), self.snow_classifier(x)

# --- Device Selection ---
def get_best_device():
    if torch.cuda.is_available():
        print(f"Using CUDA GPU: {torch.cuda.get_device_name(0)}")
        return torch.device("cuda:0")
    elif hasattr(torch.backends, "mps") and torch.backends.mps.is_available():
        print("Using Apple Metal (MPS) GPU")
        return torch.device("mps")
    elif torch.version.hip is not None and torch.cuda.is_available():
        print("Using ROCm-compatible AMD GPU")
        return torch.device("cuda:0")
    else:
        print("No GPU available. Falling back to CPU.")
        return torch.device("cpu")

device = get_best_device()

# --- Load Model ---
model_path = "best_model.pth"
model = MultiTaskResNet()
model.load_state_dict(torch.load(model_path, map_location=device))
model.to(device)
model.eval()

# --- Image Preprocessing ---
transform = transforms.Compose([
    transforms.Resize((256, 256)),
    transforms.CenterCrop(224),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406],
                         std=[0.229, 0.224, 0.225])
])

# --- Database Connection ---
conn = connect_to_database()
cursor = conn.cursor()

cursor.execute("""
IF COL_LENGTH('Images', 'WeatherPrediction') IS NULL
    ALTER TABLE Images ADD WeatherPrediction NVARCHAR(50);
IF COL_LENGTH('Images', 'WeatherPredictionPercent') IS NULL
    ALTER TABLE Images ADD WeatherPredictionPercent FLOAT;
IF COL_LENGTH('Images', 'SnowPrediction') IS NULL
    ALTER TABLE Images ADD SnowPrediction NVARCHAR(50);
IF COL_LENGTH('Images', 'SnowPredictionPercent') IS NULL
    ALTER TABLE Images ADD SnowPredictionPercent FLOAT;
""")
conn.commit()

# --- Prediction & DB Update ---
cursor.execute("SELECT Id, FilePath FROM Images")
rows = cursor.fetchall()
base_path = "/"

print("Starting image prediction run.")

for i, (img_id, file_path) in enumerate(tqdm(rows, desc="Predicting images", file=sys.stdout)):
    try:
        real_path = os.path.normpath(os.path.join(base_path, file_path.replace("/app", "").lstrip("/")))
        if not os.path.exists(real_path):
            raise FileNotFoundError(f"Missing image: {real_path}")

        image = Image.open(real_path).convert("RGB")
        image_tensor = transform(image).unsqueeze(0).to(device)

        with torch.no_grad():
            weather_out, snow_out = model(image_tensor)
            weather_probs = torch.softmax(weather_out, dim=1)[0]
            snow_probs = torch.softmax(snow_out, dim=1)[0]

            weather_idx = torch.argmax(weather_probs).item()
            snow_idx = torch.argmax(snow_probs).item()

            weather_label = ['Sunny', 'Cloudy'][weather_idx]
            snow_label = ['No Snow', 'Snow'][snow_idx]

            weather_conf = weather_probs[weather_idx].item() * 100
            snow_conf = snow_probs[snow_idx].item() * 100

        cursor.execute("""
            UPDATE Images
            SET WeatherPrediction = %s,
                WeatherPredictionPercent = %s,
                SnowPrediction = %s,
                SnowPredictionPercent = %s
            WHERE Id = %s
        """, (weather_label, weather_conf, snow_label, snow_conf, img_id))

        if i % 10000 == 0:
            tqdm.write(f"[INFO] Processed {i}/{len(rows)} images...", file=sys.stdout)

    except Exception as e:
        tqdm.write(f"[ERROR] Failed on {file_path}: {e}", file=sys.stdout)

conn.commit()
cursor.close()
conn.close()
print("All predictions complete.")
