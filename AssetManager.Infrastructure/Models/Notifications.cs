using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssetManager.Infrastructure.Models;

public class Notifications
{
    [BsonElement("_id")]
    public ObjectId Id { get; set; }

    [BsonElement("_modelid")]
    public string ModelId { get; set; }

    [BsonElement("_userid")]
    public string UserId { get; set; }

    [BsonElement("_message")]
    public string Message { get; set; }

    [BsonElement("_time")]
    public DateTime NotificationDateTime { get; set; } = DateTime.Now;

    [BsonElement("_pending")]
    public int Pending { get; set; }
}