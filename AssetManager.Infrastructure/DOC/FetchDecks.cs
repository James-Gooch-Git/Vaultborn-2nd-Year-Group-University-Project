using AssetManager.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.DOC;

public class FetchDecks
{
    private readonly IMongoCollection<BsonDocument> _decksCollection;
    private readonly string _userId;

    FetchDecks(string userId) {
        var mongo = new MongoConnection();
        _userId = userId;
    }
    
    public void Fetch()
    {
        var userDecks = _decksCollection.Find(new BsonDocument { { "owner_id", _userId } }).ToList();

        foreach (var deck in userDecks)
        {
            Console.WriteLine(deck["name"]);
        }

    }
}