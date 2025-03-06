import torch
from torchvision import models, transforms
from PIL import Image
import os

# Load the trained model
model = models.resnet18(pretrained=False)
model.fc = torch.nn.Linear(model.fc.in_features, 3)
model.load_state_dict(torch.load("image_classifier.pth"))
model.eval()

# Define transformations
transform = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
])

# Classify new images
def classify_image(image_path):
    image = Image.open(image_path).convert("RGB")
    image = transform(image).unsqueeze(0)

    with torch.no_grad():
        output = model(image)
        predicted_class = torch.argmax(output).item()

    return ["Snowy", "Cloudy", "Sunny"][predicted_class]

# Auto-sort images
new_images_path = "new_images/"
sorted_path = "sorted_images/"

for img_file in os.listdir(new_images_path):
    img_path = os.path.join(new_images_path, img_file)
    label = classify_image(img_path)
    os.makedirs(os.path.join(sorted_path, label), exist_ok=True)
    os.rename(img_path, os.path.join(sorted_path, label, img_file))

print("Sorting complete! Check the sorted_images folder.")
