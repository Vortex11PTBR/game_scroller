using System;
using UnityEngine;

[System.Serializable]
public class AdValidationHash
{
    public string userId;
    public string gameId;
    public string timestamp;
    public string validationHash;
    public int adCount;
    public int dailyLimit;

    public bool IsExpired(int maxAgeSeconds = 300)
    {
        if (!DateTime.TryParse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime hashTime))
            return true;

        return (DateTime.UtcNow - hashTime).TotalSeconds > maxAgeSeconds;
    }

    public override string ToString()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static AdValidationHash FromJson(string json)
    {
        try
        {
            return JsonUtility.FromJson<AdValidationHash>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AdValidationHash] Erro ao desserializar JSON: {ex.Message}");
            return null;
        }
    }
}
