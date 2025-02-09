using MongoDB.Bson;
using MongoDB.Driver;
using AssetManager.Infrastructure.Models;
namespace AssetManager.Infrastructure.Data;

public class MongoConnection
{
    private readonly IMongoDatabase _database;

    public MongoConnection()
    {
        string connectionString = "mongodb+srv://tomgrout65@gmail.com:9#Td*A5vrAXsddV@your-cluster.mongodb.net/AssetManagementDB?retryWrites=true&w=majority";
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("AssetManagementDB");
    }
    
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Model> Models => _database.GetCollection<Model>("models");
    public IMongoCollection<ModelVersion> ModelVersions => _database.GetCollection<ModelVersion>("versions");

    public IMongoCollection<BsonDocument> GetCollection(string collectionName)
    {
        return _database.GetCollection<BsonDocument>(collectionName);
    }
}