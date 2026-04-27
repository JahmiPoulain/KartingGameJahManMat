using System;

/// <summary>
/// Classe de données représentant une entrée dans l'historique des quêtes terminées.
/// Utilisée pour la Tâche 2.B de l'évaluation (Historique des quêtes terminées).
/// </summary>
[System.Serializable]
public class QuestHistoryEntry
{
    /// <summary>
    /// Nom de la quête qui a été complétée.
    /// </summary>
    public string QuestName;

    /// <summary>
    /// Date et heure exacte à laquelle la quête a été terminée.
    /// Format recommandé : "yyyy-MM-dd HH:mm:ss"
    /// </summary>
    public string CompletionDate;

    /// <summary>
    /// Quantité d'expérience (XP) gagnée lors de la complétion de cette quête.
    /// </summary>
    public int XpGained;

    /// <summary>
    /// Constructeur par défaut (obligatoire pour la sérialisation Unity).
    /// </summary>
    public QuestHistoryEntry() { }

    /// <summary>
    /// Constructeur avec paramètres pour faciliter la création d'entrées.
    /// </summary>
    /// <param name="questName">Nom de la quête terminée</param>
    /// <param name="completionDate">Date et heure de complétion</param>
    /// <param name="xpGained">XP gagné avec cette quête</param>
    public QuestHistoryEntry(string questName, string completionDate, int xpGained)
    {
        QuestName = questName;
        CompletionDate = completionDate;
        XpGained = xpGained;
    }

    /// <summary>
    /// Retourne une représentation lisible de l'entrée (utile pour le debug ou l'affichage).
    /// </summary>
    /// <returns>Exemple : "[2026-04-14 14:30:22] Vaincre le dragon (+200 XP)"</returns>
    public override string ToString()
    {
        return $"[{CompletionDate}] {QuestName} (+{XpGained} XP)";
    }
}
