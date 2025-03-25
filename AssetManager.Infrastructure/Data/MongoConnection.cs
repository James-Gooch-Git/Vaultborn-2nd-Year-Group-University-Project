using MongoDB.Bson;
using MongoDB.Driver;
using AssetManager.Infrastructure.Models;
namespace AssetManager.Infrastructure.Data;

public class MongoConnection
{
    private readonly IMongoDatabase _database;

    public MongoConnection()
    {
        //string connectionString = "mongodb+srv://tomgrout65@gmail.com:9#Td*A5vrAXsddV@asset-manager.mongodb.net/AssetManagementDB?retryWrites=true&w=majority";
        string connectionString = "mongodb+srv://tomgrout65:9#Td*A5vrAXsddV@asset-manager.7m7m4.mongodb.net/?retryWrites=true&w=majority&appName=Asset-Manager";
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("AssetManagementDB");
    }
    
    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<Model> Models => _database.GetCollection<Model>("Models");
    public IMongoCollection<Versions> Versions => _database.GetCollection<Versions>("Versions");
    public IMongoCollection<Comment> Comments => _database.GetCollection<Comment>("Comments");
    public IMongoCollection<ModelData> ModelData => _database.GetCollection<ModelData>("ModelData");
    public IMongoCollection<Upvotes> Upvotes => _database.GetCollection<Upvotes>("Upvotes");
    public IMongoCollection<ListedModels> ListedModels => _database.GetCollection<ListedModels>("ListedModels");
    public IMongoCollection<Purchased> Purchased => _database.GetCollection<Purchased>("Purchased");
    public IMongoCollection<Notifications> Notifications => _database.GetCollection<Notifications>("Notifications");
    
    public IMongoCollection<BsonDocument> GetCollection(string collectionName)
    {
        return _database.GetCollection<BsonDocument>(collectionName);
    }
}