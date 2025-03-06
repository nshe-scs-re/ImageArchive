import torch
import torchvision.transforms as transforms
from torchvision import datasets, models
from torch.utils.data import DataLoader, random_split
import torch.nn as nn
import torch.optim as optim
from tqdm import tqdm
import logging
from pathlib import Path
import warnings

# Filter out the torchvision image loading warnings
warnings.filterwarnings('ignore', category=UserWarning)

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class WeatherClassifier:
    def __init__(self):
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        logger.info(f"Using device: {self.device}")
        
        # Enhanced transformations
        self.transform = transforms.Compose([
            transforms.Resize(256),
            transforms.RandomCrop(224),
            transforms.RandomHorizontalFlip(),
            transforms.RandomAutocontrast(),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
        ])
        
        # Use ResNet50 with pretrained weights
        self.model = models.resnet50(weights='IMAGENET1K_V2')
        self.model.fc = nn.Sequential(
            nn.Dropout(0.2),
            nn.Linear(self.model.fc.in_features, 3)  # 3 classes: Has_Snow, Cloudy, Sunny
        )
        self.model = self.model.to(self.device)
        
        self.criterion = nn.CrossEntropyLoss()
        self.optimizer = optim.AdamW(self.model.parameters(), lr=0.0001, weight_decay=0.01)
        self.scheduler = optim.lr_scheduler.ReduceLROnPlateau(self.optimizer, mode='max', 
                                                            patience=2, factor=0.1)

    def train(self, train_loader, val_loader, num_epochs=20):
        best_acc = 0.0
        for epoch in range(num_epochs):
            # Training phase
            self.model.train()
            running_loss = 0.0
            train_progress = tqdm(train_loader, desc=f"Epoch {epoch+1}/{num_epochs} [Training]")
            
            for images, labels in train_progress:
                images, labels = images.to(self.device), labels.to(self.device)
                
                self.optimizer.zero_grad()
                outputs = self.model(images)
                loss = self.criterion(outputs, labels)
                loss.backward()
                self.optimizer.step()
                
                running_loss += loss.item()
                train_progress.set_postfix(loss=f"{loss.item():.4f}")

            # Validation phase
            val_acc = self.validate(val_loader, epoch, num_epochs)
            
            # Learning rate scheduling
            self.scheduler.step(val_acc)
            
            # Save best model
            if val_acc > best_acc:
                best_acc = val_acc
                torch.save(self.model.state_dict(), "best_model.pth")
                logger.info(f"New best model saved with accuracy: {best_acc:.4f}")

    def validate(self, val_loader, epoch, num_epochs):
        self.model.eval()
        correct = 0
        total = 0
        val_progress = tqdm(val_loader, desc=f"Epoch {epoch+1}/{num_epochs} [Validation]")
        
        with torch.no_grad():
            for images, labels in val_progress:
                images, labels = images.to(self.device), labels.to(self.device)
                outputs = self.model(images)
                _, predicted = torch.max(outputs.data, 1)
                total += labels.size(0)
                correct += (predicted == labels).sum().item()
        
        accuracy = correct / total
        logger.info(f"Epoch [{epoch+1}/{num_epochs}], Validation Accuracy: {accuracy:.4f}")
        return accuracy

def prepare_data(data_path="/Users/nathanmichelotti/Desktop/Senior Model/my_data", batch_size=32):
    data_path = Path(data_path)
    
    if not data_path.exists():
        raise FileNotFoundError(f"Data directory not found at {data_path}")
    
    # Verify the expected class directories exist
    expected_classes = {"Has_Snow", "Cloudy", "Sunny"}
    found_classes = {d.name for d in data_path.iterdir() if d.is_dir()}
    if not expected_classes.issubset(found_classes):
        missing = expected_classes - found_classes
        raise ValueError(f"Missing required class directories: {missing}")
    
    logger.info(f"Found classes: {found_classes}")
    
    # Create dataset directly from the main directory
    dataset = datasets.ImageFolder(root=data_path, transform=WeatherClassifier().transform)
    logger.info(f"Found {len(dataset)} images in total")
    logger.info(f"Class mapping: {dataset.class_to_idx}")
    
    # Split dataset
    train_size = int(0.8 * len(dataset))
    val_size = len(dataset) - train_size
    train_dataset, val_dataset = random_split(dataset, [train_size, val_size])
    
    train_loader = DataLoader(train_dataset, batch_size=batch_size, shuffle=True, num_workers=4)
    val_loader = DataLoader(val_dataset, batch_size=batch_size, shuffle=False, num_workers=4)
    
    logger.info(f"Dataset split - Train: {len(train_dataset)}, Validation: {len(val_dataset)}")
    return train_loader, val_loader

if __name__ == "__main__":
    try:
        # Use the exact path to your data directory
        train_loader, val_loader = prepare_data(data_path="/Users/nathanmichelotti/Desktop/Senior Model/my_data")
        classifier = WeatherClassifier()
        classifier.train(train_loader, val_loader)
        logger.info("Training Complete!")
    except Exception as e:
        logger.error(f"An error occurred during training: {str(e)}")
