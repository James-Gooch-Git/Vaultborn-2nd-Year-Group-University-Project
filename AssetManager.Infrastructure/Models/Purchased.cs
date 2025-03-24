using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssetManager.Infrastructure.Models;

public class Purchased
{
    [BsonElement("_id")]
    public string ModelId { get; set; }
    
    [BsonElement("_userid")]
    public string UserId { get; set; }
}