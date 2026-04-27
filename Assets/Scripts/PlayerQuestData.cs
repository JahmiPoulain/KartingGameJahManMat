/// <summary>
/// Classe de données pour représenter une quête d'un joueur.
/// </summary>
[System.Serializable]
public class PlayerQuestData
{
    public int QuestId;
    public string Name;
    public string Description;
    public int RewardXp;
    public int TargetProgress;
    public string Status;
    public int Progress;
}
