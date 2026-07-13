using AssetManager.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.DOC;

public class CreateDeck
{
    private readonly MongoConnection _mongo = new();
    private readonly IMongoCollection<BsonDocument> _decksCollection;

    public CreateDeck()
    {
        _decksCollection = _mongo.GetCollection("Decks");
    }
    
    public async Task AddNewDeck(string owner, string name, string description)
    {
        var newDeck = new BsonDocument
        {
            { "name", name },
            { "owner_id", owner },
            { "description", description },
            { "cards", new BsonArray() }, // Empty card list
            { "created_at", DateTime.UtcNow },
            { "is_listed", false},
            {"price", 0}
        };

        try
        {
            _decksCollection.InsertOneAsync(newDeck);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }
    }
}