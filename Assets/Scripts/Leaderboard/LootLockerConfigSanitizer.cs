using LootLocker;
using UnityEngine;

public static class LootLockerConfigSanitizer
{
    public static void SanitizeApiKey()
    {
        LootLockerConfig config = LootLockerConfig.current;

        if (config == null || string.IsNullOrWhiteSpace(config.apiKey))
            return;

        string sanitizedApiKey = config.apiKey.Trim().Trim('"', '\'');

        if (sanitizedApiKey == config.apiKey)
            return;

        Debug.LogWarning("LootLocker API key contenait des guillemets. Clé nettoyée avant la connexion.");
        config.apiKey = sanitizedApiKey;
    }
}
