import adsk.core, adsk.fusion, adsk.cam, traceback
import os
import sys
import json
import logging
import threading
import time
import urllib.request
import urllib.error
import urllib.parse
import ssl
import base64

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

# Get auth token if available
def get_auth_token():
    try:
        token_path = os.path.join(get_current_dir(), 'auth_token.txt')
        if os.path.exists(token_path):
            with open(token_path, 'r') as f:
                return f.read().strip()
        return None
    except:
        logging.error("Failed to read auth token")
        return None

# Helper function for making HTTP requests
def make_http_request(url, method="GET", headers=None, data=None, params=None):
    try:
        # Build the URL with parameters if provided
        if params:
            param_string = urllib.parse.urlencode(params)
            url = f"{url}?{param_string}"
        
        # Create the request object
        req = urllib.request.Request(url, method=method)
        
        # Add headers
        if headers:
            for key, value in headers.items():
                req.add_header(key, value)
        
        # Add data for POST/PUT/PATCH requests
        if data and method in ["POST", "PUT", "PATCH"]:
            if isinstance(data, dict):
                # Convert dict to JSON string and encode to bytes
                data = json.dumps(data).encode('utf-8')
                if 'Content-Type' not in headers:
                    req.add_header('Content-Type', 'application/json')
            elif isinstance(data, str):
                # Convert string to bytes
                data = data.encode('utf-8')
            
            # If data is already bytes, use it directly
            if not isinstance(data, bytes):
                data = str(data).encode('utf-8')
        
        # Make the request
        context = ssl._create_unverified_context()
        with urllib.request.urlopen(req, data=data, context=context) as response:
            status_code = response.getcode()
            response_data = response.read().decode('utf-8')
            
            if response_data:
                try:
                    response_json = json.loads(response_data)
                    return status_code, response_json
                except:
                    return status_code, response_data
            else:
                return status_code, None
    
    except urllib.error.HTTPError as e:
        error_message = e.read().decode('utf-8')
        try:
            error_json = json.loads(error_message)
            return e.code, error_json
        except:
            return e.code, error_message
    except Exception as e:
        logging.error(f"HTTP request error: {str(e)}")
        return 0, str(e)

# Helper function to upload to Autodesk hub
def upload_to_hub(file_path, project_id, item_id, folder_id=""):
    try:
        # Get the auth token
        token = get_auth_token()
        if not token:
            return False, "Authentication token not found"
        
        # Read the file as binary
        with open(file_path, 'rb') as f:
            file_data = f.read()
        
        # Base file name for display
        file_name = os.path.basename(file_path)
        
        # Headers for the request
        headers = {
            'Authorization': f'Bearer {token}',
            'Content-Type': 'application/json'
        }
        
        # Create version payload
        version_data = {
            "jsonapi": {
                "version": "1.0"
            },
            "data": {
                "type": "versions",
                "attributes": {
                    "name": file_name,
                    "extension": {
                        "type": "versions:autodesk.core:File",
                        "version": "1.0"
                    }
                },
                "relationships": {
                    "item": {
                        "data": {
                            "type": "items",
                            "id": item_id
                        }
                    }
                }
            }
        }
        
        # API endpoint for creating a version
        create_version_url = f"https://developer.api.autodesk.com/data/v1/projects/{project_id}/items/{item_id}/versions"
        
        # Parameters to include folder if specified
        params = {}
        if folder_id:
            params['folderId'] = folder_id
        
        # Create a version
        status, response = make_http_request(
            create_version_url, 
            method="POST", 
            headers=headers, 
            data=version_data, 
            params=params
        )
        
        if status != 201:
            return False, f"Failed to create version: {response}"
        
        version_id = response['data']['id']
        
        # Now initiate the storage for file upload
        upload_url = f"https://developer.api.autodesk.com/data/v1/projects/{project_id}/versions/{version_id}/relationships/storage"
        
        # Storage payload
        storage_data = {
            "jsonapi": {
                "version": "1.0"
            },
            "data": {
                "type": "objects",
                "attributes": {
                    "name": file_name
                }
            }
        }
        
        # Initiate storage
        status, response = make_http_request(
            upload_url, 
            method="POST", 
            headers=headers, 
            data=storage_data
        )
        
        if status != 201:
            return False, f"Failed to initiate storage: {response}"
        
        # Get the upload URL
        storage_url = response['data']['relationships']['target']['meta']['link']['href']
        
        # Upload the actual file
        upload_headers = {'Content-Type': 'application/octet-stream'}
        
        # Create a custom request for binary upload
        req = urllib.request.Request(storage_url, method="PUT", data=file_data, headers=upload_headers)
        
        try:
            context = ssl._create_unverified_context()
            with urllib.request.urlopen(req, context=context) as response:
                status = response.getcode()
                if status != 200:
                    response_text = response.read().decode('utf-8')
                    return False, f"Failed to upload file: {response_text}"
        except urllib.error.HTTPError as e:
            return False, f"Failed to upload file: {e.read().decode('utf-8')}"
        
        # Complete the version by patching it
        complete_url = f"https://developer.api.autodesk.com/data/v1/projects/{project_id}/versions/{version_id}"
        
        # No data needed for the PATCH request
        status, response = make_http_request(
            complete_url, 
            method="PATCH", 
            headers=headers
        )
        
        if status != 200:
            return False, f"Failed to complete version: {response}"
        
        return True, "Upload successful"
    except Exception as e:
        error_message = traceback.format_exc()
        logging.error(f"Failed in upload_to_hub: {error_message}")
        return False, f"Exception during upload: {str(e)}"

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

# Create the floating palette
def showPalette():
    try:
        global palette, ui
        
        # First, check if the palette already exists
        existingPalette = ui.palettes.itemById(paletteName)
        if existingPalette:
            # If it exists, just show it
            existingPalette.isVisible = True
            logging.info("Showing existing palette")
            return True
            
        # Create a new palette with a properly formatted HTML URL
        logging.info("Creating new palette")
        
        # Define HTML content
       # Define HTML content
        html_content = '''
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <title>Save To Hub</title>
            <style>
                body {
                    margin: 10px;
                    font-family: Arial, sans-serif;
                    background-color: #f5f5f5;
                }
                .container {
                    text-align: center;
                    padding: 10px;
                }
                h3 {
                    color: #0078D7;
                    margin-top: 0;
                }
                button {
                    width: 90%;
                    height: 40px;
                    background-color: #0078D7;
                    color: white;
                    border: none;
                    border-radius: 5px;
                    font-size: 16px;
                    cursor: pointer;
                    font-weight: bold;
                    margin: 10px 0;
                    padding: 0 15px;
                }
                button:hover {
                    background-color: #005a9e;
                }
            </style>
        </head>
        <body>
            <div class="container">
                <h3>Save To Hub</h3>
                <button id="saveButton">Save To Hub</button>
            </div>
            <script>
                document.addEventListener('DOMContentLoaded', function() {
                    var saveButton = document.getElementById('saveButton');
                    if (saveButton) {
                        saveButton.addEventListener('click', function() {
                            window.location.href = "fusion://command/''' + commandId + '''";
                            console.log("Save button clicked");
                        });
                        console.log("Button event listener added");
                    } else {
                        console.log("Save button not found");
                    }
                });
            </script>
        </body>
        </html>
        '''
        
        # Create a temporary HTML file
        html_dir = get_current_dir()
        html_file_path = os.path.join(html_dir, 'savetohub_palette.html')
        
        with open(html_file_path, 'w') as f:
            f.write(html_content)
        
# Add this debugging
        logging.info(f"HTML file created at: {html_file_path}")
        try:
            # Verify the file exists and read its size
            file_size = os.path.getsize(html_file_path)
            logging.info(f"HTML file size: {file_size} bytes")
    
            # Read the first 100 characters to verify content
            with open(html_file_path, 'r') as f:
                content_preview = f.read(100)
            logging.info(f"HTML content preview: {content_preview}")
        except Exception as e:
            logging.error(f"Error verifying HTML file: {str(e)}")
                # Use file:// protocol for the HTML file URL
        html_file_url = 'file:///' + html_file_path.replace('\\', '/')
        logging.info(f"HTML file URL: {html_file_url}")
        
        # Create palette with the file URL
    # Create palette with the file URL
        # Create palette with the file URL
        palette = ui.palettes.add(
            id=paletteName,
            name='Save To Hub',
            htmlFileURL=html_file_url,
            isVisible=True,
            showCloseButton=True,
            isResizable=True,
            width=300,  # Increased width
            height=150,  # Increased height
            useNewWebBrowser=True
        )

        # Try to set the docking state with error handling
        try:
            # Allow palette to initialize first
            time.sleep(0.1)
    
            # Set to floating state
            palette.dockingState = adsk.core.PaletteDockingStates.PaletteDockStateFloating
    
            # Position it in a visible area - with error handling
            try:
                palette.setPosition(900, 200)
            except:
                logging.warning("Could not set palette position")
        except:
            logging.warning("Could not set palette docking state - using default")
        # Add event handler
        onHTMLEvent = PaletteEventHandler()
        palette.incomingFromHTML.add(onHTMLEvent)
        handlers.append(onHTMLEvent)
        
        logging.info("Palette created successfully")
        return True
    except:
        error_message = traceback.format_exc()
        logging.error(f"Failed to create palette: {error_message}")
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

# Run when Fusion 360 starts
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
        
        # Add the command to the quick access toolbar (QAT)
        qatPanel = ui.allToolbarPanels.itemById('QAT')
        if qatPanel:
            qatControl = qatPanel.controls.addCommand(cmdDef)
            logging.info("Command added to QAT")
        
        # Add the command to the UI panel - try both panel IDs
        utilsPanel = ui.allToolbarPanels.itemById('UtilityPanel')
        if utilsPanel:
            utilsControl = utilsPanel.controls.addCommand(cmdDef)
            logging.info("Command added to Utility panel")
        else:
            utilsPanel = ui.allToolbarPanels.itemById('UtilitiesPanel')
            if utilsPanel:
                utilsControl = utilsPanel.controls.addCommand(cmdDef)
                logging.info("Command added to Utilities panel")
            else:
                # Log all available panels
                logging.info("Available panels:")
                for panel in ui.allToolbarPanels:
                    logging.info(f"Panel ID: {panel.id}, Name: {panel.name}")
                    if "UTILITY" in panel.name.upper():
                        utilsPanel = panel
                        utilsPanel.controls.addCommand(cmdDef, '', False)
                        logging.info(f"Command added to found panel: {panel.id}")
                        break
        
        # Create a command to show the palette
        createShowPaletteCommand()
        
        # Register for workspace activated events
        onWorkspaceActivated = WorkspaceActivatedHandler()
        ui.workspaceActivated.add(onWorkspaceActivated)
        handlers.append(onWorkspaceActivated)
        
        # Show the palette when the add-in starts
        success = showPalette()
        if not success:
            ui.messageBox("Could not show Save To Hub palette. Use the 'Show Save To Hub Palette' command in the Utilities panel to show it.",
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.InformationIconType)
        
        logging.info("Add-in initialization completed")
        
    except Exception as e:
        error_message = traceback.format_exc()
        logging.error(f"Failed to initialize SaveToHub add-in:\n{error_message}")
        if ui:
            ui.messageBox(f'Failed to initialize SaveToHub add-in:\n{error_message}',
                         'SaveToHub Error',
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.CriticalIconType)

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
        logging.error(f"Failed to clean MOup SaveToHub add-in:\n{error_message}")
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