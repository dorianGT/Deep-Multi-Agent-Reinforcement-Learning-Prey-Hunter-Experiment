using TMPro;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public TextMeshProUGUI score;

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
    }
}
