using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
        private readonly string _userId = "Z432FEYUJQNA3AA9"; // Simulating user authentication

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
                //ImageProcessor.CombineImages("fire_dragon.png", "ornate-gold-frame.png", "fire_dragon.png");

                if (_cardsCollection == null)
                {
                    MessageBox.Show("Database connection failed: _cardsCollection is null.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Fetch all cards for the user's deck
                var userDeckCards = _cardsCollection.Find(new BsonDocument { { "owner_id", _userId } }).ToList();

                foreach (var card in userDeckCards)
                {
                    string cardName = card["name"].ToString();
                    string cardImageUrl = card["snapshot_url"].ToString();
                    string cardDescription = card["description"].ToString();

                    // Create a clickable card preview
                    Image cardImage = new Image
                    {
                        Width = 100,
                        Height = 140,
                        Margin = new Thickness(5),
                        Stretch = System.Windows.Media.Stretch.Uniform,
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Source = new BitmapImage(new Uri(cardImageUrl))
                    };

                    // On Click, update the selected card display
                    cardImage.MouseDown += (s, e) =>
                    {
                        var statsValue = card.GetValue("stats", new BsonDocument());

                        // Ensure stats is a BsonDocument
                        BsonDocument stats = statsValue.IsBsonDocument ? statsValue.AsBsonDocument : new BsonDocument();

                        DisplaySelectedCard(cardName, cardImageUrl, cardDescription, stats);
                    };

                    // Add the card preview to the panel
                    CardListPanel.Children.Add(cardImage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading deck cards: {ex.Message}");
            }

        }

        private void DisplaySelectedCard(string name, string imageUrl, string description, BsonDocument stats)
        {
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
                    Foreground = System.Windows.Media.Brushes.Black,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                SelectedCardStatsPanel.Children.Add(statText);
            }
        }
    }
}
