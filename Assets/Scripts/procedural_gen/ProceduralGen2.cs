using UnityEngine;
using System.Collections.Generic;

public class RoomGenerator2D : MonoBehaviour
{
    public int width = 10;
    public int height = 10;

    public int seed = 42;
    public bool useRandomSeed = false;

    public float cellSize = 1f;

    [Header("Borders and Corners")]
    public GameObject wallPrefab;
    public GameObject columnPrefab;

    [Header("Wall Settings")]
    public int wallLength = 1;

    [Header("GridObject Markers")]
    public GridObject wallMarker;
    public GridObject columnMarker;

    [System.Serializable]
    public class GridObject
    {
        public GameObject prefab;
        public float probability = 1f;
        public Color debugColor = Color.white;
        public int widthInCells = 1;
        public int heightInCells = 1;
    }

    public List<GridObject> objectPrefabs;

    private GridObject[,] grid;

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (useRandomSeed)
            seed = Random.Range(0, 100000);

        Random.InitState(seed);

        grid = new GridObject[width, height];

        PlaceBordersAndCorners();

        PlaceObjects();
    }

    void PlaceObjects()
    {
        foreach (var gridObject in objectPrefabs)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (Random.value < gridObject.probability)
                    {
                        if (CanPlaceObject(x, y, gridObject))
                        {
                            PlaceGridObject(x, y, gridObject);
                        }
                    }
                }
            }
        }
    }

    bool CanPlaceObject(int startX, int startY, GridObject gridObject)
    {
        for (int x = startX; x < startX + gridObject.widthInCells; x++)
        {
            for (int y = startY; y < startY + gridObject.heightInCells; y++)
            {
                if (x >= width || y >= height || grid[x, y] != null)
                {
                    return false;
                }
            }
        }
        return true;
    }

    void PlaceGridObject(int startX, int startY, GridObject gridObject)
    {
        // Calcul du centre de la zone couverte
        float offsetX = (gridObject.widthInCells * cellSize) / 2f - cellSize / 2f;
        float offsetZ = (gridObject.heightInCells * cellSize) / 2f - cellSize / 2f;

        Vector3 position = new Vector3(
            (startX * cellSize) + offsetX,
            0,
            (startY * cellSize) + offsetZ
        );

        GameObject obj = Instantiate(gridObject.prefab, transform);
        obj.transform.localPosition = position;

        for (int x = startX; x < startX + gridObject.widthInCells; x++)
        {
            for (int y = startY; y < startY + gridObject.heightInCells; y++)
            {
                grid[x, y] = gridObject;
            }
        }
    }


    void PlaceBordersAndCorners()
    {
        float offset = cellSize / 2;

        // Haut et bas
        for (int x = 0; x < width; x += wallLength)
        {
            // Bas
            PlaceWallAndMarkCells(new Vector3(x * cellSize + offset, 0, 0), 0f, x, 0, true);
            // Haut
            PlaceWallAndMarkCells(new Vector3(x * cellSize + offset, 0, (height - 1) * cellSize), 180f, x, height - 1, true);
        }

        // Gauche et droite
        for (int y = 0; y < height; y += wallLength)
        {
            // Gauche
            PlaceWallAndMarkCells(new Vector3(0, 0, y * cellSize + offset), 90f, 0, y, false);
            // Droite
            PlaceWallAndMarkCells(new Vector3((width - 1) * cellSize, 0, y * cellSize + offset), -90f, width - 1, y, false);
        }

        // Colonnes (coins)
        MarkColumnCell(0, 0, new Vector3(0, 0, 0));
        MarkColumnCell(width - 1, 0, new Vector3((width - 1) * cellSize, 0, 0));
        MarkColumnCell(0, height - 1, new Vector3(0, 0, (height - 1) * cellSize));
        MarkColumnCell(width - 1, height - 1, new Vector3((width - 1) * cellSize, 0, (height - 1) * cellSize));
    }

    void PlaceWallAndMarkCells(Vector3 localPosition, float yRotation, int startX, int startY, bool horizontal)
    {
        if (wallPrefab == null || wallMarker == null) return;

        GameObject wall = Instantiate(wallPrefab, transform);
        wall.transform.localPosition = localPosition;
        wall.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

        for (int i = 0; i < wallLength; i++)
        {
            int x = horizontal ? startX + i : startX;
            int y = horizontal ? startY : startY + i;

            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                grid[x, y] = wallMarker;
            }
        }
    }

    void MarkColumnCell(int x, int y, Vector3 localPosition)
    {
        if (columnPrefab == null || columnMarker == null) return;

        GameObject column = Instantiate(columnPrefab, transform);
        column.transform.localPosition = localPosition;
        column.transform.localRotation = Quaternion.identity;

        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            grid[x, y] = columnMarker;
        }
    }

    private void OnDrawGizmos()
    {
        if (grid == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = transform.position + new Vector3(x * cellSize, 0, y * cellSize);
                Color color = new Color(1, 1, 1, 0.1f);

                if (grid[x, y] != null)
                    color = new Color(grid[x, y].debugColor.r, grid[x, y].debugColor.g, grid[x, y].debugColor.b, 0.25f);

                Gizmos.color = color;
                Gizmos.DrawCube(pos + new Vector3(0, 0.01f, 0), new Vector3(cellSize, 0.05f, cellSize));

                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(pos + new Vector3(0, 0.01f, 0), new Vector3(cellSize, 0.05f, cellSize));
            }
        }
    }
}
