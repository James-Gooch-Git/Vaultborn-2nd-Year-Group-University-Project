using System.Net.Http;
using System.Web;
using System.Windows;

namespace AssetManager.Desktop;

public partial class LoginWindow : Window
{
    private string loginURL = "https://developer.api.autodesk.com/authentication/v2/authorize";
    private string tokenURL = "https://developer.api.autodesk.com/authentication/v2/token";
    string clientID = "";
    private string redirect = "";
    string grantType = "authorization_code";
    
    public LoginWindow()
    {
        InitializeComponent();

        if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1].StartsWith(redirect))
        {
            
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        using (HttpClient client = new HttpClient())
        {
            string nonce = "";
            Pkce pkce = new Pkce();
            var loginData = new Dictionary<string, string>
            {
                { "response_type", "code" },
                { "client_id", clientID },
                { "redirect_uri", redirect },
                { "scope", "data:read" },
                { "nonce", nonce },
                { "prompt", "login" },
                { "code_challenge", pkce.CodeChallenge },
                { "code_challenge_method", "S256" }
            };

            var loginParameters = new FormUrlEncodedContent(loginData);
            var loginRepsonse = await client.PostAsync(loginURL, loginParameters);
            string loginResponseData = await loginRepsonse.Content.ReadAsStringAsync();
            Uri uri = new Uri(loginResponseData);
            string authCode = HttpUtility.ParseQueryString(uri.Query).Get("code");
        }
    }

    private async void GetAcessToken(string authCode)
    {
        
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
}