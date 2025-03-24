using System;
using System.Windows;
using AssetManager.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using AssetManager.Infrastructure.DOC;

namespace AssetManager.Desktop
{
    public partial class AddCardWindow : Window
    {
        private readonly IMongoCollection<BsonDocument> _cardsCollection;

        public AddCardWindow()
        {
            InitializeComponent();
            var mongo = new MongoConnection();
            _cardsCollection = mongo.GetCollection("Cards");
        }

        private async void SubmitCard_Click(object sender, RoutedEventArgs e)
        {
            string cardName = CardNameTextBox.Text;
            string description = DescriptionTextBox.Text;
            string imageUrl = ImageUrlTextBox.Text;
            string modelUrl = ModelUrlTextBox.Text;

            if (string.IsNullOrWhiteSpace(cardName) || string.IsNullOrWhiteSpace(imageUrl))
            {
                MessageBox.Show("Card Name and Image URL are required.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var createCard = new CreateCard();

            try
            {
                await createCard.AddNewCard("Z432FEYUJQNA3AA9", cardName, description, imageUrl, modelUrl);
                MessageBox.Show("Card added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch
            {
                MessageBox.Show("Failed to add card. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}