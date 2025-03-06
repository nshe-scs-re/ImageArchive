import os
import cv2
import numpy as np


def resize_with_aspect_ratio(image, target_width, target_height, padding_color=(0, 0, 0)):
    """
    Resize an image while maintaining the aspect ratio, adding padding as needed.
    """
    # Get original dimensions
    original_height, original_width = image.shape[:2]
    aspect_ratio = original_width / original_height

    # Determine new dimensions based on target size and aspect ratio
    if (target_width / target_height) > aspect_ratio:
        # Fit by height
        new_height = target_height
        new_width = int(target_height * aspect_ratio)
    else:
        # Fit by width
        new_width = target_width
        new_height = int(target_width / aspect_ratio)

    # Resize the image to the new dimensions
    resized_image = cv2.resize(
        image, (new_width, new_height), interpolation=cv2.INTER_AREA
    )

    # Create a new image with the target dimensions and the padding color
    padded_image = np.full(
        (target_height, target_width, 3), padding_color, dtype=np.uint8
    )

    # Center the resized image in the padded output
    x_offset = (target_width - new_width) // 2
    y_offset = (target_height - new_height) // 2
    padded_image[
        y_offset:y_offset + new_height, x_offset:x_offset + new_width
    ] = resized_image

    return padded_image


def process_images(input_path, output_path, target_width=256, target_height=256, padding_color=(0, 0, 0)):
    """
    Process images from the input path, resize them, and save to the output path.
    """
    # Ensure the output directory exists
    os.makedirs(output_path, exist_ok=True)

    # If input path is a single image file
    if os.path.isfile(input_path):
        # Read the single image
        image = cv2.imread(input_path)
        if image is None:
            print(f"Error: Could not read image {input_path}")
            return

        # Resize the image
        resized_image = resize_with_aspect_ratio(
            image, target_width, target_height, padding_color
        )

        # Save the processed image
        output_file_name = os.path.basename(input_path)
        output_file_path = os.path.join(output_path, output_file_name)
        cv2.imwrite(output_file_path, resized_image)
        print(f"Processed and saved: {output_file_path}")

    # If input path is a directory
    elif os.path.isdir(input_path):
        # Iterate through all files in the input directory
        for file_name in os.listdir(input_path):
            input_file_path = os.path.join(input_path, file_name)

            # Check if the file is an image
            if file_name.lower().endswith(('.png', '.jpg', '.jpeg', '.bmp', '.tiff')):
                # Read the image
                image = cv2.imread(input_file_path)
                if image is None:
                    print(f"Warning: Could not read image {input_file_path}")
                    continue

                # Resize the image
                resized_image = resize_with_aspect_ratio(
                    image, target_width, target_height, padding_color
                )

                # Save the processed image to the output path
                output_file_path = os.path.join(output_path, file_name)
                cv2.imwrite(output_file_path, resized_image)
                print(f"Processed and saved: {output_file_path}")

    else:
        print(f"Error: {input_path} is neither a file nor a directory.")


if __name__ == "__main__":
    # Define input and output paths
    input_directory = "/Users/nathanmichelotti/Desktop/College/Fall 2024/CS 425/Senior Project/Testing Input"
    output_directory = "/Users/nathanmichelotti/Desktop/College/Fall 2024/CS 425/Senior Project/Testing Output"

    # Process images
    process_images(input_directory, output_directory)
