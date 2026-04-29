using UnityEngine;
using System.Collections;

public class AdSecurityExample : MonoBehaviour
{
    [SerializeField] private AdSecurityValidator adSecurityValidator;
    [SerializeField] private string userId = "user_12345";
    [SerializeField] private string gameId = "game_jumping";
    [SerializeField] private string serverSecretSalt = "server_secret_salt_123";
    [SerializeField] private Text adCountText;
    [SerializeField] private Button showAdButton;
    [SerializeField] private Button validateHashButton;

    private void OnEnable()
    {
        if (adSecurityValidator == null)
            adSecurityValidator = GetComponent<AdSecurityValidator>();

        if (showAdButton != null)
            showAdButton.onClick.AddListener(OnShowAdPressed);

        if (validateHashButton != null)
            validateHashButton.onClick.AddListener(OnValidateHashPressed);

        UpdateAdCountUI();
    }

    private void OnDisable()
    {
        if (showAdButton != null)
            showAdButton.onClick.RemoveListener(OnShowAdPressed);

        if (validateHashButton != null)
            validateHashButton.onClick.RemoveListener(OnValidateHashPressed);
    }

    private void OnShowAdPressed()
    {
        if (!adSecurityValidator.CanShowAd())
        {
            Debug.LogWarning($"Limite diário de anúncios atingido!");
            return;
        }

        if (adSecurityValidator.TryRecordAdView())
        {
            Debug.Log("Anúncio exibido com sucesso");
            UpdateAdCountUI();

            GenerateAndSendValidationHash();
        }
        else
        {
            Debug.LogError("Falha ao registrar visualização do anúncio");
        }
    }

    private void OnValidateHashPressed()
    {
        AdValidationHash hash = adSecurityValidator.GenerateValidationHash(
            userId,
            gameId,
            serverSecretSalt
        );

        if (hash != null)
        {
            Debug.Log($"Hash gerado:\n{hash.ToString()}");

            bool isValid = adSecurityValidator.ValidateHash(hash, serverSecretSalt);
            Debug.Log($"Hash válido? {isValid}");
        }
    }

    private void GenerateAndSendValidationHash()
    {
        AdValidationHash validationHash = adSecurityValidator.GenerateValidationHash(
            userId,
            gameId,
            serverSecretSalt
        );

        if (validationHash != null)
        {
            StartCoroutine(SendHashToServer(validationHash));
        }
    }

    private IEnumerator SendHashToServer(AdValidationHash validationHash)
    {
        string jsonPayload = validationHash.ToString();
        Debug.Log($"[AdSecurityExample] Enviando hash ao servidor:\n{jsonPayload}");

        // Exemplo com UnityWebRequest (implementar conforme sua API)
        // yield return StartCoroutine(ServerApi.SendAdValidation(jsonPayload));

        // Para este exemplo, apenas simulamos
        yield return new WaitForSeconds(1f);
        Debug.Log("[AdSecurityExample] Hash enviado com sucesso ao servidor");
    }

    private void UpdateAdCountUI()
    {
        int remaining = adSecurityValidator.GetRemainingAds();
        int total = adSecurityValidator.GetCurrentDailyCount();
        float progress = adSecurityValidator.GetDailyProgress();

        if (adCountText != null)
            adCountText.text = $"Anúncios: {total} | Restantes: {remaining}\nProgresso: {progress:P1}";

        if (showAdButton != null)
            showAdButton.interactable = adSecurityValidator.CanShowAd();
    }
}
