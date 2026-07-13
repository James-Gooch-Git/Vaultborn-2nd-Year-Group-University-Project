using AssetManager.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.DOC;

public class CreateCard
{
    private readonly MongoConnection _mongo = new();
    private readonly IMongoCollection<BsonDocument> _cardsCollection;
    private readonly IMongoCollection<BsonDocument> _decksCollection;

    public CreateCard()
    {
        _cardsCollection = _mongo.GetCollection("Cards");
        _decksCollection = _mongo.GetCollection("Decks");
    }

    public void AddNewCard(string userId, string cardName, string description,
                     string modelName, string modelId, string deckId, string imageUrl)
    {
        try
        {
            Console.WriteLine("Creating card with fields:");
            Console.WriteLine($"name: {cardName}");
            Console.WriteLine($"description: {description}");
            Console.WriteLine($"model_name: {modelName}");
            Console.WriteLine($"model_id: {modelId}");
            Console.WriteLine($"owner_id: {userId}");
            Console.WriteLine($"deck_id: {deckId}");
            Console.WriteLine($"snapshot_url: {imageUrl}");

            var cardDocument = new BsonDocument
        {
            { "name", cardName },
            { "description", description },
            { "model_name", modelName },
            { "model_id", modelId },
            { "owner_id", userId },
            { "created_at", DateTime.Now },
            { "deck_id", deckId },
            { "snapshot_url", imageUrl },
            { "stats", new BsonDocument() }
        };

            // Insert the card
            _cardsCollection.InsertOne(cardDocument);

            // Get the ID of the inserted card
            var cardId = cardDocument["_id"].AsObjectId;

            // Update the deck to include this card
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(deckId));
            var update = Builders<BsonDocument>.Update.Push("cards", cardId);

            var updateResult = _decksCollection.UpdateOne(filter, update);

            if (updateResult.ModifiedCount == 0)
            {
                Console.WriteLine($"⚠️ Warning: Deck with ID {deckId} was not updated with the new card!");

                // Get the deck document to check if it exists and has cards array
                var deckDoc = _decksCollection.Find(filter).FirstOrDefault();
                if (deckDoc == null)
                {
                    Console.WriteLine("Deck not found in database.");
                }
                else
                {
                    Console.WriteLine($"Deck exists: {deckDoc.ToJson()}");
                    // If cards array doesn't exist, add it with this card
                    if (!deckDoc.Contains("cards"))
                    {
                        var createArrayUpdate = Builders<BsonDocument>.Update.Set("cards", new BsonArray { cardId });
                        _decksCollection.UpdateOne(filter, createArrayUpdate);
                        Console.WriteLine("Created new cards array in deck");
                    }
                }
            }
            else
            {
                Console.WriteLine($"✅ Card {cardId} added to deck {deckId} successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error adding card: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}