using ForgeViewerApp;
using Newtonsoft.Json;
namespace AssetManager.Infrastructure.Services;
using AutodeskApiService;

public class TokenService
{
    private readonly HttpClient _httpClient;
    
    private string _tokenUrl  = "https://developer.api.autodesk.com/authentication/v2/token";
    private string _clientId = ClientId;
    private string _redirectUri  = "https://localhost/auth/callback";

    public TokenService()
    {
        _httpClient = new HttpClient();
    }
    
    public async Task<string> GetAccessTokenAsync(string authCode, string codeVerifier)
    {
        using (HttpClient client = new HttpClient())
        {
            var tokenContent = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", _clientId },
                { "code_verifier", codeVerifier },
                { "code", authCode },
                { "redirect_uri", _redirectUri }
            };

            var tokenParameters = new FormUrlEncodedContent(tokenContent);
            var tokenResponse = await client.PostAsync(_tokenUrl, tokenParameters);
            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

            var tokenData = JsonConvert.DeserializeObject<TokenData>(tokenResponseContent);
                
            if (!string.IsNullOrEmpty(tokenData?.access_token))
            {
                TokenManager.SetToken(tokenData.access_token);  // Store token
                return tokenData.access_token;
            }
                
            throw new Exception("Failed to retrieve access token.");
        }
    }
}

public class TokenData
{
    public string access_token { get; set; }
    public int expires_in { get; set; }
    public string token_type { get; set; }
}
