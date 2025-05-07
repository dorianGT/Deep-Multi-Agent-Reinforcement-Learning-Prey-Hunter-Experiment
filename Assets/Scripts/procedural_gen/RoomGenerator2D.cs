using UnityEngine;
using System.Collections.Generic;


public class RoomGenerator2D : MonoBehaviour
{
    public int width = 10;
    public int height = 10;

    public int seed = 42;
    public bool useRandomSeed = false;

    public float cellSize = 1f;

    [Header("Wall Settings")]
    public int wallLength = 1;

    [Header("GridObject Markers")]
    public GridObject wallMarker;
    public GridObject columnMarker;

    [Header("Floor Settings")]
    public GameObject floorPrefab;

    public bool debug;

    private List<GameObject> objects;

    public enum PlacementConstraint
    {
        None,
        RequiresWallNearby,
        AvoidWallNearby,
        RequiresColumnNearby,
        AvoidColumnNearby
    }


    [System.Serializable]
    public class GridObject
    {
        public GameObject prefab;
        public float probability = 1f;
        public Color debugColor = Color.white;
        public int widthInCells = 1;
        public int heightInCells = 1;

        [Header("Placement Rule")]
        public PlacementConstraint placementConstraint = PlacementConstraint.None;

        public bool allowRandomRotation = false;
    }


    public List<GridObject> objectPrefabs;

    private GridObject[,] grid;

    private void Awake()
    {
        objects = new List<GameObject>();
        grid = new GridObject[width, height];
    }

    void Start()
    {
        //objects = new List<GameObject>();
        //Generate();
    }

    public void Generate()
    {
        ObjectPool.Instance.ReturnObjects(objects);
        objects.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = null;
            }
        }

        if (useRandomSeed)
            seed = Random.Range(0, 100000);

        Random.InitState(seed);

        //grid = new GridObject[width, height];

        PlaceFloorTiles();

        PlaceBordersAndCorners();

        PlaceObjects();
    }

    void PlaceFloorTiles()
    {
        if (floorPrefab == null) return;

        float offset = cellSize / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x * cellSize + offset, 0, y * cellSize - offset);
                GameObject tile = ObjectPool.Instance.GetObject(floorPrefab, pos,Quaternion.identity, transform);
                objects.Add(tile);
                //tile.transform.localPosition = pos;
                //tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = new Vector3(cellSize, 1, cellSize);
            }
        }
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
                            bool isNearWall = IsNearby(x, y, wallMarker);
                            bool isNearColumn = IsNearby(x, y, columnMarker);

                            if(!isNearWall)
                                isNearWall = IsNearby(x+(gridObject.widthInCells-1), y + (gridObject.heightInCells - 1), wallMarker);
                            if (!isNearColumn)
                                isNearColumn = IsNearby(x + (gridObject.widthInCells - 1), y + (gridObject.heightInCells - 1), columnMarker);

                            switch (gridObject.placementConstraint)
                            {
                                case PlacementConstraint.RequiresWallNearby:
                                    if (!isNearWall) continue;
                                    break;
                                case PlacementConstraint.AvoidWallNearby:
                                    if (isNearWall) continue;
                                    break;
                                case PlacementConstraint.RequiresColumnNearby:
                                    if (!isNearColumn) continue;
                                    break;
                                case PlacementConstraint.AvoidColumnNearby:
                                    if (isNearColumn) continue;
                                    break;
                            }

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
        bool rotated = gridObject.allowRandomRotation && Random.value < 0.5f;

        int widthInCells = rotated ? gridObject.heightInCells : gridObject.widthInCells;
        int heightInCells = rotated ? gridObject.widthInCells : gridObject.heightInCells;

        float offsetX = (widthInCells * cellSize) / 2f - cellSize / 2f;
        float offsetZ = (heightInCells * cellSize) / 2f - cellSize / 2f;

        Vector3 position = new Vector3(
            (startX * cellSize) + offsetX,
            0,
            (startY * cellSize) + offsetZ
        );
        
        GameObject obj = ObjectPool.Instance.GetObject(gridObject.prefab, position, Quaternion.identity, transform);
        objects.Add(obj);
        obj.transform.localPosition = position;

        if (rotated)
            obj.transform.localRotation = Quaternion.Euler(0, 90f, 0);
        else
            obj.transform.localRotation = Quaternion.identity;

        for (int x = 0; x < widthInCells; x++)
        {
            for (int y = 0; y < heightInCells; y++)
            {
                int gridX = startX + x;
                int gridY = startY + y;
                if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
                {
                    grid[gridX, gridY] = gridObject;
                }
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
        GameObject wall = ObjectPool.Instance.GetObject(wallMarker.prefab, localPosition, Quaternion.Euler(0f, yRotation, 0f), transform);
        objects.Add(wall);
        //wall.transform.localPosition = localPosition;
        //wall.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

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
        GameObject column = ObjectPool.Instance.GetObject(columnMarker.prefab, localPosition, Quaternion.identity, transform);
        objects.Add(column);
        //column.transform.localPosition = localPosition;
        //column.transform.localRotation = Quaternion.identity;

        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            grid[x, y] = columnMarker;
        }
    }

    bool IsNearby(int x, int y, GridObject target, int range = 1)
    {
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;

                if ((dx != 0 || dy != 0) && nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (grid[nx, ny] == target)
                        return true;
                }
            }
        }
        return false;
    }

    public List<Vector3> GetAvailableWorldPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        float offset = cellSize / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null && !IsNearby(x, y, wallMarker))
                {
                    Vector3 worldPos = new Vector3(x * cellSize + offset, 0.5f, y * cellSize - offset);
                    positions.Add(worldPos);
                }
            }
        }

        // Shuffle
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 temp = positions[i];
            int rand = Random.Range(i, positions.Count);
            positions[i] = positions[rand];
            positions[rand] = temp;
        }

        return positions.GetRange(0, Mathf.Min(count, positions.Count));
    }




    private void OnDrawGizmos()
    {
        if (!debug)
            return;
        if (grid == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = transform.position + new Vector3(x * cellSize, 0.5f, y * cellSize);
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
