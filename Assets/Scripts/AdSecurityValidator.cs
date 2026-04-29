using System;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

public class AdSecurityValidator : MonoBehaviour
{
    [SerializeField] private int dailyAdLimit = 50;
    [SerializeField] private string encryptionKey = "default_key_change_this";
    [SerializeField] private bool debugMode = false;

    private const string AdCountKey = "AdSecurity.DailyCount";
    private const string LastResetDateKey = "AdSecurity.LastResetDate";
    private const int AES_KEY_SIZE = 256;
    private const int AES_IV_SIZE = 128;

    private int currentDailyCount = 0;
    private DateTime lastResetDate = DateTime.MinValue;

    private void Awake()
    {
        InitializeEncryption();
        LoadEncryptedAdCount();
    }

    private void InitializeEncryption()
    {
        if (string.IsNullOrEmpty(encryptionKey) || encryptionKey == "default_key_change_this")
        {
            Debug.LogError("[AdSecurityValidator] AVISO: Chave de criptografia padrão detectada! Altere 'encryptionKey' no Inspector para uma chave única e segura.");
        }
    }

    private void LoadEncryptedAdCount()
    {
        string lastResetStr = PlayerPrefs.GetString(LastResetDateKey, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        DateTime lastReset = DateTime.Parse(lastResetStr);

        if (lastReset.Date != DateTime.UtcNow.Date)
        {
            currentDailyCount = 0;
            lastResetDate = DateTime.UtcNow;
            SaveEncryptedAdCount();

            if (debugMode)
                Debug.Log($"[AdSecurityValidator] Reset da contagem diária realizado");
        }
        else
        {
            string encryptedCount = PlayerPrefs.GetString(AdCountKey, "");
            if (!string.IsNullOrEmpty(encryptedCount))
            {
                try
                {
                    string decrypted = DecryptData(encryptedCount);
                    if (int.TryParse(decrypted, out int count))
                    {
                        currentDailyCount = Mathf.Clamp(count, 0, dailyAdLimit);
                        lastResetDate = lastReset;

                        if (debugMode)
                            Debug.Log($"[AdSecurityValidator] Contagem carregada: {currentDailyCount}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AdSecurityValidator] Erro ao descriptografar contagem: {ex.Message}");
                    currentDailyCount = 0;
                }
            }
        }
    }

    public bool CanShowAd()
    {
        RefreshDailyCount();

        bool canShow = currentDailyCount < dailyAdLimit;

        if (debugMode)
            Debug.Log($"[AdSecurityValidator] Pode exibir anúncio? {canShow} ({currentDailyCount}/{dailyAdLimit})");

        return canShow;
    }

    public bool TryRecordAdView()
    {
        if (!CanShowAd())
        {
            Debug.LogWarning($"[AdSecurityValidator] Limite diário de anúncios atingido ({currentDailyCount}/{dailyAdLimit})");
            return false;
        }

        currentDailyCount++;
        SaveEncryptedAdCount();

        if (debugMode)
            Debug.Log($"[AdSecurityValidator] Anúncio registrado. Contagem: {currentDailyCount}/{dailyAdLimit}");

        return true;
    }

    public AdValidationHash GenerateValidationHash(string userId, string gameId, string secretSalt)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[AdSecurityValidator] userId não pode ser vazio");
            return null;
        }

        if (string.IsNullOrEmpty(gameId))
        {
            Debug.LogError("[AdSecurityValidator] gameId não pode ser vazio");
            return null;
        }

        if (string.IsNullOrEmpty(secretSalt))
        {
            Debug.LogError("[AdSecurityValidator] secretSalt não pode ser vazio");
            return null;
        }

        try
        {
            string combinedData = $"{userId}:{gameId}:{secretSalt}:{DateTime.UtcNow:O}";
            string hash = GenerateHMACSHA256(combinedData, encryptionKey);

            AdValidationHash validationHash = new AdValidationHash
            {
                userId = userId,
                gameId = gameId,
                timestamp = DateTime.UtcNow.ToString("O"),
                validationHash = hash,
                adCount = currentDailyCount,
                dailyLimit = dailyAdLimit
            };

            if (debugMode)
                Debug.Log($"[AdSecurityValidator] Hash gerado: {hash.Substring(0, 16)}...");

            return validationHash;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AdSecurityValidator] Erro ao gerar hash: {ex.Message}");
            return null;
        }
    }

    public bool ValidateHash(AdValidationHash hash, string secretSalt)
    {
        if (hash == null)
        {
            Debug.LogError("[AdSecurityValidator] Hash é nulo");
            return false;
        }

        try
        {
            string combinedData = $"{hash.userId}:{hash.gameId}:{secretSalt}:{hash.timestamp}";
            string expectedHash = GenerateHMACSHA256(combinedData, encryptionKey);

            bool isValid = hash.validationHash == expectedHash;

            if (debugMode)
                Debug.Log($"[AdSecurityValidator] Validação de hash: {(isValid ? "✓ Válido" : "✗ Inválido")}");

            return isValid;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AdSecurityValidator] Erro ao validar hash: {ex.Message}");
            return false;
        }
    }

    public int GetCurrentDailyCount()
    {
        RefreshDailyCount();
        return currentDailyCount;
    }

    public int GetRemainingAds()
    {
        RefreshDailyCount();
        return Mathf.Max(0, dailyAdLimit - currentDailyCount);
    }

    public float GetDailyProgress()
    {
        RefreshDailyCount();
        return dailyAdLimit > 0 ? (float)currentDailyCount / dailyAdLimit : 0f;
    }

    public void SetDailyLimit(int newLimit)
    {
        if (newLimit > 0)
        {
            dailyAdLimit = newLimit;

            if (debugMode)
                Debug.Log($"[AdSecurityValidator] Limite diário atualizado: {newLimit}");
        }
        else
        {
            Debug.LogError("[AdSecurityValidator] Limite diário deve ser maior que zero");
        }
    }

    public void ResetDailyCount()
    {
        currentDailyCount = 0;
        lastResetDate = DateTime.UtcNow;
        SaveEncryptedAdCount();

        if (debugMode)
            Debug.Log("[AdSecurityValidator] Contagem diária resetada");
    }

    private void RefreshDailyCount()
    {
        if (DateTime.UtcNow.Date > lastResetDate.Date)
        {
            ResetDailyCount();
        }
    }

    private void SaveEncryptedAdCount()
    {
        try
        {
            string encrypted = EncryptData(currentDailyCount.ToString());
            PlayerPrefs.SetString(AdCountKey, encrypted);
            PlayerPrefs.SetString(LastResetDateKey, DateTime.UtcNow.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();

            if (debugMode)
                Debug.Log($"[AdSecurityValidator] Contagem salva (criptografada)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AdSecurityValidator] Erro ao salvar contagem criptografada: {ex.Message}");
        }
    }

    private string EncryptData(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.KeySize = AES_KEY_SIZE;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(encryptionKey)))
            {
                aes.Key = hmac.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
            }

            aes.GenerateIV();

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                byte[] resultBytes = new byte[aes.IV.Length + encryptedBytes.Length];
                Buffer.BlockCopy(aes.IV, 0, resultBytes, 0, aes.IV.Length);
                Buffer.BlockCopy(encryptedBytes, 0, resultBytes, aes.IV.Length, encryptedBytes.Length);

                return Convert.ToBase64String(resultBytes);
            }
        }
    }

    private string DecryptData(string cipherText)
    {
        using (Aes aes = Aes.Create())
        {
            aes.KeySize = AES_KEY_SIZE;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(encryptionKey)))
            {
                aes.Key = hmac.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
            }

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] iv = new byte[AES_IV_SIZE / 8];
            Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);

            aes.IV = iv;

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                byte[] encryptedBytes = new byte[cipherBytes.Length - iv.Length];
                Buffer.BlockCopy(cipherBytes, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }

    private string GenerateHMACSHA256(string data, string key)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash);
        }
    }
}
