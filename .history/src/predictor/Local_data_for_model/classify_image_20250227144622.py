import torch
from torchvision import models, transforms
from PIL import Image
import argparse
import os

def classify_image(image_path, model_path="best_model.pth"):
    # Set up device
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    
    # Load and set up the model
    model = models.resnet34(pretrained=False)
    model.fc = torch.nn.Sequential(
        torch.nn.Dropout(0.5),
        torch.nn.Linear(model.fc.in_features, 512),
        torch.nn.ReLU(),
        torch.nn.Dropout(0.3),
        torch.nn.Linear(512, 2)
    )
    
    # Load the trained weights
    model.load_state_dict(torch.load(model_path, map_location=device))
    model.to(device)
    model.eval()

    # Set up image transformation
    transform = transforms.Compose([
        transforms.Resize((224, 224)),
        transforms.ToTensor(),
        transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
    ])

    try:
        # Load and preprocess the image
        image = Image.open(image_path).convert("RGB")
        image_tensor = transform(image).unsqueeze(0).to(device)

        # Get prediction
        with torch.no_grad():
            output = torch.sigmoid(model(image_tensor))
            has_snow_prob = output[0][0].item()
            no_snow_prob = output[0][1].item()
            
            confidence = max(has_snow_prob, no_snow_prob)
            prediction = 'Has Snow' if has_snow_prob > no_snow_prob else 'No Snow'

        # Print results
        print("\nImage Classification Results:")
        print("-" * 30)
        print(f"Image: {os.path.basename(image_path)}")
        print(f"Prediction: {prediction}")
        print(f"Confidence: {confidence:.2%}")
        print(f"Snow Probability: {has_snow_prob:.2%}")
        print(f"No Snow Probability: {no_snow_prob:.2%}")

    except Exception as e:
        print(f"Error processing image: {str(e)}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Classify an image for snow conditions')
    parser.add_argument('image_path', type=str, help='Path to the image file')
    parser.add_argument('--model', type=str, default='best_model.pth', help='Path to the model file')
    
    args = parser.parse_args()
    
    if not os.path.exists(args.image_path):
        print(f"Error: Image file not found at {args.image_path}")
    elif not os.path.exists(args.model):
        print(f"Error: Model file not found at {args.model}")
    else:
        classify_image(args.image_path, args.model)