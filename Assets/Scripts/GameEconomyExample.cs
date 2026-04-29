using UnityEngine;

public class GameEconomyExample : MonoBehaviour
{
    [SerializeField] private GameEconomy gameEconomy;
    [SerializeField] private Text balanceText;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button checkBalanceButton;

    private int currentBalance = 0;

    private void OnEnable()
    {
        if (gameEconomy == null)
            gameEconomy = GetComponent<GameEconomy>();

        gameEconomy.CoinsUpdated += OnCoinsUpdated;
        gameEconomy.RequestFailed += OnRequestFailed;

        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAdPressed);

        if (checkBalanceButton != null)
            checkBalanceButton.onClick.AddListener(OnCheckBalancePressed);

        UpdateBalanceUI();
    }

    private void OnDisable()
    {
        if (gameEconomy != null)
        {
            gameEconomy.CoinsUpdated -= OnCoinsUpdated;
            gameEconomy.RequestFailed -= OnRequestFailed;
        }

        if (watchAdButton != null)
            watchAdButton.onClick.RemoveListener(OnWatchAdPressed);

        if (checkBalanceButton != null)
            checkBalanceButton.onClick.RemoveListener(OnCheckBalancePressed);
    }

    private void OnWatchAdPressed()
    {
        watchAdButton.interactable = false;

        gameEconomy.WatchRewardedAd("banner", (success) =>
        {
            watchAdButton.interactable = true;
            if (!success)
                Debug.LogError("Falha ao assistir anúncio");
        });
    }

    private void OnCheckBalancePressed()
    {
        checkBalanceButton.interactable = false;

        gameEconomy.CheckCreatorBalance((response) =>
        {
            checkBalanceButton.interactable = true;

            if (response != null && response.success)
            {
                currentBalance = response.balance;
                UpdateBalanceUI();
                Debug.Log($"Saldo: {response.balance} moedas | Total ganho: {response.totalEarned}");
            }
            else
            {
                Debug.LogError("Falha ao verificar saldo");
            }
        });
    }

    private void OnCoinsUpdated(int newBalance, int coinsEarned)
    {
        currentBalance = newBalance;
        UpdateBalanceUI();
        Debug.Log($"Moedas atualizadas! +{coinsEarned} moedas");
    }

    private void OnRequestFailed(string error)
    {
        Debug.LogError($"Erro da API: {error}");
    }

    private void UpdateBalanceUI()
    {
        if (balanceText != null)
            balanceText.text = $"Moedas: {currentBalance}";
    }

    public void SetCreatorId(string id)
    {
        gameEconomy.SetCreatorId(id);
    }

    public bool IsCreatorDevice()
    {
        return gameEconomy.IsCreatorDevice();
    }

    public string GetDeviceId()
    {
        return gameEconomy.GetDeviceId();
    }
}
