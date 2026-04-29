/*
 * ShopScreenUI.cs — Tela de loja com integração Unity IAP (In-App Purchasing)
 * Propósito: Exibe pacotes de moedas, vidas extras, remoção de anúncios e skins de avatar.
 *            Usa Unity IAP para compras reais no Google Play. Em modo debug, simula compras.
 * Como usar: Adicione ao painel de loja. Configure as referências no Inspector.
 * Dependências: PlayerProfileManager.cs, LifeSystem.cs, NotificationSystem.cs
 *
 * Para habilitar compras reais:
 *   1. Instale o package com.unity.purchasing via Package Manager
 *   2. Ative em Services > In-App Purchasing no Unity Dashboard
 *   3. Configure os produtos no Google Play Console com os mesmos IDs abaixo
 *   4. O símbolo UNITY_PURCHASING será definido automaticamente
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

public class ShopScreenUI : MonoBehaviour
#if UNITY_PURCHASING
    , IStoreListener
#endif
{
    // -----------------------------------------------------------------------
    // IDs de Produto (devem coincidir com os cadastrados no Google Play Console)
    // -----------------------------------------------------------------------

    private const string ProductCoins100    = "coins_100";    // R$ 1,99 — 100 moedas
    private const string ProductCoins500    = "coins_500";    // R$ 4,99 — 500 moedas
    private const string ProductCoins2000   = "coins_2000";   // R$ 14,99 — 2000 moedas
    private const string ProductCoins10000  = "coins_10000";  // R$ 49,99 — 10000 moedas
    private const string ProductRemoveAds   = "remove_ads";   // R$ 9,99 — remove anúncios (permanente)
    private const string ProductStarterPack = "starter_pack"; // R$ 2,99 — pack iniciante (200 moedas + 5 vidas)

    // Chaves de PlayerPrefs para itens permanentes
    private const string PrefKeyRemoveAds      = "remove_ads";
    private const string PrefKeyStarterPack    = "starter_pack_purchased";

    // -----------------------------------------------------------------------
    // Pacotes de Moedas (configuração de UI)
    // -----------------------------------------------------------------------

    [Header("Container de Pacotes")]
    [SerializeField] private Transform coinPackagesContainer;
    [SerializeField] private GameObject coinPackagePrefab;

    [Header("Pacotes Padrão (preços reais vêm do Google Play via IAP)")]
    [SerializeField] private List<CoinPackageConfig> coinPackages = new List<CoinPackageConfig>
    {
        new CoinPackageConfig { label = "Iniciante",  coins = 100,   productId = ProductCoins100,   priceDisplay = "R$ 1,99",  cardColor = new Color(0.3f, 0.7f, 0.9f) },
        new CoinPackageConfig { label = "Popular",    coins = 500,   productId = ProductCoins500,   priceDisplay = "R$ 4,99",  cardColor = new Color(0.3f, 0.8f, 0.3f) },
        new CoinPackageConfig { label = "Gamer",      coins = 2000,  productId = ProductCoins2000,  priceDisplay = "R$ 14,99", cardColor = new Color(0.9f, 0.6f, 0.1f), isBestValue = true },
        new CoinPackageConfig { label = "Lendário",   coins = 10000, productId = ProductCoins10000, priceDisplay = "R$ 49,99", cardColor = new Color(0.9f, 0.2f, 0.7f) }
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

    [SerializeField] private Button starterPackButton;
    [SerializeField] private Text starterPackText;

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
    [SerializeField] private AdManager adManager;

    // -----------------------------------------------------------------------
    // Estado IAP
    // -----------------------------------------------------------------------

#if UNITY_PURCHASING
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;
#endif

    private bool isIapInitialized = false;
    private bool isPurchasePending = false;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (profileManager == null)
            profileManager = GameManager.Instance?.ProfileManager ?? FindObjectOfType<PlayerProfileManager>();

        if (lifeSystem == null)
            lifeSystem = GameManager.Instance?.LifeSystem ?? FindObjectOfType<LifeSystem>();

        if (adManager == null)
            adManager = GameManager.Instance?.AdManager ?? FindObjectOfType<AdManager>();
    }

    private void Start()
    {
        InitializeIAP();
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
    // Inicialização IAP
    // -----------------------------------------------------------------------

    private void InitializeIAP()
    {
#if UNITY_PURCHASING
        if (isIapInitialized)
            return;

        // Configura os produtos disponíveis para compra
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Produtos consumíveis (moedas — podem ser comprados várias vezes)
        builder.AddProduct(ProductCoins100,    ProductType.Consumable);
        builder.AddProduct(ProductCoins500,    ProductType.Consumable);
        builder.AddProduct(ProductCoins2000,   ProductType.Consumable);
        builder.AddProduct(ProductCoins10000,  ProductType.Consumable);
        builder.AddProduct(ProductStarterPack, ProductType.Consumable);

        // Produto não-consumível (remove ads — comprado apenas uma vez)
        builder.AddProduct(ProductRemoveAds, ProductType.NonConsumable);

        UnityPurchasing.Initialize(this, builder);
        Debug.Log("[ShopScreenUI] IAP inicializando...");
#else
        Debug.Log("[ShopScreenUI] Unity IAP não instalado. Usando modo de simulação.");
        isIapInitialized = true;
#endif
    }

    // -----------------------------------------------------------------------
    // Callbacks IStoreListener (Unity IAP)
    // -----------------------------------------------------------------------

#if UNITY_PURCHASING
    /// <summary>
    /// Chamado quando o IAP é inicializado com sucesso.
    /// </summary>
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
        isIapInitialized = true;

        Debug.Log("[ShopScreenUI] IAP inicializado com sucesso.");

        // Atualiza os preços reais da loja (vindos do Google Play)
        UpdatePricesFromStore();
        RefreshItemButtonStates();
    }

    /// <summary>
    /// Chamado quando o IAP falha ao inicializar.
    /// </summary>
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[ShopScreenUI] IAP falhou ao inicializar: {error}");
        isIapInitialized = false;
    }

    /// <summary>
    /// Chamado quando o IAP falha ao inicializar (com mensagem detalhada).
    /// </summary>
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[ShopScreenUI] IAP falhou ao inicializar: {error} — {message}");
        isIapInitialized = false;
    }

    /// <summary>
    /// Chamado quando uma compra é concluída com sucesso.
    /// </summary>
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        isPurchasePending = false;
        string productId = args.purchasedProduct.definition.id;

        Debug.Log($"[ShopScreenUI] Compra concluída: {productId}");

        switch (productId)
        {
            case ProductCoins100:
                GrantCoins(100);
                break;
            case ProductCoins500:
                GrantCoins(500);
                break;
            case ProductCoins2000:
                GrantCoins(2000);
                break;
            case ProductCoins10000:
                GrantCoins(10000);
                break;
            case ProductRemoveAds:
                GrantRemoveAds();
                break;
            case ProductStarterPack:
                GrantStarterPack();
                break;
            default:
                Debug.LogWarning($"[ShopScreenUI] Produto desconhecido: {productId}");
                break;
        }

        // Retorna Complete para confirmar a compra
        return PurchaseProcessingResult.Complete;
    }

    /// <summary>
    /// Chamado quando uma compra falha.
    /// </summary>
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        isPurchasePending = false;
        Debug.LogWarning($"[ShopScreenUI] Compra falhou: {product.definition.storeSpecificId} — {failureReason}");
        NotificationSystem.Show("Compra cancelada ou falhou.", NotificationSystem.NotificationType.Erro);
    }

    /// <summary>
    /// Atualiza os textos de preço exibidos na loja com os valores reais do Google Play.
    /// </summary>
    private void UpdatePricesFromStore()
    {
        if (storeController == null)
            return;

        foreach (var pkg in coinPackages)
        {
            var product = storeController.products.WithID(pkg.productId);
            if (product != null && product.availableToPurchase)
            {
                // Usa o preço localizado retornado pelo Google Play
                pkg.priceDisplay = product.metadata.localizedPriceString;
            }
        }

        // Atualiza preço de remove_ads
        var removeAdsProduct = storeController.products.WithID(ProductRemoveAds);
        if (removeAdsProduct != null && removeAdsProduct.availableToPurchase)
        {
            if (removeAdsText != null && !(profileManager?.CurrentProfile?.adsRemoved ?? false))
                removeAdsText.text = $"Remover Anúncios\n{removeAdsProduct.metadata.localizedPriceString}";
        }

        // Atualiza preço do starter pack
        var starterProduct = storeController.products.WithID(ProductStarterPack);
        if (starterProduct != null && starterProduct.availableToPurchase)
        {
            if (starterPackText != null)
                starterPackText.text = $"Pack Iniciante\n200🪙 + 5❤\n{starterProduct.metadata.localizedPriceString}";
        }

        // Reconstrói os cards com preços atualizados
        BuildCoinPackages();
    }
#endif

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

        if (starterPackButton != null)
            starterPackButton.onClick.AddListener(OnBuyStarterPackPressed);

        if (buyLivesText != null)
            buyLivesText.text = $"+5 Vidas\n{livesCost} 🪙";

        if (removeAdsText != null)
            removeAdsText.text = "Remover Anúncios\nR$ 9,99";

        if (starterPackText != null)
            starterPackText.text = "Pack Iniciante\n200🪙 + 5❤\nR$ 2,99";
    }

    // -----------------------------------------------------------------------
    // Ações de compra via IAP
    // -----------------------------------------------------------------------

    private void OnBuyCoinPackagePressed(CoinPackageConfig pkg)
    {
#if UNITY_PURCHASING
        if (!isIapInitialized || storeController == null)
        {
            Debug.LogWarning("[ShopScreenUI] IAP não inicializado. Tentando novamente...");
            InitializeIAP();
            return;
        }

        if (isPurchasePending)
        {
            NotificationSystem.Show("Aguarde a compra anterior.", NotificationSystem.NotificationType.Info);
            return;
        }

        var product = storeController.products.WithID(pkg.productId);
        if (product != null && product.availableToPurchase)
        {
            isPurchasePending = true;
            storeController.InitiatePurchase(product);
            Debug.Log($"[ShopScreenUI] Iniciando compra IAP: {pkg.productId}");
        }
        else
        {
            Debug.LogError($"[ShopScreenUI] Produto indisponível: {pkg.productId}");
            NotificationSystem.Show("Produto indisponível.", NotificationSystem.NotificationType.Erro);
        }
#else
        // Modo debug/editor: simula compra com delay de 1s
        StartCoroutine(SimulatePurchase(pkg.coins, pkg.label));
#endif
    }

    private void OnRemoveAdsPressed()
    {
        if (profileManager?.CurrentProfile?.adsRemoved ?? false)
        {
            NotificationSystem.Show("Anúncios já removidos!", NotificationSystem.NotificationType.Info);
            return;
        }

#if UNITY_PURCHASING
        if (!isIapInitialized || storeController == null)
        {
            InitializeIAP();
            return;
        }

        if (isPurchasePending)
        {
            NotificationSystem.Show("Aguarde a compra anterior.", NotificationSystem.NotificationType.Info);
            return;
        }

        var product = storeController.products.WithID(ProductRemoveAds);
        if (product != null && product.availableToPurchase)
        {
            isPurchasePending = true;
            storeController.InitiatePurchase(product);
            Debug.Log("[ShopScreenUI] Iniciando compra IAP: remove_ads");
        }
        else
        {
            Debug.LogError("[ShopScreenUI] Produto remove_ads indisponível.");
            NotificationSystem.Show("Produto indisponível.", NotificationSystem.NotificationType.Erro);
        }
#else
        // Simulação em modo debug
        StartCoroutine(SimulateRemoveAds());
#endif
    }

    private void OnBuyStarterPackPressed()
    {
        // Verifica se já foi comprado (starter pack é vendido apenas uma vez)
        if (PlayerPrefs.GetInt(PrefKeyStarterPack, 0) == 1)
        {
            NotificationSystem.Show("Pack já adquirido!", NotificationSystem.NotificationType.Info);
            return;
        }

#if UNITY_PURCHASING
        if (!isIapInitialized || storeController == null)
        {
            InitializeIAP();
            return;
        }

        if (isPurchasePending)
        {
            NotificationSystem.Show("Aguarde a compra anterior.", NotificationSystem.NotificationType.Info);
            return;
        }

        var product = storeController.products.WithID(ProductStarterPack);
        if (product != null && product.availableToPurchase)
        {
            isPurchasePending = true;
            storeController.InitiatePurchase(product);
            Debug.Log("[ShopScreenUI] Iniciando compra IAP: starter_pack");
        }
        else
        {
            Debug.LogError("[ShopScreenUI] Produto starter_pack indisponível.");
            NotificationSystem.Show("Produto indisponível.", NotificationSystem.NotificationType.Erro);
        }
#else
        StartCoroutine(SimulateStarterPack());
#endif
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
    // Concessão de recompensas (usadas tanto no IAP real quanto no debug)
    // -----------------------------------------------------------------------

    private void GrantCoins(int amount)
    {
        profileManager?.AddCoins(amount);
        NotificationSystem.Show($"+{amount:N0} moedas adicionadas! 🪙", NotificationSystem.NotificationType.Sucesso);
        Debug.Log($"[ShopScreenUI] {amount} moedas concedidas.");
    }

    private void GrantRemoveAds()
    {
        // Salva a flag de remoção de anúncios permanentemente
        PlayerPrefs.SetInt(PrefKeyRemoveAds, 1);
        PlayerPrefs.Save();

        if (profileManager != null)
        {
            profileManager.CurrentProfile.adsRemoved = true;
            profileManager.SaveProfile();
        }

        // Esconde banners de anúncio
        adManager?.HideBanner();

        NotificationSystem.Show("Anúncios removidos! ✨", NotificationSystem.NotificationType.Sucesso);
        RefreshItemButtonStates();
        Debug.Log("[ShopScreenUI] Remove Ads concedido e salvo.");
    }

    private void GrantStarterPack()
    {
        // Marca pack como comprado para não exibir novamente
        PlayerPrefs.SetInt(PrefKeyStarterPack, 1);
        PlayerPrefs.Save();

        profileManager?.AddCoins(200);
        lifeSystem?.AddRewardLives(5);
        GameManager.NotifyLivesChanged(lifeSystem?.CurrentLives ?? 0);

        NotificationSystem.Show("Pack Iniciante recebido! 200🪙 + 5❤", NotificationSystem.NotificationType.Sucesso);
        RefreshItemButtonStates();
        Debug.Log("[ShopScreenUI] Starter Pack concedido.");
    }

    // -----------------------------------------------------------------------
    // Simulações debug (sem IAP instalado)
    // -----------------------------------------------------------------------

    private IEnumerator SimulatePurchase(int coins, string label)
    {
        Debug.Log($"[ShopScreenUI] [Debug] Simulando compra de {label}...");
        yield return new WaitForSeconds(1f);
        GrantCoins(coins);
    }

    private IEnumerator SimulateRemoveAds()
    {
        Debug.Log("[ShopScreenUI] [Debug] Simulando compra de remove_ads...");
        yield return new WaitForSeconds(1f);
        GrantRemoveAds();
    }

    private IEnumerator SimulateStarterPack()
    {
        Debug.Log("[ShopScreenUI] [Debug] Simulando compra de starter_pack...");
        yield return new WaitForSeconds(1f);
        GrantStarterPack();
    }

    // -----------------------------------------------------------------------
    // Estado dos botões
    // -----------------------------------------------------------------------

    private void RefreshItemButtonStates()
    {
        int coins = profileManager?.CurrentProfile?.coins ?? 0;
        bool adsRemoved = (profileManager?.CurrentProfile?.adsRemoved ?? false)
                          || PlayerPrefs.GetInt(PrefKeyRemoveAds, 0) == 1;
        bool starterBought = PlayerPrefs.GetInt(PrefKeyStarterPack, 0) == 1;

        if (buyLivesButton != null)
            buyLivesButton.interactable = coins >= livesCost;

        if (removeAdsButton != null)
        {
            removeAdsButton.interactable = !adsRemoved;
            if (removeAdsText != null && adsRemoved)
                removeAdsText.text = "Anúncios Removidos ✓";
        }

        if (starterPackButton != null)
        {
            starterPackButton.interactable = !starterBought;
            if (starterPackText != null && starterBought)
                starterPackText.text = "Pack Iniciante ✓";
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
        public string productId;
        public string priceDisplay;
        public Color cardColor = Color.white;
        public bool isBestValue;
    }
}
