using System.Collections;
using UnityEngine;

/// <summary>
/// Ce script retourne automatiquement l'objet au pool apr�s un d�lai d�fini.
/// � utiliser avec un syst�me de pooling pour recycler les objets.
/// </summary>
public class BackToPool : MonoBehaviour
{
    /// <summary>
    /// Temps en secondes avant que l'objet ne soit retourn� au pool.
    /// </summary>
    public float time;

    /// <summary>
    /// Lance la coroutine de retour au pool d�s que l'objet est activ�.
    /// </summary>
    private void OnEnable()
    {
        StartCoroutine(BackToPoolIE());
    }

    /// <summary>
    /// Coroutine qui attend un certain temps avant de retourner l'objet au pool.
    /// </summary>
    IEnumerator BackToPoolIE()
    {
        yield return new WaitForSeconds(time);
        ObjectPool.Instance.ReturnObject(gameObject);
    }
}
