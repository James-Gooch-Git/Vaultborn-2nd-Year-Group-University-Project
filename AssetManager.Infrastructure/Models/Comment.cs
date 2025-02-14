using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssetManager.Infrastructure.Models;


public class Comment
{
    [BsonElement("_commentId")]
    public ObjectId CommentId { get; set; }
    
    [BsonElement("_fileId")]
    public ObjectId AssetId { get; set; }
    
    [BsonElement("_userId")]
    public string UserId { get; set; }
    
    [BsonElement("_content")]
    public string Content { get; set; }
    
    [BsonElement("_dateCreated")]
    public DateTime CreatedDateTime { get; set; }
}