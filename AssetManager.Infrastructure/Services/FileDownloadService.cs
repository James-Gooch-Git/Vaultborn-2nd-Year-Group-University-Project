using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using AssetManager.Infrastructure.Services;

public class FileDownloadService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task DownloadModel(string projectId, string itemId)
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(itemId))
        {
            Console.WriteLine("❌ Error: Missing project or item ID.");
            return;
        }

        try
        {
            // ✅ Step 1: Get the direct download URL
            string downloadUrl = await GetSignedDownloadUrl(projectId, itemId);

            if (string.IsNullOrEmpty(downloadUrl))
            {
                Console.WriteLine("❌ Error: Could not retrieve download URL.");
                return;
            }

            Console.WriteLine($"✅ Direct Download URL retrieved: {downloadUrl}");

            // ✅ Step 2: Determine local file path
            string fileName = downloadUrl.Split('/').Last();
            string localFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);

            // ✅ Step 3: Download the file
            await DownloadFileAsync(downloadUrl, localFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception occurred while downloading: {ex.Message}");
        }
    }
    

    /*private async Task DownloadFileAsync(string downloadUrl, string localFilePath)
    {
        Console.WriteLine($"📥 Downloading file to: {localFilePath}");

        try
        {
            using HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error downloading file. Status Code: {response.StatusCode}");
                return;
            }

            await using FileStream fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
            await response.Content.CopyToAsync(fileStream);

            Console.WriteLine($"✅ File downloaded successfully: {localFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during file download: {ex.Message}");
        }
    }*/


    /*
    private async Task DownloadFileAsync(string downloadUrl, string localFilePath)
    {
        Console.WriteLine($"📥 Downloading file to: {localFilePath}");

        try
        {
            using HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error downloading file. Status Code: {response.StatusCode}");
                return;
            }

            await using FileStream fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
            await response.Content.CopyToAsync(fileStream);

            Console.WriteLine($"✅ File downloaded successfully: {localFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during file download: {ex.Message}");
        }
    }
    */

   
    /*private async Task DownloadFileAsync(string downloadUrl, string localFilePath)
    {
        Console.WriteLine($"📥 Downloading file from: {downloadUrl}");

        HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error downloading file: {response.StatusCode}");
            return;
        }

        await using FileStream fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
        await response.Content.CopyToAsync(fileStream);

        Console.WriteLine($"✅ File downloaded successfully to: {localFilePath}");
    }*/

   
    /*private async Task<string> GetStorageIdFromVersion(string projectId, string versionId)
    {
        if (string.IsNullOrEmpty(versionId))
        {
            Console.WriteLine("❌ Error: Version ID is missing.");
            return null;
        }

        // ✅ Step 1: Remove Query Parameters (`?version=1`)
        int queryIndex = versionId.IndexOf("?");
        if (queryIndex != -1)
        {
            versionId = versionId.Substring(0, queryIndex);
        }

        // ✅ Ensure correct `fs.file:vf.` format for version API
        if (!versionId.StartsWith("urn:adsk.wipprod:fs.file:vf."))
        {
            Console.WriteLine("❌ Error: Version ID is not in the correct `fs.file:vf.` format.");
            return null;
        }

        Console.WriteLine($"🔍 Cleaned & Formatted Version ID: {versionId}");

        // ✅ Step 3: Fetch Storage ID from Autodesk API
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/versions/{versionId}";
        Console.WriteLine($"🔍 Fetching Storage ID from Version: {url}");

        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

        HttpResponseMessage response = await httpClient.GetAsync(url);
        string jsonResponse = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"📩 API Response: {jsonResponse}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error retrieving version details. Status Code: {response.StatusCode}");
            return null;
        }

        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        JsonElement root = doc.RootElement;

        // ✅ Step 4: Extract Storage ID from API Response
        if (!root.TryGetProperty("data", out JsonElement dataElement) ||
            !dataElement.TryGetProperty("relationships", out JsonElement relationships) ||
            !relationships.TryGetProperty("storage", out JsonElement storage) ||
            !storage.TryGetProperty("data", out JsonElement storageData) ||
            !storageData.TryGetProperty("id", out JsonElement storageIdElement))
        {
            Console.WriteLine("❌ Error: Storage ID not found in version details.");
            return null;
        }

        string storageId = storageIdElement.GetString();
        Console.WriteLine($"✅ Successfully Retrieved Storage ID: {storageId}");

        return storageId;
    }*/
   private async Task<string> GetDirectDownloadUrl(string projectId, string itemId)
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(itemId))
        {
            Console.WriteLine("❌ Error: Project ID or Item ID is missing.");
            return null;
        }

        // ✅ Fetch the latest version of the item
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions";
        Console.WriteLine($"🔍 Fetching latest version: {url}");

        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

        HttpResponseMessage response = await httpClient.GetAsync(url);
        string jsonResponse = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"📩 API Response: {jsonResponse}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error retrieving version details. Status Code: {response.StatusCode}");
            return null;
        }

        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        JsonElement root = doc.RootElement;

        // ✅ Extract the "storage.meta.link.href" (Direct Download Link)
        if (!root.TryGetProperty("data", out JsonElement dataArray) || dataArray.GetArrayLength() == 0)
        {
            Console.WriteLine("❌ Error: No versions found.");
            return null;
        }

        JsonElement latestVersion = dataArray[0];

        if (latestVersion.TryGetProperty("relationships", out JsonElement relationships) &&
            relationships.TryGetProperty("storage", out JsonElement storage) &&
            storage.TryGetProperty("meta", out JsonElement meta) &&
            meta.TryGetProperty("link", out JsonElement link) &&
            link.TryGetProperty("href", out JsonElement href))
        {
            string downloadUrl = href.GetString();
            Console.WriteLine($"✅ Direct Download URL: {downloadUrl}");
            return downloadUrl;
        }

        Console.WriteLine("❌ Error: Could not find the direct download link.");
        return null;
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
    public async Task<string> GetStorageIdFromItem(string projectId, string itemId)
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(itemId))
        {
            Console.WriteLine("❌ Error: Project ID or Item ID is missing.");
            return null;
        }

        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}";
        Console.WriteLine($"🔍 Fetching Storage ID from Item: {url}");

        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

        HttpResponseMessage response = await httpClient.GetAsync(url);
        string jsonResponse = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"📩 API Response: {jsonResponse}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error retrieving item details. Status Code: {response.StatusCode}");
            return null;
        }

        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        JsonElement root = doc.RootElement;

        // ✅ Ensure "storage" relationship exists in the item details
        if (!root.TryGetProperty("data", out JsonElement dataElement) ||
            !dataElement.TryGetProperty("relationships", out JsonElement relationships) ||
            !relationships.TryGetProperty("tip", out JsonElement tip) ||
            !tip.TryGetProperty("data", out JsonElement tipData) ||
            !tipData.TryGetProperty("id", out JsonElement versionIdElement))
        {
            Console.WriteLine("❌ Error: Could not retrieve latest version ID.");
            return null;
        }

        string latestVersionId = versionIdElement.GetString();
        Console.WriteLine($"✅ Latest Version ID: {latestVersionId}");

        // 🔹 Now retrieve the storage location from the latest version
        return await GetDirectDownloadUrl(projectId, itemId);
    }


    // 🔹 Step 1: Get latest version ID of the item
    private async Task<(string versionId, string correctedItemId)> GetLatestVersionId(string projectId, string itemId)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions";
        Console.WriteLine($"🔍 Fetching Versions: {url}");

        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

        HttpResponseMessage response = await httpClient.GetAsync(url);
        string jsonResponse = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"📩 API Response: {jsonResponse}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error retrieving versions. Status Code: {response.StatusCode}");
            return (null, null);
        }

        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("data", out JsonElement dataArray) || dataArray.GetArrayLength() == 0)
        {
            Console.WriteLine("❌ Error: No versions found for the selected item.");
            return (null, null);
        }

        // ✅ Extract latest version's ID (fs.file:vf.)
        JsonElement latestVersion = dataArray[0];
        string versionId = latestVersion.GetProperty("id").GetString();

        // ✅ Extract the correct "dm.lineage" item ID
        string extractedItemId = latestVersion.GetProperty("relationships")
            .GetProperty("item")
            .GetProperty("data")
            .GetProperty("id")
            .GetString();

        Console.WriteLine($"✅ Latest Version ID: {versionId}");
        Console.WriteLine($"✅ Corrected Item ID: {extractedItemId}");

        return (versionId, extractedItemId);
    }



    // 🔹 Step 2: Get storage ID from the item
   

    // 🔹 Step 3: Extract bucket and object keys
    private (string bucketKey, string objectKey) ExtractBucketAndObjectKeys(string storageId)
    {
        if (!storageId.StartsWith("urn:adsk.objects:os.object:"))
        {
            Console.WriteLine("❌ Invalid storage ID format.");
            return (null, null);
        }

        string[] parts = storageId.Replace("urn:adsk.objects:os.object:", "").Split('/');
        string bucketKey = parts[0];
        string objectKey = string.Join("/", parts.Skip(1)); // Handles nested objects

        Console.WriteLine($"📂 Extracted Bucket Key: {bucketKey}");
        Console.WriteLine($"📄 Extracted Object Key: {objectKey}");

        return (bucketKey, objectKey);
    }

    // 🔹 Step 4: Get signed download URL
    private async Task<string> GetSignedDownloadUrl(string bucketKey, string objectKey)
    {
        string url = $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}/signeds3download";
        Console.WriteLine($"🔍 Fetching Signed URL: {url}");

        HttpResponseMessage response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error retrieving signed URL. Status Code: {response.StatusCode}");
            return null;
        }

        string jsonResponse = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        string signedUrl = doc.RootElement.GetProperty("url").GetString();

        Console.WriteLine($"✅ Signed Download URL: {signedUrl}");
        return signedUrl;
    }
    
    
    private async Task DownloadFileAsync(string downloadUrl, string localFilePath)
    {
        Console.WriteLine($"📥 Downloading file from: {downloadUrl}");

        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());

        HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error downloading file: {response.StatusCode} - {response.ReasonPhrase}");
            string errorResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"📩 API Error Response: {errorResponse}");
            return;
        }

        await using FileStream fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
        await response.Content.CopyToAsync(fileStream);

        Console.WriteLine($"✅ File downloaded successfully to: {localFilePath}");
    }


    // 🔹 Step 5: Download the file
    


}
