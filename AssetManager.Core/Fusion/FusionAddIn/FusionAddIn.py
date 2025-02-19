import sys
import os
import time
from subprocess import Popen
import subprocess

def log_to_file(message):
    """Log messages to a debug file"""
    log_path = os.path.join(os.path.dirname(__file__), 'fusion_debug.log')
    with open(log_path, 'a') as f:
        f.write(f"{message}\n")

def is_fusion_running():
    """Check if Fusion 360 is running"""
    for proc in os.popen('tasklist').readlines():
        if 'Fusion360.exe' in proc:
            return True
    return False

def launch_fusion():
    """Launch Fusion 360 and wait for it to start"""
    fusion_path = r"C:\Users\james\AppData\Local\Autodesk\webdeploy\production\30c9d5533837458c62c42054f4d8a9dcee4200a0\Fusion360.exe"

    if not os.path.exists(fusion_path):
        log_to_file(f"Could not find Fusion 360 at: {fusion_path}")
        raise Exception("Could not find Fusion 360 executable")

    log_to_file(f"Launching Fusion 360 from: {fusion_path}")
    Popen([fusion_path])
    
    for i in range(60):
        if is_fusion_running():
            log_to_file(f"Fusion 360 detected as running after {i} seconds")
            time.sleep(15)
            return True
        time.sleep(1)
    
    raise Exception("Timeout waiting for Fusion 360 to start")

def run(context):
    ui = None
    try:
        log_to_file("Checking if Fusion 360 is running...")
        if not is_fusion_running():
            log_to_file("Launching Fusion 360...")
            launch_fusion()
            log_to_file("Fusion 360 launched and initialized")
        else:
            log_to_file("Fusion 360 is already running")

        if len(sys.argv) < 2:
            log_to_file("No models directory specified")
            return

        models_dir = sys.argv[1]
        log_to_file(f"Looking in directory: {models_dir}")

        files = [(f, os.path.getmtime(os.path.join(models_dir, f))) 
                for f in os.listdir(models_dir) 
                if os.path.isfile(os.path.join(models_dir, f))]
        
        if not files:
            log_to_file("No files found in models directory")
            return

        latest_file = max(files, key=lambda x: x[1])[0]
        file_path = os.path.join(models_dir, latest_file)
        log_to_file(f"File to open: {file_path}")

        # Get Fusion path
        fusion_exe = r"C:\Users\james\AppData\Local\Autodesk\webdeploy\production\30c9d5533837458c62c42054f4d8a9dcee4200a0\Fusion360.exe"

        # Launch Fusion with the file as argument
        log_to_file(f"Launching Fusion with file: {file_path}")
        subprocess.Popen([fusion_exe, "/open", file_path])
        
        log_to_file("Launch command sent")
        
        # Wait for a few seconds to allow Fusion to process
        time.sleep(10)
        
        log_to_file("Process completed")

    except Exception as e:
        log_to_file(f"Error: {str(e)}")
        log_to_file(traceback.format_exc())

if __name__ == '__main__':
    try:
        log_to_file("\n\n=== Starting new run ===")
        run(None)
    except Exception as e:
        log_to_file(f"Main error: {str(e)}")
        print(f"Error: {str(e)}")