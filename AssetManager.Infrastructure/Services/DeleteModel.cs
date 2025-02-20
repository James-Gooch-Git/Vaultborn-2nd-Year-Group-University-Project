using System.Net.Http.Headers;
using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.Models;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.Services;

public class DeleteModel
{
    static readonly string baseApiUrl = "https://developer.api.autodesk.com";
    MongoConnection database = new MongoConnection();
    static string accessToken = TokenManager.GetToken();
    
    //usage
    //DeleteModel _delMod = new ()
    //bool isDeleted = await _delMod.DeleteModelAsync("your_project_id", "your_model_item_id");

    public async Task<bool> DeleteModelAsync(string projectId, string itemId)
    {
        string deleteUrl = $"{baseApiUrl}/data/v1/projects/{projectId}/items/{itemId}";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            HttpResponseMessage response = await client.DeleteAsync(deleteUrl);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ Model {itemId} deleted successfully.");
                await DeleteModelFromDatabaseAsync(itemId);
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Failed to delete model: {await response.Content.ReadAsStringAsync()}");
                return false;
            }
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