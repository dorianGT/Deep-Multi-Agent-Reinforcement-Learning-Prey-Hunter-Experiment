using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{

    public static ObjectPool Instance { get; private set; }


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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Pour éviter les doublons
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
                obj.name = key; // Nettoie "(Clone)" si réutilisé dans hiérarchie
                obj.SetActive(false);
                poolDictionary[key].Enqueue(obj);
                reverseLookup[obj] = key;
            }
        }
    }

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

    public void ReturnObjects(List<GameObject> objects)
    {
        foreach (var obj in objects)
        {
            ReturnObject(obj);
        }
    }


    public void ReturnAll()
    {
        foreach (GameObject obj in activeObjects)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform); // Optionnel
            if (reverseLookup.TryGetValue(obj, out string key))
            {
                poolDictionary[key].Enqueue(obj);
            }
        }

        activeObjects.Clear();
    }
}
