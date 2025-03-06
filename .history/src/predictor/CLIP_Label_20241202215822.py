import torch
from torchvision import models, transforms
from PIL import Image

# Load pre-trained ResNet
model = models.resnet50(pretrained=True)
model.eval()

# Define preprocessing transformations
preprocess = transforms.Compose([
    transforms.Resize(256),
    transforms.CenterCrop(224),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406],
                         std=[0.229, 0.224, 0.225]),
])

# Predict labels for an image


def predict_image(image_path):
    image = Image.open(image_path).convert("RGB")
    input_tensor = preprocess(image).unsqueeze(0)  # Add batch dimension
    with torch.no_grad():
        output = model(input_tensor)
    probabilities = torch.nn.functional.softmax(output[0], dim=0)
    # Get the top prediction
    class_id = torch.argmax(probabilities).item()
    confidence = probabilities[class_id].item()
    return class_id, confidence


# Example usage
image_path = "/path/to/image.jpg"
class_id, confidence = predict_image(image_path)
print(f"Predicted Class ID: {class_id}, Confidence: {confidence}")
