using System.Windows;
using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.Services;
using AssetManager.Infrastructure.Models;
using Autodesk.Authentication.Model;
using Microsoft.Win32;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssetManager.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly ModelUpload _uploadService = new ModelUpload();
        private string projectId = "PROJECT_ID";
        private string folderId = "FOLDER_ID";

        public MainWindow(string uId)
        {
            InitializeComponent();
            string userId = uId;
            MessageBox.Show($"Access Token: {Infrastructure.Services.TokenManager.GetToken()}");
            //WelcomeMessage.Content += userId;
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

        private void BtnLogout_OnClick_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow(true);
            loginWindow.Show();
            this.Close();
        }

        private void BtnViewComment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnAddComment.Visibility = Visibility.Visible;
                CommentContent.Visibility = Visibility.Visible;
                ListComments.Visibility = Visibility.Visible;
                ListAllComments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
        
        private async void BtnAddComment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string comment = CommentContent.Text;

                if (!string.IsNullOrEmpty(comment) && !comment.StartsWith("Add a comment"))
                {
                    MongoConnection database = new MongoConnection();
                    Comment commentContent = new Comment
                    {
                        CommentId = ObjectId.GenerateNewId(),
                        AssetId = "001",
                        UserId = Environment.GetEnvironmentVariable("userId"),
                        Content = comment,
                        CreatedDateTime = DateTime.Now
                    };
                    
                    await database.Comments.InsertOneAsync(commentContent);
                    ListNewComment(commentContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async void ListAllComments()
        {
            try
            {
                List<Comment> comments = await GetAllComments();
                List<CommentItem> commentItems = new List<CommentItem>();
                
                foreach (Comment comment in comments)
                {
                    commentItems.Add(new CommentItem {User = "test", Content = comment.Content, CreatedDateTime = comment.CreatedDateTime});
                }

                if (commentItems.Count != 0)
                {
                    foreach (CommentItem commentItem in commentItems)
                    {
                        ListComments.Items.Add(commentItem);
                    }
                }
                else
                {
                    ListComments.Items.Add(new CommentItem
                    {
                        User = String.Empty, Content = "There are no Comments", CreatedDateTime = DateTime.MinValue
                    });
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error: {e.Message}");
            }
        }

        private async void ListNewComment(Comment commentItem)
        {
            List<Comment> comments = await GetAllComments();
                
            foreach (Comment comment in comments)
            {
                if (commentItem.CommentId == comment.CommentId)
                    ListComments.Items.Add(new CommentItem {User = "test", Content = comment.Content, CreatedDateTime = comment.CreatedDateTime});
            }
        }
        
        private async Task<List<Comment>> GetAllComments(string assetId = "001")
        {
            try
            {
                MongoConnection database = new MongoConnection();
                var allComments = await database.Comments.Find(x => x.AssetId == assetId ).ToListAsync();
                return allComments;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error: {e.Message}");
                throw;
            }
        }

        private class CommentItem
        {
            public string User { get; set; }
            public string Content { get; set; }
            public DateTime CreatedDateTime { get; set; }
        }
    }
}
