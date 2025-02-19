namespace AssetManager.Infrastructure.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public class FileDownloadService2
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task DownloadModelAsync(string projectId, string itemId)
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(itemId))
        {
            Console.WriteLine("❌ Error: Project ID or Item ID is missing.");
            return;
        }

        try
        {
            string accessToken = TokenManager.GetToken();

            // ✅ Step 1: Retrieve Storage ID
            string storageId = await GetStorageIdFromItem(projectId, itemId, accessToken);
            if (string.IsNullOrEmpty(storageId))
            {
                Console.WriteLine("❌ Could not retrieve storage ID.");
                return;
            }

            // ✅ Step 2: Extract Bucket and Object Keys
            var (bucketKey, objectKey) = ExtractBucketAndObjectKeys(storageId);
            if (string.IsNullOrEmpty(bucketKey) || string.IsNullOrEmpty(objectKey))
            {
                Console.WriteLine("❌ Error: Invalid bucket or object key.");
                return;
            }

            // ✅ Step 3: Fetch Signed Download URL
            string signedUrl = await GetSignedDownloadUrl(bucketKey, objectKey, accessToken);
            if (string.IsNullOrEmpty(signedUrl))
            {
                Console.WriteLine("❌ Failed to retrieve signed URL.");
                return;
            }

            // ✅ Step 4: Retrieve Correct Filename
            string fileName = await GetItemFileNameAsync(projectId, itemId, accessToken);
            fileName = RemoveInvalidFileNameChars(fileName);

            // ✅ Step 5: Define Save Location
            string saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DownloadedModels");
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            // ✅ Step 6: Download the File
            await DownloadFileAsync(signedUrl, saveDirectory, fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error downloading model: {ex.Message}");
        }
    }

    private async Task<string> GetStorageIdFromItem(string projectId, string itemId, string accessToken)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        string jsonResponse = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        JsonElement root = doc.RootElement;

        if (root.TryGetProperty("included", out JsonElement includedArray) && includedArray.GetArrayLength() > 0)
        {
            return includedArray[0]
                .GetProperty("relationships")
                .GetProperty("storage")
                .GetProperty("data")
                .GetProperty("id")
                .GetString();
        }

        return null;
    }

    private (string bucketKey, string objectKey) ExtractBucketAndObjectKeys(string storageId)
    {
        if (string.IsNullOrEmpty(storageId) || !storageId.StartsWith("urn:adsk.objects:os.object:"))
        {
            return (null, null);
        }

        string[] parts = storageId.Replace("urn:adsk.objects:os.object:", "").Split('/');
        if (parts.Length < 2) return (null, null);

        return (parts[0], string.Join("/", parts.Skip(1)));
    }

    private async Task<string> GetSignedDownloadUrl(string bucketKey, string objectKey, string accessToken)
    {
        string url = $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}/signeds3download";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        string jsonResponse = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        return doc.RootElement.GetProperty("url").GetString();
    }

    private async Task<string> GetItemFileNameAsync(string projectId, string itemId, string accessToken)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return "DownloadedModel.obj";

        string jsonResponse = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        JsonElement root = doc.RootElement;

        if (root.TryGetProperty("data", out JsonElement dataElement) &&
            dataElement.TryGetProperty("attributes", out JsonElement attributesElement) &&
            attributesElement.TryGetProperty("displayName", out JsonElement displayNameElement))
        {
            return displayNameElement.GetString() ?? "DownloadedModel.obj";
        }

        return "DownloadedModel.obj";
    }

    private async Task DownloadFileAsync(string signedUrl, string saveDirectory, string fileName)
    {
        using var client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(signedUrl);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Download failed: {response.StatusCode}");
            return;
        }

        string filePath = Path.Combine(saveDirectory, fileName);
        byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(filePath, fileBytes);

        Console.WriteLine($"✅ File downloaded successfully: {filePath}");
    }

    private string RemoveInvalidFileNameChars(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Where(c => !invalidChars.Contains(c)));
    }
}


