/// <summary>
/// Représente un achievement débloqué par un joueur.
/// </summary>
[System.Serializable]
public class AchievementData
{
    public int AchievementId;
    public string Name;
    public string Description;
    public int RewardXp;
    public string UnlockedDate;

    public AchievementData() { }

    public AchievementData(int id, string name, string description, int rewardXp, string unlockedDate)
    {
        AchievementId = id;
        Name = name;
        Description = description;
        RewardXp = rewardXp;
        UnlockedDate = unlockedDate;
    }
}

