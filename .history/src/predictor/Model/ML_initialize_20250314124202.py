import tkinter as tk
from tkinter import filedialog, messagebox
from PIL import Image, ImageTk
import torch
from torchvision import models, transforms
import os
import argparse
import sys
import psycopg2  # For PostgreSQL connection (commonly used in Docker)
import mysql.connector  # For MySQL connection (alternative option)
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Global variables
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model = None

def load_model(model_path):
    """Load the ML model from the specified path."""
    global model
    if model is None:
        logger.info(f"Loading model from {model_path} on {device}")
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

def classify_image(image_path, model):
    """Classify a single image using the loaded model."""
    try:
        # Load and preprocess the image
        image = Image.open(image_path).convert("RGB")
        image_tensor = transform(image).unsqueeze(0).to(device)

        # Get prediction
        with torch.no_grad():
            output = torch.sigmoid(model(image_tensor))
            has_snow_prob = output[0][0].item()
            no_snow_prob = output[0][1].item()
            
            prediction = 'Has Snow' if has_snow_prob > no_snow_prob else 'No Snow'

        return {
            'prediction': prediction,
            'snow_prob': has_snow_prob,
            'no_snow_prob': no_snow_prob,
            'success': True
        }
    except Exception as e:
        logger.error(f"Error processing {image_path}: {str(e)}")
        return {'success': False, 'error': str(e)}

def connect_to_docker_postgres(host, port, dbname, user, password):
    """Connect to PostgreSQL database running in Docker container."""
    try:
        logger.info(f"Connecting to PostgreSQL database at {host}:{port}/{dbname}")
        conn = psycopg2.connect(
            host=host,
            port=port,
            dbname=dbname,
            user=user,
            password=password
        )
        return conn
    except Exception as e:
        logger.error(f"PostgreSQL connection error: {str(e)}")
        return None

def connect_to_docker_mysql(host, port, database, user, password):
    """Connect to MySQL database running in Docker container."""
    try:
        logger.info(f"Connecting to MySQL database at {host}:{port}/{database}")
        conn = mysql.connector.connect(
            host=host,
            port=port,
            database=database,
            user=user,
            password=password
        )
        return conn
    except Exception as e:
        logger.error(f"MySQL connection error: {str(e)}")
        return None

def update_database_schema(conn, db_type):
    """Add prediction columns to the database if they don't exist."""
    try:
        cursor = conn.cursor()
        
        if db_type == 'postgres':
            # Check if columns exist in PostgreSQL
            cursor.execute("""
                SELECT column_name FROM information_schema.columns 
                WHERE table_name = 'images' AND column_name IN ('prediction', 'snow_probability', 'no_snow_probability')
            """)
            existing_columns = [col[0] for col in cursor.fetchall()]
            
            # Add columns if they don't exist
            if 'prediction' not in existing_columns:
                logger.info("Adding 'prediction' column to database")
                cursor.execute("ALTER TABLE images ADD COLUMN prediction TEXT")
            if 'snow_probability' not in existing_columns:
                logger.info("Adding 'snow_probability' column to database")
                cursor.execute("ALTER TABLE images ADD COLUMN snow_probability REAL")
            if 'no_snow_probability' not in existing_columns:
                logger.info("Adding 'no_snow_probability' column to database")
                cursor.execute("ALTER TABLE images ADD COLUMN no_snow_probability REAL")
                
        elif db_type == 'mysql':
            # Check if columns exist in MySQL
            cursor.execute("SHOW COLUMNS FROM images")
            existing_columns = [col[0].lower() for col in cursor.fetchall()]
            
            # Add columns if they don't exist
            if 'prediction' not in existing_columns:
                logger.info("Adding 'prediction' column to database")
                cursor.execute("ALTER TABLE images ADD COLUMN prediction TEXT")
            if 'snow_probability' not in existing_columns:
                logger.info("Adding 'snow_probability' column to database")
                cursor.execute("ALTER TABLE images ADD COLUMN snow_probability FLOAT")
            if 'no_snow_probability' not in existing_columns:
                logger.info("Adding 'no_snow_probability' column to database")
                cursor.execute("ALTER TABLE images ADD COLUMN no_snow_probability FLOAT")
        
        conn.commit()
        logger.info("Database schema updated successfully")
        return True
    except Exception as e:
        logger.error(f"Schema update error: {str(e)}")
        return False

def process_database_images(conn, db_type, images_dir, model):
    """Process all images in the database and update with prediction values."""
    try:
        cursor = conn.cursor()
        
        # Get all images from the database
        logger.info("Retrieving images from database")
        cursor.execute("SELECT id, image_path FROM images")
        images = cursor.fetchall()
        
        if not images:
            logger.warning("No images found in the database")
            return False
        
        logger.info(f"Found {len(images)} images to process")
        processed_count = 0
        error_count = 0
        
        for image_id, image_path in images:
            # Construct full path if needed
            full_path = os.path.join(images_dir, image_path) if not os.path.isabs(image_path) else image_path
            
            if not os.path.exists(full_path):
                logger.warning(f"Image not found: {full_path}")
                error_count += 1
                continue
                
            logger.info(f"Processing image: {full_path}")
            result = classify_image(full_path, model)
            
            if result['success']:
                # Update database with prediction results
                if db_type == 'postgres':
                    cursor.execute(
                        "UPDATE images SET prediction = %s, snow_probability = %s, no_snow_probability = %s WHERE id = %s",
                        (result['prediction'], result['snow_prob'], result['no_snow_prob'], image_id)
                    )
                elif db_type == 'mysql':
                    cursor.execute(
                        "UPDATE images SET prediction = %s, snow_probability = %s, no_snow_probability = %s WHERE id = %s",
                        (result['prediction'], result['snow_prob'], result['no_snow_prob'], image_id)
                    )
                
                processed_count += 1
                logger.info(f"Updated image ID {image_id}: {result['prediction']}")
            else:
                error_count += 1
        
        conn.commit()
        logger.info(f"Processing complete. Processed: {processed_count}, Errors: {error_count}")
        return True
        
    except Exception as e:
        logger.error(f"Error processing images: {str(e)}")
        return False

def main():
    parser = argparse.ArgumentParser(description='Process images in a Docker database with snow detection model')
    
    # Database connection parameters
    parser.add_argument('--db-type', type=str, required=True, choices=['postgres', 'mysql'], 
                        help='Type of database in Docker (postgres or mysql)')
    parser.add_argument('--host', type=str, default='localhost', 
                        help='Database host (Docker container IP or hostname)')
    parser.add_argument('--port', type=int, 
                        help='Database port (default: 5432 for postgres, 3306 for mysql)')
    parser.add_argument('--dbname', type=str, required=True, 
                        help='Database name')
    parser.add_argument('--user', type=str, required=True, 
                        help='Database user')
    parser.add_argument('--password', type=str, required=True, 
                        help='Database password')
    
    # Other parameters
    parser.add_argument('--images-dir', type=str, required=True, 
                        help='Directory containing the images')
    parser.add_argument('--model', type=str, default='best_model.pth', 
                        help='Path to the model file')
    parser.add_argument('--log-file', type=str, 
                        help='Path to log file (optional)')
    
    args = parser.parse_args()
    
    # Set default ports if not specified
    if args.port is None:
        args.port = 5432 if args.db_type == 'postgres' else 3306
    
    # Configure file logging if specified
    if args.log_file:
        file_handler = logging.FileHandler(args.log_file)
        file_handler.setFormatter(logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s'))
        logger.addHandler(file_handler)
    
    # Check if model file exists
    if not os.path.exists(args.model):
        logger.error(f"Model file not found at {args.model}")
        return 1
    
    # Check if images directory exists
    if not os.path.exists(args.images_dir):
        logger.error(f"Images directory not found at {args.images_dir}")
        return 1
    
    # Load the model
    model = load_model(args.model)
    
    # Connect to the database based on the specified type
    conn = None
    if args.db_type == 'postgres':
        conn = connect_to_docker_postgres(args.host, args.port, args.dbname, args.user, args.password)
    elif args.db_type == 'mysql':
        conn = connect_to_docker_mysql(args.host, args.port, args.dbname, args.user, args.password)
    
    if not conn:
        logger.error("Failed to connect to the database")
        return 1
    
    # Update database schema
    if not update_database_schema(conn, args.db_type):
        logger.error("Failed to update database schema")
        conn.close()
        return 1
    
    # Process images
    success = process_database_images(conn, args.db_type, args.images_dir, model)
    
    # Close the connection
    conn.close()
    
    return 0 if success else 1

if __name__ == "__main__":
    sys.exit(main())