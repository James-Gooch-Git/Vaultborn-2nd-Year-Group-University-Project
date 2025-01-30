using RestSharp;

namespace AssetManager.Infrastructure.Services
{
    public class AuthService
    {
        private const string ClientId = "f4to4pMqA5AH8hcHVQqcTIg2DWpEHX9wXvAUM4vIfue8Yi8g";
        private const string ClientSecret = "KEvIdyiyRsMJTAxKmXeewERBjEHmcBvemLtRARydT1bsrT5G57bhnGiC4gTj7J1Y";
        private const string AuthUrl = "https://developer.api.autodesk.com/authentication/v2/token";

        public static async Task<string> GetAccessToken()
        {
            var client = new RestClient(AuthUrl);
            var request = new RestRequest(AuthUrl, Method.Post);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", ClientId);
            request.AddParameter("client_secret", ClientSecret);
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("scope", "data:read data:write data:create bucket:create bucket:read");

            var response = await client.ExecuteAsync<AuthResponse>(request);
            return response.Data?.access_token;
        }

        private class AuthResponse
        {
            public string access_token { get; set; }
        }
    }
}