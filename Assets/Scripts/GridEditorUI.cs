using UnityEngine;
using UnityEngine.UI;

public class GridEditorUI : MonoBehaviour
{
    [SerializeField] private GridPlacementSystem gridSystem;
    [SerializeField] private Text gridInfoText;
    [SerializeField] private Dropdown assetSelectorDropdown;
    [SerializeField] private Button exportButton;
    [SerializeField] private Button importButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private InputField jsonInputField;
    [SerializeField] private Text selectedAssetText;

    private int currentSelectedAssetIndex = 0;

    private void OnEnable()
    {
        if (gridSystem == null)
            gridSystem = FindObjectOfType<GridPlacementSystem>();

        gridSystem.ObjectPlaced += OnObjectPlaced;
        gridSystem.ObjectRemoved += OnObjectRemoved;
        gridSystem.GridUpdated += OnGridUpdated;

        if (assetSelectorDropdown != null)
        {
            assetSelectorDropdown.onValueChanged.AddListener(OnAssetSelected);
            PopulateAssetDropdown();
        }

        if (exportButton != null)
            exportButton.onClick.AddListener(OnExportPressed);

        if (importButton != null)
            importButton.onClick.AddListener(OnImportPressed);

        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearPressed);

        UpdateGridInfo();
    }

    private void OnDisable()
    {
        if (gridSystem != null)
        {
            gridSystem.ObjectPlaced -= OnObjectPlaced;
            gridSystem.ObjectRemoved -= OnObjectRemoved;
            gridSystem.GridUpdated -= OnGridUpdated;
        }

        if (assetSelectorDropdown != null)
            assetSelectorDropdown.onValueChanged.RemoveListener(OnAssetSelected);

        if (exportButton != null)
            exportButton.onClick.RemoveListener(OnExportPressed);

        if (importButton != null)
            importButton.onClick.RemoveListener(OnImportPressed);

        if (clearButton != null)
            clearButton.onClick.RemoveListener(OnClearPressed);
    }

    private void PopulateAssetDropdown()
    {
        if (assetSelectorDropdown == null)
            return;

        assetSelectorDropdown.ClearOptions();

        // Precisamos acessar os assets do sistema
        // Vamos usar reflexão ou uma propriedade pública
        var options = new System.Collections.Generic.List<Dropdown.OptionData>();

        for (int i = 0; i < 10; i++)  // Ajuste para quantidade real de assets
        {
            options.Add(new Dropdown.OptionData($"Asset {i}"));
        }

        assetSelectorDropdown.AddOptions(options);
    }

    private void OnAssetSelected(int index)
    {
        currentSelectedAssetIndex = index;

        if (selectedAssetText != null)
            selectedAssetText.text = $"Asset Selecionado: {index}";

        Debug.Log($"[GridEditorUI] Asset selecionado: {index}");
    }

    private void OnExportPressed()
    {
        string json = gridSystem.ExportToJSON();

        if (jsonInputField != null)
            jsonInputField.text = json;

        Debug.Log($"[GridEditorUI] JSON exportado com sucesso");
    }

    private void OnImportPressed()
    {
        if (jsonInputField == null || string.IsNullOrEmpty(jsonInputField.text))
        {
            Debug.LogError("[GridEditorUI] JSON vazio para importar");
            return;
        }

        if (gridSystem.ImportFromJSON(jsonInputField.text))
        {
            Debug.Log("[GridEditorUI] JSON importado com sucesso");
            UpdateGridInfo();
        }
        else
        {
            Debug.LogError("[GridEditorUI] Falha ao importar JSON");
        }
    }

    private void OnClearPressed()
    {
        if (EditorUtility.DisplayDialog(
            "Confirmar",
            "Deseja limpar todo o grid?",
            "Sim",
            "Não"))
        {
            gridSystem.ClearGrid();
            UpdateGridInfo();
        }
    }

    private void OnObjectPlaced(GridPlacementSystem.PlacedObject obj)
    {
        UpdateGridInfo();
    }

    private void OnObjectRemoved(Vector2Int position)
    {
        UpdateGridInfo();
    }

    private void OnGridUpdated()
    {
        UpdateGridInfo();
    }

    private void UpdateGridInfo()
    {
        if (gridInfoText == null)
            return;

        int objectCount = gridSystem.GetPlacedObjectCount();
        gridInfoText.text = $"Objetos no grid: {objectCount}";
    }
}
