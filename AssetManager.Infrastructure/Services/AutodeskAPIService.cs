using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using AssetManager.Infrastructure.Services;
using Autodesk.Forge;

namespace ForgeViewerApp
{
 
    public class AutodeskApiService
    {
       


        //  string ClientId = "eK6vNFNyFAin4PouWXfN00RfePKGZwSqeh6RTcjKAvHAqyOW";
        //  string ClientSecret = "EqBJJlqKczzkLfZSJO2cg3BajCxkZGGTGwHWFhn5jrpGSEledCG1deeBVALq734W";
        private readonly HttpClient _client = new HttpClient();
        private readonly TokenService _tokenService;
        private readonly string ClientId;
        private readonly string ClientSecret;
        public AutodeskApiService(TokenService tokenService, string clientId, string clientSecret)
        {
            _client = new HttpClient();
            _tokenService = tokenService;
            ClientId = clientId;
            ClientSecret = clientSecret;

            Console.WriteLine("ID: " + ClientId);
            Console.WriteLine("Secret: " + ClientSecret);
        }



        public AutodeskApiService(TokenService tokenService)
        {
            _client = new HttpClient();
            _tokenService = tokenService;
            Console.WriteLine("ID: " + ClientId);
            Console.WriteLine("Secret: " + ClientSecret);
        }

        public class SignedUrlResponse
        {
            public string uploadKey { get; set; }    
            public string uploadExpiration { get; set; }
            public string urlExpiration { get; set; }
            public List<string> urls { get; set; }
        }



        public async Task<string> GetItemUrn(string projectId, string itemId)
        {
            try
            {
                // Authenticate with Autodesk Forge (APS)
                TwoLeggedApi oauth2 = new TwoLeggedApi();
                dynamic bearer = await oauth2.AuthenticateAsync(
                    ClientId, ClientSecret,
                    "client_credentials", new Scope[] { Scope.DataRead });

                ItemsApi itemsApi = new ItemsApi();
                itemsApi.Configuration.AccessToken = bearer.access_token;

                // Get item details
                dynamic itemDetails = await itemsApi.GetItemAsync(projectId, itemId);

                // Extract the URN from the tip version
                string urn = itemDetails.data.relationships.tip.data.id;

                Console.WriteLine($"✅ Retrieved Model URN: {urn}");
                return urn;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching URN: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// 2. Requests a signed URL for uploading a file.
        /// </summary>
        public async Task<SignedUrlResponse> GetSignedUrlForUploadAsync(string bucketKey, string fileName, string accessToken)
        {
            string url = $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{Uri.EscapeDataString(fileName)}/signeds3upload";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SignedUrlResponse>(json);
        }

        /// <summary>
        /// 3. Uploads the file using the signed URL.
        /// </summary>
        public async Task UploadFileToSignedUrlAsync(string signedUrl, string filePath)
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            using var content = new ByteArrayContent(fileBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            HttpResponseMessage response = await _client.PutAsync(signedUrl, content);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// 4. Finalizes the upload by verifying it.
        /// </summary>
        public async Task<string> VerifyUploadAsync(string bucketKey, string fileName, string uploadKey, string accessToken)
        {
            string url = $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{Uri.EscapeDataString(fileName)}/signeds3upload";
            var payload = new { uploadKey = uploadKey };
            string jsonPayload = JsonSerializer.Serialize(payload);

            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = content;

            HttpResponseMessage response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        /// <summary>
        /// 5. Submits the translation job to Autodesk.
        /// </summary>
        public async Task<string> SubmitTranslationJobAsync(string objectId, string accessToken)
        {
            string encodedUrn = EncodeObjectIdToUrn(objectId);
            string url = "https://developer.api.autodesk.com/modelderivative/v2/designdata/job";

            var jobRequest = new
            {
                input = new { urn = encodedUrn.Replace("urn:", "") },
                output = new
                {
                    formats = new[] { new { type = "svf", views = new[] { "2d", "3d" } } }
                }
            };

            string jsonPayload = JsonSerializer.Serialize(jobRequest);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = content;

            HttpResponseMessage response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        /// <summary>
        /// 6. Checks if the model translation is complete.
        /// </summary>
        public async Task<bool> IsTranslationCompletedAsync(string encodedUrn, string accessToken)
        {
            string urnWithoutPrefix = encodedUrn.Replace("urn:", "");
            string url = $"https://developer.api.autodesk.com/modelderivative/v2/designdata/{urnWithoutPrefix}/manifest";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            HttpResponseMessage response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return false;

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("status", out JsonElement statusElement))
            {
                string status = statusElement.GetString();
                return status.Equals("success", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// Encodes the object ID into Base64 URN format.
        /// </summary>
        public string EncodeObjectIdToUrn(string objectId)
        {
            return "urn:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(objectId));
        }

        /// <summary>
        /// **Runs the entire process: Upload → Verify → Translate → View**
        /// </summary>
        // public async Task<string> UploadAndTranslateAsync(string bucketKey, string filePath)
        // {
        //     string fileName = Path.GetFileName(filePath);
        //     string accessToken = await _tokenService.GetAccessTokenAsync();
        //
        //     // Step 1: Get Signed URL
        //     SignedUrlResponse signedUrlResponse = await GetSignedUrlForUploadAsync(bucketKey, fileName, accessToken);
        //
        //     // Step 2: Upload File
        //     await UploadFileToSignedUrlAsync(signedUrlResponse.urls[0], filePath);
        //
        //     // Step 3: Verify Upload
        //     string verifyResponse = await VerifyUploadAsync(bucketKey, fileName, signedUrlResponse.uploadKey, accessToken);
        //     Console.WriteLine($"Upload Verified: {verifyResponse}");
        //
        //     // Step 4: Submit Translation Job
        //     string objectId = $"urn:adsk.objects:os.object:{bucketKey}/{fileName}";
        //     await SubmitTranslationJobAsync(objectId, accessToken);
        //
        //     // Step 5: Return URN for Viewer
        //     return EncodeObjectIdToUrn(objectId);
        // }
    }
}
