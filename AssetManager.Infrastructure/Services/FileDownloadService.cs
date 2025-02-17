using System.Net.Http.Headers;
using System.Text.Json;
using AssetManager.Infrastructure.Services;

public class FileDownloadService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task DownloadModel(string projectId, string folderId, string itemId)
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(folderId) || string.IsNullOrEmpty(itemId))
        {
            Console.WriteLine("❌ Error: Missing project, folder, or item ID.");
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

            // 🔹 Step 2: Get the downloadable URL
            string downloadUrl = await GetDownloadUrlAsync(projectId, versionId);
            if (string.IsNullOrEmpty(downloadUrl))
            {
                Console.WriteLine("❌ Error: Could not retrieve download URL.");
                return;
            }

            // 🔹 Step 3: Download the file
            string localFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{itemId}.f3d");
            await DownloadFileAsync(downloadUrl, localFilePath);
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

    private async Task<string> GetDownloadUrlAsync(string projectId, string versionId)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/versions/{versionId}";

        HttpResponseMessage response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        string jsonResponse = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(jsonResponse);

        return doc.RootElement
                  .GetProperty("data")
                  .GetProperty("relationships")
                  .GetProperty("storage")
                  .GetProperty("meta")
                  .GetProperty("link")
                  .GetProperty("href")
                  .GetString();
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
