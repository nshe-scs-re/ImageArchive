import sys, os
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "../../..")))
import torch
from torchvision import transforms
from PIL import Image
from tqdm import tqdm
from data_labeling_model import MultiTaskResNet
from db_connect import connect_to_database

# UNIVERSAL GPU / CPU DEVICE SELECTOR

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

    # If no GPU backend is available, ask to use CPU
    user_input = input("No GPU available. Proceed using CPU? (y/n): ").strip().lower()
    if user_input != 'y':
        print("Aborting.")
        sys.exit(1)
    print("Using CPU")
    return torch.device("cpu")

device = get_best_device()


# LOAD MODEL

model_path = "best_model.pth"
model = MultiTaskResNet()
model.load_state_dict(torch.load(model_path, map_location=device))
model.to(device)
model.eval()


# IMAGE TRANSFORM

transform = transforms.Compose([
    transforms.Resize((256, 256)),
    transforms.CenterCrop(224),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406],
                         std=[0.229, 0.224, 0.225])
])


# DATABASE CONNECTION

conn = connect_to_database()
cursor = conn.cursor()

# Ensure prediction columns exist

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


# RUN PREDICTIONS

cursor.execute("SELECT Id, FilePath FROM Images")
rows = cursor.fetchall()

base_path = "/home/nmichelotti/Desktop/Image Archives/OneDrive_1_4-3-2025"

for img_id, file_path in tqdm(rows, desc="Predicting images"):
    try:
        real_path = os.path.join(base_path, file_path.replace("/app", "").lstrip("/"))
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

    except Exception as e:
        print(f"[!] Failed on {file_path}: {e}")

conn.commit()
cursor.close()
conn.close()
print("All predictions complete.")
