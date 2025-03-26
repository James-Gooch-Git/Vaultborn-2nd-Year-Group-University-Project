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
        string folderId, string itemId, string versionId)

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
  

}