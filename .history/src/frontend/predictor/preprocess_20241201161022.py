import cv2
import numpy as np


def resize_with_aspect_ratio(image, target_width, target_height, padding_color=(0, 0, 0)):
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
        image, (new_width, new_height), interpolation=cv2.INTER_AREA)

    # Create a new image with the target dimensions and the padding color
    padded_image = np.full((target_height, target_width, 3),
                           padding_color, dtype=np.uint8)

    # Center the resized image in the padded output
    x_offset = (target_width - new_width) // 2
    y_offset = (target_height - new_height) // 2
    padded_image[y_offset:y_offset+new_height,
                 x_offset:x_offset+new_width] = resized_image

    return padded_image
