using MongoDB.Bson.Serialization.Attributes;

namespace AssetManager.Infrastructure.Models
{
    public class Versions
    {
        [BsonElement("_id")]
        public string Id { get; set; }
        
        [BsonElement("_latestversion")]
        public int VersionNumber { get; set; }
    }
}