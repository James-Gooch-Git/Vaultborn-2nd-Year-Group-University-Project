using System.Net.Http.Headers;
using System.Text.Json;

namespace AssetManager.Infrastructure.Services;

public class FileDownloadService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public static async Task DownloadFileAsync(string accessToken, string projectId, string folderId, string itemId, string localFilePath)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // 🔹 Step 1: Get Versions for the Item (to retrieve the latest version ID)
        string versionsUrl = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}/versions";
        HttpResponseMessage versionsResponse = await httpClient.GetAsync(versionsUrl);
        versionsResponse.EnsureSuccessStatusCode();
        string versionsJson = await versionsResponse.Content.ReadAsStringAsync();
        
        using JsonDocument versionsDoc = JsonDocument.Parse(versionsJson);
        string versionId = versionsDoc.RootElement
            .GetProperty("data")[0]
            .GetProperty("id")
            .GetString();

        // 🔹 Step 2: Get the Storage Location (Download URL)
        string downloadUrl = await GetDownloadUrlAsync(accessToken, projectId, versionId);
        if (string.IsNullOrEmpty(downloadUrl))
        {
            Console.WriteLine("❌ Failed to get the download URL.");
            return;
        }

        // 🔹 Step 3: Download the file
        HttpResponseMessage fileResponse = await httpClient.GetAsync(downloadUrl);
        if (fileResponse.IsSuccessStatusCode)
        {
            await using FileStream fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
            await fileResponse.Content.CopyToAsync(fileStream);
            Console.WriteLine($"✅ File downloaded to: {localFilePath}");
        }
        else
        {
            Console.WriteLine($"❌ Error downloading file: {fileResponse.StatusCode}");
        }
    }

    private static async Task<string> GetDownloadUrlAsync(string accessToken, string projectId, string versionId)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/versions/{versionId}";

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        HttpResponseMessage response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string jsonResponse = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(jsonResponse);
        string downloadUrl = doc.RootElement
            .GetProperty("data")
            .GetProperty("relationships")
            .GetProperty("storage")
            .GetProperty("meta")
            .GetProperty("link")
            .GetProperty("href")
            .GetString();

        return downloadUrl;
    }
}

