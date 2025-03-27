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

    public void AddNewCard(string userId, string cardName, string description,
                            byte[] imageData, string modelName, string modelId, string deckId, string imageUrl)
    {
        try
        {
            var cardDocument = new BsonDocument
                {
                    { "card_name", cardName },
                    { "description", description },
                    { "model_name", modelName },
                    { "model_id", modelId },
                    { "user_id", userId },
                    { "creation_date", DateTime.Now },
                   { "deck_id", deckId },
                { "image_url", imageUrl }

                };

            // Store the image as binary data, not as a base64 string
            // MongoDB can store binary data directly using BsonBinaryData
            cardDocument.Add("image_data", new BsonBinaryData(imageData));

            _cardsCollection.InsertOne(cardDocument);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error adding card: {ex.Message}");
            throw;
        }
    }

}