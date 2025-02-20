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
using MaterialDesignThemes.Wpf;
using MongoDB.Driver;
using MongoDB.Bson;


using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AssetManager.Infrastructure.Data;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Net;


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
        private List<Dictionary<string, string>> Models;
        private string _userId = Environment.GetEnvironmentVariable("userId", EnvironmentVariableTarget.User);


        // ✅ Constructor
        public MainWindow()
        {
            InitializeComponent();
            //ModelComboBox.SelectionChanged += ModelComboBox_SelectionChanged;
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

            Username_TextBlock.Text = await GetUserName(_userId);
            UserPic_Image.Source = new BitmapImage(new Uri(await GetUserPic(_userId)));

            // 🔹 Initialize data
            await TestDataManagement();
            await LoadProjectsAsync();
            LoadAllModels();
            FusionManager.InitializePythonEngine();
        }

        private async Task<string> GetUserName(string userId)
        {
            MongoConnection database = new MongoConnection();
            var userData = await database.Users.Find(x => x.Id == userId).FirstOrDefaultAsync();
            return userData.Username;
        }

        private async Task<string> GetUserPic(string userId)
        {
            MongoConnection database = new MongoConnection();
            var userData = await database.Users.Find(x => x.Id == userId).FirstOrDefaultAsync();
            return userData.ProfilePic;
        }

        private async void LoadAllModels()
        {
            Models = await GetAllModels();

            if (Models != null)
            {
                ModelsDataGrid.ItemsSource = Models;
            }
        }

        //private void DragDeltaThumb(object sender, DragDeltaEventArgs e)
        //{
        //    if (ResizeSidebar.Width.IsAuto || ResizeSidebar.Width.IsStar)
        //    {
        //        ResizeSidebar.Width = new GridLength(ResizeSidebar.ActualWidth, GridUnitType.Pixel);
        //    }

        //    double newWidth = ResizeSidebar.Width.Value = e.HorizontalChange;

        //    if (newWidth > 100 && newWidth < 400)
        //    {
        //        ResizeSidebar.Width = new GridLength(newWidth);
        //    }
        //}

        
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
            string projectid = null;

            foreach (var (projectId, projectName) in projects)
            {
                Console.WriteLine($"\n📌 Project ID: {projectId}, Name: {projectName}\n");
                if (projectName == "Default Project")
                {
                    projectid = projectId;
                }
            }
            var topFolder = await DataManagement.GetTopLevelFolder(hubID, projectid);
            string folderId = topFolder.Item1;

            var items = await DataManagement.GetItemsInFolder(projectid, folderId);

            foreach (var (itemId, itemName) in items)
            {
                Console.WriteLine($"\nItem Name: {itemName}\t Item ID: {itemId}\n");
            }

            string itemid = "urn: adsk.wipprod:dm.lineage:pwGqGrbgRx6IUlR4Wtskdg";

            Console.WriteLine("\n\nShowing example thumbnail\n\n");
            await ShowThumbnail(projectid, itemid);


        }

        public async Task ShowThumbnail(string projectId, string itemId)
        {
            string thumbnailUrl = await DataManagement.GetLatestItemThumbnail(projectId, itemId);

            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                Console.WriteLine("❌ Thumbnail URL is null or empty.");
                return;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Ensure valid token
                    string accessToken = TokenManager.GetToken();
                    

                    // Add Authorization header
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    // Debug: Print Thumbnail URL
                    Console.WriteLine($"📷 Fetching thumbnail from: {thumbnailUrl}");

                    byte[] imageBytes = await client.GetByteArrayAsync(thumbnailUrl);

                    // Create a BitmapImage from the byte array
                    BitmapImage bitmapImage = new BitmapImage();
                    using (MemoryStream stream = new MemoryStream(imageBytes))
                    {
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();
                    }

                    bitmapImage.Freeze(); // Make it usable across threads

                    // Update UI on the main thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ThumbnailImage.Source = bitmapImage;
                    });
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("❌ Unauthorized! Check your access token and permissions.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading thumbnail: {ex.Message}");
            }
        }






        private async Task LoadProjectsAsync()
        {
            var results = await DataManagement.GetPersonalHubDetails();

            if (results == null || !results.HasValue)
            {
                Console.WriteLine("❌ Error: No personal hub details found.");
                return;
            }

            var (hubID, hubName, hubType) = results.Value;
            Console.WriteLine($"✅ Retrieved Hub ID: {hubID}, Name: {hubName}, Type: {hubType}");

            var projects = await DataManagement.GetAllProjectsFromHub(hubID);
            if (projects == null || !projects.Any())
            {
                Console.WriteLine("❌ No projects found or failed to load projects.");
            }

            ProjectTreeView.Items.Clear();

            foreach (var (projectId, projectName) in projects)
            {
                var topFolder = await DataManagement.GetTopLevelFolder(hubID, projectId);
                var folderId = topFolder.Item1;

                TreeViewItem projectItem = new TreeViewItem
                {
                    Header = $"📁 {projectName}",
                    Tag = (projectId, folderId, true)
                };

                projectItem.Items.Add(null); // Placeholder for expansion
                projectItem.Expanded += TreeViewItem_Expanded;

                ProjectTreeView.Items.Add(projectItem);
            }
        }

        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.Tag is (string projectId, string folderId, bool isFolder))
            {
                if (item.Items.Count == 1 && item.Items[0] == null) // Check if it needs loading
                {
                    item.Items.Clear();

                    var items = await DataManagement.GetProjectItems(projectId, folderId);

                    if (items == null || !items.Any())
                    {
                        item.Items.Add(new TreeViewItem { Header = "❌ No items found" });
                        return;
                    }

                    foreach (var (itemId, itemName, isFolderItem) in items)
                    {
                        TreeViewItem fileItem = new TreeViewItem
                        {
                            Header = isFolderItem ? $"📁 {itemName}" : $"📄 {itemName}",
                            Tag = (projectId, itemId, isFolderItem),
                            ContextMenu = CreateContextMenu(projectId, itemId, isFolderItem) // ✅ Add right-click menu
                        };

                        if (isFolderItem)
                        {
                            await LoadSubfoldersAsync(fileItem, projectId, itemId);
                        }

                        item.Items.Add(fileItem);
                    }
                }
            }
        }

        private async Task LoadSubfoldersAsync(TreeViewItem parentFolder, string projectId, string folderId)
        {
            var subItems = await DataManagement.GetProjectItems(projectId, folderId);

            if (subItems == null || !subItems.Any())
            {
                return; // No subfolders to add
            }

            foreach (var (subItemId, subItemName, isSubFolder) in subItems)
            {
                TreeViewItem subItem = new TreeViewItem
                {
                    Header = isSubFolder ? $"📁 {subItemName}" : $"📄 {subItemName}",
                    Tag = (projectId, subItemId, isSubFolder),
                    ContextMenu = CreateContextMenu(projectId, subItemId, isSubFolder) // ✅ Add right-click menu
                };

                if (isSubFolder)
                {
                    await LoadSubfoldersAsync(subItem, projectId, subItemId);
                }

                parentFolder.Items.Add(subItem);
            }
        }

        private ContextMenu CreateContextMenu(string projectId, string itemId, bool isFolder)
        {
            ContextMenu menu = new ContextMenu();

            // Only show "Create Folder" for items under the top-level project, excluding subfolders
            //if (!isFolder)
            {
                MenuItem createFolderItem = new MenuItem { Header = "📁 Create New Folder" };
                createFolderItem.Click += async (s, e) => await CreateNewFolder(projectId);
                menu.Items.Add(createFolderItem);
            }

            return menu;
        }

        private async Task CreateNewFolder(string projectId)
        {
            string folderName = PromptForFolderName();
            if (string.IsNullOrWhiteSpace(folderName)) return;

            // Disable menu temporarily
            ContextMenu currentMenu = ContextMenuService.GetContextMenu(ProjectTreeView);
            if (currentMenu != null)
            {
                currentMenu.IsEnabled = false;
            }

            // Get the top-level folder for the project
            var topFolder = await DataManagement.GetTopLevelFolder(projectId, projectId);
            var parentFolderId = topFolder.Item1;

            bool success = await DataManagement.CreateNewFolder(projectId, parentFolderId, folderName);

            if (currentMenu != null)
            {
                currentMenu.IsEnabled = true;
            }

            if (success)
            {
                MessageBox.Show($"✅ Folder '{folderName}' created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Add the new folder to the top-level folder
                TreeViewItem parentItem = FindTreeViewItem(ProjectTreeView.Items, parentFolderId);
                if (parentItem != null)
                {
                    // Only add the new folder, without loading subfolders from the parent
                    await AddNewFolderToTreeView(parentItem, projectId, parentFolderId, folderName);
                }
            }
        }

        private async Task AddNewFolderToTreeView(TreeViewItem parentItem, string projectId, string parentFolderId, string folderName)
        {
            TreeViewItem newFolderItem = new TreeViewItem
            {
                Header = $"📁 {folderName}",
                Tag = (projectId, parentFolderId, true),
                ContextMenu = CreateContextMenu(projectId, parentFolderId, true) // Add context menu for folder operations
            };

            // Add the new folder to the parent folder’s items
            parentItem.Items.Add(newFolderItem);
        }

        private string PromptForFolderName()
        {
            // Create a simple input dialog
            string folderName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the name for the new folder:",
                "Create New Folder",
                "New Folder"
            );

            return string.IsNullOrWhiteSpace(folderName) ? null : folderName;
        }



        private TreeViewItem FindTreeViewItem(ItemCollection items, string folderId)
        {
            if (items == null) return null; // ✅ Prevent null reference

            foreach (TreeViewItem item in items)
            {
                if (item?.Tag is (string _, string id, _) && id == folderId) // ✅ Null check
                {
                    return item;
                }

                TreeViewItem found = FindTreeViewItem(item?.Items, folderId);
                if (found != null) return found;
            }
            return null;
        }

        private async Task<List<Dictionary<string, string>>> GetAllModels()
        {
            List<Dictionary<string, string>> allModels = new List<Dictionary<string, string>>();

            string accessToken = TokenManager.GetToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("❌ No valid access token.");
                return null;
            }

            string hubsUrl = "https://developer.api.autodesk.com/project/v1/hubs";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    HttpResponseMessage hubsResponse = await client.GetAsync(hubsUrl);

                    if (!hubsResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ Error fetching hubs: {hubsResponse.StatusCode} - {hubsResponse.ReasonPhrase}");
                        return null;
                    }

                    string hubsJson = await hubsResponse.Content.ReadAsStringAsync();
                    using JsonDocument hubsDoc = JsonDocument.Parse(hubsJson);
                    JsonElement hubsRoot = hubsDoc.RootElement;

                    foreach (JsonElement hub in hubsRoot.GetProperty("data").EnumerateArray())
                    {
                        string hubID = hub.GetProperty("id").GetString();

                        string projectsUrl = $"https://developer.api.autodesk.com/project/v1/hubs/{hubID}/projects";
                        HttpResponseMessage projectsResponse = await client.GetAsync(projectsUrl);

                        if (!projectsResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"❌ Error fetching projects for hub {hubID}: {projectsResponse.StatusCode}");
                            continue;
                        }

                        string projectsJson = await projectsResponse.Content.ReadAsStringAsync();
                        using JsonDocument projectsDoc = JsonDocument.Parse(projectsJson);
                        JsonElement projectsRoot = projectsDoc.RootElement;

                        foreach (JsonElement project in projectsRoot.GetProperty("data").EnumerateArray())
                        {
                            string projectId = project.GetProperty("id").GetString();
                            string projectName = project.GetProperty("attributes").GetProperty("name").GetString();

                            var topFolder = await DataManagement.GetTopLevelFolder(hubID, projectId);
                            string folderId = topFolder.Item1;

                            if (string.IsNullOrEmpty(folderId))
                            {
                                Console.WriteLine($"❌ No valid top-level folder found for project {projectId}");
                                continue;
                            }

                            string modelsUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}/contents";
                            HttpResponseMessage modelsResponse = await client.GetAsync(modelsUrl);

                            if (!modelsResponse.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"❌ Error fetching models for folder {folderId}: {modelsResponse.StatusCode}");
                                continue;
                            }

                            string modelsJson = await modelsResponse.Content.ReadAsStringAsync();
                            using JsonDocument modelsDoc = JsonDocument.Parse(modelsJson);
                            JsonElement modelsRoot = modelsDoc.RootElement;

                            foreach (JsonElement item in modelsRoot.GetProperty("data").EnumerateArray())
                            {
                                if (item.GetProperty("type").GetString() == "items")
                                {
                                    var attributes = item.GetProperty("attributes");

                                    string modelName = attributes.GetProperty("displayName").GetString();
                                    string lastModified = attributes.TryGetProperty("lastModifiedTime", out JsonElement modifiedTime) ? modifiedTime.GetString() : "Unknown";

                                    allModels.Add(new Dictionary<string, string>
                                    {
                                        { "Name", modelName },
                                        { "Project", projectName },
                                        { "LastModified", lastModified }
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return null;
            }

            return allModels;
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

        //private async void BtnDownloadModel_Click(object sender, RoutedEventArgs e)
        //{
        //    if (ModelComboBox.SelectedItem == null)
        //    {
        //        MessageBox.Show("❌ Please select a model before downloading.", "Error", MessageBoxButton.OK,
        //            MessageBoxImage.Error);
        //        return;
        //    }

        //    //string selectedModelId = ModelComboBox.SelectedValue.ToString(); // Ensure this is the correct ID
        //    var fileDownloadService2 = new FileDownloadService2();
        //    await fileDownloadService2.DownloadModelAsync(_selectedProjectId, _selectedItemId);
        //}


        //private async void BtnViewInFusion_Click(object sender, RoutedEventArgs e)
        //{
        //    if (ModelComboBox.SelectedItem == null)
        //    {
        //        MessageBox.Show("❌ Please select a model before viewing in Fusion 360.", "Error", MessageBoxButton.OK,
        //            MessageBoxImage.Error);
        //        return;
        //    }

        //    try
        //    {
        //        // Download the model first
        //        var fileDownloadService = new FileDownloadService2();
        //        await fileDownloadService.DownloadModelAsync(_selectedProjectId, _selectedItemId);

        //        // Now launch Fusion with the downloaded model
        //        LaunchFusionWithModel();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"❌ Error preparing model for Fusion 360: {ex.Message}", "Error", MessageBoxButton.OK,
        //            MessageBoxImage.Error);
        //    }
        //}

        private void LaunchFusionWithModel()
        {
            try
            {
                string modelPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "DownloadedModels"
                );

                // Define Fusion paths
                string fusionBasePath = @"C:\Users\james\AppData\Local\Autodesk\webdeploy\production";
                string versionPath = Path.Combine(fusionBasePath, "30c9d5533837458c62c42054f4d8a9dcee4200a0");
                string pythonPath = Path.Combine(versionPath, "Python");
                string apiPath = Path.Combine(versionPath, "Api");
                string pythonExe = Path.Combine(pythonPath, "python.exe");

                if (!File.Exists(pythonExe))
                {
                    MessageBox.Show($"❌ Cannot find Fusion's Python at: {pythonExe}\nPlease verify Fusion 360 is installed correctly.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string pythonScriptPath = @"C:\Users\james\Desktop\AssetManagerTom2\AssetManager\AssetManager.Core\Fusion\FusionAddIn\FusionAddIn.py";

                if (!File.Exists(pythonScriptPath))
                {
                    MessageBox.Show($"❌ Python script not found at: {pythonScriptPath}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{pythonScriptPath}\" \"{modelPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(pythonScriptPath)
                };

                // Set up environment variables for Fusion Python
                startInfo.EnvironmentVariables["PYTHONPATH"] = $"{apiPath};{pythonPath}";
                startInfo.EnvironmentVariables["PYTHONHOME"] = pythonPath;
                startInfo.EnvironmentVariables["PATH"] = $"{pythonPath};{apiPath};{Environment.GetEnvironmentVariable("PATH")}";

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        MessageBox.Show($"❌ Error:\n{error}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    if (!string.IsNullOrEmpty(output))
                    {
                        MessageBox.Show($"✅ Output:\n{output}", "Info",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error launching Fusion script: {ex.Message}\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



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


        //private async void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (ProjectComboBox.SelectedItem is ComboBoxItem selectedItem)
        //    {
        //        _selectedProjectId = selectedItem.Tag as string;
        //        Console.WriteLine($"📌 Selected Project ID: {_selectedProjectId}");

        //        try
        //        {
        //            var results = await DataManagement.GetPersonalHubDetails();
        //            if (results == null)
        //            {
        //                Console.WriteLine("❌ Error: Could not retrieve hub details.");
        //                return;
        //            }
        //            var (hubID, hubName, hubType) = results.Value;

        //            var topFolderResult = await DataManagement.GetTopLevelFolder(hubID, _selectedProjectId);
        //            if (topFolderResult.FolderId == null)
        //            {
        //                Console.WriteLine("❌ Error: Failed to retrieve the top-level folder.");
        //                _folderId = null;
        //                return;
        //            }

        //            _folderId = topFolderResult.FolderId;
        //            Console.WriteLine($"📂 Top-Level Folder ID: {_folderId}");

        //            await ListModelsForProject(_selectedProjectId, _folderId);
        //            await RetrieveItemIdAsync();
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"❌ Exception occurred: {ex.Message}");
        //        }
        //    }
        //}


        //private async Task RetrieveItemIdAsync()
        //{
        //    if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_folderId))
        //    {
        //        Console.WriteLine("❌ Cannot retrieve items - project or folder is missing.");
        //        return;
        //    }

        //    try
        //    {
        //        Console.WriteLine($"🔍 Fetching items for Folder: {_folderId}");
        //        var items = await DataManagement.GetItemsInFolder(_selectedProjectId, _folderId);

        //        if (items == null || !items.Any())
        //        {
        //            Console.WriteLine("❌ No items found in the selected folder.");
        //            return;
        //        }

        //        Dispatcher.Invoke(() =>
        //        {
        //            ModelComboBox.Items.Clear();
        //            foreach (var (itemId, itemName) in items)
        //            {
        //                var comboBoxItem = new ComboBoxItem
        //                {
        //                    Content = itemName,
        //                    Tag = itemId
        //                };
        //                ModelComboBox.Items.Add(comboBoxItem);
        //            }
        //        });

        //        Console.WriteLine($"✅ {items.Count} items added to dropdown.");

        //        // 🔹 Step 1: Retrieve storage ID for the selected item
        //        foreach (var (itemId, itemName) in items)
        //        {
        //            FileDownloadService fileDownloadService = new FileDownloadService();
        //            string storageId = await fileDownloadService.GetStorageIdFromItem(_selectedProjectId, itemId);


        //            if (string.IsNullOrEmpty(storageId))
        //            {
        //                Console.WriteLine($"❌ Error: Could not retrieve storage ID for {itemName}.");
        //                continue;
        //            }
        //            Console.WriteLine($"📦 Storage ID for {itemName}: {storageId}");

        //            // 🔹 Step 2: Retrieve versions for the selected item
        //            var versions = await fileDownloadService.GetVersionsForItemAsync(_selectedProjectId, itemId);

        //            if (versions == null || !versions.Any())
        //            {
        //                Console.WriteLine($"❌ No versions found for {itemName}.");
        //                continue;
        //            }

        //            Console.WriteLine($"✅ {versions.Count} versions found for {itemName}.");
        //            foreach (var (versionId, versionName, versionStorageId) in versions)
        //            {
        //                Console.WriteLine($"📄 Version: {versionName}, ID: {versionId}, Storage ID: {versionStorageId}");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error retrieving items: {ex.Message}");
        //    }
        //}


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

        //James Function
        //private async Task<List<string>> GetModelsFromProject(string projectId, string folderId)
        //{
        //    string accessToken = TokenManager.GetToken();
        //    if (string.IsNullOrEmpty(accessToken))
        //    {
        //        Console.WriteLine("❌ No valid access token.");
        //        return null;
        //    }

        //    string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}/contents";

        //    try
        //    {
        //        using (HttpClient client = new HttpClient())
        //        {
        //            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        //            HttpResponseMessage response = await client.GetAsync(url);

        //            if (!response.IsSuccessStatusCode)
        //            {
        //                Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
        //                return null;
        //            }

        //            string jsonResponse = await response.Content.ReadAsStringAsync();
        //            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        //            JsonElement root = doc.RootElement;

        //            List<string> modelNames = new List<string>();

        //            foreach (JsonElement item in root.GetProperty("data").EnumerateArray())
        //            {
        //                if (item.GetProperty("type").GetString() == "items")
        //                {
        //                    string modelName = item.GetProperty("attributes").GetProperty("displayName").GetString();
        //                    modelNames.Add(modelName);
        //                }
        //            }

        //            return modelNames;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Exception occurred: {ex.Message}");
        //        return null;
        //    }
        //}

        private async Task<List<Dictionary<string, string>>> GetModelsFromProject(string projectId, string folderId)
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

                    List<Dictionary<string, string>> models = new List<Dictionary<string, string>>();

                    foreach (JsonElement item in root.GetProperty("data").EnumerateArray())
                    {
                        if (item.GetProperty("type").GetString() == "items")
                        {
                            var attributes = item.GetProperty("attributes");

                            string modelName = attributes.GetProperty("displayName").GetString();
                            string lastModified = attributes.TryGetProperty("lastModifiedTime", out JsonElement modifiedTime) ? modifiedTime.GetString() : "Unknown";

                            models.Add(new Dictionary<string, string>
                            {
                                { "Name", modelName },
                                { "ProjectId", projectId },
                                { "LastModified", lastModified }
                            });
                        }
                    }

                    return models;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return null;
            }
        }


        // ✅ Button Click Event to Refresh Models

        //private void BtnRefreshModels_Click(object sender, RoutedEventArgs e)
        //{
        //    Console.WriteLine("🔄 Refreshing models...");
        //    if (!string.IsNullOrEmpty(_selectedProjectId) && !string.IsNullOrEmpty(_folderId))
        //    {
        //        ListModelsForProject(_selectedProjectId, _folderId);
        //    }
        //    else
        //    {
        //        Console.WriteLine("❌ Error: No project or folder selected.");
        //    }
        //}

        //// ✅ Button Click Event to Log Out
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("👤 Logging out...");

            // Close the current window and show the login screen
            LoginWindow loginWindow = new LoginWindow(true);
            this.Close();
            loginWindow.Show();
        }


        //private async Task ListModelsForProject(string projectId, string folderId)
        //{
        //    if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(folderId))
        //    {
        //        Console.WriteLine("❌ Error: Project ID or Folder ID is missing.");
        //        return;
        //    }

        //    try
        //    {
        //        // 🔹 Fetch models from the project folder
        //        var models = await GetModelsFromProject(projectId, folderId);

        //        if (models != null && models.Any())
        //        {
        //            Dispatcher.Invoke(() =>
        //            {
        //                ModelComboBox.Items.Clear(); // ✅ Clear existing items in dropdown

        //                foreach (var model in models)
        //                {
        //                    ModelComboBox.Items.Add(new ComboBoxItem { Content = model }); // ✅ Add each model name
        //                }
        //            });

        //            Console.WriteLine($"✅ {models.Count} models added to dropdown.");
        //        }
        //        else
        //        {
        //            Console.WriteLine("❌ No models found in the folder.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error retrieving models: {ex.Message}");
        //    }
        //}


        //private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (ModelComboBox.SelectedItem is ComboBoxItem selectedItem)
        //    {
        //        _selectedItemId = selectedItem.Tag as string;
        //        Console.WriteLine($"📌 Selected Item ID: {_selectedItemId}");
        //    }
        //}

        private string GetFilePathFromDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "3D Files|*.stl;*.obj;*.f3d;*.step;*.igs;*.iges;*.sldprt;*.3mf;*.fbx;*.glb;*.gltf|All Files|*.*",
                Title = "Select a 3D Model to Upload"
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }


        private void BtnViewinApp_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ModelsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Border_Enter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = sender as Border;
            var icon = border?.Child as PackIcon;

            if (icon != null)
            {
                icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F25505"));
            }
        }

        private void Border_Leave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = sender as Border;
            var icon = border?.Child as PackIcon;

            if (icon != null)
            {
                icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B4B4B"));
            }
        }

        private void Chevron_Click(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var icon = sender as PackIcon;

            if (icon != null)
            {
                icon.Kind = PackIconKind.ChevronUp;
            }

            var contextMenu = icon.ContextMenu;

            if (contextMenu != null)
            {
                contextMenu.IsOpen = true;
            }
        }

    }
}




//using System.Windows;
//using System.Windows.Controls.Primitives;
//using AssetManager.Infrastructure.Services;
//using Autodesk.Authentication.Model;
//using Microsoft.Win32;

//namespace AssetManager.Desktop
//{
//    public partial class MainWindow : Window
//    {
//        private readonly ModelUpload _uploadService = new ModelUpload();
//        private string projectId = "PROJECT_ID";
//        private string folderId = "FOLDER_ID";

//        public MainWindow(UserInfo userData = null)
//        {
//            InitializeComponent();
//            BtnUploadFile.Click += BtnUploadFile_Click;
//        }

//        private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
//        {
//            Console.WriteLine("Button Clicked");

//            OpenFileDialog openFileDialog = new OpenFileDialog
//            {
//                Title = "Select a Model File",
//                Filter = "All Files (*.*)|*.*"
//            };

//            if (openFileDialog.ShowDialog() == true)
//            {
//                string filePath = openFileDialog.FileName;
//                string accessToken = await GetAccessToken(); // 🔹 Get token

//                try
//                {
//                    string fileUrn = await _uploadService.UploadModel(filePath, projectId, folderId, accessToken);
//                    MessageBox.Show($"✅ Upload Successful!\nFile URN: {fileUrn}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show($"❌ Upload Failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }

//        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
//        {
//            double newWidth = Sidebar.Width + e.HorizontalChange;

//            if (newWidth > 100)
//            {
//                Sidebar.Width = newWidth;
//            }
//        }

//        private async void BtnSwitchLogin_Click(object sender, RoutedEventArgs e)
//        {
//            LoginWindow loginWindow = new LoginWindow();
//            this.Hide();
//            loginWindow.Show();
//        }

//        private Task<string> GetAccessToken()
//        {
//            string token = LoginWindow.TokenManager.GetToken();
//            Console.WriteLine($"🔹 Access Token: {token}");
//            return Task.FromResult(token);
//        }

//    }
//}
