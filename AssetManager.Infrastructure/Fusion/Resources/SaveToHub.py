import adsk.core, adsk.fusion, adsk.cam, traceback
import os
import sys
import json
import logging
import threading
import time

# Set up logging
log_dir = os.path.dirname(os.path.realpath(__file__))
log_path = os.path.join(log_dir, 'savetohub_log.txt')
logging.basicConfig(filename=log_path, level=logging.INFO, 
                   format='%(asctime)s - %(levelname)s - %(message)s')

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

# Create the floating palette
def showPalette():
    try:
        global palette, ui

        # First, check if the palette already exists
        existingPalette = ui.palettes.itemById(paletteName)
        if existingPalette:
            existingPalette.isVisible = True  # Make sure it's visible
            existingPalette.dockingState = adsk.core.PaletteDockingStates.PaletteDockStateFloating  # Force floating
            logging.info("Showing existing floating palette")
            return True

        # Get script directory
        script_dir = get_current_dir()

        # 🔹 Define the correct HTML content
        html_content = '''
        <html>
        <head>
            <style>
                body {
                    margin: 10px;
                    font-family: Arial;
                    background-color: #f0f0f0;
                }
                h3 {
                    text-align: center;
                    margin-top: 0;
                }
                button {
                    width: 100%;
                    height: 40px;
                    background-color: #0078D7;
                    color: white;
                    border: none;
                    border-radius: 5px;
                    font-size: 16px;
                    cursor: pointer;
                    font-weight: bold;
                }
            </style>
        </head>
        <body>
            <h3>Save To Hub</h3>
            <button id="saveButton">Save To Hub</button>
            <script>
                document.getElementById("saveButton").addEventListener("click", function() {
                    window.location.href = "fusion://command/''' + commandId + '''";
                });
            </script>
        </body>
        </html>
        '''

        # 🔹 Create a temporary HTML file if it doesn't exist
        html_file_path = os.path.join(script_dir, 'savetohub_palette.html')

        with open(html_file_path, 'w', encoding='utf-8') as f:
            f.write(html_content)

        # 🔹 Convert file path to a URL format
        html_file_url = 'file:///' + html_file_path.replace('\\', '/')

        # 🔹 Create the floating palette
        palette = ui.palettes.add(
            id=paletteName,
            name='Save To Hub',
            htmlFileURL=html_file_url,  # ✅ Dynamically generated HTML file
            isVisible=True,
            showCloseButton=True,
            isResizable=True,
            width=300,
            height=200,
            useNewWebBrowser=True
        )

        # Force floating mode
        palette.dockingState = adsk.core.PaletteDockingStates.PaletteDockStateFloating

        # Set position manually
        try:
            palette.setPosition(800, 200)
        except:
            logging.warning("⚠️ Could not set palette position")

        logging.info("✅ Floating palette with dynamically generated HTML created successfully")
        return True
    except:
        error_message = traceback.format_exc()
        logging.error(f"❌ Failed to create floating palette: {error_message}")
        return False

# Handler for palette HTML events
class PaletteEventHandler(adsk.core.HTMLEventHandler):
    def __init__(self):
        super().__init__()
    
    def notify(self, args):
        try:
            htmlArgs = adsk.core.HTMLEventArgs.cast(args)
            logging.info(f"Received HTML event: {htmlArgs.action}")
            
            # If we get any event from the HTML side, we can handle it here
            if htmlArgs.action == "save":
                saveToHub()
        except:
            error_message = traceback.format_exc()
            logging.error(f"Failed in palette event: {error_message}")

# Command created event handler
class CommandCreatedEventHandler(adsk.core.CommandCreatedEventHandler):
    def __init__(self):
        super().__init__()
    
    def notify(self, args):
        try:
            cmd = args.command
            logging.info("Command created event triggered")
            
            # Connect to the execute event
            onExecute = CommandExecuteHandler()
            cmd.execute.add(onExecute)
            handlers.append(onExecute)
        except:
            error_message = traceback.format_exc()
            logging.error(f"Failed in command created event: {error_message}")

# Command execute event handler
class CommandExecuteHandler(adsk.core.CommandEventHandler):
    def __init__(self):
        super().__init__()
    
    def notify(self, args):
        try:
            # Execute the save to hub operation
            logging.info("Command execute event triggered")
            saveToHub()
        except:
            error_message = traceback.format_exc()
            logging.error(f"Failed in command execute event: {error_message}")

# Create a command to show the palette
def createShowPaletteCommand():
    try:
        # Check if command already exists
        showPaletteCmdDef = ui.commandDefinitions.itemById('ShowSaveToHubPaletteCommand')
        if showPaletteCmdDef:
            showPaletteCmdDef.deleteMe()
            
        # Create command definition
        showPaletteCmdDef = ui.commandDefinitions.addButtonDefinition(
            'ShowSaveToHubPaletteCommand',
            'Show Save To Hub Palette',
            'Shows the Save To Hub floating palette',
            ''
        )
        
        # Connect to command created event
        onShowPaletteCreated = ShowPaletteCommandCreatedHandler()
        showPaletteCmdDef.commandCreated.add(onShowPaletteCreated)
        handlers.append(onShowPaletteCreated)
        
        # Add to toolbar
        utilsPanel = ui.allToolbarPanels.itemById('UtilityPanel')  # Try the correct panel name first
        if not utilsPanel:
            utilsPanel = ui.allToolbarPanels.itemById('UtilitiesPanel')  # Fallback to original name
        
        if utilsPanel:
            utilsPanel.controls.addCommand(showPaletteCmdDef, '', False)
            logging.info(f"Command added to panel: {utilsPanel.id}")
        else:
            # Log all available panels for debugging
            panel_ids = [panel.id for panel in ui.allToolbarPanels]
            logging.info(f"Available panels: {panel_ids}")
            logging.warning("Could not find Utility panel")
            
        return showPaletteCmdDef
    except:
        error_message = traceback.format_exc()
        logging.error(f"Failed to create show palette command: {error_message}")
        return None

# Handler for show palette command
class ShowPaletteCommandCreatedHandler(adsk.core.CommandCreatedEventHandler):
    def __init__(self):
        super().__init__()
    
    def notify(self, args):
        try:
            cmd = args.command
            
            # Connect to execute event
            onExecute = ShowPaletteCommandExecuteHandler()
            cmd.execute.add(onExecute)
            handlers.append(onExecute)
        except:
            error_message = traceback.format_exc()
            logging.error(f"Failed in show palette command created: {error_message}")

# Handler for show palette command execute
class ShowPaletteCommandExecuteHandler(adsk.core.CommandEventHandler):
    def __init__(self):
        super().__init__()
    
    def notify(self, args):
        try:
            # Show the palette
            showPalette()
        except:
            error_message = traceback.format_exc()
            logging.error(f"Failed to show palette from command: {error_message}")

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
                    showPalette()
                
                # Run in a separate thread
                thread = threading.Thread(target=delayed_show)
                thread.daemon = True
                thread.start()
        except:
            error_message = traceback.format_exc()
            logging.error(f"Failed in workspace activated: {error_message}")

def run(context):
    try:
        global app, ui
        app = adsk.core.Application.get()
        ui = app.userInterface

        logging.info("SaveToHub add-in starting...")

        # Create a command definition for the SaveToHub command
        cmdDef = ui.commandDefinitions.addButtonDefinition(
            commandId, 
            commandTitle, 
            'Save your design to Autodesk Hub',
            ''  # Use empty string for default icon
        )

        # Connect to the command created event
        onCommandCreated = CommandCreatedEventHandler()
        cmdDef.commandCreated.add(onCommandCreated)
        handlers.append(onCommandCreated)

        # 🔹 Add button to the UTILITIES PANEL
        utilitiesPanel = ui.allToolbarPanels.itemById('UtilitiesPanel')

        if utilitiesPanel:
            utilitiesPanel.controls.addCommand(cmdDef, '', False)
            logging.info("✅ Save To Hub button added to the Utilities panel!")
        else:
            logging.warning("⚠️ Could not find the Utilities panel. Trying alternative panels...")

            # Try alternative panel IDs
            for panel in ui.allToolbarPanels:
                if "UTILITY" in panel.id.upper():
                    panel.controls.addCommand(cmdDef, '', False)
                    logging.info(f"✅ Save To Hub added to {panel.id}")
                    break

        # Show the palette when the add-in starts
        success = showPalette()
        if not success:
            ui.messageBox("Could not show Save To Hub palette. Click the Utilities panel button to show it.",
                          "SaveToHub",
                          adsk.core.MessageBoxButtonTypes.OKButtonType,
                          adsk.core.MessageBoxIconTypes.InformationIconType)

    except Exception as e:
        error_message = traceback.format_exc()
        logging.error(f"❌ Failed to initialize SaveToHub add-in:\n{error_message}")
        if ui:
            ui.messageBox(f'Failed to initialize SaveToHub add-in:\n{error_message}',
                          'SaveToHub Error',
                          adsk.core.MessageBoxButtonTypes.OKButtonType,
                          adsk.core.MessageBoxIconTypes.CriticalIconType)

# Stop function called when Fusion 360 stops
def stop(context):
    global palette, handlers

    try:
        logging.info("Stopping SaveToHub add-in")

        app = adsk.core.Application.get()
        ui = app.userInterface

        # Remove the palette if it exists
        if palette:
            palette.deleteMe()
            palette = None
            logging.info("Palette removed")

        # Remove the command definitions
        cmdDef = ui.commandDefinitions.itemById(commandId)
        if cmdDef:
            cmdDef.deleteMe()
            logging.info("Command definition removed")

        # 🔹 REMOVE FROM UTILITIES PANEL
        utilitiesPanel = ui.allToolbarPanels.itemById('UtilitiesPanel')

        if utilitiesPanel:
            control = utilitiesPanel.controls.itemById(commandId)
            if control:
                control.deleteMe()
                logging.info("✅ Save To Hub button removed from the Utilities panel")

        logging.info("✅ Add-in cleanup completed!")

    except Exception as e:
        error_message = traceback.format_exc()
        logging.error(f"❌ Failed to clean up SaveToHub add-in:\n{error_message}")
        try:
            ui.messageBox(f'Failed to clean up SaveToHub add-in:\n{error_message}',
                          'SaveToHub Error',
                          adsk.core.MessageBoxButtonTypes.OKButtonType,
                          adsk.core.MessageBoxIconTypes.CriticalIconType)
        except:
            logging.error("⚠️ Could not show error message box")
