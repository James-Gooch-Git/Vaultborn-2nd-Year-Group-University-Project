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
        
        [BsonElement("_folderid")]
        public string FolderId { get; set; }

        [BsonElement("_upvotes")]
        public int UpvoteCount { get; set; }

        [BsonElement("thumbnail_url")]
        public string Thumbnail_Url { get; set; }
        
        [BsonElement("thumbnail_base64")]
        public string Thumbnail_Base64 { get; set; }

        [BsonElement("_version")]
        public string Version { get; set; }

        [BsonElement("_format")]
        public string Format { get; set; }

        [BsonElement("_polycount")]
        public int PolyCount { get; set; }

        [BsonElement("_dimensions")]
        public string Dimensions { get; set; }
        
        [BsonElement("isDeleted")]
        public bool isDeleted { get; set; }
    }
}