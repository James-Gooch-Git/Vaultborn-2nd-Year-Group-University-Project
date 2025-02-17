using MongoDB.Bson;
using MongoDB.Driver;
using AssetManager.Infrastructure.Models;
namespace AssetManager.Infrastructure.Data;

public class MongoConnection
{
    private readonly IMongoDatabase _database;

    public MongoConnection()
    {
        //string connectionString = "mongodb+srv://tomgrout65@gmail.com:9#TdA5vrAXsddV@asset-manager.mongodb.net/AssetManagementDB?retryWrites=true&w=majority";
        string connectionString = "mongodb+srv://tomgrout65:9#TdA5vrAXsddV@asset-manager.7m7m4.mongodb.net/?retryWrites=true&w=majority&appName=Asset-Manager";
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("AssetManagementDB");
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<Model> Models => _database.GetCollection<Model>("Models");
    public IMongoCollection<ModelVersion> ModelVersions => _database.GetCollection<ModelVersion>("Versions");
    public IMongoCollection<Comment> Comments => _database.GetCollection<Comment>("Comments");

    public IMongoCollection<BsonDocument> GetCollection(string collectionName)
    {
        return _database.GetCollection<BsonDocument>(collectionName);
    }
}