import tensorflow as tf
import numpy as np
from tensorflow.keras.preprocessing.image import load_img, img_to_array
from model import create_cnn_model


def predict_image(image_path, model_path, input_shape=(256, 256, 3)):
    # Load the trained model
    model = tf.keras.models.load_model(model_path)

    # Load and preprocess the image
    image = load_img(image_path, target_size=input_shape[:2])
    image_array = img_to_array(image) / 255.0  # Normalize
    image_array = np.expand_dims(image_array, axis=0)  # Add batch dimension

    # Predict
    prediction = model.predict(image_array)

    # Interpret prediction
    if prediction[0] > 0.5:
        return "Snow"
    else:
        return "No Snow"


# Example Usage
if __name__ == "__main__":
    image_path = "dataset"
    model_path = "model.h5"
    result = predict_image(image_path, model_path)
    print(f"Prediction: {result}")
