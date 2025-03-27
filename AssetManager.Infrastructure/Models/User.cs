using MongoDB.Bson.Serialization.Attributes;

namespace AssetManager.Infrastructure.Models
{
    public class User
    {
        [BsonElement("_id")]
        public string Id { get; set; }
        
        [BsonElement("_username")]
        public string Username { get; set; }
        
        [BsonElement("_email")]
        public string Email { get; set; }
        
        [BsonElement("_profilepic")]
        public string ProfilePic { get; set; } 
        
        [BsonElement("_datecreated")]
        public DateTime DateCreated { get; set; } = DateTime.Now;
        
        [BsonElement("decks")]
        public List<string> Decks { get; set; } = new List<string>();
    }
}