import tkinter as tk
from tkinter import filedialog, messagebox
from PIL import Image, ImageTk
import torch
from torchvision import models, transforms
import os

class WeatherClassifierApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Weather Image Classifier")
        self.root.geometry("800x600")
        
        # Initialize model
        self.setup_model()
        
        # Create GUI elements
        self.create_widgets()
        
    def setup_model(self):
        # Set up device and model
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.model = models.resnet34(pretrained=False)
        self.model.fc = torch.nn.Sequential(
            torch.nn.Dropout(0.5),
            torch.nn.Linear(self.model.fc.in_features, 512),
            torch.nn.ReLU(),
            torch.nn.Dropout(0.3),
            torch.nn.Linear(512, 4)
        )
        
        # Load model weights
        model_path = "best_model.pth"
        if not os.path.exists(model_path):
            messagebox.showerror("Error", f"Model file not found at {model_path}")
            self.root.destroy()
            return
            
        self.model.load_state_dict(torch.load(model_path, map_location=self.device))
        self.model.to(self.device)
        self.model.eval()
        
        # Set up image transformation
        self.transform = transforms.Compose([
            transforms.Resize((224, 224)),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
        ])
        
    def create_widgets(self):
        # Create main frame
        main_frame = tk.Frame(self.root, padx=20, pady=20)
        main_frame.pack(expand=True, fill='both')
        
        # Create and pack widgets
        self.create_button = tk.Button(main_frame, text="Select Image", command=self.select_image)
        self.create_button.pack(pady=10)
        
        # Image display
        self.image_frame = tk.Frame(main_frame, width=400, height=400)
        self.image_frame.pack(pady=10)
        self.image_label = tk.Label(self.image_frame, text="No image selected")
        self.image_label.pack()
        
        # Results display
        self.results_frame = tk.Frame(main_frame)
        self.results_frame.pack(pady=10, fill='x')
        
        # Results labels
        self.prediction_label = tk.Label(self.results_frame, text="Prediction: ")
        self.prediction_label.pack()
        self.confidence_label = tk.Label(self.results_frame, text="Confidence: ")
        self.confidence_label.pack()
        self.snow_prob_label = tk.Label(self.results_frame, text="Snow Probability: ")
        self.snow_prob_label.pack()
        self.no_snow_prob_label = tk.Label(self.results_frame, text="No Snow Probability: ")
        self.no_snow_prob_label.pack()
        
    def select_image(self):
        # Open file dialog
        file_path = filedialog.askopenfilename(
            filetypes=[("Image files", "*.jpg *.jpeg *.png *.bmp *.gif *.tiff")]
        )
        
        if file_path:
            try:
                # Load and display image
                image = Image.open(file_path)
                self.display_image(image)
                
                # Classify image
                self.classify_image(image)
                
            except Exception as e:
                messagebox.showerror("Error", f"Error processing image: {str(e)}")
    
    def display_image(self, image):
        # Resize image for display
        display_size = (380, 380)
        image.thumbnail(display_size, Image.Resampling.LANCZOS)
        
        # Convert to PhotoImage
        photo = ImageTk.PhotoImage(image)
        
        # Update image label
        self.image_label.configure(image=photo)
        self.image_label.image = photo  # Keep a reference
        
    def classify_image(self, image):
        try:
            # Preprocess image
            image_tensor = self.transform(image).unsqueeze(0).to(self.device)
            
            # Get prediction
            with torch.no_grad():
                output = self.model(image_tensor)
                probabilities = torch.softmax(output, dim=1)[0]
                
                # Get class probabilities
                sunny_snow_prob = probabilities[0].item()
                cloudy_snow_prob = probabilities[1].item()
                sunny_no_snow_prob = probabilities[2].item()
                cloudy_no_snow_prob = probabilities[3].item()
                
                # Determine overall prediction
                snow_prob = sunny_snow_prob + cloudy_snow_prob
                no_snow_prob = sunny_no_snow_prob + cloudy_no_snow_prob
                sunny_prob = sunny_snow_prob + sunny_no_snow_prob
                cloudy_prob = cloudy_snow_prob + cloudy_no_snow_prob
                
                # Get main prediction
                conditions = []
                if sunny_prob > cloudy_prob:
                    conditions.append("Sunny")
                else:
                    conditions.append("Cloudy")
                    
                if snow_prob > no_snow_prob:
                    conditions.append("with Snow")
                else:
                    conditions.append("No Snow")
                
                prediction = " ".join(conditions)
                confidence = max(probabilities).item()
            
            # Update results
            self.prediction_label.config(text=f"Prediction: {prediction}")
            self.confidence_label.config(text=f"Confidence: {confidence:.2%}")
            self.snow_prob_label.config(text=f"Snow Probability: {snow_prob:.2%}")
            self.no_snow_prob_label.config(text=f"No Snow Probability: {no_snow_prob:.2%}")
            
        except Exception as e:
            messagebox.showerror("Error", f"Error classifying image: {str(e)}")

if __name__ == "__main__":
    root = tk.Tk()
    app = WeatherClassifierApp(root)
    root.mainloop() 