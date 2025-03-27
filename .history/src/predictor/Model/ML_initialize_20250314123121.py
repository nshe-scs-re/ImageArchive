import torch
from torchvision import models, transforms
from PIL import Image
import argparse
import os
import sqlite3  # For database connectivity
import glob     # For finding all images

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

        # Return results as a dictionary
        return {
            'prediction': prediction,
            'snow_prob': has_snow_prob,
            'no_snow_prob': no_snow_prob,
            'success': True
        }

    except Exception as e:
        print(f"Error processing {image_path}: {str(e)}")
        return {'success': False, 'error': str(e)}

def connect_to_database(db_path):
    """Connect to the SQLite database."""
    try:
        conn = sqlite3.connect(db_path)
        return conn
    except Exception as e:
        print(f"Database connection error: {str(e)}")
        return None

def update_database_schema(conn):
    """Add prediction columns to the database if they don't exist."""
    try:
        cursor = conn.cursor()
        # Check if columns exist and add them if they don't
        cursor.execute("PRAGMA table_info(images)")
        columns = [info[1] for info in cursor.fetchall()]
        
        if 'prediction' not in columns:
            cursor.execute("ALTER TABLE images ADD COLUMN prediction TEXT")
        if 'snow_probability' not in columns:
            cursor.execute("ALTER TABLE images ADD COLUMN snow_probability REAL")
        if 'no_snow_probability' not in columns:
            cursor.execute("ALTER TABLE images ADD COLUMN no_snow_probability REAL")
            
        conn.commit()
        return True
    except Exception as e:
        print(f"Schema update error: {str(e)}")
        return False

def process_database_images(db_path, images_dir, model_path="best_model.pth"):
    """Process all images in the database and update with prediction values."""
    conn = connect_to_database(db_path)
    if not conn:
        return False
    
    if not update_database_schema(conn):
        conn.close()
        return False
    
    cursor = conn.cursor()
    
    # Get all images from the database
    try:
        cursor.execute("SELECT id, image_path FROM images")
        images = cursor.fetchall()
        
        if not images:
            print("No images found in the database.")
            conn.close()
            return False
        
        print(f"Found {len(images)} images to process.")
        
        # Load model once for all images
        model = load_model(model_path)
        
        for image_id, image_path in images:
            # Construct full path if needed
            full_path = os.path.join(images_dir, image_path) if not os.path.isabs(image_path) else image_path
            
            if not os.path.exists(full_path):
                print(f"Image not found: {full_path}")
                continue
                
            print(f"Processing image: {full_path}")
            result = classify_image(full_path, model_path)
            
            if result['success']:
                # Update database with prediction results
                cursor.execute(
                    "UPDATE images SET prediction = ?, snow_probability = ?, no_snow_probability = ? WHERE id = ?",
                    (result['prediction'], result['snow_prob'], result['no_snow_prob'], image_id)
                )
                print(f"Updated image ID {image_id}: {result['prediction']}")
            
        conn.commit()
        print("All images processed successfully.")
        conn.close()
        return True
        
    except Exception as e:
        print(f"Error processing images: {str(e)}")
        conn.close()
        return False

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Classify images for snow conditions and update database')
    parser.add_argument('--db', type=str, required=True, help='Path to the SQLite database')
    parser.add_argument('--images_dir', type=str, required=True, help='Directory containing the images')
    parser.add_argument('--model', type=str, default='best_model.pth', help='Path to the model file')
    parser.add_argument('--single_image', type=str, help='Process only a single image (optional)')
    
    args = parser.parse_args()
    
    if not os.path.exists(args.model):
        print(f"Error: Model file not found at {args.model}")
        exit(1)
    
    if args.single_image:
        # Process a single image
        if not os.path.exists(args.single_image):
            print(f"Error: Image file not found at {args.single_image}")
            exit(1)
        
        result = classify_image(args.single_image, args.model)
        if result['success']:
            print(f"Prediction: {result['prediction']}")
            print(f"Snow Probability: {result['snow_prob']:.2%}")
            print(f"No Snow Probability: {result['no_snow_prob']:.2%}")
            exit(0)
        else:
            exit(1)
    else:
        # Process all images in the database
        success = process_database_images(args.db, args.images_dir, args.model)
        exit(0 if success else 1)