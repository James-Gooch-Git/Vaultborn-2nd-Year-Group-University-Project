using System;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AssetManager.Infrastructure.Services;
using AssetManager.Infrastructure.Data;
using System.Text;
using Newtonsoft.Json;
using ForgeViewerApp;
using MongoDB.Bson;
using MongoDB.Driver;
using AssetManager.Infrastructure.Models;


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

                            return await FetchThumbnailUrl(encodedUrn, accessToken, projectId, itemId);
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

        public static async Task<string> GetVersionThumbnail(string projectId, string itemId, string encodedUrn, string versionId)
        {
            string accessToken = TokenManager.GetToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("❌ No valid access token.");
                return null;
            }

            // ✅ Step 1: Fetch version metadata for the specified versionId
            string versionUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions/{versionId}";
            Console.WriteLine($"Fetching version metadata for Version ID: {versionId}");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    HttpResponseMessage response = await client.GetAsync(versionUrl);

                    Console.WriteLine($"Version metadata fetch status: {response.StatusCode} - {response.ReasonPhrase}");

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ Error fetching version metadata: {response.StatusCode} - {response.ReasonPhrase}");
                        return null;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Version metadata response: {jsonResponse}");

                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("data", out JsonElement data))
                    {
                        var version = data;
                        string responseVersionId = version.GetProperty("id").GetString();  // Get the ID from the response
                        Console.WriteLine($"Response version ID: {responseVersionId}"); // Printing the response version ID

                        // Check if the version ID matches
                        if (responseVersionId != versionId)
                        {
                            Console.WriteLine($"❌ Version ID mismatch: Expected {versionId}, but got {responseVersionId}. Cannot fetch thumbnail.");
                            return null;
                        }

                        // If the IDs match, continue processing
                        if (version.TryGetProperty("relationships", out JsonElement relationships) &&
                            relationships.TryGetProperty("derivatives", out JsonElement derivatives) &&
                            derivatives.TryGetProperty("data", out JsonElement derivativeData) &&
                            derivativeData.TryGetProperty("id", out JsonElement urnElement))
                        {
                            string rawUrn = urnElement.GetString();
                            Console.WriteLine($"✅ Found URN: {rawUrn}");

                            return await FetchThumbnailUrl(encodedUrn, accessToken, projectId, itemId, versionId);
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




        public static async Task<string> FetchThumbnailUrl(string encodedUrn, string accessToken, string projectId, string itemId, string versionId = null)
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
                else
                {
                    var mongo = new MongoConnection();
                    var _models = mongo.GetCollection("ModelData");

                    byte[] imageData = await response.Content.ReadAsByteArrayAsync();

                    // ✅ Convert the image to a Base64 string (for storage in MongoDB)
                    string base64Image = Convert.ToBase64String(imageData);

                    // ✅ Use versionId if provided, otherwise fall back to itemId
                    string filterId = string.IsNullOrEmpty(versionId) ? itemId : versionId;

                    // ✅ Update the existing model document using projectId & filterId (either itemId or versionId)
                    var filter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("_folderid", projectId),
                        Builders<BsonDocument>.Filter.Eq("_id", filterId)
                    );

                    var update = Builders<BsonDocument>.Update.Set("thumbnail_url", base64Image);

                    try
                    {
                        var result = await _models.UpdateOneAsync(filter, update);
                        if (result.MatchedCount == 0)
                        {
                            Console.WriteLine($"⚠️ No matching model found to update for Project: {projectId}, Item/Version: {filterId}");
                        }
                        else
                        {
                            Console.WriteLine($"✅ Thumbnail image updated for Project: {projectId}, Item/Version: {filterId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error updating model thumbnail in MongoDB: {ex.Message}");
                        return null;
                    }
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

        public static async Task<(int VersionNumber, string VersionID, string CreateTime, string CreatedBy, long FileSize, string FileFormat, string FileType, string LastModifiedTime)> GetItemVersionMetadata(string projectId, string itemId, string versionId)
        {
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions/{versionId}";
            string _accessToken = TokenManager.GetToken();
            var versionMetadata = (0, string.Empty, string.Empty, string.Empty, 0L, "Not available", "Not available", "Not available");

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing or invalid.");
                return versionMetadata;
            }

            try
            {
                // Log URL being accessed
                Console.WriteLine($"🔗 Fetching version metadata from URL: {url}");

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return versionMetadata;
                }

                // Log successful API response
                Console.WriteLine($"✅ Successfully fetched version metadata for projectId: {projectId}, itemId: {itemId}, versionId: {versionId}");

                string jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔍 API Response: {jsonResponse.Substring(0, Math.Min(1000, jsonResponse.Length))}..."); // Log first 1000 characters of response for easier debugging

                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                // Extract version metadata
                int versionNumber = root.GetProperty("data").GetProperty("attributes").GetProperty("versionNumber").GetInt32();
                string versionID = root.GetProperty("data").GetProperty("id").GetString();
                string createTime = root.GetProperty("data").GetProperty("attributes").GetProperty("createTime").GetString();
                string createdBy = root.GetProperty("data").GetProperty("attributes").GetProperty("createUserName").GetString();

                // Now fetch the storage details to get the file size and additional metadata
                string storageUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions/{versionID}/storage";
                Console.WriteLine($"🔗 Fetching storage details from: {storageUrl}");

                HttpResponseMessage storageResponse = await client.GetAsync(storageUrl);

                if (storageResponse.IsSuccessStatusCode)
                {
                    string storageJsonResponse = await storageResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"🔍 Storage API Response: {storageJsonResponse.Substring(0, Math.Min(1000, storageJsonResponse.Length))}..."); // Log first 1000 characters for debugging

                    using JsonDocument storageDoc = JsonDocument.Parse(storageJsonResponse);
                    JsonElement storageRoot = storageDoc.RootElement;

                    long fileSize = 0;
                    string fileFormat = "Not available";
                    string fileType = "Not available";
                    string lastModifiedTime = "Not available";

                    if (storageRoot.TryGetProperty("data", out JsonElement data))
                    {
                        fileSize = data.GetProperty("attributes").GetProperty("size").GetInt64();
                        fileFormat = data.GetProperty("attributes").TryGetProperty("format", out JsonElement format) ? format.GetString() : "Not available";
                        fileType = data.GetProperty("attributes").TryGetProperty("type", out JsonElement type) ? type.GetString() : "Not available";
                        lastModifiedTime = data.GetProperty("attributes").TryGetProperty("lastModifiedTime", out JsonElement modifiedTime) ? modifiedTime.GetString() : "Not available";
                    }

                    // Log the file size and other version info
                    Console.WriteLine($"📄 Found Version: Number={versionNumber}, ID={versionID}, Created={createTime} by {createdBy}, File Size={fileSize} bytes, Format={fileFormat}, Type={fileType}, Last Modified={lastModifiedTime}");

                    versionMetadata = (versionNumber, versionID, createTime, createdBy, fileSize, fileFormat, fileType, lastModifiedTime);
                }
                else
                {
                    Console.WriteLine($"❌ Error fetching storage details for version {versionID}. Status Code: {storageResponse.StatusCode}");
                }

                return versionMetadata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return versionMetadata;
            }
        }

        public static async Task<List<(int VersionNumber, string VersionID, string CreateTime, string CreatedBy, long FileSize, string FileFormat, string FileType, string LastModifiedTime)>> GetItemVersionsWithExtraMetadata(string projectId, string itemId)
        {
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions";
            string _accessToken = TokenManager.GetToken();
            var versionList = new List<(int, string, string, string, long, string, string, string)>();

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

                    // Now we fetch the storage details to get the file size and additional metadata
                    string storageUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions/{versionID}/storage";
                    HttpResponseMessage storageResponse = await client.GetAsync(storageUrl);

                    if (storageResponse.IsSuccessStatusCode)
                    {
                        string storageJsonResponse = await storageResponse.Content.ReadAsStringAsync();
                        using JsonDocument storageDoc = JsonDocument.Parse(storageJsonResponse);
                        JsonElement storageRoot = storageDoc.RootElement;

                        long fileSize = 0;
                        string fileFormat = "Not available";
                        string fileType = "Not available";
                        string lastModifiedTime = "Not available";

                        if (storageRoot.TryGetProperty("data", out JsonElement data))
                        {
                            fileSize = data.GetProperty("attributes").GetProperty("size").GetInt64();
                            fileFormat = data.GetProperty("attributes").TryGetProperty("format", out JsonElement format) ? format.GetString() : "Not available";
                            fileType = data.GetProperty("attributes").TryGetProperty("type", out JsonElement type) ? type.GetString() : "Not available";
                            lastModifiedTime = data.GetProperty("attributes").TryGetProperty("lastModifiedTime", out JsonElement modifiedTime) ? modifiedTime.GetString() : "Not available";
                        }

                        versionList.Add((versionNumber, versionID, createTime, createdBy, fileSize, fileFormat, fileType, lastModifiedTime));
                    }
                    else
                    {
                        Console.WriteLine($"❌ Error fetching storage details for version {versionID}.");
                    }
                }

                return versionList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return versionList;
            }
        }








        public async Task<ModelData> GetModelMetadataAsync(string projectId, string itemId)
        {
            try
            {
                string token = TokenManager.GetToken();
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // 🔹 STEP 1: Get item metadata
                    string itemUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}";
                    var itemResponse = await client.GetAsync(itemUrl);
                    string itemJson = await itemResponse.Content.ReadAsStringAsync();
                    dynamic itemData = JsonConvert.DeserializeObject<dynamic>(itemJson);
                    dynamic attributes = itemData?.data?.attributes;

                    string creatorId = attributes?.createUserId;
                    string modifiedById = attributes?.lastModifiedUserId;
                    string folderId = itemData?.data?.relationships?.parent?.data?.id;

                    // 🔹 STEP 2: Get folder name
                    string folderName = "N/A";
                    if (!string.IsNullOrEmpty(folderId))
                    {
                        string folderUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
                        var folderResponse = await client.GetAsync(folderUrl);
                        string folderJson = await folderResponse.Content.ReadAsStringAsync();
                        dynamic folderData = JsonConvert.DeserializeObject<dynamic>(folderJson);
                        folderName = folderData?.data?.attributes?.displayName ?? "Unknown Folder";
                    }

                    // 🔹 STEP: Get creator name safely
                    string creatorName = "N/A";
                    if (!string.IsNullOrEmpty(creatorId))
                    {
                        string userUrl = $"https://developer.api.autodesk.com/userprofile/v1/users/{creatorId}";
                        var userResponse = await client.GetAsync(userUrl);

                        if (!userResponse.IsSuccessStatusCode)
                        {
                            string errorText = await userResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"❌ Failed to get user profile: {userResponse.StatusCode} - {errorText}");
                        }
                        else
                        {
                            string userJson = await userResponse.Content.ReadAsStringAsync();

                            try
                            {
                                dynamic userData = JsonConvert.DeserializeObject<dynamic>(userJson);
                                creatorName = userData?.displayName ?? "Unknown";
                            }
                            catch (JsonReaderException jrex)
                            {
                                Console.WriteLine("❌ JSON parse error (user profile): " + jrex.Message);
                                Console.WriteLine("🔎 Raw content: " + userJson);
                            }
                        }
                    }


                    // 🔹 STEP 6: Build ModelData
                    ModelData data = new ModelData
                    {
                        Id = itemId,
                        Name = attributes?.displayName ?? "N/A",
                        CreatedBy = creatorName,
                        CreatedDate = attributes?.createTime ?? "N/A",
                        ModifiedDate = attributes?.lastModifiedTime ?? "N/A",
                        //ModifiedBy = modifiedById ?? "N/A",
                        ModifiedBy = "N/A",
                        FileSize = itemData?["included"]?[0]?["attributes"]?["storageSize"] ?? 0,
                        Foldername = folderName,
                        Version = attributes?.versionNumber != null ? attributes.versionNumber.ToString() : "Latest Version",
                        Format = attributes?.fileType ?? "N/A",
                        PolyCount = attributes?.polyCount ?? 0,
                        Dimensions = (attributes?.dimensions != null)
                            ? $"{attributes.dimensions.height}cm (H) x {attributes.dimensions.width}cm (W)"
                            : "N/A"
                    };

                    return data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching model metadata: {ex.Message}");
                return null;
            }
        }
        public async Task<ModelData> GetVersionMetadataAsync(string projectId, string versionId)
        {
            Console.WriteLine($"Getting version metadata for project ID: {projectId}, version ID: {versionId}");

            try
            {

                string token = TokenManager.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("❌ Error: Token is missing or invalid.");
                    return null;
                }

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // 🔹 STEP 1: Get version metadata
                    string versionUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/versions/{versionId}";
                    var versionResponse = await client.GetAsync(versionUrl);

                    if (!versionResponse.IsSuccessStatusCode)
                    {
                        // Log the full error response for debugging
                        string errorResponse = await versionResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ API Error: {versionResponse.StatusCode} - {versionResponse.ReasonPhrase}");
                        Console.WriteLine($"Error Response: {errorResponse}");
                        return null;
                    }

                    // 🔹 STEP 2: Read version data
                    string versionJson = await versionResponse.Content.ReadAsStringAsync();
                    dynamic versionData = JsonConvert.DeserializeObject<dynamic>(versionJson);
                    dynamic attributes = versionData?.data?.attributes;

                    if (attributes == null)
                    {
                        Console.WriteLine("❌ Error: No attributes found in version data.");
                        return null;
                    }

                    // Extract metadata values
                    string creatorName = attributes?.createUserName ?? "Unknown";
                    string itemId = versionData?.data?.relationships?.item?.data?.id;
                    string versionNumber = attributes?.versionNumber?.ToString() ?? "N/A";
                    long fileSize = attributes?.storageSize ?? 0;
                    string fileType = attributes?.fileType ?? "N/A";
                    string polyCount = attributes?.polyCount?.ToString() ?? "0";
                    string dimensions = (attributes?.dimensions != null)
                        ? $"{attributes.dimensions.height}cm (H) x {attributes.dimensions.width}cm (W)"
                        : "N/A";
                    string createTime = attributes?.createTime ?? "N/A";
                    string lastModifiedTime = attributes?.lastModifiedTime ?? "N/A";

                    // 🔹 STEP 3: Get item name (Model name) if itemId exists
                    string modelName = "Unknown";
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        string itemUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}";
                        var itemResponse = await client.GetAsync(itemUrl);

                        if (!itemResponse.IsSuccessStatusCode)
                        {
                            string itemErrorResponse = await itemResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"❌ Item API Error: {itemResponse.StatusCode} - {itemResponse.ReasonPhrase}");
                            Console.WriteLine($"Error Response: {itemErrorResponse}");
                            return null;
                        }

                        string itemJson = await itemResponse.Content.ReadAsStringAsync();
                        dynamic itemData = JsonConvert.DeserializeObject<dynamic>(itemJson);
                        modelName = itemData?.data?.attributes?.displayName ?? "Unknown";
                    }

                    // 🔹 STEP 4: Build ModelData object
                    ModelData data = new ModelData
                    {
                        Id = versionId,
                        Name = modelName,
                        CreatedBy = creatorName,
                        CreatedDate = createTime,
                        ModifiedDate = lastModifiedTime,
                        ModifiedBy = "N/A",  // You can add logic for ModifiedBy if needed
                        FileSize = (int)fileSize,
                        Version = versionNumber,
                        Format = fileType,
                        PolyCount = int.TryParse(polyCount, out var count) ? count : 0,
                        Dimensions = dimensions
                    };

                    return data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching version metadata: {ex.Message}");
                return null;
            }
        }
        public static async Task<(int VersionNumber, string VersionID, string CreateTime, string CreatedBy, string StorageURN, string MimeType, string FileSize)> GetVersionMetadata(string versionId, string projectId)
        {
            // Construct the URL that includes both versionId and projectId
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/versions/{versionId}";
            string _accessToken = TokenManager.GetToken();

            if (string.IsNullOrEmpty(_accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing or invalid.");
                return (0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            try
            {
                // Log the URL being used
                Console.WriteLine($"🔗 Requesting URL: {url}");

                // Prepare the client with authorization header
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                // Log the Authorization header
                Console.WriteLine($"🔑 Authorization: Bearer {_accessToken.Substring(0, 20)}..."); // Only show part of the token for security

                // Make the GET request
                HttpResponseMessage response = await client.GetAsync(url);

                // Log the HTTP status code
                Console.WriteLine($"📋 Response Status Code: {response.StatusCode}");

                // Check if the response is not successful
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return (0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                }

                // Read the response content
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // Debug: Print out the raw JSON response for inspection
                Console.WriteLine("📡 Raw JSON Response:");
                Console.WriteLine(jsonResponse);

                // Parse the JSON response
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                // Ensure "data" exists in the root element
                if (!root.TryGetProperty("data", out JsonElement versionData))
                {
                    Console.WriteLine("❌ Error: 'data' not found in the response.");
                    return (0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                }

                // Parse necessary fields from the "data" object
                int versionNumber = versionData.GetProperty("attributes").GetProperty("versionNumber").GetInt32();
                string versionID = versionData.GetProperty("id").GetString();
                string createTime = versionData.GetProperty("attributes").GetProperty("createTime").GetString();
                string createdBy = versionData.GetProperty("attributes").GetProperty("createUserName").GetString();
                string storageURN = versionData.GetProperty("relationships").GetProperty("storage").GetProperty("data").GetProperty("id").GetString();
                string mimeType = versionData.GetProperty("attributes").GetProperty("mimeType").GetString();
                string fileSize = versionData.GetProperty("attributes").GetProperty("fileSize").GetString();

                // Log the retrieved version metadata
                Console.WriteLine($"📄 Found Version Metadata: Number={versionNumber}, ID={versionID}, Created={createTime} by {createdBy}, MimeType={mimeType}, FileSize={fileSize}");

                // Return the version metadata
                return (versionNumber, versionID, createTime, createdBy, storageURN, mimeType, fileSize);
            }
            catch (Exception ex)
            {
                // Log any exceptions for debugging
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // If there's an error with parsing or the request itself, print a helpful message
                return (0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }












    }
}
