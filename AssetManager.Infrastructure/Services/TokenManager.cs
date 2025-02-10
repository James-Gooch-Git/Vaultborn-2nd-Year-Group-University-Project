namespace AssetManager.Infrastructure.Services;

public class TokenManager
{
    private static string _accessToken;

    public static void SetToken(string token) => _accessToken = token;
    public static string GetToken() => _accessToken;
}
