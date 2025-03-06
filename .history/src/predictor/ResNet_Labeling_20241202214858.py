import os


def count_images_in_folder(folder_path):
    image_extensions = ('.png', '.jpg', '.jpeg', '.bmp', '.tiff', '.gif')
    image_count = 0

    for root, _, files in os.walk(folder_path):
        for file in files:
            if file.lower().endswith(image_extensions):
                image_count += 1

    return image_count


if __name__ == "__main__":
    folder_path = "/Users/nathanmichelotti/Desktop/College/Fall 2024/CS 425/Senior Project/OneDrive_1_10-22-2024/Images"
    total_images = count_images_in_folder(folder_path)
    print(f"Total images in '{folder_path}': {total_images}")
