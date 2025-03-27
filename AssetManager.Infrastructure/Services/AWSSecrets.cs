using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

public static class AwsSecretsHelper
{
    public static async Task<Dictionary<string, string>> GetSecretsAsync(string secretName, string region = "us-east-1")
    {
        var config = new AmazonSecretsManagerConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
        using var client = new AmazonSecretsManagerClient(config);

        var request = new GetSecretValueRequest
        {
            SecretId = secretName
        };

        var response = await client.GetSecretValueAsync(request);
        var secretString = response.SecretString;

        return JsonSerializer.Deserialize<Dictionary<string, string>>(secretString);
    }
}
