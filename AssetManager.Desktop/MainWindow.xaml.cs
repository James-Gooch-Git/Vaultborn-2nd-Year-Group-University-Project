using System.Windows;
using AssetManager.Infrastructure.Services;
using Autodesk.Authentication.Model;
using Microsoft.Win32;

namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly ModelUpload _uploadService = new ModelUpload();
        private string projectId = "PROJECT_ID";
        private string folderId = "FOLDER_ID";

        public MainWindow(UserInfo userData)
        {
            InitializeComponent();
            BtnUploadFile.Click += BtnUploadFile_Click;

            //DATA TESTING
            //DataManagement.GetPersonalHub();
            DataManagement.GetProjectIdAsync(DataManagement.GetPersonalHub());
        }

        private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Button Clicked");

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
            LoginWindow loginWindow = new LoginWindow();
            this.Hide();
            loginWindow.Show();
        }
        
        private Task<string> GetAccessToken()
        {
            string token = LoginWindow.TokenManager.GetToken();
            Console.WriteLine($"🔹 Access Token: {token}");
            return Task.FromResult(token);
        }

    }
}
