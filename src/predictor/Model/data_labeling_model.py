import torch
import torchvision.transforms as transforms
from torchvision import datasets, models
from torch.utils.data import DataLoader, random_split
import os
from pathlib import Path
import warnings
from tqdm import tqdm
warnings.filterwarnings('ignore', category=UserWarning)

# Define paths
dataset_path = "/Users/nathanmichelotti/Desktop/Senior Model/my_data"

# Verify directories exist
print("Verifying dataset structure...\n")
required_dirs = ['Sunny_With_Snow', 'Cloudy_With_Snow', 'Sunny_No_Snow', 'Cloudy_No_Snow']
for dir_name in required_dirs:
    dir_path = os.path.join(dataset_path, dir_name)
    if not os.path.exists(dir_path):
        raise FileNotFoundError(f"Directory not found: {dir_path}")

# Print directory structure
for root, dirs, files in os.walk(dataset_path):
    print(f"Directory: {root}")
    for d in dirs:
        print(f" Subdirectory: {d}")
    image_count = len([f for f in files if f.endswith(('.jpg', '.jpeg', '.png'))])
    if image_count > 0:
        print(f" Images found: {image_count}")

# Custom dataset loader with hierarchical labels
class CustomDataset(torch.utils.data.Dataset):
    def __init__(self, root_dir, transform=None):
        self.root_dir = root_dir
        self.transform = transform
        self.image_paths = []
        self.weather_labels = []  # 0 for Sunny, 1 for Cloudy
        self.snow_labels = []     # 0 for No Snow, 1 for Snow
        
        # Process all categories
        for category in ['Sunny_With_Snow', 'Cloudy_With_Snow', 'Sunny_No_Snow', 'Cloudy_No_Snow']:
            category_dir = os.path.join(root_dir, category)
            if os.path.exists(category_dir):
                for file in os.listdir(category_dir):
                    if file.endswith(('.jpg', '.jpeg', '.png')):
                        self.image_paths.append(os.path.join(category_dir, file))
                        
                        # Set weather label
                        is_cloudy = 'Cloudy' in category
                        self.weather_labels.append(1 if is_cloudy else 0)
                        
                        # Set snow label
                        has_snow = 'With_Snow' in category
                        self.snow_labels.append(1 if has_snow else 0)

    def __len__(self):
        return len(self.image_paths)

    def __getitem__(self, idx):
        img_path = self.image_paths[idx]
        image = datasets.folder.default_loader(img_path)
        weather_label = self.weather_labels[idx]
        snow_label = self.snow_labels[idx]
        
        if self.transform:
            image = self.transform(image)
            
        return image, weather_label, snow_label

# Enhanced data augmentation
transform = transforms.Compose([
    transforms.Resize((256, 256)),
    transforms.RandomCrop(224),
    transforms.RandomHorizontalFlip(),
    transforms.RandomRotation(10),
    transforms.ColorJitter(brightness=0.2, contrast=0.2),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
])

# Create dataset
dataset = CustomDataset(root_dir=dataset_path, transform=transform)

# Print dataset statistics
weather_counts = [0, 0]  # [Sunny, Cloudy]
snow_counts = [0, 0]     # [No Snow, Snow]
for _, weather_label, snow_label in dataset:
    weather_counts[weather_label] += 1
    snow_counts[snow_label] += 1

print(f"\nDataset Statistics:")
print(f"Total images: {len(dataset)}")
print(f"Weather - Sunny: {weather_counts[0]} images ({weather_counts[0]/len(dataset):.2%})")
print(f"Weather - Cloudy: {weather_counts[1]} images ({weather_counts[1]/len(dataset):.2%})")
print(f"Snow - No Snow: {snow_counts[0]} images ({snow_counts[0]/len(dataset):.2%})")
print(f"Snow - Snow: {snow_counts[1]} images ({snow_counts[1]/len(dataset):.2%})")

# Split data into train and validation
train_size = int(0.8 * len(dataset))
val_size = len(dataset) - train_size
train_dataset, val_dataset = random_split(dataset, [train_size, val_size])

# Create dataloaders
train_loader = DataLoader(train_dataset, batch_size=32, shuffle=True)
val_loader = DataLoader(val_dataset, batch_size=32, shuffle=False)

# Load Models - Multi-task model with two heads
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
print(f"\nUsing device: {device}")

# Initialize model
class MultiTaskResNet(torch.nn.Module):
    def __init__(self):
        super(MultiTaskResNet, self).__init__()
        # Load base model
        self.base_model = models.resnet34(pretrained=True)
        
        # Freeze early layers
        for param in self.base_model.parameters():
            param.requires_grad = False
            
        # Unfreeze final layers
        for param in self.base_model.layer4.parameters():
            param.requires_grad = True
            
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

# Initialize the model
model = MultiTaskResNet()
model = model.to(device)

# Define loss function and optimizer
criterion = torch.nn.CrossEntropyLoss()
optimizer = torch.optim.Adam(model.parameters(), lr=0.001)
scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(optimizer, mode='min', patience=2)

# Training loop with validation
num_epochs = 30
best_val_loss = float('inf')
print("\nStarting training...")

for epoch in range(num_epochs):
    # Training phase
    model.train()
    train_loss = 0
    weather_correct = 0
    snow_correct = 0
    total = 0
    
    # Add progress bar for training
    train_pbar = tqdm(train_loader, desc=f"Epoch {epoch+1}/{num_epochs} [Train]", leave=False)
    for images, weather_labels, snow_labels in train_pbar:
        images = images.to(device)
        weather_labels = weather_labels.to(device)
        snow_labels = snow_labels.to(device)
        
        # Forward pass
        optimizer.zero_grad()
        weather_outputs, snow_outputs = model(images)
        
        # Calculate loss
        weather_loss = criterion(weather_outputs, weather_labels)
        snow_loss = criterion(snow_outputs, snow_labels)
        loss = weather_loss + snow_loss  # Equal weighting for both tasks
        
        # Backward pass
        loss.backward()
        optimizer.step()
        
        train_loss += loss.item()
        
        # Calculate accuracy
        _, weather_predicted = torch.max(weather_outputs.data, 1)
        _, snow_predicted = torch.max(snow_outputs.data, 1)
        total += weather_labels.size(0)
        weather_correct += (weather_predicted == weather_labels).sum().item()
        snow_correct += (snow_predicted == snow_labels).sum().item()
        
        # Update progress bar with current loss and accuracies
        weather_acc = 100 * weather_correct / total
        snow_acc = 100 * snow_correct / total
        train_pbar.set_postfix(loss=f"{loss.item():.4f}", w_acc=f"{weather_acc:.2f}%", s_acc=f"{snow_acc:.2f}%")
    
    # Validation phase
    model.eval()
    val_loss = 0
    val_weather_correct = 0
    val_snow_correct = 0
    val_total = 0
    
    # Add progress bar for validation
    val_pbar = tqdm(val_loader, desc=f"Epoch {epoch+1}/{num_epochs} [Valid]", leave=False)
    with torch.no_grad():
        for images, weather_labels, snow_labels in val_pbar:
            images = images.to(device)
            weather_labels = weather_labels.to(device)
            snow_labels = snow_labels.to(device)
            
            # Forward pass
            weather_outputs, snow_outputs = model(images)
            
            # Calculate loss
            weather_loss = criterion(weather_outputs, weather_labels)
            snow_loss = criterion(snow_outputs, snow_labels)
            loss = weather_loss + snow_loss
            
            val_loss += loss.item()
            
            # Calculate accuracy
            _, weather_predicted = torch.max(weather_outputs.data, 1)
            _, snow_predicted = torch.max(snow_outputs.data, 1)
            val_total += weather_labels.size(0)
            val_weather_correct += (weather_predicted == weather_labels).sum().item()
            val_snow_correct += (snow_predicted == snow_labels).sum().item()
            
            # Update progress bar
            val_w_acc = 100 * val_weather_correct / val_total
            val_s_acc = 100 * val_snow_correct / val_total
            val_pbar.set_postfix(loss=f"{loss.item():.4f}", w_acc=f"{val_w_acc:.2f}%", s_acc=f"{val_s_acc:.2f}%")
    
    # Calculate average metrics
    train_loss = train_loss / len(train_loader)
    val_loss = val_loss / len(val_loader)
    weather_accuracy = 100 * weather_correct / total
    snow_accuracy = 100 * snow_correct / total
    val_weather_accuracy = 100 * val_weather_correct / val_total
    val_snow_accuracy = 100 * val_snow_correct / val_total
    
    # Print progress
    print(f"Epoch [{epoch+1}/{num_epochs}]")
    print(f"Train Loss: {train_loss:.4f}, Weather Acc: {weather_accuracy:.2f}%, Snow Acc: {snow_accuracy:.2f}%")
    print(f"Val Loss: {val_loss:.4f}, Weather Acc: {val_weather_accuracy:.2f}%, Snow Acc: {val_snow_accuracy:.2f}%")
    
    # Learning rate scheduling
    scheduler.step(val_loss)
    
    # Save best model
    if val_loss < best_val_loss:
        best_val_loss = val_loss
        torch.save(model.state_dict(), "best_model.pth")
        print(f"New best model saved with validation loss: {val_loss:.4f}")

print("\nTraining Complete!")