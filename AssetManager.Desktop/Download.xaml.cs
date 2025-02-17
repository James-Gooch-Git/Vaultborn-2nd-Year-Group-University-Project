using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using AssetManager.Infrastructure.Services;
using IdentityModel.OidcClient;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace AssetManager.Desktop
{
    public partial class Download : Window
    {
        //private readonly FileDownloadService _fileDownloadService = new();
        private string projectId = "Admin Project";
        private string folderId = "Folder";
        string accessToken = TokenManager.GetToken();
        string itemId = "your_file_item_id";
        string savePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "Downloads"));

            
        public Download()
        {
            InitializeComponent();
        }

        public async void DownloadModelButton_Click(object sender, RoutedEventArgs e)
        {
            string rootFolder = savePath;
            Console.WriteLine($"dl path: {savePath}");
            string defaultDownloadPath = Path.Combine(rootFolder, "Downloads");
            Console.WriteLine("path: " + defaultDownloadPath);
            Console.WriteLine("rootFolder: " + rootFolder);
            
            if (!Directory.Exists(defaultDownloadPath))
                Directory.CreateDirectory(defaultDownloadPath);

            string selectedFolderPath = SelectDownloadFolder(defaultDownloadPath);
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                MessageBox.Show("No folder selected. Download cancelled.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string localFilePath = Path.Combine(selectedFolderPath, "DownloadedModel.ext");

            if (!string.IsNullOrEmpty(accessToken))
            {
                await FileDownloadService.DownloadFileAsync(accessToken, projectId, folderId, itemId, savePath);
                MessageBox.Show($"Download Complete!\nFile saved to: {localFilePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Authentication Failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string SelectDownloadFolder(string defaultPath)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = defaultPath,
                Title = "Select a folder to save the downloaded model"
            };

            return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : null;
        }

        // private async Task<string> AuthenticateAsync()
        // {
        //     var options = new OidcClientOptions
        //     {
        //         Authority = "https://developer.api.autodesk.com/authentication/v2/authorize",
        //         ClientId = ClientId,
        //         RedirectUri = RedirectUri,
        //         Scope = Scope,
        //         ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect,
        //         Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCodePkce
        //     };
        //
        //     var oidcClient = new OidcClient(options);
        //     var loginResult = await oidcClient.LoginAsync(new LoginRequest());
        //
        //     return loginResult.IsError ? null : loginResult.AccessToken;
        // }
    }
}
