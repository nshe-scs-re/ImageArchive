import sys, os
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "../../..")))
import torch
from torchvision import transforms
from PIL import Image
from tqdm import tqdm
from data_labeling_model import MultiTaskResNet
from db_connect import connect_to_database

# Use GPU only
assert torch.cuda.is_available(), "CUDA is not available — GPU required"
device = torch.device("cuda")

# Load model
model_path = "src/predictor/Model/best_model.pth"
model = MultiTaskResNet()
model.load_state_dict(torch.load(model_path, map_location=device))
model.to(device)
model.eval()

# Transform used during training
transform = transforms.Compose([
    transforms.Resize((256, 256)),
    transforms.CenterCrop(224),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406],
                         std=[0.229, 0.224, 0.225])
])

# Connect to DB
conn = connect_to_database()
cursor = conn.cursor()

# Ensure prediction columns exist
cursor.execute("""
IF COL_LENGTH('Images', 'WeatherPrediction') IS NULL
    ALTER TABLE Images ADD WeatherPrediction NVARCHAR(50);
IF COL_LENGTH('Images', 'SnowPrediction') IS NULL
    ALTER TABLE Images ADD SnowPrediction NVARCHAR(50);
""")
conn.commit()

# Pull paths
cursor.execute("SELECT Id, FilePath FROM Images")
rows = cursor.fetchall()

# Fix path prefix
base_path = "/home/nmichelotti/Desktop/Image Archives/OneDrive_1_4-3-2025"

# Predict
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

            weather_str = f"{weather_label} ({weather_conf:.1f}%)"
            snow_str = f"{snow_label} ({snow_conf:.1f}%)"

        cursor.execute("""
            UPDATE Images
            SET WeatherPrediction = %s, SnowPrediction = %s
            WHERE Id = %s
        """, (weather_str, snow_str, img_id))

    except Exception as e:
        print(f"[!] Failed on {file_path}: {e}")

conn.commit()
cursor.close()
conn.close()
print("All predictions complete.")