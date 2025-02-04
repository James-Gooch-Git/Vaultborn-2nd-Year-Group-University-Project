using System.Windows;
using AssetManager.Infrastructure.Services;
using ForgeViewerApp;

namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly AutodeskApiService _autodeskService;

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
            try
            {
                string bucketName = "assetbucket19";
                string filePath = @"C:\Users\tomgr\source\repos\AssetManager\Uploads\p3166.glb";

                //string bucketKey = await OssService.CreateBucket(bucketName);
                string bucketKey = bucketName;
                
                string urn = await _autodeskService.UploadAndTranslateAsync(bucketKey, filePath);
                MessageBox.Show($"Upload successful! Model URN: {urn}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Upload Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OpenDownloadWindow_Click(object sender, RoutedEventArgs e)
        {
            Download downloadWindow = new Download();
            downloadWindow.Show();
        }
    }
}
