using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows;
using System.Threading.Tasks;

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
            string bucketName = "assetBucket"; // Replace with a unique name
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
            string bucketKey = "assetBucket"; // Replace with an actual bucket key
            string filePath = @"C:\Users\tomgr\source\repos\AssetManager\Uploads\test.txt"; // Replace with the actual file path

            string objectId = await OssService.UploadFile(bucketKey, filePath);

            if (!string.IsNullOrEmpty(objectId))
            {
                MessageBox.Show($"File uploaded successfully! Object ID: {objectId}");
            }
            else
            {
                MessageBox.Show("Failed to upload file.");
            }
        }
    }
}
