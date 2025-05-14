using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Classe de génération de pièce en 2D, avec murs, colonnes et objets selon des règles de placement.
/// </summary>
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

    /// <summary>
    /// Contrainte de placement.
    /// </summary>
    public enum PlacementConstraint
    {
        None,
        RequiresWallNearby,
        AvoidWallNearby,
        RequiresColumnNearby,
        AvoidColumnNearby
    }

    /// <summary>
    /// Représente un objet de la grille.
    /// </summary>
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

    /// <summary>
    /// Initialise les données internes du générateur.
    /// </summary>
    private void Awake()
    {
        objects = new List<GameObject>();
        grid = new GridObject[width, height];
    }

    /// <summary>
    /// Lance la génération complète de la pièce : sol, murs, colonnes et objets.
    /// </summary>
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

    /// <summary>
    /// Place les tiles sur toute la grille.
    /// </summary>
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
                tile.transform.localScale = new Vector3(cellSize, 1, cellSize);
            }
        }
    }

    /// <summary>
    /// Place les objets définis dans la grille selon leur probabilité et contraintes.
    /// </summary>
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

    /// <summary>
    /// Vérifie si un objet peut être placé à une position donnée sans chevauchement.
    /// </summary>
    /// <param name="startX">Coordonnée X de départ.</param>
    /// <param name="startY">Coordonnée Y de départ.</param>
    /// <param name="gridObject">Objet à placer.</param>
    /// <returns>Vrai si le placement est possible, sinon faux.</returns>
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

    /// <summary>
    /// Place un objet sur la grille avec gestion de la rotation et enregistrement dans la grille.
    /// </summary>
    /// <param name="startX">Coordonnée X de départ.</param>
    /// <param name="startY">Coordonnée Y de départ.</param>
    /// <param name="gridObject">Objet à placer.</param>
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

    /// <summary>
    /// Place les murs autour de la pièce et marque les coins comme colonnes.
    /// </summary>
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

    /// <summary>
    /// Place un mur à une position donnée et marque les cellules qu’il occupe.
    /// </summary>
    /// <param name="localPosition">Position locale du mur.</param>
    /// <param name="yRotation">Rotation du mur.</param>
    /// <param name="startX">Coordonnée X de départ.</param>
    /// <param name="startY">Coordonnée Y de départ.</param>
    /// <param name="horizontal">Indique si le mur est horizontal.</param>
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

    /// <summary>
    /// Marque une cellule comme étant occupée par une colonne.
    /// </summary>
    /// <param name="x">Coordonnée X de la cellule.</param>
    /// <param name="y">Coordonnée Y de la cellule.</param>
    /// <param name="localPosition">Position locale de la colonne.</param>
    void MarkColumnCell(int x, int y, Vector3 localPosition)
    {  
        GameObject column = ObjectPool.Instance.GetObject(columnMarker.prefab, localPosition, Quaternion.identity, transform);
        objects.Add(column);

        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            grid[x, y] = columnMarker;
        }
    }

    /// <summary>
    /// Vérifie si un objet cible est proche d’une position donnée.
    /// </summary>
    /// <param name="x">Coordonnée X de la position.</param>
    /// <param name="y">Coordonnée Y de la position.</param>
    /// <param name="target">Objet cible à rechercher.</param>
    /// <param name="range">Rayon de recherche autour de la position.</param>
    /// <returns>Vrai si un objet cible est proche, sinon faux.</returns>
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


    /// <summary>
    /// Retourne une liste de positions libres dans le monde, en évitant murs et objets spécifiques.
    /// </summary>
    /// <param name="count">Nombre de positions à récupérer.</param>
    /// <returns>Liste de positions disponibles dans le monde.</returns>
    public List<Vector3> GetAvailableWorldPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        float offset = cellSize / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null && !IsNearby(x, y, wallMarker) && !IsNearby(x, y, objectPrefabs[objectPrefabs.Count-1]))
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



    /// <summary>
    /// Dessine la grille en mode édition pour visualiser les objets placés.
    /// </summary>
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
