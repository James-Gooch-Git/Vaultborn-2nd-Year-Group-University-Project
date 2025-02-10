using System.Text.Json;

namespace AssetManager.Infrastructure.Services;

public class TokenService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public TokenService()
    {
        _httpClient = new HttpClient();
        _clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
        _clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var tokenUrl = "https://developer.api.autodesk.com/authentication/v2/token";
        var formData = new Dictionary<string, string>
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "grant_type", "client_credentials" },
            { "scope", "data:read data:write bucket:create bucket:read" }
        };

        using var content = new FormUrlEncodedContent(formData);
        HttpResponseMessage response = await _httpClient.PostAsync(tokenUrl, content);
        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();
        TokenResponse tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);
        Console.WriteLine("AccessToken: " + tokenResponse.access_token);
        return tokenResponse.access_token;
    }
    public class TokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }
}