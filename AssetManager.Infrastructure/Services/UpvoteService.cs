using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Infrastructure.Services;

public class UpvoteService
{
    private readonly MongoConnection database = new();

    public async Task<int> GetModelUpvoteCount(string id)
    {
        var userData = await database.ModelData.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (userData == null)
        {
            return 0;
        }
        return userData.UpvoteCount;
    }

    public async Task SetUserModelVote(string modelId, string userId)
    {
        var findVote = await database.Upvotes.Find(x => x.ModelId == modelId && x.UserId == userId).FirstOrDefaultAsync();

        if (findVote == null)
        {
            Upvotes upvote = new Upvotes()
            {
                Id = new ObjectId(), ModelId = modelId, UserId = userId, Vote = 0
            };
            await database.Upvotes.InsertOneAsync(upvote);
        }
            
    }
    
    public async Task UpdateUserModelVote(string modelId, string userId, int vote)
    {
        var filter = Builders<Upvotes>.Filter.And(
            Builders<Upvotes>.Filter.Eq("ModelId", modelId),
            Builders<Upvotes>.Filter.Eq("UserId", userId));
        var update = Builders<Upvotes>.Update.Set("Vote", vote);
        await database.Upvotes.UpdateOneAsync(filter, update);
    }
    
    public async Task<int> GetUserModelVote(string modelId, string userId)
    {
        var result = await database.Upvotes.Find(x => x.ModelId == modelId && x.UserId == userId).FirstOrDefaultAsync();
        if (result == null)
        {
            return 0;
        }
        else
        {
            return result.Vote;
        }
    }
}