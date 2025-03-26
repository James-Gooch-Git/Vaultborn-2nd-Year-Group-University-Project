using System.Windows;
using AssetManager.Infrastructure.DOC;

namespace AssetManager.Desktop;

public partial class NewDeckPrompt : Window
{
    private readonly CreateDeck _createDeck;

    // Constructor accepts CreateDeck instance
    public NewDeckPrompt()
    {
        InitializeComponent();
        //MessageText.Text = "Create a New Deck"; 
    }

    // Change Ok_Click to async so we can call CreateDeck.NewDeck asynchronously
    private async void Ok_Click(object sender, RoutedEventArgs e)
    {
        var deckName = DeckNameBox.Text;
        var deckDescription = DeckDescriptionBox.Text;

        if (string.IsNullOrWhiteSpace(deckName) || string.IsNullOrWhiteSpace(deckDescription))
        {
            MessageBox.Show("Card Name, 3D Model and Image URL are required.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var createDeck = new CreateDeck();

        try
        {
            await createDeck.AddNewDeck(MainWindow._userId, deckName, deckDescription);
                
            MessageBox.Show("Card added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
        catch
        {
            MessageBox.Show("Failed to add card. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}