import requests
import re
import pandas as pd
import random
from pathlib import Path
from tqdm import tqdm
import os

# Define categories based on filename keywords (Modify as needed)
CATEGORIES = {
    "snowy": ["snow", "winter"],
    "cloudy": ["cloud", "overcast"],
    "sunny": ["sun", "clear"]
}

TRAIN_SPLIT = 0.8  # 80% images go to Train, 20% go to Test

def categorize_image(filename):
    """Categorizes an image based on filename keywords."""
    for category, keywords in CATEGORIES.items():
        if any(keyword in filename.lower() for keyword in keywords):
            return category
    return "unknown"

def download_images(site_name, base_save_dir, max_total=600):
    """Download images from PhenoCam and split into Train/Test datasets."""
    print(f"Fetching URLs for {site_name}...")

    # Fetch the site data page
    try:
        resp = requests.get(f"https://phenocam.nau.edu/webcam/browse/{site_name}/", timeout=5)
        resp.raise_for_status()
    except requests.exceptions.RequestException as e:
        raise SystemExit(f"ERROR: Failed to fetch URLs for {site_name}: {e}")

    content = resp.content.decode()
    year_tags = re.findall(r"<a name=\"[0-9]{4}\">", content)
    years = [int(re.search(r"\d+", yt).group()) for yt in year_tags]
    dates = pd.date_range(f"{min(years)}-01-01", f"{max(years)}-12-31").strftime("%Y/%m/%d")

    root = "https://phenocam.nau.edu"
    pattern = re.compile(
        rf"\/data\/archive\/{site_name}\/[0-9]{{4}}\/[0-9]{{2}}\/{site_name}_[0-9]{{4}}_[0-9]{{2}}_[0-9]{{2}}_[0-9]{{6}}\.jpg"
    )

    all_photos = []
    for d in tqdm(dates, desc="Fetching image URLs"):
        try:
            resp = requests.get(f"https://phenocam.nau.edu/webcam/browse/{site_name}/{d}/", timeout=5)
            resp.raise_for_status()
        except requests.exceptions.RequestException as e:
            print(f"WARNING: Failed to access {d}: {e}")
            continue

        if resp.ok:
            content = resp.content.decode()
            matches = pattern.finditer(content)
            for m in matches:
                if len(all_photos) >= max_total:
                    break
                all_photos.append(f"{root}{m.group()}")

    # Shuffle URLs for better randomness in dataset split
    random.shuffle(all_photos)

    # Save URLs to a file
    url_file = f"{site_name}_urls.txt"
    with open(url_file, "w") as f:
        f.write("\n".join(all_photos))
    print(f"Saved {len(all_photos)} URLs to {url_file}")
 
    base_save_dir = Path(base_save_dir)
    train_dir = base_save_dir / "Train"
    test_dir = base_save_dir / "Test"
 
    for dataset in [train_dir, test_dir]:
        for category in CATEGORIES.keys():
            (dataset / category).mkdir(parents=True, exist_ok=True)
        (dataset / "unknown").mkdir(parents=True, exist_ok=True) 
    
    train_size = int(len(all_photos) * TRAIN_SPLIT)
    train_images = all_photos[:train_size]
    test_images = all_photos[train_size:]

    # Function to download images
    def save_images(image_list, save_path):
        for url in tqdm(image_list, desc=f"Downloading images to {save_path}"):
            try:
                resp = requests.get(url, timeout=10)
                resp.raise_for_status()
                if resp.ok:
                    img_name = url.split("/")[-1]
                    category = categorize_image(img_name)
                    img_path = save_path / category / img_name
                    with open(img_path, "wb") as f:
                        f.write(resp.content)
            except requests.exceptions.RequestException as e:
                print(f"ERROR: Failed to download {url}: {e}")
                continue

    # Download Train & Test images
    save_images(train_images, train_dir)
    save_images(test_images, test_dir)

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("site_name", help="PhenoCam site name (e.g., 'delnortecounty2')")
    parser.add_argument("save_dir", help="Base directory where Train & Test images should be stored")
    args = parser.parse_args()
    
    download_images(args.site_name, args.save_dir)
