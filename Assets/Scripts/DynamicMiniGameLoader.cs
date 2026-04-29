using System.Collections;
using UnityEngine;

public class DynamicMiniGameLoader : MonoBehaviour
{
    [SerializeField] private AddressablesManager addressablesManager;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float preloadOffset = 1000f;
    [SerializeField] private bool debugMode = false;

    private InfiniteScrollManager infiniteScrollManager;

    public delegate void OnGameLoaded(int index, string gameId, GameObject instance);
    public delegate void OnGameLoadFailed(int index, string address, string error);

    public event OnGameLoaded GameLoaded;
    public event OnGameLoadFailed GameLoadFailed;

    private void Start()
    {
        if (addressablesManager == null)
            addressablesManager = FindObjectOfType<AddressablesManager>();

        infiniteScrollManager = GetComponent<InfiniteScrollManager>();

        if (infiniteScrollManager != null)
        {
            infiniteScrollManager.GameSelected += OnGameSelected;
        }
    }

    private void OnGameSelected(int itemIndex, string gameId)
    {
        PreloadNextGames(itemIndex);
    }

    private void PreloadNextGames(int currentIndex)
    {
        if (infiniteScrollManager == null)
            return;

        for (int i = currentIndex; i < currentIndex + 2; i++)
        {
            PreloadGameAtIndex(i);
        }
    }

    private void PreloadGameAtIndex(int index)
    {
        string address = $"minigame_{index}";

        if (addressablesManager.IsAssetLoaded(address))
        {
            if (debugMode)
                Debug.Log($"[DynamicMiniGameLoader] Jogo já estava carregado: {address}");

            return;
        }

        addressablesManager.LoadAssetAsync<GameObject>(
            address,
            (prefab) => OnGamePrefabLoaded(index, address, prefab),
            (error) => OnGamePrefabLoadFailed(index, address, error),
            (progress) => OnGameLoadProgress(index, address, progress)
        );
    }

    public void LoadGameDynamically(int index, string address, System.Action<GameObject> onLoaded)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[DynamicMiniGameLoader] Endereço não pode ser vazio");
            return;
        }

        addressablesManager.LoadAssetAsync<GameObject>(
            address,
            (prefab) =>
            {
                if (prefab != null)
                {
                    onLoaded?.Invoke(prefab);
                    GameLoaded?.Invoke(index, address, prefab);
                }
            },
            (error) =>
            {
                Debug.LogError($"[DynamicMiniGameLoader] Falha ao carregar {address}: {error}");
                GameLoadFailed?.Invoke(index, address, error);
            }
        );
    }

    public void SpawnGameInstance(int index, string address, System.Action<GameObject> onSpawned)
    {
        LoadGameDynamically(index, address, (prefab) =>
        {
            if (prefab != null)
            {
                GameObject instance = Instantiate(
                    prefab,
                    spawnParent ?? transform
                );

                instance.name = $"{address}_Instance_{index}";

                if (debugMode)
                    Debug.Log($"[DynamicMiniGameLoader] Instância criada: {instance.name}");

                onSpawned?.Invoke(instance);
            }
        });
    }

    public void UnloadGameAddress(string address)
    {
        addressablesManager.UnloadAsset(address);

        if (debugMode)
            Debug.Log($"[DynamicMiniGameLoader] Endereço descarregado: {address}");
    }

    private void OnGamePrefabLoaded(int index, string address, GameObject prefab)
    {
        if (debugMode)
            Debug.Log($"[DynamicMiniGameLoader] Prefab carregado: {address} (Índice: {index})");

        GameLoaded?.Invoke(index, address, prefab);
    }

    private void OnGamePrefabLoadFailed(int index, string address, string error)
    {
        Debug.LogError($"[DynamicMiniGameLoader] Falha ao carregar prefab {address}: {error}");
        GameLoadFailed?.Invoke(index, address, error);
    }

    private void OnGameLoadProgress(int index, string address, float progress)
    {
        if (debugMode && progress % 0.25f < 0.01f)
            Debug.Log($"[DynamicMiniGameLoader] Progresso de {address}: {progress:P0}");
    }

    private void OnDestroy()
    {
        if (infiniteScrollManager != null)
        {
            infiniteScrollManager.GameSelected -= OnGameSelected;
        }
    }
}
