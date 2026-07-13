using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AssetManager.Infrastructure.Services
{
    public class DeleteService
    {
        private readonly FileDownloadService _fileService = new FileDownloadService();
        private readonly MongoConnection _mongo = new();

        public async Task<bool> DeleteModelAsync(string projectId, string itemId, string folderId)
        {
            Console.WriteLine($"🗑️ Deleting Model from Project: {projectId}, Item: {itemId}, Folder: {folderId}");

            try
            {
                // Step 1: Attempt to delete the actual object from OSS (optional)
                string storageId = await _fileService.GetStorageIdFromItem(projectId, itemId);
                if (!string.IsNullOrEmpty(storageId))
                {
                    var (bucketKey, objectKey) = _fileService.ExtractBucketAndObjectKeys(storageId);
                    if (!string.IsNullOrEmpty(bucketKey) && !string.IsNullOrEmpty(objectKey))
                    {
                        string url = $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}";
                        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

                        var deleteResponse = await SharedHttp.Client.SendAsync(request);
                        Console.WriteLine(deleteResponse.IsSuccessStatusCode
                            ? $"✅ Deleted OSS object: {objectKey}"
                            : $"❌ Failed OSS delete: {deleteResponse.StatusCode} - {deleteResponse.ReasonPhrase}");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ No storage ID found — skipping OSS delete.");
                }
                var modelService = new ModelService(_mongo);
                var result = await modelService.SoftDeleteModel(itemId);

                // ✅ Step 2: Soft delete in MongoDB
           
                if (!result)
                {
                    Console.WriteLine("❌ Failed to update MongoDB isDeleted flag.");
                    return false;
                }

                Console.WriteLine("✅ Model soft-deleted via MongoDB.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DeleteService Exception: {ex.Message}");
                return false;
            }
        }


    }
}
