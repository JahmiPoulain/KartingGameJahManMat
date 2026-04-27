using UnityEngine;

public static class DataBaseConstants
{
    // ==================== TABLES ====================
    public const string TablePlayers = "players";  

    // ==================== VUES ====================
    public const string ViewPlayerStats = "vw_PlayerStats";

    // ==================== COLONNES - PLAYERS ====================
    public const string ColPlayerId = "id";
    public const string ColPlayerName = "name";
    public const string ColPlayerBestTimeAttack = "bestTimeAttack";
    public const string ColPlayerBestTimeTrial = "bestTimeTrial";
    public const string ColPlayerBestReverseTimeAttack = "bestReverseTimeAttack";
    public const string ColPlayerBestReverseTimeTrial = "bestReverseTimeTrial";
}
