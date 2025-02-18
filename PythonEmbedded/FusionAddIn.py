""" 
import os
import locale
import sys

# Force UTF-8 Encoding for Output
sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

print(f"✅ Encoding set to {locale.getpreferredencoding()}")


sys.path.append(r"C:\Program Files\Autodesk\Autodesk Fusion 360\Python\Lib\site-packages")

try:
    import adsk.core
    import adsk.fusion
    import adsk.cam
    print("✅ Successfully imported adsk!")
except ModuleNotFoundError:
    print("❌ Error: 'adsk' module still not found.")

try:
    import adsk.core
    import adsk.fusion
    import adsk.cam
    print("Successfully imported adsk!")
except ModuleNotFoundError:
    print("Error: 'adsk' module still not found.")


print(sys.path)

script_dir = os.path.dirname(os.path.abspath(__file__))

# Define the relative path to the Fusion API modules
fusion_api_path = os.path.join(script_dir, "..", "..", "..", "..", "..", "PythonEmbedded", "Lib")

if fusion_api_path not in sys.path:
    sys.path.append(fusion_api_path)
    
import adsk.core, adsk.fusion, adsk.cam, traceback
import requests

# Fusion 360 App Context
app = adsk.core.Application.get()
ui = app.userInterface

# API Endpoint of Asset Manager (Modify this to match your C# API server)`
ASSET_MANAGER_API = "http://localhost:5000/api/models"

# Event Handlers
handlers = []

def run(context):
     """
"""Executed when the add-in is started. """
"""
    try:
        # Create command definition
        command_def = ui.commandDefinitions.addButtonDefinition(
            'LoadAssetCmd', 'Load Asset', 'Load a model from Asset Manager into Fusion 360'
        )
        command_def.commandCreated.add(command_created)
        handlers.append(command_def)

        # Add button to UI
        workspace = ui.workspaces.itemById('FusionSolidEnvironment')
        toolbar_panel = workspace.toolbarPanels.itemById('SolidScriptsAddinsPanel')
        toolbar_panel.controls.addCommand(command_def)

        ui.messageBox('Asset Manager Add-in Loaded Successfully!')
    except Exception as e:
        ui.messageBox(f'Error in Add-in Initialization: {str(e)}')

def stop(context):
     """
"""Executed when the add-in is stopped. """
"""
    ui.messageBox('Unloading Asset Manager Add-in')

def command_created(args):
     """
"""Handles the button click event to load a model. """
"""
    try:
        # Step 1: Get the currently selected model ID from Asset Manager
        model_id = get_selected_model_id()
        if not model_id:
            ui.messageBox('No model selected in Asset Manager.')
            return

        # Step 2: Download the model from the Asset Manager
        model_path = fetch_model_from_manager(model_id)
        if not model_path:
            ui.messageBox('Failed to download the model.')
            return

        # Step 3: Import the model into Fusion 360
        import_model(model_path)

    except Exception as e:
        ui.messageBox(f'Error: {str(e)}')

def get_selected_model_id():
     """
"""Retrieve the currently selected model ID from Asset Manager. """
"""
    try:
        response = requests.get(f"{ASSET_MANAGER_API}/selected_model")
        data = response.json()
        return data.get('model_id')
    except:
        return None

def fetch_model_from_manager(model_id):
     """
"""Download the model file from the Asset Manager API. """
"""
    try:
        response = requests.get(f"{ASSET_MANAGER_API}/download/{model_id}", stream=True)
        if response.status_code == 200:
            file_path = os.path.join(os.path.expanduser("~"), "Downloads", f"{model_id}.f3d")
            with open(file_path, "wb") as file:
                for chunk in response.iter_content(chunk_size=8192):
                    file.write(chunk)
            return file_path
    except:
        return None

def import_model(file_path):
     """
"""Imports the downloaded model into Fusion 360. """
"""
    doc = app.documents.add(adsk.core.DocumentTypes.FusionDesignDocumentType)
    design = adsk.fusion.Design.cast(doc.products.itemByProductType("DesignProductType"))
    design.importManager.importToTarget(file_path, design.rootComponent)

 """
import sys
import os
import locale
import adsk.core, adsk.fusion, adsk.cam, traceback
import requests

# Force UTF-8 Encoding for Output
sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

print(f"✅ Encoding set to {locale.getpreferredencoding()}")

# Add Fusion 360's Python library path if not already present
fusion_api_path = r"C:\Program Files\Autodesk\Autodesk Fusion 360\Python\Lib\site-packages"
if fusion_api_path not in sys.path:
    sys.path.append(fusion_api_path)

try:
    import adsk.core, adsk.fusion, adsk.cam
    print("✅ Successfully imported adsk!")
except ModuleNotFoundError:
    print("❌ Error: 'adsk' module still not found.")
    sys.exit(1)  # Exit if the API is not available

# Fusion 360 Application Context
app = adsk.core.Application.get()
ui = app.userInterface
def run(context=None):
    """Executed when the add-in is started."""
    try:
        # Check if command already exists
        cmd_def = ui.commandDefinitions.itemById('LoadAssetCmd')
        if not cmd_def:
            # Create command definition
            cmd_def = ui.commandDefinitions.addButtonDefinition(
                'LoadAssetCmd', 'Load Asset', 'Load a model from Asset Manager into Fusion 360'
            )
            cmd_def.commandCreated.add(command_created)
            handlers.append(cmd_def)  # Store reference

        # Add button to UI only if it's not already added
        workspace = ui.workspaces.itemById('FusionSolidEnvironment')
        toolbar_panel = workspace.toolbarPanels.itemById('SolidScriptsAddinsPanel')

        if not toolbar_panel.controls.itemById('LoadAssetCmd'):
            toolbar_panel.controls.addCommand(cmd_def)

        ui.messageBox('✅ Asset Manager Add-in Loaded Successfully!', 'Asset Manager', adsk.core.MessageBoxButtonTypes.OKButtonType, adsk.core.MessageBoxIconTypes.InformationIconType)
    
    except Exception as e:
        ui.messageBox(f'❌ Error in Add-in Initialization: {str(e)}', 'Error', adsk.core.MessageBoxButtonTypes.OKButtonType, adsk.core.MessageBoxIconTypes.WarningIconType)
