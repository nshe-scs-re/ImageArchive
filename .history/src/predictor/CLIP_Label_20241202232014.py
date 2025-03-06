import os
import torch
from PIL import Image
from torchvision import transforms
import clip
from tqdm import tqdm
import csv

# Load the CLIP model
device = "cuda" if torch.cuda.is_available() else "cpu"
model, preprocess = clip.load("ViT-B/32", device=device)

# Define your labels
labels = ["sunny", "cloudy", "rainy", "snowy", "foggy", "stormy", "clear sky"]

# Convert labels to tokenized text
text_inputs = torch.cat([clip.tokenize(
    f"This is a {label} weather image.") for label in labels]).to(device)

# Define the folder paths
input_folder = "/Users/nathanmichelotti/Desktop/College/Fall 2024/CS 425/Senior Project/OneDrive_1_10-22-2024/Images/Rockland"
output_file = "labeled_images.csv"

# Preprocess the images
image_preprocess = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor(),
    transforms.Normalize((0.48145466, 0.4578275, 0.40821073),
                         (0.26862954, 0.26130258, 0.27577711)),
])

# Find all images in folders and subfolders


def get_all_images(base_path, extensions=('.jpg', '.jpeg', '.png')):
    image_paths = []
    for root, _, files in os.walk(base_path):
        for file in files:
            if file.lower().endswith(extensions):
                image_paths.append(os.path.join(root, file))
    return image_paths


# Get all images in the input folder
image_files = get_all_images(input_folder)

# Process and label images
results = []

print(f"Processing {len(image_files)} images...")
for file_path in tqdm(image_files, desc="Processing Images", unit="image"):
    # Open and preprocess the image
    image = Image.open(file_path).convert("RGB")
    image_input = preprocess(image).unsqueeze(0).to(device)

    # Calculate similarity between the image and text inputs
    with torch.no_grad():
        image_features = model.encode_image(image_input)
        text_features = model.encode_text(text_inputs)

        # Normalize features
        image_features /= image_features.norm(dim=-1, keepdim=True)
        text_features /= text_features.norm(dim=-1, keepdim=True)

        # Compute similarities
        similarities = (image_features @ text_features.T).squeeze(0)
        best_match_idx = similarities.argmax().item()
        best_label = labels[best_match_idx]

    # Append result
    results.append((file_path, best_label))

# Save results to a CSV file
with open(output_file, "w", newline="") as csvfile:
    writer = csv.writer(csvfile)
    writer.writerow(["Image Path", "Label"])
    writer.writerows(results)

print(f"Labels saved to {output_file}")
