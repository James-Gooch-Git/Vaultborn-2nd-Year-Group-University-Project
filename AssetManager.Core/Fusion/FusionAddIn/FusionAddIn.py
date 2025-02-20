import adsk.core, adsk.fusion, adsk.cam, traceback
import json
import os
from datetime import datetime

app = adsk.core.Application.get()
ui = app.userInterface
handlers = []  # Event handlers must be stored to keep them alive

def get_metadata(model_path):
    """ Reads metadata file if available. """
    metadata_path = model_path + ".metadata.json"
    if not os.path.exists(metadata_path):
        return None

    with open(metadata_path, "r") as f:
        return json.load(f)

def save_metadata(model_path, metadata):
    """ Writes updated metadata back to file. """
    metadata_path = model_path + ".metadata.json"
    with open(metadata_path, "w") as f:
        json.dump(metadata, f, indent=4)

def on_document_saved(event_args):
    """ Event triggered when a model is saved. Updates metadata with a new version entry. """
    try:
        doc = event_args.document
        model_name = doc.name  # Only gets the filename, no extension
        save_directory = os.path.join(os.path.expanduser("~"), "Documents", "DownloadedModels")
        full_model_path = os.path.join(save_directory, model_name)

        metadata = get_metadata(full_model_path)
        if not metadata:
            ui.messageBox(f"⚠️ No metadata found for {model_name}. Saving aborted.")
            return

        # Increment version and add new entry
        new_version = len(metadata["versions"]) + 1
        metadata["versions"].append({
            "version": new_version,
            "timestamp": datetime.utcnow().isoformat(),
            "savedBy": app.currentUser.displayName  # Get Fusion 360 user
        })

        # Save updated metadata
        save_metadata(full_model_path, metadata)
        ui.messageBox(f"✅ Model {model_name} saved. Version {new_version} recorded.")

    except Exception as e:
        ui.messageBox(f"❌ Error updating metadata: {str(e)}")

def run(context):
    try:
        # Listen for document save event
        app = adsk.core.Application.get()
        event = app.documentSaving
        handler = adsk.core.ApplicationCommandEventHandler(on_document_saved)
        event.add(handler)
        handlers.append(handler)  # Prevent garbage collection

        ui.messageBox("🔹 Fusion Add-In Loaded: Auto-Versioning Enabled")
    except Exception as e:
        ui.messageBox(f"❌ Add-In Failed to Start: {str(e)}")

def stop(context):
    ui.messageBox("🔹 Fusion Add-In Stopped")
