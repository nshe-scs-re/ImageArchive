import torch
import torchvision.transforms as transforms
from PIL import Image
import os
import argparse
from pathlib import Path
import shutil
import pandas as pd
from tqdm import tqdm
import warnings

warnings.filterwarnings('ignore', category=UserWarning)

# Define the multi-task model
class MultiTaskResNet(torch.nn.Module):
    def __init__(self):
        super(MultiTaskResNet, self).__init__()
        # Load base model
        self.base_model = torch.hub.load('pytorch/vision:v0.10.0', 'resnet34', pretrained=False)
        
        # Remove the final fully connected layer
        self.features = torch.nn.Sequential(*list(self.base_model.children())[:-1])
        
        # Add task-specific heads
        self.weather_classifier = torch.nn.Sequential(
            torch.nn.Dropout(0.5),
            torch.nn.Linear(512, 256),
            torch.nn.ReLU(),
            torch.nn.Dropout(0.3),
            torch.nn.Linear(256, 2)  # 2 outputs for weather (Sunny/Cloudy)
        )
        
        self.snow_classifier = torch.nn.Sequential(
            torch.nn.Dropout(0.5),
            torch.nn.Linear(512, 256),
            torch.nn.ReLU(),
            torch.nn.Dropout(0.3),
            torch.nn.Linear(256, 2)  # 2 outputs for snow (Snow/No Snow)
        )
    
    def forward(self, x):
        # Extract features
        x = self.features(x)
        x = torch.flatten(x, 1)
        
        # Get predictions from each head
        weather_output = self.weather_classifier(x)
        snow_output = self.snow_classifier(x)
        
        return weather_output, snow_output

def predict_image(image_path, model_path="best_model.pth"):
    # Set up device
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    
    # Load model
    print("Loading model...")
    model = MultiTaskResNet()
    model.load_state_dict(torch.load(model_path, map_location=device))
    model.to(device)
    model.eval()
    print("Model loaded successfully!")
    
    # Image preprocessing
    transform = transforms.Compose([
        transforms.Resize((224, 224)),
        transforms.ToTensor(),
        transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
    ])
    
    # Load and transform image
    print("Processing image...")
    image = Image.open(image_path).convert("RGB")
    image_tensor = transform(image).unsqueeze(0).to(device)
    
    # Get predictions
    with torch.no_grad():
        weather_outputs, snow_outputs = model(image_tensor)
        
        # Get probabilities
        weather_probs = torch.softmax(weather_outputs, dim=1)[0]
        snow_probs = torch.softmax(snow_outputs, dim=1)[0]
        
        # Get predictions
        _, weather_pred = torch.max(weather_outputs, 1)
        _, snow_pred = torch.max(snow_outputs, 1)
        
        weather_result = "Cloudy" if weather_pred.item() == 1 else "Sunny"
        snow_result = "Snow" if snow_pred.item() == 1 else "No Snow"
        
        # Get confidence
        weather_confidence = weather_probs[weather_pred.item()].item()
        snow_confidence = snow_probs[snow_pred.item()].item()
    
    # Print results
    print("\nImage Classification Results:")
    print("-" * 30)
    print(f"Image: {os.path.basename(image_path)}")
    print(f"Weather: {weather_result} (Confidence: {weather_confidence:.2%})")
    print(f"Snow: {snow_result} (Confidence: {snow_confidence:.2%})")
    print(f"Weather probabilities - Sunny: {weather_probs[0]:.2%}, Cloudy: {weather_probs[1]:.2%}")
    print(f"Snow probabilities - No Snow: {snow_probs[0]:.2%}, Snow: {snow_probs[1]:.2%}")
    
    combined_result = f"{weather_result} with {snow_result}"
    print(f"Combined: {combined_result}")
    
    return weather_result, snow_result, weather_confidence, snow_confidence

def find_all_images(directory):
    """Recursively find all images in nested directories"""
    image_files = []
    image_extensions = ('.jpg', '.jpeg', '.png', '.bmp', '.tiff', '.webp')
    
    # Use tqdm to show progress while scanning for images
    print(f"Scanning {directory} for images...")
    roots = []
    files_list = []
    
    # First collect all directories and files
    for root, _, files in os.walk(directory):
        roots.append(root)
        files_list.append(files)
    
    # Then process with progress bar
    for root, files in tqdm(zip(roots, files_list), total=len(roots), desc="Scanning directories"):
        for file in files:
            if file.lower().endswith(image_extensions):
                image_files.append(os.path.join(root, file))
    
    print(f"Found {len(image_files)} images")
    return image_files

def batch_process_images(input_dir, output_dir, model_path="best_model.pth", confidence_threshold=0.7, copy_files=True):
    """
    Process all images in a directory and organize them by classification
    """
    # Set up device
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Using device: {device}")
    
    # Load model
    print("Loading model...")
    model = MultiTaskResNet()
    try:
        model.load_state_dict(torch.load(model_path, map_location=device))
        print(f"Model loaded from {model_path}")
    except Exception as e:
        print(f"Error loading model: {e}")
        return
    
    model.to(device)
    model.eval()
    
    # Image preprocessing
    transform = transforms.Compose([
        transforms.Resize((224, 224)),
        transforms.ToTensor(),
        transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
    ])
    
    # Create output directories
    if copy_files:
        output_path = os.path.join(output_dir)
        categories = ['Sunny_With_Snow', 'Sunny_No_Snow', 'Cloudy_With_Snow', 'Cloudy_No_Snow', 'Uncertain']
        for category in categories:
            os.makedirs(os.path.join(output_path, category), exist_ok=True)
        print(f"Created output directories in {output_path}")
    else:
        output_path = output_dir
        os.makedirs(output_path, exist_ok=True)
    
    # Get all image paths
    image_files = find_all_images(input_dir)
    
    # Process images with tqdm progress bar
    results = []
    skipped = 0
    
    for img_path in tqdm(image_files, desc="Processing images", unit="img"):
        try:
            # Load and preprocess image
            image = Image.open(img_path).convert("RGB")
            image_tensor = transform(image).unsqueeze(0).to(device)
            
            # Get prediction
            with torch.no_grad():
                weather_outputs, snow_outputs = model(image_tensor)
                
                # Calculate probabilities
                weather_probs = torch.softmax(weather_outputs, dim=1)[0]
                snow_probs = torch.softmax(snow_outputs, dim=1)[0]
                
                # Get predictions (0=sunny/no_snow, 1=cloudy/snow)
                _, weather_pred = torch.max(weather_outputs, 1)
                _, snow_pred = torch.max(snow_outputs, 1)
                
                weather_result = "Cloudy" if weather_pred.item() == 1 else "Sunny"
                snow_result = "With_Snow" if snow_pred.item() == 1 else "No_Snow"
                
                # Get confidence scores
                weather_confidence = weather_probs[weather_pred.item()].item()
                snow_confidence = snow_probs[snow_pred.item()].item()
                
                # Combine for overall category
                category = f"{weather_result}_{snow_result}"
                
                # Check confidence threshold
                if weather_confidence < confidence_threshold or snow_confidence < confidence_threshold:
                    category = "Uncertain"
                
                # Save results
                results.append({
                    'filename': os.path.basename(img_path),
                    'path': img_path,
                    'weather': weather_result,
                    'snow': snow_result,
                    'weather_confidence': weather_confidence,
                    'snow_confidence': snow_confidence,
                    'category': category
                })
                
                # Copy file to appropriate category folder
                if copy_files:
                    dest_path = os.path.join(output_path, category, os.path.basename(img_path))
                    os.makedirs(os.path.dirname(dest_path), exist_ok=True)
                    import shutil
                    shutil.copy2(img_path, dest_path)
        
        except Exception as e:
            skipped += 1
            print(f"Error processing {img_path}: {e}")
    
    # Save results to CSV with tqdm
    print("Saving results to CSV...")
    import pandas as pd
    results_df = pd.DataFrame(results)
    csv_path = os.path.join(output_path, "classification_results.csv")
    results_df.to_csv(csv_path, index=False)
    
    # Print summary
    print("\nProcessing complete!")
    print(f"Processed {len(results)} images, skipped {skipped} images")
    print(f"CSV report saved to: {csv_path}")
    
    # Print category counts
    if results:
        print("\nClassification Summary:")
        category_counts = results_df['category'].value_counts()
        for category, count in category_counts.items():
            print(f"{category}: {count} images ({count/len(results):.2%})")
    
    return results_df

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Classify an image for weather and snow conditions')
    parser.add_argument('image_path', type=str, help='Path to the image file or directory')
    parser.add_argument('--model', type=str, default='best_model.pth', help='Path to the model file')
    parser.add_argument('--batch', action='store_true', help='Process a batch of images')
    parser.add_argument('--output', type=str, default='classified_images', help='Output directory for batch processing')
    parser.add_argument('--threshold', type=float, default=0.7, help='Confidence threshold')
    parser.add_argument('--no-copy', action='store_true', help='Do not copy files, just create report')
    
    args = parser.parse_args()
    
    # Check if path exists
    if not os.path.exists(args.image_path):
        print(f"Error: Path not found: {args.image_path}")
    elif not os.path.exists(args.model):
        print(f"Error: Model file not found: {args.model}")
    else:
        if args.batch or os.path.isdir(args.image_path):
            # Batch processing mode
            if not os.path.isdir(args.image_path):
                print(f"Error: {args.image_path} is not a directory")
            else:
                print(f"Starting batch processing of images in {args.image_path}")
                batch_process_images(
                    input_dir=args.image_path,
                    output_dir=args.output,
                    model_path=args.model,
                    confidence_threshold=args.threshold,
                    copy_files=not args.no_copy
                )
        else:
            # Single image mode
            predict_image(args.image_path, args.model)