using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AssetManager.Infrastructure.Security;

/// <summary>
/// Persists session tokens (access / refresh / two-legged) encrypted with DPAPI
/// under the current user's local app data, replacing the previous practice of
/// writing raw tokens into user environment variables (the registry).
/// </summary>
public static class SessionTokenStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Vaultborn", "session.bin");

    private static readonly object Sync = new();

    public static string? Get(string key)
    {
        lock (Sync)
        {
            var store = Load();
            return store.TryGetValue(key, out string? value) ? value : null;
        }
    }

    public static void Set(string key, string? value)
    {
        lock (Sync)
        {
            var store = Load();
            if (string.IsNullOrEmpty(value))
                store.Remove(key);
            else
                store[key] = value;
            Save(store);
        }
    }

    public static void Clear()
    {
        lock (Sync)
        {
            try
            {
                if (File.Exists(StorePath))
                    File.Delete(StorePath);
            }
            catch (IOException)
            {
                Save(new Dictionary<string, string>());
            }
        }
    }

    private static Dictionary<string, string> Load()
    {
        try
        {
            if (!File.Exists(StorePath))
                return new Dictionary<string, string>();

            byte[] encrypted = File.ReadAllBytes(StorePath);
            byte[] plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(plain))
                   ?? new Dictionary<string, string>();
        }
        catch (Exception)
        {
            // Corrupt or undecryptable store: treat as empty so the user just logs in again.
            return new Dictionary<string, string>();
        }
    }

    private static void Save(Dictionary<string, string> store)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
        byte[] plain = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(store));
        byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(StorePath, encrypted);
    }
}
