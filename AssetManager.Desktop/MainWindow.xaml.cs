
using System.IO;
using System.Windows;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows.Controls;
using AssetManager.Core;
using AssetManager.Infrastructure.Services;
using Microsoft.Win32;
using System.Diagnostics;

using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private string _accessToken;
        private string _selectedProjectId;
        private string _selectedItemId;
        private string _folderId;
        private string hubID;
        private readonly ModelUpload _uploadService;


        // ✅ Constructor
        public MainWindow()
        {
            InitializeComponent();
            ModelComboBox.SelectionChanged += ModelComboBox_SelectionChanged;
            Initialize();
        }

        public MainWindow(string userData)
        {
            InitializeComponent();
            _accessToken = TokenManager.GetToken();
            _uploadService = new ModelUpload(_accessToken);
            Initialize();
        }


        private async void Initialize()
        {
            _accessToken = TokenManager.GetToken();

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing.");
                return;
            }

            Console.WriteLine($"✅ Debug: Retrieved Access Token: {_accessToken}");

            // 🔹 Initialize data
            await TestDataManagement();
            await LoadProjectsAsync();
            FusionManager.InitializePythonEngine();
        }


        private async Task TestDataManagement()
        {
            var result = await DataManagement.GetPersonalHubDetails();
            if (result == null)
            {
                Console.WriteLine("❌ No personal hub details found.");
                return;
            }

            (hubID, string hubName, string hubType) = result.Value;
            Console.WriteLine($"🏠 Hub ID: {hubID}, Name: {hubName}, Type: {hubType}");

            var projects = await DataManagement.GetAllProjectsFromHub(hubID);

            string projectID = null;

            foreach (var (projectId, projectName) in projects)
            {
                Console.WriteLine($"📌 Project ID: {projectId}, Name: {projectName}");
                if (projectName == "Default Project")
                {
                    projectID = projectId;
                    Console.WriteLine("lorem ipsum dolor\n\n\n\n\n\n");
                }
            }

            var topFolder = await DataManagement.GetTopLevelFolder(hubID, projectID);
            var folderId = topFolder.Item1;

            string itemId = null;
            string itemName = null;

            var items = await DataManagement.GetFolderItems(projectID, folderId);
            foreach (var item in items)
            {
                //Console.WriteLine($"item name: {item.Item2}");
                if (item.Item2 == "Tourus.obj")
                {
                    itemId = item.Item1;
                    itemName = item.Item2;
                }
            }

            Console.WriteLine($"Item ID: {itemId}  Name: {itemName}");
        }


        private async Task LoadProjectsAsync()
        {
            var results = await DataManagement.GetPersonalHubDetails();

            // ✅ Check if `results` has a value
            if (results == null || !results.HasValue)
            {
                Console.WriteLine("❌ Error: No personal hub details found.");
                return;
            }

            var (hubID, hubName, hubType) = results.Value; // ✅ Now it's safe to access

            Console.WriteLine($"✅ Retrieved Hub ID: {hubID}, Name: {hubName}, Type: {hubType}");

            var projects = await DataManagement.GetAllProjectsFromHub(hubID);
            if (projects != null && projects.Any())
            {
                ProjectComboBox.Items.Clear();
                foreach (var (projectId, projectName) in projects)
                {
                    ComboBoxItem item = new ComboBoxItem
                    {
                        Content = projectName,
                        Tag = projectId
                    };
                    ProjectComboBox.Items.Add(item);
                }
            }
            else
            {
                Console.WriteLine("❌ No projects found or failed to load projects.");
            }
        }

//WOerking One
        /*private async void BtnDownloadModel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_selectedItemId))
            {
                MessageBox.Show("❌ Please select a project and model before downloading.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var fileDownloadService = new FileDownloadService();

                // ✅ Step 1: Retrieve Storage ID
                string storageId = await fileDownloadService.GetStorageIdFromItem(_selectedProjectId, _selectedItemId);

                if (string.IsNullOrEmpty(storageId))
                {
                    MessageBox.Show("❌ Could not retrieve storage ID.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ Step 2: Extract Bucket and Object Keys
                var (bucketKey, objectKey) = fileDownloadService.ExtractBucketAndObjectKeys(storageId);

                // ✅ Step 3: Retrieve Access Token
                string accessToken = TokenManager.GetToken();

                // ✅ Step 4: Fetch Signed URL
                string signedUrl = await fileDownloadService.GetSignedDownloadUrl(bucketKey, objectKey, accessToken);

                if (string.IsNullOrEmpty(signedUrl))
                {
                    MessageBox.Show("❌ Failed to retrieve signed URL.", "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ Step 5: Retrieve Correct Filename
                string fileName = await fileDownloadService.GetItemFileNameAsync(_selectedProjectId, _selectedItemId, accessToken);
                fileName = fileDownloadService.RemoveInvalidFileNameChars(fileName); // Ensure filename is valid

                // ✅ Step 6: Define Save Location
                string saveDirectory = @"C:\Users\james\Downloads"; // Modify if needed

                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }

                // ✅ Step 7: Download the File with Correct Filename
                await fileDownloadService.DownloadFileAsync(signedUrl, saveDirectory, fileName);

                MessageBox.Show($"✅ File downloaded successfully!\nSaved as: {fileName}", "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error downloading model: {ex.Message}", "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }*/
        /*private async void BtnDownloadModel_Click(object sender, RoutedEventArgs e)
        {
            var fileDownloadService = new FileDownloadService2();
            await fileDownloadService.DownloadModelAsync(_selectedProjectId, _selectedItemId);
        }*/

        private async void BtnDownloadModel_Click(object sender, RoutedEventArgs e)
        {
            if (ModelComboBox.SelectedItem == null)
            {
                MessageBox.Show("❌ Please select a model before downloading.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            //string selectedModelId = ModelComboBox.SelectedValue.ToString(); // Ensure this is the correct ID
            var fileDownloadService2 = new FileDownloadService2();
            await fileDownloadService2.DownloadModelAsync(_selectedProjectId, _selectedItemId);
        }


        private async void BtnViewInFusion_Click(object sender, RoutedEventArgs e)
        {
            if (ModelComboBox.SelectedItem == null)
            {
                MessageBox.Show("❌ Please select a model before viewing in Fusion 360.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                // Download the model first
                var fileDownloadService = new FileDownloadService2();
                await fileDownloadService.DownloadModelAsync(_selectedProjectId, _selectedItemId);

                // Now launch Fusion with the downloaded model
                LaunchFusionWithModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error preparing model for Fusion 360: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LaunchFusionWithModel()
        {
            string fusion360Uri = "fusion360://command=openCloudModel&itemId=urn:adsk.wipprod:dm.lineage:pwGqGrbgRx6IUlR4Wtskdg";
        }


//        private void LaunchFusionWithModel()
// {
//     try
//     {
//         string modelPath = Path.Combine(
//             Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
//             "DownloadedModels"
//         );
//
//         // Define Fusion paths
//         string fusionBasePath = @"C:\Users\james\AppData\Local\Autodesk\webdeploy\production";
//         string versionPath = Path.Combine(fusionBasePath, "30c9d5533837458c62c42054f4d8a9dcee4200a0");
//         string pythonPath = Path.Combine(versionPath, "Python");
//         string apiPath = Path.Combine(versionPath, "Api");
//         string pythonExe = Path.Combine(pythonPath, "python.exe");
//
//         if (!File.Exists(pythonExe))
//         {
//             MessageBox.Show($"❌ Cannot find Fusion's Python at: {pythonExe}\nPlease verify Fusion 360 is installed correctly.", 
//                 "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//             return;
//         }
//
//         string pythonScriptPath = @"C:\Users\james\Desktop\AssetManagerTom2\AssetManager\AssetManager.Core\Fusion\FusionAddIn\FusionAddIn.py";
//
//         if (!File.Exists(pythonScriptPath))
//         {
//             MessageBox.Show($"❌ Python script not found at: {pythonScriptPath}", 
//                 "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//             return;
//         }
//
//         ProcessStartInfo startInfo = new ProcessStartInfo
//         {
//             FileName = pythonExe,
//             Arguments = $"\"{pythonScriptPath}\" \"{modelPath}\"",
//             RedirectStandardOutput = true,
//             RedirectStandardError = true,
//             UseShellExecute = false,
//             CreateNoWindow = true,
//             WorkingDirectory = Path.GetDirectoryName(pythonScriptPath)
//         };
//
//         // Set up environment variables for Fusion Python
//         startInfo.EnvironmentVariables["PYTHONPATH"] = $"{apiPath};{pythonPath}";
//         startInfo.EnvironmentVariables["PYTHONHOME"] = pythonPath;
//         startInfo.EnvironmentVariables["PATH"] = $"{pythonPath};{apiPath};{Environment.GetEnvironmentVariable("PATH")}";
//
//         using (Process process = new Process { StartInfo = startInfo })
//         {
//             process.Start();
//             string output = process.StandardOutput.ReadToEnd();
//             string error = process.StandardError.ReadToEnd();
//             process.WaitForExit();
//
//             if (!string.IsNullOrEmpty(error))
//             {
//                 MessageBox.Show($"❌ Error:\n{error}", "Error", 
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//             if (!string.IsNullOrEmpty(output))
//             {
//                 MessageBox.Show($"✅ Output:\n{output}", "Info", 
//                     MessageBoxButton.OK, MessageBoxImage.Information);
//             }
//         }
//     }
//     catch (Exception ex)
//     {
//         MessageBox.Show($"❌ Error launching Fusion script: {ex.Message}\n{ex.StackTrace}", 
//             "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//     }
//    }



    private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            string filePath = GetFilePathFromDialog();
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("❌ No file selected.");
                return;
            }

            if (string.IsNullOrEmpty(_selectedProjectId))
            {
                Console.WriteLine("❌ No project selected. Please select a project before uploading.");
                return;
            }

            Console.WriteLine($"🚀 Uploading {Path.GetFileName(filePath)} to Autodesk Forge...");

            bool uploadSuccess = await _uploadService.UploadModel(_selectedProjectId, _folderId, filePath);

            if (!uploadSuccess)
            {
                Console.WriteLine("❌ File upload failed.");
                return;
            }

            Console.WriteLine("✅ Upload process completed successfully!");
            MessageBox.Show("✅ Upload Successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        
        private async void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _selectedProjectId = selectedItem.Tag as string;
                Console.WriteLine($"📌 Selected Project ID: {_selectedProjectId}");

                try
                {
                    var results = await DataManagement.GetPersonalHubDetails();
                    if (results == null)
                    {
                        Console.WriteLine("❌ Error: Could not retrieve hub details.");
                        return;
                    }
                    var (hubID, hubName, hubType) = results.Value;

                    var topFolderResult = await DataManagement.GetTopLevelFolder(hubID, _selectedProjectId);
                    if (topFolderResult.FolderId == null)
                    {
                        Console.WriteLine("❌ Error: Failed to retrieve the top-level folder.");
                        _folderId = null;
                        return;
                    }

                    _folderId = topFolderResult.FolderId;
                    Console.WriteLine($"📂 Top-Level Folder ID: {_folderId}");

                    await ListModelsForProject(_selectedProjectId, _folderId);
                    await RetrieveItemIdAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                }
            }
        }
       

        private async Task RetrieveItemIdAsync()
{
    if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_folderId))
    {
        Console.WriteLine("❌ Cannot retrieve items - project or folder is missing.");
        return;
    }

    try
    {
        Console.WriteLine($"🔍 Fetching items for Folder: {_folderId}");
        var items = await DataManagement.GetItemsInFolder(_selectedProjectId, _folderId);

        if (items == null || !items.Any())
        {
            Console.WriteLine("❌ No items found in the selected folder.");
            return;
        }

        Dispatcher.Invoke(() =>
        {
            ModelComboBox.Items.Clear();
            foreach (var (itemId, itemName) in items)
            {
                var comboBoxItem = new ComboBoxItem
                {
                    Content = itemName,
                    Tag = itemId
                };
                ModelComboBox.Items.Add(comboBoxItem);
            }
        });

        Console.WriteLine($"✅ {items.Count} items added to dropdown.");

        // 🔹 Step 1: Retrieve storage ID for the selected item
        foreach (var (itemId, itemName) in items)
        {
            FileDownloadService fileDownloadService = new FileDownloadService();
            string storageId = await fileDownloadService.GetStorageIdFromItem(_selectedProjectId, itemId);


            if (string.IsNullOrEmpty(storageId))
            {
                Console.WriteLine($"❌ Error: Could not retrieve storage ID for {itemName}.");
                continue;
            }
            Console.WriteLine($"📦 Storage ID for {itemName}: {storageId}");

            // 🔹 Step 2: Retrieve versions for the selected item
            var versions = await fileDownloadService.GetVersionsForItemAsync(_selectedProjectId, itemId);

            if (versions == null || !versions.Any())
            {
                Console.WriteLine($"❌ No versions found for {itemName}.");
                continue;
            }

            Console.WriteLine($"✅ {versions.Count} versions found for {itemName}.");
            foreach (var (versionId, versionName, versionStorageId) in versions)
            {
                Console.WriteLine($"📄 Version: {versionName}, ID: {versionId}, Storage ID: {versionStorageId}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error retrieving items: {ex.Message}");
    }
}

        
        // 🔹 Fetch all versions of an Item
        private async Task<List<(string versionId, string versionName, string storageId)>> GetVersionsForItemAsync(string projectId, string itemId)
        {
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(itemId))
            {
                Console.WriteLine("❌ Error: Project ID or Item ID is missing.");
                return null;
            }

            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions";
            string accessToken = TokenManager.GetToken();

            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                Console.WriteLine($"🔍 Fetching versions for Item: {itemId}");
                HttpResponseMessage response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error retrieving versions. Status Code: {response.StatusCode}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                List<(string versionId, string versionName, string storageId)> versions = new();

                if (root.TryGetProperty("data", out JsonElement versionsArray))
                {
                    foreach (JsonElement versionElement in versionsArray.EnumerateArray())
                    {
                        string versionId = versionElement.GetProperty("id").GetString();
                        string versionName = versionElement.GetProperty("attributes").GetProperty("displayName").GetString();

                        string storageId = null;
                        if (versionElement.TryGetProperty("relationships", out JsonElement relationships) &&
                            relationships.TryGetProperty("storage", out JsonElement storage) &&
                            storage.TryGetProperty("data", out JsonElement storageData) &&
                            storageData.TryGetProperty("id", out JsonElement storageIdElement))
                        {
                            storageId = storageIdElement.GetString();
                        }

                        Console.WriteLine($"📄 Found Version: {versionName} (ID: {versionId}) - Storage ID: {storageId}");
                        versions.Add((versionId, versionName, storageId));
                    }
                }

                return versions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception while retrieving versions: {ex.Message}");
                return null;
            }
        }
        // 🔹 Fetch Storage ID from an Item

        
        private async Task<List<string>> GetModelsFromProject(string projectId, string folderId)
        {
            string accessToken = TokenManager.GetToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("❌ No valid access token.");
                return null;
            }

            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}/contents";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
                        return null;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;

                    List<string> modelNames = new List<string>();

                    foreach (JsonElement item in root.GetProperty("data").EnumerateArray())
                    {
                        if (item.GetProperty("type").GetString() == "items")
                        {
                            string modelName = item.GetProperty("attributes").GetProperty("displayName").GetString();
                            modelNames.Add(modelName);
                        }
                    }

                    return modelNames;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return null;
            }
        }
        // ✅ Button Click Event to Refresh Models
        
        
        private void BtnRefreshModels_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("🔄 Refreshing models...");
            if (!string.IsNullOrEmpty(_selectedProjectId) && !string.IsNullOrEmpty(_folderId))
            {
                ListModelsForProject(_selectedProjectId, _folderId);
            }
            else
            {
                Console.WriteLine("❌ Error: No project or folder selected.");
            }
        }

        // ✅ Button Click Event to Log Out
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("👤 Logging out...");
    
            // Close the current window and show the login screen
            LoginWindow loginWindow = new LoginWindow(true);
            this.Close();
            loginWindow.Show();
        }

        
        private async Task ListModelsForProject(string projectId, string folderId)
        {
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(folderId))
            {
                Console.WriteLine("❌ Error: Project ID or Folder ID is missing.");
                return;
            }

            try
            {
                // 🔹 Fetch models from the project folder
                var models = await GetModelsFromProject(projectId, folderId);

                if (models != null && models.Any())
                {
                    Dispatcher.Invoke(() =>
                    {
                        ModelComboBox.Items.Clear(); // ✅ Clear existing items in dropdown

                        foreach (var model in models)
                        {
                            ModelComboBox.Items.Add(new ComboBoxItem { Content = model }); // ✅ Add each model name
                        }
                    });

                    Console.WriteLine($"✅ {models.Count} models added to dropdown.");
                }
                else
                {
                    Console.WriteLine("❌ No models found in the folder.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving models: {ex.Message}");
            }
        }


        private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _selectedItemId = selectedItem.Tag as string;
                Console.WriteLine($"📌 Selected Item ID: {_selectedItemId}");
            }
        }

        private string GetFilePathFromDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "3D Files|*.stl;*.obj;*.f3d;*.step;*.igs;*.iges;*.sldprt;*.3mf;*.fbx;*.glb;*.gltf|All Files|*.*",
                Title = "Select a 3D Model to Upload"
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }
      
       


    }
}
