using System.Collections;
using UnityEngine;

public class BackToPool : MonoBehaviour
{
    public float time;

    private void OnEnable()
    {
        StartCoroutine(BackToPoolIE());
    }

    IEnumerator BackToPoolIE()
    {
        yield return new WaitForSeconds(time);
        ObjectPool.Instance.ReturnObject(gameObject);
    }
}
