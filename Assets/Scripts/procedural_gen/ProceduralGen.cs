using UnityEngine;

public class ProceduralGen : MonoBehaviour
{
    public GameObject[] prefab; // Objects to spawn
    public Transform spawnParent; // Assign the parent in the Inspector
    public int objectCount = 50;
    public Vector2 areaSize = new Vector2(10, 10); // Local area within parent

    void Start()
    {
        for (int i = 0; i < objectCount; i++)
        {
            GameObject objToSpawn = prefab[Random.Range(0, prefab.Length)];

            // Local position within the parent
            Vector3 localPosition = new Vector3(
                Random.Range(-areaSize.x, areaSize.x),
                0,
                Random.Range(-areaSize.y, areaSize.y)
            );

            // Instantiate and set parent
            GameObject spawned = Instantiate(objToSpawn, spawnParent);
            spawned.transform.localPosition = localPosition; // Local to parent
        }
    }
}
