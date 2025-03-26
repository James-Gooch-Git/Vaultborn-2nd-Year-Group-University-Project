using AssetManager.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.DOC;

public class CreateDeck
{
    private readonly IMongoCollection<BsonDocument> _decksCollection;

    public CreateDeck()
    {
        var mongo = new MongoConnection();
    }
    
    public void NewDeck(string deckName, string deckDescription)
    {
        var newDeck = new BsonDocument
        {
            { "name", deckName },
            { "owner_id", Environment.GetEnvironmentVariable("userId", EnvironmentVariableTarget.User) },  
            { "description", deckDescription },
            { "is_listed", false },
            { "price", 0.0 },
            { "created_at", DateTime.UtcNow }
        };

        _decksCollection.InsertOneAsync(newDeck);
    }
}