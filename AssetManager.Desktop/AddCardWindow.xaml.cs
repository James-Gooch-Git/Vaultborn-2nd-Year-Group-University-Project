using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using AssetManager.Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using AssetManager.Infrastructure.DOC;
using Autodesk.Forge.Model;
using System.Windows.Media;

namespace AssetManager.Desktop
{
    public partial class AddCardWindow : Window
    {
        private readonly IMongoCollection<BsonDocument> _cardsCollection;
        private string _modelId;
        private string _deckId;
        private string cardName;
        private string description;
        private byte[] imageData; // Changed from string to byte[]
        private string modelName;
        private string localImagePath;

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

                ImageSource thumbnail = await DeckView.LoadImageFromUrl(thumbnailUrl);

                if (thumbnailUrl == null || thumbnailUrl.Length == 0)
                {
                    MessageBox.Show("No thumbnail available for this model.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    ImageUrlTextBox.Text = thumbnailUrl; // Show the actual URL instead of "Model default thumbnail"
                    AddThumbnail(thumbnail);
                }

                ModelTextBox.Text = modelName;
                CardNameTextBox.Text = modelName.Contains(".") ? modelName.Remove(modelName.IndexOf('.')) : modelName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching model data: {ex.Message}");
                MessageBox.Show("Failed to load model data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddThumbnail(ImageSource imageUrl)
        {
            try
            {
                // Check if the imageUrl is valid
                

                // Set the image to preview
                SelectedCardImage.Source = imageUrl;

                Console.WriteLine("Image loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
                // You might want to set a default/placeholder image here
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

            if (string.IsNullOrWhiteSpace(cardName) || string.IsNullOrWhiteSpace(modelName))
            {
                MessageBox.Show("Card Name and 3D Model are required.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get the image URL from the model
            string imageUrl = string.Empty;

            try
            {
                // Get the thumbnail URL from the model in the database
                var mongo = new MongoConnection();
                var _models = mongo.GetCollection("ModelData");
                var filter = Builders<BsonDocument>.Filter.Eq("_id", _modelId);
                var model = _models.Find(filter).FirstOrDefault();

                if (model != null && model.Contains("thumbnail_url"))
                {
                    imageUrl = model["thumbnail_url"].AsString;
                    Console.WriteLine($"Using thumbnail URL from database: {imageUrl}");
                }

                if (string.IsNullOrEmpty(imageUrl))
                {
                    MessageBox.Show("No image URL available for this model. Please select a different model.",
                                     "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var createCard = new CreateCard();

                // Call the method without the imageData parameter
                createCard.AddNewCard(MainWindow._userId, cardName, description, modelName, _modelId, _deckId, imageUrl);

                MessageBox.Show("Card added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add card: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLocalImageButton_Click(object sender, RoutedEventArgs e)
        {
            string imagePath = LoadImageFromLocalFolder();
            if (!string.IsNullOrEmpty(imagePath))
            {
                localImagePath = imagePath;
                ImageUrlTextBox.Text = "Local image: " + Path.GetFileName(imagePath);
                LoadLocalImageToPreview(imagePath);
            }
        }

        // Function to load image from local folder, prioritizing ModelSnapshots folder
        private string LoadImageFromLocalFolder()
        {
            // Define path to ModelSnapshots directory in Pictures folder
            string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string modelSnapshotsFolder = Path.Combine(picturesFolder, "ModelSnapshots");

            // Create the ModelSnapshots directory if it doesn't exist
            try
            {
                if (!Directory.Exists(modelSnapshotsFolder))
                {
                    Directory.CreateDirectory(modelSnapshotsFolder);
                    // If we just created the directory, it will be empty, so default to Pictures
                    modelSnapshotsFolder = picturesFolder;
                }
            }
            catch (Exception)
            {
                // If we can't create the directory, default to Pictures
                modelSnapshotsFolder = picturesFolder;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Snapshot Image",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                InitialDirectory = modelSnapshotsFolder
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    return openFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return null;
        }

        // Function to preview local image
        private void LoadLocalImageToPreview(string imagePath)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();

                // Set the image to preview
                SelectedCardImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}