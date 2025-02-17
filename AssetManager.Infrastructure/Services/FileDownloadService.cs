namespace AssetManager.Infrastructure.Services;

public class FileDownloadService
{
    public static async Task DownloadFileAsync(string accessToken, string bucketName, string itemId, string localFilePath)
    {
       using var httpClient = new HttpClient();
       httpClient.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

       string url = $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketName}/objects/{itemId}";

       HttpResponseMessage response = await httpClient.GetAsync(url);
       if (response.IsSuccessStatusCode)
       {
        await using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
        await response.Content.CopyToAsync(fileStream);
        Console.WriteLine($"File downloaded to {localFilePath}");
       }
       else
       {
        Console.WriteLine($"Error: {response.StatusCode}");
       }
    }
}
