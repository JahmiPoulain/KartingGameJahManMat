/// <summary>
/// Classe de données pour représenter un joueur.
/// Utilisée par PlayerRepository et GameManager.
/// </summary>
[System.Serializable]
public class PlayerData
{
    public int Id;
    public string Name;
    public string Class;
    public int Level;
    public int Experience;

    /// <summary>
    /// Constructeur par défaut (nécessaire pour Unity).
    /// </summary>
    public PlayerData() { }

    /// <summary>
    /// Constructeur avec paramètres pour faciliter les tests.
    /// </summary>
    public PlayerData(int id, string name, string playerClass, int level, int experience)
    {
        Id = id;
        Name = name;
        Class = playerClass;
        Level = level;
        Experience = experience;
    }
}

