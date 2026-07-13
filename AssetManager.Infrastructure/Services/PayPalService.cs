using System.Text;
using AssetManager.Infrastructure.Configuration;
using AssetManager.Infrastructure.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AssetManager.Infrastructure.Services;

public class PayPalService
{
    // Sandbox environment; switch to https://api-m.paypal.com for production.
    private const string BaseUrl = "https://api-m.sandbox.paypal.com";

    private static string ClientId => AppSecrets.PayPalClientId;
    private static string ClientSecret => AppSecrets.PayPalClientSecret;

    public async Task<string> GetPayPalAccessToken()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" }
                })
            };
            var authArray = Encoding.ASCII.GetBytes($"{ClientId}:{ClientSecret}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(authArray));

            var response = await SharedHttp.Client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonConvert.DeserializeObject<PayPalAccessTokenResponse>(responseString);
            return jsonResponse.access_token;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error getting PayPal access token: {e.Message}");
            return null;
        }
    }

    public async Task<string> CreateOrder(string accessToken, double amount, string currency = "GBP")
    {
        var data = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount = new
                    {
                        currency_code = currency,
                        value = amount.ToString("0.00")
                    },
                    description = "Model Purchase",
                }
            },
            application_context = new
            {
                return_url = "https://localhost:8080/return",
                cancel_url = "https://localhost:8080/cancel"
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/checkout/orders")
        {
            Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await SharedHttp.Client.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"PayPal CreateOrder failed: {response.StatusCode}");
            return null;
        }

        // Link order is not guaranteed by the API — select by rel, never by index.
        var jsonResponse = JObject.Parse(responseString);
        string approvalUrl = jsonResponse["links"]?
            .FirstOrDefault(l => (string)l["rel"] == "approve")?["href"]?.ToString();
        if (approvalUrl == null)
            Console.WriteLine("PayPal CreateOrder response contained no approve link");
        return approvalUrl;
    }

    public async Task<bool> CapturePayment(string token, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/checkout/orders/{token}/capture")
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await SharedHttp.Client.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"PayPal capture failed: {response.StatusCode}");
            return false;
        }

        var jsonResponse = JObject.Parse(responseString);
        return (string)jsonResponse["status"] == "COMPLETED";
    }

    private class PayPalAccessTokenResponse
    {
        public string access_token { get; set; }
    }
}
