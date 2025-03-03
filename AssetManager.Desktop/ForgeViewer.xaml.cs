using System;
using System.Text;
using System.Windows;
using AssetManager.Infrastructure.Services;
using Microsoft.Web.WebView2.Core;

namespace AssetManager.Desktop
{
    public partial class ForgeViewerWindow : Window
    {
        private string _modelUrn;

        public ForgeViewerWindow(string modelUrn)
        {
            InitializeComponent();
            _modelUrn = modelUrn;
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string accessToken = TokenManager.GetToken();
                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("❌ Access token is missing.");
                    MessageBox.Show("Authentication error. Please log in again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                Console.WriteLine("🔄 Initializing WebView2...");
                await ForgeWebView.EnsureCoreWebView2Async();
                Console.WriteLine("✅ WebView2 initialized successfully.");

                string htmlContent = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta http-equiv='X-UA-Compatible' content='IE=Edge' />
    <title>Forge Viewer</title>
    <script src='https://developer.api.autodesk.com/modelderivative/v2/viewers/7.52/viewer3D.min.js'></script>
    <link rel='stylesheet' href='https://developer.api.autodesk.com/modelderivative/v2/viewers/7.52/style.min.css' type='text/css'>
</head>
<body>
    <div id='forgeViewer' style='width: 100%; height: 100vh;'></div>
    <script>
        var options = {{
            env: 'AutodeskProduction',
            getAccessToken: function(onTokenReady) {{
                onTokenReady('{accessToken}', 3599);
            }}
        }};
        var documentId = 'urn:{_modelUrn}';
        Autodesk.Viewing.Initializer(options, function() {{
            var viewerDiv = document.getElementById('forgeViewer');
            var viewer = new Autodesk.Viewing.GuiViewer3D(viewerDiv);
            viewer.start();
            Autodesk.Viewing.Document.load(documentId, function(doc) {{
                var defaultModel = doc.getRoot().getDefaultGeometry();
                viewer.loadDocumentNode(doc, defaultModel);
            }}, function(errorMsg) {{
                console.error('Error loading document: ' + errorMsg);
            }});
        }});
    </script>
</body>
</html>";

                ForgeWebView.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WebView2 initialization failed: {ex.Message}");
            }
        }
    }
}