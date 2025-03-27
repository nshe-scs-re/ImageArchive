import torch
from torchvision import models, transforms
from PIL import Image
import argparse
import os
import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart

# Load model only once when script starts
device = torch.device("cpu")
model = None

def load_model(model_path):
    global model
    if model is None:
        model = models.resnet34(pretrained=False)
        model.fc = torch.nn.Sequential(
            torch.nn.Linear(model.fc.in_features, 2)
        )
        model.load_state_dict(torch.load(model_path, map_location=device))
        model.to(device)
        model.eval()
    return model

# Prepare transforms only once
transform = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
])

def classify_image(image_path, model_path="best_model.pth"):
    try:
        # Load model if not already loaded
        model = load_model(model_path)

        # Load and preprocess the image
        image = Image.open(image_path).convert("RGB")
        image_tensor = transform(image).unsqueeze(0).to(device)

        # Get prediction
        with torch.no_grad():
            output = torch.sigmoid(model(image_tensor))
            has_snow_prob = output[0][0].item()
            no_snow_prob = output[0][1].item()
            
            prediction = 'Has Snow' if has_snow_prob > no_snow_prob else 'No Snow'

        # Print results in a parseable format
        print(f"Prediction:{prediction}")
        print(f"Snow Probability:{has_snow_prob:.2%}")
        print(f"No Snow Probability:{no_snow_prob:.2%}")

        return True

    except Exception as e:
        print(f"Error: {str(e)}")
        return False

def send_email(email_address, results):
    # Configure this with your email settings
    sender_email = "your-email@example.com"
    password = "your-app-password"
    
    message = MIMEMultipart()
    message["From"] = sender_email
    message["To"] = email_address
    message["Subject"] = "Snow Analysis Results"
    
    body = f"""
    Analysis Results:
    Prediction: {results['prediction']}
    Confidence: {results['confidence']:.2%}
    Snow Probability: {results['snow_prob']:.2%}
    No Snow Probability: {results['no_snow_prob']:.2%}
    """
    
    message.attach(MIMEText(body, "plain"))
    
    try:
        with smtplib.SMTP_SSL("smtp.gmail.com", 465) as server:
            server.login(sender_email, password)
            server.send_message(message)
    except Exception as e:
        print(f"Error sending email: {str(e)}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Classify an image for snow conditions')
    parser.add_argument('image_path', type=str, help='Path to the image file')
    parser.add_argument('--model', type=str, default='best_model.pth', help='Path to the model file')
    parser.add_argument('--email', type=str, help='Email address for results')
    
    args = parser.parse_args()
    
    if not os.path.exists(args.image_path):
        print(f"Error: Image file not found at {args.image_path}")
        exit(1)
    elif not os.path.exists(args.model):
        print(f"Error: Model file not found at {args.model}")
        exit(1)
    else:
        success = classify_image(args.image_path, args.model)
        exit(0 if success else 1)