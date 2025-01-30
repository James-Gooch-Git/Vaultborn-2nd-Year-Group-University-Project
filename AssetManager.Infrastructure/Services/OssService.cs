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
            var client = new RestClient($"{OssBaseUrl}/buckets");
            var request = new RestRequest(OssBaseUrl, Method.Put);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Content-Type", "application/json");

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
                return null;
            }
        }

        // Upload a file to the OSS bucket
        public static async Task<string> UploadFile(string bucketKey, string filePath)
        {
            string token = await AuthService.GetAccessToken();
            string fileName = System.IO.Path.GetFileName(filePath);
            
            Console.WriteLine($"Uploading file {filePath} to {bucketKey}");
            Console.WriteLine($"Using token {token}");

            var client = new RestClient($"{OssBaseUrl}/buckets/{bucketKey}/objects/{fileName}");
            var request = new RestRequest(OssBaseUrl, Method.Post);

            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddHeader("Content-Type", "application/octet-stream");
            request.AddFile("file", filePath);

            var response = await client.ExecuteAsync<FileUploadResponse>(request);

            if (response.IsSuccessful)
            {
                return response.Data.objectId; // Return the objectId of the uploaded file
            }

            return null;
        }

        private class FileUploadResponse
        {
            public string objectId { get; set; }
        }
    }
}