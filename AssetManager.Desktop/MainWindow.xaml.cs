using System;
using System.Threading.Tasks;
using System.Windows;
using AssetManager.Infrastructure.Services;
using Autodesk.Authentication.Model;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;


namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private string _accessToken;
        private string _projectId;
        private string _folderId = "urn:adsk.wipprod:fs.folder:co.KVsRYzFtQdi1b6j3eLcjhA";
        private string _selectedProjectId;
        private readonly ModelUpload _uploadService;

        // ✅ Default Constructor
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        // ✅ Constructor accepting UserInfo
        public MainWindow(UserInfo userData)
        {
            InitializeComponent();
            _accessToken = TokenManager.GetToken();
            _uploadService = new ModelUpload(_accessToken); // ✅ Initialize here

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
            var result = await DataManagement.GetPersonalHubDetails(); // Ensure you await the async call

            string hubID = null;

            if (result == null)
            {
                Console.WriteLine("No personal hub details found.");
            }
            else
            {
                // Deconstruct only if result is not null
                (hubID, string HubName, string HubType) = result.Value;
                Console.WriteLine($"Hub Id = {hubID}, Hub Name = {HubName}, Hub Type = {HubType}");
            }

            var projects = await DataManagement.GetAllProjectsFromHub(hubID);

            string TestProject = null; //Default project

            foreach (var (projectId, projectName) in projects)
            {
                Console.WriteLine($"📌 Project ID: {projectId}, Name: {projectName}");
                if (projectName == "Default Project")
                {
                    TestProject = projectId;
                }
            }

            List<(string FolderId, string FolderName)> folders =
                await DataManagement.GetTopLevelFolders(hubID, TestProject);

            string TestFolder = null;

            if (folders != null && folders.Count > 0)
            {
                Console.WriteLine("\n📂 Top-Level Folders:");
                foreach (var folder in folders)
                {
                    Console.WriteLine($"📁 Folder Name: {folder.FolderName}");
                    Console.WriteLine($"🔹 Folder ID: {folder.FolderId}\n");
                    if (folder.FolderId == "DefaultTestFolder")
                    {
                        TestFolder = folder.FolderId;
                    }
                }
            }
            else
            {
                Console.WriteLine("⚠️ No folders found or request failed.");
            }
            // Get the list of items with their IDs, Names, and Types
            // List<(string ItemId, string ItemName, string ItemType)> items = await DataManagement.GetFolderItems(TestProject, TestFolder);
            //
            // if (items == null || items.Count == 0)
            // {
            //     Console.WriteLine("❌ No items found or an error occurred.");
            //     return;
            // }
            //
            // // Iterate through the list and print each item's details
            // foreach (var item in items)
            // {
            //     Console.WriteLine($"Item ID: {item.ItemId}");
            //     Console.WriteLine($"Item Name: {item.ItemName}");
            //     Console.WriteLine($"Item Type: {item.ItemType}");
            //     Console.WriteLine(new string('-', 40)); // Separator line for clarity
            // }
        }

        private async void InitializeAsync()
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
        }


        private void BtnRefreshModels_Click(object sender, RoutedEventArgs e)
        {
            ListModelsForProject(_selectedProjectId, _folderId);
            Console.WriteLine("🔄 Models refreshed.");
        }

      



      
        private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {

            Console.WriteLine("📤 Upload button clicked");

            if (_uploadService == null || string.IsNullOrEmpty(_projectId) || string.IsNullOrEmpty(_folderId))
            {
                Console.WriteLine("❌ Error: Required variables are missing.");
                MessageBox.Show("❌ Error: Required variables are missing. Cannot upload.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a Model File",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                Console.WriteLine($"📂 File selected: {filePath}");

                try
                {
                    string fileUrn = await _uploadService.UploadModel(filePath, _projectId, _folderId);
                    MessageBox.Show($"✅ Upload Successful!\nFile URN: {fileUrn}", "Success", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Upload Failed: {ex.Message}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
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
                    ModelDropdown.Items.Clear(); // Clear the ComboBox before adding new items

                    // Populate ComboBox with model names
                    foreach (var model in models)
                    {
                        ModelDropdown.Items.Add(model); // Add each model name to the ComboBox
                    }
                });
            }
            else
            {
                Console.WriteLine("❌ No models found in the folder.");
            }
        }








        /*public class Project
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }*/

// Method to load projects from the hub and populate ComboBox
        private async Task LoadProjectsAsync()
        {
            var results = await DataManagement.GetPersonalHubDetails();
            var (hubId, hubName, hubType) = results.Value; // Replace this with the actual Hub ID
            var projects = await DataManagement.GetAllProjectsFromHub(hubId);

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
        private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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