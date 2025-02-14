using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssetManager.Infrastructure.Models;


public class Comment
{
    public ObjectId CommentId { get; set; }
    public ObjectId AssetId { get; set; }
    public string UserId { get; set; }
    public string Content { get; set; }
    public DateTime CreatedDateTime { get; set; }
}