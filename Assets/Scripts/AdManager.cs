/*
 * AdManager.cs — Integração com Google AdMob para o Game Scroller
 * Propósito: Gerencia anúncios recompensados, interstitiais e banner via Google AdMob.
 *            Em debugMode=true usa simulação (funciona no Editor). debugMode=false usa SDK real.
 * Como usar: Adicione ao GameObject do GameManager. Configure IDs no Inspector.
 * Dependências: AdSecurityValidator.cs, GamePrivacyManager.cs
 *
 * IDs de teste AdMob (usar apenas em desenvolvimento):
 *   App ID:       ca-app-pub-3940256099942544~3347511713
 *   Rewarded:     ca-app-pub-3940256099942544/5224354917
 *   Interstitial: ca-app-pub-3940256099942544/1033173712
 *   Banner:       ca-app-pub-3940256099942544/6300978111
 *
 * Para produção: substitua pelos IDs reais obtidos no painel AdMob (admob.google.com)
 */

using System;
using System.Collections;
using UnityEngine;

// Importações do SDK AdMob — disponíveis quando o pacote com.google.ads.mobile estiver instalado
#if UNITY_ANDROID && !UNITY_EDITOR
using GoogleMobileAds.Api;
#endif

public class AdManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Configuração — IDs do AdMob (substituir pelos IDs reais no Inspector)
    // -----------------------------------------------------------------------

    [Header("Modo de Operação")]
    [Tooltip("true = simula anúncios no Editor | false = usa Google AdMob SDK real")]
    [SerializeField] private bool debugMode = true;

    [Header("IDs do AdMob (substituir pelos seus IDs reais)")]
    [Tooltip("App ID do AdMob. Exemplo de teste: ca-app-pub-3940256099942544~3347511713")]
    [SerializeField] private string admobAppId = "ca-app-pub-3940256099942544~3347511713";

    [Tooltip("Ad Unit ID do Rewarded (vida grátis). Teste: ca-app-pub-3940256099942544/5224354917")]
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

    [Tooltip("Ad Unit ID do Interstitial (entre jogos). Teste: ca-app-pub-3940256099942544/1033173712")]
    [SerializeField] private string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";

    [Tooltip("Ad Unit ID do Banner (topo ou base). Teste: ca-app-pub-3940256099942544/6300978111")]
    [SerializeField] private string bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";

    [Header("Configuração de Simulação (debug)")]
    [SerializeField] private float rewardedAdSimulatedDuration = 3f;
    [SerializeField] private float interstitialAdSimulatedDuration = 2f;

    [Header("Configuração de Interstitial")]
    [Tooltip("Exibe interstitial a cada N sessões de jogo")]
    [SerializeField] private int interstitialEveryNSessions = 3;

    [Header("Integração")]
    [SerializeField] private AdSecurityValidator securityValidator;
    [SerializeField] private GamePrivacyManager privacyManager;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private bool isShowingAd = false;
    private bool isSdkInitialized = false;
    private int sessionCount = 0;

    // Objetos de anúncio reais (usados somente em Android real com SDK)
#if UNITY_ANDROID && !UNITY_EDITOR
    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private BannerView bannerView;
#endif

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (securityValidator == null)
            securityValidator = FindObjectOfType<AdSecurityValidator>();

        if (privacyManager == null)
            privacyManager = FindObjectOfType<GamePrivacyManager>();
    }

    private void Start()
    {
        // Aguarda consentimento do usuário antes de inicializar o SDK
        if (privacyManager != null)
        {
            if (privacyManager.IsPrivacyAccepted())
                InitializeAdMob();
            else
                privacyManager.OnConsentReady += InitializeAdMob;
        }
        else
        {
            // Sem gerenciador de privacidade: inicializa direto (não recomendado em produção)
            InitializeAdMob();
        }
    }

    private void OnDestroy()
    {
        if (privacyManager != null)
            privacyManager.OnConsentReady -= InitializeAdMob;

        DestroyBanner();
    }

    /// <summary>
    /// Inicializa o SDK do AdMob e pré-carrega os anúncios.
    /// Chamado automaticamente após o consentimento do usuário.
    /// </summary>
    private void InitializeAdMob()
    {
        if (isSdkInitialized)
            return;

        isSdkInitialized = true;

        if (debugMode)
        {
            Debug.Log("[AdManager] Modo debug ativo — usando simulação de anúncios.");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Inicializa o SDK do Google Mobile Ads
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[AdManager] AdMob SDK inicializado com sucesso.");
            LoadRewardedAd();
            LoadInterstitialAd();
        });
#else
        Debug.Log("[AdManager] AdMob SDK não disponível nesta plataforma.");
#endif
    }

    // -----------------------------------------------------------------------
    // Carregamento de anúncios
    // -----------------------------------------------------------------------

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Pré-carrega um anúncio recompensado.
    /// </summary>
    private void LoadRewardedAd()
    {
        // Destrói anúncio anterior se existir
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        var adRequest = CreateAdRequest();
        RewardedAd.Load(rewardedAdUnitId, adRequest, (RewardedAd ad, LoadAdError loadError) =>
        {
            if (loadError != null || ad == null)
            {
                Debug.LogError($"[AdManager] Falha ao carregar Rewarded Ad: {loadError?.GetMessage()}");
                return;
            }

            rewardedAd = ad;
            Debug.Log("[AdManager] Rewarded Ad carregado com sucesso.");

            // Recarrega automaticamente quando fechado
            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdManager] Rewarded Ad fechado. Recarregando...");
                LoadRewardedAd();
            };
        });
    }

    /// <summary>
    /// Pré-carrega um anúncio intersticial.
    /// </summary>
    private void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        var adRequest = CreateAdRequest();
        InterstitialAd.Load(interstitialAdUnitId, adRequest, (InterstitialAd ad, LoadAdError loadError) =>
        {
            if (loadError != null || ad == null)
            {
                Debug.LogError($"[AdManager] Falha ao carregar Interstitial Ad: {loadError?.GetMessage()}");
                return;
            }

            interstitialAd = ad;
            Debug.Log("[AdManager] Interstitial Ad carregado com sucesso.");

            // Recarrega automaticamente quando fechado
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdManager] Interstitial Ad fechado. Recarregando...");
                LoadInterstitialAd();
            };
        });
    }

    /// <summary>
    /// Cria uma requisição de anúncio respeitando as preferências de privacidade.
    /// </summary>
    private AdRequest CreateAdRequest()
    {
        return new AdRequest();
    }
#endif

    // -----------------------------------------------------------------------
    // API pública
    // -----------------------------------------------------------------------

    /// <summary>
    /// Exibe um anúncio recompensado (vida grátis).
    /// Retorna true via callback se o usuário assistiu até o fim.
    /// </summary>
    public void ShowRewardedAd(Action<bool> onComplete)
    {
        if (isShowingAd)
        {
            Debug.LogWarning("[AdManager] Já existe um anúncio sendo exibido.");
            onComplete?.Invoke(false);
            return;
        }

        // Verifica limite diário de segurança
        if (securityValidator != null && !securityValidator.CanShowAd())
        {
            Debug.LogWarning("[AdManager] Limite diário de anúncios atingido.");
            onComplete?.Invoke(false);
            return;
        }

        // Verifica consentimento
        if (privacyManager != null && !privacyManager.IsPrivacyAccepted())
        {
            Debug.LogWarning("[AdManager] Anúncio bloqueado: consentimento pendente.");
            onComplete?.Invoke(false);
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!debugMode)
        {
            ShowRewardedAdReal(onComplete);
            return;
        }
#endif

        // Modo debug: simulação
        StartCoroutine(SimulateRewardedAd(onComplete));
    }

    /// <summary>
    /// Exibe um anúncio intersticial entre jogos (a cada N sessões).
    /// </summary>
    public void ShowInterstitialAd(Action onComplete = null)
    {
        if (isShowingAd)
        {
            onComplete?.Invoke();
            return;
        }

        // Verifica consentimento
        if (privacyManager != null && !privacyManager.IsPrivacyAccepted())
        {
            onComplete?.Invoke();
            return;
        }

        sessionCount++;
        if (sessionCount % interstitialEveryNSessions != 0)
        {
            onComplete?.Invoke();
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!debugMode)
        {
            ShowInterstitialAdReal(onComplete);
            return;
        }
#endif

        StartCoroutine(SimulateInterstitialAd(onComplete));
    }

    /// <summary>
    /// Exibe o banner de anúncio (topo ou base da tela).
    /// </summary>
    public void ShowBanner()
    {
        if (debugMode)
        {
            Debug.Log("[AdManager] [Debug] Banner seria exibido aqui.");
            return;
        }

        if (privacyManager != null && !privacyManager.IsPrivacyAccepted())
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        ShowBannerReal();
#endif
    }

    /// <summary>
    /// Esconde o banner de anúncio.
    /// </summary>
    public void HideBanner()
    {
        if (debugMode)
        {
            Debug.Log("[AdManager] [Debug] Banner seria escondido aqui.");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        DestroyBanner();
#endif
    }

    /// <summary>
    /// Verifica se o anúncio recompensado está pronto para exibição.
    /// </summary>
    public bool IsRewardedReady()
    {
        if (debugMode)
            return securityValidator == null || securityValidator.CanShowAd();

#if UNITY_ANDROID && !UNITY_EDITOR
        return rewardedAd != null && rewardedAd.CanShowAd();
#else
        return false;
#endif
    }

    /// <summary>
    /// Verifica se o anúncio intersticial está pronto para exibição.
    /// </summary>
    public bool IsInterstitialReady()
    {
        if (debugMode)
            return true;

#if UNITY_ANDROID && !UNITY_EDITOR
        return interstitialAd != null && interstitialAd.CanShowAd();
#else
        return false;
#endif
    }

    /// <summary>
    /// Mantém compatibilidade com código legado.
    /// </summary>
    public bool IsAdReady()
    {
        return IsRewardedReady();
    }

    // -----------------------------------------------------------------------
    // Exibição real (SDK AdMob)
    // -----------------------------------------------------------------------

#if UNITY_ANDROID && !UNITY_EDITOR
    private void ShowRewardedAdReal(Action<bool> onComplete)
    {
        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            Debug.LogWarning("[AdManager] Rewarded Ad não está pronto. Recarregando...");
            LoadRewardedAd();
            onComplete?.Invoke(false);
            return;
        }

        isShowingAd = true;
        securityValidator?.TryRecordAdView();

        rewardedAd.Show(reward =>
        {
            isShowingAd = false;
            Debug.Log($"[AdManager] Recompensa concedida: {reward.Amount} {reward.Type}");

            // Valida com o validador de segurança antes de conceder a recompensa
            if (securityValidator != null && !securityValidator.CanShowAd())
            {
                Debug.LogWarning("[AdManager] Validação de segurança falhou. Recompensa negada.");
                onComplete?.Invoke(false);
                return;
            }

            onComplete?.Invoke(true);
        });
    }

    private void ShowInterstitialAdReal(Action onComplete)
    {
        if (interstitialAd == null || !interstitialAd.CanShowAd())
        {
            Debug.LogWarning("[AdManager] Interstitial Ad não está pronto. Recarregando...");
            LoadInterstitialAd();
            onComplete?.Invoke();
            return;
        }

        isShowingAd = true;
        securityValidator?.TryRecordAdView();

        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            isShowingAd = false;
            onComplete?.Invoke();
        };

        interstitialAd.Show();
    }

    private void ShowBannerReal()
    {
        DestroyBanner();

        // Posição do banner: base da tela (AdPosition.Bottom) ou topo (AdPosition.Top)
        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
        var adRequest = CreateAdRequest();
        bannerView.LoadAd(adRequest);

        Debug.Log("[AdManager] Banner carregado.");
    }

    private void DestroyBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }
#else
    private void DestroyBanner() { }
#endif

    // -----------------------------------------------------------------------
    // Simulações internas (debug)
    // -----------------------------------------------------------------------

    private IEnumerator SimulateRewardedAd(Action<bool> onComplete)
    {
        isShowingAd = true;
        Debug.Log("[AdManager] [Debug] Simulando anúncio recompensado...");
        securityValidator?.TryRecordAdView();

        yield return new WaitForSeconds(rewardedAdSimulatedDuration);

        isShowingAd = false;
        Debug.Log("[AdManager] [Debug] Anúncio recompensado finalizado — recompensa concedida.");
        onComplete?.Invoke(true);
    }

    private IEnumerator SimulateInterstitialAd(Action onComplete)
    {
        isShowingAd = true;
        Debug.Log("[AdManager] [Debug] Simulando anúncio intersticial...");
        securityValidator?.TryRecordAdView();

        yield return new WaitForSeconds(interstitialAdSimulatedDuration);

        isShowingAd = false;
        Debug.Log("[AdManager] [Debug] Anúncio intersticial finalizado.");
        onComplete?.Invoke();
    }
}
