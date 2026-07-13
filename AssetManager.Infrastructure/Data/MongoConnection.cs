using MongoDB.Bson;
using MongoDB.Driver;
using AssetManager.Infrastructure.Configuration;
using AssetManager.Infrastructure.Models;
namespace AssetManager.Infrastructure.Data;

public class MongoConnection
{
    // MongoClient is designed to be a process-wide singleton (it manages its own
    // connection pool), so every MongoConnection instance shares this one client.
    private static readonly Lazy<MongoClient> SharedClient =
        new(() => new MongoClient(AppSecrets.MongoConnectionString));

    private readonly IMongoDatabase _database;

    public MongoConnection()
    {
        _database = SharedClient.Value.GetDatabase("AssetManagementDB");
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
