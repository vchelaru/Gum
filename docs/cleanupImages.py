import os
import sys


def search_markdown_folder(markdown_folder, image_name):
    #print("  " * level + f"Searching in folder: {markdown_folder} for image: {image_name}")
    for root, dirs, files in os.walk(markdown_folder):
        
        for file in files:
            if file.lower().endswith('.md'):
                markdown_file_path = os.path.join(root, file)
                if is_image_in_markdown(markdown_file_path, image_name):
                    files_to_analyze[filename].append(markdown_file_path)
                    return True

    return False

def is_image_in_markdown(markdown_file_path, image_name):
    with open(markdown_file_path, 'r', encoding='utf-8') as file:
        content = file.read()
        if image_name in content:
            return True
    return False





gum_git_folder = "c:/git/gum"
image_relative_path = "docs/.gitbook/assets"

image_full_path = os.path.join(gum_git_folder, image_relative_path)
print(image_full_path)

all_items = os.listdir(image_full_path)
print(len(all_items))
print(all_items[0])
print(os.path.splitext(all_items[0]))

files_to_analyze = {}

extensions = {}
for item in all_items:
    full_path_item = os.path.join(image_full_path, item)
    if os.path.isfile(full_path_item):
        filename = os.path.splitext(item)[0]
        extension = os.path.splitext(item)[1]
        extensions[extension] = extensions.get(extension, 0) + 1
        files_to_analyze[item] = []

        # Analysis on why we have things like .sh (setup_gum_mac.sh)
        # if extension.lower() not in ('.gif', '.svg', '.webp', '.png', '.mp4', '.jpg', '.jpeg'):
        #     print(full_path_item)

        # if extension.lower() in ['.gif', '.svg', '.webp', '.png', '.mp4', '.jpg', '.jpeg, '.avif']:
        #print(full_path_item)


        
    #sys.exit(0)

print(f"Asset extension analysis: {extensions}")

file_index = 0
total_files = len(files_to_analyze)
for filename in files_to_analyze.keys():
    file_index += 1
    print(f"Analyzing image: {filename} ({file_index} of {total_files})")   
    found_in_markdown = search_markdown_folder(os.path.join(gum_git_folder, "docs"), filename)
    files_to_analyze[filename] = found_in_markdown


# get list of unused images
unused_images = []
for filename, markdown_files in files_to_analyze.items():
    if not markdown_files:
        #print(f"Image not used: {filename}")
        unused_images.append(filename)
        # Optionally delete the unused image file
        # full_path_item = os.path.join(image_full_path, filename)
        # os.remove(full_path_item)
        # print(f"Deleted unused image file: {full_path_item}")

print(f"Total unused images: {len(unused_images)}")