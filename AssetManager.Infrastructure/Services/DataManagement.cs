using System;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AssetManager.Infrastructure.Services;

namespace AssetManager.Infrastructure.Services
{
    public class DataManagement
    {
        private static readonly HttpClient client = new HttpClient();

public static async Task<string> GetPersonalHub()
{
    string url = "https://developer.api.autodesk.com/project/v1/hubs";
    string _accessToken = TokenManager.GetToken(); 

    if (string.IsNullOrEmpty(_accessToken))
    {
        Console.WriteLine("Error: Access token is missing or invalid.");
        return null;
    }

    try
    {
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        HttpResponseMessage response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();

            // Parse JSON response
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            JsonElement root = doc.RootElement;

            // Look for the personal hub
            foreach (JsonElement hub in root.GetProperty("data").EnumerateArray())
            {
                string type = hub.GetProperty("attributes").GetProperty("extension").GetProperty("type").GetString();

                // Debugging: Print the type of each hub
                Console.WriteLine($"Found hub type: {type}");

                if (type == "hubs:autodesk.a360:PersonalHub") // Check for personal hub type
                {
                    string hubId = hub.GetProperty("id").GetString();
                    string hubName = hub.GetProperty("attributes").GetProperty("name").GetString();

                    Console.WriteLine($"Personal Hub Found: ID = {hubId}, Name = {hubName}");
                    return hubId;  // Return the Personal Hub ID as a string
                }
            }
            Console.WriteLine("No Personal Hub found.");
            return null;  // No personal hub found, return null
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
            return null;  // If the response is not successful, return null
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception occurred: {ex.Message}");
        return null;  // Return null if an exception occurs
    }
}


        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> GetProjectIdAsync(string hubId)
        {
            string accessToken = TokenManager.GetToken();
    
            try
            {
                string url = $"https://developer.api.autodesk.com/project/v1/hubs/{hubId}/projects";

                // Set up request headers
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Send request
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();  // Ensure the request succeeded
                string responseJson = await response.Content.ReadAsStringAsync();

                // Parse JSON response
                using JsonDocument doc = JsonDocument.Parse(responseJson);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("data", out JsonElement projects) && projects.GetArrayLength() > 0)
                {
                    string projectId = projects[0].GetProperty("id").GetString();
                    Console.WriteLine($"✅ Project ID Retrieved: {projectId}");
                    return projectId;
                }
                else
                {
                    Console.WriteLine("❌ No projects found in this hub.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving Project ID: {ex.Message}");
                return null;
            }
        }
        
        
        
        
        
        
        
        
        //ADD PROJECT NAME PARAMATER
        public static void CreateNewProject()
        {
            string url = "https://developer.api.autodesk.com/project/v1/hubs/{hub_id}/projects";
            string _accessToken = TokenManager.GetToken();

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            //using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            //JsonElement root = doc.RootElement;

        }
    }
}

