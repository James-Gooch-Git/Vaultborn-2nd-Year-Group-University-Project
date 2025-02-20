import sys
import time
from subprocess import Popen
import subprocess
import adsk.core, adsk.fusion, adsk.cam, traceback
import json
import os

def read_metadata(model_path):
    """Reads metadata JSON for the model."""
    metadata_file = model_path + ".json"

    if not os.path.exists(metadata_file):
        return None

    with open(metadata_file, "r") as file:
        metadata = json.load(file)

    return metadata

def save_to_correct_location(model_path):
    """Reads metadata and sets Fusion's save path accordingly."""
    metadata = read_metadata(model_path)
    if not metadata:
        ui.messageBox("⚠️ No metadata found for this model.")
        return

    project_id = metadata.get("ProjectId", "Unknown")
    folder_path = metadata.get("FolderPath", None)

    ui.messageBox(f"🔹 Model belongs to Project: {project_id}\nSaving to: {folder_path}")

# Assume model path is passed when opening Fusion 360
if __name__ == "__main__":
    import sys
    if len(sys.argv) > 1:
        model_path = sys.argv[1]
        save_to_correct_location(model_path)
