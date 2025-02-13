namespace AssetManager.Infrastructure.Services;

public class TokenManager
{
    private static string _accessToken;
    private static string _refreshToken;

    public static void SetToken(string token) => _accessToken = token;
    public static string GetToken() => _accessToken;
    
    public static void SetRefreshToken(string token) => _refreshToken = token;
    public static string GetRefreshToken() => _refreshToken;
}
