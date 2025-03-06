from tensorflow.keras.models import load_model
from preprocess import load_images


def predict(image_path):
    model = load_model("model.h5")
    image = load_images(image_path)
    prediction = model.predict(image)
    return prediction


if __name__ == "__main__":
    image_path = "path/to/new/image.jpg"
    result = predict(image_path)
    print("Prediction:", result)
