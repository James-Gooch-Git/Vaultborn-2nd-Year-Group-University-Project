using System;
using System.Diagnostics;
//using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using AssetManager.Infrastructure.Services;
using Autodesk.Authentication;
using Autodesk.Authentication.Model;
using MongoDB.Driver;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using AssetManager.Infrastructure.Data;
using AssetManager.Infrastructure.Models;
using AssetManager.Infrastructure.Configuration;
using AssetManager.Infrastructure.Security;


namespace AssetManager.Desktop
{
    public partial class LoginWindow : Window
    {
        private static string ClientID => AppSecrets.ApsClientId;
        private const string TokenUrl = "https://developer.api.autodesk.com/authentication/v2/token";
        private const string RedirectUri = "http://localhost:8080/callback";
        private string loginURL = $"https://developer.api.autodesk.com/authentication/v2/authorize";
        private string _codeVerifier;
        private readonly TokenService _tokenService = new TokenService();

        private readonly string userSession;
        private readonly string aToken;
        private readonly string refreshToken;

        private bool _isLogout;

        public LoginWindow()
        {
            InitializeComponent();
            userSession = Environment.GetEnvironmentVariable("userId", EnvironmentVariableTarget.User);
            aToken = SessionTokenStore.Get("accessToken");
            refreshToken = SessionTokenStore.Get("refresh_token");
            TokenManager.SetRefreshToken(refreshToken);

            if (!string.IsNullOrEmpty(userSession) && !string.IsNullOrEmpty(aToken))
            {
                // ✅ Only validate session if token exists
                ValidateUserSession();
            }
            else
            {
                InitializeWebView(); // Only show login WebView if no valid session
            }
        }


        public LoginWindow(bool isLogout)
        {
            InitializeComponent(); // ✅ MUST be called first

            _isLogout = isLogout;

            if (_isLogout)
            {
               // HandleLogout();
            }
            else
            {
                InitializeWebView();
            }
        }
        public static void PerformLogout()
        {
            Console.WriteLine("👤 Logging out...");

            // Clear session state
            Environment.SetEnvironmentVariable("userId", "", EnvironmentVariableTarget.User);
            SessionTokenStore.Clear();
        }



        private void HandleLogout()
        {
            Console.WriteLine("👤 Logging out due to expired or invalid token...");

            // Clear user session and stored tokens
            Environment.SetEnvironmentVariable("userId", "", EnvironmentVariableTarget.User);
            SessionTokenStore.Clear();

            MessageBox.Show("⚠️ Your session has expired. Please log in again.", "Session Expired", MessageBoxButton.OK, MessageBoxImage.Warning);

            // 🚀 Reopen the login window
            Dispatcher.Invoke(() =>
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            });
        }


        private async void InitializeWebView()
        {
            try
            {
                await WebViewHelper.InitializeAsync(webView, Redirected);

                string nonce = GenerateNonce();
                Pkce pkce = GeneratePkce();
                _codeVerifier = pkce.CodeVerifier;
                string loginURL = $"https://developer.api.autodesk.com/authentication/v2/authorize"
                                  + $"?response_type=code"
                                  + $"&client_id={ClientID}"
                                  + $"&redirect_uri={RedirectUri}"
                                  + $"&scope=data:read%20data:write%20data:create%20bucket:read%20bucket:create%20bucket:update%20account:write%20viewables:read"
                                  + $"&nonce={nonce}"
                                  + $"&prompt=login"
                                  + $"&code_challenge={pkce.CodeChallenge}"
                                  + $"&code_challenge_method=S256";

                webView.CoreWebView2.Navigate(loginURL);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        /// <summary>
        /// Checks if the user has a valid session.
        /// </summary>
        /// <summary>
        /// Checks if the user has a valid session.
        /// If the access token is expired, logs out and returns to the login window.
        /// </summary>
        /// <summary>
        /// Checks if the user has a valid session.
        /// If the access token is expired, logs out and returns to the login window.
        /// </summary>
        private async void ValidateUserSession()
        {
            string userSession = Environment.GetEnvironmentVariable("userId", EnvironmentVariableTarget.User);
            string storedAccessToken = SessionTokenStore.Get("accessToken");
            string storedTwoLeggedToken = SessionTokenStore.Get("twoLeggedToken");

            if (!string.IsNullOrEmpty(userSession) && !string.IsNullOrEmpty(storedAccessToken))
            {
                bool isTokenValid = await IsTokenValid(storedAccessToken) && await VerifyTokenWithApiAsync(storedAccessToken);

                if (isTokenValid)
                {
                    Console.WriteLine("✅ Token is valid. Opening MainWindow...");
                    TokenManager.SetToken(storedAccessToken);
                    TokenManager.SetTwoLeggedToken(storedTwoLeggedToken);

                    // ✅ Ensure MainWindow opens only once
                    if (Application.Current.Windows.OfType<MainWindow>().Any())
                    {
                        Console.WriteLine("⚠️ MainWindow is already open, skipping...");
                        return; // Prevent duplicate opening
                    }

                    MainWindow mainWindow = new MainWindow(userSession);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    Console.WriteLine("⚠️ Token expired. Logging out...");
                    HandleLogout();
                }
            }
        }

        private async void InsertUserDataDB(UserInfo userInfo)
        {
            try
            {
                MongoConnection database = new MongoConnection();
                var findUser = await database.Users.Find(x => x.Id == userInfo.Sub).FirstOrDefaultAsync();

                if (findUser == null)
                {
                    User newUser = new User { Id = userInfo.Sub, Username = userInfo.PreferredUsername, Email = userInfo.Email, ProfilePic = userInfo.Picture };
                    await database.Users.InsertOneAsync(newUser);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error: {e.Message}");
            }
        }


        /// <summary>
        /// Clears stored credentials if the token is invalid.
        /// </summary>
        private void ClearStoredCredentials()
        {
            Environment.SetEnvironmentVariable("userId", "", EnvironmentVariableTarget.User);
            SessionTokenStore.Clear();
        }

        /// <summary>
        /// Validates if a JWT token is still valid.
        /// </summary>
        private async Task<bool> IsTokenValid(string token)
        {

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("❌ Token is null or empty");
                return false;
            }

            try
            {
                // Check token format
                string[] tokenParts = token.Split('.');
                if (tokenParts.Length != 3)
                {
                    Console.WriteLine("❌ Token does not have the correct JWT format (3 parts separated by dots)");
                    return false;
                }

                // Try to decode the middle part (payload)
                string base64Payload = tokenParts[1];
                int padding = 4 - (base64Payload.Length % 4);
                if (padding < 4)
                {
                    base64Payload += new string('=', padding);
                }

                base64Payload = base64Payload.Replace('-', '+').Replace('_', '/');

                // Try to decode and parse as JSON
                byte[] payloadBytes = Convert.FromBase64String(base64Payload);
                string payloadJson = Encoding.UTF8.GetString(payloadBytes);

                using JsonDocument document = JsonDocument.Parse(payloadJson);

                // Check expiration
                if (document.RootElement.TryGetProperty("exp", out JsonElement expElement))
                {
                    long expTimestamp = expElement.GetInt64();
                    DateTime expDateTime = DateTimeOffset.FromUnixTimeSeconds(expTimestamp).DateTime.ToLocalTime();

                    if (DateTime.Now > expDateTime)
                    {
                        Console.WriteLine($"❌ Token expired at {expDateTime}");
                        string[] scopes = new[] {
                            "data:read",
                            "data:write",
                            "bucket:read",
                            "bucket:create",
                            "viewables:read"
                        };

                        // Pass the scopes parameter to Get2LeggedTokenAsync
                        string twoLeggedToken = await _tokenService.Get2LeggedTokenAsync(scopes);
                        TokenManager.SetTwoLeggedToken(twoLeggedToken);
                        SessionTokenStore.Set("twoLeggedToken", twoLeggedToken);
                        await RefreshToken();
                        return true;
                    }

                    // Token is valid and not expired
                    TimeSpan timeLeft = expDateTime - DateTime.Now;
                    Console.WriteLine($"✅ Token valid for: {timeLeft.Hours}h {timeLeft.Minutes}m {timeLeft.Seconds}s");
                    return true;
                }

                // If no expiration found, check for other required properties
                if (!document.RootElement.TryGetProperty("scope", out _))
                {
                    Console.WriteLine("❌ Token is missing scope property");
                    return false;
                }

                // If we made it here, the token seems valid
                Console.WriteLine("✅ Token appears valid but couldn't verify expiration");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception validating token: {ex.Message}");
                return false;
            }
        }

        // Method to validate access tokens before making API calls
        private async Task<bool> VerifyTokenWithApiAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            try
            {
                // Use a simple API call to check if the token is accepted
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // APS user profile endpoint is a good test
                string testUrl = "https://developer.api.autodesk.com/userprofile/v1/users/@me";

                HttpResponseMessage response = await client.GetAsync(testUrl);

                // If the response is 401 Unauthorized, the token is invalid
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("❌ Token rejected by API (401 Unauthorized)");
                    return false;
                }

                // Any other successful response means the token is valid
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Token accepted by API");
                    return true;
                }

                // Any other status code may indicate other issues
                Console.WriteLine($"⚠️ API returned {response.StatusCode} when validating token");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception verifying token with API: {ex.Message}");
                return false;
            }
        }
        
        private static async Task RefreshToken()
        {
            string tokenURL = "https://developer.api.autodesk.com/authentication/v2/token";
            string refreshToken = TokenManager.GetRefreshToken();

            using (HttpClient client = new HttpClient())
            {
                var tokenContent = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken },
                    { "client_id", ClientID },
                    {  "scope", "data:read data:write data:create bucket:read bucket:create bucket:update account:write" } ,
                };

                var tokenParameters = new FormUrlEncodedContent(tokenContent);
                var tokenResponse = await client.PostAsync(tokenURL, tokenParameters);
                var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Token refresh failed: {tokenResponse.StatusCode}");
                    return;
                }
                var tokenData = JsonConvert.DeserializeObject<TokenData>(tokenResponseContent);
                TokenManager.SetToken(tokenData.access_token);
                TokenManager.SetRefreshToken(tokenData.refresh_token);
                SessionTokenStore.Set("accessToken", tokenData.access_token);
                SessionTokenStore.Set("refresh_token", tokenData.refresh_token);
            }
        }

        /// <summary>
        /// Handles the login button click event.
        /// </summary>
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nonce = GenerateNonce();
                Pkce pkce = GeneratePkce();
                _codeVerifier = pkce.CodeVerifier;

                string loginURL = $"https://developer.api.autodesk.com/authentication/v2/authorize"
                                  + $"?response_type=code"
                                  + $"&client_id={ClientID}"
                                  + $"&redirect_uri={RedirectUri}"
                                  + $"&scope=data:read data:write data:create bucket:read bucket:create bucket:update account:read account:write"
                                  + $"&nonce={nonce}"
                                  + $"&prompt=login"
                                  + $"&code_challenge={pkce.CodeChallenge}"
                                  + $"&code_challenge_method=S256";

                await WebViewHelper.InitializeAsync(webView, Redirected);
                webView.CoreWebView2.Navigate(loginURL);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error Logging in: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles redirection after authentication.
        /// </summary>
        private async void Redirected(object sender, CoreWebView2NavigationStartingEventArgs args)
        {

            if (args.Uri.StartsWith(RedirectUri))
            {
                args.Cancel = true;
                Uri uri = new Uri(args.Uri);
                string authCode = HttpUtility.ParseQueryString(uri.Query).Get("code");

                if (!string.IsNullOrEmpty(authCode))
                {
                    await GetAccessToken(authCode);
                }
            }
        }



        /// <summary>
        /// Fetches access tokens using the authorization code.
        /// </summary>
        private async Task GetAccessToken(string authCode)
        {
            TokenService _tokenService = new TokenService();
            try
            {
                // Debug token status before
                Console.WriteLine($"Before token request - TokenManager has token: {!string.IsNullOrEmpty(TokenManager.GetToken())}");

                string token = await _tokenService.GetThreeLeggedAccessTokenAsync(authCode, _codeVerifier);
                Console.WriteLine($"Received 3-legged token: {!string.IsNullOrEmpty(token)}");

                // Define the required scopes for 2-legged authentication
                string[] scopes = new[] {
            "data:read",
            "data:write",
            "bucket:read",
            "bucket:create",
            "viewables:read"
        };

                // Pass the scopes parameter to Get2LeggedTokenAsync
                string twoLeggedToken = await _tokenService.Get2LeggedTokenAsync(scopes);
                Console.WriteLine($"Received 2-legged token: {!string.IsNullOrEmpty(twoLeggedToken)}");

                // Set tokens in TokenManager
                TokenManager.SetToken(token);
                TokenManager.SetTwoLeggedToken(twoLeggedToken);
                Console.WriteLine("✅ Tokens stored in TokenManager");

                // Verify tokens were saved correctly
                Console.WriteLine($"After storage - TokenManager has token: {!string.IsNullOrEmpty(TokenManager.GetToken())}");

                // Persist tokens encrypted at rest
                SessionTokenStore.Set("accessToken", token);
                SessionTokenStore.Set("twoLeggedToken", twoLeggedToken);
                SessionTokenStore.Set("refresh_token", TokenManager.GetRefreshToken());

                // Now get user data with the token we just received
                GetUserData(token);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves user data from Autodesk.
        /// </summary>
        private async void GetUserData(string tokenParam)
        {
            try
            {
                string token = !string.IsNullOrEmpty(tokenParam) ? tokenParam : TokenManager.GetToken();

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("❌ No valid token available");
                    MessageBox.Show("Authentication token is missing or invalid. Please log in again.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Console.WriteLine("🔍 Getting user data with token...");

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                string userProfileUrl = "https://developer.api.autodesk.com/userprofile/v1/users/@me";
                HttpResponseMessage response = await client.GetAsync(userProfileUrl);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Failed to get user info: {response.StatusCode}");
                    HandleLogout();
                    return;
                }

                using JsonDocument document = JsonDocument.Parse(responseContent);
                string userName = document.RootElement.GetProperty("firstName").GetString() + " " +
                                  document.RootElement.GetProperty("lastName").GetString();
                string userId = document.RootElement.GetProperty("userId").GetString();
                string email = document.RootElement.GetProperty("emailId").GetString();

                // 🖼️ Get profile picture if available
                string profileImageUrl = string.Empty;
                if (document.RootElement.TryGetProperty("profileImages", out JsonElement profileImages) &&
                    profileImages.TryGetProperty("sizeX42", out JsonElement imageUrlElement))
                {
                    profileImageUrl = imageUrlElement.GetString();
                }

                Console.WriteLine($"✅ User data retrieved: {userName} ({email})");

                // Store user info in environment variables if needed
                Environment.SetEnvironmentVariable("userName", userName, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("userId", userId, EnvironmentVariableTarget.User);

                // Create a UserInfo object with the retrieved data
                UserInfo userDataResponse = new UserInfo
                {
                    Sub = userId,
                    PreferredUsername = userName,
                    Email = email,
                    Picture = profileImageUrl
                };

                // 🧠 Pass user info to your database handler
                InsertUserDataDB(userDataResponse);

                if (!Application.Current.Windows.OfType<MainWindow>().Any())
                {
                    Dispatcher.Invoke(() =>
                    {
                        MainWindow mainWindow = new MainWindow(responseContent);
                        this.Close();
                        mainWindow.Show();
                    });
                }
                else
                {
                    Console.WriteLine("⚠️ MainWindow already open, skipping duplicate launch.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in GetUserData: {ex.Message}");
                MessageBox.Show($"Error retrieving user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        /// <summary>
        /// Generates a secure nonce.
        /// </summary>
        private string GenerateNonce()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Generates PKCE challenge and verifier.
        /// </summary>
        private Pkce GeneratePkce()
        {
            var pkce = new Pkce
            {
                CodeVerifier = CryptoRandom.CreateUniqueId(32)
            };
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pkce.CodeVerifier));
                pkce.CodeChallenge = Base64Url.Encode(challengeBytes);
            }
            return pkce;
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
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
    }

    public static class CryptoRandom
    {
        public static string CreateUniqueId(int length)
        {
            byte[] randomBytes = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

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
            string base64 = Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return base64;
        }
    }
}
