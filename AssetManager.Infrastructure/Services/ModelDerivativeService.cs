using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using AssetManager.Infrastructure.Services;
using AssetManager.Infrastructure.Http;

namespace AssetManager.Infrastructure.Services
{
    public class ModelDerivativeService
    {
        private readonly HttpClient _httpClient;

        private readonly string _modelDerivativeUrl =
            "https://developer.api.autodesk.com/modelderivative/v2/designdata/job";

        private readonly string _manifestUrl =
            "https://developer.api.autodesk.com/modelderivative/v2/designdata/{0}/manifest";

        public ModelDerivativeService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public static async Task<bool> IsModelDerivativeReady(string encodedUrn)
        {
            string accessToken = TokenManager.GetToken();
            string url = $"https://developer.api.autodesk.com/modelderivative/v2/designdata/{encodedUrn}/manifest";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await SharedHttp.Client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error checking manifest: {response.StatusCode} - {response.ReasonPhrase}");
                return false;
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("status", out JsonElement statusElement))
            {
                string status = statusElement.GetString();
                Console.WriteLine($"🔄 Manifest status: {status}");

                if (status == "success")
                    return true;
                if (status == "pending" || status == "processing")
                    return false; // Wait for processing
            }

            return false;
        }


        public async Task<bool> SubmitModelForTranslationAsync(string encodedUrn, string accessToken)
        {
            if (string.IsNullOrEmpty(encodedUrn) || string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("❌ Error: Missing required parameters.");
                return false;
            }

            Console.WriteLine($"🔍 Encoded URN: {encodedUrn}");
            Console.WriteLine($"🔑 Using Access Token: {accessToken.Substring(0, 10)}...");

            string modelDerivativeUrl = "https://developer.api.autodesk.com/modelderivative/v2/designdata/job";

            // ✅ JSON Payload
            var jobPayload = new
            {
                input = new { urn = encodedUrn },
                output = new
                {
                    formats = new[]
                    {
                        new { type = "svf", views = new[] { "2d", "3d" } }
                    }
                }
            };

            string jsonPayload = JsonSerializer.Serialize(jobPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            Console.WriteLine($"📄 JSON Payload: {jsonPayload}");

            using var request = new HttpRequestMessage(HttpMethod.Post, modelDerivativeUrl)
            {
                // ✅ Set JSON Content Properly
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("x-ads-force", "true"); // Force re-translation if already processed

            // ✅ Send the request
            HttpResponseMessage response = await SharedHttp.Client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"🔍 Response Status: {response.StatusCode}");
            Console.WriteLine($"📄 Response Content: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ API Request Failed: {response.StatusCode}");
                Console.WriteLine($"Error Details: {responseContent}");
                return false;
            }

            Console.WriteLine("✅ Model successfully submitted for translation.");
            return true;
        }

        // Method to check if a model has already been translated
        public async Task<bool> IsTranslationCompletedAsync(string encodedUrn, string accessToken)
        {
            // Remove the "urn:" prefix for the API call if necessary
            string urnWithoutPrefix = encodedUrn.Replace("urn:", "");
            string url =
                $"https://developer.api.autodesk.com/modelderivative/v2/designdata/{urnWithoutPrefix}/manifest";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            // Log the status code for debugging purposes
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error checking manifest: {response.StatusCode} - {response.ReasonPhrase}");
                return false; // Return false if manifest not found or there was another issue
            }

            string json = await response.Content.ReadAsStringAsync();

            // Deserialize the manifest response to get the status
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("status", out JsonElement statusElement))
            {
                string status = statusElement.GetString();
                return status.Equals("success", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public async Task<bool> SubmitPdfForTranslationAsync(string encodedUrn, string accessToken)
        {
            if (string.IsNullOrEmpty(encodedUrn) || string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("❌ Error: Missing required parameters for PDF translation.");
                return false;
            }

            Console.WriteLine($"🔍 PDF Encoded URN: {encodedUrn}");
            Console.WriteLine($"🔑 Using Access Token: {accessToken.Substring(0, 10)}...");

            // JSON Payload specifically for PDF translation
            // Ensure this is in your SubmitPdfForTranslationAsync method
            var jobPayload = new
            {
                input = new { urn = encodedUrn },
                output = new
                {
                    formats = new[]
                    {
                        new
                        {
                            type = "svf",
                            views = new[] { "2d" },
                            advanced = new
                            {
                                pdfOptions = new
                                {
                                    generateMasterViews = true
                                }
                            }
                        }
                    }
                }
            };


            string jsonPayload = JsonSerializer.Serialize(jobPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            Console.WriteLine($"📄 PDF Translation JSON Payload: {jsonPayload}");

            using var request = new HttpRequestMessage(HttpMethod.Post, _modelDerivativeUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("x-ads-force", "true"); // Force re-translation if already processed

            HttpResponseMessage response = await SharedHttp.Client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"🔍 PDF Translation Response Status: {response.StatusCode}");
            Console.WriteLine($"📄 Response Content: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ PDF Translation API Request Failed: {response.StatusCode}");
                Console.WriteLine($"Error Details: {responseContent}");
                return false;
            }

            Console.WriteLine("✅ PDF successfully submitted for translation.");
            return true;
        }
        
         public static async Task<bool> IsVersionViewableAsync(string encodedUrn, string accessToken)
            {
                string urnWithoutPrefix = encodedUrn.Replace("urn:", "");
                string url =
                    $"https://developer.api.autodesk.com/modelderivative/v2/designdata/{urnWithoutPrefix}/manifest";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                HttpResponseMessage response = await SharedHttp.Client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error checking model version: {response.StatusCode}");
                    return false;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                if (doc.RootElement.TryGetProperty("status", out JsonElement statusElement))
                {
                    string status = statusElement.GetString();
                    if (status == "success")
                    {
                        return true; // Translation is successful and viewable
                    }
                    else if (status == "pending" || status == "processing")
                    {
                        Console.WriteLine("🔄 Model is still processing, please wait...");
                        return false; // Still processing, not yet viewable
                    }
                    else
                    {
                        Console.WriteLine("❌ Translation failed or there was an error with the model.");
                        return false; // Failed or error state
                    }
                }

                return false; // Default return if no status found

            }
    }
}