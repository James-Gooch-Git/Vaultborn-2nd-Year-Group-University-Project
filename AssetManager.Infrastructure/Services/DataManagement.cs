using System;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.Http;
using System.Text;
using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using AssetManager.Infrastructure.Models;


namespace AssetManager.Infrastructure.Services
{
    public class DataManagement
    {
        // Single shared MongoConnection for this class (MongoClient is pooled internally).
        private static readonly MongoConnection _db = new();

        // ---------------------------------------------------------------
        // Shared HTTP helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Sends an authenticated GET request via the shared HttpClient and returns the
        /// response body as a string, or null on failure (with a single logged error).
        /// </summary>
        private static async Task<string> GetStringAsync(string url, string token = null)
        {
            token ??= TokenManager.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("❌ Error: Access token is missing or invalid.");
                return null;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using HttpResponseMessage response = await SharedHttp.Client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: {response.StatusCode} - {response.ReasonPhrase} ({url})");
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sends an authenticated GET request and parses the response as JSON.
        /// Returns null on any failure. Callers should dispose the returned document.
        /// </summary>
        private static async Task<JsonDocument> GetJsonAsync(string url, string token = null)
        {
            string body = await GetStringAsync(url, token);
            if (body == null)
            {
                return null;
            }

            try
            {
                return JsonDocument.Parse(body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred parsing JSON: {ex.Message}");
                return null;
            }
        }

        // ---------------------------------------------------------------
        // Hubs
        // ---------------------------------------------------------------

        public static async Task<string> GetPersonalHub()
        {
            var details = await GetPersonalHubDetails();
            return details?.Item1;
        }

        public static async Task<string> GetProjectIdAsync(string hubID)
        {
            string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubID}/projects";

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return null;
            }

            try
            {
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("data", out JsonElement projects) && projects.GetArrayLength() > 0)
                {
                    string projectId = projects[0].GetProperty("id").GetString();
                    Console.WriteLine($"✅ Project ID Retrieved: {projectId}");
                    return projectId;
                }

                Console.WriteLine("❌ No projects found in this hub.");
                return null;
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

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return null;
            }

            try
            {
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

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return null;
            }

            try
            {
                List<(string ProjectId, string ProjectName)> projects = new List<(string, string)>();

                // Loop through the "data" array to get project information
                foreach (JsonElement project in doc.RootElement.GetProperty("data").EnumerateArray())
                {
                    string projectId = project.GetProperty("id").GetString();
                    string projectName = project.GetProperty("attributes").GetProperty("name").GetString();

                    projects.Add((projectId, projectName));
                }

                return projects;
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

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return (null, null);
            }

            try
            {
                JsonElement root = doc.RootElement;

                // Check if "data" property exists and is not empty
                if (root.TryGetProperty("data", out JsonElement data) && data.GetArrayLength() > 0)
                {
                    // Get the first folder's ID and Name
                    JsonElement firstFolder = data[0];
                    string folderId = firstFolder.GetProperty("id").GetString();
                    string folderName = firstFolder.GetProperty("attributes").GetProperty("displayName").GetString();

                    return (folderId, folderName);
                }

                Console.WriteLine("❌ No top-level folders found.");
                return (null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                return (null, null);
            }
        }

        public static async Task<string> GetParentFolderIdOffolder(string hubID, string projectId, string folderId)
        {
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return null;
            }

            try
            {
                JsonElement root = doc.RootElement;

                // Check if "relationships" -> "parent" exists in the response
                if (root.TryGetProperty("relationships", out JsonElement relationships) &&
                    relationships.TryGetProperty("parent", out JsonElement parent))
                {
                    string parentFolderId = parent.GetProperty("data").GetProperty("id").GetString();
                    return parentFolderId;
                }

                Console.WriteLine("❌ No parent folder found.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                return null;
            }
        }


        public static async Task<List<(string ItemId, string ItemName, bool IsFolder)>> GetProjectItems(string projectId, string folderId)
        {
            ModelService modelService = new ModelService(_db);

            // Fetch all deleted model IDs for this project/folder in one query
            var deletedModelIds = await modelService.GetDeletedModelIds(projectId, folderId);
            Console.WriteLine($"Found {deletedModelIds.Count} deleted models in the database for this folder");

            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}/contents?include=tip";
            var projectItems = new List<(string, string, bool)>();

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return projectItems;
            }

            try
            {
                var root = doc.RootElement;

                // Build version map: versionId → (extensionType, storageId)
                Dictionary<string, (string ExtensionType, string StorageId)> versionMap = new();

                if (root.TryGetProperty("included", out JsonElement includedArray))
                {
                    foreach (JsonElement included in includedArray.EnumerateArray())
                    {
                        if (included.GetProperty("type").GetString() == "versions")
                        {
                            string versionId = included.GetProperty("id").GetString();
                            string extensionType = null;
                            string storageId = null;

                            if (included.TryGetProperty("attributes", out JsonElement attr) &&
                                attr.TryGetProperty("extension", out JsonElement ext) &&
                                ext.TryGetProperty("type", out JsonElement extType))
                            {
                                extensionType = extType.GetString();
                            }

                            if (included.TryGetProperty("relationships", out JsonElement relationships) &&
                                relationships.TryGetProperty("storage", out JsonElement storage) &&
                                storage.TryGetProperty("data", out JsonElement storageData) &&
                                storageData.TryGetProperty("id", out JsonElement storageIdElement))
                            {
                                storageId = storageIdElement.GetString();
                            }

                            if (!string.IsNullOrEmpty(extensionType) && !string.IsNullOrEmpty(storageId))
                            {
                                versionMap[versionId] = (extensionType, storageId);
                            }
                        }
                    }
                }

                // Loop through folder contents
                foreach (JsonElement item in root.GetProperty("data").EnumerateArray())
                {
                    string itemId = item.GetProperty("id").GetString();
                    string itemName = item.GetProperty("attributes").GetProperty("displayName").GetString();
                    bool isFolder = item.GetProperty("type").GetString() == "folders";

                    if (isFolder)
                    {
                        projectItems.Add((itemId, itemName, true));
                        continue;
                    }

                    // Check if the model is marked as deleted in MongoDB
                    if (deletedModelIds.Contains(itemId))
                    {
                        Console.WriteLine($"🧹 Skipping '{itemName}' (ID: {itemId}) — marked as deleted in database.");
                        continue;
                    }

                    // For items, find the tip version
                    string tipVersionId = null;
                    if (item.TryGetProperty("relationships", out JsonElement relationships) &&
                        relationships.TryGetProperty("tip", out JsonElement tip) &&
                        tip.TryGetProperty("data", out JsonElement tipData))
                    {
                        tipVersionId = tipData.GetProperty("id").GetString();
                    }

                    if (string.IsNullOrEmpty(tipVersionId) || !versionMap.ContainsKey(tipVersionId))
                    {
                        Console.WriteLine($"🧹 Skipping '{itemName}' — no valid tip version or missing storage.");
                        continue;
                    }

                    var (extensionType, storageId) = versionMap[tipVersionId];

                    // Only allow models with supported extensions
                    if (!extensionType.StartsWith("versions:autodesk.") && !extensionType.Contains("fusion"))
                    {
                        Console.WriteLine($"⚠️ Skipping '{itemName}' — unsupported extension type: {extensionType}");
                        continue;
                    }

                    // Passed all checks — add the model
                    projectItems.Add((itemId, itemName, false));
                }

                return projectItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return projectItems;
            }
        }

        public static async Task<List<(string ItemId, string ItemName)>> GetItemsInFolder(string projectId, string folderId)
        {
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}/contents";

            string jsonResponse = await GetStringAsync(url);
            if (jsonResponse == null)
            {
                return null;
            }

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

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return null;
            }

            try
            {
                List<(string ItemId, string ItemName, string ItemType)> items = new List<(string, string, string)>();

                // Extract Item IDs, Names, and Types
                foreach (JsonElement item in doc.RootElement.GetProperty("data").EnumerateArray())
                {
                    if (item.GetProperty("type").GetString() == "items")
                    {
                        string itemId = item.GetProperty("id").GetString();
                        string itemName = item.GetProperty("attributes").GetProperty("displayName").GetString();
                        string itemType = item.GetProperty("type").GetString();

                        items.Add((itemId, itemName, itemType));
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                return null;
            }
        }


        //Creates new folder in a specified project
        public static async Task<string> CreateNewFolder(string projectId, string parentFolderId, string folderName)
        {
            try
            {
                var result = await GetPersonalHubDetails();
                (string hubID, string HubName, string HubType) = result.Value;

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
                                    id = parentFolderId
                                }
                            }
                        }
                    }
                };

                string json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders";

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

                using var response = await SharedHttp.Client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: Failed to create folder. Status: {response.StatusCode}");
                    return null;
                }

                // Parse the response to get the folder ID
                using JsonDocument doc = JsonDocument.Parse(responseContent);
                string folderId = doc.RootElement.GetProperty("data").GetProperty("id").GetString();

                return folderId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception while creating folder: {ex.Message}");
                return null;
            }
        }

        // ---------------------------------------------------------------
        // Thumbnails — public variants all delegate to FetchThumbnailUrl.
        // ---------------------------------------------------------------

        public static async Task<string> GetLatestItemThumbnail(string projectId, string itemId, string encodedUrn)
        {
            return await GetThumbnailCoreAsync(encodedUrn, projectId, itemId, null);
        }

        public static async Task<string> GetVersionThumbnail(string projectId, string itemId, string encodedUrn, string versionId)
        {
            return await GetThumbnailCoreAsync(encodedUrn, projectId, itemId, versionId);
        }

        private static async Task<string> GetThumbnailCoreAsync(string encodedUrn, string projectId, string itemId, string versionId)
        {
            string accessToken = TokenManager.GetToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("❌ No valid access token.");
                return null;
            }

            try
            {
                return await FetchThumbnailUrl(encodedUrn, accessToken, projectId, itemId, versionId);
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

            using var request = new HttpRequestMessage(HttpMethod.Get, thumbnailUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using HttpResponseMessage response = await SharedHttp.Client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Thumbnail not found or model not translated yet: {response.StatusCode}");
                return null;
            }

            var _models = _db.GetCollection("ModelData");

            byte[] imageData = await response.Content.ReadAsByteArrayAsync();

            // Convert the image to a Base64 string (for storage in MongoDB)
            string base64Image = Convert.ToBase64String(imageData);

            // Use versionId if provided, otherwise fall back to itemId
            string filterId = string.IsNullOrEmpty(versionId) ? itemId : versionId;

            // Update the existing model document using projectId & filterId (either itemId or versionId)
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_folderid", projectId),
                Builders<BsonDocument>.Filter.Eq("_id", filterId)
            );

            var update = Builders<BsonDocument>.Update.Set("thumbnail_url", thumbnailUrl).Set("thumbnail_base64", base64Image);

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

            return thumbnailUrl;
        }



        public static async Task<List<(string HubID, string HubName, string HubType)>> GetAllHubs()
        {
            string url = "https://developer.api.autodesk.com/project/v1/hubs";
            var hubList = new List<(string, string, string)>();

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return hubList;
            }

            try
            {
                foreach (JsonElement hub in doc.RootElement.GetProperty("data").EnumerateArray())
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

        // ---------------------------------------------------------------
        // Version metadata — all public variants delegate to the private
        // FetchItemVersionsAsync / VersionInfo core below and project out
        // their own return shapes.
        // ---------------------------------------------------------------

        /// <summary>Metadata for one version of an item, parsed once and shared.</summary>
        private sealed class VersionInfo
        {
            public int VersionNumber;
            public string VersionId;
            public string DisplayName;
            public string CreateTime;
            public string CreatedBy;
            public string ModifiedBy = "Not available";
            public string ModifiedTime = "Not available";
            public string FileType = "Not available";
            public long StorageSize;
            public string StorageId;
        }

        /// <summary>Parses a single "versions" JSON element into a VersionInfo.</summary>
        private static VersionInfo ParseVersionInfo(JsonElement version)
        {
            var info = new VersionInfo { VersionId = version.GetProperty("id").GetString() };

            if (version.TryGetProperty("attributes", out JsonElement attr))
            {
                if (attr.TryGetProperty("versionNumber", out var vn)) info.VersionNumber = vn.GetInt32();
                if (attr.TryGetProperty("displayName", out var dn)) info.DisplayName = dn.GetString();
                if (attr.TryGetProperty("createTime", out var ct)) info.CreateTime = ct.GetString();
                if (attr.TryGetProperty("createUserName", out var cu)) info.CreatedBy = cu.GetString();
                if (attr.TryGetProperty("lastModifiedUserName", out var mu)) info.ModifiedBy = mu.GetString();
                if (attr.TryGetProperty("lastModifiedTime", out var mt)) info.ModifiedTime = mt.GetString();
                if (attr.TryGetProperty("fileType", out var ft)) info.FileType = ft.GetString();
                if (attr.TryGetProperty("storageSize", out var ss)) info.StorageSize = ss.GetInt64();
            }

            if (version.TryGetProperty("relationships", out JsonElement relationships) &&
                relationships.TryGetProperty("storage", out JsonElement storage) &&
                storage.TryGetProperty("data", out JsonElement storageData) &&
                storageData.TryGetProperty("id", out JsonElement storageIdElement))
            {
                info.StorageId = storageIdElement.GetString();
            }

            return info;
        }

        /// <summary>
        /// Fetches the full version list for an item once. Returns null on failure.
        /// </summary>
        private static async Task<List<VersionInfo>> FetchItemVersionsAsync(string projectId, string itemId)
        {
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions";

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return null;
            }

            try
            {
                var versions = new List<VersionInfo>();

                if (doc.RootElement.TryGetProperty("data", out JsonElement versionsArray))
                {
                    foreach (JsonElement versionElement in versionsArray.EnumerateArray())
                    {
                        versions.Add(ParseVersionInfo(versionElement));
                    }
                }

                return versions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetches the storage metadata (size/format/type/lastModified) for a version.
        /// Returns null on failure.
        /// </summary>
        private static async Task<(long FileSize, string FileFormat, string FileType, string LastModifiedTime)?> FetchVersionStorageMetadataAsync(string projectId, string itemId, string versionId)
        {
            string storageUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions/{versionId}/storage";

            using JsonDocument storageDoc = await GetJsonAsync(storageUrl);
            if (storageDoc == null)
            {
                return null;
            }

            long fileSize = 0;
            string fileFormat = "Not available";
            string fileType = "Not available";
            string lastModifiedTime = "Not available";

            if (storageDoc.RootElement.TryGetProperty("data", out JsonElement data))
            {
                fileSize = data.GetProperty("attributes").GetProperty("size").GetInt64();
                fileFormat = data.GetProperty("attributes").TryGetProperty("format", out JsonElement format) ? format.GetString() : "Not available";
                fileType = data.GetProperty("attributes").TryGetProperty("type", out JsonElement type) ? type.GetString() : "Not available";
                lastModifiedTime = data.GetProperty("attributes").TryGetProperty("lastModifiedTime", out JsonElement modifiedTime) ? modifiedTime.GetString() : "Not available";
            }

            return (fileSize, fileFormat, fileType, lastModifiedTime);
        }

        public async Task<List<(string versionId, string versionName, string storageId)>> GetVersionsForItemAsync(string projectId, string itemId)
        {
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(itemId))
            {
                Console.WriteLine("❌ Error: Project ID or Item ID is missing.");
                return null;
            }

            Console.WriteLine($"🔍 Fetching versions for Item: {itemId}");

            var versionInfos = await FetchItemVersionsAsync(projectId, itemId);
            if (versionInfos == null)
            {
                return null;
            }

            var versions = new List<(string versionId, string versionName, string storageId)>();
            foreach (var v in versionInfos)
            {
                versions.Add((v.VersionId, v.DisplayName, v.StorageId));
            }
            return versions;
        }

        public static async Task<List<(int VersionNumber, string VersionID, string CreateTime, string CreatedBy, string StorageURN)>> GetItemVersions(string projectId, string itemId)
        {
            var versionList = new List<(int, string, string, string, string)>();

            var versionInfos = await FetchItemVersionsAsync(projectId, itemId);
            if (versionInfos == null)
            {
                return versionList;
            }

            foreach (var v in versionInfos)
            {
                Console.WriteLine($"📄 Found Version: Number={v.VersionNumber}, ID={v.VersionId}, Created={v.CreateTime} by {v.CreatedBy}");
                versionList.Add((v.VersionNumber, v.VersionId, v.CreateTime, v.CreatedBy, v.StorageId));
            }

            return versionList;
        }

        public static async Task<(int VersionNumber, string VersionID, string CreateTime, string CreatedBy, long FileSize, string FileFormat, string FileType, string LastModifiedTime)> GetItemVersionMetadata(string projectId, string itemId, string versionId)
        {
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions/{versionId}";
            var versionMetadata = (0, string.Empty, string.Empty, string.Empty, 0L, "Not available", "Not available", "Not available");

            Console.WriteLine($"🔗 Fetching version metadata from URL: {url}");

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return versionMetadata;
            }

            try
            {
                Console.WriteLine($"✅ Successfully fetched version metadata for projectId: {projectId}, itemId: {itemId}, versionId: {versionId}");

                var v = ParseVersionInfo(doc.RootElement.GetProperty("data"));

                // Now fetch the storage details to get the file size and additional metadata
                var storageMeta = await FetchVersionStorageMetadataAsync(projectId, itemId, v.VersionId);

                if (storageMeta.HasValue)
                {
                    var (fileSize, fileFormat, fileType, lastModifiedTime) = storageMeta.Value;

                    Console.WriteLine($"📄 Found Version: Number={v.VersionNumber}, ID={v.VersionId}, Created={v.CreateTime} by {v.CreatedBy}, File Size={fileSize} bytes, Format={fileFormat}, Type={fileType}, Last Modified={lastModifiedTime}");

                    versionMetadata = (v.VersionNumber, v.VersionId, v.CreateTime, v.CreatedBy, fileSize, fileFormat, fileType, lastModifiedTime);
                }
                else
                {
                    Console.WriteLine($"❌ Error fetching storage details for version {v.VersionId}.");
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
            var versionList = new List<(int, string, string, string, long, string, string, string)>();

            var versionInfos = await FetchItemVersionsAsync(projectId, itemId);
            if (versionInfos == null)
            {
                return versionList;
            }

            try
            {
                foreach (var v in versionInfos)
                {
                    // Fetch the storage details for each version to get file size and extra metadata
                    var storageMeta = await FetchVersionStorageMetadataAsync(projectId, itemId, v.VersionId);

                    if (storageMeta.HasValue)
                    {
                        var (fileSize, fileFormat, fileType, lastModifiedTime) = storageMeta.Value;
                        versionList.Add((v.VersionNumber, v.VersionId, v.CreateTime, v.CreatedBy, fileSize, fileFormat, fileType, lastModifiedTime));
                    }
                    else
                    {
                        Console.WriteLine($"❌ Error fetching storage details for version {v.VersionId}.");
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

        public static async Task<ModelData> GetModelVersionMetadata(string projectId, string itemId, string targetVersionId = null)
        {
            var versionInfos = await FetchItemVersionsAsync(projectId, itemId);
            if (versionInfos == null)
            {
                return null;
            }

            try
            {
                // When no specific version is requested, keep the last entry in the
                // response (matches the original selection behaviour).
                VersionInfo selected = targetVersionId == null
                    ? versionInfos.LastOrDefault()
                    : versionInfos.FirstOrDefault(v => v.VersionId == targetVersionId);

                if (selected == null)
                {
                    Console.WriteLine("❌ Could not find the requested version.");
                    return null;
                }

                string createdDate = selected.CreateTime;
                string modifiedDate = selected.ModifiedTime;

                // Format the dates to show "YYYY-MM-DD HH:mm:ss"
                if (DateTime.TryParse(createdDate, out DateTime parsedCreatedDate))
                {
                    createdDate = parsedCreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                }
                if (DateTime.TryParse(modifiedDate, out DateTime parsedModifiedDate))
                {
                    modifiedDate = parsedModifiedDate.ToString("yyyy-MM-dd HH:mm:ss");
                }

                string folderName = "Not available";
                int polyCount = 0; // If available in derivatives, would be separate call
                string dimensions = "Not available"; // Likewise

                return new ModelData
                {
                    Version = selected.VersionNumber.ToString(),
                    Name = selected.DisplayName,
                    CreatedBy = selected.CreatedBy,
                    CreatedDate = createdDate,
                    ModifiedBy = selected.ModifiedBy,
                    ModifiedDate = modifiedDate,
                    Format = selected.FileType,
                    FileSize = (int)selected.StorageSize,
                    Foldername = folderName,
                    PolyCount = polyCount,
                    Dimensions = dimensions
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception while fetching model metadata: {ex.Message}");
                return null;
            }
        }

        public async Task<ModelData> GetModelMetadataAsync(string projectId, string itemId)
        {
            try
            {
                // STEP 1: Get item metadata
                string itemUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}";
                string itemJson = await GetStringAsync(itemUrl);
                if (itemJson == null)
                {
                    return null;
                }

                dynamic itemData = JsonConvert.DeserializeObject<dynamic>(itemJson);
                dynamic attributes = itemData?.data?.attributes;

                string creatorId = attributes?.createUserId;
                string modifiedById = attributes?.lastModifiedUserId;
                string folderId = itemData?.data?.relationships?.parent?.data?.id;

                // STEP 2: Get folder name
                string folderName = "N/A";
                if (!string.IsNullOrEmpty(folderId))
                {
                    string folderUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
                    string folderJson = await GetStringAsync(folderUrl);
                    if (folderJson != null)
                    {
                        dynamic folderData = JsonConvert.DeserializeObject<dynamic>(folderJson);
                        folderName = folderData?.data?.attributes?.displayName ?? "Unknown Folder";
                    }
                }

                // STEP 3: Get creator name safely
                string creatorName = "N/A";
                if (!string.IsNullOrEmpty(creatorId))
                {
                    string userUrl = $"https://developer.api.autodesk.com/userprofile/v1/users/{creatorId}";
                    string userJson = await GetStringAsync(userUrl);

                    if (userJson != null)
                    {
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

                // STEP 4: Build ModelData
                ModelData data = new ModelData
                {
                    Id = itemId,
                    Name = attributes?.displayName ?? "N/A",
                    CreatedBy = creatorName,
                    CreatedDate = attributes?.createTime ?? "N/A",
                    ModifiedDate = attributes?.lastModifiedTime ?? "N/A",
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
                // STEP 1: Get version metadata
                string versionUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/versions/{versionId}";
                string versionJson = await GetStringAsync(versionUrl);
                if (versionJson == null)
                {
                    return null;
                }

                // STEP 2: Read version data
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
                int polyCount = 0;
                string dimensions = (attributes?.dimensions != null)
                    ? $"{attributes.dimensions.height}cm (H) x {attributes.dimensions.width}cm (W)"
                    : "N/A";
                string createTime = attributes?.createTime ?? "N/A";
                string lastModifiedTime = attributes?.lastModifiedTime ?? "N/A";

                // STEP 3: Get item name (Model name) if itemId exists
                string modelName = "Unknown";
                if (!string.IsNullOrEmpty(itemId))
                {
                    string itemUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}";
                    string itemJson = await GetStringAsync(itemUrl);
                    if (itemJson == null)
                    {
                        return null;
                    }

                    dynamic itemData = JsonConvert.DeserializeObject<dynamic>(itemJson);
                    modelName = itemData?.data?.attributes?.displayName ?? "Unknown";
                }

                // STEP 4: Build ModelData object
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
                    PolyCount = polyCount,
                    Dimensions = dimensions
                };

                return data;
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

            Console.WriteLine($"🔗 Requesting URL: {url}");

            using JsonDocument doc = await GetJsonAsync(url);
            if (doc == null)
            {
                return (0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            try
            {
                // Ensure "data" exists in the root element
                if (!doc.RootElement.TryGetProperty("data", out JsonElement versionData))
                {
                    Console.WriteLine("❌ Error: 'data' not found in the response.");
                    return (0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                }

                // Parse the common fields, then the fields specific to this endpoint
                var v = ParseVersionInfo(versionData);
                string mimeType = versionData.GetProperty("attributes").GetProperty("mimeType").GetString();
                string fileSize = versionData.GetProperty("attributes").GetProperty("fileSize").GetString();

                Console.WriteLine($"📄 Found Version Metadata: Number={v.VersionNumber}, ID={v.VersionId}, Created={v.CreateTime} by {v.CreatedBy}, MimeType={mimeType}, FileSize={fileSize}");

                return (v.VersionNumber, v.VersionId, v.CreateTime, v.CreatedBy, v.StorageId, mimeType, fileSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception occurred: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return (0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }

        public async Task<string> GetModelName(string modelId, string projectId = null)
        {
            // Step 1: Try MongoDB first
            var modelData = await _db.ModelData.Find(x => x.Id == modelId).FirstOrDefaultAsync();

            if (modelData != null && !string.IsNullOrEmpty(modelData.Name))
            {
                return modelData.Name;
            }

            Console.WriteLine("🔎 Model name not found in Mongo. Attempting to fetch from Autodesk Hub...");

            // Step 2: Try Autodesk Hub
            try
            {
                if (string.IsNullOrEmpty(projectId))
                {
                    Console.WriteLine("⚠️ projectId is required to get model name from the Hub.");
                    return "Unknown Name";
                }

                string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{modelId}";

                using JsonDocument doc = await GetJsonAsync(url);
                if (doc == null)
                {
                    return "Unknown Name";
                }

                JsonElement data = doc.RootElement.GetProperty("data");

                if (data.TryGetProperty("attributes", out JsonElement attributes) &&
                    attributes.TryGetProperty("displayName", out JsonElement displayNameElement))
                {
                    return displayNameElement.GetString() ?? "Unknown Name";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception fetching model name from Hub: {ex.Message}");
            }

            return "Unknown Name";
        }
    }
}
