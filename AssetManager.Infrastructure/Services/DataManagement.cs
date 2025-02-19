using System;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AssetManager.Infrastructure.Services;
using System.Text;
using Newtonsoft.Json;


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
                    string type = hub.GetProperty("attributes").GetProperty("extension").GetProperty("type")
                        .GetString();
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

        //Gets a list of Folder IDs and Folder Names for all Top-level folders from a specific project 
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

        public static async Task<List<(string ItemId, string ItemName)>> GetItemsInFolder(string projectId,
            string folderId)
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
        /*public static async Task<string> CreateNewFolder(string projectId, string accessToken, string folderName)
        {
            try
            {
                Console.WriteLine("🔹 Debug: Creating a new folder...");

                string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders";
                string _accessToken = TokenManager.GetToken();
                var result = await GetPersonalHubDetails();
                (string hubID, string HubName, string HubType) = result.Value;

                // Retrieve the default storage location or parent folder
                var (parentFolderId, parentFolderName) = await GetTopLevelFolder(hubID, projectId);
                
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
        }*/


    }
}

