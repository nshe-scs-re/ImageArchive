# Standard library imports
import os
from pathlib import Path

# Third-party imports
import pandas as pd
import requests
from PIL import Image
from io import BytesIO

def download_image(url, save_path):
    try:
        resp = requests.get(url, timeout=10, stream=True)
        if resp.ok:
            img = Image.open(BytesIO(resp.content))
            img.save(save_path)
            return True
    except Exception as e:
        print(f"Error downloading {url}: {e}")
    return False

def count_images_in_folders(base_path, categories):
    counts = {}
    for category in categories:
        category_path = Path(base_path) / category
        if category_path.exists():
            counts[category] = len(list(category_path.glob("*.jpg")))
        else:
            counts[category] = 0
    return counts

def delete_extra_images(folder_path, target_count):
    folder = Path(folder_path)
    images = list(folder.glob("*.jpg"))
    if len(images) > target_count:
        images_to_delete = images[target_count:]
        for img in images_to_delete:
            img.unlink()
        print(f"Deleted {len(images_to_delete)} images from {folder_path}")

def read_labels(labels_file):
    df = pd.read_csv(labels_file)
    return df

def save_labels(labels, save_path):
    labels.to_csv(save_path, index=False)
    print(f"Labels saved to {save_path}")
