using System;
using System.Threading.Tasks;
using System.Windows;
using AssetManager.Infrastructure.Services;
using Autodesk.Authentication.Model;
using Microsoft.Win32;

namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly ModelUpload _uploadService = new ModelUpload();
        private string _projectId = "PROJECT_ID";
        private string _folderId = "FOLDER_ID";


        public MainWindow(UserInfo userData)
        {
            InitializeComponent();
            BtnUploadFile.Click += BtnUploadFile_Click;
            LoadProjectIdAsync();
        }

        private async void LoadProjectIdAsync()
        {
            try
            {
                string hubId = await DataManagement.GetPersonalHub();  // ✅ Await it first
                if (hubId == null)
                {
                    Console.WriteLine("❌ Failed to retrieve Hub ID.");
                    return;
                }

                _projectId = await DataManagement.GetProjectIdAsync(hubId); // ✅ Now it's a string
                Console.WriteLine($"📂 Loaded Project ID: {_projectId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }


        private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("📤 Upload button clicked");

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a Model File",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                Console.WriteLine($"📂 File selected: {filePath}");

                string accessToken = await GetAccessToken();
                if (string.IsNullOrEmpty(accessToken))
                {
                    MessageBox.Show("❌ Access Token is missing!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    string fileUrn = await _uploadService.UploadModel(filePath, _projectId, _folderId, accessToken);
                    MessageBox.Show($"✅ Upload Successful!\nFile URN: {fileUrn}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Upload Failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task<string> GetAccessToken()
        {
            return await Task.Run(() => LoginWindow.TokenManager.GetToken());
        }
    }
}
