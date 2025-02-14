using System;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AssetManager.Infrastructure.Services;
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
                    string type = hub.GetProperty("attributes").GetProperty("extension").GetProperty("type").GetString();
                    string hubId = hub.GetProperty("id").GetString();
                    string hubName = hub.GetProperty("attributes").GetProperty("name").GetString();

                    Console.WriteLine($"🔍 Found hub type: {type}, ID: {hubId}, Name: {hubName}");

                    // Store the first available hub
                    if (selectedHubId == null)
                    {
                        selectedHubId = hubId;
                        selectedHubType = type;
                    }

                    // Prioritize Personal Hub if available
                    if (type == "hubs:autodesk.a360:PersonalHub")
                    {
                        Console.WriteLine($"✅ Selected Personal Hub: {hubId}");
                        return hubId;
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

        public static async Task<List<Project>> GetProjectsAsync(string hubId)
        {
            List<Project> projects = new List<Project>();

            try
            {
                // ✅ Ensure Hub ID is valid
                if (string.IsNullOrEmpty(hubId))
                {
                    Console.WriteLine("❌ Error: Hub ID is null or empty.");
                    return projects;
                }

                string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubId}/projects";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());
            
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                        return projects;
                    }

                    string responseData = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseData);

                    foreach (var item in jsonResponse.data)
                    {
                        projects.Add(new Project
                        {
                            Id = item.id,
                            Name = item.attributes.name
                        });
                    }
                }

                Console.WriteLine($"✅ Retrieved {projects.Count} projects.");
                return projects;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in GetProjectsAsync: {ex.Message}");
                return projects;
            }
        }
        
        


        public class Project
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> GetProjectIdAsync(string hubId)
        {
            string accessToken = TokenManager.GetToken();
    
            try
            {
                string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubId}/projects";

                // Set up request headers
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Send request
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();  // Ensure the request succeeded
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
                    string hubType = hub.GetProperty("attributes").GetProperty("extension").GetProperty("type").GetString();
                    string hubId = hub.GetProperty("id").GetString();
                    string hubName = hub.GetProperty("attributes").GetProperty("name").GetString();

                    Console.WriteLine($"🔍 Found hub type: {hubType}, ID: {hubId}, Name: {hubName}");

                    // Store the first available hub
                    if (selectedHubId == null)
                    {
                        selectedHubId = hubId;
                        selectedHubType = hubType;
                        selectedHubName = hubName;
                    }

                    // Prioritize Personal Hub if available
                    if (hubType == "hubs:autodesk.a360:PersonalHub")
                    {
                        Console.WriteLine($"✅ Selected Personal Hub: {hubId}");
                        return (hubId, hubName, hubType);
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
        public static async Task<List<(string ProjectId, string ProjectName)>> GetAllProjectsFromHub(string hubId)
            {
                string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubId}/projects";
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

                        List<(string ProjectId, string ProjectName)> projects = new List<(string, string)>(); // List of tuples

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
        public static async Task<List<(string FolderId, string FolderName)>> GetTopLevelFolders(string hubId, string projectId)
    {
        string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubId}/projects/{projectId}/topFolders";
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

                List<(string FolderId, string FolderName)> folders = new List<(string, string)>();

                // Extract Folder IDs and Names
                foreach (JsonElement folder in root.GetProperty("data").EnumerateArray())
                {
                    string folderId = folder.GetProperty("id").GetString();
                    string folderName = folder.GetProperty("attributes").GetProperty("displayName").GetString();
                    folders.Add((folderId, folderName));
                }

                return folders;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
            return null;
        }
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

                    List<(string ItemId, string ItemName, string ItemType)> items = new List<(string, string, string)>();

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

        
        
    }
}

