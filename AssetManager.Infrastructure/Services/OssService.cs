using RestSharp;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace AssetManager.Infrastructure.Services
{
    public static class OssService
    {
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
        public static async Task<string> UploadFile(string bucketKey, string filePath)
        {
            string token = await AuthService.GetAccessToken();
            string fileName = System.IO.Path.GetFileName(filePath);

            Console.WriteLine($"Requesting upload URL for file {filePath} in bucket {bucketKey}");

            var client = new RestClient(OssBaseUrl);
    
            // **Step 1: Request an upload URL**
            var request = new RestRequest($"/buckets/{bucketKey}/objects/{fileName}/signeds3upload", Method.Post);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Content-Type", "application/json");
            
            var requestBody = new
            {
                uploadKey = fileName, 
                minutesExpiration = 10 // Set expiration time for pre-signed URL (max: 60)
            };
            request.AddJsonBody(requestBody);
            
            var preSignedResponse = await client.ExecuteAsync<PreSignedUrlResponse>(request);

            if (!preSignedResponse.IsSuccessful || preSignedResponse.Data == null)
            {
                Console.WriteLine($"Error getting upload URL: {preSignedResponse.StatusCode} - {preSignedResponse.Content}");
                return null;
            }

            string uploadUrl = preSignedResponse.Data.uploadUrl;
            Console.WriteLine($"Received upload URL: {uploadUrl}");

            // **Step 2: Upload File to S3 using Pre-signed URL**
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var uploadClient = new RestClient();
            var uploadRequest = new RestRequest(uploadUrl, Method.Put);
    
            uploadRequest.AddHeader("Content-Type", "application/octet-stream");
            uploadRequest.AddParameter("application/octet-stream", fileBytes, ParameterType.RequestBody);

            var uploadResponse = await uploadClient.ExecuteAsync(uploadRequest);

            if (uploadResponse.IsSuccessful)
            {
                Console.WriteLine("File uploaded successfully!");
                return preSignedResponse.Data.objectId;
            }

            Console.WriteLine($"Upload failed: {uploadResponse.StatusCode} - {uploadResponse.Content}");
            return null;
        }

        public class PreSignedUrlResponse
        {
            public string uploadUrl { get; set; }
            public string objectId { get; set; }
        }


        private class FileUploadResponse
        {
            public string objectId { get; set; }
        }
    }
}