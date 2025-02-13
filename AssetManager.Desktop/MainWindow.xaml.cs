using System;
using System.Threading.Tasks;
using System.Windows;
using AssetManager.Infrastructure.Services;
using Autodesk.Authentication.Model;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private string _accessToken;
        private string _projectId;
        private string _folderId;
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

            // ✅ Attach Click Events
            BtnUploadFile.Click += BtnUploadFile_Click;
            BtnRefreshModels.Click += BtnRefreshModels_Click;

            // ✅ Start initialization asynchronously
            InitializeAsync();
        }
    

        private async void InitializeAsync()
        {
            try
            {
                _accessToken = TokenManager.GetToken(); // ✅ Get token first
                if (string.IsNullOrEmpty(_accessToken))
                {
                    Console.WriteLine("❌ Error: Access token is missing.");
                    return;
                }

                Console.WriteLine($"✅ Debug: Retrieved Access Token: {_accessToken}");

                _projectId = await LoadProjectIdAsync();
                if (string.IsNullOrEmpty(_projectId))
                {
                    Console.WriteLine("❌ Error: Project ID is missing.");
                    return;
                }

                _folderId = await LoadProjectAndFolderIds(); // ✅ Wait for folder retrieval

                if (string.IsNullOrEmpty(_folderId))
                {
                    Console.WriteLine("⚠️ Folder not found, creating a new one...");
                    _folderId = await CreateNewFolder(_projectId, _accessToken); // ✅ Create folder only if needed
                }

                if (string.IsNullOrEmpty(_folderId))
                {
                    Console.WriteLine("❌ Error: Failed to create or retrieve a folder.");
                    return;
                }

                Console.WriteLine($"✅ Loaded Folder ID: {_folderId}");

                await LoadModelListAsync(); // ✅ Load models only after folder is ready
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during initialization: {ex.Message}");
            }
        }

        private void BtnRefreshModels_Click(object sender, RoutedEventArgs e)
        {
            LoadModelListAsync();
            Console.WriteLine("🔄 Models refreshed.");
        }
        
        private async Task<string> GetOrCreateFolderAsync(string projectId, string accessToken)
        {
            try
            {
                Console.WriteLine($"🔹 Debug: Checking for existing folders in project {projectId}");

                string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.GetAsync(url);
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Debug: Folder API Response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: Failed to retrieve existing folders. Status Code: {response.StatusCode}");
                    return null;
                }

                using JsonDocument doc = JsonDocument.Parse(responseContent);

                // 🔹 Search for an existing folder called "MyModels"
                foreach (JsonElement folder in doc.RootElement.GetProperty("data").EnumerateArray())
                {
                    string folderName = folder.GetProperty("attributes").GetProperty("name").GetString();
                    string folderId = folder.GetProperty("id").GetString();

                    if (folderName == "MyModels")
                    {
                        Console.WriteLine($"✅ Found existing folder: {folderName} (ID: {folderId})");
                        return folderId; // ✅ Return the found folder ID
                    }
                }

                // 🔹 If no "MyModels" folder was found, create a new one
                Console.WriteLine("⚠️ No 'MyModels' folder found. Creating one...");
                return await CreateNewFolder(projectId, accessToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception while retrieving or creating folder: {ex.Message}");
                return null;
            }
        }

        private async Task<string> LoadProjectAndFolderIds()
        {
            try
            {
                await Task.Delay(500); // ✅ Prevent race condition
                string hubId = await DataManagement.GetPersonalHub();

                if (string.IsNullOrEmpty(hubId))
                {
                    Console.WriteLine("❌ Failed to retrieve Hub ID.");
                    return null;
                }

                _projectId = await DataManagement.GetProjectIdAsync(hubId);

                // 🔹 Ensure the project ID starts with "b."
                if (!_projectId.StartsWith("b."))
                {
                    Console.WriteLine($"⚠️ Warning: Project ID '{_projectId}' does not start with 'b.', correcting it...");
                    _projectId = "b." + _projectId.Substring(2); // ✅ Replace "a." or "w." with "b."
                }

                Console.WriteLine($"✅ Loaded Project ID: {_projectId}");

                if (!string.IsNullOrEmpty(_projectId))
                {
                    _folderId = await GetOrCreateFolderAsync(_projectId, _accessToken);

                    if (!string.IsNullOrEmpty(_folderId))
                    {
                        Console.WriteLine($"✅ Loaded Folder ID: {_folderId}");
                        return _folderId; // ✅ Return the folder ID
                    }
                    else
                    {
                        Console.WriteLine("❌ Error: Folder ID retrieval failed.");
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine("❌ Error: Cannot retrieve Folder ID without a valid Project ID.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return null;
            }
        }


        
         

       private async Task<string> CreateNewFolder(string projectId, string accessToken)
        {
            try
            {
                Console.WriteLine("🔹 Debug: Creating a new folder...");

                string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders";

                // Retrieve the default storage location or parent folder
                string parentFolderId = await GetDefaultFolderIdAsync(projectId, accessToken);
                if (string.IsNullOrEmpty(parentFolderId))
                {
                    Console.WriteLine("❌ Error: No valid parent folder found for folder creation.");
                    return null;
                }

                var requestBody = new
                {
                    jsonapi = new { version = "1.0" },
                    data = new
                    {
                        type = "folders",
                        attributes = new
                        {
                            name = "MyModels",
                            extension = new
                            {
                                type = "folders:autodesk.bim360:Folder",
                                version = "1.0"
                            }
                        },
                        relationships = new
                        {
                            parent = new
                            {
                                data = new
                                {
                                    type = "folders",
                                    id = parentFolderId // ✅ Ensure a valid parent ID
                                }
                            }
                        }
                    }
                };

                string json = JsonSerializer.Serialize(requestBody);
                using var client = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Debug: Folder Creation Response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: Failed to create folder. Status: {response.StatusCode}");
                    return null;
                }

                using JsonDocument doc = JsonDocument.Parse(responseContent);
                return doc.RootElement.GetProperty("data").GetProperty("id").GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception while creating folder: {ex.Message}");
                return null;
            }
        }

        
        private async Task<string> GetDefaultFolderIdAsync(string projectId, string accessToken)
        {
            try
            {
                Console.WriteLine($"🔹 Debug: Retrieving top-level folder ID for project {projectId}");

                string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/topFolders";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.GetAsync(url);
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Debug: Folder API Response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: Failed to retrieve Folder ID. Status Code: {response.StatusCode}");
                    return null;
                }

                using JsonDocument doc = JsonDocument.Parse(responseContent);
                string folderId = doc.RootElement.GetProperty("data")[0].GetProperty("id").GetString();

                Console.WriteLine($"✅ Debug: Retrieved Folder ID: {folderId}");
                return folderId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception while retrieving folder ID: {ex.Message}");
                return null;
            }
        }



        private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("📤 Upload button clicked");

            if (_uploadService == null || string.IsNullOrEmpty(_projectId) || string.IsNullOrEmpty(_folderId))
            {
                Console.WriteLine("❌ Error: Required variables are missing.");
                MessageBox.Show("❌ Error: Required variables are missing. Cannot upload.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show($"✅ Upload Successful!\nFile URN: {fileUrn}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Upload Failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task LoadModelListAsync()
        {
            try
            {
                List<string> modelNames = await GetModelsFromProject(_projectId, _folderId, _accessToken);
                if (modelNames == null || modelNames.Count == 0)
                {
                    Console.WriteLine("❌ No models found in this project.");
                    return;
                }

                ModelDropdown.ItemsSource = modelNames;
                Console.WriteLine("✅ Model list loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading model list: {ex.Message}");
            }
        }
        
        private async Task<string> LoadProjectIdAsync()
        {
            try
            {
                await Task.Delay(500); // ✅ Prevent race condition
                string hubId = await DataManagement.GetPersonalHub();

                if (string.IsNullOrEmpty(hubId))
                {
                    Console.WriteLine("❌ Failed to retrieve Hub ID.");
                    return null;
                }

                string projectId = await DataManagement.GetProjectIdAsync(hubId);
                Console.WriteLine($"📂 Loaded Project ID: {projectId}");

                return projectId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving project ID: {ex.Message}");
                return null;
            }
        }


        private async Task<List<string>> GetModelsFromProject(string projectId, string folderId, string accessToken)
        {
            List<string> modelNames = new List<string>();
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}/contents";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error retrieving models: {response.StatusCode}");
                return null;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseContent);
            foreach (JsonElement item in doc.RootElement.GetProperty("data").EnumerateArray())
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
}
