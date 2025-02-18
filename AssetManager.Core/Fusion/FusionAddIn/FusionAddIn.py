import adsk.core, adsk.fusion, adsk.cam, traceback
import os
import requests

# Asset Manager API Endpoint
ASSET_MANAGER_API = "http://localhost:5000/api/models"

def run(context=None):
    try:
        app = adsk.core.Application.get()
        ui = app.userInterface

        # Step 1: Fetch Model Info from Asset Manager
        model_info = get_selected_model()
        if not model_info:
            ui.messageBox("❌ No models available in Asset Manager.")
            return "No models found"

        model_id = model_info["model_id"]
        model_name = model_info["model_name"]
        
        # Step 2: Download Model File
        model_path = fetch_model_from_manager(model_id, model_name)
        if not model_path:
            ui.messageBox("❌ Failed to download model.")
            return "Download failed"

        # Debug: Check the file path
        ui.messageBox(f"✅ Model downloaded to: {model_path}", "Fusion Debug")

        # Step 3: Open Model in Fusion 360
        open_fusion_model(model_path)

        return f"Model Opened: {model_name}"
    
    except Exception as e:
        ui.messageBox(f"❌ Error: {traceback.format_exc()}", "Fusion 360 Error")
        return str(e)

def get_selected_model():
    """Retrieve the latest model ID and name from Asset Manager."""
    try:
        response = requests.get(f"{ASSET_MANAGER_API}/latest")
        response.raise_for_status()
        return response.json()
    except requests.RequestException as e:
        return None

def fetch_model_from_manager(model_id, model_name):
    """Download the model file from Asset Manager API."""
    try:
        model_extension = ".f3d"  # Adjust if necessary
        download_url = f"{ASSET_MANAGER_API}/download/{model_id}"
        save_path = os.path.join(os.path.expanduser("~/Downloads"), f"{model_name}{model_extension}")

        response = requests.get(download_url, stream=True)
        response.raise_for_status()

        with open(save_path, "wb") as file:
            for chunk in response.iter_content(chunk_size=8192):
                file.write(chunk)

        return save_path
    except requests.RequestException:
        return None

def open_fusion_model(file_path):
    """Opens the downloaded model in Fusion 360."""
    try:
        app = adsk.core.Application.get()
        ui = app.userInterface

        # Check if file exists
        if not os.path.exists(file_path):
            ui.messageBox(f"❌ Model file not found: {file_path}", "Fusion Error")
            return

        # Debug: Show the file path before opening
        ui.messageBox(f"Opening model: {file_path}", "Fusion 360 Debug")

        # Open the model
        doc = app.documents.open(file_path)
        if doc:
            ui.messageBox(f"✅ Successfully opened model: {file_path}", "Fusion 360")
        else:
            ui.messageBox("❌ Fusion 360 failed to open the model.", "Fusion 360 Error")

    except Exception as e:
        ui.messageBox(f"❌ Error opening model: {traceback.format_exc()}", "Fusion 360 Error")
