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

using System.Text;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Windows.Data; // Fix for Binding issue
using System.Windows.Controls; // Ensure we use WPF DataGrid

using MongoDB.Bson;
using AssetManager.Infrastructure.Models;
using Newtonsoft.Json;

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
        private string selectedHubID;
        private string _folderId;
        private static string hubID;
        private string _objectId;
        private List<(string HubID, string HubName, string HubType)> hubs = new List<(string, string, string)>();
        private CancellationTokenSource _modelLoadCancellationTokenSource = new CancellationTokenSource();
        private readonly ModelUpload _uploadService;
        private readonly FileDownloadService _filedwnService;
        private List<Dictionary<string, string>> Models;
        private string _userId = Environment.GetEnvironmentVariable("userId", EnvironmentVariableTarget.User);
        private static readonly HttpClient client = new HttpClient();
        private string selectedHubName = "Loading..."; // Default value before hubs load
        private bool isModelLoaded = false;
        
        private List<Dictionary<string, string>> originalResults;

        private enum ViewType { Grid, List }
        private ViewType _lastViewType = ViewType.List; // Default to List View


        // ✅ Constructor
        public MainWindow()
        {
            InitializeComponent();
            //InitializeWebView2();
            //  ModelDataGrid.SelectionChanged += ModelDataGrid_SelectionChanged;
            Initialize();
        }
   
        public MainWindow(string userData)
        {
            InitializeComponent();
            _accessToken = TokenManager.GetToken();
            _uploadService = new ModelUpload(_accessToken);
            _filedwnService = new FileDownloadService();
            Initialize();
        }


        private async void Initialize()
        {
            try
            {
                _accessToken = TokenManager.GetToken();

                if (string.IsNullOrEmpty(_accessToken))
                {
                    Console.WriteLine("❌ Error: Access token is missing.");
                    //TokenManager.GetThreeLegged();
                    TokenManager.GetToken();
                }

                Console.WriteLine($"✅ Debug: Retrieved Access Token: {_accessToken}");

                Username_TextBlock.Text = await GetUserName(_userId);
                UserPic_Image.Source = new BitmapImage(new Uri(await GetUserPic(_userId)));

                // 🔹 Initialize data
                LoadHubsAsync();
                await LoadAllModels();

                var hubDetails = await DataManagement.GetPersonalHubDetails();

                if (hubDetails == null)
                {
                    Console.WriteLine("❌ Error: Unable to retrieve hub details. Checking token...");
                    TokenManager.GetToken();
                    hubDetails = await DataManagement.GetPersonalHubDetails();

                    if (hubDetails == null)
                    {
                        Console.WriteLine("❌ Failed to retrieve hub details after token refresh.");
                        return;
                    }
                }

                var (hubID, hubName, hubType) = hubDetails.Value;
                Console.WriteLine($"Hub ID: {hubID}, Name: {hubName}, Type: {hubType}");

                LoadProjectsForHub(hubID);
                await TestDataManagement();

                FusionManager.InitializePythonEngine();

                Username_TextBlock.Text = await GetUserName(_userId);
                UserPic_Image.Source = new BitmapImage(new Uri(await GetUserPic(_userId)));
                //DisplayGridModels();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Initialization error: {ex.Message}");
            }
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
            Image tempImage = new Image();

            Console.WriteLine("\n\nShowing example thumbnail\n\n");
            await ShowThumbnail(projectid, itemid, tempImage);


        }

        private async void LoadHubsAsync()
        {
            try
            {
                hubs.Clear(); // Clear previous hubs
                var hubDetails = await DataManagement.GetAllHubs();

                if (hubDetails != null && hubDetails.Count > 0)
                {
                    hubs.AddRange(hubDetails);

                    // Set the first hub as the default selected hub
                    selectedHubName = hubs[0].HubName;
                    selectedHubID = hubs[0].HubID;

                    // Update UI
                    HubsHeaderTextBlock.Text = $"Hubs - {selectedHubName}";
                }
                else
                {
                    selectedHubName = "No Hubs Found";
                    HubsHeaderTextBlock.Text = $"Hubs - {selectedHubName}";
                }

                PopulateHubMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading hubs: {ex.Message}");
            }
        }

        private async void LoadProjectsForHub(string hubID)
        {
            try
            {
                var projects = await DataManagement.GetAllProjectsFromHub(hubID);
                if (projects == null || !projects.Any())
                {
                    Console.WriteLine("❌ No projects found or failed to load projects.");
                    ProjectTreeView.Items.Clear(); // Clear tree if no projects are found
                    return;
                }

                ProjectTreeView.Items.Clear(); // ✅ Clear previous projects before loading new ones

                foreach (var (projectId, projectName) in projects)
                {
                    var topFolder = await DataManagement.GetTopLevelFolder(hubID, projectId);
                    var folderId = topFolder.Item1;

                    TreeViewItem projectItem = new TreeViewItem
                    {
                        Header = $"📁 {projectName}",
                        Tag = (projectId, folderId, true)
                    };

                    projectItem.Items.Add(null); // Placeholder for lazy loading
                    projectItem.Expanded += TreeViewItem_Expanded;

                    ProjectTreeView.Items.Add(projectItem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading projects: {ex.Message}");
            }
        }


        //NEEDS MIGRATING TO USERSERVICES || USER INFORMATION//
        #region User Services
        private async Task<string> GetUserName(string userId)
        {
            MongoConnection database = new MongoConnection();
            var userData = await database.Users.Find(x => x.Id == userId).FirstOrDefaultAsync();
            if (userData == null)
            {
                return "Unknown User";
            }
            return userData.Username;
        }

        private async Task<string> GetUserPic(string userId)
        {
            MongoConnection database = new MongoConnection();
            var userData = await database.Users.Find(x => x.Id == userId).FirstOrDefaultAsync();
            return userData.ProfilePic;
        }
        #endregion

        //NEEDS MIGRATING TO HUBS SERVICE || HUBS//
        #region HUBS
        private void HubButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string hubID)
            {
                selectedHubName = button.Content.ToString();
                selectedHubID = hubID;

                // Update UI to reflect selected hub
                HubsHeaderTextBlock.Text = $"Hubs - {selectedHubName}";

                HubsMenuPopup.IsOpen = false; // Close the menu after selecting

                LoadProjectsForHub(selectedHubID);
                //GetAllModels();
            }
        }

        private void PopulateHubMenu()
        {
            HubsMenuStackPanel.Children.Clear(); // Clear previous entries

            foreach (var hub in hubs)
            {
                Button hubButton = new Button
                {
                    Content = hub.HubName,
                    Tag = hub.HubID,
                    Padding = new Thickness(5),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                hubButton.Click += HubButton_Click;
                HubsMenuStackPanel.Children.Add(hubButton);
            }

            HubsMenuPopup.StaysOpen = true; // Ensures popup stays open
        }

        private void ToggleHubsMenu(object sender, MouseButtonEventArgs e)
        {
            HubsMenuPopup.IsOpen = !HubsMenuPopup.IsOpen;
        }
        #endregion


        //NEEDS MIGRATING TO MODELSERVICES || MODEL INFORMATION//
        #region Model Services
        private async Task LoadAllModels()
        {
            if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_folderId))
            {
                Console.WriteLine("❌ Error: No project or folder selected.");
                //return;
            }

            try
            {
                Console.WriteLine($"🔄 Loading models for project: {_selectedProjectId}");

                // Fetch models from Autodesk API
                var models = await GetAllModels();//await GetModelsFromProject(_selectedProjectId, _folderId);

                if (models != null && models.Any())
                {
                    Dispatcher.Invoke(() =>
                    {
                        // ✅ FIX: Clear DataGrid BEFORE setting ItemsSource
                        ModelsDataGrid.ItemsSource = null;
                        ModelsDataGrid.Items.Clear();
                        ModelsDataGrid.ItemsSource = models;
                        originalResults = models;
                        Models = models;
                    });

                    Console.WriteLine($"✅ {models.Count} models loaded successfully.");
                }
                else
                {
                    Console.WriteLine("❌ No models found for this project.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading models: {ex.Message}");
            }
        }

        /*        private async void DisplayGridModels()
                {
                    _modelLoadCancellationTokenSource.Cancel();
                    _modelLoadCancellationTokenSource = new CancellationTokenSource();
                    CancellationToken token = _modelLoadCancellationTokenSource.Token;
                    if (isModelLoaded) return; // Prevent duplicate calls
                    isModelLoaded = true;

                    ModelsContainer.Children.Clear(); // Clear existing squares
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

                        StackPanel content = new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Left
                        };

                        TextBlock modelName = new TextBlock
                        {
                            Text = model["Name"],
                            FontSize = 16,
                            FontWeight = FontWeights.Normal,
                            TextAlignment = TextAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(5, 2, 5, 2)
                        };

                        TextBlock projectName = new TextBlock
                        {
                            Text = $"Project: {model["Project"]}",
                            FontSize = 14,
                            FontWeight = FontWeights.Normal,
                            Foreground = Brushes.Gray,
                            TextAlignment = TextAlignment.Left,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(5, 2, 5, 2)
                        };

                        content.Children.Add(modelName);
                        content.Children.Add(projectName);
                        modelSquare.Child = content;
                       // Stop execution if project changes mid-load
                        ModelsContainer.Children.Add(modelSquare);
                       // ModelsContainer.Children.Add(modelSquare);
                        if (token.IsCancellationRequested) return;
                    }
                }*/
        private async void DisplayGridModels()
        {
            _modelLoadCancellationTokenSource.Cancel();
            _modelLoadCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _modelLoadCancellationTokenSource.Token;

            if (isModelLoaded) return;
            isModelLoaded = true;

            ModelsContainer.Children.Clear(); // Clear existing squares
            List<Dictionary<string, string>> models = await GetAllModels();
            if (token.IsCancellationRequested) return;

            foreach (var model in models)
            {
                if (token.IsCancellationRequested) return;

                string modelId = model["Id"];
                string modelName = model["Name"];
                string projectId = _selectedProjectId;

                // Border container
                Border modelSquare = new Border
                {
                    Width = 263,
                    Height = 300,
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

                StackPanel content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                // Thumbnail Image
                Image thumbnailImage = new Image
                {
                    Width = 200,
                    Height = 200,
                    Margin = new Thickness(10),
                    Stretch = Stretch.Uniform
                };
                _ = ShowThumbnail(projectId, modelId, thumbnailImage);

                // Model Name
                TextBlock modelNameBlock = new TextBlock
                {
                    Text = modelName,
                    FontSize = 16,
                    FontWeight = FontWeights.Normal,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5, 2, 5, 2)
                };

                // Three-dot menu button
                Button menuButton = new Button
                {
                    Content = "⋮", // Three-dot icon
                    FontSize = 18,
                    Width = 30,
                    Height = 30,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    Padding = new Thickness(5),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    ToolTip = "More Options"
                };

                // Create and attach ContextMenu
                ContextMenu contextMenu = CreateModelContextMenu(modelId, modelName);
                menuButton.ContextMenu = contextMenu;

                // Ensure the menu opens on button click
                menuButton.Click += (s, e) =>
                {
                    menuButton.ContextMenu.PlacementTarget = menuButton;
                    menuButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    menuButton.ContextMenu.IsOpen = true;
                };

                // Top row container for model name and menu button
                DockPanel topPanel = new DockPanel();
                DockPanel.SetDock(modelNameBlock, Dock.Left);
                DockPanel.SetDock(menuButton, Dock.Right);
                topPanel.Children.Add(modelNameBlock);
                topPanel.Children.Add(menuButton);

                // Add elements to content panel
                content.Children.Add(thumbnailImage);
                content.Children.Add(topPanel);

                modelSquare.Child = content;
                ModelsContainer.Children.Add(modelSquare);
            }
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
                        MessageBox.Show($"error with hub: {hubsResponse.StatusCode} - {hubsResponse.ReasonPhrase}");
                        Console.WriteLine($"❌ Error fetching hubs: {hubsResponse.StatusCode} - {hubsResponse.ReasonPhrase}");
                        return null;
                    }

                    string hubsJson = await hubsResponse.Content.ReadAsStringAsync();
                    using JsonDocument hubsDoc = JsonDocument.Parse(hubsJson);
                    JsonElement hubsRoot = hubsDoc.RootElement;

                    foreach (JsonElement hub in hubsRoot.GetProperty("data").EnumerateArray())
                    {
                        string hubID = hub.GetProperty("id").GetString();
                        hubID = selectedHubID;

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

                                    string modelId = item.GetProperty("id").GetString();
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
                                    await GetModelData(modelId, projectId, projectName);
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

        private async Task FetchAndSetStorageId()
        {
            if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_selectedItemId))
            {
                Console.WriteLine("❌ Cannot retrieve storage ID - project or item is missing.");
                return;
            }

            try
            {
                Console.WriteLine($"🔍 Fetching storage ID for Item: {_selectedItemId} in Project: {_selectedProjectId}");
                FileDownloadService fileService = new FileDownloadService();
                string storageId = await fileService.GetStorageIdFromItem(_selectedProjectId, _selectedItemId);

                if (!string.IsNullOrEmpty(storageId))
                {
                    _objectId = storageId; // Set the global storage ID
                    Console.WriteLine($"✅ Set global storage ID: {_objectId}");

                    // Also update the selected model dictionary if it exists
                    if (_selectedModel != null)
                    {
                        _selectedModel["StorageId"] = storageId;
                    }
                }
                else
                {
                    Console.WriteLine("❌ Error: No valid storage ID found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving storage ID: {ex.Message}");
            }
        }



        public async Task ShowThumbnail(string projectId, string itemId, Image targetImage)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string accessToken = TokenManager.GetToken();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    FileDownloadService _fileService = new FileDownloadService();
                    string objectId = await _fileService.GetStorageIdFromItem(projectId, itemId);
                    if (string.IsNullOrEmpty(objectId))
                    {
                        Console.WriteLine("❌ Could not retrieve object ID.");
                        return;
                    }

                    string encodedUrn = EncodeObjectIdToUrn(objectId);
                    if (string.IsNullOrEmpty(encodedUrn))
                    {
                        Console.WriteLine("❌ Failed to encode object ID.");
                        return;
                    }

                    ModelDerivativeService modelService = new ModelDerivativeService(client);
                    bool isReady = await ModelDerivativeService.IsModelDerivativeReady(encodedUrn);

                    if (!isReady)
                    {
                        bool isCompleted = await modelService.IsTranslationCompletedAsync(encodedUrn, accessToken);
                        if (!isCompleted)
                        {
                            Console.WriteLine("⏳ Model not ready. Requesting translation...");
                            bool translationStarted = await modelService.SubmitModelForTranslationAsync(encodedUrn, accessToken);
                            if (!translationStarted)
                            {
                                Console.WriteLine("❌ Translation failed. Cannot fetch thumbnail.");
                                return;
                            }

                            // Wait for translation to complete
                            int maxRetries = 5;
                            int delayMs = 5000;
                            for (int attempt = 1; attempt <= maxRetries; attempt++)
                            {
                                Console.WriteLine($"⏳ Waiting for translation... (Attempt {attempt}/{maxRetries})");
                                await Task.Delay(delayMs);

                                if (await ModelDerivativeService.IsModelDerivativeReady(encodedUrn))
                                {
                                    Console.WriteLine("✅ Model translation completed!");
                                    break;
                                }

                                if (attempt == maxRetries)
                                {
                                    Console.WriteLine("❌ Model translation failed after multiple attempts.");
                                    return;
                                }
                            }

                        }
                    }

                    // ✅ Fetch the thumbnail **AFTER** translation is ready
                    string thumbnailUrl = await DataManagement.GetLatestItemThumbnail(projectId, itemId, encodedUrn);
                    if (string.IsNullOrEmpty(thumbnailUrl))
                    {
                        Console.WriteLine("❌ Thumbnail URL is null or empty.");
                        return;
                    }

                    // 🛠 Retry thumbnail fetching
                    int thumbnailRetries = 5;
                    int thumbnailDelayMs = 5000;
                    BitmapImage bitmapImage = null; // Define before loop

                    for (int attempt = 1; attempt <= thumbnailRetries; attempt++)
                    {
                        try
                        {
                            Console.WriteLine($"📷 Fetching thumbnail (Attempt {attempt}/{thumbnailRetries}): {thumbnailUrl}");

                            byte[] imageBytes = await client.GetByteArrayAsync(thumbnailUrl);
                            bitmapImage = new BitmapImage();
                            using (MemoryStream stream = new MemoryStream(imageBytes))
                            {
                                bitmapImage.BeginInit();
                                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                bitmapImage.StreamSource = stream;
                                bitmapImage.EndInit();
                            }
                            bitmapImage.Freeze();

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                targetImage.Source = bitmapImage;
                            });

                            Console.WriteLine("✅ Thumbnail loaded successfully!");
                            return; // ✅ Exit loop once successful
                        }
                        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                        {
                            Console.WriteLine($"⚠️ Thumbnail not found, retrying in {thumbnailDelayMs / 1000} seconds...");
                            await Task.Delay(thumbnailDelayMs);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error loading thumbnail: {ex.Message}");
                            return;
                        }
                    }

                    Console.WriteLine("❌ Failed to fetch thumbnail after multiple attempts.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
            }
        }

        public static string EncodeObjectIdToUrn(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
            {
                Console.WriteLine("❌ Error: objectId is null or empty.");
                return null;
            }

            // 🔹 Log the object ID before encoding
            Console.WriteLine($"📦 Encoding Object ID: {objectId}");

            byte[] objectIdBytes = Encoding.UTF8.GetBytes(objectId);
            string base64Urn = Convert.ToBase64String(objectIdBytes)
                .TrimEnd('=')  // Remove padding
                .Replace('+', '-')  // Replace '+' with '-'
                .Replace('/', '_');  // Replace '/' with '_'

            Console.WriteLine($"✅ Encoded URN: {base64Urn}");
            return base64Urn;
        }

        private async Task RetrieveStorageIdForSelectedItem()
        {
            if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_selectedItemId))
            {
                Console.WriteLine("❌ Cannot retrieve storage ID - project or item is missing.");
                return;
            }

            try
            {
                Console.WriteLine($"🔍 Fetching latest storage ID for Item: {_selectedItemId}");
                FileDownloadService fileService = new FileDownloadService();
                string storageId = await fileService.GetStorageIdFromItem(_selectedProjectId, _selectedItemId);

                if (!string.IsNullOrEmpty(storageId))
                {
                    _objectId = storageId; // Store the latest storage ID
                    Console.WriteLine($"✅ Updated Storage ID: {_objectId}");
                }
                else
                {
                    Console.WriteLine("❌ Error: No valid storage ID found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving storage ID: {ex.Message}");
            }
        }


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
                            string itemId = item.GetProperty("id").GetString();
                            string modelName = attributes.GetProperty("displayName").GetString();

                            string lastModified = attributes.TryGetProperty("lastModifiedTime", out JsonElement modifiedTime)
                                ? modifiedTime.GetString()
                                : "Unknown";

                            string formattedDate = "Unknown";
                            if (lastModified != "Unknown")
                            {
                                string[] dateParts = lastModified.Split('T');
                                if (dateParts.Length >= 2)
                                {
                                    string date = dateParts[0];
                                    string time = dateParts[1].Substring(0, Math.Min(8, dateParts[1].Length));
                                    formattedDate = $"{date} {time}";
                                }
                            }

                            // Create model dictionary with all necessary IDs
                            var modelDict = new Dictionary<string, string>
                            {
                                { "Id", itemId },
                                { "Name", modelName },
                                { "ProjectId", projectId },
                                { "FolderId", folderId },
                                { "Project", _selectedProjectName ?? "Unknown Project" },
                                { "LastModified", formattedDate }
                            };

                            // Add to models list
                            models.Add(modelDict);

                            // Pre-fetch storage IDs for each model - but don't block the UI thread
                            _ = Task.Run(async () => {
                                try
                                {
                                    FileDownloadService fileService = new FileDownloadService();
                                    string storageId = await fileService.GetStorageIdFromItem(projectId, itemId);
                                    if (!string.IsNullOrEmpty(storageId))
                                    {
                                        // We need to use Dispatcher to safely update the dictionary from a background thread
                                        Application.Current.Dispatcher.Invoke(() => {
                                            modelDict["StorageId"] = storageId;
                                        });
                                        Console.WriteLine($"✅ Added StorageId for {modelName}: {storageId}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"❌ Failed to fetch storage ID for {modelName}: {ex.Message}");
                                }
                            });
                        }
                    }

                    return models;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        /*       private async void LoadModelsForSelectedProject()
               {
                   if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_folderId))
                   {
                       Console.WriteLine("❌ No project selected, cannot load models.");
                       Dispatcher.Invoke(() =>
                       {
                           ModelsDataGrid.ItemsSource = null;
                           ModelsDataGrid.Items.Clear();
                       });
                       return;
                   }

                   Console.WriteLine($"🔄 Fetching models for Project: {_selectedProjectId}, Folder: {_folderId}");

                   var models = await GetModelsFromProject(_selectedProjectId, _folderId);

                   if (models == null || !models.Any())
                   {
                       Console.WriteLine("❌ No models found for this project.");
                       Dispatcher.Invoke(() =>
                       {
                           ModelsDataGrid.ItemsSource = null;
                           ModelsDataGrid.Items.Clear();
                       });
                       return;
                   }

                   // Log the models we're loading
                   Console.WriteLine($"Found {models.Count} models for project {_selectedProjectId}:");
                   foreach (var model in models)
                   {
                       Console.WriteLine($"- {model["Name"]} (ID: {model["Id"]})");
                   }

                   Dispatcher.Invoke(() =>
                   {
                       ModelsDataGrid.ItemsSource = null;
                       ModelsDataGrid.Items.Clear();
                       ModelsDataGrid.ItemsSource = models;

                       // Auto-select the first item if available
                       if (models.Count > 0)
                       {
                           ModelsDataGrid.SelectedIndex = 0;
                           Console.WriteLine("✅ Auto-selected first model in grid");

                           // The selection changed event will be triggered automatically,
                           // which will set all the global IDs
                       }
                   });

                   Console.WriteLine($"✅ {models.Count} models loaded successfully.");
               }*/

        private async void LoadModelsForSelectedProject()
        {
            if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_folderId))
            {
                Console.WriteLine("❌ No project selected, cannot load models.");
                Dispatcher.Invoke(() =>
                {
                    ModelsDataGrid.ItemsSource = null;
                    ModelsDataGrid.Items.Clear();
                });
                return;
            }

            Console.WriteLine($"🔄 Fetching models for Project: {_selectedProjectId}, Folder: {_folderId}");

            var models = await GetModelsFromProject(_selectedProjectId, _folderId);

            if (models == null || !models.Any())
            {
                Console.WriteLine("❌ No models found for this project.");
                Dispatcher.Invoke(() =>
                {
                    ModelsDataGrid.ItemsSource = null;
                    ModelsDataGrid.Items.Clear();
                });
                return;
            }

            // Log the models we're loading
            Console.WriteLine($"Found {models.Count} models for project {_selectedProjectId}:");
            foreach (var model in models)
            {
                Console.WriteLine($"- {model["Name"]} (ID: {model["Id"]})");
            }

            Dispatcher.Invoke(() =>
            {
                ModelsDataGrid.ItemsSource = null;
                ModelsDataGrid.Items.Clear();
                ModelsDataGrid.ItemsSource = models;
                originalResults = models;

                // Clear existing columns
                ModelsDataGrid.Columns.Clear();

                // Normal Data Columns
                ModelsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Name",
                    Binding = new Binding("Name")
                });
                ModelsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Project",
                    Binding = new Binding("Project")
                });
                ModelsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
                {
                    Header = "Last Modified",
                    Binding = new Binding("LastModified")
                });

                // ✅ Add Three-Dot Menu Column
                DataGridTemplateColumn menuColumn = new DataGridTemplateColumn { Header = "Poo", Width = 50 };
                FrameworkElementFactory menuButtonFactory = new FrameworkElementFactory(typeof(Button));
                menuButtonFactory.SetValue(Button.ContentProperty, "⋮");
                menuButtonFactory.SetValue(Button.WidthProperty, 30.0);
                menuButtonFactory.SetValue(Button.HeightProperty, 30.0);
                menuButtonFactory.SetValue(Button.ToolTipProperty, "More Options");
                menuButtonFactory.SetValue(Button.BackgroundProperty, Brushes.Transparent);
                menuButtonFactory.SetValue(Button.BorderBrushProperty, Brushes.Transparent);

                // Attach event handler to open context menu
                menuButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) =>
                {
                    Button button = s as Button;
                    if (button?.DataContext is Dictionary<string, string> model)
                    {
                        ContextMenu contextMenu = CreateModelContextMenu(model["Id"], model["Name"]);
                        contextMenu.PlacementTarget = button;
                        contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                        contextMenu.IsOpen = true;
                    }
                }));

                DataTemplate menuTemplate = new DataTemplate { VisualTree = menuButtonFactory };
                menuColumn.CellTemplate = menuTemplate;
                ModelsDataGrid.Columns.Add(menuColumn);
            });

            Console.WriteLine($"✅ {models.Count} models loaded successfully.");
        }




        #endregion

        //NEEDS MIGRATING TO UI SERVICES || UI FUNCTIONALITY//
        #region UI Functionality
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
        #endregion

        //UI BUTTONS//
        #region UI Buttons
        private void ProjectTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem selectedItem && selectedItem.Tag is ValueTuple<string, string, bool> projectData)
            {
                _selectedProjectId = projectData.Item1;
                _folderId = projectData.Item2;

                // Extract the project name from the header (remove the 📁 emoji if present)
                string header = selectedItem.Header.ToString();
                _selectedProjectName = header.StartsWith("📁 ") ? header.Substring(3) : header;

                Console.WriteLine($"📌 Selected Project: {_selectedProjectName}, Project ID: {_selectedProjectId}, Folder ID: {_folderId}");

                // Reset model loading flag and clear UI before loading new models
                isModelLoaded = false;
                ModelsContainer.Children.Clear();

                // Automatically refresh the grid view if it's currently visible
                if (Grid_View.Visibility == Visibility.Visible)
                {
                    Grid_Click(null, null);  // Refresh grid view with the new project
                }
                else if (ModelsDataGrid.Visibility == Visibility.Visible)
                {
                    List_Click(null, null);  // Refresh List View
                }
                else
                {
                    // Otherwise, load models normally
                    LoadModelsForSelectedProject();
                }
            }
        }



        private async void ModelsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelsDataGrid.SelectedItem is Dictionary<string, string> model)
            {
                _selectedModel = model;

                // Debug - print what we're actually selecting
                Console.WriteLine("Selection changed - Selected item data:");
                foreach (var kvp in model)
                {
                    Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
                }

                // Extract and set all relevant IDs in one place
                if (model.TryGetValue("Id", out string modelId) || model.TryGetValue("id", out modelId))
                {
                    _selectedItemId = modelId;
                    Console.WriteLine($"✅ Set selected item ID: {_selectedItemId}");
                }
                else
                {
                    _selectedItemId = null;
                    Console.WriteLine("❌ Model ID missing in selection.");
                }

                // Set the name and related project properties
                _selectedItemName = model.ContainsKey("Name") ? model["Name"] :
                                   (model.ContainsKey("name") ? model["name"] : "Unknown");

                // Set the project ID - either from the model or use the currently selected one
                if (model.TryGetValue("ProjectId", out string projectId) || model.TryGetValue("projectId", out projectId))
                {
                    _selectedProjectId = projectId;
                    Console.WriteLine($"✅ Set selected project ID: {_selectedProjectId}");
                }
                // Don't reset the project ID if it's not in the model - keep the current value

                _selectedProjectName = model.ContainsKey("Project") ? model["Project"] :
                                      (model.ContainsKey("project") ? model["project"] : _selectedProjectName);

                Console.WriteLine($"✅ Selected Model: {_selectedItemName} (ID: {_selectedItemId}, Project ID: {_selectedProjectId})");

                // Also immediately fetch and set the storage ID for the selected item
                await FetchAndSetStorageId();
                
                //display upvotes
                int upvotes = await GetModelUpvoteCount(model["Id"]);
                        
                await SetUserModelVote(_selectedItemId, _userId);
                int vote = await GetUserModelVote(_selectedItemId, _userId);
                if (vote == 1)
                {
                    UpArrow.Kind = PackIconKind.ArrowTopBold;
                    UpArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#11d137"));
                    DownArrow.Kind = PackIconKind.ArrowDownBoldOutline;
                    DownArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B4B4B"));
                }
                else if (vote == -1)
                {
                    DownArrow.Kind = PackIconKind.ArrowDownBold;
                    DownArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d11111"));
                    UpArrow.Kind = PackIconKind.ArrowTopBoldOutline;
                    UpArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B4B4B"));
                }
                else
                {
                    UpArrow.Kind = PackIconKind.ArrowTopBoldOutline;
                    UpArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B4B4B"));
                    DownArrow.Kind = PackIconKind.ArrowDownBoldOutline;
                    DownArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B4B4B"));
                }
                        
                UpvoteTextBlock.Text = upvotes.ToString();
                UpArrowBorder.Visibility = Visibility.Visible;
                UpvoteTextBlock.Visibility = Visibility.Visible;
                DownArrowBorder.Visibility = Visibility.Visible;
            }
            else if (ModelsDataGrid.SelectedItem != null)
            {
                // If it's not a Dictionary<string, string>, log what it actually is
                Console.WriteLine($"❌ Selected item is not a Dictionary<string, string> but a {ModelsDataGrid.SelectedItem.GetType().Name}");
            }
            else
            {
                Console.WriteLine("❌ No item selected in DataGrid");
            }
        }

        private void Border_Enter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = sender as Border;
            var icon = border?.Child as PackIcon;

            if (icon.Kind.ToString() == "ArrowTopBoldOutline")
            {
                icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#11d137"));
            }
            else if (icon.Kind.ToString() == "ArrowDownBoldOutline")
            {
                icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d11111"));
            }
            else if (icon != null && icon.Kind != PackIconKind.ArrowTopBold && icon.Kind != PackIconKind.ArrowDownBold)
            {
                icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F25505"));
            }
        }

        private void Border_Leave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = sender as Border;
            var icon = border?.Child as PackIcon;

            if (icon != null && icon.Kind != PackIconKind.ArrowTopBold && icon.Kind != PackIconKind.ArrowDownBold)
            {
                icon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B4B4B"));
            }
        }

        private bool isDropdownOpen = false;

        private void Chevron_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (ChevronDownClick.ContextMenu != null)
            {
                if (!isDropdownOpen)
                {
                    ChevronDownClick.ContextMenu.PlacementTarget = ChevronDownClick;
                    ChevronDownClick.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    ChevronDownClick.ContextMenu.IsOpen = true;
                    ChevronDownClick.Kind = PackIconKind.ChevronUp;
                    isDropdownOpen = true;
                }
                else
                {
                    ChevronDownClick.ContextMenu.IsOpen = false;
                    ChevronDownClick.Kind = PackIconKind.ChevronDown;
                    isDropdownOpen = false;
                }
            }

            User_Grid.Cursor = Cursors.Hand;
        }

        private void ProjectTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProjectTreeView.SelectedItem is TreeViewItem selectedItem)
            {
                string projectName = selectedItem.Header.ToString();
                Console.WriteLine($"Project double-clicked: {projectName}");
            }
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

        private ContextMenu CreateModelContextMenu(string modelId, string modelName)
        {
            ContextMenu menu = new ContextMenu();

            MenuItem openInFusionItem = new MenuItem { Header = "🔷 Open in Fusion 360" };
            openInFusionItem.Click += (s, e) =>
            {
                if ((s as MenuItem)?.DataContext is Dictionary<string, string> model)
                {
                    BtnViewInFusion_Click(model["Id"]);
                }
            };

            MenuItem viewInAppItem = new MenuItem { Header = "🖥️ View in App" };
            viewInAppItem.Click += (s, e) =>
            {
                if ((s as MenuItem)?.DataContext is Dictionary<string, string> model)
                {
                    BtnViewInApp_Click(model["Id"]);
                }
            };

            MenuItem downloadItem = new MenuItem { Header = "📥 Download" };
            downloadItem.Click += (s, e) =>
            {
                if ((s as MenuItem)?.DataContext is Dictionary<string, string> model)
                {
                    BtnDownload_Click(model["Id"]);
                }
            };

            MenuItem deleteItem = new MenuItem { Header = "🗑️ Delete" };
            deleteItem.Click += (s, e) =>
            {
                if ((s as MenuItem)?.DataContext is Dictionary<string, string> model)
                {
                    BtnDeleteModel_Click(model["Id"]);
                }
            };

            menu.Items.Add(openInFusionItem);
            menu.Items.Add(viewInAppItem);
            menu.Items.Add(downloadItem);
            menu.Items.Add(deleteItem);

            return menu;
        }
        private async void List_Click(object sender, MouseButtonEventArgs e)
        {
            ModelsDataGrid.Visibility = Visibility.Visible; // Show DataGrid
            Grid_View.Visibility = Visibility.Collapsed;
            isModelLoaded = false;

            try
            {
                ModelsDataGrid.ItemsSource = null; // Clear previous data

                // Fetch models for the selected project
                List<Dictionary<string, string>> models = await GetModelsFromProject(_selectedProjectId, _folderId);

                if (models == null || models.Count == 0)
                {
                    Console.WriteLine("🔄 No models found, clearing grid.");
                    ModelsDataGrid.ItemsSource = null;
                    return;
                }

                ModelsDataGrid.ItemsSource = models;
                Console.WriteLine($"✅ Loaded {models.Count} models.");

                // ✅ Ensure "Actions" column exists only once
                if (!ModelsDataGrid.Columns.Any(col => col.Header?.ToString() == "Actions"))
                {
                    var actionsColumn = new DataGridTemplateColumn
                    {
                        Header = "Actions",
                        Width = new DataGridLength(50)
                    };

                    var cellTemplate = new DataTemplate();
                    var buttonFactory = new FrameworkElementFactory(typeof(Button));

                    buttonFactory.SetValue(Button.ContentProperty, "⋮"); // Three-dot menu
                    buttonFactory.SetValue(Button.CursorProperty, Cursors.Hand);
                    buttonFactory.SetValue(Button.ToolTipProperty, "Click for options");

                    // ✅ Open ContextMenu when button is clicked
                    buttonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, ev) =>
                    {
                        if (s is Button btn && btn.DataContext is Dictionary<string, string> selectedModel)
                        {
                            ContextMenu dynamicContextMenu = CreateModelContextMenu(selectedModel["Id"], selectedModel["Name"]);
                            dynamicContextMenu.PlacementTarget = btn;
                            dynamicContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                            dynamicContextMenu.IsOpen = true;
                        }
                    }));

                    cellTemplate.VisualTree = buttonFactory;
                    actionsColumn.CellTemplate = cellTemplate;

                    ModelsDataGrid.Columns.Add(actionsColumn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error loading models: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Grid_Border.Background = Brushes.Transparent;
            List_Border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9E9E9"));
        }



        private async void Grid_Click(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedProjectId))
            {
                MessageBox.Show("❌ Please select a project to view models.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                CreateGridView(Models);
                ModelsDataGrid.Visibility = Visibility.Collapsed; // Hide DataGrid
                Grid_View.Visibility = Visibility.Visible; // Show Grid View
                return;
            }

            ModelsDataGrid.Visibility = Visibility.Collapsed; // Hide DataGrid
            Grid_View.Visibility = Visibility.Visible; // Show Grid View

            // Clear previous grid data
            ModelsContainer.Children.Clear();

            try
            {
                // Fetch models for the selected project only
                List<Dictionary<string, string>> models = await GetModelsFromProject(_selectedProjectId, _folderId);

                if (models == null || models.Count == 0)
                {
                    MessageBox.Show("No models found for this project.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                foreach (var model in models)
                {
                    string projectId = _selectedProjectId;
                    string itemId = model["Id"];

                    // UI Container for Model
                    Border modelSquare = new Border
                    {
                        Width = 263,
                        Height = 300, // Increased to fit the image
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

                    StackPanel content = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    // Thumbnail Image
                    Image thumbnailImage = new Image
                    {
                        Width = 200,
                        Height = 200,
                        Margin = new Thickness(10),
                        Stretch = Stretch.Uniform
                    };

                    // Load thumbnail asynchronously
                    _ = ShowThumbnail(projectId, itemId, thumbnailImage);

                    TextBlock modelName = new TextBlock
                    {
                        Text = model["Name"],
                        FontSize = 16,
                        FontWeight = FontWeights.Normal,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(5, 2, 5, 2)
                    };

                    // ✅ Three-dot menu button
                    Button menuButton = new Button
                    {
                        Content = "⋮", // Three-dot icon
                        FontSize = 18,
                        Width = 30,
                        Height = 30,
                        Background = Brushes.Transparent,
                        BorderBrush = Brushes.Transparent,
                        Padding = new Thickness(5),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        ToolTip = "More Options"
                    };

                    // ✅ Ensure the menu button has the correct model assigned
                    menuButton.DataContext = model;

                    // ✅ Create and attach ContextMenu
                    ContextMenu contextMenu = CreateModelContextMenu(model["Id"], model["Name"]);
                    menuButton.ContextMenu = contextMenu;

                    // ✅ Ensure the menu opens on button click
                    menuButton.Click += (s, e) =>
                    {
                        if (s is Button btn && btn.DataContext is Dictionary<string, string> selectedModel)
                        {
                            ContextMenu dynamicContextMenu = CreateModelContextMenu(selectedModel["Id"], selectedModel["Name"]);
                            dynamicContextMenu.PlacementTarget = btn;
                            dynamicContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                            dynamicContextMenu.IsOpen = true;
                        }
                    };

                    // Container for Model Name + Menu Button
                    DockPanel topPanel = new DockPanel();
                    DockPanel.SetDock(modelName, Dock.Left);
                    DockPanel.SetDock(menuButton, Dock.Right);
                    topPanel.Children.Add(modelName);
                    topPanel.Children.Add(menuButton);

                    // Add elements to content panel
                    content.Children.Add(thumbnailImage);
                    content.Children.Add(topPanel);

                    modelSquare.Child = content;
                    ModelsContainer.Children.Add(modelSquare);
                }

                Console.WriteLine($"✅ {models.Count} models loaded successfully in grid view.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error loading models: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Update UI styles to reflect active view mode
            List_Border.Background = Brushes.Transparent;
            Grid_Border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9E9E9"));
        }

        private void CreateGridView(List<Dictionary<string, string>> models)
        {
            foreach (var model in models)
            {
                // UI Container for Model
                    Border modelSquare = new Border
                    {
                        Width = 263,
                        Height = 300, // Increased to fit the image
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

                    StackPanel content = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    // Thumbnail Image
                    Image thumbnailImage = new Image
                    {
                        Width = 200,
                        Height = 200,
                        Margin = new Thickness(10),
                        Stretch = Stretch.Uniform
                    };

                    // Load thumbnail asynchronously
                    _ = ShowThumbnail(model["ProjectId"], model["Id"], thumbnailImage);

                    TextBlock modelName = new TextBlock
                    {
                        Text = model["Name"],
                        FontSize = 16,
                        FontWeight = FontWeights.Normal,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(5, 2, 5, 2)
                    };

                    // ✅ Three-dot menu button
                    Button menuButton = new Button
                    {
                        Content = "⋮", // Three-dot icon
                        FontSize = 18,
                        Width = 30,
                        Height = 30,
                        Background = Brushes.Transparent,
                        BorderBrush = Brushes.Transparent,
                        Padding = new Thickness(5),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        ToolTip = "More Options"
                    };

                    // ✅ Ensure the menu button has the correct model assigned
                    menuButton.DataContext = model;

                    // ✅ Create and attach ContextMenu
                    ContextMenu contextMenu = CreateModelContextMenu(model["Id"], model["Name"]);
                    menuButton.ContextMenu = contextMenu;

                    // ✅ Ensure the menu opens on button click
                    menuButton.Click += (s, e) =>
                    {
                        if (s is Button btn && btn.DataContext is Dictionary<string, string> selectedModel)
                        {
                            ContextMenu dynamicContextMenu = CreateModelContextMenu(selectedModel["Id"], selectedModel["Name"]);
                            dynamicContextMenu.PlacementTarget = btn;
                            dynamicContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                            dynamicContextMenu.IsOpen = true;
                        }
                    };

                    // Container for Model Name + Menu Button
                    DockPanel topPanel = new DockPanel();
                    DockPanel.SetDock(modelName, Dock.Left);
                    DockPanel.SetDock(menuButton, Dock.Right);
                    topPanel.Children.Add(modelName);
                    topPanel.Children.Add(menuButton);

                    // Add elements to content panel
                    content.Children.Add(thumbnailImage);
                    content.Children.Add(topPanel);

                    modelSquare.Child = content;
                    ModelsContainer.Children.Add(modelSquare);
            }
        }

/*
        private void SetupDataGrid()
        {
            if (ModelsDataGrid.Columns.Count > 0)
            {
                Console.WriteLine("🔄 DataGrid already set up, skipping redundant setup.");
                return;
            }

            Console.WriteLine("🛠️ Setting up DataGrid...");

            ModelsDataGrid.Columns.Clear(); // Clear previous setup
            ModelsDataGrid.AutoGenerateColumns = false;

            ModelsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Name",
                Binding = new Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            ModelsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Project",
                Binding = new Binding("Project"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            ModelsDataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn
            {
                Header = "Last Modified",
                Binding = new Binding("LastModified"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            // Ensure Actions Column is only added once
            if (!ModelsDataGrid.Columns.Any(col => col.Header?.ToString() == "Actions"))
            {
                var actionsColumn = new DataGridTemplateColumn
                {
                    Header = "Actions",
                    Width = new DataGridLength(50)
                };

                var cellTemplate = new DataTemplate();
                var buttonFactory = new FrameworkElementFactory(typeof(Button));

                buttonFactory.SetValue(Button.ContentProperty, "⋮"); // Three dots
                buttonFactory.SetValue(Button.CursorProperty, Cursors.Hand);
                buttonFactory.SetValue(Button.ToolTipProperty, "Click for options");

                // Make sure the button opens the ContextMenu dynamically
                buttonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) =>
                {
                    if (s is Button btn && btn.DataContext is Dictionary<string, string> model)
                    {
                        ContextMenu dynamicContextMenu = CreateModelContextMenu(model["Id"], model["Name"]);
                        dynamicContextMenu.PlacementTarget = btn;
                        dynamicContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                        dynamicContextMenu.IsOpen = true;
                    }
                }));

                cellTemplate.VisualTree = buttonFactory;
                actionsColumn.CellTemplate = cellTemplate;

                ModelsDataGrid.Columns.Add(actionsColumn);
            }

            // ✅ Attach ContextMenu to **entire DataGridRow**
            ModelsDataGrid.RowStyle = new Style(typeof(DataGridRow))
            {
                Setters =
        {
            new EventSetter(DataGridRow.LoadedEvent, new RoutedEventHandler((s, e) =>
            {
                if (s is DataGridRow row && row.DataContext is Dictionary<string, string> model)
                {
                    // Ensure each row gets the correct menu dynamically
                    row.ContextMenu = CreateModelContextMenu(model["Id"], model["Name"]);
                }
            }))
        }
            };

            Console.WriteLine("✅ DataGrid setup complete.");
        }

*/

        #endregion

        //NEEDS MIGRATING TO FUSION SERVICES || OPEN WITH FUSION FUNCTIONALITY //
        #region Fusion Functionality
        private async void LaunchFusionWithModel()
        {
            if (string.IsNullOrEmpty(_selectedProjectId) || string.IsNullOrEmpty(_selectedItemId))
            {
                MessageBox.Show("❌ No model selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Fetch the latest storage ID to ensure we open the correct cloud version
                string storageId = await _filedwnService.GetStorageIdFromItem(_selectedProjectId, _selectedItemId);

                if (string.IsNullOrEmpty(storageId))
                {
                    MessageBox.Show("❌ Could not retrieve storage ID for the model.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Construct the Fusion 360 command URL
                string fusionUri = $"fusion360://command=openCloudModel&projectId={_selectedProjectId}&itemId={_selectedItemId}&storage={storageId}";

                Console.WriteLine($"✅ Launching Fusion 360 with model: {fusionUri}");

                // Open Fusion 360 with the correct model reference
                Process.Start(new ProcessStartInfo
                {
                    FileName = fusionUri,
                    UseShellExecute = true
                });
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

        private string GetFilePathFromDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "3D Files|*.stl;*.obj;*.f3d;*.step;*.igs;*.iges;*.sldprt;*.3mf;*.fbx;*.glb;*.gltf|All Files|*.*",
                Title = "Select a 3D Model to Upload"
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        #endregion

        //SERVICE BUTTONS//
        #region Service Buttons
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
            Console.WriteLine("Attempting to delete id: " + itemId + " from project: " + projectId);
            DeleteModel _delMod = new();
            bool isDeleted = await _delMod.DeleteModelAsync(projectId, itemId);
            if (!isDeleted)
            {
                MessageBox.Show("Failed to delete");
            }
        }

        private async void BtnDeleteModel_Click(string selectedItemId)
        {
            string itemId = "urn:adsk.wipprod:dm.item:8IKCVBh3Qg-P8lQuCRewaQ";
            string projectId = _selectedProjectId;
            Console.WriteLine("Attempting to delete id: " + itemId + " from project: " + projectId);
            DeleteModel _delMod = new();
            bool isDeleted = await _delMod.DeleteModelAsync(projectId, itemId);
            if (!isDeleted)
            {
                MessageBox.Show("Failed to delete");
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("👤 Logging out...");

            // Close the current window and show the login screen
            LoginWindow loginWindow = new LoginWindow(true);
            this.Close();
            loginWindow.Show();
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Downloading Model");
            FileDownloadService fileDownloadService = new FileDownloadService();
            fileDownloadService.DownloadModelAsync(_selectedProjectId, _selectedItemId);
        }
        private void BtnDownload_Click(string selectedItemId)
        {
            Console.WriteLine("Downloading Model");
            FileDownloadService fileDownloadService = new FileDownloadService();
            fileDownloadService.DownloadModelAsync(_selectedProjectId, _selectedItemId);
        }
        private async void BtnViewInFusion_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine($"View in Fusion button clicked. Using global IDs:");
            Console.WriteLine($"- Selected Item ID: {_selectedItemId}");
            Console.WriteLine($"- Selected Project ID: {_selectedProjectId}");
            Console.WriteLine($"- Selected Item Name: {_selectedItemName}");

            if (string.IsNullOrEmpty(_selectedItemId) || string.IsNullOrEmpty(_selectedProjectId))
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
                var fileDownloadService = new FileDownloadService();
                await fileDownloadService.DownloadModelAsync(_selectedProjectId, _selectedItemId);
            }

            try
            {
                string saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "DownloadedModels", _selectedItemName);
                Console.WriteLine("Model dir: " + saveDirectory);

                if (!Directory.Exists(saveDirectory))
                {
                   // LaunchFusionWithModel(saveDirectory);
                }
                else
                {
                    // Directory exists, so we can just launch Fusion with the model
                    //LaunchFusionWithModel(saveDirectory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error launching Fusion script: {ex.Message}\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void BtnViewInFusion_Click(string selectedItemId)
        {
            Console.WriteLine($"View in Fusion button clicked. Using global IDs:");
            Console.WriteLine($"- Selected Item ID: {_selectedItemId}");
            Console.WriteLine($"- Selected Project ID: {_selectedProjectId}");
            Console.WriteLine($"- Selected Item Name: {_selectedItemName}");

            if (string.IsNullOrEmpty(_selectedItemId) || string.IsNullOrEmpty(_selectedProjectId))
            {
                MessageBox.Show("❌ Please select a model before viewing in Fusion 360.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Fetch the latest storage ID from Autodesk API

                string storageId = await _filedwnService.GetStorageIdFromItem(_selectedProjectId, _selectedItemId);

                if (string.IsNullOrEmpty(storageId))
                {
                    MessageBox.Show("❌ Could not retrieve storage ID for the model.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Construct the Fusion 360 command URI
                string fusionUri = $"fusion360://command=openCloudModel&projectId={_selectedProjectId}&itemId={_selectedItemId}&storage={storageId}";

                Console.WriteLine($"✅ Launching Fusion 360 with model: {fusionUri}");

                // Open Fusion 360 with the correct model reference
                Process.Start(new ProcessStartInfo
                {
                    FileName = fusionUri,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error launching Fusion 360: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        //FORGE VIEWER 
        #region Forge Viewer
        private async void BtnViewInApp_Click(object sender, RoutedEventArgs e)
        {
            ModelsDataGrid.Visibility = Visibility.Collapsed;
            Grid_View.Visibility = Visibility.Collapsed;

            // Show Forge Viewer
            ForgeViewerContainer.Visibility = Visibility.Visible;
            ForgeWebView.Visibility = Visibility.Visible;

            Console.WriteLine($"View in App button clicked. Using global IDs:");
            Console.WriteLine($"- Selected Item ID: {_selectedItemId}");
            Console.WriteLine($"- Selected Project ID: {_selectedProjectId}");
            Console.WriteLine($"- Storage ID: {_objectId}");

            if (string.IsNullOrEmpty(_selectedItemId) || string.IsNullOrEmpty(_selectedProjectId))
            {
                MessageBox.Show("❌ Please select a model before viewing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // If we already have the storage ID, use it directly
            string objectId = _objectId;

            // If not available, fetch it on demand
            if (string.IsNullOrEmpty(objectId))
            {
                Console.WriteLine("🔍 Storage ID not set globally, fetching now...");
                FileDownloadService fileService = new FileDownloadService();
                objectId = await fileService.GetStorageIdFromItem(_selectedProjectId, _selectedItemId);
                if (string.IsNullOrEmpty(objectId))
                {
                    MessageBox.Show("Could not retrieve model information.", "Model Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _objectId = objectId; // Update global storage ID
            }

            // Encode URN
            string encodedUrn = EncodeObjectIdToUrn(objectId);
            if (string.IsNullOrEmpty(encodedUrn))
            {
                MessageBox.Show("Failed to process model identifier.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Console.WriteLine($"✅ Encoded URN: {encodedUrn}");

            // Continue with model viewer loading...
            ModelDerivativeService modelService = new ModelDerivativeService(new HttpClient());
            bool translationComplete = await modelService.IsTranslationCompletedAsync(encodedUrn, _accessToken);

            if (!translationComplete)
            {
                Console.WriteLine("🔄 Submitting translation job...");
                bool jobResponse = await modelService.SubmitModelForTranslationAsync(encodedUrn, _accessToken);

                Console.WriteLine($"✅ Translation job submitted: {jobResponse}");

                if (!jobResponse)
                {
                    MessageBox.Show("❌ Translation job failed to submit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            Console.WriteLine($"🔍 Opening Forge Viewer for Model: {_selectedItemId}");

            // Open Forge Viewer in a new window
            //ForgeViewerWindow forgeViewer = new ForgeViewerWindow(encodedUrn);
            // forgeViewer.Show();
            LoadForgeViewer(encodedUrn);
        }

        private async void BtnViewInApp_Click(string selectedItemId)
        {
            // ✅ Track the last active view
            if (ModelsDataGrid.Visibility == Visibility.Visible)
            {
                _lastViewType = ViewType.List;
            }
            else if (Grid_View.Visibility == Visibility.Visible)
            {
                _lastViewType = ViewType.Grid;
            }

            // Hide both views
            ModelsDataGrid.Visibility = Visibility.Collapsed;
            Grid_View.Visibility = Visibility.Collapsed;

            // Show the Forge Viewer
            ForgeViewerContainer.Visibility = Visibility.Visible;
            ForgeWebView.Visibility = Visibility.Visible;

            Console.WriteLine($"View in App button clicked. Returning to: {_lastViewType} view after closing.");

            ModelsDataGrid.Visibility = Visibility.Collapsed;
            Grid_View.Visibility = Visibility.Collapsed;

            // Show Forge Viewer
            ForgeViewerContainer.Visibility = Visibility.Visible;
            ForgeWebView.Visibility = Visibility.Visible;

            Console.WriteLine($"View in App button clicked. Using global IDs:");
            Console.WriteLine($"- Selected Item ID: {_selectedItemId}");
            Console.WriteLine($"- Selected Project ID: {_selectedProjectId}");
            Console.WriteLine($"- Storage ID: {_objectId}");

            if (string.IsNullOrEmpty(_selectedItemId) || string.IsNullOrEmpty(_selectedProjectId))
            {
                MessageBox.Show("❌ Please select a model before viewing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // If we already have the storage ID, use it directly
            string objectId = _objectId;

            // If not available, fetch it on demand
            if (string.IsNullOrEmpty(objectId))
            {
                Console.WriteLine("🔍 Storage ID not set globally, fetching now...");
                FileDownloadService fileService = new FileDownloadService();
                objectId = await fileService.GetStorageIdFromItem(_selectedProjectId, _selectedItemId);
                if (string.IsNullOrEmpty(objectId))
                {
                    MessageBox.Show("Could not retrieve model information.", "Model Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _objectId = objectId; // Update global storage ID
            }

            // Encode URN
            string encodedUrn = EncodeObjectIdToUrn(objectId);
            if (string.IsNullOrEmpty(encodedUrn))
            {
                MessageBox.Show("Failed to process model identifier.", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Console.WriteLine($"✅ Encoded URN: {encodedUrn}");

            // Continue with model viewer loading...
            ModelDerivativeService modelService = new ModelDerivativeService(new HttpClient());
            bool translationComplete = await modelService.IsTranslationCompletedAsync(encodedUrn, _accessToken);

            if (!translationComplete)
            {
                Console.WriteLine("🔄 Submitting translation job...");
                bool jobResponse = await modelService.SubmitModelForTranslationAsync(encodedUrn, _accessToken);

                Console.WriteLine($"✅ Translation job submitted: {jobResponse}");

                if (!jobResponse)
                {
                    MessageBox.Show("❌ Translation job failed to submit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            Console.WriteLine($"🔍 Opening Forge Viewer for Model: {_selectedItemId}");

            // Open Forge Viewer in a new window
            //ForgeViewerWindow forgeViewer = new ForgeViewerWindow(encodedUrn);
            // forgeViewer.Show();
            LoadForgeViewer(encodedUrn);
        }

        private void Btn_CloseViewer_Click(object sender, RoutedEventArgs e)
        {
            // Hide the Forge Viewer
            ForgeViewerContainer.Visibility = Visibility.Collapsed;
            ForgeWebView.Visibility = Visibility.Collapsed;

            // ✅ Restore the last active view
            if (_lastViewType == ViewType.Grid)
            {
                ModelsDataGrid.Visibility = Visibility.Collapsed;
                Grid_View.Visibility = Visibility.Visible;
            }
            else
            {
                ModelsDataGrid.Visibility = Visibility.Visible;
                Grid_View.Visibility = Visibility.Collapsed;
            }

            Console.WriteLine($"🔄 Returning to {_lastViewType} view.");
        }


        private async void LoadForgeViewer(string encodedUrn)
        {
            try
            {
                ForgeWebView.Visibility = Visibility.Visible;

                // Initialize WebView2
                if (ForgeWebView.CoreWebView2 == null)
                {
                    await ForgeWebView.EnsureCoreWebView2Async();
                }
                string accessToken = TokenManager.GetToken();
                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("❌ Access token is missing.");
                    MessageBox.Show("Authentication error. Please log in again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Console.WriteLine("🔄 Initializing WebView2...");
                await ForgeWebView.EnsureCoreWebView2Async();
                Console.WriteLine("✅ WebView2 initialized successfully.");

                string htmlContent = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta http-equiv='X-UA-Compatible' content='IE=Edge' />
    <title>Forge Viewer</title>
    <script src='https://developer.api.autodesk.com/modelderivative/v2/viewers/7.52/viewer3D.min.js'></script>
    <link rel='stylesheet' href='https://developer.api.autodesk.com/modelderivative/v2/viewers/7.52/style.min.css' type='text/css'>
</head>
<body>
    <div id='forgeViewer' style='width: 100%; height: 100vh;'></div>
    <script>
        var options = {{
            env: 'AutodeskProduction',
            getAccessToken: function(onTokenReady) {{
                onTokenReady('{accessToken}', 3599);
            }}
        }};
        var documentId = 'urn:{encodedUrn}';
        Autodesk.Viewing.Initializer(options, function() {{
            var viewerDiv = document.getElementById('forgeViewer');
            var viewer = new Autodesk.Viewing.GuiViewer3D(viewerDiv);
            viewer.start();
            Autodesk.Viewing.Document.load(documentId, function(doc) {{
                var defaultModel = doc.getRoot().getDefaultGeometry();
                viewer.loadDocumentNode(doc, defaultModel);
            }}, function(errorMsg) {{
                console.error('Error loading document: ' + errorMsg);
            }});
        }});
    </script>
</body>
</html>";

                ForgeWebView.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WebView2 initialization failed: {ex.Message}");
            }
        }
        #endregion





        //ZERO REFERENCES FUNCTIONS//
        #region Zero References
        private async Task UpdateGlobalIdsFromModel(Dictionary<string, string> model)
        {
            if (model == null)
            {
                Console.WriteLine("❌ Cannot update global IDs - model is null");
                return;
            }

            // Update item ID and name
            if (model.TryGetValue("Id", out string itemId))
            {
                _selectedItemId = itemId;
                Console.WriteLine($"✅ Updated global item ID: {_selectedItemId}");
            }

            if (model.TryGetValue("Name", out string name))
            {
                _selectedItemName = name;
                Console.WriteLine($"✅ Updated global item name: {_selectedItemName}");
            }

            // Update project ID and name
            if (model.TryGetValue("ProjectId", out string projectId))
            {
                _selectedProjectId = projectId;
                Console.WriteLine($"✅ Updated global project ID: {_selectedProjectId}");
            }

            if (model.TryGetValue("Project", out string project))
            {
                _selectedProjectName = project;
                Console.WriteLine($"✅ Updated global project name: {_selectedProjectName}");
            }

            // Update folder ID
            if (model.TryGetValue("FolderId", out string folderId))
            {
                _folderId = folderId;
                Console.WriteLine($"✅ Updated global folder ID: {_folderId}");
            }

            // Update storage ID or fetch it if missing
            if (model.TryGetValue("StorageId", out string storageId) && !string.IsNullOrEmpty(storageId))
            {
                _objectId = storageId;
                Console.WriteLine($"✅ Updated global storage ID: {_objectId}");
            }
            else if (!string.IsNullOrEmpty(_selectedItemId) && !string.IsNullOrEmpty(_selectedProjectId))
            {
                // Fetch and set the storage ID
                await FetchAndSetStorageId();
            }

            // Save the full model reference
            _selectedModel = model;
        }



        public string EncodeStorageIdToUrn(string storageId)
        {
            if (string.IsNullOrEmpty(storageId))
            {
                Console.WriteLine("❌ Error: Storage ID is null or empty.");
                return null;
            }

            // 🔹 Remove the "urn:" prefix
            if (storageId.StartsWith("urn:"))
            {
                storageId = storageId.Substring(4);
            }

            // 🔹 Convert to Base64
            string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(storageId));

            // 🔹 Autodesk requires URL-safe Base64 encoding
            string urn = base64Encoded.TrimEnd('=').Replace('+', '-').Replace('/', '_');

            Console.WriteLine($"✅ Encoded URN: {urn}");
            return urn;
        }

        private async Task RetrieveItemIdAsync()
        {
            string accessToken = TokenManager.GetToken();
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

                List<Dictionary<string, string>> modelsList = new List<Dictionary<string, string>>();

                // 🔹 Step 1: Retrieve storage IDs & versions for each item
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
                    string latestVersion = versions?.FirstOrDefault().versionName ?? "N/A";

                    Console.WriteLine($"✅ {versions?.Count ?? 0} versions found for {itemName}.");

                    // 🔹 Step 3: Store item details for data grid
                    modelsList.Add(new Dictionary<string, string>
                    {
                        { "Id", itemId },
                        { "Name", itemName },
                        { "StorageId", storageId },
                        { "LatestVersion", latestVersion }
                    });
                }

                // 🔹 Step 4: Update Data Grid on UI thread
                Dispatcher.Invoke(() =>
                {
                    ModelsDataGrid.ItemsSource = null;  // Clear existing items
                    ModelsDataGrid.ItemsSource = modelsList;
                });

                Console.WriteLine($"✅ {modelsList.Count} models added to the data grid.");

                // 🔹 Step 5: Auto-select first item if none is selected
                if (modelsList.Any() && string.IsNullOrEmpty(_selectedItemId))
                {
                    var firstModel = modelsList.First();
                    _selectedItemId = firstModel["Id"];
                    _selectedItemName = firstModel["Name"];

                    Console.WriteLine($"✅ Auto-selected first model: {_selectedItemName} (ID: {_selectedItemId})");

                    await RetrieveStorageIdForSelectedItem(); // Fetch storage for selected item
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving items: {ex.Message}");
            }
        }

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

        public async Task<BitmapImage> GetThumbnail(string urn)
        {
            try
            {
                if (!await EnsureModelTranslation(urn))
                {
                    Console.WriteLine("❌ Model translation failed, skipping thumbnail retrieval.");
                    return null;
                }

                string thumbnailUrl = $"https://developer.api.autodesk.com/modelderivative/v2/designdata/{urn}/thumbnail";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                    Console.WriteLine($"📷 Fetching thumbnail from: {thumbnailUrl}");

                    byte[] imageBytes = await client.GetByteArrayAsync(thumbnailUrl);
                    BitmapImage bitmapImage = new BitmapImage();
                    using (MemoryStream stream = new MemoryStream(imageBytes))
                    {
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();
                    }

                    bitmapImage.Freeze(); // Make it usable across threads
                    return bitmapImage;
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("❌ Thumbnail not found. The model might still be processing.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading thumbnail: {ex.Message}");
            }

            return null;
        }

        //V This function is referenced by a 0 referenced function 
        private async Task<bool> EnsureModelTranslation(string encodedUrn)
        {
            ModelDerivativeService modelService = new ModelDerivativeService(client);

            bool isReady = await ModelDerivativeService.IsModelDerivativeReady(encodedUrn);
            if (!isReady)
            {
                Console.WriteLine("🔄 Model is not ready for SVF. Requesting translation...");
                bool translationStarted = await modelService.SubmitModelForTranslationAsync(encodedUrn, _accessToken);

                if (!translationStarted)
                {
                    Console.WriteLine("❌ Translation failed. Cannot fetch thumbnail.");
                    return false;
                }

                Console.WriteLine("⏳ Waiting for model translation...");
                await Task.Delay(10000); // Wait before retrying
            }

            return true;
        }

        private Image FindThumbnailImageControl(string itemId)
        {
            foreach (UIElement element in ModelsContainer.Children)
            {
                if (element is Border border && border.Child is StackPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is Image img && img.Tag.ToString() == itemId)
                        {
                            return img;
                        }
                    }
                }
            }
            return null;
        }

        private async Task LoadThumbnailAsync(string projectId, string itemId, Image thumbnailImage)
        {
            string thumbnailUrl = await DataManagement.FetchThumbnailUrl(_objectId, _accessToken);

            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                Console.WriteLine($"❌ No thumbnail found for item {itemId}.");
                return;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Ensure valid token
                    string accessToken = TokenManager.GetToken();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    Console.WriteLine($"📷 Fetching thumbnail from: {thumbnailUrl}");

                    byte[] imageBytes = await client.GetByteArrayAsync(thumbnailUrl);

                    BitmapImage bitmapImage = new BitmapImage();
                    using (MemoryStream stream = new MemoryStream(imageBytes))
                    {
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();
                    }

                    bitmapImage.Freeze();

                    // Update UI on the main thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        thumbnailImage.Source = bitmapImage;
                    });
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("❌ Unauthorized! Check your access token.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading thumbnail: {ex.Message}");
            }
        }
        #endregion
        
        //models put in db
        private static async Task GetModelData(string modelId, string projectId, string folderName)
        {
            try
            {
                //MessageBox.Show($"{DateTime.Now}");
                string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{modelId}";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());
                    var response = await client.GetAsync(url);
                    string responseString = await response.Content.ReadAsStringAsync();
                    var JsonResponse = JsonConvert.DeserializeObject<dynamic>(responseString);
                    
                    //MessageBox.Show("Item: " + responseString);
                    ModelData data = new ModelData
                    {
                        Id = modelId,
                        Name = JsonResponse.data.attributes.displayName,
                        HubId = hubID,
                        CreatedBy = JsonResponse.data.attributes.createUserId,
                        CreatedDate = JsonResponse.data.attributes.createTime,
                        ModifiedDate = JsonResponse.data.attributes.lastModifiedTime,
                        ModifiedBy = JsonResponse.data.attributes.lastModifiedUserId,
                        FileSize = (int)JsonResponse["included"][0]["attributes"]["storageSize"],
                        PublicPrivate = "Private",
                        Foldername = folderName,
                        FolderId = projectId,
                        UpvoteCount = 0
                    };
                    
                    InsertModelDB(data);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error getting model data: {e.Message}");
                throw;
            }
        } 
        
        private async static void InsertModelDB(ModelData modelData)
        {
            MongoConnection database = new MongoConnection();
            var findModel = await database.ModelData.Find(x => x.Id == modelData.Id).FirstOrDefaultAsync();

            if (findModel == null)
            {
                await database.ModelData.InsertOneAsync(modelData);
            }
        }
        
        //Fuzzy search
        private async void SearchText_Box_OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                List<Dictionary<string, string>> searchResults = new List<Dictionary<string, string>>();
                if (e.Key == Key.Enter)
                {
                    MongoConnection database = new MongoConnection();
                    List<ModelData> result = await database.ModelData.Find(x => x.PublicPrivate == "Public" || x.HubId == hubID).ToListAsync();
            
                    string[,] modelsArray = new string[result.Count, 9];
                    int index = 0;
                    foreach (ModelData modelData in result)
                    {
                        modelsArray[index, 0] = modelData.Id;
                        modelsArray[index, 1] = modelData.Name;
                        modelsArray[index, 2] = modelData.HubId;
                        modelsArray[index, 3] = modelData.CreatedBy;
                        modelsArray[index, 4] = modelData.CreatedDate;
                        modelsArray[index, 5] = modelData.ModifiedDate;
                        modelsArray[index, 6] = modelData.ModifiedBy;
                        modelsArray[index, 7] = modelData.FileSize.ToString();
                        modelsArray[index, 8] = modelData.FolderId;
                        index++;
                    }
            
                    var modelNames = result.Select(x => x.Name).ToList();
                    var topResults = FuzzySharp.Process.ExtractTop(SearchText_Box.Text, modelNames, limit: 3);
                
                    foreach (var match in topResults)
                    {
                        for (int i = 0; i < modelNames.Count; i++)
                        {
                            Console.WriteLine($"{match.Value}: {modelsArray[i, 1]}");
                            if (match.Value == modelsArray[i, 1])
                            {
                                Console.WriteLine($"Found Match: {match.Value}");
                                string name = await GetUserName(modelsArray[i,3]);
                                //MessageBox.Show($"Result: {match.Value}, Score: {match.Score}");
                                searchResults.Add(new Dictionary<string, string>
                                {
                                    { "Name", modelsArray[i, 1] },
                                    { "Project", name },
                                    { "LastModified", match.Score.ToString() },
                                    { "Id", modelsArray[i, 0] },
                                    { "ProjectId" , modelsArray[i, 8]}
                                });
                                break;
                            }
                        }
                    }
                }
                DisplaySearchResults(searchResults);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching text box: {ex.Message}");
            }
        }

        private void DisplaySearchResults(List<Dictionary<string, string>> searchResults)
        {
            ModelsDataGrid.Columns[1].Header = "Created By";
            ModelsDataGrid.Columns[2].Header = "Score";
            ModelsDataGrid.ItemsSource = searchResults;
            originalResults = searchResults;
            Models = searchResults;
            DisplayGridModels();
        }
        
        //Filter tags
        private void Filter_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var icon = sender as Border;
                if (icon != null)
                {
                    ContextMenu filterMenu =  this.FindResource("FilterContextMenu") as ContextMenu;
                    if (filterMenu != null)
                    {
                        filterMenu.PlacementTarget = icon;
                        filterMenu.IsOpen = true;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error filtering: {exception.Message}");
            }
        }

        private async void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu filterMenu = FindResource("FilterContextMenu") as ContextMenu;
            foreach (var item in filterMenu.Items)
            {
                if (item is CheckBox checkBox)
                    checkBox.IsChecked = false;
            }

            ModelsDataGrid.ItemsSource = originalResults;
            Models = originalResults;
            DisplayGridModels();
        }

        private async void BtnSearchFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ModelsDataGrid.ItemsSource = originalResults;
                List<Dictionary<string, string>> models = new List<Dictionary<string, string>>();
                List<string> selectedTags = new List<string>();
                List<string> IDs = new List<string>();
            
                MongoConnection database = new MongoConnection();
                
                ContextMenu filterMenu = FindResource("FilterContextMenu") as ContextMenu;
                foreach (var item in filterMenu.Items)
                {
                    if (item != null && item is CheckBox checkBox)
                    {
                        if (checkBox.IsChecked == true)
                        {
                            selectedTags.Add(checkBox.Content.ToString());
                        }
                    }
                }
                
                foreach (var item in ModelsDataGrid.Items)
                {
                    if (item is Dictionary<string, string> modelData && modelData.ContainsKey("Id"))
                    {
                        string Id = modelData["Id"];
                        IDs.Add(Id);
                    }
                }
                
                ModelsDataGrid.ItemsSource = null;
                
                foreach (var id in IDs)
                {
                    var filter = Builders<ModelData>.Filter.And(Builders<ModelData>.Filter.Eq("Id", id),
                        Builders<ModelData>.Filter.In("Tags", selectedTags));
                
                    var result = await database.ModelData.Find(filter).FirstOrDefaultAsync();

                    if (result != null)
                    {
                        models.Add(new Dictionary<string, string>
                        {
                            { "Name", result.Name },
                            { "Project", result.Foldername },
                            { "LastModified", result.ModifiedDate },
                            { "Id", id },
                            { "ProjectId", result.FolderId}
                        });
                    }
                }
                
                ModelsDataGrid.ItemsSource = models;
                Models = models;
                DisplayGridModels();
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error filtering: {exception.Message}");
            }
        }

        //Upvote system
        private async void UpArrow_Click(object sender, RoutedEventArgs e)
        {
            if (UpArrow.Kind == PackIconKind.ArrowTopBoldOutline)
            {
                if (DownArrow.Kind == PackIconKind.ArrowDownBold)
                {
                    DownArrow.Kind = PackIconKind.ArrowDownBoldOutline;
                    DownArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B4B4B"));
                    await UpdateModelUpvoteCount(2, _selectedItemId); //remove downvote and add upvote
                }
                else
                {
                    await UpdateModelUpvoteCount(1, _selectedItemId); //add upvote
                }
                UpArrow.Kind = PackIconKind.ArrowTopBold;
                UpArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#11d137"));
                await UpdateUserModelVote(_selectedItemId, _userId, 1);
            }
            else
            {
                UpArrow.Kind = PackIconKind.ArrowTopBoldOutline;
                UpArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#11d137"));
                await UpdateModelUpvoteCount(-1, _selectedItemId); //remove upvote
                await UpdateUserModelVote(_selectedItemId, _userId, 0);
            }
        }

        private async void DownArrow_Click(object sender, RoutedEventArgs e)
        {
            if (DownArrow.Kind == PackIconKind.ArrowDownBoldOutline)
            {
                if (UpArrow.Kind == PackIconKind.ArrowTopBold)
                {
                    UpArrow.Kind = PackIconKind.ArrowTopBoldOutline;
                    UpArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B4B4B"));
                    await UpdateModelUpvoteCount(-2, _selectedItemId); //remove upvote and add downvote
                }
                else
                {
                    await UpdateModelUpvoteCount(-1, _selectedItemId); //add downvote
                }
                DownArrow.Kind = PackIconKind.ArrowDownBold;
                DownArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d11111"));
                await UpdateUserModelVote(_selectedItemId, _userId, -1);
            }
            else
            {
                DownArrow.Kind = PackIconKind.ArrowDownBoldOutline;
                DownArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d11111"));
                await UpdateModelUpvoteCount(1, _selectedItemId); //remove downvote
                await UpdateUserModelVote(_selectedItemId, _userId, 0);
            }
        }
        
        private async Task<int> GetModelUpvoteCount(string id)
        {
            MongoConnection database = new MongoConnection();
            var userData = await database.ModelData.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (userData == null)
            {
                return 0;
            }
            return userData.UpvoteCount;
        }

        private async Task UpdateModelUpvoteCount(int num, string id)
        {
            MongoConnection database = new MongoConnection();
            var filter = Builders<ModelData>.Filter.Eq("Id", id);
            var update = Builders<ModelData>.Update.Inc("UpvoteCount", num);
            await database.ModelData.UpdateOneAsync(filter, update);
            int upvoteCount = await GetModelUpvoteCount(id);
            UpvoteTextBlock.Text = upvoteCount.ToString();
        }

        private async Task SetUserModelVote(string modelId, string userId)
        {
            MongoConnection database = new MongoConnection();
            var findVote = await database.Upvotes.Find(x => x.ModelId == modelId && x.UserId == userId).FirstOrDefaultAsync();

            if (findVote == null)
            {
                Upvotes upvote = new Upvotes()
                {
                    Id = new ObjectId(), ModelId = modelId, UserId = userId, Vote = 0
                };
                await database.Upvotes.InsertOneAsync(upvote);
            }
            
        }

        private async Task UpdateUserModelVote(string modelId, string userId, int vote)
        {
            MongoConnection database = new MongoConnection();
            var filter = Builders<Upvotes>.Filter.And(
                Builders<Upvotes>.Filter.Eq("ModelId", modelId),
                            Builders<Upvotes>.Filter.Eq("UserId", userId));
            var update = Builders<Upvotes>.Update.Set("Vote", vote);
            await database.Upvotes.UpdateOneAsync(filter, update);
        }
        
        private async Task<int> GetUserModelVote(string modelId, string userId)
        {
            MongoConnection database = new MongoConnection();
            var result = await database.Upvotes.Find(x => x.ModelId == modelId && x.UserId == userId).FirstOrDefaultAsync();
            if (result == null)
            {
                return 0;
            }
            else
            {
                return result.Vote;
            }
        }
        
        //COMMENTED OUT FUNCTIONS//

        /* private void BtnGenerate3D_Click(object sender, RoutedEventArgs e)
        {

        }
        */
        /*  public async Task ShowThumbnail(string projectId, string itemId)
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
                   //ThumbnailImage.Source = bitmapImage;
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

*/
        /*   private void ModelsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
   {
       if (ModelsDataGrid.SelectedItem is Dictionary<string, string> model)
       {
           _selectedModel = model;

           if (model.TryGetValue("Id", out string modelId))
           {
               _selectedItemId = modelId;
           }
           else
           {
               _selectedItemId = null;
               Console.WriteLine("❌ Model ID missing in selection.");
           }

           _selectedItemName = model.ContainsKey("Name") ? model["Name"] : "Unknown";
           _selectedProjectId = model.ContainsKey("ProjectId") ? model["ProjectId"] : null;
           _selectedProjectName = model.ContainsKey("Project") ? model["Project"] : null;

           Console.WriteLine($"✅ Selected Model: {_selectedItemName} (ID: {_selectedItemId}, selected Project ID: {_selectedProjectId})");
       }
   }
*/
        /* private void DragDeltaThumb(object sender, DragDeltaEventArgs e)
        {
            if (ResizeSidebar.Width.IsAuto || ResizeSidebar.Width.IsStar)
            {
                ResizeSidebar.Width = new GridLength(ResizeSidebar.ActualWidth, GridUnitType.Pixel);
            }

            double newWidth = ResizeSidebar.Width.Value = e.HorizontalChange;

            if (newWidth > 100 && newWidth < 400)
            {
                ResizeSidebar.Width = new GridLength(newWidth);
            }
        }*/
        /* private async void Grid_Click(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedProjectId))
            {
                MessageBox.Show("❌ Please select a project to view models.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ModelsDataGrid.Visibility = Visibility.Collapsed; // Hide DataGrid
            Grid_View.Visibility = Visibility.Visible; // Show Grid View

            // Clear previous grid data
            ModelsContainer.Children.Clear();

            try
            {
                // Fetch models for the selected project only
                List<Dictionary<string, string>> models = await GetModelsFromProject(_selectedProjectId, _folderId);

                if (models == null || models.Count == 0)
                {
                    MessageBox.Show("No models found for this project.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Populate grid with models from the selected project
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

                    StackPanel content = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    TextBlock modelName = new TextBlock
                    {
                        Text = model["Name"],
                        FontSize = 16,
                        FontWeight = FontWeights.Normal,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(5, 2, 5, 2)
                    };

                    TextBlock projectName = new TextBlock
                    {
                        Text = $"Project: {model["Project"]}",
                        FontSize = 14,
                        FontWeight = FontWeights.Normal,
                        Foreground = Brushes.Gray,
                        TextAlignment = TextAlignment.Left,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(5, 2, 5, 2)
                    };

                    content.Children.Add(modelName);
                    content.Children.Add(projectName);
                    modelSquare.Child = content;
                    ModelsContainer.Children.Add(modelSquare);
                }

                Console.WriteLine($"✅ {models.Count} models loaded successfully in grid view.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error loading models: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Update UI styles to reflect active view mode
            List_Border.Background = Brushes.Transparent;
            Grid_Border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9E9E9"));
        }

*/


    }

}


