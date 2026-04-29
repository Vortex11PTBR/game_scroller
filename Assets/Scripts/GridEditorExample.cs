using UnityEngine;

public class GridEditorExample : MonoBehaviour
{
    [SerializeField] private GridPlacementSystem gridSystem;

    private void Start()
    {
        if (gridSystem == null)
            gridSystem = GetComponent<GridPlacementSystem>();

        gridSystem.ObjectPlaced += HandleObjectPlaced;
        gridSystem.ObjectRemoved += HandleObjectRemoved;
    }

    private void HandleObjectPlaced(GridPlacementSystem.PlacedObject obj)
    {
        Debug.Log($"[GridEditorExample] Objeto colocado:\n" +
                 $"  ID: {obj.id}\n" +
                 $"  Asset: {obj.assetId}\n" +
                 $"  Posição Grid: ({obj.gridPosition.x}, {obj.gridPosition.y})\n" +
                 $"  Posição Mundo: {obj.worldPosition}");
    }

    private void HandleObjectRemoved(Vector2Int position)
    {
        Debug.Log($"[GridEditorExample] Objeto removido em ({position.x}, {position.y})");
    }

    public void ExportAndPrint()
    {
        string json = gridSystem.ExportToJSON();
        Debug.Log($"[GridEditorExample] JSON da fase:\n{json}");
    }

    public void PlaceObjectAtPosition(int x, int y, int assetIndex)
    {
        bool success = gridSystem.TryPlaceObject(new Vector2Int(x, y), assetIndex);
        if (success)
            Debug.Log($"[GridEditorExample] Objeto colocado em ({x}, {y})");
        else
            Debug.LogWarning($"[GridEditorExample] Falha ao colocar em ({x}, {y})");
    }

    public void RemoveObjectAtPosition(int x, int y)
    {
        bool success = gridSystem.RemoveObject(new Vector2Int(x, y));
        if (success)
            Debug.Log($"[GridEditorExample] Objeto removido de ({x}, {y})");
        else
            Debug.LogWarning($"[GridEditorExample] Nenhum objeto em ({x}, {y})");
    }
}
