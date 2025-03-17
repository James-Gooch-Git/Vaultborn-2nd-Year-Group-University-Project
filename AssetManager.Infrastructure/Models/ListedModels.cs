using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssetManager.Infrastructure.Models;

public class ListedModels
{
    [BsonElement("_id")]
    public string ModelId { get; set; }
    
    [BsonElement("_name")]
    public string Name { get; set; }
    
    [BsonElement("_sellerid")]
    public string SellerId { get; set; }
    
    [BsonElement("_price")]
    public float Price { get; set; }
    
    [BsonElement("_tags")]
    public List<string> Tags { get; set; } = new List<string>();
    
    [BsonElement("_description")]
    public string Description { get; set; }
}