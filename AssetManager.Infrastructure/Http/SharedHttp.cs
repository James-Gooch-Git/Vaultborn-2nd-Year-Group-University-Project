namespace AssetManager.Infrastructure.Http;

/// <summary>
/// Single process-wide HttpClient. Creating an HttpClient per request leaks
/// sockets; sharing one instance is the documented usage pattern.
/// Auth headers must be set per-request on HttpRequestMessage, never on
/// Client.DefaultRequestHeaders (which would race between concurrent calls).
/// </summary>
public static class SharedHttp
{
    public static System.Net.Http.HttpClient Client { get; } = new();
}
