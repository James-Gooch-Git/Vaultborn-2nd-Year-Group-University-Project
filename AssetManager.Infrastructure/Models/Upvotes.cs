using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssetManager.Infrastructure.Models;

public class Upvotes
{
    [BsonElement("_id")]
    public ObjectId Id { get; set; }

    [BsonElement("_modelid")]
    public string ModelId { get; set; }

    [BsonElement("_userid")]
    public string UserId { get; set; }

    [BsonElement("_vote")]
    public int Vote { get; set; }
}