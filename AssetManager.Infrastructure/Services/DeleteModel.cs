using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AssetManager.Infrastructure.Services
{
    public class DeleteService
    {
        private readonly FileDownloadService _fileService = new FileDownloadService();

        public async Task<bool> DeleteModelAsync(string projectId, string itemId, string folderId)
        {
            Console.WriteLine($"🗑️ Deleting Model from Project: {projectId}, Item: {itemId}, Folder: {folderId}");

            try
            {
                // Step 1: Get the storage ID
                string storageId = await _fileService.GetStorageIdFromItem(projectId, itemId);
                if (string.IsNullOrEmpty(storageId))
                {
                    Console.WriteLine("⚠️ No storage ID found — skipping OSS delete.");
                }
                else
                {
                    // Step 2: Extract OSS keys & delete the actual object
                    var (bucketKey, objectKey) = _fileService.ExtractBucketAndObjectKeys(storageId);
                    if (!string.IsNullOrEmpty(bucketKey) && !string.IsNullOrEmpty(objectKey))
                    {
                        string url = $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}";
                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

                        HttpResponseMessage deleteResponse = await client.DeleteAsync(url);
                        Console.WriteLine(deleteResponse.IsSuccessStatusCode
                            ? $"✅ Deleted OSS object: {objectKey}"
                            : $"❌ Failed OSS delete: {deleteResponse.StatusCode} - {deleteResponse.ReasonPhrase}");
                    }
                }

                // Step 3: Move item to Archived folder
                bool archived = await MoveItemToArchiveAsync(projectId, itemId, folderId);
                if (!archived)
                {
                    Console.WriteLine("❌ Failed to move item to Archived folder.");
                    return false;
                }

                // Step 4 (optional): Update MongoDB (pseudo)
                // await _mongo.UpdateModelFlag(itemId, new { Archived = true });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DeleteService Exception: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> MoveItemToArchiveAsync(string projectId, string itemId, string folderId, string archiveFolderName = "Archived")
        {
            try
            {
                string accessToken = TokenManager.GetToken();

                // Step 1: Get the Storage ID from the latest version
                string storageId = await _fileService.GetStorageIdFromItem(projectId, itemId);
                if (string.IsNullOrEmpty(storageId))
                {
                    Console.WriteLine("❌ Could not retrieve storage ID.");
                    return false;
                }

                // Step 2: Get or create Archive folder
                string archiveFolderId = await EnsureArchiveFolderExists(projectId, folderId, archiveFolderName);
                if (string.IsNullOrEmpty(archiveFolderId))
                {
                    Console.WriteLine("❌ Failed to get or create Archive folder.");
                    return false;
                }

                // Step 3: Get original file name
                DataManagement dataService = new DataManagement();
                string fileName = await dataService.GetModelName(itemId, projectId);

                // Step 4: Create a new item in the archive folder
                var requestBody = new
                {
                    data = new
                    {
                        type = "items",
                        attributes = new
                        {
                            displayName = fileName,
                            extension = new
                            {
                                type = "items:autodesk.bim360:File",
                                version = "1.0"
                            }
                        },
                        relationships = new
                        {
                            tip = new
                            {
                                data = new
                                {
                                    type = "versions",
                                    id = storageId
                                }
                            },
                            parent = new
                            {
                                data = new
                                {
                                    type = "folders",
                                    id = archiveFolderId
                                }
                            }
                        }
                    }
                };

                string json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/vnd.api+json");

                string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Moved item to Archive folder.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Move failed: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in MoveItemToArchiveAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<string> EnsureArchiveFolderExists(string projectId, string parentFolderId, string archiveFolderName)
        {
            string accessToken = TokenManager.GetToken();
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{parentFolderId}/contents";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                if (item.GetProperty("type").GetString() == "folders" &&
                    item.GetProperty("attributes").GetProperty("displayName").GetString() == archiveFolderName)
                {
                    return item.GetProperty("id").GetString();
                }
            }

            // Not found: create folder
            var requestBody = new
            {
                data = new
                {
                    type = "folders",
                    attributes = new
                    {
                        name = archiveFolderName,
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

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/vnd.api+json");
            var createUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders";

            var createResponse = await client.PostAsync(createUrl, content);
            var createJson = await createResponse.Content.ReadAsStringAsync();
            var createDoc = JsonDocument.Parse(createJson);

            return createDoc.RootElement.GetProperty("data").GetProperty("id").GetString();
        }


    }
}
