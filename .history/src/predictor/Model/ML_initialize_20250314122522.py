import tkinter as tk
from tkinter import filedialog, messagebox
from PIL import Image, ImageTk
import torch
from torchvision import models, transforms
import os

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

if __name__ == "__main__":
    pass