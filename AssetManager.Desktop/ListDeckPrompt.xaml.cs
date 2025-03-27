using System.Windows;
using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.DOC;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Desktop;

public partial class ListDeckPrompt : Window
{
    private string _deckId;
    private readonly IMongoCollection<BsonDocument> _decksCollection;
    
    public ListDeckPrompt(string deckId, IMongoCollection<BsonDocument> deckCollection)
    {
        _deckId = deckId;
        //var mongo = new MongoConnection();
        _decksCollection = deckCollection;
        InitializeComponent();
    }

    private async void SubmitListing_Click(object sender, RoutedEventArgs e)
    {
        var price = PriceField.Text;
        
        if (string.IsNullOrEmpty(_deckId) || string.IsNullOrEmpty(price))
        {
            MessageBox.Show("Error: No deck selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(_deckId));
            var update = Builders<BsonDocument>.Update.Set("is_listed", true).Set("price", price);

            var result = await _decksCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                MessageBox.Show("Deck successfully listed on the marketplace!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Error: Deck listing failed. It may already be listed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating deck: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();

        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}