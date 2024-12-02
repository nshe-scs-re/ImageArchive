from model import create_model
from preprocess import load_images
from tensorflow.keras.utils import to_categorical
import numpy as np

# Load data
images = load_images("path/to/images")
labels = np.array([0, 1, 1, 0])  # Example labels for 4 images
labels = to_categorical(labels, num_classes=2)

# Create model
model = create_model()

# Train model
model.fit(images, labels, epochs=10, batch_size=2)

# Save model
model.save("model.h5")
