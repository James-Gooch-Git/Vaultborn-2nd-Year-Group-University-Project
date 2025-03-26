using AssetManager.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.DOC;

public class CreateCard
{
    private readonly IMongoCollection<BsonDocument> _cardsCollection;
    private readonly IMongoCollection<BsonDocument> _decksCollection;

    public CreateCard()
    {
        var mongo = new MongoConnection();
        _cardsCollection = mongo.GetCollection("Cards");
        _decksCollection = mongo.GetCollection("Decks");
    }
    
    public async void AddNewCard(string userId, string name, string description, string imageUrl, string modelName, string modelId, string deckId)
    {
        var newCard = new BsonDocument
        {
            { "name", name },
            { "owner_id", userId },  // You can modify this to get the actual user ID dynamically
            { "description", description },
            { "model_name", modelName },
            { "model_id", modelId },
            { "snapshot_url", imageUrl },
            { "created_at", DateTime.UtcNow }
        };

        try
        {
            _cardsCollection.InsertOneAsync(newCard);
            
            var cardId = newCard["_id"].ToString();

            // Update the corresponding deck to add this card ID to its "cards" array
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(deckId));
            var update = Builders<BsonDocument>.Update.Push("cards", cardId);

            var result = await _decksCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                Console.WriteLine("Card added successfully!");
            }
            else
            {
                Console.WriteLine("Failed to add card to deck. Deck may not exist.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to add card: {ex.Message}");
            throw;
        }
        
        
    }
}