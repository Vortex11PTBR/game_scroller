using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EconomyUIController : MonoBehaviour
{
    [SerializeField] private Text balanceText;
    [SerializeField] private Text balanceFormattedText;
    [SerializeField] private Image coinIcon;
    [SerializeField] private CoinAnimator coinAnimator;
    [SerializeField] private float updateAnimationDuration = 0.5f;
    [SerializeField] private bool useCompactFormat = true;
    [SerializeField] private bool debugMode = false;

    private int currentBalance = 0;
    private Coroutine updateBalanceCoroutine;

    public delegate void OnBalanceChanged(int newBalance, int previousBalance);
    public event OnBalanceChanged BalanceChanged;

    private void OnEnable()
    {
        if (coinAnimator == null)
            coinAnimator = FindObjectOfType<CoinAnimator>();

        UpdateBalanceDisplay();
    }

    public void SetBalance(int newBalance, bool animate = false)
    {
        int previousBalance = currentBalance;
        currentBalance = newBalance;

        if (updateBalanceCoroutine != null)
            StopCoroutine(updateBalanceCoroutine);

        if (animate && newBalance > previousBalance)
        {
            updateBalanceCoroutine = StartCoroutine(AnimateBalanceUpdate(previousBalance, newBalance));
        }
        else
        {
            UpdateBalanceDisplay();
        }

        BalanceChanged?.Invoke(newBalance, previousBalance);

        if (debugMode)
            Debug.Log($"[EconomyUIController] Saldo atualizado: {previousBalance} → {newBalance}");
    }

    public void AddBalance(int amount, Vector3 sourceWorldPos = default)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[EconomyUIController] Valor deve ser maior que zero");
            return;
        }

        int previousBalance = currentBalance;
        int newBalance = currentBalance + amount;

        SetBalance(newBalance, true);

        if (sourceWorldPos != default && coinAnimator != null)
        {
            Vector3 targetWorldPos = balanceText?.rectTransform?.position ?? transform.position;
            coinAnimator.AnimateCoinsFlying(
                sourceWorldPos,
                targetWorldPos,
                Mathf.Min(amount / 10, 10),
                () =>
                {
                    if (debugMode)
                        Debug.Log($"[EconomyUIController] Animação de moedas concluída");
                }
            );
        }
    }

    public void RemoveBalance(int amount, bool animate = false)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("[EconomyUIController] Valor deve ser maior que zero");
            return;
        }

        int newBalance = Mathf.Max(0, currentBalance - amount);
        SetBalance(newBalance, animate);
    }

    public int GetCurrentBalance()
    {
        return currentBalance;
    }

    private IEnumerator AnimateBalanceUpdate(int fromBalance, int toBalance)
    {
        float elapsedTime = 0f;
        float duration = updateAnimationDuration;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            int animatedBalance = Mathf.RoundToInt(Mathf.Lerp(fromBalance, toBalance, progress));

            if (balanceText != null)
                balanceText.text = animatedBalance.ToString();

            if (balanceFormattedText != null)
                balanceFormattedText.text = NumberFormatter.FormatCurrency(animatedBalance);

            yield return null;
        }

        UpdateBalanceDisplay();
    }

    private void UpdateBalanceDisplay()
    {
        if (balanceText != null)
            balanceText.text = currentBalance.ToString();

        if (balanceFormattedText != null)
        {
            if (useCompactFormat)
                balanceFormattedText.text = NumberFormatter.FormatCurrency(currentBalance);
            else
                balanceFormattedText.text = NumberFormatter.FormatWithSeparator(currentBalance);
        }

        if (debugMode)
            Debug.Log($"[EconomyUIController] Display atualizado: {currentBalance}");
    }

    public void SetBalanceTextFormat(bool useCompact)
    {
        useCompactFormat = useCompact;
        UpdateBalanceDisplay();
    }

    private void OnDestroy()
    {
        if (updateBalanceCoroutine != null)
            StopCoroutine(updateBalanceCoroutine);
    }
}
