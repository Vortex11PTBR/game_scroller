using UnityEngine;

public class AddressablesLoaderExample : MonoBehaviour
{
    [SerializeField] private AddressablesManager addressablesManager;
    [SerializeField] private DynamicMiniGameLoader dynamicLoader;
    [SerializeField] private LoadingProgressUI loadingProgressUI;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button unloadGameButton;
    [SerializeField] private InputField addressInputField;
    [SerializeField] private Text statusText;

    private string lastLoadedAddress = "";

    private void OnEnable()
    {
        if (addressablesManager == null)
            addressablesManager = FindObjectOfType<AddressablesManager>();

        if (dynamicLoader == null)
            dynamicLoader = FindObjectOfType<DynamicMiniGameLoader>();

        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(OnLoadGamePressed);

        if (unloadGameButton != null)
            unloadGameButton.onClick.AddListener(OnUnloadGamePressed);

        UpdateStatusText();
    }

    private void OnDisable()
    {
        if (loadGameButton != null)
            loadGameButton.onClick.RemoveListener(OnLoadGamePressed);

        if (unloadGameButton != null)
            unloadGameButton.onClick.RemoveListener(OnUnloadGamePressed);
    }

    private void OnLoadGamePressed()
    {
        string address = addressInputField != null && addressInputField.text.Length > 0
            ? addressInputField.text
            : "minigame_0";

        lastLoadedAddress = address;

        Debug.Log($"[AddressablesLoaderExample] Carregando: {address}");

        dynamicLoader.SpawnGameInstance(0, address, (instance) =>
        {
            Debug.Log($"[AddressablesLoaderExample] Instância criada: {instance.name}");
            UpdateStatusText($"Carregado: {address}");

            if (unloadGameButton != null)
                unloadGameButton.interactable = true;
        });
    }

    private void OnUnloadGamePressed()
    {
        if (string.IsNullOrEmpty(lastLoadedAddress))
        {
            Debug.LogWarning("[AddressablesLoaderExample] Nenhum ativo carregado");
            return;
        }

        dynamicLoader.UnloadGameAddress(lastLoadedAddress);
        Debug.Log($"[AddressablesLoaderExample] Descarregado: {lastLoadedAddress}");

        UpdateStatusText($"Descarregado: {lastLoadedAddress}");

        if (unloadGameButton != null)
            unloadGameButton.interactable = false;
    }

    private void UpdateStatusText(string message = "")
    {
        if (statusText == null)
            return;

        int queueCount = addressablesManager.GetLoadQueueCount();
        int activeCount = addressablesManager.GetActiveLoadCount();

        string status = $"Fila: {queueCount} | Ativo: {activeCount}\n{message}";
        statusText.text = status;
    }
}
