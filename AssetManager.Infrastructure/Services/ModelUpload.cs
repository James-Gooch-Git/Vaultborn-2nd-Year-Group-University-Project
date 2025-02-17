using Autodesk.Forge;
using Autodesk.Forge.Model;
using System.Net.Http;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using AssetManager.Infrastructure.Services;



public class ModelUpload2
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly string _accessToken; // ✅ Store the token as a class property
    

    public ModelUpload2(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new ArgumentException("❌ Error: Access token cannot be null or empty.", nameof(accessToken));
        }
        
        _accessToken = accessToken; // ✅ Set the token when the class is instantiated
    }
    /*public async Task<string> UploadModel(string filePath, string projectId, string folderId)
    {
        string fileName = Path.GetFileName(filePath);
        Console.WriteLine($"🔹 Debug: Upload started for {fileName}");

        if (string.IsNullOrEmpty(folderId))
        {
            Console.WriteLine("❌ Error: Folder ID is missing before attempting to upload.");
            return null;
        }

        // Step 1: Get an Upload URL from Autodesk
        string storageUrn = await CreateStorageLocation(projectId, folderId, fileName);
    
        if (string.IsNullOrEmpty(storageUrn))
        {
            Console.WriteLine("❌ Error: Failed to create storage location (Missing Storage URN)");
            return null;
        }

        Console.WriteLine($"✅ Storage URN retrieved: {storageUrn}");

        // Step 2: Upload file to the signed URL
        bool uploadSuccess = await UploadToAutodesk(storageUrn, filePath);
    
        if (!uploadSuccess)
        {
            Console.WriteLine("❌ Error: File upload to Autodesk failed");
            return null;
        }

        Console.WriteLine("✅ File uploaded successfully");

        // Step 3: Create a new item & version in the project folder
        string fileUrn = await CreateItemVersion(projectId, folderId, storageUrn, fileName, _accessToken);

        if (string.IsNullOrEmpty(fileUrn))
        {
            Console.WriteLine("❌ Error: Failed to generate file URN");
            return null;
        }

        Console.WriteLine($"✅ Upload completed successfully. File URN: {fileUrn}");
        return fileUrn;
    
    }*/
    /*private async Task<string> GetOrCreateFolderAsync(string projectId, string accessToken)
    {
        try
        {
            Console.WriteLine($"🔹 Debug: Checking for existing folders in project {projectId}");

            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await client.GetAsync(url);
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"🔹 Debug: Folder API Response: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error: Failed to retrieve existing folders. Status Code: {response.StatusCode}");
                return null;
            }

            using JsonDocument doc = JsonDocument.Parse(responseContent);

            // 🔹 Search for an existing folder called "MyModels"
            foreach (JsonElement folder in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                string folderName = folder.GetProperty("attributes").GetProperty("name").GetString();
                string folderId = folder.GetProperty("id").GetString();

                if (folderName == "MyModels")
                {
                    Console.WriteLine($"✅ Found existing folder: {folderName} (ID: {folderId})");
                    return folderId; // ✅ Return the found folder ID
                }
            }

            // 🔹 If no "MyModels" folder was found, create a new one
            Console.WriteLine("⚠️ No 'MyModels' folder found. Creating one...");
            return await CreateNewFolder(projectId, accessToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception while retrieving or creating folder: {ex.Message}");
            return null;
        }
    }*/
    /*private async Task<string> CreateNewFolder(string projectId, string accessToken)
        {
            try
            {
                Console.WriteLine("🔹 Debug: Creating a new folder...");

                string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders";

                // Retrieve the default storage location or parent folder
                string parentFolderId = await GetDefaultFolderIdAsync(projectId, accessToken);
                if (string.IsNullOrEmpty(parentFolderId))
                {
                    Console.WriteLine("❌ Error: No valid parent folder found for folder creation.");
                    return null;
                }

                var requestBody = new
                {
                    jsonapi = new { version = "1.0" },
                    data = new
                    {
                        type = "folders",
                        attributes = new
                        {
                            name = "MyModels",
                            extension = new
                            {
                                type = "folders:autodesk.bim360:Folder",
                                version = "1.0"
                            }
                        },
                        relationships = new
                        {
                            parent = new
                            {
                                data = new
                                {
                                    type = "folders",
                                    id = parentFolderId // ✅ Ensure a valid parent ID
                                }
                            }
                        }
                    }
                };

                string json = JsonSerializer.Serialize(requestBody);
                using var client = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Debug: Folder Creation Response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error: Failed to create folder. Status: {response.StatusCode}");
                    return null;
                }

                using JsonDocument doc = JsonDocument.Parse(responseContent);
                return doc.RootElement.GetProperty("data").GetProperty("id").GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception while creating folder: {ex.Message}");
                return null;
            }
        }*/
    /*
    private async Task<string> GetDefaultFolderIdAsync(string projectId, string accessToken)
    {
        try
        {
            Console.WriteLine($"🔹 Debug: Retrieving top-level folder ID for project {projectId}");

            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/topFolders";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await client.GetAsync(url);
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"🔹 Debug: Folder API Response: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error: Failed to retrieve Folder ID. Status Code: {response.StatusCode}");
                return null;
            }

            using JsonDocument doc = JsonDocument.Parse(responseContent);
            string folderId = doc.RootElement.GetProperty("data")[0].GetProperty("id").GetString();

            Console.WriteLine($"✅ Debug: Retrieved Folder ID: {folderId}");
            return folderId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception while retrieving folder ID: {ex.Message}");
            return null;
        }
    }
    public static async Task<string> GetFolderIdAsync(string projectId, string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("❌ Error: Access token is missing.");
                return null;
            }

            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/topFolders";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await client.GetAsync(url);
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"🔹 Debug: Folder API Response: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error: Failed to retrieve Folder ID. Status Code: {response.StatusCode}");
                return null;
            }

            using JsonDocument doc = JsonDocument.Parse(responseContent);

            // ✅ Ensure there is data before accessing
            if (!doc.RootElement.TryGetProperty("data", out JsonElement dataArray) || dataArray.GetArrayLength() == 0)
            {
                Console.WriteLine("❌ Error: No folders found in this project.");
                return null;
            }

            string folderId = dataArray[0].GetProperty("id").GetString();
            Console.WriteLine($"✅ Debug: Retrieved Folder ID: {folderId}");
            return folderId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception while fetching folder ID: {ex.Message}");
            return null;
        }
    }
    */

    /*
    public static async Task<string> CreateFolderAsync(string projectId, string folderName, string accessToken)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders";

        var requestBody = new
        {
            jsonapi = new { version = "1.0" },
            data = new
            {
                type = "folders",
                attributes = new { name = folderName },
                relationships = new
                {
                    parent = new
                    {
                        data = new { type = "folders", id = "wip" } // ✅ Using `wip` as parent folder
                    }
                }
            }
        };

        string json = JsonSerializer.Serialize(requestBody);
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        Console.WriteLine($"🔹 Debug: Creating folder '{folderName}' in project {projectId}");
        HttpResponseMessage response = await client.SendAsync(request);
        string responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"🔹 Debug: Response from Autodesk: {responseContent}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error: Failed to create folder. {response.StatusCode}");
            return null;
        }

        using JsonDocument doc = JsonDocument.Parse(responseContent);
        string folderId = doc.RootElement.GetProperty("data").GetProperty("id").GetString();

        Console.WriteLine($"✅ New Folder Created: {folderName} (ID: {folderId})");
        return folderId;
    }
    */







    // 🔹 Step 1: Create a Storage Location
    /*private async Task<string> CreateStorageLocation(string projectId, string folderId, string fileName)
    {
        string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/storage";

        if (string.IsNullOrEmpty(_accessToken))
        {
            Console.WriteLine("❌ Error: Access token is missing.");
            return null;
        }

        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(folderId))
        {
            Console.WriteLine("❌ Error: Project ID or Folder ID is missing.");
            return null;
        }

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
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        Console.WriteLine($"🔹 Debug: Sending request to {url}");
        Console.WriteLine($"🔹 Debug: Request Body: {json}");

        HttpResponseMessage response = await client.SendAsync(request);
        string responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"🔹 Debug: Response from Autodesk: {responseContent}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error: Failed to create storage location. {response.StatusCode}");
            return null;
        }

        using JsonDocument doc = JsonDocument.Parse(responseContent);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString();
    }*/





    // 🔹 Step 2: Upload the File to Autodesk Storage
    private async Task<bool> UploadToAutodesk(string storageUrn, string filePath)
    {
        string uploadUrl = $"https://developer.api.autodesk.com/oss/v2/buckets/wip.dm.prod/objects/{storageUrn}";

        using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        Console.WriteLine($"🔹 Debug: Uploading file to {uploadUrl}");

        HttpResponseMessage response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("✅ File uploaded successfully!");
            return true;
        }

        string errorResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"❌ Upload failed: {response.StatusCode} - {errorResponse}");
        return false;
    }



    // 🔹 Step 3: Create a New Item & Version in the Project Folder
    private async Task<string> CreateItemVersion(string projectId, string folderId, string storageUrn, string fileName, string _accessToken)
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
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        Console.WriteLine($"🔹 Debug: Requesting file version creation at {url}");
        HttpResponseMessage response = await _httpClient.SendAsync(request);

        string responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"🔹 Debug: Response from Autodesk: {responseContent}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Error: Failed to create file version. {response.StatusCode}");
            return null;
        }

        using JsonDocument doc = JsonDocument.Parse(responseContent);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetString();
    }

}
