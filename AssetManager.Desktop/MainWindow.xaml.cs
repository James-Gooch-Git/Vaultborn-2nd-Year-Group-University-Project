using System.Windows;
using AssetManager.Infrastructure.Services;
using ForgeViewerApp;
using Microsoft.Win32;

namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly AutodeskApiService _autodeskService;
        private readonly ModelUpload _uploadService = new ModelUpload();

        public MainWindow()
        {
            InitializeComponent();
            var tokenService = new TokenService();
            _autodeskService = new AutodeskApiService(tokenService);
        }

        private async void BtnCreateBucket_Click(object sender, RoutedEventArgs e)
        {
            // Create a bucket
            string bucketName = "assetbucket19"; 
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
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a Model File",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string accessToken = await TokenService.GetAccessTokenAsync(); // 🔹 Get token

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
        
        private void DownloadModelButton_Click(object sender, RoutedEventArgs e)
        {
            Download downloadWindow = new Download();
    
            // Manually trigger the download function inside Download.xaml.cs
            downloadWindow.DownloadModelButton_Click(sender, e);        
        }
    }
}
