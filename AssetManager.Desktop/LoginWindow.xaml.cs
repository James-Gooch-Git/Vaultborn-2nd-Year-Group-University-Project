using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Windows;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.Raw;

namespace AssetManager.Desktop;

public partial class LoginWindow : Window
{
    //private string loginURL = "https://developer.api.autodesk.com/authentication/v2/authorize";
    private string tokenURL = "https://developer.api.autodesk.com/authentication/v2/token";
    string clientID = "ONI3GGJaqwHUKpXUmOJeYUfUMu5UUfNX11oqHSxuuLFr0ELv";
    private string redirect = "https://localhost/auth/callback";
    string grantType = "authorization_code";
    public string codeVerifier;
    
    public LoginWindow()
    {
        InitializeComponent();

        /*if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1].StartsWith(redirect))
        {
            Uri uri = new Uri(Environment.GetCommandLineArgs()[1]);
            string authCode = HttpUtility.ParseQueryString(uri.Query).Get("code");
            GetAccessToken(authCode);
        }*/
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string nonce = GenerateNonce();
            Pkce pkce = GeneratePkce();
            codeVerifier = pkce.CodeVerifier;
            string loginURL = $"https://developer.api.autodesk.com/authentication/v2/authorize?response_type=code&client_id={clientID}&redirect_uri={redirect}&scope=data:read%20user-profile:read&nonce={nonce}&prompt=login&code_challenge={pkce.CodeChallenge}&code_challenge_method=S256";

            webView.CoreWebView2.NavigationStarting -= Redirected;
            webView.CoreWebView2.NavigationStarting += Redirected;
            
            webView.CoreWebView2.Navigate(loginURL);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Logging in: {ex.Message}");
            throw;
        }
    }

    private async void GetAccessToken(string authCode)
    {
        using (HttpClient client = new HttpClient())
        {
            var tokenContent = new Dictionary<string, string>
            {
                { "grant_type", grantType },
                { "client_id", clientID },
                { "code_verifier", codeVerifier },
                { "code", authCode },
                { "redirect_uri", redirect }
            };
            
            var tokenParameters = new FormUrlEncodedContent(tokenContent);
            var tokenResponse = await client.PostAsync(tokenURL, tokenParameters);
            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<TokenData>(tokenResponseContent);
            MessageBox.Show($"Access Token: {tokenData.AccessToken}");
        }
    }

    private async void GetUserData(string accessToken)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var userDataResponse = await client.GetAsync("https://api.userprofile.autodesk.com/userinfo");
            string userDataContent = await userDataResponse.Content.ReadAsStringAsync();
            MessageBox.Show($"User Data: {userDataContent}");
        }
    }

    private async void Redirected(object sender, CoreWebView2NavigationStartingEventArgs args)
    {
        string redirectedURL = args.Uri;
        if (redirectedURL.StartsWith("https://localhost/auth/callback"))
        {
            args.Cancel = true;
            Uri uri = new Uri(Environment.GetCommandLineArgs()[1]);
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
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public string ExpiresIn { get; set; }
        public string RefreshToken { get; set; }
        
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