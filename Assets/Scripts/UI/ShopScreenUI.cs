/*
 * ShopScreenUI.cs — Tela de loja de moedas e itens
 * Propósito: Exibe pacotes de moedas, vidas extras, remoção de anúncios e skins de avatar.
 * Como usar: Adicione ao painel de loja. Configure os dados dos pacotes no Inspector.
 * Dependências: PlayerProfileManager.cs, LifeSystem.cs, NotificationSystem.cs
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopScreenUI : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Pacotes de Moedas
    // -----------------------------------------------------------------------

    [Header("Container de Pacotes")]
    [SerializeField] private Transform coinPackagesContainer;
    [SerializeField] private GameObject coinPackagePrefab;

    [Header("Pacotes Padrão (configurar no Inspector)")]
    [SerializeField] private List<CoinPackageConfig> coinPackages = new List<CoinPackageConfig>
    {
        new CoinPackageConfig { label = "Iniciante", coins = 100,   priceDisplay = "R$ 1,99",  cardColor = new Color(0.3f, 0.7f, 0.9f) },
        new CoinPackageConfig { label = "Popular",   coins = 500,   priceDisplay = "R$ 4,99",  cardColor = new Color(0.3f, 0.8f, 0.3f), isBestValue = false },
        new CoinPackageConfig { label = "Gamer",     coins = 2000,  priceDisplay = "R$ 14,99", cardColor = new Color(0.9f, 0.6f, 0.1f), isBestValue = true },
        new CoinPackageConfig { label = "Lendário",  coins = 10000, priceDisplay = "R$ 49,99", cardColor = new Color(0.9f, 0.2f, 0.7f) }
    };

    // -----------------------------------------------------------------------
    // Itens da Loja
    // -----------------------------------------------------------------------

    [Header("Botões de Itens")]
    [SerializeField] private Button buyLivesButton;
    [SerializeField] private Text buyLivesText;
    [SerializeField] private int livesCost = 50;

    [SerializeField] private Button removeAdsButton;
    [SerializeField] private Text removeAdsText;
    [SerializeField] private int removeAdsCost = 500;

    [Header("Skins de Avatar")]
    [SerializeField] private Transform skinsContainer;
    [SerializeField] private GameObject skinButtonPrefab;
    [SerializeField] private Color[] avatarSkinColors;
    [SerializeField] private int skinCost = 200;

    // -----------------------------------------------------------------------
    // Referências
    // -----------------------------------------------------------------------

    [Header("Sistemas")]
    [SerializeField] private PlayerProfileManager profileManager;
    [SerializeField] private LifeSystem lifeSystem;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (profileManager == null)
            profileManager = GameManager.Instance?.ProfileManager ?? FindObjectOfType<PlayerProfileManager>();

        if (lifeSystem == null)
            lifeSystem = GameManager.Instance?.LifeSystem ?? FindObjectOfType<LifeSystem>();
    }

    private void Start()
    {
        BuildCoinPackages();
        BuildSkins();
        SetupItemButtons();
        RefreshItemButtonStates();

        GameManager.OnCoinsChanged += _ => RefreshItemButtonStates();
    }

    private void OnDestroy()
    {
        GameManager.OnCoinsChanged -= _ => RefreshItemButtonStates();
    }

    private void OnEnable()
    {
        RefreshItemButtonStates();
    }

    // -----------------------------------------------------------------------
    // Construção dinâmica de UI
    // -----------------------------------------------------------------------

    private void BuildCoinPackages()
    {
        if (coinPackagesContainer == null || coinPackagePrefab == null)
            return;

        foreach (Transform child in coinPackagesContainer)
            Destroy(child.gameObject);

        foreach (var pkg in coinPackages)
        {
            GameObject pkgGo = Instantiate(coinPackagePrefab, coinPackagesContainer);

            // Cor do card
            Image cardImage = pkgGo.GetComponent<Image>();
            if (cardImage != null) cardImage.color = pkg.cardColor;

            // Textos
            Text[] texts = pkgGo.GetComponentsInChildren<Text>();
            if (texts.Length > 0) texts[0].text = $"{pkg.coins:N0} moedas";
            if (texts.Length > 1) texts[1].text = pkg.label;
            if (texts.Length > 2) texts[2].text = pkg.priceDisplay;

            // Botão de compra
            Button btn = pkgGo.GetComponent<Button>();
            if (btn != null)
            {
                var capturedPkg = pkg;
                btn.onClick.AddListener(() => OnBuyCoinPackagePressed(capturedPkg));
            }

            // Badge "Melhor Custo"
            Transform badge = pkgGo.transform.Find("BestValueBadge");
            if (badge != null) badge.gameObject.SetActive(pkg.isBestValue);
        }
    }

    private void BuildSkins()
    {
        if (skinsContainer == null || skinButtonPrefab == null || avatarSkinColors == null)
            return;

        foreach (Transform child in skinsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < avatarSkinColors.Length; i++)
        {
            GameObject skinGo = Instantiate(skinButtonPrefab, skinsContainer);

            Image skinImg = skinGo.GetComponent<Image>();
            if (skinImg != null) skinImg.color = avatarSkinColors[i];

            Button skinBtn = skinGo.GetComponent<Button>();
            if (skinBtn != null)
            {
                int capturedIndex = i;
                skinBtn.onClick.AddListener(() => OnBuySkinPressed(capturedIndex));
            }
        }
    }

    private void SetupItemButtons()
    {
        if (buyLivesButton != null)
            buyLivesButton.onClick.AddListener(OnBuyLivesPressed);

        if (removeAdsButton != null)
            removeAdsButton.onClick.AddListener(OnRemoveAdsPressed);

        if (buyLivesText != null)
            buyLivesText.text = $"+5 Vidas\n{livesCost} 🪙";

        if (removeAdsText != null)
            removeAdsText.text = $"Remover Anúncios\n{removeAdsCost} 🪙";
    }

    // -----------------------------------------------------------------------
    // Ações de compra
    // -----------------------------------------------------------------------

    private void OnBuyCoinPackagePressed(CoinPackageConfig pkg)
    {
        // Em produção: chamar loja de plataforma (Google Play / App Store)
        // Por enquanto: simula compra bem-sucedida
        profileManager?.AddCoins(pkg.coins);
        NotificationSystem.Show($"+{pkg.coins:N0} moedas adicionadas! 🪙", NotificationSystem.NotificationType.Sucesso);
        Debug.Log($"[ShopScreenUI] Pacote comprado: {pkg.label} ({pkg.coins} moedas)");
    }

    private void OnBuyLivesPressed()
    {
        if (profileManager == null)
            return;

        if (profileManager.SpendCoins(livesCost))
        {
            lifeSystem?.AddRewardLives(5);
            GameManager.NotifyLivesChanged(lifeSystem?.CurrentLives ?? 0);
            NotificationSystem.Show("+5 Vidas! ❤❤❤❤❤", NotificationSystem.NotificationType.Sucesso);
        }
        else
        {
            NotificationSystem.Show("Moedas insuficientes!", NotificationSystem.NotificationType.Erro);
        }
    }

    private void OnRemoveAdsPressed()
    {
        if (profileManager == null)
            return;

        if (profileManager.CurrentProfile.adsRemoved)
        {
            NotificationSystem.Show("Anúncios já removidos!", NotificationSystem.NotificationType.Info);
            return;
        }

        if (profileManager.SpendCoins(removeAdsCost))
        {
            profileManager.CurrentProfile.adsRemoved = true;
            profileManager.SaveProfile();
            NotificationSystem.Show("Anúncios removidos! ✨", NotificationSystem.NotificationType.Sucesso);
            RefreshItemButtonStates();
        }
        else
        {
            NotificationSystem.Show("Moedas insuficientes!", NotificationSystem.NotificationType.Erro);
        }
    }

    private void OnBuySkinPressed(int colorIndex)
    {
        if (profileManager == null)
            return;

        if (profileManager.SpendCoins(skinCost))
        {
            profileManager.UpdateAvatarColor(colorIndex);
            NotificationSystem.Show("Skin equipada! 🎨", NotificationSystem.NotificationType.Sucesso);
        }
        else
        {
            NotificationSystem.Show("Moedas insuficientes!", NotificationSystem.NotificationType.Erro);
        }
    }

    // -----------------------------------------------------------------------
    // Estado dos botões
    // -----------------------------------------------------------------------

    private void RefreshItemButtonStates()
    {
        int coins = profileManager?.CurrentProfile?.coins ?? 0;

        if (buyLivesButton != null)
            buyLivesButton.interactable = coins >= livesCost;

        if (removeAdsButton != null)
        {
            bool adsRemoved = profileManager?.CurrentProfile?.adsRemoved ?? false;
            removeAdsButton.interactable = !adsRemoved && coins >= removeAdsCost;
            if (removeAdsText != null)
                removeAdsText.text = adsRemoved
                    ? "Anúncios Removidos ✓"
                    : $"Remover Anúncios\n{removeAdsCost} 🪙";
        }
    }

    // -----------------------------------------------------------------------
    // Classe de configuração de pacote
    // -----------------------------------------------------------------------

    [System.Serializable]
    public class CoinPackageConfig
    {
        public string label;
        public int coins;
        public string priceDisplay;
        public Color cardColor = Color.white;
        public bool isBestValue;
    }
}
