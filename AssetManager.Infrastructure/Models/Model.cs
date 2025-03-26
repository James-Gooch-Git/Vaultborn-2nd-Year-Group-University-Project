using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AssetManager.Infrastructure.Models
{
    public class Model
    {
        [BsonId]  // This tells MongoDB to use Id as the primary key
        [BsonRepresentation(BsonType.ObjectId)]  // Converts string & int 
        public string Id { get; set; } 
        public string Name { get; set; }
        public string Description { get; set; }
        
        public string BucketKey { get; set; }
        public string FileUrn { get; set; }
        
        public string HubID { get; set; } 
        public string ProjectId { get; set; }  
        public string FolderId { get; set; } 
        public string ItemId { get; set; }  
        public string VersionId { get; set; }
        
        public bool isDeleted {  get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}