
using AssetManager.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.DOC;

public class CreateCard
{
    private readonly IMongoCollection<BsonDocument> _cardsCollection;

    public CreateCard()
    {
        var mongo = new MongoConnection();
    }

    public void NewCard()
    {

        var newCard = new BsonDocument
        {
            { "name", "Fire Dragon" },
            { "owner_id", "user123" },
            { "description", "A fearsome dragon with fire abilities" },
            { "model_3d_url", "https://cdn.yoursite.com/models/fire_dragon.glb" },
            { "snapshot_url", "https://cdn.yoursite.com/images/fire_dragon.png" },
            { "stats", new BsonDocument {
                { "power", 7 },
                { "toughness", 5 },
                { "mana_cost", 4 },
                { "special_ability", "Fire Breath" }
            }},
            { "tags", new BsonArray { "dragon", "fire", "legendary" } },
            { "created_at", DateTime.UtcNow }
        };

        _cardsCollection.InsertOneAsync(newCard);

    }
}