using LootLocker;
using UnityEngine;

public static class LootLockerConfigSanitizer
{
    public static void SanitizeApiKey()
    {
        LootLockerConfig config = LootLockerConfig.current;

        if (config == null)
            return;

        SanitizeApiKey(config);
        ForceDashboardApiDomain(config);
    }

    private static void SanitizeApiKey(LootLockerConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.apiKey))
            return;

        string sanitizedApiKey = config.apiKey.Trim().Trim('"', '\'');

        if (sanitizedApiKey == config.apiKey)
            return;

        Debug.LogWarning("LootLocker API key contenait des guillemets. Clé nettoyée avant la connexion.");
        config.apiKey = sanitizedApiKey;
    }

    private static void ForceDashboardApiDomain(LootLockerConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.domainKey))
            return;

        string baseUrl = $"https://{config.domainKey}.api.lootlocker.io";
        string webSocketBaseUrl = $"wss://{config.domainKey}.api.lootlocker.io/game";

        config.url = "https://api.lootlocker.io/v1";
        config.adminUrl = baseUrl + "/admin";
        config.playerUrl = baseUrl + "/player";
        config.userUrl = baseUrl + "/game";
        config.webSocketBaseUrl = webSocketBaseUrl;
        config.baseUrl = baseUrl;
    }
}
