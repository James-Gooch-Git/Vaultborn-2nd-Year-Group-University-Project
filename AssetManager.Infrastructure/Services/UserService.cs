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
        private readonly MongoConnection database = new();

        public async Task<string> GetUserName(string userId)
        {
            var userData = await database.Users.Find(x => x.Id == userId).FirstOrDefaultAsync();
            if (userData == null)
            {
                return "Unknown User";
            }
            return userData.Username;
        }

        public async Task<string> GetUserPic(string userId)
        {
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
            var userData = await database.ModelData.Find(x => x.Id == modelId).FirstOrDefaultAsync();
            if (userData == null)
            {
                return "Unknown Project";
            }
            return userData.FolderId;
        }
        
        public async Task<string> GetModelSeller(string modelId)
        {
            var userData = await database.ListedModels.Find(x => x.ModelId == modelId).FirstOrDefaultAsync();
            if (userData == null)
            {
                return "Unknown User";
            }
            return userData.SellerId;
        }

        public async Task<string> GetDeckName(string deckId)
        {

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

            var collection = database.GetCollection("Decks");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(deckId));
            var decks = await collection.Find(filter).FirstOrDefaultAsync();
            if (decks == null)
            {
                return "Unknown User";
            }

            return decks["owner_id"].ToString();
        }
        
        public async Task<ModelData> GetModelTags(string modelId)
        {
            var result = await database.ModelData.Find(x => x.Id == modelId).FirstOrDefaultAsync();
            return result;
        }
        
        public async Task<List<Comment>> GetAllComments(string assetId)
        {
            try
            {
                var allComments = await database.Comments.Find(x => x.AssetId == assetId ).ToListAsync();
                return allComments;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                throw;
            }
        }
        
        public async Task<bool> CheckModelPurchased(string modelId, string userId)
        {
            var result = await database.Purchased.Find(x => x.ModelId == modelId && x.UserId == userId).FirstOrDefaultAsync();
            if (result == null)
            {
                return false;
            }

            return true;
        }
        
        public async Task<List<Dictionary<string, string>>> GetAllListedModels(string userId)
        {
            List<Dictionary<string, string>> allListedModels = new List<Dictionary<string, string>>();
            var listedModels = await database.ListedModels.Find(FilterDefinition<ListedModels>.Empty).ToListAsync();
            foreach (var model in listedModels)
            {
                bool purchased = await CheckModelPurchased(model.ModelId, userId);
                string projectId = await GetModelProjectId(model.ModelId);
                string sellerName = await GetUserName(model.SellerId);
                allListedModels.Add(new Dictionary<string, string>
                {
                    { "Name", model.Name },
                    { "Description", model.Description },
                    { "Seller", sellerName },
                    { "Id", model.ModelId },
                    { "Price", $"£{model.Price.ToString("0.00")}"},
                    { "ProjectId", projectId},
                    { "BuyVisibility", purchased ? "Collapsed" : "Visible" },
                    { "PurchasedVisibility", purchased ? "Visible" : "Collapsed" }
                });
            }
            return allListedModels;
        }
        
        public async Task<List<Dictionary<string, string>>> GetAllListedDecks(string userId)
        {
            List<Dictionary<string, string>> allListedDecks = new List<Dictionary<string, string>>();

            var collection = database.GetCollection("Decks");
            var filter = Builders<BsonDocument>.Filter.Eq("is_listed", true);
            var decks = await collection.Find(filter).ToListAsync();

            foreach (var deck in decks)
            {
                bool purchased = await CheckModelPurchased(deck["_id"].ToString(), userId);
                string sellerName = await GetUserName(deck["owner_id"].ToString());
                double amount = double.Parse(deck["price"].ToString());
                string price = amount.ToString("0.00");
                allListedDecks.Add(new Dictionary<string, string>
                {
                    { "Name", deck["name"].ToString() },
                    { "Description",deck["description"].ToString() },
                    { "Seller", sellerName },
                    { "Id", deck["_id"].ToString() },
                    { "Price", $"£{price}"},
                    { "BuyVisibility", purchased ? "Collapsed" : "Visible" },
                    { "PurchasedVisibility", purchased ? "Visible" : "Collapsed" },
                    { "ProjectId", "N/A"}
                });
            }
            
            return allListedDecks;
        }
        
        public async Task<List<Notifications>> GetPendingNotifications(string userId)
        {
            var result = await database.Notifications.Find(x => x.UserId == userId && x.Pending == 1).ToListAsync();
            if (result == null)
            {
                return null;
            }

            return result;
        }

        public async Task AddDeckToUser(string deckId, string userId)
        {
            var filter = Builders<User>.Filter.Eq("_id", userId);
            var update = Builders<User>.Update.AddToSet(x => x.Decks, deckId);
            await database.Users.UpdateOneAsync(filter, update);
        }
    }
}
