using TMPro;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public TextMeshProUGUI score;
    public TextMeshProUGUI percentage; // Nouveau champ pour afficher les pourcentages

    private int preyWinCount;
    private int hunterWinCount;

    public void HunterWin()
    {
        hunterWinCount++;
        UpdateUI();
    }

    public void PreyWin()
    {
        preyWinCount++;
        UpdateUI();
    }

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
