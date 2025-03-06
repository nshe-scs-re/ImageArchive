import torch
import torchvision.transforms as transforms
from torchvision import datasets, models
from torch.utils.data import DataLoader, random_split
import torch.nn as nn
import torch.optim as optim
from tqdm import tqdm 

# **Set Device (GPU if available)**
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
print(f"Using device: {device}")

# **Define Transformations**
transform = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.RandomHorizontalFlip(),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
])

# **Load Dataset**
dataset = datasets.ImageFolder(root="my_data/Train", transform=transform)

# **Split into Train & Validation**
train_size = int(0.8 * len(dataset))  # 80% Train, 20% Validation
val_size = len(dataset) - train_size
train_dataset, val_dataset = random_split(dataset, [train_size, val_size])

train_loader = DataLoader(train_dataset, batch_size=32, shuffle=True)
val_loader = DataLoader(val_dataset, batch_size=32, shuffle=False)

# **Modify ResNet18 for Multi-Label Classification**
model = models.resnet18(pretrained=True)
num_features = model.fc.in_features
model.fc = nn.Linear(num_features, 2)  # 2 independent outputs (Sunny/Cloudy, Has Snow/No Snow)
model = model.to(device)

# **Define Loss & Optimizer**
criterion = nn.BCEWithLogitsLoss()
optimizer = optim.Adam(model.parameters(), lr=0.001)

# **Training Loop with tqdm Progress Bar**
num_epochs = 10
for epoch in range(num_epochs):
    model.train()
    total_loss = 0
    train_progress = tqdm(train_loader, desc=f"Epoch {epoch+1}/{num_epochs} [Training]")

    for images, labels in train_progress:
        images = images.to(device)
        
        # Convert Labels to Binary Format
        binary_labels = torch.zeros((labels.size(0), 2)).to(device)
        binary_labels[:, 0] = (labels == 2).float()  # Sunny
        binary_labels[:, 1] = (labels == 0).float()  # Has_Snow

        optimizer.zero_grad()
        outputs = model(images)
        loss = criterion(outputs, binary_labels)
        loss.backward()
        optimizer.step()
        total_loss += loss.item()

        train_progress.set_postfix(loss=f"{loss.item():.4f}")

    # **Validation with tqdm Progress Bar**
    model.eval()
    correct_sunny, correct_snow = 0, 0
    total = 0
    val_progress = tqdm(val_loader, desc=f"Epoch {epoch+1}/{num_epochs} [Validation]", leave=False)

    with torch.no_grad():
        for images, labels in val_progress:
            images = images.to(device)
            labels = labels.to(device)
            
            binary_labels = torch.zeros((labels.size(0), 2)).to(device)
            binary_labels[:, 0] = (labels == 2).float()  # Sunny
            binary_labels[:, 1] = (labels == 0).float()  # Has_Snow

            outputs = model(images)
            preds = torch.sigmoid(outputs) > 0.5  # Convert logits to binary (0 or 1)
            
            correct_sunny += (preds[:, 0] == binary_labels[:, 0]).sum().item()
            correct_snow += (preds[:, 1] == binary_labels[:, 1]).sum().item()
            total += labels.size(0)

    print(f"Epoch [{epoch+1}/{num_epochs}], Loss: {total_loss/len(train_loader):.4f}, "
          f"Sunny Acc: {correct_sunny/total:.4f}, Snow Acc: {correct_snow/total:.4f}")

print("Training Complete!")
