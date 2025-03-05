using System.Net.Http.Headers;
using RestSharp;
using Newtonsoft.Json;
using System.Text.Json;


namespace AssetManager.Infrastructure.Services
{
    public class AuthService
    {
        private const string BaseUrl = "https://developer.api.autodesk.com";
        private readonly string _accessToken;

        public AuthService(string accessToken)
        {
            _accessToken = accessToken;
        }

        private const string OssBaseUrl = "https://developer.api.autodesk.com/oss/v2";

        // Create a new bucket


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