using System.Diagnostics;
using System.IO;
using System.Windows;
using AssetManager.Infrastructure.Services;

namespace AssetManager.Desktop;

public class OpenFusion
{
    private string _selectedProjectId;
    private string _selectedItemId;
    private string _selectedItemName;
    private string _folderId;
    
    private void LaunchFusionWithModel(string modelPath)
    {
        string fusion360Uri = "fusion360://command=openCloudModel&itemId=urn:adsk.wipprod:dm.lineage:pwGqGrbgRx6IUlR4Wtskdg";
        
        string tempFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "fusion_model_path.txt");
        File.WriteAllText(tempFilePath, modelPath);
        
        string fusionPath = GetFusion360ExecutablePath();
        // string fusionApiPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Autodesk\\webdeploy\\production\\ec15d50cfe0119bd0166ce9a1aa68bd8f670e085\\Api");
        // string pythonScriptPath = Path.Combine(fusionApiPath, "FusionAddIn.py");

        if (string.IsNullOrEmpty(fusionPath) || !File.Exists(fusionPath))
        {
            MessageBox.Show("⚠️ Fusion 360 is not installed or could not be found.", "Fusion 360 Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        FileDownloadService2 fileDownloadService = new FileDownloadService2();

        try
        {
            fileDownloadService.DownloadModelAndSaveMetadata(_selectedProjectId, _selectedItemId, _selectedItemName, _folderId);
            // Start Fusion 360 and open the model  
            Process.Start(fusionPath, $"--exec \"{modelPath}\"");
            Console.WriteLine($"✅ Launched Fusion 360 with: {modelPath}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"❌ Failed to launch Fusion 360: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private string GetFusion360ExecutablePath()
    {
        string fusionBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Autodesk", "webdeploy", "production");
        Console.WriteLine("fusion location: " + fusionBasePath);
        
        if (Directory.Exists(fusionBasePath))
        {
            var fusionExecutables = Directory.GetFiles(fusionBasePath, "Fusion360.exe", SearchOption.AllDirectories);
            if (fusionExecutables.Length > 0)
            {
                return fusionExecutables[0]; // Return the first valid Fusion 360 path found
            }
        }

        return null; // Fusion 360 not found
    }
}