using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Windows;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using AssetManager.Infrastructure.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.Raw;
using Autodesk.Authentication;
using Autodesk.Authentication.Model;

namespace AssetManager.Desktop;

public partial class LoginWindow : Window
{
    //private string loginURL = "https://developer.api.autodesk.com/authentication/v2/authorize";
    private string tokenURL = "https://developer.api.autodesk.com/authentication/v2/token";
    string clientID = "ONI3GGJaqwHUKpXUmOJeYUfUMu5UUfNX11oqHSxuuLFr0ELv";
    private string redirect = "https://localhost/auth/callback";
    string grantType = "authorization_code";
    public string _codeVerifier;
    private readonly TokenService _tokenService = new TokenService();
    
    public LoginWindow()
    {
        InitializeComponent();
    }
    
    public class TokenManager
    {
        private static string _accessToken;
        public static void SetToken(string token) => _accessToken = token;
        public static string GetToken() => _accessToken;
    }


    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string nonce = GenerateNonce();
            Pkce pkce = GeneratePkce();
            _codeVerifier = pkce.CodeVerifier;

            string loginURL = $"https://developer.api.autodesk.com/authentication/v2/authorize"
                              + $"?response_type=code"
                              + $"&client_id={clientID}"
                              + $"&redirect_uri={redirect}"
                              + $"&scope=data:read%20data:write%20data:create%20bucket:read%20bucket:create%20bucket:update%20account:write"
                              + $"&nonce={nonce}"
                              + $"&prompt=login"
                              + $"&code_challenge={pkce.CodeChallenge}"
                              + $"&code_challenge_method=S256";

            if (webView.CoreWebView2 == null)
            {
                Console.WriteLine("❌ WebView is not initialized. Initializing...");
                await webView.EnsureCoreWebView2Async(); // ✅ Ensure initialization
            }

            webView.CoreWebView2.NavigationStarting -= Redirected;
            webView.CoreWebView2.NavigationStarting += Redirected;

            Console.WriteLine($"🔹 Navigating to: {loginURL}");
            webView.CoreWebView2.Navigate(loginURL);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error Logging in: {ex.Message}");
        }
    }

    private async void GetAccessToken(string authCode)
    {
        try
        {
            string clientId = clientID; // Use the client ID from the class variable

            string token = await _tokenService.GetAccessTokenAsync(authCode, _codeVerifier, clientId);
            MessageBox.Show($"✅ Access Token: {token}");
            GetUserData(token);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"❌ Error: {ex.Message}");
        }
    }

    

    private async void GetUserData(string accessToken)
    {
        AuthenticationClient authClient = new AuthenticationClient();
        UserInfo userDataResponse = await authClient.GetUserInfoAsync(accessToken);
        MessageBox.Show($"Id: {userDataResponse.Sub}, Name: {userDataResponse.Name}, Email: {userDataResponse.Email}");
        MainWindow mainWindow = new MainWindow(userDataResponse);
        mainWindow.Show();
        this.Close();
    }

    private async void Redirected(object sender, CoreWebView2NavigationStartingEventArgs args)
    {
        //MessageBox.Show($"Redirect URL: {args.Uri}");
        //string redirectedURL = args.Uri;
        if (args.Uri.StartsWith("https://localhost/auth/callback"))
        {
            args.Cancel = true;
            Uri uri = new Uri(args.Uri);
            string authCode = HttpUtility.ParseQueryString(uri.Query).Get("code");

            if (!string.IsNullOrEmpty(authCode))
                GetAccessToken(authCode);
        }
    }

    public class Pkce
    {
        public string CodeVerifier { get; set; }
        public string CodeChallenge { get; set; }
    }

    public class TokenData
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }  // ✅ Fix: Ensure this is an integer
        public string refresh_token { get; set; }
    }


    public class UserData
    {
        public string sub { get; set; } //user id
        public string name { get; set; } //full name
        public string given_name { get; set; } //first name
        public string family_name { get; set; } //last name
        public string preferred_username { get; set; } //username
        public string email { get; set; } //email address
        public string picture { get; set; } // profile pic
    }

    public string GenerateNonce()
    {
        return Guid.NewGuid().ToString("N");
    }

    public Pkce GeneratePkce()
    {
        var pkce = new Pkce
        {
            CodeVerifier = CryptoRandom.CreateUniqueId(32)
        };
        using (var sha256 = SHA256.Create())
        {
            // Here we create a hash of the code verifier
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pkce.CodeVerifier));

            // and produce the "Code Challenge" from it by base64Url encoding it.
            pkce.CodeChallenge = Base64Url.Encode(challengeBytes);
        }
        return pkce;
    }
    
    public static class CryptoRandom
    {
        // This method generates a secure random string of a specified length
        public static string CreateUniqueId(int length)
        {
            // Create a byte array to hold random data
            byte[] randomBytes = new byte[length];

            // Using RNGCryptoServiceProvider to generate cryptographically secure random data
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            // Convert the random bytes into a Base64 string and make it URL-safe by removing padding and replacing characters
            return Convert.ToBase64String(randomBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }

    public static class Base64Url
    {
        public static string Encode(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);

            // Remove padding character '=' from the end
            base64 = base64.TrimEnd('=');

            // Replace '+' with '-' and '/' with '_'
            base64 = base64.Replace('+', '-').Replace('/', '_');

            return base64;
        }
    }
}