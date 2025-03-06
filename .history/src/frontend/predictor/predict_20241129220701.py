from tensorflow.keras.models import load_model
from tensorflow.keras.preprocessing.image import load_img, img_to_array
import numpy as np

# Load the model
model = load_model("snow_detection_model.h5")

# Load and preprocess an image


def predict_image(image_path):
    image = load_img(image_path, target_size=(128, 128))
    image_array = img_to_array(image) / 255.0
    image_array = np.expand_dims(image_array, axis=0)  # Add batch dimension

    prediction = model.predict(image_array)
    if prediction[0][0] > 0.5:
        print("Snow detected")
    else:
        print("No snow detected")


# Test with a new image
predict_image("path/to/new/image.jpg")
