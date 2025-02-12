using System.Windows;
using AssetManager.Infrastructure.Services;
using Autodesk.Authentication.Model;
using Microsoft.Win32;
using AssetManager.Core;

namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly ModelUpload _uploadService = new ModelUpload();
        private string projectId = "Admin Project";
        private string folderId = "Folder";

        public MainWindow(string uId)
        {
            InitializeComponent();
            string userId = uId;
            //WelcomeMessage.Content += userId;
        }

        private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            HelloWorld.RunHW();

            Console.WriteLine("Upload Button Clicked");

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a Model File",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string accessToken = await GetAccessToken(); // 🔹 Get token

                try
                {
                    string fileUrn = await _uploadService.UploadModel(filePath, projectId, folderId, accessToken);
                    MessageBox.Show($"✅ Upload Successful!\nFile URN: {fileUrn}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Upload Failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnSwitchLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow(true);
            this.Hide();
            loginWindow.Show();
            this.Close();
        }
        
        private Task<string> GetAccessToken()
        {
            string token = TokenManager.GetToken();
            Console.WriteLine($"🔹 (mainwindow) Access Token: {token}");
            return Task.FromResult(token);
        }

    }
}
