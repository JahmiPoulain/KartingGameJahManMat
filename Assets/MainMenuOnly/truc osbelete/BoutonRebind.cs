using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class BoutonRebind : MonoBehaviour
{
    [Header("Configuration")]
    public InputActionReference maTouche;
    public TMP_Text texteAffichage;

    [Header("Type de Contrôle")]
    public bool estPourManette; // Coche cette case dans Unity pour la colonne de droite !

    private const string NomFichierSauvegarde = "MesTouchesCustom";

    private void Start()
    {
        ChargerLesTouches();
        ActualiserTexte();
    }

    public void LancerLeChangement()
    {
        if (maTouche == null) return;

        texteAffichage.text = "...";
        maTouche.action.Disable();

        var operation = maTouche.action.PerformInteractiveRebinding();

        // --- LA MAGIE DU FILTRAGE ---
        if (estPourManette)
        {
            // On veut QUE la manette, donc on exclut Clavier et Souris
            operation.WithControlsExcluding("<Keyboard>")
                     .WithControlsExcluding("<Mouse>");
        }
        else
        {
            // On veut QUE le clavier, donc on exclut la Manette
            operation.WithControlsExcluding("<Gamepad>");
        }

        operation.OnComplete(op => {
            ActualiserTexte();
            SauvegarderLesTouches();
            maTouche.action.Enable();
            op.Dispose();
        })
        .Start();
    }

    private void SauvegarderLesTouches()
    {
        var asset = maTouche.action.actionMap.asset;
        string donnees = asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(NomFichierSauvegarde, donnees);
        PlayerPrefs.Save();
    }

    private void ChargerLesTouches()
    {
        string donneesSauvegardees = PlayerPrefs.GetString(NomFichierSauvegarde);
        if (!string.IsNullOrEmpty(donneesSauvegardees))
        {
            maTouche.action.actionMap.asset.LoadBindingOverridesFromJson(donneesSauvegardees);
        }
    }

    public void ActualiserTexte()
    {
        if (maTouche != null && texteAffichage != null)
        {
            // IMPORTANT : On demande d'afficher la touche spécifique au groupe (Clavier ou Manette)
            int bindingIndex = estPourManette ? 1 : 0;

            // Si tu as bien rangé tes touches dans l'Input Action Asset :
            // Binding 0 = Clavier
            // Binding 1 = Manette
            texteAffichage.text = maTouche.action.GetBindingDisplayString(bindingIndex);
        }
    }
}