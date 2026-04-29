using System;
using System.Collections.Generic;
using UnityEngine;

public class GridPlacementSystem : MonoBehaviour
{
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 20;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject[] placeableAssets;
    [SerializeField] private bool showDebugGrid = true;
    [SerializeField] private Color gridColor = Color.white;
    [SerializeField] private bool debugMode = false;

    private Dictionary<Vector2Int, GridCell> gridCells = new Dictionary<Vector2Int, GridCell>();
    private List<PlacedObject> placedObjects = new List<PlacedObject>();
    private Camera mainCamera;

    public delegate void OnObjectPlaced(PlacedObject obj);
    public delegate void OnObjectRemoved(Vector2Int position);
    public delegate void OnGridUpdated();

    public event OnObjectPlaced ObjectPlaced;
    public event OnObjectRemoved ObjectRemoved;
    public event OnGridUpdated GridUpdated;

    private void Awake()
    {
        mainCamera = Camera.main;
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        if (gridParent == null)
            gridParent = transform;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                gridCells[gridPos] = new GridCell { position = gridPos, isOccupied = false };
            }
        }

        if (debugMode)
            Debug.Log($"[GridPlacementSystem] Grid inicializado: {gridWidth}x{gridHeight}");
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(touch.position);
                Vector2Int gridPos = WorldToGridPosition(worldPos);

                if (IsValidGridPosition(gridPos))
                {
                    TryPlaceObject(gridPos, 0);
                }
            }
        }

        if (Input.GetMouseButtonDown(0) && Application.isEditor)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = WorldToGridPosition(worldPos);

            if (IsValidGridPosition(gridPos))
            {
                TryPlaceObject(gridPos, 0);
            }
        }
    }

    public bool TryPlaceObject(Vector2Int gridPos, int assetIndex)
    {
        if (!IsValidGridPosition(gridPos))
        {
            Debug.LogWarning($"[GridPlacementSystem] Posição inválida: {gridPos}");
            return false;
        }

        if (assetIndex < 0 || assetIndex >= placeableAssets.Length)
        {
            Debug.LogError($"[GridPlacementSystem] Índice de asset inválido: {assetIndex}");
            return false;
        }

        if (IsGridPositionOccupied(gridPos))
        {
            Debug.LogWarning($"[GridPlacementSystem] Posição já ocupada: {gridPos}");
            return false;
        }

        GameObject assetPrefab = placeableAssets[assetIndex];
        Vector3 worldPos = GridToWorldPosition(gridPos);

        GameObject instance = Instantiate(
            assetPrefab,
            worldPos,
            Quaternion.identity,
            gridParent
        );

        instance.name = $"{assetPrefab.name}_{gridPos.x}_{gridPos.y}";

        PlacedObject placedObj = new PlacedObject
        {
            id = Guid.NewGuid().ToString().Substring(0, 8),
            gridPosition = gridPos,
            worldPosition = worldPos,
            assetIndex = assetIndex,
            assetId = assetPrefab.name,
            instance = instance,
            placedAtTime = DateTime.UtcNow
        };

        placedObjects.Add(placedObj);
        gridCells[gridPos].isOccupied = true;
        gridCells[gridPos].occupyingObjectId = placedObj.id;

        if (debugMode)
            Debug.Log($"[GridPlacementSystem] Objeto colocado: {placedObj.assetId} em {gridPos}");

        ObjectPlaced?.Invoke(placedObj);
        GridUpdated?.Invoke();

        return true;
    }

    public bool RemoveObject(Vector2Int gridPos)
    {
        if (!IsValidGridPosition(gridPos) || !IsGridPositionOccupied(gridPos))
        {
            return false;
        }

        string objectId = gridCells[gridPos].occupyingObjectId;
        PlacedObject placedObj = placedObjects.Find(obj => obj.id == objectId);

        if (placedObj != null)
        {
            Destroy(placedObj.instance);
            placedObjects.Remove(placedObj);
            gridCells[gridPos].isOccupied = false;
            gridCells[gridPos].occupyingObjectId = null;

            if (debugMode)
                Debug.Log($"[GridPlacementSystem] Objeto removido: {gridPos}");

            ObjectRemoved?.Invoke(gridPos);
            GridUpdated?.Invoke();

            return true;
        }

        return false;
    }

    public bool RemoveObjectById(string objectId)
    {
        PlacedObject placedObj = placedObjects.Find(obj => obj.id == objectId);

        if (placedObj == null)
            return false;

        return RemoveObject(placedObj.gridPosition);
    }

    public bool IsGridPositionOccupied(Vector2Int gridPos)
    {
        if (!IsValidGridPosition(gridPos))
            return false;

        return gridCells[gridPos].isOccupied;
    }

    public bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int gridX = Mathf.RoundToInt(worldPos.x / cellSize);
        int gridY = Mathf.RoundToInt(worldPos.y / cellSize);

        return new Vector2Int(gridX, gridY);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * cellSize + cellSize * 0.5f,
            gridPos.y * cellSize + cellSize * 0.5f,
            0f
        );
    }

    public PlacedObject GetObjectAtPosition(Vector2Int gridPos)
    {
        if (!IsValidGridPosition(gridPos) || !IsGridPositionOccupied(gridPos))
            return null;

        string objectId = gridCells[gridPos].occupyingObjectId;
        return placedObjects.Find(obj => obj.id == objectId);
    }

    public List<PlacedObject> GetAllPlacedObjects()
    {
        return new List<PlacedObject>(placedObjects);
    }

    public int GetPlacedObjectCount()
    {
        return placedObjects.Count;
    }

    public void ClearGrid()
    {
        foreach (var placedObj in placedObjects)
        {
            Destroy(placedObj.instance);
        }

        placedObjects.Clear();

        foreach (var cell in gridCells.Values)
        {
            cell.isOccupied = false;
            cell.occupyingObjectId = null;
        }

        if (debugMode)
            Debug.Log("[GridPlacementSystem] Grid limpo");

        GridUpdated?.Invoke();
    }

    public string ExportToJSON()
    {
        GridExportData exportData = new GridExportData
        {
            gridWidth = gridWidth,
            gridHeight = gridHeight,
            cellSize = cellSize,
            placedObjects = new List<PlacedObjectData>()
        };

        foreach (var placedObj in placedObjects)
        {
            exportData.placedObjects.Add(new PlacedObjectData
            {
                id = placedObj.id,
                assetId = placedObj.assetId,
                assetIndex = placedObj.assetIndex,
                gridX = placedObj.gridPosition.x,
                gridY = placedObj.gridPosition.y,
                worldX = placedObj.worldPosition.x,
                worldY = placedObj.worldPosition.y,
                placedAtTime = placedObj.placedAtTime.ToString("O")
            });
        }

        string json = JsonUtility.ToJson(exportData, true);

        if (debugMode)
            Debug.Log($"[GridPlacementSystem] JSON exportado:\n{json}");

        return json;
    }

    public bool ImportFromJSON(string json)
    {
        try
        {
            GridExportData importData = JsonUtility.FromJson<GridExportData>(json);

            if (importData == null || importData.placedObjects == null)
            {
                Debug.LogError("[GridPlacementSystem] JSON inválido");
                return false;
            }

            ClearGrid();
            gridWidth = importData.gridWidth;
            gridHeight = importData.gridHeight;
            cellSize = importData.cellSize;

            InitializeGrid();

            foreach (var objData in importData.placedObjects)
            {
                Vector2Int gridPos = new Vector2Int(objData.gridX, objData.gridY);

                if (!IsValidGridPosition(gridPos))
                {
                    Debug.LogWarning($"[GridPlacementSystem] Posição inválida no import: {gridPos}");
                    continue;
                }

                if (objData.assetIndex < 0 || objData.assetIndex >= placeableAssets.Length)
                {
                    Debug.LogWarning($"[GridPlacementSystem] Índice de asset inválido: {objData.assetIndex}");
                    continue;
                }

                TryPlaceObject(gridPos, objData.assetIndex);
            }

            if (debugMode)
                Debug.Log($"[GridPlacementSystem] Importado {placedObjects.Count} objetos");

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GridPlacementSystem] Erro ao importar JSON: {ex.Message}");
            return false;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGrid || cellSize <= 0)
            return;

        Gizmos.color = gridColor;

        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 startPos = new Vector3(x * cellSize, 0, 0);
            Vector3 endPos = new Vector3(x * cellSize, gridHeight * cellSize, 0);
            Gizmos.DrawLine(startPos, endPos);
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 startPos = new Vector3(0, y * cellSize, 0);
            Vector3 endPos = new Vector3(gridWidth * cellSize, y * cellSize, 0);
            Gizmos.DrawLine(startPos, endPos);
        }
    }

    [System.Serializable]
    public class PlacedObject
    {
        public string id;
        public Vector2Int gridPosition;
        public Vector3 worldPosition;
        public int assetIndex;
        public string assetId;
        public GameObject instance;
        public DateTime placedAtTime;
    }

    private class GridCell
    {
        public Vector2Int position;
        public bool isOccupied;
        public string occupyingObjectId;
    }

    [System.Serializable]
    public class GridExportData
    {
        public int gridWidth;
        public int gridHeight;
        public float cellSize;
        public List<PlacedObjectData> placedObjects = new List<PlacedObjectData>();
    }

    [System.Serializable]
    public class PlacedObjectData
    {
        public string id;
        public string assetId;
        public int assetIndex;
        public int gridX;
        public int gridY;
        public float worldX;
        public float worldY;
        public string placedAtTime;
    }
}
