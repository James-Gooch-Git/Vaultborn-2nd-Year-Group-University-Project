using System.Net.Http.Headers;
using System.Text.Json;
using AssetManager.Infrastructure.Services;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public class FileDownloadService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task DownloadModel(string projectId, string folderId, string itemId)
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(itemId))
        {
            Console.WriteLine("❌ Error: Missing project or item ID.");
            return;
        }

        string accessToken = TokenManager.GetToken();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            // 🔹 Step 1: Retrieve the latest version ID for the selected item
            string versionId = await GetLatestVersionId(projectId, itemId);
            if (string.IsNullOrEmpty(versionId))
            {
                Console.WriteLine("❌ Error: Failed to retrieve the latest version ID.");
                return;
            }

            // 🔹 Step 2: Get the signed download URL
            string signedDownloadUrl = await GetSignedDownloadUrl(projectId, versionId);
            if (string.IsNullOrEmpty(signedDownloadUrl))
            {
                Console.WriteLine("❌ Error: Could not retrieve signed download URL.");
                return;
            }

            // 🔹 Step 3: Download the file
            string localFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{itemId}.f3d");
            await DownloadFileAsync(signedDownloadUrl, localFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception occurred while downloading: {ex.Message}");
        }
    }

    private async Task<string> GetLatestVersionId(string projectId, string itemId)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions";

        HttpResponseMessage response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        string jsonResponse = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        
        return doc.RootElement
                  .GetProperty("data")[0]
                  .GetProperty("id")
                  .GetString();
    }

    private async Task<string> GetSignedDownloadUrl(string projectId, string versionId)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/versions/{versionId}";

        HttpResponseMessage response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        string jsonResponse = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(jsonResponse);

        // 🔹 Extract Storage ID
        string storageId = doc.RootElement
                              .GetProperty("data")
                              .GetProperty("relationships")
                              .GetProperty("storage")
                              .GetProperty("data")
                              .GetProperty("id")
                              .GetString();

        if (string.IsNullOrEmpty(storageId))
        {
            Console.WriteLine("❌ Error: Storage ID not found.");
            return null;
        }

        Console.WriteLine($"📌 Storage ID: {storageId}");
        Console.WriteLine($"🔍 Full API Response: {jsonResponse}");

        // 🔹 Call Autodesk OSS API to get signed URL
        return await GetSignedUrlFromStorage(storageId);
    }

    private async Task<string> GetSignedUrlFromStorage(string storageId)
    {
        string url = $"https://developer.api.autodesk.com/oss/v2/signedresources/{storageId}";

        // 🔹 POST request instead of GET
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.GetToken());
    
        HttpResponseMessage response = await httpClient.SendAsync(request);
        string jsonResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error fetching signed URL: {response.StatusCode} - {jsonResponse}");
            return null;
        }

        Console.WriteLine($"✅ Signed URL Response: {jsonResponse}");

        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        return doc.RootElement.GetProperty("url").GetString();
    }


    private async Task DownloadFileAsync(string downloadUrl, string localFilePath)
    {
        HttpResponseMessage response = await httpClient.GetAsync(downloadUrl);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error downloading file: {response.StatusCode}");
            return;
        }

        await using FileStream fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
        await response.Content.CopyToAsync(fileStream);
        Console.WriteLine($"✅ Model downloaded to: {localFilePath}");
    }
}
