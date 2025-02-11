using Autodesk.Forge;
using Autodesk.Forge.Model;
using System.Net.Http;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class ModelUpload
{
    private readonly HttpClient _httpClient = new HttpClient();

    public async Task<string> UploadModel(string filePath, string projectId, string folderId, string accessToken)
    {
        string fileName = Path.GetFileName(filePath);
        
        // 🔹 Step 1: Get an Upload URL from Autodesk (Create Storage Location)
        string storageUrn = await CreateStorageLocation(projectId, folderId, fileName, accessToken);

        // 🔹 Step 2: Upload file to the provided URL
        await UploadToAutodesk(storageUrn, filePath, accessToken);

        // 🔹 Step 3: Create a new item & version in the project folder
        string fileUrn = await CreateItemVersion(projectId, folderId, storageUrn, fileName, accessToken);

        Console.WriteLine($"✅ Model '{fileName}' uploaded successfully.");
        return fileUrn;
    }

    // 🔹 Step 1: Create a Storage Location
    private async Task<string> CreateStorageLocation(string projectId, string folderId, string fileName, string accessToken)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/storage";
        var requestBody = new
        {
            jsonapi = new { version = "1.0" },
            data = new
            {
                type = "objects",
                attributes = new { name = fileName },
                relationships = new
                {
                    target = new
                    {
                        data = new { type = "folders", id = folderId }
                    }
                }
            }
        };

        string json = JsonSerializer.Serialize(requestBody);
        HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("Authorization", $"Bearer {accessToken}");

        HttpResponseMessage response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(responseJson);
        string storageUrn = doc.RootElement.GetProperty("data").GetProperty("id").GetString();

        return storageUrn;
    }

    // 🔹 Step 2: Upload the File to Autodesk Storage
    private async Task UploadToAutodesk(string storageUrn, string filePath, string accessToken)
    {
        string uploadUrl = $"https://developer.api.autodesk.com/oss/v2/buckets/wip.dm.prod/objects/{storageUrn}";

        using FileStream fileStream = new FileStream(filePath, FileMode.Open);
        using var content = new StreamContent(fileStream);
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        content.Headers.Add("Content-Type", "application/octet-stream");

        HttpResponseMessage response = await _httpClient.PutAsync(uploadUrl, content);
        response.EnsureSuccessStatusCode();
    }

    // 🔹 Step 3: Create a New Item & Version in the Project Folder
    private async Task<string> CreateItemVersion(string projectId, string folderId, string storageUrn, string fileName, string accessToken)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items";

        var requestBody = new
        {
            jsonapi = new { version = "1.0" },
            data = new
            {
                type = "items",
                attributes = new { name = fileName },
                relationships = new
                {
                    tip = new { data = new { type = "versions", id = storageUrn } },
                    parent = new { data = new { type = "folders", id = folderId } }
                }
            }
        };

        string json = JsonSerializer.Serialize(requestBody);
        HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        Console.WriteLine(_httpClient.DefaultRequestHeaders.Authorization);
        
        HttpResponseMessage response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(responseJson);
        string fileUrn = doc.RootElement.GetProperty("data").GetProperty("id").GetString();

        return fileUrn;
    }
}

