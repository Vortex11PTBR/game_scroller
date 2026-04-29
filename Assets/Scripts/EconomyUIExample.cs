using UnityEngine;
using UnityEngine.UI;

public class EconomyUIExample : MonoBehaviour
{
    [SerializeField] private EconomyUIController economyUI;
    [SerializeField] private GiftCardRedeemer giftCardRedeemer;
    [SerializeField] private Button addCoinsButton;
    [SerializeField] private Button removeCoinsButton;
    [SerializeField] private Button testAnimationButton;
    [SerializeField] private InputField balanceInputField;

    private void OnEnable()
    {
        if (economyUI == null)
            economyUI = FindObjectOfType<EconomyUIController>();

        if (giftCardRedeemer == null)
            giftCardRedeemer = FindObjectOfType<GiftCardRedeemer>();

        if (addCoinsButton != null)
            addCoinsButton.onClick.AddListener(OnAddCoinsPressed);

        if (removeCoinsButton != null)
            removeCoinsButton.onClick.AddListener(OnRemoveCoinsPressed);

        if (testAnimationButton != null)
            testAnimationButton.onClick.AddListener(OnTestAnimationPressed);

        economyUI.SetBalance(1000);
    }

    private void OnDisable()
    {
        if (addCoinsButton != null)
            addCoinsButton.onClick.RemoveListener(OnAddCoinsPressed);

        if (removeCoinsButton != null)
            removeCoinsButton.onClick.RemoveListener(OnRemoveCoinsPressed);

        if (testAnimationButton != null)
            testAnimationButton.onClick.RemoveListener(OnTestAnimationPressed);
    }

    private void OnAddCoinsPressed()
    {
        int amount = 500;

        if (balanceInputField != null && int.TryParse(balanceInputField.text, out int inputAmount))
            amount = inputAmount;

        economyUI.AddBalance(amount, transform.position);
        Debug.Log($"[EconomyUIExample] Adicionado {amount} moedas");
    }

    private void OnRemoveCoinsPressed()
    {
        int amount = 100;

        if (balanceInputField != null && int.TryParse(balanceInputField.text, out int inputAmount))
            amount = inputAmount;

        economyUI.RemoveBalance(amount);
        Debug.Log($"[EconomyUIExample] Removido {amount} moedas");
    }

    private void OnTestAnimationPressed()
    {
        // Simular moedas voando de um anúncio
        Vector3 adPosition = new Vector3(-5, 0, 0);  // Posição do anúncio
        Vector3 balancePosition = transform.position;

        economyUI.AddBalance(50, adPosition);
        Debug.Log("[EconomyUIExample] Testando animação de moedas");
    }
}
