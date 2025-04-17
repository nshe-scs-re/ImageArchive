import os
import shutil
from tqdm import tqdm
from db_connect import connect_to_database

# Define output folders and limits
output_base = "/home/nmichelotti/Desktop/data"
categories = {
    "Sunny_With_Snow": ("Sunny", "Snow"),
    "Sunny_No_Snow": ("Sunny", "No Snow"),
    "Cloudy_With_Snow": ("Cloudy", "Snow"),
    "Cloudy_No_Snow": ("Cloudy", "No Snow")
}
max_per_category = 500

# Confidence thresholds
thresholds = {
    "Sunny_With_Snow": (82.0, 82.0),
    "Sunny_No_Snow": (98.0, 98.0),
    "Cloudy_With_Snow": (98.0, 98.0),
    "Cloudy_No_Snow": (0,0)
}

# Ensure folders exist
for folder in categories:
    os.makedirs(os.path.join(output_base, folder), exist_ok=True)

# Track how many we've copied
image_counts = {key: 0 for key in categories}

# Connect to DB
conn = connect_to_database()
cursor = conn.cursor()

# Base image folder
base_path = "/home/nmichelotti/Desktop/Image Archives/OneDrive_1_4-3-2025"

# Query all rows and filter in Python
cursor.execute("""
    SELECT FilePath, WeatherPrediction, SnowPrediction,
           WeatherPredictionPercent, SnowPredictionPercent
    FROM Images
""")
rows = cursor.fetchall()

# Process rows
for file_path, weather, snow, weather_pct, snow_pct in tqdm(rows, desc="Filtering and copying"):
    for folder_name, (expected_weather, expected_snow) in categories.items():
        w_thresh, s_thresh = thresholds[folder_name]
        if weather == expected_weather and snow == expected_snow:
            if weather_pct >= w_thresh and snow_pct >= s_thresh:
                if image_counts[folder_name] < max_per_category:
                    src = os.path.join(base_path, file_path.replace("/app", "").lstrip("/"))
                    dst_folder = os.path.join(output_base, folder_name)
                    dst = os.path.join(dst_folder, os.path.basename(src))
                    try:
                        if os.path.exists(src):
                            shutil.copy2(src, dst)
                            print(f"COPIED: {src} -> {dst}")
                            image_counts[folder_name] += 1
                    except Exception as e:
                        print(f"Failed to copy {src}: {e}")
            break

# Clean up
cursor.close()
conn.close()

# Report
print("\nFinished. Images copied per category:")
for category, count in image_counts.items():
    print(f"{category}: {count}")
