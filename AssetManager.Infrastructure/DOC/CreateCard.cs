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
        _cardsCollection = mongo.GetCollection("Cards");
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
    
    public async Task AddNewCard(string userId, string name, string description, string imageUrl, string modelUrl)
    {
        var newCard = new BsonDocument
        {
            { "name", name },
            { "owner_id", userId },  // You can modify this to get the actual user ID dynamically
            { "description", description },
            { "model_3d_url", modelUrl },
            { "snapshot_url", imageUrl },
            { "created_at", DateTime.UtcNow }
        };

        try
        {
            await _cardsCollection.InsertOneAsync(newCard);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to add card: {ex.Message}");
            throw;
        }
    }
}