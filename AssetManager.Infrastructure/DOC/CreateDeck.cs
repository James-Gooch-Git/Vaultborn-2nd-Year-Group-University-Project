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
    
    public void NewDeck()
    {
        
        var newDeck = new BsonDocument
        {
            { "name", "Dragon Masters" },
            { "owner_id", "user123" },
            { "cards", new BsonArray { "unique_card_id_1", "unique_card_id_2" } },
            { "created_at", DateTime.UtcNow }
        };

        _decksCollection.InsertOneAsync(newDeck);
    }
}