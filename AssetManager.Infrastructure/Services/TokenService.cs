using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AssetManager.Infrastructure.Configuration;
using AssetManager.Infrastructure.Http;
using AssetManager.Infrastructure.Security;
using Newtonsoft.Json;

namespace AssetManager.Infrastructure.Services;

public class TokenService
{
    private const string _tokenUrl = "https://developer.api.autodesk.com/authentication/v2/token";
    private const string _redirectUri = "http://localhost:8080/callback";

    private static string ClientId => AppSecrets.ApsClientId;
    private static string ClientSecret => AppSecrets.ApsClientSecret;

    private readonly HttpClient _httpClient = SharedHttp.Client;

    // Token scoped for the Forge viewer only — read access to translated viewables.
    // Never request write scopes here: this token is injected into viewer HTML/JS.
    public async Task<string> GetViewerAccessTokenAsync()
    {
        var formData = new Dictionary<string, string>
            {
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "grant_type", "client_credentials" },
                { "scope", "viewables:read" }
            };

        using var content = new FormUrlEncodedContent(formData);
        HttpResponseMessage response = await _httpClient.PostAsync(_tokenUrl, content);
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
            HttpResponseMessage response = await SharedHttp.Client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                string projectId = jsonResponse.data.id;
                Console.WriteLine($"Created project: {projectName} (ID: {projectId})");
                return projectId;
            }

            Console.WriteLine($"Error creating project '{projectName}': {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in CreateProject: {ex.Message}");
            return null;
        }
    }

    // This is the primary method for getting a token with specific scopes
    public async Task<string> Get2LeggedTokenAsync(string[] scopes)
    {
        string scopeString = string.Join(" ", scopes);

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", ClientId },
                    { "client_secret", ClientSecret },
                    { "grant_type", "client_credentials" },
                    { "scope", scopeString }
                });

            HttpResponseMessage response = await _httpClient.PostAsync(_tokenUrl, content);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Token request failed: {response.StatusCode}");
                return null;
            }

            using JsonDocument document = JsonDocument.Parse(responseJson);
            if (document.RootElement.TryGetProperty("access_token", out JsonElement tokenElement))
            {
                string token = tokenElement.GetString();
                TokenManager.SetTwoLeggedToken(token);
                return token;
            }

            Console.WriteLine("No access_token found in response");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception getting token: {ex.Message}");
            return null;
        }
    }

    // Method for 3-legged OAuth - updated for APS v2
    public async Task<string> GetThreeLeggedAccessTokenAsync(string authCode, string codeVerifier)
    {
        if (string.IsNullOrEmpty(authCode))
        {
            Console.WriteLine("Error: Missing required parameters for 3-legged auth");
            return null;
        }

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", ClientId },
                    { "client_secret", ClientSecret },
                    { "grant_type", "authorization_code" },
                    { "code", authCode },
                    { "code_verifier", codeVerifier },
                    { "redirect_uri", _redirectUri }
                });

            HttpResponseMessage response = await _httpClient.PostAsync(_tokenUrl, content);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"3-legged token request failed: {response.StatusCode}");
                return null;
            }

            using JsonDocument document = JsonDocument.Parse(responseJson);
            if (document.RootElement.TryGetProperty("access_token", out JsonElement tokenElement))
            {
                string token = tokenElement.GetString();

                // If present, also save the refresh token (encrypted at rest)
                if (document.RootElement.TryGetProperty("refresh_token", out JsonElement refreshTokenElement))
                {
                    string refreshToken = refreshTokenElement.GetString();
                    TokenManager.SetRefreshToken(refreshToken);
                    SessionTokenStore.Set("refresh_token", refreshToken);
                }

                return token;
            }

            Console.WriteLine("No access_token found in 3-legged response");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in 3-legged auth: {ex.Message}");
            return null;
        }
    }
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
