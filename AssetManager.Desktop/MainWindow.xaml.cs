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
using System.Windows.Media;
using AssetManager.Infrastructure.Data;
using System.Windows.Media.Imaging;
using System.Net;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Input;


namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private string _accessToken;
        private Dictionary<string, string> _selectedModel;
        private string _selectedProjectId;
        private string _selectedProjectName;
        private string _selectedItemId;
        private string _selectedItemName;
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

            Username_TextBlock.Text = await GetUserName(_userId);
            UserPic_Image.Source = new BitmapImage(new Uri(await GetUserPic(_userId)));
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

        private bool isModelLoaded = false;
        private async void DisplayGridModels()
        {
            if (isModelLoaded) return;
            isModelLoaded = true;

            ModelsContainer.Children.Clear();
            List<Dictionary<string, string>> models = await GetAllModels();

            foreach (var model in models)
            {
                Border modelSquare = new Border
                {
                    Width = 263,
                    Height = 253,
                    CornerRadius = new CornerRadius(5),
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(10),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        Opacity = 0.1,
                        BlurRadius = 10,
                        ShadowDepth = 2
                    }
                };

                Grid grid = new Grid();

                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(160) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });


                Border headerBackground = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(5)
                };

                Grid.SetRow(headerBackground, 0);
                grid.Children.Add(headerBackground);

                Grid overlayGrid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                StackPanel content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(8, 5, 5, 2)
                };

                TextBlock modelName = new TextBlock
                {
                    Text = model["Name"],
                    FontSize = 16,
                    FontWeight = FontWeights.Normal,
                    Foreground = (Brush) new BrushConverter().ConvertFrom("#4B4B4B"),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    TextWrapping = TextWrapping.Wrap
                };

                TextBlock projectName = new TextBlock
                {
                    Text = model["Project"],
                    FontSize = 14,
                    FontWeight = FontWeights.Normal,
                    Foreground = Brushes.Gray,
                    TextAlignment = TextAlignment.Left,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    TextWrapping = TextWrapping.Wrap
                };

                content.Children.Add(modelName);
                content.Children.Add(projectName);

                Grid.SetRow(content, 1);
                grid.Children.Add(content);

                Border iconBorder = new Border
                {
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 6.5, 2, 0)
                };

                PackIcon icon = new PackIcon
                {
                    Kind = PackIconKind.DotsVertical,
                    Width = 20,
                    Height = 20,
                    Foreground = Brushes.Gray,
                    Cursor = Cursors.Hand
                };

                iconBorder.Child = icon;

                Border iconContainer = new Border
                {
                    Child = iconBorder,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(5)
                };

                overlayGrid.Children.Add(iconContainer);

                Grid parentGrid = new Grid();
                parentGrid.Children.Add(grid);
                parentGrid.Children.Add(overlayGrid);

                modelSquare.Child = parentGrid;
                ModelsContainer.Children.Add(modelSquare);
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
                                    string lastModifiedDate = lastModified.Split('T')[0];
                                    string lastModifiedTime = (lastModified.Split('T')[1]).Remove(8);
                                    lastModified = lastModifiedDate + " " + lastModifiedTime;
                                    
                                    allModels.Add(new Dictionary<string, string>
                                    {
                                        { "Id", modelId },
                                        { "Name", modelName },
                                        { "ProjectId", projectId },
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


        private async void BtnViewInFusion_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItemId == null)
            {
                MessageBox.Show("❌ Please select a model before viewing in Fusion 360.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
        
            string modelFilePath = FindExistingModelFile(_selectedItemName);
        
            if (modelFilePath == null)
            {
                Console.WriteLine("Downloading model");
                // No matching file found, so download it
                var fileDownloadService = new FileDownloadService2();
                fileDownloadService.DownloadModelAsync(_selectedProjectId, _selectedItemId);
        
            }
        
            try
            {
                // Download the model first
                var fileDownloadService = new FileDownloadService2();
                await fileDownloadService.DownloadModelAsync(_selectedProjectId, _selectedItemId);
        
                string saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "DownloadedModels", _selectedItemName);
                Console.WriteLine("Model dir: " + saveDirectory);
        
                if (!Directory.Exists(saveDirectory))
                {
                    LaunchFusionWithModel(saveDirectory);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"❌ Error launching Fusion script: {ex.Message}\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
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

        private string FindExistingModelFile(string modelName)
        {
            string ModelStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DownloadedModels");
                
            var files = Directory.GetFiles(ModelStoragePath);

            foreach (var file in files)
            {
                if (Path.GetFileNameWithoutExtension(file).Equals(modelName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Model found in downloads");
                    return file; // Return the first matching file found
                }
            }

            return null; // No matching file found
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

        private async void BtnDeleteModel_Click(object sender, RoutedEventArgs e)
        {
            string itemId = "urn:adsk.wipprod:dm.item:8IKCVBh3Qg-P8lQuCRewaQ";
            string projectId = _selectedProjectId;
            Console.WriteLine("Attempting to delete id: "+itemId+" from project: "+projectId);
            DeleteModel _delMod = new(); 
            bool isDeleted = await _delMod.DeleteModelAsync(projectId, itemId);
            if (!isDeleted)
            {
                MessageBox.Show("Failed to delete");
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
                            string projectName = _selectedProjectName;
                            
                            models.Add(new Dictionary<string, string>
                            {
                                { "Name", modelName },
                                { "ProjectId", projectId },
                                { "Project", projectName },
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
            if (ModelsDataGrid.SelectedItem is Dictionary<string, string> model)
            {
                _selectedModel = model; // Register the selected model
        
                // Extract model details from the dictionary
                string modelId = model.ContainsKey("Id") ? model["Id"] : "Unknown";
                string modelName = model.ContainsKey("Name") ? model["Name"] : "Unknown";
                string projectId = model.ContainsKey("ProjectId") ? model["ProjectId"] : "Unknown";
                string projectName = model.ContainsKey("Project") ? model["Project"] : "Unknown";
                string lastModified = model.ContainsKey("LastModified") ? model["LastModified"] : "Unknown";
                

                _selectedItemId = modelId;
                _selectedItemName = modelName;
                _selectedProjectId = projectId;
                Console.WriteLine($"Selected Model: {modelName} -{modelId}- from project {projectName} -{projectId}- (Last Modified: {lastModified})");
            }
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

        private void ProjectTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem selectedItem && selectedItem.Tag is ValueTuple<string, string, bool> projectData)
            {
                _selectedProjectId = projectData.Item1;
                _selectedProjectName = selectedItem.Header.ToString();
                _folderId = projectData.Item2;

                LoadModelsForSelectedProject();

                Console.WriteLine($"📌 Clicked Project: {selectedItem.Header}, Project ID: {_selectedProjectId}, Folder ID: {_folderId}");
        
            }
        }
        
        private void ProjectTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProjectTreeView.SelectedItem is TreeViewItem selectedItem)
            {
                string projectName = selectedItem.Header.ToString();
                Console.WriteLine($"Project double-clicked: {projectName}");
            }
        }
        
        private async void LoadModelsForSelectedProject()
        {
            if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_folderId))
            {
                Console.WriteLine("❌ No project selected, cannot load models.");
                ModelsDataGrid.ItemsSource = null;
                return;
            }

            Console.WriteLine($"🔄 Fetching models for Project: {_selectedProjectId}, Folder: {_folderId}");

            var models = await GetModelsFromProject(_selectedProjectId, _folderId);

            if (models == null || !models.Any())
            {
                Console.WriteLine("❌ No models found for this project.");
                ModelsDataGrid.ItemsSource = null;
                return;
            }

            // Update DataGrid with models
            ModelsDataGrid.ItemsSource = models;
        }

        private void ProjectTreeView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not TreeViewItem)
            {
                Console.WriteLine("No project selected, showing all models.");
                _selectedProjectId = null;
                _folderId = null;
                LoadAllModels();
            }
        }
        private async void List_Click(object sender, MouseButtonEventArgs e)
        {
            ModelsDataGrid.Visibility = Visibility.Visible; // Show DataGrid
            Grid_View.Visibility = Visibility.Collapsed;
            isModelLoaded = false;

            if (Models == null || Models.Count == 0)
            {
                Models = await GetAllModels();
                ModelsDataGrid.ItemsSource = Models;
            }

            Grid_Border.Background = Brushes.Transparent;

            //Grid_Icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B4B4B"));

            List_Border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9E9E9"));

            //List_Icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F25505"));
        }

        private async void Grid_Click(object sender, MouseButtonEventArgs e)
        {
            ModelsDataGrid.Visibility = Visibility.Collapsed; // Show DataGrid
            Grid_View.Visibility = Visibility.Visible;

            DisplayGridModels();

            List_Border.Background = Brushes.Transparent;

            //List_Icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B4B4B"));

            Grid_Border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9E9E9"));

            //Grid_Icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F25505"));
        }

    }
}


