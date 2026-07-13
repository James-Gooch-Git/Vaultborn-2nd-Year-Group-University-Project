namespace AssetManager.Infrastructure.Configuration;

/// <summary>
/// Central access point for all application credentials. Values are read from
/// environment variables (process scope first, then the current user's scope)
/// so no secret ever lives in source control.
/// </summary>
public static class AppSecrets
{
    public const string MongoConnectionStringVar = "VAULTBORN_MONGODB_URI";
    public const string ApsClientIdVar = "VAULTBORN_APS_CLIENT_ID";
    public const string ApsClientSecretVar = "VAULTBORN_APS_CLIENT_SECRET";
    public const string PayPalClientIdVar = "VAULTBORN_PAYPAL_CLIENT_ID";
    public const string PayPalClientSecretVar = "VAULTBORN_PAYPAL_CLIENT_SECRET";

    public static string MongoConnectionString => Require(MongoConnectionStringVar);
    public static string ApsClientId => Require(ApsClientIdVar);
    public static string ApsClientSecret => Require(ApsClientSecretVar);
    public static string PayPalClientId => Require(PayPalClientIdVar);
    public static string PayPalClientSecret => Require(PayPalClientSecretVar);

    private static string Require(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name)
                        ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Required environment variable '{name}' is not set. " +
                "See README section 'Configuring credentials' for setup instructions.");
        return value;
    }
}
