/// <summary>
/// Contient toutes les constantes pour les noms de tables et de colonnes.
/// Cela permet d'éviter les erreurs de frappe et facilite la maintenance.
/// </summary>
public static class DatabaseConstants
{
    // ==================== TABLES ====================
    public const string TablePlayers = "players";
    public const string TableQuests = "quests";
    public const string TablePlayerQuests = "player_quests";
    public const string TableAchievements = "achievements";
    public const string TablePlayerAchievements = "player_achievements";
    public const string TableQuestHistory = "quest_history";

    // ==================== VUES ====================
    public const string ViewPlayerStats = "vw_PlayerStats";

    // ==================== COLONNES - PLAYERS ====================
    public const string ColPlayerId = "id";
    public const string ColPlayerName = "name";
    public const string ColPlayerClass = "class";
    public const string ColPlayerLevel = "level";
    public const string ColPlayerExperience = "experience";

    // ==================== COLONNES - QUESTS ====================
    public const string ColQuestId = "quest_id";
    public const string ColQuestName = "name";
    public const string ColQuestDescription = "description";
    public const string ColQuestType = "type";
    public const string ColQuestRewardXp = "reward_xp";
    public const string ColQuestTargetProgress = "target_progress";
    public const string ColQuestIsDynamic = "is_dynamic";

    // ==================== COLONNES - PLAYER_QUESTS ====================
    public const string ColPlayerQuestPlayerId = "player_id";
    public const string ColPlayerQuestQuestId = "quest_id";
    public const string ColPlayerQuestStatus = "status";
    public const string ColPlayerQuestProgress = "progress";

    // ==================== COLONNES - ACHIEVEMENTS ====================
    public const string ColAchievementId = "id";
    public const string ColAchievementName = "name";
    public const string ColAchievementDescription = "description";
    public const string ColAchievementConditionType = "condition_type";
    public const string ColAchievementConditionValue = "condition_value";
    public const string ColAchievementRewardXp = "reward_xp";

    // ==================== COLONNES - PLAYER_ACHIEVEMENTS ====================
    public const string ColPlayerAchievementPlayerId = "player_id";
    public const string ColPlayerAchievementAchievementId = "achievement_id";
    public const string ColPlayerAchievementUnlockedDate = "unlocked_date";

    // ==================== COLONNES - QUEST_HISTORY ====================
    public const string ColQuestHistoryId = "id";
    public const string ColQuestHistoryPlayerId = "player_id";
    public const string ColQuestHistoryQuestId = "quest_id";
    public const string ColQuestHistoryQuestName = "quest_name";
    public const string ColQuestHistoryCompletionDate = "completion_date";
    public const string ColQuestHistoryXpGained = "xp_gained";
}
