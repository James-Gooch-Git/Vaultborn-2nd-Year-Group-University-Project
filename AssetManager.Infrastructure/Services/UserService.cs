using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.Services
{
    public class UserService
    {
        public async Task<string> GetUserName(string userId)
        {
            MongoConnection database = new MongoConnection();
            var userData = await database.Users.Find(x => x.Id == userId).FirstOrDefaultAsync();
            if (userData == null)
            {
                return "Unknown User";
            }
            return userData.Username;
        }

        public async Task<string> GetUserPic(string userId)
        {
            MongoConnection database = new MongoConnection();
            var userData = await database.Users.Find(x => x.Id == userId).FirstOrDefaultAsync();

            if (userData == null)
            {
                // Failsafe: shouldn't happen if InsertUserDataDB runs properly
                return "https://your-bucket.s3.amazonaws.com/fallback.png";
            }

            // List of your official AWS profile pic URLs
            List<string> awsProfilePics = new List<string>
            {
                "https://usericonsvaultborn.s3.eu-north-1.amazonaws.com/Angel.png",
                "https://usericonsvaultborn.s3.eu-north-1.amazonaws.com/BusinessMan.png",
                "https://usericonsvaultborn.s3.eu-north-1.amazonaws.com/Cthulu.png",
                "https://usericonsvaultborn.s3.eu-north-1.amazonaws.com/Unicorn.png",
                "https://usericonsvaultborn.s3.eu-north-1.amazonaws.com/Vampire.png",
                "https://usericonsvaultborn.s3.eu-north-1.amazonaws.com/Werewolf.png",
                "https://usericonsvaultborn.s3.eu-north-1.amazonaws.com/Witch.jpeg",
                "https://usericonsvaultborn.s3.eu-north-1.amazonaws.com/Wizard.png"
            };


            string currentPic = userData.ProfilePic;

            // ✅ If the saved MongoDB profile pic is not one of your approved AWS images
            if (!string.IsNullOrEmpty(currentPic) && !awsProfilePics.Contains(currentPic))
            {
                Random rng = new Random();
                string newPic = awsProfilePics[rng.Next(awsProfilePics.Count)];

                // Update the user document in MongoDB with the new profile picture
                var update = Builders<User>.Update.Set(u => u.ProfilePic, newPic);
                await database.Users.UpdateOneAsync(x => x.Id == userId, update);

                return newPic;
            }

            // ✅ If it's already one of your official pics, just return it
            return currentPic;
        }



 
        
        public async Task<string> GetModelProjectId(string modelId)
        {
            MongoConnection database = new MongoConnection();
            var userData = await database.ModelData.Find(x => x.Id == modelId).FirstOrDefaultAsync();
            if (userData == null)
            {
                return "Unknown Project";
            }
            return userData.FolderId;
        }
        
        public async Task<string> GetModelSeller(string modelId)
        {
            MongoConnection database = new MongoConnection();
            var userData = await database.ListedModels.Find(x => x.ModelId == modelId).FirstOrDefaultAsync();
            if (userData == null)
            {
                return "Unknown User";
            }
            return userData.SellerId;
        }

        public async Task<string> GetDeckName(string deckId)
        {
            MongoConnection database = new MongoConnection();

            var collection = database.GetCollection("Decks");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(deckId));
            var decks = await collection.Find(filter).FirstOrDefaultAsync();
            if (decks == null)
            {
                return "Unknown Deck";
            }

            return decks["name"].ToString();
        }
        
        public async Task<string> GetDeckOwner(string deckId)
        {
            MongoConnection database = new MongoConnection();

            var collection = database.GetCollection("Decks");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(deckId));
            var decks = await collection.Find(filter).FirstOrDefaultAsync();
            if (decks == null)
            {
                return "Unknown User";
            }

            return decks["owner_id"].ToString();
        }
    }
}
