using UnityEngine;

/// <summary>
/// Permet de modifier la vitesse du temps en jeux (pour tests).
/// </summary>
public class TimeManager : MonoBehaviour
{
    [Range(0f, 10f)]
    public float timeSpeed = 1;

    private void Update()
    {
        if (Time.timeScale != timeSpeed)
        {
            Time.timeScale = timeSpeed;
        }
    }
}
