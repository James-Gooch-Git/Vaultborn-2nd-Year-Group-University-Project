using AssetManager.Infrastructure.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.Data;

public class ModelService
{
    private readonly IMongoCollection<Model> _models;

    public ModelService(MongoConnection mongoConnection)
    {
        _models = mongoConnection.Models;
    }

    public async Task<bool> SoftDeleteModel(string itemId)
    {
        var filter = Builders<Model>.Filter.Eq(x => x.ItemId, itemId);
        var update = Builders<Model>.Update.Set(x => x.isDeleted, true);

        var result = await _models.UpdateOneAsync(filter, update);
        return result.ModifiedCount == 1;
    }
    public async Task UploadModelDB(string ownerId, string modelName, string autodeskUrn, string hubID, string projectId,
        string folderId, string itemId, string versionId, bool isDeleted)

    {
        var model = new Model
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CreatedBy = ownerId,  // Autodesk User ID
            Name = modelName,
            FileUrn = autodeskUrn,  // Autodesk unique file identifier
        
            // Autodesk APS References
            HubID = hubID,
            ProjectId = projectId,
            FolderId = folderId,
            ItemId = itemId,
            VersionId = versionId,
            isDeleted = false,

            CreatedAt = DateTime.UtcNow
        };

        await _models.InsertOneAsync(model);
        Console.WriteLine($"✅ '{modelName}' uploaded successfully!");
    }
    public async Task<bool> IsModelDeleted(string itemId)
    {
        var filter = Builders<Model>.Filter.Eq(x => x.ItemId, itemId);
        var model = await _models.Find(filter).FirstOrDefaultAsync();
        return model?.isDeleted ?? false;
    }
    public async Task<bool> ModelExistsById(string itemId)
    {
        var filter = Builders<Model>.Filter.Eq(x => x.ItemId, itemId);
        var count = await _models.CountDocumentsAsync(filter);
        return count > 0;
    }

    // Add this method to your ModelService class
    public async Task<HashSet<string>> GetDeletedModelIds(string projectId, string folderId)
    {
        try
        {
            // Create a filter for models that belong to the project/folder and are marked as deleted
            var filter = Builders<Model>.Filter.And(
                Builders<Model>.Filter.Eq(x => x.ProjectId, projectId),
                Builders<Model>.Filter.Eq(x => x.FolderId, folderId),
                Builders<Model>.Filter.Eq(x => x.isDeleted, true)
            );

            // Fetch only the ItemId field to minimize data transfer
            var deletedModels = await _models.Find(filter)
                .Project(m => m.ItemId)
                .ToListAsync();

            // Convert to HashSet for efficient lookups
            return new HashSet<string>(deletedModels.Where(id => !string.IsNullOrEmpty(id)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error fetching deleted model IDs: {ex.Message}");
            return new HashSet<string>();
        }
    }

    // Add this method to your ModelService class
    // Add this method to your ModelService class

}