using System.Windows;
using AssetManager.Infrastructure.Services;
using Microsoft.Win32;

namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly ModelUpload _uploadService = new ModelUpload();
        private string projectId = "YOUR_PROJECT_ID";
        private string folderId = "YOUR_FOLDER_ID";

        public MainWindow()
        {
            InitializeComponent();
            BtnUploadFile.Click += BtnUploadFile_Click;

        }

        private async void BtnCreateBucket_Click(object sender, RoutedEventArgs e)
        {
            // Create a bucket
            string bucketName = "assetbucket15"; 
            string bucketKey = await OssService.CreateBucket(bucketName);

            if (!string.IsNullOrEmpty(bucketKey))
            {
                MessageBox.Show($"Bucket created successfully! Bucket Key: {bucketKey}");
            }
            else
            {
                MessageBox.Show("Failed to create bucket.");
            }
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
            return Task.FromResult(token);
        }

    }
}
