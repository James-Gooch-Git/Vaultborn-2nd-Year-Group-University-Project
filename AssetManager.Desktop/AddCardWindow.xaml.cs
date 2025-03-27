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
                byte[] thumbnailData = model.Contains("thumbnail_data") ? model["thumbnail_data"].AsByteArray : null;

                if (thumbnailData == null || thumbnailData.Length == 0)
                {
                    MessageBox.Show("No thumbnail available for this model.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    ImageUrlTextBox.Text = "Model default thumbnail";
                    AddThumbnail(thumbnailData);
                    imageData = thumbnailData;
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

        private void AddThumbnail(byte[] imageBytes)
        {
            // ✅ Convert binary thumbnail to image
            if (imageBytes != null && imageBytes.Length > 0)
            {
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

            if (string.IsNullOrWhiteSpace(cardName) || string.IsNullOrWhiteSpace(modelName))
            {
                MessageBox.Show("Card Name and 3D Model are required.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Handle local image if one was selected
            if (!string.IsNullOrEmpty(localImagePath))
            {
                try
                {
                    // Read the image into a byte array directly
                    imageData = File.ReadAllBytes(localImagePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to process the image: {ex.Message}", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (imageData == null || imageData.Length == 0)
            {
                MessageBox.Show("An image is required. Please select a local image or use the model's thumbnail.",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var createCard = new CreateCard();

            try
            {
                // Pass the binary image data directly
                createCard.AddNewCard(MainWindow._userId, cardName, description, imageData, modelName, _modelId, _deckId);

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