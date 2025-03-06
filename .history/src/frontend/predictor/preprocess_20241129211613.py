import os
import cv2
import numpy as np


def load_and_preprocess_images(image_dir, target_size=(128, 128)):
    images = []
    for filename in os.listdir(image_dir):
        img_path = os.path.join(image_dir, filename)
        img = cv2.imread(img_path)
        if img is not None:
            img = cv2.resize(img, target_size) / 255.0  # Normalize to [0, 1]
            images.append(img)
    return np.array(images)
