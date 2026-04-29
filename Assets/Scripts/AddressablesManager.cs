using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesManager : MonoBehaviour
{
    [SerializeField] private bool preloadAssets = true;
    [SerializeField] private int maxConcurrentLoads = 3;
    [SerializeField] private bool debugMode = false;

    private Dictionary<string, AsyncOperationHandle> loadedAssets = new Dictionary<string, AsyncOperationHandle>();
    private Queue<LoadRequest> loadQueue = new Queue<LoadRequest>();
    private int activeLoadCount = 0;

    public delegate void OnAssetLoaded<T>(string address, T asset) where T : class;
    public delegate void OnAssetLoadFailed(string address, string error);
    public delegate void OnProgressChanged(string address, float progress);

    public event OnProgressChanged ProgressChanged;
    public event OnAssetLoadFailed LoadFailed;

    private void OnDestroy()
    {
        UnloadAll();
    }

    public void LoadAssetAsync<T>(
        string address,
        System.Action<T> onComplete,
        System.Action<string> onFailed = null,
        System.Action<float> onProgress = null) where T : class
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[AddressablesManager] Endereço não pode ser vazio");
            onFailed?.Invoke("Endereço vazio");
            return;
        }

        if (loadedAssets.ContainsKey(address))
        {
            if (loadedAssets[address].IsValid() && loadedAssets[address].IsDone)
            {
                T cachedAsset = loadedAssets[address].Result as T;
                if (cachedAsset != null)
                {
                    if (debugMode)
                        Debug.Log($"[AddressablesManager] Ativo carregado do cache: {address}");

                    onComplete?.Invoke(cachedAsset);
                    return;
                }
            }
        }

        LoadRequest request = new LoadRequest
        {
            address = address,
            onComplete = (asset) => onComplete?.Invoke(asset as T),
            onFailed = onFailed,
            onProgress = onProgress,
            assetType = typeof(T)
        };

        loadQueue.Enqueue(request);
        ProcessLoadQueue();
    }

    private void ProcessLoadQueue()
    {
        while (loadQueue.Count > 0 && activeLoadCount < maxConcurrentLoads)
        {
            LoadRequest request = loadQueue.Dequeue();
            StartCoroutine(ExecuteLoadRequest(request));
        }
    }

    private IEnumerator ExecuteLoadRequest(LoadRequest request)
    {
        activeLoadCount++;

        try
        {
            AsyncOperationHandle handle = Addressables.LoadAssetAsync<object>(request.address);

            if (debugMode)
                Debug.Log($"[AddressablesManager] Iniciando carregamento: {request.address}");

            while (!handle.IsDone)
            {
                float progress = handle.PercentComplete;
                request.onProgress?.Invoke(progress);
                ProgressChanged?.Invoke(request.address, progress);

                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                object loadedAsset = handle.Result;

                if (!loadedAssets.ContainsKey(request.address))
                {
                    loadedAssets[request.address] = handle;
                }

                if (debugMode)
                    Debug.Log($"[AddressablesManager] Ativo carregado com sucesso: {request.address}");

                request.onComplete?.Invoke(loadedAsset);
                ProgressChanged?.Invoke(request.address, 1f);
            }
            else
            {
                string errorMsg = $"Falha ao carregar {request.address}: {handle.OperationException?.Message}";
                Debug.LogError($"[AddressablesManager] {errorMsg}");

                request.onFailed?.Invoke(errorMsg);
                LoadFailed?.Invoke(request.address, errorMsg);
                Addressables.Release(handle);
            }
        }
        catch (Exception ex)
        {
            string errorMsg = $"Exceção ao carregar {request.address}: {ex.Message}";
            Debug.LogError($"[AddressablesManager] {errorMsg}");

            request.onFailed?.Invoke(errorMsg);
            LoadFailed?.Invoke(request.address, errorMsg);
        }
        finally
        {
            activeLoadCount--;
            ProcessLoadQueue();
        }
    }

    public bool IsAssetLoaded(string address)
    {
        if (!loadedAssets.ContainsKey(address))
            return false;

        AsyncOperationHandle handle = loadedAssets[address];
        return handle.IsValid() && handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded;
    }

    public T GetCachedAsset<T>(string address) where T : class
    {
        if (!loadedAssets.ContainsKey(address))
            return null;

        AsyncOperationHandle handle = loadedAssets[address];
        if (handle.IsValid() && handle.IsDone)
        {
            return handle.Result as T;
        }

        return null;
    }

    public void UnloadAsset(string address)
    {
        if (loadedAssets.ContainsKey(address))
        {
            AsyncOperationHandle handle = loadedAssets[address];
            if (handle.IsValid())
            {
                Addressables.Release(handle);
                loadedAssets.Remove(address);

                if (debugMode)
                    Debug.Log($"[AddressablesManager] Ativo descarregado: {address}");
            }
        }
    }

    public void UnloadAll()
    {
        foreach (var kvp in loadedAssets)
        {
            if (kvp.Value.IsValid())
            {
                Addressables.Release(kvp.Value);
            }
        }

        loadedAssets.Clear();

        if (debugMode)
            Debug.Log("[AddressablesManager] Todos os ativos foram descarregados");
    }

    public int GetLoadQueueCount()
    {
        return loadQueue.Count;
    }

    public int GetActiveLoadCount()
    {
        return activeLoadCount;
    }

    private class LoadRequest
    {
        public string address;
        public System.Action<object> onComplete;
        public System.Action<string> onFailed;
        public System.Action<float> onProgress;
        public System.Type assetType;
    }
}
