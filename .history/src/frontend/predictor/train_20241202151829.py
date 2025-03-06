import os
import tensorflow as tf
from model import create_cnn_model
from tensorflow.keras.preprocessing.image import ImageDataGenerator
from tensorflow.keras.callbacks import ModelCheckpoint, EarlyStopping


def train_model(data_dir, output_model_path, input_shape=(256, 256, 3), batch_size=32, epochs=20):
    # Data Generators
    train_datagen = ImageDataGenerator(
        rescale=1./255,
        rotation_range=30,
        width_shift_range=0.2,
        height_shift_range=0.2,
        shear_range=0.2,
        zoom_range=0.2,
        horizontal_flip=True,
        validation_split=0.2
    )

    train_generator = train_datagen.flow_from_directory(
        os.path.join(data_dir, 'train'),
        target_size=input_shape[:2],
        batch_size=batch_size,
        class_mode='binary',
        subset='training'
    )

    validation_generator = train_datagen.flow_from_directory(
        os.path.join(data_dir, 'train'),
        target_size=input_shape[:2],
        batch_size=batch_size,
        class_mode='binary',
        subset='validation'
    )

    # Create Model
    model = create_cnn_model(input_shape=input_shape, num_classes=1)
    model.compile(optimizer='adam', loss='binary_crossentropy',
                  metrics=['accuracy'])

    # Callbacks
    checkpoint = ModelCheckpoint(
        output_model_path, save_best_only=True, monitor='val_accuracy', mode='max')
    early_stopping = EarlyStopping(
        monitor='val_loss', patience=5, restore_best_weights=True)

    # Train the Model
    history = model.fit(
        train_generator,
        validation_data=validation_generator,
        epochs=epochs,
        callbacks=[checkpoint, early_stopping]
    )

    return model, history


# Example Usage
if __name__ == "__main__":
    data_dir = "/path/to/your/dataset"
    output_model_path = "model.h5"
    train_model(data_dir, output_model_path)
