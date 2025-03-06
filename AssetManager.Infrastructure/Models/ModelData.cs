using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssetManager.Infrastructure.Models
{
    public class ModelData
    {
        [BsonElement("_id")]
        public string Id { get; set; }

        [BsonElement("_name")]
        public string Name { get; set; }

        [BsonElement("_hubid")]
        public string HubId { get; set; }

        [BsonElement("_createdby")]
        public string CreatedBy { get; set; }

        [BsonElement("_createddate")]
        public string CreatedDate { get; set; }

        [BsonElement("_modifieddate")]
        public string ModifiedDate { get; set; }

        [BsonElement("_modifiedby")]
        public string ModifiedBy { get; set; }

        [BsonElement("_filesize")]
        public int FileSize { get; set; }

        [BsonElement("_publicprivate")]
        public string PublicPrivate { get; set; }

        [BsonElement("_tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [BsonElement("_foldername")]
        public string Foldername { get; set; }

        [BsonElement("_upvotes")]
        public int UpvoteCount { get; set; }
    }
}