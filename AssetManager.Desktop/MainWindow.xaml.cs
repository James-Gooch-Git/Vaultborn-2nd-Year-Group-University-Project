using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using AssetManager.Infrastructure.Services;
using Autodesk.Authentication.Model;
using Microsoft.Win32;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using System.Text.Json;




namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private string _accessToken;
        private string _projectId;
       //private string _folderId = "urn:adsk.wipprod:fs.folder:co.KVsRYzFtQdi1b6j3eLcjhA"; 
       private string _folderId; 
        private string _selectedProjectId;
        private string _selectedItemId;
        private readonly ModelUpload _uploadService;
        string hubID = null;
        

        // ✅ Default Constructor
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        // ✅ Constructor accepting UserInfo
        public MainWindow(string userData)
        {
            string UserId = userData;
            InitializeComponent();
            _accessToken = TokenManager.GetToken();
            _uploadService = new ModelUpload(_accessToken);  // ✅ Ensure this is correctly initialized
            Initialize();
        }


        private void Initialize()
        {


            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing.");
            }
            else
            {
                Console.WriteLine($"✅ Debug: Retrieved Access Token: {_accessToken}");
            }


            //DILLAN TESTING
            // ✅ Attach Click Events
            //BtnUploadFile.Click += BtnUploadFile_Click;
            //BtnRefreshModels.Click += BtnRefreshModels_Click;

            // ✅ Start initialization asynchronously
            //InitializeAsync();

            TestDataManagement();

            LoadProjectsAsync();

        }

        private async void TestDataManagement()
        {
            // Get personal hub details
            var result = await DataManagement.GetPersonalHubDetails(); 
            
            if (result == null)
            {
                Console.WriteLine("❌ No personal hub details found.");
                return;
            }
            
            // Deconstruct only if result is not null
            (hubID, string hubName, string hubType) = result.Value;
            Console.WriteLine($"🏠 Hub ID: {hubID}, Name: {hubName}, Type: {hubType}");

            // Get all projects from the hub
            var projects = await DataManagement.GetAllProjectsFromHub(hubID);

            string testProject = null; // Default project

            foreach (var (projectId, projectName) in projects)
            {
                Console.WriteLine($"📌 Project ID: {projectId}, Name: {projectName}");
                
                if (projectName == "Default Project")
                {
                    testProject = projectId;
                }
            }

            if (string.IsNullOrEmpty(testProject))
            {
                Console.WriteLine("❌ No default project found.");
                return;
            }
            
            // Get the top-level folder
            var topFolder = await DataManagement.GetTopLevelFolder(hubID, testProject);
            

            string testFolder = null;

            Console.WriteLine("\n📂 Top-Level Folder:");
            Console.WriteLine($"📁 Name: {topFolder.FolderName}");
            Console.WriteLine($"🔹 ID: {topFolder.FolderId}\n");

            if (topFolder.FolderId == "DefaultTestFolder")
            {
                testFolder = topFolder.FolderId;
            }

           
        }

        private async void BtnDownloadModel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_selectedItemId))
            {
                MessageBox.Show("❌ Please select a project and model before downloading.");
                return;
            }

            try
            {
                // ✅ If no folder is selected, use the top-level folder
                var (topFolderId, _) = await DataManagement.GetTopLevelFolder(hubID, _selectedProjectId);
                string folderIdToUse = string.IsNullOrEmpty(_folderId) ? topFolderId : _folderId;


                if (string.IsNullOrEmpty(folderIdToUse))
                {
                    MessageBox.Show("❌ Could not determine the folder for downloading.");
                    return;
                }

                var fileDownloadService = new FileDownloadService();
                await fileDownloadService.DownloadModel(_selectedProjectId, folderIdToUse, _selectedItemId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error downloading model: {ex.Message}");
            }
        }
        
        private async void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow(true);
            this.Close();
            loginWindow.Show();
        }
        
        // Button click event to upload file
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

            // ✅ Call UploadModel directly
            bool uploadSuccess = await _uploadService.UploadModel(_selectedProjectId, _folderId, filePath);

            if (!uploadSuccess)
            {
                Console.WriteLine("❌ File upload failed.");
                return;
            }

            Console.WriteLine("✅ Upload process completed successfully, and file is now visible in Forge!");
            MessageBox.Show("✅ Upload Successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }







// Method to open file dialog and return selected file path
        private string GetFilePathFromDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "3D Files|*.stl;*.obj;*.f3d;*.step;*.igs;*.iges;*.sldprt;*.3mf;*.fbx;*.glb;*.gltf|All Files|*.*",
                Title = "Select a 3D Model to Upload"
            };

            bool? result = openFileDialog.ShowDialog();
            return result == true ? openFileDialog.FileName : null;
        }


    


        /*private async void InitializeAsync()
        {
            try
            {
                _accessToken = TokenManager.GetToken();
                if (string.IsNullOrEmpty(_accessToken))
                {
                    Console.WriteLine("❌ Error: Access token is missing.");
                    return;
                }

                Console.WriteLine($"✅ Debug: Retrieved Access Token: {_accessToken}");

                // 🔹 Load all projects
                await LoadProjectsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during initialization: {ex.Message}");
            }
        }*/


        private void BtnRefreshModels_Click(object sender, RoutedEventArgs e)
        {
            ListModelsForProject(_selectedProjectId, _folderId);
            Console.WriteLine("🔄 Models refreshed.");
        }
      
        
        //Its this one
        private async Task ListModelsForProject(string projectId, string folderId)
        {
            // Get models from the project folder
            List<string> models = await GetModelsFromProject(projectId, folderId);

            // Check if models were retrieved
            if (models != null && models.Any())
            {
                // Clear the existing items in the ComboBox
                Dispatcher.Invoke(() =>
                {
                    ModelComboBox.Items.Clear(); // Clear the ComboBox before adding new items

                    // Populate ComboBox with model names
                    foreach (var model in models)
                    {
                        ModelComboBox.Items.Add(model); // Add each model name to the ComboBox
                    }
                });
            }
            else
            {
                Console.WriteLine("❌ No models found in the folder.");
            }
        }
      

// Method to load projects from the hub and populate ComboBox
        private async Task LoadProjectsAsync()
        {
            var results = await DataManagement.GetPersonalHubDetails();
            var (hubID, hubName, hubType) = results.Value; // Replace this with the actual Hub ID
            var projects = await DataManagement.GetAllProjectsFromHub(hubID);

            if (projects != null && projects.Any())
            {
                // Clear existing items in the ComboBox
                ProjectComboBox.Items.Clear();

                // Populate ComboBox with project names, store project IDs in Tag
                foreach (var (projectId, projectName) in projects)
                {
                    ComboBoxItem item = new ComboBoxItem
                    {
                        Content = projectName, // Display project name
                        Tag = projectId // Store project ID in Tag property
                    };
                    ProjectComboBox.Items.Add(item); // Add item to ComboBox
                }
            }
            else
            {
                Console.WriteLine("❌ No projects found or failed to load projects.");
            }
        }

// Event handler when the user selects a project
        /*private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


            // Ensure that the selection is not null or empty
            if (ProjectComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _selectedProjectId =
                    selectedItem.Tag as string; // Store the selected project ID in the class-level variable

                // Optionally, print the selected project ID
                Console.WriteLine($"Selected Project ID: {_selectedProjectId}");
                ListModelsForProject(_selectedProjectId, _folderId);

            }
        }*/
        private async void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ensure that the selection is not null or empty
            if (ProjectComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _selectedProjectId = selectedItem.Tag as string; // Store the selected project ID
                Console.WriteLine($"📌 Selected Project ID: {_selectedProjectId}");
                
                try
                {
                    // 🔹 Get the user's hub details
                    var results = await DataManagement.GetPersonalHubDetails();
                    if (results == null)
                    {
                        Console.WriteLine("❌ Error: Could not retrieve hub details.");
                        return;
                    }
                    var (hubID, hubName, hubType) = results.Value;
                    Console.WriteLine($"🏠 Hub ID: {hubID}, Name: {hubName}, Type: {hubType}");

                    // 🔹 Retrieve the top-level folder for the selected project
                    var topFolderResult = await DataManagement.GetTopLevelFolder(hubID, _selectedProjectId);

                    // Correct null check (tuples cannot be directly checked for null)
                    if (topFolderResult.FolderId == null)
                    {
                        Console.WriteLine("❌ Error: Failed to retrieve the top-level folder.");
                        _folderId = null;
                        return;
                    }

                    // ✅ Deconstruct tuple to get FolderId and FolderName
                    var (folderId, folderName) = topFolderResult;
                    _folderId = folderId; // Assign dynamically retrieved folder ID
                    Console.WriteLine($"📂 Top-Level Folder ID: {_folderId} ({folderName})");

                    // 🔹 List models using the retrieved project and folder IDs
                    await ListModelsForProject(_selectedProjectId, _folderId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                }
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



        public static async Task<List<string>> GetModelsFromProject(string projectId, string folderId)
        {
            // Get the access token
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
                    // Set the Authorization header with the access token
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    // Make the GET request
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
                        // Check if the item is a file (model)
                        if (item.GetProperty("type").GetString() == "items")
                        {
                            string modelName = item.GetProperty("attributes").GetProperty("displayName").GetString();
                            modelNames.Add(modelName); // Add the model name to the list
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
        

    }
}