import torch
from torchvision import transforms
from PIL import Image
import os
from tqdm import tqdm
import pymssql
import predictor.Model.db_connect as db_connect
from data_labeling_model import MultiTaskResNet

# Setup image transform (should match training)
transform = transforms.Compose([
    transforms.Resize((256, 256)),
    transforms.CenterCrop(224),  # Use CenterCrop for deterministic behavior
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406],
                         std=[0.229, 0.224, 0.225])
])

# Load model
model_path = "/Users/nathanmichelotti/Desktop/College/CS 425/Senior Project/ImageArchive/src/predictor/Model/best_model.pth"
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

model = MultiTaskResNet()
model.load_state_dict(torch.load(model_path, map_location=device))
model = model.to(device)
model.eval()

# Class mappings
weather_labels = ['Sunny', 'Cloudy']
snow_labels = ['No Snow', 'Snow']

# Connect to DB
conn = db_connect.connect_to_database()  # use the pymssql-based connection
cursor = conn.cursor()

# Add prediction columns if missing
cursor.execute("""
IF COL_LENGTH('Images', 'WeatherPrediction') IS NULL
BEGIN
    ALTER TABLE Images ADD WeatherPrediction NVARCHAR(50)
END;
IF COL_LENGTH('Images', 'SnowPrediction') IS NULL
BEGIN
    ALTER TABLE Images ADD SnowPrediction NVARCHAR(50)
END;
""")
conn.commit()

# Fetch image file paths
cursor.execute("SELECT TOP 10 Id, FilePath FROM Images")
rows = cursor.fetchall()

# Wrap rows with tqdm
for row in tqdm(rows, desc="Predicting images"):
    img_id = row[0]
    file_path = row[1]

    try:
        # Open and transform the image
        image = Image.open(file_path).convert('RGB')
        image_tensor = transform(image).unsqueeze(0).to(device)

        with torch.no_grad():
            weather_out, snow_out = model(image_tensor)
            weather_pred = torch.argmax(weather_out, dim=1).item()
            snow_pred = torch.argmax(snow_out, dim=1).item()

            weather_label = weather_labels[weather_pred]
            snow_label = snow_labels[snow_pred]

        # Update DB with predictions
        cursor.execute("""
            UPDATE Images
            SET WeatherPrediction = %s, SnowPrediction = %s
            WHERE Id = %s
        """, (weather_label, snow_label, img_id))

    except Exception as e:
        print(f"[!] Error processing {file_path}: {e}")

conn.commit()
cursor.close()
conn.close()
print("All predictions completed and database updated.")
