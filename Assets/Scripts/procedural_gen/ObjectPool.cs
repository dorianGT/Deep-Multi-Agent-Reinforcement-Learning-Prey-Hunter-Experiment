using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// G�re un syst�me de pooling d'objets pour �viter les instantiations et destructions r�p�t�es.
/// Fournit des m�thodes pour obtenir et retourner des objets au pool.
/// </summary>
public class ObjectPool : MonoBehaviour
{

    public static ObjectPool Instance { get; private set; }


    /// <summary>
    /// Repr�sente un objet � instancier dans le pool avec un prefab et une taille initiale.
    /// </summary>
    [System.Serializable]
    public class PoolItem
    {
        public GameObject prefab;
        public int initialSize = 10;
    }


    public List<PoolItem> itemsToPool;

    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<GameObject, string> reverseLookup = new Dictionary<GameObject, string>();
    private List<GameObject> activeObjects = new List<GameObject>();

    /// <summary>
    /// Initialise l'instance singleton et pr�pare les pools pour chaque prefab sp�cifi�.
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Pour �viter les doublons
            return;
        }

        Instance = this;

        foreach (PoolItem item in itemsToPool)
        {
            string key = item.prefab.name;

            if (!poolDictionary.ContainsKey(key))
                poolDictionary[key] = new Queue<GameObject>();

            for (int i = 0; i < item.initialSize; i++)
            {
                GameObject obj = Instantiate(item.prefab);
                obj.name = key; // Nettoie "(Clone)" si r�utilis� dans hi�rarchie
                obj.SetActive(false);
                poolDictionary[key].Enqueue(obj);
                reverseLookup[obj] = key;
            }
        }
    }


    /// <summary>
    /// R�cup�re un objet du pool correspondant au prefab donn�, ou en instancie un nouveau si le pool est vide.
    /// Positionne et active l'objet avant de le retourner.
    /// </summary>
    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        string key = prefab.name;

        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"Pool for prefab '{key}' does not exist.");
            return null;
        }

        GameObject obj;

        if (poolDictionary[key].Count > 0)
        {
            obj = poolDictionary[key].Dequeue();
        }
        else
        {
            obj = Instantiate(prefab);
            obj.name = key;
            reverseLookup[obj] = key;
        }

        obj.transform.SetParent(parent);
        obj.transform.localPosition = position;
        obj.transform.localRotation = rotation;
        obj.SetActive(true);

        activeObjects.Add(obj);
        return obj;
    }

    /// <summary>
    /// D�sactive et retourne un objet au pool dont il provient.
    /// </summary>
    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(transform);

        if (reverseLookup.TryGetValue(obj, out string key))
        {
            poolDictionary[key].Enqueue(obj);
        }

        activeObjects.Remove(obj);
    }

    /// <summary>
    /// Retourne une liste d�objets au pool.
    /// </summary>
    public void ReturnObjects(List<GameObject> objects)
    {
        foreach (var obj in objects)
        {
            ReturnObject(obj);
        }
    }

    /// <summary>
    /// Retourne tous les objets actifs au pool et les d�sactive.
    /// </summary>
    public void ReturnAll()
    {
        foreach (GameObject obj in activeObjects)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            if (reverseLookup.TryGetValue(obj, out string key))
            {
                poolDictionary[key].Enqueue(obj);
            }
        }

        activeObjects.Clear();
    }
}
