import adsk.core, adsk.fusion, adsk.cam, traceback
import os
import sys
import json
import logging
import threading
import time

# Set up logging
# Set up logging
log_dir = os.path.dirname(os.path.realpath(__file__))
log_path = os.path.join(log_dir, 'savetohub_log.txt')

# Force logging to create the file immediately
try:
    with open(log_path, 'w') as f:
        f.write("Log started\n")
except Exception as e:
    print(f"Logging error: {e}")

logging.basicConfig(filename=log_path, level=logging.INFO, 
                   format='%(asctime)s - %(levelname)s - %(message)s')

logging.info("SaveToHub add-in started.")

# Global variables
app = None
ui = None
handlers = []
commandId = 'SaveToHubCommand'
commandTitle = 'Save To Hub'
palette = None
paletteName = 'SaveToHubPalette'

# Get current directory
def get_current_dir():
    return os.path.dirname(os.path.realpath(__file__))

# Helper function to save model to Autodesk Hub
def saveToHub():
    try:
        ui = app.userInterface
        doc = app.activeDocument
        
        if not doc:
            ui.messageBox("No active document to save.", 
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.InformationIconType)
            return False
        
        # Get model file path
        model_path = doc.dataFile.filePath if doc.dataFile else None
        
        if not model_path:
            ui.messageBox("Document has not been saved yet. Please save it first.",
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.InformationIconType)
            return False
            
        model_name = os.path.splitext(os.path.basename(model_path))[0]
    
        # Find metadata file
        metadata_path = os.path.join(os.path.dirname(model_path), f"{model_name}.metadata.json")
        
        if not os.path.exists(metadata_path):
            ui.messageBox(f"Metadata file missing for {model_name}. Cannot save to Autodesk Hub.",
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.WarningIconType)
            return False
        
        with open(metadata_path, 'r') as file:
            metadata = json.load(file)
    
        project_id = metadata["projectId"]
        item_id = metadata["itemId"]
        folder_id = metadata.get("folderId", "")
    
        ui.messageBox(f"This is where we would upload {model_name} to project: {project_id}, item: {item_id}",
                     "SaveToHub", 
                     adsk.core.MessageBoxButtonTypes.OKButtonType,
                     adsk.core.MessageBoxIconTypes.InformationIconType)
        ui.messageBox("This button is working! You need to implement the upload functionality with your API integration.",
                     "SaveToHub", 
                     adsk.core.MessageBoxButtonTypes.OKButtonType,
                     adsk.core.MessageBoxIconTypes.InformationIconType)
        return True
    except:
        error_message = traceback.format_exc()
        logging.error(f"Failed in saveToHub: {error_message}")
        if ui:
            ui.messageBox(f'Failed in saveToHub: {error_message}',
                         'SaveToHub Error',
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.CriticalIconType)
        return False
def showSaveDialog():
    try:
        global ui
        
        # Ensure the UI is available
        if not ui:
            app = adsk.core.Application.get()
            ui = app.userInterface

        # Check if the command already exists
        dialogCmdDef = ui.commandDefinitions.itemById('SaveToHubDialogCommand')
        if dialogCmdDef:
            dialogCmdDef.deleteMe()
        
        # Create new command definition
        dialogCmdDef = ui.commandDefinitions.addButtonDefinition(
            'SaveToHubDialogCommand',
            'Save To Hub',
            'Save your design to Autodesk Hub'
        )

        # Connect to the command created event
        onDialogCreated = SaveDialogCreatedHandler()
        dialogCmdDef.commandCreated.add(onDialogCreated)
        handlers.append(onDialogCreated)

        # 🚀 Instead of directly executing, use UI's command execution
        ui.commandDefinitions.itemById('SaveToHubDialogCommand').execute()

        logging.info("✅ Save dialog command executed")
        return True
    except:
        logging.error(traceback.format_exc())
        return False

class SaveDialogCreatedHandler(adsk.core.CommandCreatedEventHandler):
    def __init__(self):
        super().__init__()

    def notify(self, args):
        try:
            cmd = args.command
            inputs = cmd.commandInputs

            # ✅ Make sure we register handlers before user input happens
            onExecute = SaveDialogExecuteHandler()
            cmd.execute.add(onExecute)
            handlers.append(onExecute)

            onCancel = SaveDialogCancelHandler()
            cmd.destroy.add(onCancel)
            handlers.append(onCancel)

            # ✅ Add UI elements
            inputs.addTextBoxCommandInput('infoText', '', 'Upload your design to Autodesk Hub', 3, True)
            inputs.addSeparatorCommandInput('separator', 'Options')
            inputs.addBoolValueInput('notifyCheckbox', 'Send notification when complete', False)

            logging.info("✅ Save dialog UI created successfully")
        except:
            logging.error(traceback.format_exc())

class SaveDialogExecuteHandler(adsk.core.CommandEventHandler):
    def __init__(self):
        super().__init__()
    
    def notify(self, args):
        try:
            eventArgs = adsk.core.CommandEventArgs.cast(args)
            inputs = eventArgs.command.commandInputs
            
            # Get any input values if needed
            notifyUser = inputs.itemById('notifyCheckbox').value
            
            # Perform the save operation
            saveToHub()
            
        except:
            logging.error(traceback.format_exc())

class SaveDialogCancelHandler(adsk.core.CommandEventHandler):
    def __init__(self):
        super().__init__()
    
    def notify(self, args):
        try:
            # Handle cancel if needed
            logging.info("Save operation cancelled")
        except:
            logging.error(traceback.format_exc())

# Handler for workspace activated event - show palette when design workspace activated
class WorkspaceActivatedHandler(adsk.core.WorkspaceEventHandler):
    def __init__(self):
        super().__init__()
    
    def notify(self, args):
        try:
            workspace = adsk.core.WorkspaceEventArgs.cast(args).workspace
            if workspace.id == 'FusionSolidEnvironment':
                # Wait a moment before showing palette to ensure UI is ready
                def delayed_show():
                    time.sleep(1)
                    showSaveDialog()
                
                # Run in a separate thread
                thread = threading.Thread(target=delayed_show)
                thread.daemon = True
                thread.start()
        except:
            error_message = traceback.format_exc()
            logging.error(f"Failed in workspace activated: {error_message}")

# Run when Fusion 360 starts
def run(context):
    try:
        global app, ui
        app = adsk.core.Application.get()
        ui = app.userInterface

        logging.info("SaveToHub add-in starting...")

        # Register workspace handler
        onWorkspaceActivated = WorkspaceActivatedHandler()
        ui.workspaceActivated.add(onWorkspaceActivated)
        handlers.append(onWorkspaceActivated)

        # ✅ 🚀 Show the dialog correctly
        success = showSaveDialog()
        if not success:
            ui.messageBox("Could not show Save To Hub dialog.", "SaveToHub",
                          adsk.core.MessageBoxButtonTypes.OKButtonType,
                          adsk.core.MessageBoxIconTypes.WarningIconType)

        logging.info("✅ Add-in initialization completed")
    except Exception as e:
        logging.error(f"❌ Failed to initialize SaveToHub: {traceback.format_exc()}")

# Stop function called when Fusion 360 stops
def stop(context):
    # Declare global variables at the beginning of the function
    global palette, handlers
    
    try:
        logging.info("Stopping SaveToHub add-in")
        
        # First, try to get a fresh reference to the application and UI
        try:
            app = adsk.core.Application.get()
            if app:
                ui = app.userInterface
            else:
                ui = None
        except:
            ui = None
            logging.warning("Could not get application reference during shutdown")
        
        # Remove the palette if it exists
        try:
            if palette is not None:
                palette.deleteMe()
                palette = None
                logging.info("Palette removed")
            else:
                logging.info("No palette to remove")
        except:
            logging.warning("Error removing palette")
        
        # Remove the command definitions if UI is still available
        if ui:
            try:
                cmdDef = ui.commandDefinitions.itemById(commandId)
                if cmdDef:
                    cmdDef.deleteMe()
                    logging.info("Command definition removed")
            except:
                logging.warning("Error removing command definition")
                
            try:
                showPaletteCmdDef = ui.commandDefinitions.itemById('ShowSaveToHubPaletteCommand')
                if showPaletteCmdDef:
                    showPaletteCmdDef.deleteMe()
                    logging.info("Show palette command removed")
            except:
                logging.warning("Error removing show palette command")
        
        # Clean up the handlers
        try:
            if handlers:
                handlers.clear()
                logging.info("Handlers cleared")
        except:
            logging.warning("Error clearing handlers")
        
        logging.info("Add-in cleanup completed")
        
    except Exception as e:
        error_message = traceback.format_exc()
        logging.error(f"Failed to clean up SaveToHub add-in:\n{error_message}")
        try:
            # Try to get UI again if needed for message box
            app = adsk.core.Application.get()
            ui = app.userInterface
            if ui:
                ui.messageBox(f'Failed to clean up SaveToHub add-in:\n{error_message}',
                             'SaveToHub Error',
                             adsk.core.MessageBoxButtonTypes.OKButtonType,
                             adsk.core.MessageBoxIconTypes.CriticalIconType)
        except:
            # If everything fails, just log the error
            logging.error("Could not show error message box")
# Helper function to save model to Autodesk Hub
def saveToHub():
    try:
        ui = app.userInterface
        doc = app.activeDocument
        
        if not doc:
            ui.messageBox("No active document to save.", 
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.InformationIconType)
            return False
        
        # Get model file path
        model_path = doc.dataFile.filePath if doc.dataFile else None
        
        if not model_path:
            ui.messageBox("Document has not been saved yet. Please save it first.",
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.InformationIconType)
            return False
            
        model_name = os.path.splitext(os.path.basename(model_path))[0]
    
        # Find metadata file
        metadata_path = os.path.join(os.path.dirname(model_path), f"{model_name}.metadata.json")
        
        if not os.path.exists(metadata_path):
            ui.messageBox(f"Metadata file missing for {model_name}. Cannot save to Autodesk Hub.",
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.WarningIconType)
            return False
        
        with open(metadata_path, 'r') as file:
            metadata = json.load(file)
    
        project_id = metadata["projectId"]
        item_id = metadata["itemId"]
        folder_id = metadata.get("folderId", "")
        
        # Show a progress dialog
        progress = ui.createProgressDialog()
        progress.cancelButtonText = "Cancel"
        progress.isBackgroundTranslucent = False
        progress.isCancelButtonShown = True
        progress.show("Save To Hub", "Uploading to Autodesk Hub...", 0, 100, 0)
        
        # Perform the upload operation
        success, message = upload_to_hub(model_path, project_id, item_id, folder_id)
        
        # Close the progress dialog
        progress.hide()
        
        if success:
            ui.messageBox(f"Successfully uploaded {model_name} to Autodesk Hub.",
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.InformationIconType)
            return True
        else:
            ui.messageBox(f"Failed to upload {model_name} to Autodesk Hub:\n{message}",
                         "SaveToHub Error", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.CriticalIconType)
            return False
    except:
        error_message = traceback.format_exc()
        logging.error(f"Failed in saveToHub: {error_message}")
        if ui:
            ui.messageBox(f'Failed in saveToHub: {error_message}',
                         'SaveToHub Error',
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.CriticalIconType)
        return False
