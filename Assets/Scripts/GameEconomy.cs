using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GameEconomy : MonoBehaviour
{
    [SerializeField] private string apiBaseUrl = "https://api.example.com";
    [SerializeField] private string creatorId = "";
    [SerializeField] private bool debugMode = false;

    private string deviceId;
    private const string DeviceIdKey = "GameEconomy.DeviceId";
    private const int MaxRetries = 3;
    private const float RetryDelaySeconds = 2f;

    public delegate void OnCoinsUpdated(int newBalance, int coinsEarned);
    public event OnCoinsUpdated CoinsUpdated;

    public delegate void OnRequestFailed(string error);
    public event OnRequestFailed RequestFailed;

    private void Awake()
    {
        InitializeDeviceId();
    }

    private void InitializeDeviceId()
    {
        if (PlayerPrefs.HasKey(DeviceIdKey))
        {
            deviceId = PlayerPrefs.GetString(DeviceIdKey);
        }
        else
        {
            deviceId = SystemInfo.deviceUniqueIdentifier;
            PlayerPrefs.SetString(DeviceIdKey, deviceId);
            PlayerPrefs.Save();
        }

        if (debugMode)
            Debug.Log($"[GameEconomy] Device ID: {deviceId}");
    }

    public void WatchRewardedAd(string adType, System.Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(creatorId))
        {
            Debug.LogError("[GameEconomy] Creator ID não foi configurado!");
            onComplete?.Invoke(false);
            return;
        }

        if (string.IsNullOrEmpty(deviceId))
        {
            Debug.LogError("[GameEconomy] Device ID não foi inicializado!");
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(SendAdWatchedRequest(adType, onComplete));
    }

    private IEnumerator SendAdWatchedRequest(string adType, System.Action<bool> onComplete)
    {
        AdWatchRequest request = new AdWatchRequest
        {
            creatorId = creatorId,
            deviceId = deviceId,
            adType = adType,
            timestamp = DateTime.UtcNow.ToString("O")
        };

        string jsonPayload = JsonUtility.ToJson(request);

        if (debugMode)
            Debug.Log($"[GameEconomy] Enviando requisição:\n{jsonPayload}");

        yield return StartCoroutine(SendRequestWithRetry(
            "/api/reward/watch-ad",
            jsonPayload,
            (success, response) =>
            {
                if (success)
                {
                    try
                    {
                        AdWatchResponse parsedResponse = JsonUtility.FromJson<AdWatchResponse>(response);

                        if (parsedResponse.success)
                        {
                            if (debugMode)
                                Debug.Log($"[GameEconomy] Anúncio registrado! Moedas ganhas: {parsedResponse.coinsEarned}");

                            CoinsUpdated?.Invoke(parsedResponse.newBalance, parsedResponse.coinsEarned);
                            onComplete?.Invoke(true);
                        }
                        else
                        {
                            Debug.LogWarning($"[GameEconomy] Servidor retornou erro: {parsedResponse.message}");
                            RequestFailed?.Invoke(parsedResponse.message);
                            onComplete?.Invoke(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[GameEconomy] Erro ao parsear resposta: {ex.Message}");
                        RequestFailed?.Invoke($"Parsing error: {ex.Message}");
                        onComplete?.Invoke(false);
                    }
                }
                else
                {
                    Debug.LogError($"[GameEconomy] Falha na requisição: {response}");
                    RequestFailed?.Invoke(response);
                    onComplete?.Invoke(false);
                }
            }
        ));
    }

    public void CheckCreatorBalance(System.Action<CreatorBalanceResponse> onComplete)
    {
        StartCoroutine(SendCheckBalanceRequest(onComplete));
    }

    private IEnumerator SendCheckBalanceRequest(System.Action<CreatorBalanceResponse> onComplete)
    {
        CreatorBalanceRequest request = new CreatorBalanceRequest
        {
            creatorId = creatorId,
            deviceId = deviceId
        };

        string jsonPayload = JsonUtility.ToJson(request);

        if (debugMode)
            Debug.Log($"[GameEconomy] Verificando saldo:\n{jsonPayload}");

        yield return StartCoroutine(SendRequestWithRetry(
            "/api/creator/balance",
            jsonPayload,
            (success, response) =>
            {
                if (success)
                {
                    try
                    {
                        CreatorBalanceResponse parsedResponse = JsonUtility.FromJson<CreatorBalanceResponse>(response);

                        if (debugMode)
                            Debug.Log($"[GameEconomy] Saldo: {parsedResponse.balance} moedas");

                        onComplete?.Invoke(parsedResponse);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[GameEconomy] Erro ao parsear saldo: {ex.Message}");
                        onComplete?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[GameEconomy] Falha ao verificar saldo: {response}");
                    onComplete?.Invoke(null);
                }
            }
        ));
    }

    private IEnumerator SendRequestWithRetry(
        string endpoint,
        string jsonPayload,
        System.Action<bool, string> onComplete,
        int retryCount = 0)
    {
        string url = apiBaseUrl + endpoint;
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadRawData(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("User-Agent", $"GameEconomy/1.0 (Unity {Application.unityVersion})");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(true, request.downloadHandler.text);
            }
            else if (retryCount < MaxRetries)
            {
                if (debugMode)
                    Debug.LogWarning($"[GameEconomy] Tentativa {retryCount + 1}/{MaxRetries} falhou. Retentando em {RetryDelaySeconds}s...");

                yield return new WaitForSeconds(RetryDelaySeconds);
                yield return StartCoroutine(SendRequestWithRetry(endpoint, jsonPayload, onComplete, retryCount + 1));
            }
            else
            {
                string errorMessage = $"HTTP {request.responseCode}: {request.error}";
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                    errorMessage += $"\n{request.downloadHandler.text}";

                onComplete?.Invoke(false, errorMessage);
            }
        }
    }

    public string GetDeviceId()
    {
        return deviceId;
    }

    public bool IsCreatorDevice()
    {
        return deviceId == creatorId;
    }

    public void SetCreatorId(string id)
    {
        creatorId = id;
        if (debugMode)
            Debug.Log($"[GameEconomy] Creator ID configurado: {creatorId}");
    }

    [System.Serializable]
    public class AdWatchRequest
    {
        public string creatorId;
        public string deviceId;
        public string adType;
        public string timestamp;
    }

    [System.Serializable]
    public class AdWatchResponse
    {
        public bool success;
        public string message;
        public int coinsEarned;
        public int newBalance;
        public bool isCreatorDevice;
    }

    [System.Serializable]
    public class CreatorBalanceRequest
    {
        public string creatorId;
        public string deviceId;
    }

    [System.Serializable]
    public class CreatorBalanceResponse
    {
        public bool success;
        public string creatorId;
        public int balance;
        public int totalEarned;
        public int adWatchCount;
    }
}
