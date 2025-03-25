using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.DOC;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Desktop
{
    public partial class DeckView : Window
    {
        private readonly IMongoCollection<BsonDocument> _cardsCollection;
        private readonly string _userId = MainWindow._userId; // Simulating user authentication
        private BsonDocument _selectedCard;
        private string _selectedCardId;

        public DeckView()
        {
            InitializeComponent();

            // MongoDB Connection
            var mongo = new MongoConnection();
            _cardsCollection = mongo.GetCollection("Cards");

            LoadDeckCards();
        }

        private async void LoadDeckCards()
        {
            try
            {
                if (_cardsCollection == null)
                {
                    MessageBox.Show("Database connection failed: _cardsCollection is null.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Clear existing cards before adding new ones
                CardListPanel.Children.Clear();

                // Fetch all cards for the user's deck
                var userDeckCards = _cardsCollection.Find(new BsonDocument { { "owner_id", _userId } }).ToList();

                foreach (var card in userDeckCards)
                {
                    string cardName = card["name"].ToString();
                    string cardId = card["_id"].ToString();
                    string cardImageUrl = card["snapshot_url"].ToString();
                    string cardDescription = card["description"].ToString();
                    
                    ImageSource imageUrlSource = await LoadImageFromUrl(cardImageUrl);

                    Image cardImage = new Image
                    {
                        Source = imageUrlSource,
                        Stretch = System.Windows.Media.Stretch.UniformToFill
                    };

                    // Apply high-quality bitmap scaling mode
                    RenderOptions.SetBitmapScalingMode(cardImage, BitmapScalingMode.HighQuality);

                    // Create the ContentControl to apply the FantasyCardStyle
                    ContentControl cardControl = new ContentControl
                    {
                        Style = (Style)FindResource("FantasyCardStyle"),
                        Tag = cardName,  // Used for binding the card name in the template
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Content = cardImage // Set the image as content
                    };

                    // Handle card click event
                    cardControl.MouseDown += (s, e) =>
                    {
                        var statsValue = card.GetValue("stats", new BsonDocument());
                        BsonDocument stats = statsValue.IsBsonDocument ? statsValue.AsBsonDocument : new BsonDocument();
                        //_selectedCard = stats;
                        _selectedCardId = cardId;
                        Console.WriteLine("\n\nCard id ==" +  cardId);
                        DisplaySelectedCard(cardName, imageUrlSource, cardDescription, stats);
                    };

                    // Add the styled card to panel
                    CardListPanel.Children.Add(cardControl);

                }
            }
            catch (Exception ex)
            {
               // Console.WriteLine($"{cardImageUrl}");
                Console.WriteLine($"Error loading deck cards: {ex.Message}");
            }
        }
        
        private static async Task<ImageSource> LoadImageFromUrl(string imageUrl)
        {
            if (imageUrl.StartsWith("https://developer.api.autodesk.com"))
            {
                return await LoadImageFromAPI(imageUrl, MainWindow._accessToken);
            }

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute); // Ensure Absolute URL
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;

        }
        
        private static async Task<ImageSource> LoadImageFromAPI(string imageUrl, string accessToken)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Set Authorization Header
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    // Fetch the image from Autodesk API
                    HttpResponseMessage response = await client.GetAsync(imageUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"❌ Error fetching image: {response.StatusCode} - {response.ReasonPhrase}");
                        return null;
                    }

                    // Read image bytes
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                    // Convert to BitmapImage
                    BitmapImage bitmap = new BitmapImage();
                    using (MemoryStream stream = new MemoryStream(imageBytes))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Freeze for thread safety
                    }

                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception loading image: {ex.Message}");
                return null;
            }
        }



        private void DisplaySelectedCard(string name, ImageSource imageSource, string description, BsonDocument stats)
        {
            _selectedCard = stats; // Store the selected card for later use

            // Set the card name in the nameplate
            SelectedCardName.Text = name;

            // Assign the card image to the UI
            SelectedCardImage.Source = imageSource;
            RenderOptions.SetBitmapScalingMode(SelectedCardImage, BitmapScalingMode.HighQuality);

            // Update description
            SelectedCardDescription.Text = description;

            // Clear previous stats
            SelectedCardStatsPanel.Children.Clear();

            // Add stats dynamically
            foreach (var stat in stats.Elements)
            {
                TextBlock statText = new TextBlock
                {
                    Text = $"{stat.Name}: {stat.Value}",
                    FontSize = 14,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                statText.Style = (Style)FindResource("StatsTextStyle");
                
                SelectedCardStatsPanel.Children.Add(statText);
            }
            
            // + Add stats
            SelectedCardStatsPanel.Children.Add(new TextBlock
            {
                Style = (Style)FindResource("StatsTextStyle"),
                Text = $"+ Add Stats for {name}",
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 2)
            });

            
        }
        
        public class CardModel
        {
            public string Name { get; set; }
            public ImageSource ImageSource { get; set; }
            // Add other card properties as needed
        }
        
        private void View3DButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCard  == null)
            {
                MessageBox.Show("3D model unavailable for this card.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(_selectedCardId));
            var selectedCardData = _cardsCollection.Find(filter).FirstOrDefault();

            if (selectedCardData == null)
            {
                MessageBox.Show("Card data not found in database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (!selectedCardData.Contains("model_id") || selectedCardData["model_id"].IsBsonNull)
            {
                MessageBox.Show("This card does not have an associated 3D model.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            string modelUrn = selectedCardData["model_id"].ToString();

            // Open the Forge Viewer Window with the model URN
            ForgeViewerWindow viewerWindow = new ForgeViewerWindow(modelUrn);
            viewerWindow.Show();
        }

        private void AddCardButton_Click(object sender, RoutedEventArgs e)
        { 
            AddCardWindow addCardWindow = new AddCardWindow();
            addCardWindow.Owner = this;  // Set the owner to the deck (this) window
            addCardWindow.ShowDialog();
        }

        private void ExpandCard_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCard == null) return;

            string cardName = SelectedCardName.Text;
            ImageSource cardImage = SelectedCardImage.Source;
            string cardDescription = SelectedCardDescription.Text;
            string cardStats = _selectedCard.ToJson(); // Convert BSON to string

            FullScreenCardWindow fullScreenWindow = new FullScreenCardWindow(cardName, cardImage, cardDescription, cardStats);
            fullScreenWindow.ShowDialog();
        }

        private void PurchaseCard_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void DrawRandomCard_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
