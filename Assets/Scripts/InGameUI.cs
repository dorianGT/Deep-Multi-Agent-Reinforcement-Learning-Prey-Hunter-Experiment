using TMPro;
using UnityEngine;

/// <summary>
/// G�re l'affichage de l'interface utilisateur en jeu, notamment le score et les pourcentages de victoire
/// pour les �quipes de proies et de chasseurs.
/// </summary>
public class InGameUI : MonoBehaviour
{
    /// <summary>
    /// R�f�rence vers le composant TextMeshProUGUI qui affiche le score.
    /// </summary>
    public TextMeshProUGUI score;

    /// <summary>
    /// R�f�rence vers le composant TextMeshProUGUI qui affiche les pourcentages de victoire.
    /// </summary>
    public TextMeshProUGUI percentage;

    private int preyWinCount;
    private int hunterWinCount;

    /// <summary>
    /// Incr�mente le nombre de victoires des chasseurs et met � jour l'interface.
    /// </summary>
    public void HunterWin()
    {
        hunterWinCount++;
        UpdateUI();
    }

    /// <summary>
    /// Incr�mente le nombre de victoires des proies et met � jour l'interface.
    /// </summary>
    public void PreyWin()
    {
        preyWinCount++;
        UpdateUI();
    }

    /// <summary>
    /// Met � jour l'affichage du score et des pourcentages de victoire.
    /// </summary>
    private void UpdateUI()
    {
        score.text = $"<color=blue>{preyWinCount}</color> / <color=red>{hunterWinCount}</color>";

        int total = preyWinCount + hunterWinCount;
        if (total > 0)
        {
            float preyPercent = (preyWinCount / (float)total) * 100f;
            float hunterPercent = (hunterWinCount / (float)total) * 100f;
            percentage.text = $"<color=blue>{preyPercent:F1}%</color> / <color=red>{hunterPercent:F1}%</color>";
        }
    }
}
