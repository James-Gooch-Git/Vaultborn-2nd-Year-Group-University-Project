using System.Net.Http.Headers;
using System.Text.Json;
using AssetManager.Infrastructure.Services;
//using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Http;









public class FileDownloadService
{
    private static readonly HttpClient httpClient = new HttpClient();

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

    // 🔹 Step 3: Extract bucket and object keys
    public (string bucketKey, string objectKey) ExtractBucketAndObjectKeys(string storageId)
    {
        if (string.IsNullOrEmpty(storageId) || !storageId.StartsWith("urn:adsk.objects:os.object:"))
        {
            Console.WriteLine("❌ Invalid storage ID format.");
            return (null, null);
        }

        // ✅ Remove prefix and split
        string[] parts = storageId.Replace("urn:adsk.objects:os.object:", "").Split('/');
        if (parts.Length < 2)
        {
            Console.WriteLine("❌ Error: Unable to extract bucket and object key.");
            return (null, null);
        }

        string bucketKey = parts[0];  // First part is the bucket key
        string objectKey = string.Join("/", parts.Skip(1)); // Remaining is the object key

        Console.WriteLine($"📂 Bucket Key: {bucketKey}");
        Console.WriteLine($"📄 Object Key: {objectKey}");

        return (bucketKey, objectKey);
    }

    public async Task<string> GetSignedDownloadUrl(string bucketKey, string objectKey, string accessToken)
    {
        string url = $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}/signeds3download";
    
        Console.WriteLine($"🔍 Fetching Signed URL from: {url}");

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await client.GetAsync(url); // GET request

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error retrieving signed URL. Status Code: {response.StatusCode}");
                Console.WriteLine($"❌ Response: {await response.Content.ReadAsStringAsync()}");
                return null;
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            string signedUrl = doc.RootElement.GetProperty("url").GetString();

            Console.WriteLine($"✅ Signed Download URL retrieved: {signedUrl}");
            return signedUrl;
        }
    }



   
    


    





   


  




    



    public async Task DownloadFileAsync(string signedUrl, string saveDirectory, string fileName)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(signedUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Download failed: {response.StatusCode}");
                    return;
                }

                // ✅ Ensure filename is valid
                fileName = RemoveInvalidFileNameChars(fileName);

                // ✅ Create full file save path
                string savePath = Path.Combine(saveDirectory, fileName);
                Console.WriteLine($"📂 Saving to: {savePath}");

                // ✅ Download and save the file
                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(savePath, fileBytes);

                Console.WriteLine($"✅ File downloaded successfully: {savePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception occurred while downloading: {ex.Message}");
        }
    }

    /// ✅ Function to Remove Invalid Characters from File Name
    public string RemoveInvalidFileNameChars(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Where(c => !invalidChars.Contains(c)));
    }


    public async Task<string> GetItemFileNameAsync(string projectId, string itemId, string accessToken)
    {
        try
        {
            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/items/{itemId}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Failed to retrieve item details: {response.StatusCode}");
                    return "DownloadedModel.obj"; // Default fallback filename
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                using (JsonDocument jsonDoc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = jsonDoc.RootElement;
                
                    // ✅ Extract the filename (displayName)
                    if (root.TryGetProperty("data", out JsonElement dataElement) &&
                        dataElement.TryGetProperty("attributes", out JsonElement attributesElement) &&
                        attributesElement.TryGetProperty("displayName", out JsonElement displayNameElement))
                    {
                        string fileName = displayNameElement.GetString();
                        return !string.IsNullOrEmpty(fileName) ? fileName : "DownloadedModel.obj";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception retrieving file name: {ex.Message}");
        }

        return "DownloadedModel.obj"; // Default fallback filename
    }




    /*private async Task DownloadModelAsync(string projectId, string itemId)
    {
        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(itemId))
        {
            Console.WriteLine("❌ Error: Missing project or item ID.");
            return;
        }

        try
        {
            FileDownloadService fileDownloadService = new FileDownloadService();
            string accessToken = TokenManager.GetToken(); // Replace with your actual token retrieval method

            // ✅ Step 1: Retrieve Storage ID
            string storageId = await fileDownloadService.GetStorageIdFromItem(projectId, itemId);
            if (string.IsNullOrEmpty(storageId))
            {
                Console.WriteLine("❌ Error: Could not retrieve storage ID.");
                return;
            }
            var (bucketKey, objectKey) = ExtractBucketAndObjectKeys(storageId);
            string accessTokens =  TokenManager.GetToken(); 
            // ✅ Step 2: Get the signed download URL
            string downloadUrl = await fileDownloadService.GetSignedDownloadUrl(bucketKey, objectKey,accessTokens );
            if (string.IsNullOrEmpty(downloadUrl))
            {
                Console.WriteLine("❌ Error: Could not retrieve signed download URL.");
                return;
            }

            // ✅ Step 3: Determine local file path
            string fileName = downloadUrl.Split('/').Last();
            string localFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);

            // ✅ Step 4: Download the file
            await fileDownloadService.DownloadFileAsync(downloadUrl, localFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception occurred while downloading: {ex.Message}");
        }
    }*/




    // 🔹 Step 5: Download the file
    


}
