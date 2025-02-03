using System.Windows;
using AssetManager.Infrastructure.Services;

namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void BtnCreateBucket_Click(object sender, RoutedEventArgs e)
        {
            // Create a bucket
            string bucketName = "assetbucket1"; 
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
            // Upload file to the bucket
            string bucketName = "assetbucket11"; 
            string filePath = @"C:\Users\tomgr\source\repos\AssetManager\Uploads\test.txt"; 
            string fileName = System.IO.Path.GetFileName(filePath);

            string bucketKey = await OssService.CreateBucket(bucketName);
            string token = await AuthService.GetAccessToken();
            var ossService = new OssService(token);

            // Step 1: Get Signed URL
            string signedUrl = await ossService.GetSignedUploadUrlAsync(bucketKey, fileName);
    
            if (string.IsNullOrEmpty(signedUrl))
            {
                MessageBox.Show("Failed to get signed URL.");
                return;
            }

            // Step 2: Upload file using signed URL
            bool success = await ossService.UploadFileToSignedUrlAsync(signedUrl, filePath);

            MessageBox.Show(success ? "File uploaded successfully!" : "File upload failed.");
        }
    }
}
