namespace AssetManager.Infrastructure.Services;

public static class TokenManager
{
    private static string access_token;
    private static string _refreshToken;
    private static string _2accessToken;
    private static string _clientId;

    public static void SetToken(string token) => access_token = token;
    public static string GetToken() => access_token;

    public static void SetRefreshToken(string token) => _refreshToken = token;
    public static string GetRefreshToken() => _refreshToken;

    public static void SetTwoLeggedToken(string token) => _2accessToken = token;
    public static string GetTwoLeggedToken() => _2accessToken;

    public static void SetClientId(string clientId) => _clientId = clientId;
    public static string GetClientId() => _clientId;
}
