using System.Net.Http.Headers;
using System.Text;
using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.Models;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.Services;

public class DeleteModel
{
    static readonly string baseApiUrl = "https://developer.api.autodesk.com";
    MongoConnection database = new MongoConnection();
    static string accessToken = TokenManager.GetToken();

    public async Task<bool> DeleteModelAsync(string projectId, string itemId)
    {
      if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(accessToken))
    {
        Console.WriteLine("❌ Invalid parameters. Project ID, Item ID, and Access Token are required.");
        return false;
    }

    string url = $"https://developer.api.autodesk.com/data/v1/projects/{Uri.EscapeDataString(projectId)}/versions";
    Console.WriteLine($"🔍 Requesting: {url}");  // Debug URL

    string jsonPayload = $@"
    {{
        ""jsonapi"": {{ ""version"": ""1.0"" }},
        ""data"": {{
            ""type"": ""versions"",
            ""attributes"": {{
                ""extension"": {{
                    ""type"": ""versions:autodesk.core:Deleted"",
                    ""version"": ""1.0""
                }}
            }},
            ""relationships"": {{
                ""item"": {{
                    ""data"": {{
                        ""type"": ""items"",
                        ""id"": ""{itemId}""
                    }}
                }}
            }}
        }}
    }}";

    try
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.api+json");

            HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/vnd.api+json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Model deleted successfully.");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Failed to delete model: {responseBody}");
                return false;
            }
        }
    }
    catch (HttpRequestException httpEx)
    {
        Console.WriteLine($"❌ HTTP Request Error: {httpEx.Message}");
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception occurred: {ex.Message}");
        return false;
    }
    }
    
    
     public async Task<bool> DeleteModelFromDatabaseAsync(string itemId)
     {
         try
         {
             var collection = database.Models;
    
             var filter = MongoDB.Driver.Builders<Model>.Filter.Eq(m => m.ItemId, itemId);
             var result = await collection.DeleteOneAsync(filter);
    
             if (result.DeletedCount > 0)
             {
                 Console.WriteLine($"✅ Model with ItemId {itemId} deleted from MongoDB.");
                 return true;
             }
             else
             {
                 Console.WriteLine($"⚠️ Model with ItemId {itemId} not found in MongoDB.");
                 return false;
             }
         }
         catch (Exception ex)
         {
             Console.WriteLine($"❌ Error deleting model from MongoDB: {ex.Message}");
             return false;
         }
    }
}