using System.Text;
using Newtonsoft.Json;

namespace AssetManager.Infrastructure.Services;

public class PayPalService
{
    private string clientId = "AczMZeknBHiVxYKbTXXJcnBBQGzbyxezvXljdSo762l99bhMOfQIvZYsUOljr3CcZwZ4BjtLZMnUUZ1O";
    private string clientSecret = "EBFRT8ssTMtNlfw2753rw689IE6PF9MY4TQnotc3SUCm9rY-7vTxpkhwHK7GecKfrgPn1GLfk3FEvalC";

    public async Task<string> GetPayPalAcessToken()
    {
        try
        {
            using var httpClient = new HttpClient();
            {
                var url = "https://api-m.sandbox.paypal.com/v1/oauth2/token";
                var data = new Dictionary<string, string>()
                {
                    { "grant_type", "client_credentials" }
                };
                
                var authArray = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
                var encodedData = Convert.ToBase64String(authArray);
                
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedData);
                
                var content = new FormUrlEncodedContent(data);
                
                var response = await httpClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<PayPalAccessTokenResponse>(responseString);
                return jsonResponse.access_token;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error getting API context: {e.Message}");
            return null;
        }
    }

    public async Task<string> CreateOrder(string accessToken, double amount, string currency = "GBP")
    {
        var url = "https://api-m.sandbox.paypal.com/v2/checkout/orders";

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
        
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(url, content);
        var responseString = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseString);
        string approvalUrl = jsonResponse["links"][1]["href"];
        Console.WriteLine($"Approval Url: {approvalUrl}");
        return approvalUrl;
    }

    public async Task<bool> CapturePayment(string token, string accessToken)
    {
        var url = $"https://api-m.sandbox.paypal.com/v2/checkout/orders/{token}/capture";
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var content = new StringContent("", Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(url, content);
        var responseString = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseString);
        Console.WriteLine($"PayPal Capture Response: {jsonResponse}");
        if (jsonResponse.status == "COMPLETED")
        {
            return true;
        }
        return false;
    }

    private class PayPalAccessTokenResponse
    {
        public string access_token { get; set; }
    }
}