namespace AssetManager.Infrastructure.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public class FileDownloadService
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
            string storageId = await GetStorageIdFromItem(projectId, itemId);
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


    public async Task DownloadModelAndSaveMetadata(string _selectedProjectId, string _selectedItemId, string _selectedItemName, string _selectedFolderId)
    {
        await DownloadModelAsync(_selectedProjectId, _selectedItemId);

        string saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DownloadedModels");
        string modelPath = Path.Combine(saveDirectory, _selectedItemName); // Model file path

        string metadataPath = modelPath + ".metadata.json";

        // Write metadata
        var metadata = new
        {
            projectId = _selectedProjectId,
            folderId = _selectedFolderId,
            itemId = _selectedItemId,
            itemName = _selectedItemName
        };

        File.WriteAllText(metadataPath, System.Text.Json.JsonSerializer.Serialize(metadata));

        Console.WriteLine($"✅ Model metadata saved: {metadataPath}");
    }

    /*    private async Task<string> GetStorageIdFromItem(string projectId, string itemId, string accessToken)
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
        }*/

    public async Task<string> GetStorageIdFromItem(string projectId, string itemId)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}";
        Console.WriteLine($"🔍 Fetching Storage ID from: {url}");

        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

        HttpResponseMessage response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error retrieving storage ID. Status Code: {response.StatusCode}");
            return null;
        }

        string jsonResponse = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(jsonResponse);


        // ✅ Extract storage ID
        string storageId = doc.RootElement
            .GetProperty("included")[0]
            .GetProperty("relationships")
            .GetProperty("storage")
            .GetProperty("data")
            .GetProperty("id")
            .GetString();

        Console.WriteLine($"📂 Storage ID: {storageId}");
        return storageId;
    }

    public async Task<string> GetStorageIdFromVersion(string projectId, string versionId)
    {
        // ✅ URL-encode versionId to avoid API errors
        string encodedVersionId = Uri.EscapeDataString(versionId);

        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/versions/{encodedVersionId}";
        Console.WriteLine($"🔍 Fetching Storage ID for Version: {versionId} from {url}");

        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

        HttpResponseMessage response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error retrieving storage ID. Status Code: {response.StatusCode}");
            return null;
        }

        string jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"🔹 API Response: {jsonResponse}"); // Debugging output

        using JsonDocument doc = JsonDocument.Parse(jsonResponse);

        JsonElement root = doc.RootElement.GetProperty("data");

        // ✅ Ensure "relationships" and "storage" exist before accessing "id"
        if (root.TryGetProperty("relationships", out JsonElement relationships) &&
            relationships.TryGetProperty("storage", out JsonElement storage) &&
            storage.TryGetProperty("data", out JsonElement data) &&
            data.TryGetProperty("id", out JsonElement idElement))
        {
            string storageId = idElement.GetString();
            Console.WriteLine($"📂 Storage ID for Version {versionId}: {storageId}");
            return storageId;
        }

        Console.WriteLine("❌ No storage ID found for this version.");
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

    private string RemoveInvalidFileNameChars(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Where(c => !invalidChars.Contains(c)));
    }
    
 
}


