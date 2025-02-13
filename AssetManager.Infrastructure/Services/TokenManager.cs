namespace AssetManager.Infrastructure.Services;

public static class TokenManager
{
    private static string _accessToken;
    private static string _clientId;

    public static void SetToken(string token)
    {
        Console.WriteLine($"🔹 Debug: Storing access token: {token}");
        _accessToken = token;
    }

    public static string GetToken()
    {
        Console.WriteLine($"🔹 Debug: Retrieving access token: {_accessToken}");
        return _accessToken;
    }

    public static void SetClientId(string clientId)
    {
        Console.WriteLine($"🔹 Debug: Storing client ID: {clientId}");
        _clientId = clientId;
    }

    public static string GetClientId()
    {
        Console.WriteLine($"🔹 Debug: Retrieving client ID: {_clientId}");
        return _clientId;
    }
}
