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
using AssetManager.Infrastructure.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Desktop
{
    public partial class DeckView : Window
    {
        private IMongoCollection<BsonDocument> _cardsCollection;
        private IMongoCollection<BsonDocument> _decksCollection;
        private readonly string _userId = MainWindow._userId;
        private BsonDocument _selectedCard;
        private string _selectedCardId;
        private string _deckId;

        public DeckView()
        {
            InitializeComponent();
            
            // MongoDB Connection
            var mongo = new MongoConnection();
            _cardsCollection = mongo.GetCollection("Cards");
            _decksCollection = mongo.GetCollection("Decks");

            LoadUserDecks();
            
            if (!string.IsNullOrEmpty(_deckId))
            {
                CheckIfUserIsDeckOwner(_deckId);
                LoadDeckCards(_deckId);
            }
        }

        private async Task LoadUserDecks()
        {
            var mongo = new MongoConnection();
            _decksCollection = mongo.GetCollection("Decks");
            var usersCollection = mongo.GetCollection("Users");

            try
            {
                if (string.IsNullOrEmpty(_userId))
                {
                    MessageBox.Show("Error: User not identified.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Fetch user's purchased/downloaded decks from Users collection
                var userFilter = Builders<BsonDocument>.Filter.Eq("_id", _userId);
                var userDoc = await usersCollection.Find(userFilter).FirstOrDefaultAsync();

                List<ObjectId> accessibleDeckIds = new();

                if (userDoc != null && userDoc.Contains("decks"))
                {
                    foreach (var deckIdStr in userDoc["decks"].AsBsonArray.Select(d => d.ToString()))
                    {
                        if (ObjectId.TryParse(deckIdStr, out ObjectId deckId))
                        {
                            accessibleDeckIds.Add(deckId);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid ObjectId: {deckIdStr}");
                        }
                    }
                }

                // Query decks: owned by the user OR in the user's "decks" array
                var deckFilter = Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.Eq("owner_id", _userId),
                    Builders<BsonDocument>.Filter.In("_id", accessibleDeckIds)
                );

                var userDecks = await _decksCollection.Find(deckFilter).ToListAsync();

                // Ensure UI updates on the main thread
                Dispatcher.Invoke(() =>
                {
                    DeckListPanel.Children.Clear(); // Clear previous items

                    foreach (var deck in userDecks)
                    {
                        string deckId = deck["_id"].ToString();
                        string deckName = deck.Contains("name") ? deck["name"].ToString() : "Unnamed Deck";

                        // Create a button for each deck
                        Button deckButton = new Button
                        {
                            Content = deckName,
                            Tag = deckId,
                            Width = 130,
                            Height = 40,
                            Background = new SolidColorBrush(Color.FromRgb(30, 30, 48)),
                            Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                            FontFamily = new FontFamily("Gabriola"),
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Cursor = Cursors.Hand,
                            Margin = new Thickness(5),
                        };

                        deckButton.Click += SelectDeck_Click;
                        DeckListPanel.Children.Add(deckButton);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading decks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SelectDeck_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string selectedDeckId)
            {
                _deckId = selectedDeckId;
                CheckIfUserIsDeckOwner(_deckId);
                LoadDeckCards(selectedDeckId);
            }
        }

        private void CheckIfUserIsDeckOwner(string deckId)
        {
            BsonDocument GetDeckById(string deckId)
            {
                var collection = _decksCollection;
                var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(deckId));
                return collection.Find(filter).FirstOrDefault();
            }
            
            var deck = GetDeckById(deckId);
    
            if (deck == null)
            {
                MessageBox.Show("Deck not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close(); // Close the window if deck doesn't exist
                return;
            }

            // Extract owner_id from the deck
            string ownerId = deck.Contains("owner_id") ? deck["owner_id"].AsString : string.Empty;

            // Check if the current user is the owner
            if (_userId != ownerId)
            {
                ListButton.Visibility = Visibility.Hidden;
                return;
            }
            else
            {
                ListButton.Visibility = Visibility.Visible;
            }
        }

        private async void LoadDeckCards(string deckId)
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

                if (string.IsNullOrEmpty(deckId))
                {
                    MessageBox.Show("Error: No deck selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Query the deck document
                var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(deckId));
                var deck = await _decksCollection.Find(filter).FirstOrDefaultAsync();

                if (deck == null || !deck.Contains("cards"))
                {
                    MessageBox.Show("Error: Deck not found or has no cards.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Extract the "cards" array
                var cardIds = deck["cards"].AsBsonArray.Select(c => c.ToString()).ToList();

                if (!cardIds.Any())
                {
                    MessageBox.Show("This deck has no cards.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Query cards collection to fetch details of all cards in the deck
                var cardFilter = Builders<BsonDocument>.Filter.In("_id", cardIds.Select(id => ObjectId.Parse(id)));
                var cardList = await _cardsCollection.Find(cardFilter).ToListAsync();

                foreach (var card in cardList)
                {
                    var mongo = new MongoConnection();
                    _decksCollection = mongo.GetCollection("Decks");
                    _cardsCollection = mongo.GetCollection("Cards");
                    
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
                        // var mongo = new MongoConnection();
                        // _decksCollection = mongo.GetCollection("Decks");
                        // _cardsCollection = mongo.GetCollection("Cards");
                        
                        var statsValue = card.GetValue("stats", new BsonDocument());
                        BsonDocument stats = statsValue.IsBsonDocument ? statsValue.AsBsonDocument : new BsonDocument();
                        //_selectedCard = stats;
                        _selectedCardId = cardId;
                        Console.WriteLine("Card id ==" +  cardId);
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
        
        public static async Task<ImageSource> LoadImageFromUrl(string imageUrl)
        {
            if (imageUrl.StartsWith("https://developer.api.autodesk.com"))
            {
                return await LoadImageFromAPI(imageUrl, TokenManager.GetToken());
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
        }
        
        private async void View3DButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCard == null)
            {
                MessageBox.Show("3D model unavailable for this card.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_selectedCardId))
            {
                MessageBox.Show("No card is selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Find the selected card in the cards collection
            var cardFilter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(_selectedCardId));
            var selectedCardData = _cardsCollection.Find(cardFilter).FirstOrDefault();

            if (selectedCardData == null)
            {
                MessageBox.Show("Card data not found in database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ensure the card has a model_id
            if (!selectedCardData.Contains("model_id") || selectedCardData["model_id"].IsBsonNull)
            {
                MessageBox.Show("This card does not have an associated 3D model.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedItemModelId = selectedCardData["model_id"].ToString();

            var mongo = new MongoConnection();
            IMongoCollection<BsonDocument> _modelDataCollection = mongo.GetCollection("ModelData");
            
            // Now search the ModelData collection for the _folderId using selectedItemModelId
            var modelFilter = Builders<BsonDocument>.Filter.Eq("_id", (selectedItemModelId));
            var selectedModelData = _modelDataCollection.Find(modelFilter).FirstOrDefault();

            if (selectedModelData == null || !selectedModelData.Contains("_folderid") || selectedModelData["_folderid"].IsBsonNull)
            {
                MessageBox.Show("Project ID (_folderId) not found for this model.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string projectFolderId = selectedModelData["_folderid"].ToString();

            // Find the MainWindow instance
            MainWindow? mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            if (mainWindow == null)
            {
                MessageBox.Show("MainWindow is not open, opening a new instance.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                mainWindow = new MainWindow();
                mainWindow.Show();
            }

            // Ensure we're passing the correct ID
            MessageBox.Show($"Opening model with project ID: {projectFolderId}");

            // Call BtnViewInApp_Click with _folderId instead of model_id
            
            // Ensure the function is being called
            MessageBox.Show($"Calling BtnViewInApp_Click with model ID: {selectedItemModelId}");
            mainWindow.BtnViewInApp_Click(selectedItemModelId, projectFolderId, 0);

            
        }

        private void AddCardButton_Click(object sender, RoutedEventArgs e)
        { 
            if (_deckId == null)
            {
                MessageBox.Show("Please select a deck first");
            }
            else
            {
                AddCardWindow addCardWindow = new AddCardWindow(_deckId);
                addCardWindow.Owner = this; // Set the owner to the deck (this) window
                addCardWindow.ShowDialog();
            }
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

        private async void DrawRandomCard_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_deckId))
            {
                MessageBox.Show("No deck selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Fetch the deck document
                var deckFilter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(_deckId));
                var deck = await _decksCollection.Find(deckFilter).FirstOrDefaultAsync();

                if (deck == null || !deck.Contains("cards") || !deck["cards"].IsBsonArray)
                {
                    MessageBox.Show("No cards found in this deck.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var cardsArray = deck["cards"].AsBsonArray;
                if (cardsArray.Count == 0)
                {
                    MessageBox.Show("Deck has no cards.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Pick a random card ID from the deck's card array
                var random = new Random();
                var randomIndex = random.Next(cardsArray.Count);
                var randomCardId = cardsArray[randomIndex].AsString; // Ensure it's stored as a string

                // Fetch the card details from the cards collection
                var cardFilter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(randomCardId));
                var card = await _cardsCollection.Find(cardFilter).FirstOrDefaultAsync();

                if (card == null)
                {
                    MessageBox.Show("Error loading card.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                string cardImageUrl = card["snapshot_url"].AsString;
                string cardName = card["name"].AsString;
                string cardDescription = card["description"].AsString;
                var statsValue = card.GetValue("stats", new BsonDocument());

                ImageSource imageUrlSource = await LoadImageFromUrl(cardImageUrl);

                BsonDocument stats = statsValue.IsBsonDocument ? statsValue.AsBsonDocument : new BsonDocument();
                DisplaySelectedCard(cardName, imageUrlSource, cardDescription, stats);
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error drawing a card: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }        
        }

        private async void ListOnMarketplace_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Clicked on marketplace button");
            ListDeckPrompt listPrompt = new ListDeckPrompt(_deckId, _decksCollection);
            listPrompt.Owner = this;
            listPrompt.Show();
        }
        
        private async void NewDeck_Click(object sender, RoutedEventArgs e)
        {
            // Prompt the user for deck name and description
            NewDeckPrompt prompt = new NewDeckPrompt();

            // Show the dialog and wait for the result
            if (prompt.ShowDialog() == true)
            {
                // Reload the decks to update the UI after the new deck has been created
                await LoadUserDecks();
            }
        }


        private async void AddStats_Click(object sender, RoutedEventArgs e)
        {
            var statName = Microsoft.VisualBasic.Interaction.InputBox("Enter stat name:", "Add Stat", "");
            var statValue = Microsoft.VisualBasic.Interaction.InputBox($"Enter value for {statName}:", "Add Stat", "");

            // Ensure both name and value are provided
            if (!string.IsNullOrWhiteSpace(statName) && !string.IsNullOrWhiteSpace(statValue))
            {
                if (_decksCollection != null && _deckId != null && _selectedCard != null)
                {
                    TextBlock statText = new TextBlock
                    {
                        Text = $"{statName}: {statValue}",
                        FontSize = 14,
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    statText.Style = (Style)FindResource("StatsTextStyle");
                    SelectedCardStatsPanel.Children.Add(statText);

                    // Upload stat to MongoDB
                    await UploadStatToMongo(statName, statValue);
                }
                else
                {
                    MessageBox.Show("Please select a deck and card", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("PLease fill all fields", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }
        
        private async Task UploadStatToMongo(string statName, string statValue)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(_selectedCardId));
                var update = Builders<BsonDocument>.Update.Set($"stats.{statName}", statValue);

                var result = await _cardsCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount > 0)
                    Console.WriteLine($"Updated {statName} to {statValue} successfully.");
                else
                    Console.WriteLine("No matching card found or no changes made.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating card: {ex.Message}");
            }
            LoadDeckCards(_deckId);
        }
    }
}
