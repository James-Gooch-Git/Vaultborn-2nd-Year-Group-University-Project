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

    public async Task UploadModelDB(string ownerId, string modelName, string autodeskUrn, string hubId, string projectId, 
        string folderId, string itemId, string versionId)
    {
        var model = new Model
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CreatedBy = ownerId,  // Autodesk User ID
            Name = modelName,
            FileUrn = autodeskUrn,  // Autodesk unique file identifier
        
            // Autodesk APS References
            HubId = hubId,
            ProjectId = projectId,
            FolderId = folderId,
            ItemId = itemId,
            VersionId = versionId,

            CreatedAt = DateTime.UtcNow
        };

        await _models.InsertOneAsync(model);
        Console.WriteLine($"✅ '{modelName}' uploaded successfully!");
    }
}