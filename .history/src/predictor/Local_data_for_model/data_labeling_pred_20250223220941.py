import torch
from torchvision import models, transforms
from PIL import Image
import os
import logging
from pathlib import Path

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class WeatherClassifier:
    def __init__(self, model_path="image_classifier.pth", confidence_threshold=0.7):
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.confidence_threshold = confidence_threshold
        self.classes = ["Snowy", "Cloudy", "Sunny"]
        
        # Use a more powerful model - ResNet50 with pretrained weights
        self.model = models.resnet50(weights='IMAGENET1K_V2')
        self.model.fc = torch.nn.Sequential(
            torch.nn.Dropout(0.2),
            torch.nn.Linear(self.model.fc.in_features, len(self.classes))
        )
        
        # Load trained weights and move to appropriate device
        self.model.load_state_dict(torch.load(model_path, map_location=self.device))
        self.model.to(self.device)
        self.model.eval()

        # Enhanced transformations with augmentation
        self.transform = transforms.Compose([
            transforms.Resize(256),
            transforms.CenterCrop(224),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
        ])

    def classify_image(self, image_path):
        try:
            image = Image.open(image_path).convert("RGB")
            image_tensor = self.transform(image).unsqueeze(0).to(self.device)

            with torch.no_grad():
                output = torch.nn.functional.softmax(self.model(image_tensor), dim=1)
                confidence, predicted_class = torch.max(output, 1)
                
                confidence = confidence.item()
                predicted_class = predicted_class.item()

                if confidence < self.confidence_threshold:
                    return "Uncertain", confidence

                return self.classes[predicted_class], confidence

        except Exception as e:
            logger.error(f"Error processing {image_path}: {str(e)}")
            return "Error", 0.0

def sort_images(input_dir="new_images/", output_dir="sorted_images/", model_path="image_classifier.pth"):
    try:
        # Initialize classifier
        classifier = WeatherClassifier(model_path)
        
        # Create directories
        input_path = Path(input_dir)
        output_path = Path(output_dir)
        uncertain_path = output_path / "Uncertain"
        error_path = output_path / "Error"

        # Ensure directories exist
        for class_name in classifier.classes + ["Uncertain", "Error"]:
            (output_path / class_name).mkdir(parents=True, exist_ok=True)

        # Process images
        processed_count = 0
        for img_file in input_path.glob("*.*"):
            if img_file.suffix.lower() in ['.jpg', '.jpeg', '.png']:
                label, confidence = classifier.classify_image(str(img_file))
                
                # Determine destination path
                dest_path = output_path / label / img_file.name
                
                # Move file
                img_file.rename(dest_path)
                
                processed_count += 1
                logger.info(f"Processed {img_file.name}: {label} (confidence: {confidence:.2f})")

        logger.info(f"Sorting complete! Processed {processed_count} images.")
        logger.info(f"Check the sorted images in: {output_path}")

    except Exception as e:
        logger.error(f"An error occurred during sorting: {str(e)}")

if __name__ == "__main__":
    sort_images()
