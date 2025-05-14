using System.Collections;
using UnityEngine;

/// <summary>
/// Ce script retourne automatiquement l'objet au pool après un délai défini.
/// À utiliser avec un système de pooling pour recycler les objets.
/// </summary>
public class BackToPool : MonoBehaviour
{
    /// <summary>
    /// Temps en secondes avant que l'objet ne soit retourné au pool.
    /// </summary>
    public float time;

    /// <summary>
    /// Lance la coroutine de retour au pool dès que l'objet est activé.
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
