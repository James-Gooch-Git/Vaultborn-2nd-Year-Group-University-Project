using System.Net.Http.Headers;
using RestSharp;
using Newtonsoft.Json;
using System.Text.Json;


namespace AssetManager.Infrastructure.Services
{
    public class OssService
    {
        private const string BaseUrl = "https://developer.api.autodesk.com";
        private readonly string _accessToken;

        public OssService(string accessToken)
        {
            _accessToken = accessToken;
        }

        private const string OssBaseUrl = "https://developer.api.autodesk.com/oss/v2";

        // Create a new bucket
        public static async Task<string> CreateBucket(string bucketName)
        {
            // Get the access token from the AuthService
            string token = await AuthService.GetAccessToken();

            // Initialize the RestClient
            var client = new RestClient(OssBaseUrl);
            var request = new RestRequest("/buckets", Method.Post);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Content-Type", "application/json");
            bucketName = bucketName.ToLower();

            // Bucket request body
            var bucketRequestBody = new
            {
                bucketKey = bucketName, // You can use the bucket name or any unique identifier as the bucket key
                policyKey = "transient" // 'transient', 'persistent', or 'temporary'
            };

            // Add the JSON body
            request.AddJsonBody(bucketRequestBody);

            // Send the request
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                // Deserialize the response to get the bucket key
                var bucketData = JsonConvert.DeserializeObject<dynamic>(response.Content);
                string bucketKey = bucketData.bucketKey;
                return bucketKey;
            }
            else
            {
                // Handle error
                Console.WriteLine($"Error: {response.StatusCode} - {response.Content}");
                return null;
            }
        }

        // Upload a file to the OSS bucket
        public async Task<string> GetSignedUploadUrlAsync(string bucketKey, string objectName)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                string requestUrl = $"{OssBaseUrl}/buckets/{bucketKey}/objects/{objectName}/signeds3upload";

                var response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = System.Text.Json.JsonSerializer.Deserialize<SignedUploadResponse>(content);

                    Console.WriteLine($"Upload URL: {json.Urls[0]}");
                    return json.Urls[0]; // First signed URL for upload
                }
                else
                {
                    Console.WriteLine(
                        $"Error getting signed upload URL: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return null;
                }
            }
        }

        private class SignedUploadResponse
        {
            public string UploadKey { get; set; }
            public string UploadExpiration { get; set; }
            public string UrlExpiration { get; set; }
            public string[] Urls { get; set; }
        }

        public async Task<bool> UploadFileToSignedUrlAsync(string signedUrl, string filePath)
        {
            using (var client = new HttpClient())
            {
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var content = new ByteArrayContent(fileBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var response = await client.PutAsync(signedUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("File uploaded successfully!");
                    return true;
                }
                else
                {
                    Console.WriteLine(
                        $"Upload failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return false;
                }
            }
        }
    }
}