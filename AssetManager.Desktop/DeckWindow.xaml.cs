using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.DOC;
using AssetManager.Infrastructure.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Desktop
{
    public partial class DeckView : Window
    {
        private readonly IMongoCollection<BsonDocument> _cardsCollection;
        private readonly string _userId = "Z432FEYUJQNA3AA9"; // Simulating user authentication
        private BsonDocument _selectedCard;

        public DeckView()
        {
            InitializeComponent();

            // MongoDB Connection
            var mongo = new MongoConnection();
            _cardsCollection = mongo.GetCollection("Cards");

            LoadDeckCards();
        }

        private void LoadDeckCards()
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
                    string cardImageUrl = card["snapshot_url"].ToString();
                    string cardDescription = card["description"].ToString();

                    // Load the card image
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(cardImageUrl, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // Create an Image element
                    Image cardImage = new Image
                    {
                        Source = bitmap,
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
                        //_selectedCardId = cardId; !!!!!!!!!!!!!!!!!!!!!!
                        DisplaySelectedCard(cardName, cardImageUrl, cardDescription, stats);
                    };


                    // Add the styled card to panel
                    CardListPanel.Children.Add(cardControl);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading deck cards: {ex.Message}");
            }
        }

        private void DisplaySelectedCard(string name, string imageUrl, string description, BsonDocument stats)
        {
            _selectedCard = stats; // Store the selected card for later use

            // Set the card name in the nameplate
            SelectedCardName.Text = name;

            // Load the card image dynamically
            BitmapImage cardImage = new BitmapImage();
            cardImage.BeginInit();
            cardImage.UriSource = new Uri(imageUrl, UriKind.Absolute);
            cardImage.CacheOption = BitmapCacheOption.OnLoad;
            cardImage.EndInit();

            // Assign the card image to the UI
            SelectedCardImage.Source = cardImage;
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

        private async void View3DButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCard == null || !_selectedCard.Contains("item_id"))
            {
                MessageBox.Show("3D model unavailable for this card.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedItemId = _selectedCard["item_id"].ToString();

            // Get reference to MainWindow
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.ViewModelInEmbeddedViewerAsync(selectedItemId);
            }
            else
            {
                MessageBox.Show("Main window not accessible.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void AddCardButton_Click(object sender, RoutedEventArgs e)
        { 
            AddCardWindow addCardWindow = new AddCardWindow();
            addCardWindow.Owner = this;  // Set the owner to the deck (this) window
            addCardWindow.ShowDialog();
        }
    }
}
