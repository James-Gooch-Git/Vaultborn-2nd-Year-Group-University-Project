using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AssetManagement.Infrastructure.Services; 
using RestSharp;
using AssetManager.Infrastructure.Services;
using System.Net.Http.Headers;
namespace AssetManagement.Infrastructure.Services
{
    public class DeleteModel
    {
        private readonly string _accessToken;
        private const string ApiBaseUrl = "https://developer.api.autodesk.com";

        public DeleteModel(string accessToken)
        {
            TokenService tokenService = new TokenService();
            _accessToken = TokenManager.GetToken();
        }

        public async Task<bool> DeleteLatestModelVersionAsync(string projectId, string itemId)
        {
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(itemId))
            {
                Console.WriteLine("❌ Error: Project ID or Item ID is missing.");
                return false;
            }

            // ✅ Step 1: Get the correct version ID
            DataManagement dataService = new DataManagement();
            List<(string versionId, string versionName, string storageId)> versions = await dataService.GetVersionsForItemAsync(projectId, itemId);

            if (versions == null || versions.Count == 0)
            {
                Console.WriteLine("❌ No versions found for this item.");
                return false;
            }

            var latestVersion = versions[0]; // First entry is the latest
            string latestVersionId = latestVersion.versionId;

            // ✅ Step 2: Strip query parameters (Fixes "version=1" issue)
            latestVersionId = latestVersionId.Split('?')[0];

            Console.WriteLine($"🗑️ Attempting to delete latest version: {latestVersionId}");

            string url = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/versions/{latestVersionId}";
            string accessToken = TokenManager.GetToken();

            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                HttpResponseMessage response = await httpClient.DeleteAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Successfully deleted version: {latestVersionId}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Failed to delete model. Status Code: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception while deleting model: {ex.Message}");
                return false;
            }
        }

    }
}