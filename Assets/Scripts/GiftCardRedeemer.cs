using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GiftCardRedeemer : MonoBehaviour
{
    [SerializeField] private EconomyUIController economyUI;
    [SerializeField] private Button redeemButton;
    [SerializeField] private Image redeemButtonImage;
    [SerializeField] private Text redeemButtonText;
    [SerializeField] private Text redeemStatusText;
    [SerializeField] private int minimumBalanceRequired = 5000;
    [SerializeField] private int giftCardValue = 0;
    [SerializeField] private Color enabledButtonColor = Color.white;
    [SerializeField] private Color disabledButtonColor = Color.gray;
    [SerializeField] private float statusDisplayDuration = 3f;
    [SerializeField] private bool debugMode = false;

    private bool isProcessing = false;
    private Coroutine statusCoroutine;

    public delegate void OnGiftCardRedeemed(int giftCardValue, int newBalance);
    public delegate void OnRedeemFailed(string reason);

    public event OnGiftCardRedeemed GiftCardRedeemed;
    public event OnRedeemFailed RedeemFailed;

    private void OnEnable()
    {
        if (economyUI == null)
            economyUI = FindObjectOfType<EconomyUIController>();

        if (redeemButton != null)
        {
            redeemButton.onClick.AddListener(OnRedeemPressed);
            economyUI.BalanceChanged += OnEconomyBalanceChanged;
        }

        UpdateRedeemButtonState();
    }

    private void OnDisable()
    {
        if (redeemButton != null)
            redeemButton.onClick.RemoveListener(OnRedeemPressed);

        if (economyUI != null)
            economyUI.BalanceChanged -= OnEconomyBalanceChanged;

        if (statusCoroutine != null)
            StopCoroutine(statusCoroutine);
    }

    private void OnRedeemPressed()
    {
        if (isProcessing)
        {
            Debug.LogWarning("[GiftCardRedeemer] Já está processando uma requisição");
            return;
        }

        if (economyUI.GetCurrentBalance() < minimumBalanceRequired)
        {
            ShowStatus($"Saldo insuficiente! Mínimo: {NumberFormatter.FormatCurrency(minimumBalanceRequired)}", false);
            RedeemFailed?.Invoke("Saldo insuficiente");
            return;
        }

        StartCoroutine(ProcessGiftCardRedeem());
    }

    private IEnumerator ProcessGiftCardRedeem()
    {
        isProcessing = true;
        redeemButton.interactable = false;

        ShowStatus("Processando...", true);

        if (debugMode)
            Debug.Log("[GiftCardRedeemer] Iniciando resgate de gift card");

        // Simular requisição ao servidor
        yield return new WaitForSeconds(1f);

        int currentBalance = economyUI.GetCurrentBalance();
        int newBalance = currentBalance - minimumBalanceRequired;

        // Aqui você faria a chamada real ao servidor
        // bool success = await SendRedeemRequestToServer();
        bool success = true;

        if (success)
        {
            economyUI.SetBalance(newBalance, true);

            ShowStatus($"Gift Card resgatado! Valor: {NumberFormatter.FormatCurrency(giftCardValue > 0 ? giftCardValue : minimumBalanceRequired)}", true);

            GiftCardRedeemed?.Invoke(giftCardValue > 0 ? giftCardValue : minimumBalanceRequired, newBalance);

            if (debugMode)
                Debug.Log($"[GiftCardRedeemer] Gift Card resgatado com sucesso");
        }
        else
        {
            ShowStatus("Falha ao resgatar. Tente novamente.", false);
            RedeemFailed?.Invoke("Erro do servidor");
        }

        isProcessing = false;
        UpdateRedeemButtonState();
    }

    private void OnEconomyBalanceChanged(int newBalance, int previousBalance)
    {
        UpdateRedeemButtonState();
    }

    private void UpdateRedeemButtonState()
    {
        bool hasEnoughBalance = economyUI.GetCurrentBalance() >= minimumBalanceRequired;

        if (redeemButton != null)
        {
            redeemButton.interactable = hasEnoughBalance && !isProcessing;
        }

        if (redeemButtonImage != null)
        {
            redeemButtonImage.color = hasEnoughBalance ? enabledButtonColor : disabledButtonColor;
        }

        if (redeemButtonText != null)
        {
            string minRequired = NumberFormatter.FormatCurrency(minimumBalanceRequired);
            redeemButtonText.text = $"Resgatar\n({minRequired})";
        }

        if (debugMode)
            Debug.Log($"[GiftCardRedeemer] Botão estado: {(hasEnoughBalance ? "Habilitado" : "Desabilitado")}");
    }

    private void ShowStatus(string message, bool isSuccess)
    {
        if (redeemStatusText != null)
        {
            redeemStatusText.text = message;
            redeemStatusText.color = isSuccess ? Color.green : Color.red;
        }

        if (statusCoroutine != null)
            StopCoroutine(statusCoroutine);

        statusCoroutine = StartCoroutine(HideStatusAfterDelay());
    }

    private IEnumerator HideStatusAfterDelay()
    {
        yield return new WaitForSeconds(statusDisplayDuration);

        if (redeemStatusText != null)
        {
            redeemStatusText.text = "";
        }
    }

    public void SetMinimumBalance(int newMinimum)
    {
        minimumBalanceRequired = newMinimum;
        UpdateRedeemButtonState();

        if (debugMode)
            Debug.Log($"[GiftCardRedeemer] Saldo mínimo atualizado: {newMinimum}");
    }

    public int GetMinimumBalance()
    {
        return minimumBalanceRequired;
    }

    public bool CanRedeem()
    {
        return economyUI.GetCurrentBalance() >= minimumBalanceRequired && !isProcessing;
    }
}
