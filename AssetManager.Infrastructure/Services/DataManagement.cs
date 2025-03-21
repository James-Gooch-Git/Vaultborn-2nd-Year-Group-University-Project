using System;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AssetManager.Infrastructure.Services;
using System.Text;
using Newtonsoft.Json;
using ForgeViewerApp;


namespace AssetManager.Infrastructure.Services
{
    public class DataManagement
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> GetPersonalHub()
        {
            string url = "https://developer.api.autodesk.com/project/v1/hubs";
            string _accessToken = TokenManager.GetToken();

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing or invalid.");
                return null;
            }

            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                string selectedHubId = null;
                string selectedHubType = null;

                foreach (JsonElement hub in root.GetProperty("data").EnumerateArray())
                {
                    string type = hub.GetProperty("attributes").GetProperty("extension").GetProperty("type").GetString();
                    string hubID = hub.GetProperty("id").GetString();
                    string hubName = hub.GetProperty("attributes").GetProperty("name").GetString();

                    Console.WriteLine($"🔍 Found hub type: {type}, ID: {hubID}, Name: {hubName}");

                    // Store the first available hub
                    if (selectedHubId == null)
                    {
                        selectedHubId = hubID;
                        selectedHubType = type;
                    }

                    // Prioritize Personal Hub if available
                    if (type == "hubs:autodesk.a360:PersonalHub")
                    {
                        Console.WriteLine($"✅ Selected Personal Hub: {hubID}");
                        return hubID;
                    }
                }

                if (selectedHubId != null)
                {
                    Console.WriteLine($"✅ No Personal Hub found, using {selectedHubType} instead: {selectedHubId}");
                    return selectedHubId;
                }

                Console.WriteLine("❌ No hubs found.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return null;
            }
        }


        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> GetProjectIdAsync(string hubID)
        {
            string accessToken = TokenManager.GetToken();

            try
            {
                string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubID}/projects";

                // Set up request headers
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Send request
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Ensure the request succeeded
                string responseJson = await response.Content.ReadAsStringAsync();

                // Parse JSON response
                using JsonDocument doc = JsonDocument.Parse(responseJson);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("data", out JsonElement projects) && projects.GetArrayLength() > 0)
                {
                    string projectId = projects[0].GetProperty("id").GetString();
                    Console.WriteLine($"✅ Project ID Retrieved: {projectId}");
                    return projectId;
                }
                else
                {
                    Console.WriteLine("❌ No projects found in this hub.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving Project ID: {ex.Message}");
                return null;
            }
        }

        //Gets the personal hub and returns it's HubID, HubName and HubType in a tuple
        public static async Task<(string, string, string)?> GetPersonalHubDetails()
        {
            string url = "https://developer.api.autodesk.com/project/v1/hubs";
            string _accessToken = TokenManager.GetToken();

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing or invalid.");
                return null;
            }

            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                string selectedHubId = null;
                string selectedHubType = null;
                string selectedHubName = null;

                foreach (JsonElement hub in root.GetProperty("data").EnumerateArray())
                {
                    string hubType = hub.GetProperty("attributes").GetProperty("extension").GetProperty("type")
                        .GetString();
                    string hubID = hub.GetProperty("id").GetString();
                    string hubName = hub.GetProperty("attributes").GetProperty("name").GetString();

                    Console.WriteLine($"🔍 Found hub type: {hubType}, ID: {hubID}, Name: {hubName}");

                    // Store the first available hub
                    if (selectedHubId == null)
                    {
                        selectedHubId = hubID;
                        selectedHubType = hubType;
                        selectedHubName = hubName;
                    }

                    // Prioritize Personal Hub if available
                    if (hubType == "hubs:autodesk.a360:PersonalHub")
                    {
                        Console.WriteLine($"✅ Selected Personal Hub: {hubID}");
                        return (hubID, hubName, hubType);
                    }
                }

                if (selectedHubId != null)
                {
                    Console.WriteLine($"✅ No Personal Hub found, using {selectedHubType} instead: {selectedHubId}");
                    return (selectedHubId, selectedHubName, selectedHubType);
                }

                Console.WriteLine("❌ No hubs found.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return null;
            }
        }

        //Gets every project from a specific hub and returns them as a tuple of their respective Project ID and Project name
        public static async Task<List<(string ProjectId, string ProjectName)>> GetAllProjectsFromHub(string hubID)
        {
            string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubID}/projects";
            string _accessToken = TokenManager.GetToken();

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing or invalid.");
                return null;
            }

            try
            {
                using (HttpClient client = new HttpClient()) // Create a new HttpClient instance
                {
                    // Set Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                    // Make the GET request
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Check if the response is successful
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
                        return null;
                    }

                    // Parse the JSON response
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;

                    List<(string ProjectId, string ProjectName)>
                        projects = new List<(string, string)>(); // List of tuples

                    // Loop through the "data" array to get project information
                    foreach (JsonElement project in root.GetProperty("data").EnumerateArray())
                    {
                        // Access the project ID and name
                        string projectId = project.GetProperty("id").GetString();
                        string projectName = project.GetProperty("attributes").GetProperty("name").GetString();

                        // Add to the list as a tuple
                        projects.Add((projectId, projectName));
                    }

                    return projects;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return null;
            }
        }

        //Gets Folder IDs and Folder Names for Top-level folders from a specific project 
        public static async Task<(string FolderId, string FolderName)> GetTopLevelFolder(string hubID, string projectId)
        {
            string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubID}/projects/{projectId}/topFolders";
            string _accessToken = TokenManager.GetToken(); // Ensure you have a valid token

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing or invalid.");
                return (null, null); // Return a tuple with null values if the access token is invalid
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set Authorization Header
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                    // Make GET Request
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
                        return (null, null); // Return a tuple with null values if the request fails
                    }

                    // Parse JSON Response
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;

                    // Check if "data" property exists and is not empty
                    if (root.TryGetProperty("data", out JsonElement data) && data.GetArrayLength() > 0)
                    {
                        // Get the first folder's ID and Name
                        JsonElement firstFolder = data[0]; // Access the first folder
                        string folderId = firstFolder.GetProperty("id").GetString();
                        string folderName = firstFolder.GetProperty("attributes").GetProperty("displayName").GetString();

                        // Return the first folder as a tuple
                        return (folderId, folderName);
                    }
                    else
                    {
                        Console.WriteLine("❌ No top-level folders found.");
                        return (null, null); // Return a tuple with null values if no folders are found
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                return (null, null); // Return a tuple with null values in case of an exception
            }
        }

        public static async Task<List<(string ItemId, string ItemName, bool IsFolder)>> GetProjectItems(string projectId, string folderId)
        {
            string accessToken = TokenManager.GetToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("❌ No valid access token.");
                return new List<(string, string, bool)>(); // ✅ Return an empty list instead of null
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
                        Console.WriteLine($"❌ Error fetching folder contents: {response.StatusCode} - {response.ReasonPhrase}");
                        return new List<(string, string, bool)>(); // ✅ Return an empty list instead of null
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;

                    List<(string, string, bool)> projectItems = new List<(string, string, bool)>();

                    foreach (JsonElement item in root.GetProperty("data").EnumerateArray())
                    {
                        string itemId = item.GetProperty("id").GetString();
                        string itemName = item.GetProperty("attributes").GetProperty("displayName").GetString();
                        bool isFolder = item.GetProperty("type").GetString() == "folders";

                        projectItems.Add((itemId, itemName, isFolder));
                    }

                    return projectItems;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return new List<(string, string, bool)>(); // ✅ Always return an empty list
            }
        }


        public static async Task<List<(string ItemId, string ItemName)>> GetItemsInFolder(string projectId, string folderId)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}/contents";

            HttpResponseMessage response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error fetching items: {response.StatusCode}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonResponse);

            var items = new List<(string ItemId, string ItemName)>();

            foreach (var item in data.data)
            {
                string itemId = item.id;
                string itemName = item.attributes.displayName;
                items.Add((itemId, itemName));
            }

            return items;
        }

        //Gets a list of Item IDs, Item Names, and Item Types from a specific folder in a project
        public static async Task<List<(string ItemId, string ItemName, string ItemType)>> GetFolderItems(string projectId, string folderId)
        {
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}/contents";
            string _accessToken = TokenManager.GetToken(); // Ensure you have a valid token

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing or invalid.");
                return null;
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set Authorization Header
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                    // Make GET Request
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
                        return null;
                    }

                    // Parse JSON Response
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;

                    List<(string ItemId, string ItemName, string ItemType)>
                        items = new List<(string, string, string)>();

                    // Extract Item IDs, Names, and Types
                    foreach (JsonElement item in root.GetProperty("data").EnumerateArray())
                    {
                        if (item.GetProperty("type").GetString() == "items")
                        {
                            string itemId = item.GetProperty("id").GetString();
                            string itemName = item.GetProperty("attributes").GetProperty("displayName").GetString();
                            string itemType = item.GetProperty("type").GetString(); // Item type is now included

                            items.Add((itemId, itemName, itemType));
                        }
                    }

                    return items;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                return null;
            }
        }

      
        //Creates new folder in a specified 
        public static async Task<bool> CreateNewFolder(string projectId, string parentFolderId, string folderName)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

                // Step 1: Retrieve the necessary data for folder creation
                var result = await GetPersonalHubDetails();
                (string hubID, string HubName, string HubType) = result.Value;


                if (string.IsNullOrEmpty(parentFolderId))
                {
                    Console.WriteLine("❌ Error: No valid parent folder found for folder creation.");
                    return false;
                }

                // Step 3: Prepare the request body for folder creation
                var requestBody = new
                {
                    jsonapi = new { version = "1.0" },
                    data = new
                    {
                        type = "folders",
                        attributes = new
                        {
                            name = folderName,
                            extension = new
                            {
                                type = "folders:autodesk.core:Folder",
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

                string json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders";

                // Step 4: Send the request
                var response = await httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: Failed to create folder. Status: {response.StatusCode}");
                    return false;
                }

                // Step 5: Parse the response to get the folder ID
                using JsonDocument doc = JsonDocument.Parse(responseContent);
                string folderId = doc.RootElement.GetProperty("data").GetProperty("id").GetString();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception while creating folder: {ex.Message}");
                return false;
            }
        }


        /*
                public static async Task<string> GetLatestItemThumbnail(string projectId, string itemId)
                {
                    string accessToken = TokenManager.GetToken();
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        Console.WriteLine("❌ No valid access token.");
                        return null;
                    }

                    string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions";

                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                            HttpResponseMessage response = await client.GetAsync(url);

                            if (!response.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"❌ Error fetching item versions: {response.StatusCode} - {response.ReasonPhrase}");
                                return null;
                            }

                            string jsonResponse = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"JSON Response: {jsonResponse}");

                            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                            JsonElement root = doc.RootElement;

                            if (root.TryGetProperty("data", out JsonElement data))
                            {
                                var latestVersion = data.EnumerateArray()
                                                        .OrderByDescending(version => version.GetProperty("attributes").GetProperty("createTime").GetString())
                                                        .FirstOrDefault();

                                if (latestVersion.ValueKind == JsonValueKind.Undefined)
                                {
                                    Console.WriteLine("❌ No versions found for this item.");
                                    return null;
                                }

                                if (latestVersion.TryGetProperty("relationships", out JsonElement relationships) &&
                                    relationships.TryGetProperty("thumbnails", out JsonElement thumbnails) &&
                                    thumbnails.TryGetProperty("meta", out JsonElement meta) &&
                                    meta.TryGetProperty("link", out JsonElement link) &&
                                    link.TryGetProperty("href", out JsonElement href))
                                {
                                    return href.GetString(); // Correct thumbnail URL
                                }
                                else
                                {
                                    Console.WriteLine("❌ Thumbnail link not found in response.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("❌ No 'data' property found in the response.");
                            }

                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                        return null;
                    }
                }*/
        public static async Task<string> GetLatestItemThumbnail(string projectId, string itemId, string encodedUrn)
        {
            string accessToken = TokenManager.GetToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("❌ No valid access token.");
                return null;
            }

            // ✅ Step 1: Fetch the latest version URN
            string versionsUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    HttpResponseMessage response = await client.GetAsync(versionsUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ Error fetching item versions: {response.StatusCode} - {response.ReasonPhrase}");
                        return null;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine($"JSON Response: {jsonResponse}");

                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("data", out JsonElement data))
                    {
                        var latestVersion = data.EnumerateArray()
                                                .OrderByDescending(version => version.GetProperty("attributes").GetProperty("createTime").GetString())
                                                .FirstOrDefault();

                        if (latestVersion.ValueKind == JsonValueKind.Undefined)
                        {
                            Console.WriteLine("❌ No versions found for this item.");
                            return null;
                        }

                        // ✅ Extract the correct URN (Base64 encoding is required)
                        if (latestVersion.TryGetProperty("relationships", out JsonElement relationships) &&
                            relationships.TryGetProperty("derivatives", out JsonElement derivatives) &&
                            derivatives.TryGetProperty("data", out JsonElement derivativeData) &&
                            derivativeData.TryGetProperty("id", out JsonElement urnElement))
                        {
                            string rawUrn = urnElement.GetString();
                            //string encodedUrn = EncodeObjectIdToUrn(rawUrn);

                            Console.WriteLine($"✅ Encoded URN: {encodedUrn}");

                            return await FetchThumbnailUrl(encodedUrn, accessToken);
                        }
                        else
                        {
                            Console.WriteLine("❌ URN not found in response.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ No 'data' property found in the response.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
            }

            return null;
        }

        public static async Task<string> FetchThumbnailUrl(string encodedUrn, string accessToken)
        {
            string thumbnailUrl = $"https://developer.api.autodesk.com/modelderivative/v2/designdata/{encodedUrn}/thumbnail";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.GetAsync(thumbnailUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Thumbnail not found or model not translated yet: {response.StatusCode}");
                    return null;
                }

                return thumbnailUrl;
            }
        }


        public static async Task<List<(string HubID, string HubName, string HubType)>> GetAllHubs()
        {
            string url = "https://developer.api.autodesk.com/project/v1/hubs";
            string _accessToken = TokenManager.GetToken();
            var hubList = new List<(string, string, string)>();

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing or invalid.");
                return hubList;
            }

            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return hubList;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                foreach (JsonElement hub in root.GetProperty("data").EnumerateArray())
                {
                    string hubType = hub.GetProperty("attributes").GetProperty("extension").GetProperty("type").GetString();
                    string hubID = hub.GetProperty("id").GetString();
                    string hubName = hub.GetProperty("attributes").GetProperty("name").GetString();

                    Console.WriteLine($"🔍 Found Hub: Type={hubType}, ID={hubID}, Name={hubName}");

                    hubList.Add((hubID, hubName, hubType));
                }

                return hubList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return hubList;
            }
        }

        public static async Task<string> GetProjectIdByName(string hubID, string projectName)
        {
            var projects = await GetAllProjectsFromHub(hubID);

            foreach (var (id, name) in projects)
            {
                if (name.Equals(projectName, StringComparison.OrdinalIgnoreCase))
                {
                    return id;
                }
            }
            return null;
        }

        public static async Task<string> CreateProject(string hubID, string projectName)
        {
            string newProjectId = await TokenService.CreateProject(hubID, projectName);
            return newProjectId;
        }

        public async Task<List<(string versionId, string versionName, string storageId)>> GetVersionsForItemAsync(string projectId, string itemId)
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
                        versions.Add((versionId, versionName, storageId));
                    }
                }
                return versions;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static async Task<List<(int VersionNumber, string VersionID, string CreateTime, string CreatedBy, string StorageURN)>> GetItemVersions(string projectId, string itemId)
        {
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions";
            string _accessToken = TokenManager.GetToken();
            var versionList = new List<(int, string, string, string, string)>();

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing or invalid.");
                return versionList;
            }

            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return versionList;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                foreach (JsonElement version in root.GetProperty("data").EnumerateArray())
                {
                    int versionNumber = version.GetProperty("attributes").GetProperty("versionNumber").GetInt32();
                    string versionID = version.GetProperty("id").GetString();
                    string createTime = version.GetProperty("attributes").GetProperty("createTime").GetString();
                    string createdBy = version.GetProperty("attributes").GetProperty("createUserName").GetString();
                    string storageURN = version.GetProperty("relationships").GetProperty("storage").GetProperty("data").GetProperty("id").GetString();

                    Console.WriteLine($"📄 Found Version: Number={versionNumber}, ID={versionID}, Created={createTime} by {createdBy}");

                    versionList.Add((versionNumber, versionID, createTime, createdBy, storageURN));
                }

                return versionList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return versionList;
            }
        }





    }
}
