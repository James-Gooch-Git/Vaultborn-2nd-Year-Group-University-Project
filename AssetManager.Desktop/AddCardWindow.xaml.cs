using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using AssetManager.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using AssetManager.Infrastructure.DOC;
using Autodesk.Forge.Model;

namespace AssetManager.Desktop
{
    public partial class AddCardWindow : Window
    {
        private readonly IMongoCollection<BsonDocument> _cardsCollection;
        private string _modelId;
        private string _deckId;
        private string cardName;
        private string description;
        private string imageUrl;
        private string modelName;

        public AddCardWindow(string deckId = null, string modelId = null)
        {
            _deckId = deckId;
            _modelId = modelId;
            InitializeComponent();
            //GetData();
        }

        private async void GetData()
        {
            if (_modelId == null)
            {
                MessageBox.Show("No model selected!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // ✅ Fetch the model data from MongoDB
                var mongo = new MongoConnection();
                var _models = mongo.GetCollection("ModelData");

                var filter = Builders<BsonDocument>.Filter.Eq("_id", (_modelId));
                var model = await _models.Find(filter).FirstOrDefaultAsync();

                if (model == null)
                {
                    MessageBox.Show("Model not found in the database!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string modelName = model.GetValue("_name", "").AsString;
                string thumbnailUrl = model.Contains("thumbnail_url") ? model["thumbnail_url"].AsString : null;
                string thumbnail_base64 = model.Contains("thumbnail_base64") ? model["thumbnail_base64"].AsString : null;
                
                if (string.IsNullOrEmpty(thumbnail_base64))
                {
                    MessageBox.Show("No thumbnail available for this model.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //ImageUrlTextBox.Text = "";
                }
                else
                {
                    ImageUrlTextBox.Text = "Model default thumbnail";
                    AddThumbnail(thumbnail_base64);
                    imageUrl = thumbnailUrl;
                }

                ModelTextBox.Text = modelName;
                CardNameTextBox.Text = modelName.Remove(modelName.IndexOf('.'));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching model data: {ex.Message}");
                MessageBox.Show("Failed to load model data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddThumbnail(string? thumbnailUrl)
        {

            // ✅ Convert Base64 thumbnail to image
            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                byte[] imageBytes = Convert.FromBase64String(thumbnailUrl);
                BitmapImage bitmap = new BitmapImage();

                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                }

                // ✅ Set the image to preview
                SelectedCardImage.Source = bitmap;
            }
        }
        
        private void BrowseModelsButton_Click(object sender, RoutedEventArgs e)
        {
            SelectModelWindow modelBrowser = new SelectModelWindow();
    
            if (modelBrowser.ShowDialog() == true)
            {
                // ✅ User selected a model, update AddCardWindow with its details
                ModelTextBox.Text = modelBrowser.SelectedModelName;
                _modelId = modelBrowser.SelectedModel;
                ImageUrlTextBox.Text = " loading . . . ";
                GetData();            
            }
        }

        private void SubmitCard_Click(object sender, RoutedEventArgs e)
        {
            cardName = CardNameTextBox.Text;
            description = DescriptionTextBox.Text;
            modelName = ModelTextBox.Text;

            if (string.IsNullOrWhiteSpace(cardName) || string.IsNullOrWhiteSpace(imageUrl) || string.IsNullOrWhiteSpace(modelName))
            {
                MessageBox.Show("Card Name, 3D Model and Image URL are required.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var createCard = new CreateCard();

            try
            {
                createCard.AddNewCard(MainWindow._userId, cardName, description, imageUrl, modelName, _modelId, _deckId);
                
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