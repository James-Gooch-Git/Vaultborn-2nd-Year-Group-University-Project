import adsk.core, adsk.fusion, adsk.cam, traceback
import os
import sys
import json
import logging
import threading
import time


# Add the packages directory to the Python path
packages_dir = os.path.join(os.path.dirname(os.path.realpath(__file__)), "packages")
if packages_dir not in sys.path:
    sys.path.insert(0, packages_dir)

# Set up logging
# Replace the logging setup code at the beginning of the file

# Set up logging
try:
    log_dir = os.path.join(os.path.expanduser("~"), "Documents", "Fusion360Logs")
    os.makedirs(log_dir, exist_ok=True)  # Ensure directory exists
    log_path = os.path.join(log_dir, 'savetohub_log.txt')
    
    # Try to write a test line to see if the directory is writable
    with open(log_path, 'a') as test_file:
        test_file.write(f"Log initialized at {time.strftime('%Y-%m-%d %H:%M:%S')}\n")
    
    # Configure logging with more detailed format
    logging.basicConfig(
    filename=log_path, 
    level=logging.DEBUG,  # Lower level to capture more logs
    format='%(asctime)s - %(levelname)s - %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S',
    force=True  # Ensure this logging setup is used
    )

# Manually flush logs after writing to force immediate output
    def log_flush(message, level=logging.INFO):
        logging.log(level, message)
        for handler in logging.getLogger().handlers:
            handler.flush()

    
    # Add a handler to also log to console during development
    console = logging.StreamHandler()
    console.setLevel(logging.INFO)
    formatter = logging.Formatter('%(asctime)s - %(levelname)s - %(message)s')
    console.setFormatter(formatter)
    logging.getLogger('').addHandler(console)
    
    logging.info("=" * 80)
    logging.info("SaveToHub Add-in Logging Initialized")
    logging.info("=" * 80)
except Exception as e:
    # If logging setup fails, try to write to a more accessible location
    import sys
    import traceback
    
    # Try Desktop folder as fallback
    fallback_path = os.path.join(os.path.expanduser("~"), "Desktop", "savetohub_error.txt")
    with open(fallback_path, 'a') as f:
        f.write(f"Error setting up logging at {time.strftime('%Y-%m-%d %H:%M:%S')}: {str(e)}\n")
        f.write(traceback.format_exc())
        f.write("\n\nTrying to write to Documents folder as well...\n")
    
    # Also try Documents folder
    try:
        docs_path = os.path.join(os.path.expanduser("~"), "Documents", "savetohub_error.txt")
        with open(docs_path, 'a') as f:
            f.write(f"Error setting up logging at {time.strftime('%Y-%m-%d %H:%M:%S')}: {str(e)}\n")
            f.write(traceback.format_exc())
    except:
        pass  # If this fails too, we've already written to Desktop
# Global variables
app = None
ui = None
handlers = []
commandId = 'SaveToHubCommand'
commandTitle = 'Save To Hub'
#palette = None
#paletteName = 'SaveToHubPalette'

# Get current directory
def get_current_dir():
    return os.path.dirname(os.path.realpath(__file__))

# Helper function to save model to Autodesk Hub
# Helper function to save model to Autodesk Hub
# Helper function to save model to Autodesk Hub
# Helper function to save model to Autodesk Hub
# Helper function to save model to Autodesk Hub
def saveToHub():
    try:
        logs_dir = os.path.join(os.path.expanduser("~"), "Documents", "Fusion360Logs")
        os.makedirs(logs_dir, exist_ok=True)
        log_path = os.path.join(logs_dir, 'savetohub_log.txt')
        
        # Try writing a direct message to confirm we can write to this location
        with open(log_path, 'a') as f:
            timestamp = time.strftime('%Y-%m-%d %H:%M:%S')
            f.write(f"[{timestamp}] SaveToHub function called\n")
            f.flush()
        
        ui = app.userInterface
        doc = app.activeDocument
        log_flush(f"SaveToHub started - Active document: {doc.name if doc else 'None'}", logging.INFO)
        

        
        if not doc:
            ui.messageBox("No active document to export.", 
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.InformationIconType)
            return False
        
        # First try to get file path from the C# application
        external_path_file = os.path.join(get_current_dir(), "current_model_path.txt")
        external_path = None
        
        if os.path.exists(external_path_file):
            try:
                with open(external_path_file, 'r') as f:
                    external_path = f.read().strip()
                logging.info(f"Read external path from file: {external_path}")
            except Exception as e:
                logging.error(f"Error reading external path file: {str(e)}")
        
        # Get the file path of the open document
        original_path = None
        if doc.dataFile:
            original_path = doc.dataFile.filePath
            logging.info(f"Document has dataFile path: {original_path}")
        
        # Decide which path to use - prefer external path if available
        model_path = external_path if external_path else original_path
        
        # If no path is found, document hasn't been saved
        if not model_path:
            # Try to determine the path based on document name
            model_name = doc.name
            downloaded_models_dir = os.path.join(os.path.expanduser("~"), "Documents", "DownloadedModels")
            
            # Look for files matching the document name in the DownloadedModels directory
            potential_files = []
            if os.path.exists(downloaded_models_dir):
                for file in os.listdir(downloaded_models_dir):
                    file_name_no_ext = os.path.splitext(file)[0]
                    if file_name_no_ext == model_name:
                        potential_files.append(os.path.join(downloaded_models_dir, file))
            
            if potential_files:
                model_path = potential_files[0]  # Use the first matching file
                logging.info(f"Found potential file path based on name: {model_path}")
            else:
                ui.messageBox("Document has not been saved and no matching file was found. Please save the document first.",
                             "SaveToHub", 
                             adsk.core.MessageBoxButtonTypes.OKButtonType,
                             adsk.core.MessageBoxIconTypes.InformationIconType)
                return False
            
        model_name = os.path.splitext(os.path.basename(model_path))[0]
        model_extension = os.path.splitext(model_path)[1]
        model_dir = os.path.dirname(model_path)
        
        logging.info(f"Model path: {model_path}")
        logging.info(f"Model name: {model_name}")
        logging.info(f"Model extension: {model_extension}")
        logging.info(f"Model directory: {model_dir}")
        
        # Find metadata file in the same directory
        metadata_path = os.path.join(model_dir, f"{model_name}.metadata.json")
        logging.info(f"Looking for metadata at: {metadata_path}")
        
        if not os.path.exists(metadata_path):
            logging.info("Metadata not found at expected path, searching directory...")
            # Try to find metadata file with a different name pattern
            for file in os.listdir(model_dir):
                if file.startswith(model_name) and file.endswith('.metadata.json'):
                    metadata_path = os.path.join(model_dir, file)
                    logging.info(f"Found metadata file: {metadata_path}")
                    break
        
        if not os.path.exists(metadata_path):
            logging.error("No metadata file found")
            ui.messageBox(f"Metadata file missing for {model_name}. Cannot save to Autodesk Hub.",
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.WarningIconType)
            return False
        
        # Read the metadata file
        try:
            with open(metadata_path, 'r') as file:
                metadata = json.load(file)
            
            project_id = metadata.get("projectId")
            item_id = metadata.get("itemId")
            folder_id = metadata.get("folderId")
            
            if not project_id or not item_id:
                logging.error(f"Missing project_id or item_id in metadata: {metadata}")
                ui.messageBox(f"Project ID or Item ID missing in metadata. Cannot save to Autodesk Hub.",
                             "SaveToHub", 
                             adsk.core.MessageBoxButtonTypes.OKButtonType,
                             adsk.core.MessageBoxIconTypes.WarningIconType)
                return False
                
            logging.info(f"Found metadata - Project ID: {project_id}, Item ID: {item_id}, Folder ID: {folder_id}")
        except Exception as e:
            logging.error(f"Error reading metadata file: {str(e)}")
            ui.messageBox(f"Error reading metadata file: {str(e)}. Cannot save to Autodesk Hub.",
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.CriticalIconType)
            return False
        
        # Read the access token from the file created by C#
        auth_token_path = os.path.join(get_current_dir(), "auth_token.txt")
        logging.info(f"Looking for auth token at: {auth_token_path}")
        if not os.path.exists(auth_token_path):
            logging.error("Auth token file not found")
            ui.messageBox("Authentication token not found. Please log in first.",
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.WarningIconType)
            return False
            
        # Read the token
        with open(auth_token_path, 'r') as f:
            access_token = f.read().strip()
            logging.info("Auth token read successfully")
            
        # Check if token is empty
        if not access_token:
            logging.error("Auth token is empty")
            ui.messageBox("Authentication token is empty. Please log in again.",
                         "SaveToHub", 
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.WarningIconType)
            return False
        
        # Show progress dialog
        progressDialog = ui.createProgressDialog()
        progressDialog.cancelButtonText = "Cancel"
        progressDialog.isBackgroundTranslucent = False
        progressDialog.isCancelButtonShown = True
        progressDialog.show("Save To Hub", "Preparing document...", 0, 100)
        
        try:
            # Create a backup of the original file
            progressDialog.progressValue = 10
            progressDialog.message = "Creating backup..."
            
            backup_file_path = os.path.join(model_dir, f"{model_name}_backup{model_extension}")
            try:
                import shutil
                if os.path.exists(model_path):  # Only backup if file exists
                    shutil.copy2(model_path, backup_file_path)
                    logging.info(f"Created backup of original file: {backup_file_path}")
            except Exception as e:
                logging.warning(f"Failed to create backup: {str(e)}")
            
            # Get design product if available
            design = None
            try:
                design = adsk.fusion.Design.cast(doc.products.itemByProductType('DesignProductType'))
                logging.info("Found design product")
            except Exception as e:
                logging.warning(f"No design product found: {str(e)}")
                design = None
            
            # Combine bodies if design is available and there are multiple bodies
            combined_bodies = False
            original_timeline_object = None
            if design:
                progressDialog.progressValue = 20
                progressDialog.message = "Analyzing document bodies..."
                
                # Check if we have multiple bodies
                rootComp = design.rootComponent
                bodies = rootComp.bRepBodies
                
                # Get visible bodies
                visible_bodies = [body for body in bodies if body.isLightBulbOn]
                
                logging.info(f"Found {len(visible_bodies)} visible bodies in the document")
                
                if len(visible_bodies) > 1:
                    # Ask if user wants to combine bodies
                    progressDialog.hide()  # Hide dialog temporarily
                    
                    result = ui.messageBox(f"Document contains {len(visible_bodies)} separate bodies. Would you like to combine them into a single body before export?",
                                        "SaveToHub", 
                                        adsk.core.MessageBoxButtonTypes.YesNoButtonType,
                                        adsk.core.MessageBoxIconTypes.QuestionIconType)
                    
                    progressDialog.show("Save To Hub", "Processing bodies...", 0, 100)
                    progressDialog.progressValue = 25
                    
                    if result == adsk.core.DialogResults.DialogYes:
                        logging.info(f"Attempting to combine {len(visible_bodies)} bodies")
                        
                        try:
                            # Start a transaction
                            transaction = design.fusionDocument.documentEditor.beginTransaction()
                            
                            # Remember the current end of the timeline
                            timeline = design.timeline
                            original_timeline_object = timeline.item(timeline.count - 1)
                            
                            # Combine bodies
                            # First, get all bodies that are valid for combining
                            validBodies = adsk.core.ObjectCollection.create()
                            for body in visible_bodies:
                                if body.isValid:
                                    validBodies.add(body)
                            
                            if validBodies.count > 1:
                                # Get the combine features
                                combineFeatures = rootComp.features.combineFeatures
                                
                                # Define combine input
                                combineInput = combineFeatures.createInput(validBodies.item(0), validBodies)
                                combineInput.operation = adsk.fusion.FeatureOperations.JoinFeatureOperation
                                
                                # Create the combine feature
                                combineFeature = combineFeatures.add(combineInput)
                                
                                if combineFeature:
                                    logging.info("Bodies combined successfully")
                                    combined_bodies = True
                                else:
                                    logging.warning("Failed to combine bodies")
                            else:
                                logging.info(f"Not enough valid bodies to combine (valid: {validBodies.count})")
                            
                            # Commit the transaction
                            transaction.commit()
                            
                        except Exception as combine_error:
                            logging.error(f"Error combining bodies: {str(combine_error)}")
                            logging.error(traceback.format_exc())
                            try:
                                # Rollback the transaction in case of error
                                transaction.abort()
                            except:
                                pass
                    else:
                        logging.info("User chose not to combine bodies")
            
            # Export the document to the local file
            progressDialog.progressValue = 40
            progressDialog.message = "Exporting document..."
            
            export_successful = False
            
            # For Fusion 360 files, use saveAs with the local option
            if model_extension.lower() in ['.f3d', '.f3z'] and hasattr(doc, 'saveAs'):
                try:
                    # Use saveAs with local option to save locally
                    doc.saveAs(model_name, model_dir, '', '')  # Empty strings for description and options
                    export_successful = True
                    logging.info(f"Saved document locally to: {model_path}")
                except Exception as save_error:
                    logging.error(f"Error saving document: {str(save_error)}")
            # For design files that can be exported
            elif design:
                try:
                    export_manager = design.exportManager
                    
                    if model_extension.lower() in ['.f3d', '.f3z']:
                        # Fusion 360 Archive
                        options = export_manager.createFusionArchiveExportOptions(model_path)
                        export_successful = export_manager.execute(options)
                    elif model_extension.lower() == '.stl':
                        # STL format
                        rootComp = design.rootComponent
                        options = export_manager.createSTLExportOptions(rootComp, model_path)
                        export_successful = export_manager.execute(options)
                    elif model_extension.lower() == '.obj':
                        # Only export if Fusion's createOBJExportOptions is available.
                        if hasattr(export_manager, 'createOBJExportOptions'):
                            rootComp = design.rootComponent
                            options = export_manager.createOBJExportOptions(rootComp, model_path)
                            export_successful = export_manager.execute(options)
                        else:
                            ui.messageBox("Your version of Fusion 360 does not support OBJ export via the API.")
                            return False


                    try:
                        doc.saveAs(model_name, model_dir, '', '')
                        export_successful = True
                    except:
                        # Use the original file
                        logging.info(f"Unable to export {model_extension} directly, using original file")
                        export_successful = True
                            
                except Exception as export_error:
                    logging.error(f"Error during export: {str(export_error)}")
                    # Use the original file as a fallback
                    logging.info("Using original file as fallback")
                    export_successful = True
            else:
                # If no design product or unsupported format, use the original file
                logging.info("Using original file without export")
                export_successful = True
            
            # If we combined bodies and successfully exported, now we need to undo the combine operation
            if combined_bodies and original_timeline_object and design:
                progressDialog.progressValue = 50
                progressDialog.message = "Restoring original document state..."
                
                try:
                    # Roll the timeline back to restore original state
                    timeline = design.timeline
                    timeline.rollTo(original_timeline_object)
                    logging.info("Restored original document state")
                except Exception as rollback_error:
                    logging.error(f"Error rolling back timeline: {str(rollback_error)}")
            
            if not export_successful:
                progressDialog.hide()
                ui.messageBox(f"Failed to save {model_name}. Cannot proceed with upload.",
                             "SaveToHub", 
                             adsk.core.MessageBoxButtonTypes.OKButtonType,
                             adsk.core.MessageBoxIconTypes.CriticalIconType)
                return False
            
            # Upload the file to Autodesk Hub
            progressDialog.progressValue = 70
            progressDialog.message = "Uploading to Autodesk Hub..."
            
            success = upload_file_to_hub(model_path, project_id, item_id, access_token)
            
            # Clean up the backup file
            progressDialog.progressValue = 90
            progressDialog.message = "Cleaning up..."
            
            try:
                if os.path.exists(backup_file_path):
                    os.remove(backup_file_path)
                    logging.info(f"Removed backup file: {backup_file_path}")
            except Exception as e:
                logging.warning(f"Failed to remove backup file: {str(e)}")
            
            if success:
                progressDialog.hide()
                ui.messageBox(f"Successfully exported and uploaded {model_name} to Autodesk Hub!",
                             "SaveToHub", 
                             adsk.core.MessageBoxButtonTypes.OKButtonType,
                             adsk.core.MessageBoxIconTypes.InformationIconType)
                return True
            else:
                progressDialog.hide()
                ui.messageBox(f"Failed to upload {model_name} to Autodesk Hub. The local file was exported but upload failed. See logs for details.",
                             "SaveToHub", 
                             adsk.core.MessageBoxButtonTypes.OKButtonType,
                             adsk.core.MessageBoxIconTypes.CriticalIconType)
                return False
        finally:
            progressDialog.hide()
            
    except:
        error_message = traceback.format_exc()
        logging.error(f"Failed in saveToHub: {error_message}")
        if ui:
            ui.messageBox(f'Failed in saveToHub: {error_message}',
                         'SaveToHub Error',
                         adsk.core.MessageBoxButtonTypes.OKButtonType,
                         adsk.core.MessageBoxIconTypes.CriticalIconType)
        return False
def upload_file_to_hub(file_path, project_id, item_id, access_token):
    """Upload a file to Autodesk Hub using S3 signed URLs (matching the C# approach)"""
    try:
        import json
        import os
        import time
        import requests
    
        

        logs_dir = os.path.join(os.path.expanduser("~"), "Documents", "Fusion360Logs")
        os.makedirs(logs_dir, exist_ok=True)
        log_path = os.path.join(logs_dir, 'savetohub_log.txt')
        
        # Try writing a direct message to confirm we can write to this location
        with open(log_path, 'a') as f:
            timestamp = time.strftime('%Y-%m-%d %H:%M:%S')
            f.write(f"[{timestamp}] SaveToHub function called\n")
            f.flush()
        
        logging.info(f"Starting upload of file: {file_path} to project: {project_id}, item: {item_id}")
        
        # First, get the folder ID from the metadata
        folderId = None
        metadata_path = os.path.dirname(file_path)
        fileName = os.path.basename(file_path)
        base_name = os.path.splitext(fileName)[0]
        
        # Look for metadata file
        metadata_file = os.path.join(metadata_path, f"{base_name}.metadata.json")
        if os.path.exists(metadata_file):
            try:
                with open(metadata_file, 'r') as f:
                    metadata = json.load(f)
                # Use the exact key 'folderId' from your metadata
                folderId = metadata.get("folderId")
                # Support for URN format folder IDs
                if folderId and folderId.startswith("urn:"):
                    logging.info(f"Found URN format folderId in metadata: {folderId}")
                logging.info(f"Found folderId in metadata: {folderId}")
            except Exception as e:
                logging.error(f"Error reading metadata file: {str(e)}")
        
        if not folderId:
            # Try to get folder ID from item details
            logging.info("folderId not found in metadata, fetching from item details")
            item_url = f"https://developer.api.autodesk.com/data/v1/projects/{project_id}/items/{item_id}"
            headers = {
                "Authorization": f"Bearer {access_token}"
            }
            
            try:
                response = requests.get(item_url, headers=headers)
                if response.status_code == 200:
                    item_data = response.json()
                    # Extract folder ID from parent relationship
                    try:
                        folderId = item_data["data"]["relationships"]["parent"]["data"]["id"]
                        logging.info(f"Retrieved folderId from item details: {folderId}")
                    except (KeyError, TypeError) as e:
                        logging.error(f"Failed to extract folderId from item details: {str(e)}")
                else:
                    logging.error(f"Failed to get item details. Status: {response.status_code}, Response: {response.text[:500]}")
            except Exception as e:
                logging.error(f"Error getting item details: {str(e)}")
        
        if not folderId:
            logging.error("Failed to determine folderId. Cannot proceed with upload.")
            return False
        
        # Step 1: Create storage location
        storage_url = f"https://developer.api.autodesk.com/data/v1/projects/{project_id}/storage"
        storage_payload = {
            "jsonapi": {"version": "1.0"},
            "data": {
                "type": "objects",
                "attributes": {"name": fileName},
                "relationships": {
                    "target": {
                        "data": {"type": "folders", "id": folderId}
                    }
                }
            }
        }
        
        headers = {
            "Content-Type": "application/vnd.api+json",
            "Authorization": f"Bearer {access_token}"
        }
        
        logging.info(f"Creating storage location. Payload: {json.dumps(storage_payload)}")
        response = requests.post(storage_url, headers=headers, json=storage_payload)
        
        if response.status_code not in [200, 201]:
            logging.error(f"Failed to create storage location. Status: {response.status_code}")
            logging.error(f"Response: {response.text[:500]}")
            return False
        
        storage_response = response.json()
        storage_urn = storage_response.get("data", {}).get("id")
        
        if not storage_urn:
            logging.error("Failed to get storage URN from response")
            logging.error(f"Response: {json.dumps(storage_response)[:500]}")
            return False
        
        logging.info(f"Storage location created: {storage_urn}")
        
        # Step 2: Extract bucket and object keys
        def extract_bucket_and_object_keys(storage_id):
            if not storage_id or not storage_id.startswith("urn:adsk.objects:os.object:"):
                logging.error(f"Invalid storage ID format: {storage_id}")
                return None, None
            
            # Remove prefix and split by '/'
            parts = storage_id.replace("urn:adsk.objects:os.object:", "").split('/', 1)
            
            if len(parts) < 2:
                logging.error(f"Unable to extract bucket and object key from: {storage_id}")
                return None, None
            
            bucketKey = parts[0]  # First part is the bucket key
            objectKey = parts[1]  # Second part is the object key
            
            logging.info(f"Extracted Bucket Key: {bucketKey}")
            logging.info(f"Extracted Object Key: {objectKey}")
            
            return bucketKey, objectKey
        
        bucketKey, objectKey = extract_bucket_and_object_keys(storage_urn)
        if not bucketKey or not objectKey:
            logging.error("Failed to extract bucket and object keys")
            return False
        
        # Step 3: Get signed S3 upload URL
        signed_url_endpoint = f"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}/signeds3upload"
        logging.info(f"Getting signed S3 upload URL from: {signed_url_endpoint}")
        
        response = requests.get(signed_url_endpoint, headers={"Authorization": f"Bearer {access_token}"})
        
        if response.status_code != 200:
            logging.error(f"Failed to get signed S3 upload URL. Status: {response.status_code}")
            logging.error(f"Response: {response.text[:500]}")
            return False
        
        signed_data = response.json()
        signedUrl = signed_data.get("urls", [])[0] if "urls" in signed_data and signed_data["urls"] else None
        uploadKey = signed_data.get("uploadKey")
        
        if not signedUrl or not uploadKey:
            logging.error("Failed to get signed URL or upload key from response")
            logging.error(f"Response: {json.dumps(signed_data)[:500]}")
            return False
        
        logging.info(f"Signed URL obtained: {signedUrl[:50]}...")  # Log only part for security
        logging.info(f"Upload key: {uploadKey}")
        
        # Step 4: Upload file to S3
        headers = {
            "Content-Type": "application/octet-stream"
        }
        
        try:
            with open(file_path, 'rb') as file_content:
                logging.info(f"Uploading file to S3 ({os.path.getsize(file_path)} bytes)")
                response = requests.put(signedUrl, headers=headers, data=file_content)
                
                if response.status_code not in [200, 201, 204]:
                    logging.error(f"Failed to upload file to S3. Status: {response.status_code}")
                    logging.error(f"Response: {response.text[:500]}")
                    return False
                
                logging.info("File uploaded to S3 successfully")
        except Exception as e:
            logging.error(f"Error uploading file to S3: {str(e)}")
            logging.error(traceback.format_exc())
            return False
        
        # Step 5: Complete upload
        logging.info("Finalizing S3 upload...")
        complete_payload = {"uploadKey": uploadKey}
        
        response = requests.post(
            signed_url_endpoint, 
            headers={"Authorization": f"Bearer {access_token}", "Content-Type": "application/json"}, 
            json=complete_payload
        )
        
        if response.status_code not in [200, 201, 204]:
            logging.error(f"Failed to complete upload. Status: {response.status_code}")
            logging.error(f"Response: {response.text[:500]}")
            return False
        
        logging.info("Upload completed successfully")
        
        # Step 6: Create a new version for the item
        version_url = f"https://developer.api.autodesk.com/data/v1/projects/{project_id}/versions"
        version_payload = {
            "jsonapi": {"version": "1.0"},
            "data": {
                "type": "versions",
                "attributes": {
                    "name": fileName,
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
                    },
                    "storage": {
                        "data": {
                            "type": "objects",
                            "id": storage_urn
                        }
                    }
                }
            }
        }
        
        headers = {
            "Content-Type": "application/vnd.api+json",
            "Authorization": f"Bearer {access_token}"
        }
        
        logging.info(f"Creating new version for item. Payload: {json.dumps(version_payload)}")
        response = requests.post(version_url, headers=headers, json=version_payload)
        
        if response.status_code not in [200, 201]:
            logging.error(f"Failed to create new version. Status: {response.status_code}")
            logging.error(f"Response: {response.text[:500]}")
            return False
        
        logging.info("New version created successfully")
        
        return True
    except Exception as e:
        logging.error(f"Error in upload_file_to_hub: {str(e)}")
        logging.error(traceback.format_exc())
        return False
# Create the floating palette
# def showPalette():
#     try:
#         global palette, ui
        
#         # First, check if the palette already exists
#         existingPalette = ui.palettes.itemById(paletteName)
#         if existingPalette:
#             # If it exists, just show it
#             existingPalette.isVisible = True
#             logging.info("Showing existing palette")
#             return True
            
#         # Create a new palette with a properly formatted HTML URL
#         logging.info("Creating new palette")
        
#         # Define HTML content
#         html_content = '''
#         <html>
#         <head>
#             <style>
#                 body {
#                     margin: 10px;
#                     font-family: Arial;
#                     background-color: #f0f0f0;
#                 }
#                 h3 {
#                     text-align: center;
#                     margin-top: 0;
#                 }
#                 button {
#                     width: 100%;
#                     height: 40px;
#                     background-color: #0078D7;
#                     color: white;
#                     border: none;
#                     border-radius: 5px;
#                     font-size: 16px;
#                     cursor: pointer;
#                     font-weight: bold;
#                 }
#             </style>
#         </head>
#         <body>
#             <h3>Save To Hub</h3>
#             <button id="saveButton">Save To Hub</button>
#            <script>
#             document.getElementById("saveButton").addEventListener("click", function() {
#                 // Use adsk.fusionSendData instead of window.location.href
#                 window.adsk.fusionSendData('save', '');
#             });
#         </script>
#         </body>
#         </html>
#         '''
        
#         # Create a temporary HTML file
#         html_dir = get_current_dir()
#         html_file_path = os.path.join(html_dir, 'savetohub_palette.html')
        
#         with open(html_file_path, 'w') as f:
#             f.write(html_content)
        
#         # Use file:// protocol for the HTML file URL
#         html_file_url = 'file:///' + html_file_path.replace('\\', '/')
#         logging.info(f"HTML file URL: {html_file_url}")
        
#         # Create palette with the file URL
#         palette = ui.palettes.add(
#             id=paletteName,
#             name='Save To Hub',
#             htmlFileURL=html_file_url,
#             isVisible=True,
#             showCloseButton=True,
#             isResizable=False,
#             width=250,
#             height=120,
#             useNewWebBrowser=True
#         )
        
#         # Force the palette to be floating
#        # palette.dockingState = adsk.core.PaletteDockingStates.PaletteDockStateFloating
        
#         # Position it in a visible area
#         try:
#             # Set position at top-right using fixed coordinates
#             palette.setPosition(900, 200)
#         except:
#             logging.warning("Could not set palette position")
        
#         # Add event handler
#         onHTMLEvent = PaletteEventHandler()
#         palette.incomingFromHTML.add(onHTMLEvent)
#         handlers.append(onHTMLEvent)
        
#         logging.info("Palette created successfully")
#         return True
#     except:
#         error_message = traceback.format_exc()
#         logging.error(f"Failed to create palette: {error_message}")
#         return False

# Handler for palette HTML events
# class PaletteEventHandler(adsk.core.HTMLEventHandler):
#     def __init__(self):
#         super().__init__()
    
#     def notify(self, args):
#         try:
#             htmlArgs = adsk.core.HTMLEventArgs.cast(args)
#             logging.info(f"Received HTML event: {htmlArgs.action}")
            
#             # If we get any event from the HTML side, we can handle it here
#             if htmlArgs.action == "save":
#                 saveToHub()
#         except:
#             error_message = traceback.format_exc()
#             logging.error(f"Failed in palette event: {error_message}")

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
# def createShowPaletteCommand():
#     try:
#         # Check if command already exists
#         showPaletteCmdDef = ui.commandDefinitions.itemById('ShowSaveToHubPaletteCommand')
#         if showPaletteCmdDef:
#             showPaletteCmdDef.deleteMe()
            
#         # Create command definition
#         showPaletteCmdDef = ui.commandDefinitions.addButtonDefinition(
#             'ShowSaveToHubPaletteCommand',
#             'Show Save To Hub Palette',
#             'Shows the Save To Hub floating palette',
#             ''
#         )
        
#         # Connect to command created event
#         onShowPaletteCreated = ShowPaletteCommandCreatedHandler()
#         showPaletteCmdDef.commandCreated.add(onShowPaletteCreated)
#         handlers.append(onShowPaletteCreated)
        
#         # Add to toolbar
#         utilsPanel = ui.allToolbarPanels.itemById('UtilityPanel')  # Try the correct panel name first
#         if not utilsPanel:
#             utilsPanel = ui.allToolbarPanels.itemById('UtilitiesPanel')  # Fallback to original name
        
#         if utilsPanel:
#             utilsPanel.controls.addCommand(showPaletteCmdDef, '', False)
#             logging.info(f"Command added to panel: {utilsPanel.id}")
#         else:
#             # Log all available panels for debugging
#             panel_ids = [panel.id for panel in ui.allToolbarPanels]
#             logging.info(f"Available panels: {panel_ids}")
#             logging.warning("Could not find Utility panel")
            
#         return showPaletteCmdDef
#     except:
#         error_message = traceback.format_exc()
#         logging.error(f"Failed to create show palette command: {error_message}")
#         return None

# Handler for show palette command
# class ShowPaletteCommandCreatedHandler(adsk.core.CommandCreatedEventHandler):
#     def __init__(self):
#         super().__init__()
    
#     def notify(self, args):
#         try:
#             cmd = args.command
            
#             # Connect to execute event
#             onExecute = ShowPaletteCommandExecuteHandler()
#             cmd.execute.add(onExecute)
#             handlers.append(onExecute)
#         except:
#             error_message = traceback.format_exc()
#             logging.error(f"Failed in show palette command created: {error_message}")

# # Handler for show palette command execute
# class ShowPaletteCommandExecuteHandler(adsk.core.CommandEventHandler):
#     def __init__(self):
#         super().__init__()
    
#     def notify(self, args):
#         try:
#             # Show the palette
#             showPalette()
#         except:
#             error_message = traceback.format_exc()
#             logging.error(f"Failed to show palette from command: {error_message}")

# # Handler for workspace activated event - show palette when design workspace activated
# class WorkspaceActivatedHandler(adsk.core.WorkspaceEventHandler):
#     def __init__(self):
#         super().__init__()
    
#     def notify(self, args):
#         try:
#             workspace = adsk.core.WorkspaceEventArgs.cast(args).workspace
#             if workspace.id == 'FusionSolidEnvironment':
#                 # Wait a moment before showing palette to ensure UI is ready
#                 def delayed_show():
#                     time.sleep(1)
#                     showPalette()
                
#                 # Run in a separate thread
#                 thread = threading.Thread(target=delayed_show)
#                 thread.daemon = True
#                 thread.start()
#         except:
#             error_message = traceback.format_exc()
#             logging.error(f"Failed in workspace activated: {error_message}")

# Run when Fusion 360 starts
def run(context):
    try:
        global app, ui
        app = adsk.core.Application.get()
        ui = app.userInterface
        
        logging.info("SaveToHub add-in starting...")
        
        # Delete command if it already exists
        existing_cmd = ui.commandDefinitions.itemById(commandId)
        if existing_cmd:
            logging.info(f"Command '{commandId}' already exists - removing it")
            try:
                existing_cmd.deleteMe()
            except:
                logging.warning(f"Could not delete existing command: {commandId}")
        
        # Get icon folder path
        # Adjust this path to match your actual folder structure
        icon_folder = os.path.join(get_current_dir(), 'commands', 'commandDialog', 'resources')
        
        # Log the icon path to verify it's correct
        logging.info(f"Icon folder path: {icon_folder}")
        
        # Create a command definition for the SaveToHub command with icon
        cmdDef = ui.commandDefinitions.addButtonDefinition(
            commandId, 
            commandTitle, 
            'Save your design to Autodesk Hub',
            icon_folder  # Specify the folder containing icon resources
        )
        
        # Connect to the command created event
        onCommandCreated = CommandCreatedEventHandler()
        cmdDef.commandCreated.add(onCommandCreated)
        handlers.append(onCommandCreated)
        
        # Add the command to the Make panel
        workspace = ui.workspaces.itemById('FusionSolidEnvironment')
        if workspace:
            # Get Make panel
            make_panel = workspace.toolbarPanels.itemById('MakePanel')
            
            if make_panel:
                # Check if control already exists
                existing_control = make_panel.controls.itemById(commandId)
                if existing_control:
                    existing_control.deleteMe()
                
                # Add command to panel
                control = make_panel.controls.addCommand(cmdDef)
                
                # Make sure it's promoted to appear as a button
                control.isPromoted = True
                
                logging.info("Command added to Make panel")
            else:
                logging.warning("Make panel not found")
                
                # Log available panels for debugging
                panel_ids = [panel.id for panel in workspace.toolbarPanels]
                logging.info(f"Available panels: {panel_ids}")
                logging.info(f"Panel names: {[panel.name for panel in workspace.toolbarPanels]}")
        else:
            logging.warning("FusionSolidEnvironment workspace not found")
        
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
    global handlers
    
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
        
        # Remove the command definitions if UI is still available
        if ui:
            try:
                # Get workspace, panel and control
                workspace = ui.workspaces.itemById('FusionSolidEnvironment')
                if workspace:
                    panel = workspace.toolbarPanels.itemById('MakePanel')
                    if panel:
                        command_control = panel.controls.itemById(commandId)
                        if command_control:
                            command_control.deleteMe()
                
                # Remove command definition
                cmdDef = ui.commandDefinitions.itemById(commandId)
                if cmdDef:
                    cmdDef.deleteMe()
                    logging.info("Command definition removed")
            except:
                logging.warning("Error removing command definition")
        
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