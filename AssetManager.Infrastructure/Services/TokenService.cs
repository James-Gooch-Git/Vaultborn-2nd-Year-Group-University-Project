//using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AssetManager.Infrastructure.Services;

public class TokenService
{
    private readonly HttpClient _httpClient;
    private const string _tokenUrl = "https://developer.api.autodesk.com/authentication/v2/token";
    private readonly string _redirectUri = "http://localhost:8080/callback";
    private readonly string _clientId;
    private readonly string _clientSecret;


    public TokenService()
    {
        _httpClient = new HttpClient();


        // Debug info
        Console.WriteLine($"?? Using APS Authentication V2 endpoint: {_tokenUrl}");
    }
    public TokenService(string clientId, string clientSecret)
    {
        _httpClient = new HttpClient();
        _clientId = clientId;
        _clientSecret = clientSecret;

        Console.WriteLine($"🔐 TokenService using injected client ID and secret");
    }
    /*    // Updated to v2 endpoint
        private readonly string _tokenUrl = "https://developer.api.autodesk.com/authentication/v2/token";*/


    // Get a token with data:read and data:write scopes for Model Derivative API
    public async Task<string> GetViewerAccessTokenAsync()
    {
        var tokenUrl = "https://developer.api.autodesk.com/authentication/v2/token";
        var formData = new Dictionary<string, string>
            {
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "grant_type", "client_credentials" },
                { "scope", "account:read account:write data:read data:write bucket:create bucket:read viewables:read" }
            };

        using var content = new FormUrlEncodedContent(formData);
        HttpResponseMessage response = await _httpClient.PostAsync(tokenUrl, content);
        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();
        TokenResponse tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(jsonResponse);
        return tokenResponse.access_token;
    }

    public static async Task<string> CreateProject(string hubId, string projectName)
    {
        try
        {
            string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubId}/projects";

            var payload = new
            {
                data = new
                {
                    type = "projects",
                    attributes = new
                    {
                        name = projectName,
                        description = "Automatically generated project for asset management.",
                        extension = new
                        {
                            type = "projects:autodesk.core:Project",
                            version = "1.0"
                        }
                    }
                }
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {TokenManager.GetToken()}");
            HttpClient _httpClient = new HttpClient();
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                string projectId = jsonResponse.data.id;
                Console.WriteLine($"✅ Created Project: {projectName} (ID: {projectId})");
                return projectId;
            }
            else
            {
                Console.WriteLine($"❌ Error creating project '{projectName}': {responseContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception in CreateProject: {ex.Message}");
            return null;
        }
    }
    // This is the primary method for getting a token with specific scopes
    public async Task<string> Get2LeggedTokenAsync(string[] scopes)
    {
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
        {
            Console.WriteLine("? Error: Missing client credentials");
            return null;
        }

        // Debug - safely display part of client ID
        if (!string.IsNullOrEmpty(ClientId) && ClientId.Length > 5)
            Console.WriteLine($"?? Client ID: {ClientId.Substring(0, 5)}...");

        // Format the scopes for the request
        string scopeString = string.Join(" ", scopes);
        Console.WriteLine($"?? Requesting scopes: {scopeString}");

        try
        {
            // Create the request content
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", ClientId },
                    { "client_secret", ClientSecret },
                    { "grant_type", "client_credentials" },
                    { "scope", scopeString }
                });

            // Make the request
            HttpResponseMessage response = await _httpClient.PostAsync(_tokenUrl, content);

            // Read response
            string responseJson = await response.Content.ReadAsStringAsync();

            // Log full response for debugging
            Console.WriteLine($"?? Auth Response Status: {response.StatusCode}");
            Console.WriteLine($"?? Auth Response: {responseJson}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"? Token request failed: {response.StatusCode}");
                Console.WriteLine($"Error details: {responseJson}");
                return null;
            }

            Console.WriteLine("? Successfully obtained new token");

            // Parse the response
            using JsonDocument document = JsonDocument.Parse(responseJson);
            if (document.RootElement.TryGetProperty("access_token", out JsonElement tokenElement))
            {
                string token = tokenElement.GetString();

                // Store token in TokenManager for future use
                TokenManager.SetTwoLeggedToken(token);

                // Return just first few chars for log display, safely
                if (!string.IsNullOrEmpty(token) && token.Length > 10)
                    Console.WriteLine($"?? Token received (first 10 chars): {token}");

                return token;
            }
            else
            {
                Console.WriteLine("? No access_token found in response");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Exception getting token: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            return null;
        }
    }

    // Method for 3-legged OAuth - updated for APS v2
    public async Task<string> GetThreeLeggedAccessTokenAsync(string authCode, string codeVerifier)
    {
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(authCode))
        {
            Console.WriteLine("? Error: Missing required parameters for 3-legged auth");
            return null;
        }

        try
        {
            // Redirect URI must match the one in your app registration
            string redirectUri = "http://localhost:8080/callback"; // Update this if needed

            // Create the request content
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", ClientId },
                    { "client_secret", ClientSecret },
                    { "grant_type", "authorization_code" },
                    { "code", authCode },
                    { "code_verifier", codeVerifier },
                    { "redirect_uri", redirectUri }
                });

            // Make the request
            HttpResponseMessage response = await _httpClient.PostAsync(_tokenUrl, content);
            string responseJson = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"?? 3-Legged Auth Response Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"? 3-Legged token request failed: {response.StatusCode}");
                Console.WriteLine($"Error details: {responseJson}");
                return null;
            }

            // Parse the response
            using JsonDocument document = JsonDocument.Parse(responseJson);
            if (document.RootElement.TryGetProperty("access_token", out JsonElement tokenElement))
            {
                string token = tokenElement.GetString();

                // If present, also save the refresh token
                if (document.RootElement.TryGetProperty("refresh_token", out JsonElement refreshTokenElement))
                {
                    string refreshToken = refreshTokenElement.GetString();
                    TokenManager.SetRefreshToken(refreshToken);
                    Console.WriteLine("? Refresh token stored");
                }

                return token;
            }
            else
            {
                Console.WriteLine("? No access_token found in 3-legged response");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Exception in 3-legged auth: {ex.Message}");
            return null;
        }
    }



    /*/// **Fetches a Three-Legged Access Token (User Login)**
    public async Task<string> GetThreeLeggedAccessTokenAsync(string authCode, string codeVerifier)
        {
            var tokenContent = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "code_verifier", codeVerifier },
                { "code", authCode },
                { "redirect_uri", _redirectUri }
            };

            using var tokenParameters = new FormUrlEncodedContent(tokenContent);
            HttpResponseMessage tokenResponse = await _httpClient.PostAsync(_tokenUrl, tokenParameters);
            string tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

            var tokenData = JsonConvert.DeserializeObject<TokenData>(tokenResponseContent);

            if (!string.IsNullOrEmpty(tokenData?.access_token))
            {
                TokenManager.SetToken(tokenData.access_token);
                TokenManager.SetRefreshToken(tokenData.refresh_token);
                return tokenData.access_token;
            }

            throw new Exception("Failed to retrieve access token.");
        }
    }
    */
    /// **Class for Token Data** <summary>
    /// **Class for Token Data**
    /// </summary>
}
public class TokenData
{
    public string access_token { get; set; }
    public int expires_in { get; set; }
    public string token_type { get; set; }
    public string refresh_token { get; set; }

}
public class TokenResponse
{
    public string access_token { get; set; }
    public string token_type { get; set; }
    public int expires_in { get; set; }
}