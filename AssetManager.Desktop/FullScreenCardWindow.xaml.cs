using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AssetManager.Desktop;

public partial class FullScreenCardWindow : Window
{
    public FullScreenCardWindow(string cardName, ImageSource cardImage, string cardDescription, string cardStats)
    {
        InitializeComponent();

        // Assign values to UI elements
        FullScreenCardImage.Source = cardImage;
        FullScreenCardName.Text = cardName;
        FullScreenCardDescription.Text = cardDescription;
        FullScreenCardStats.Text = cardStats;

        // Close when clicking outside
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Close();
    }
}
