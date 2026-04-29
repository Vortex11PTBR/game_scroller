/*
 * GamePrivacyManager.cs — Gerenciamento de consentimento GDPR/LGPD
 * Propósito: Verifica e solicita consentimento do usuário para uso de dados e anúncios personalizados.
 *            Integra com Google UMP (User Messaging Platform) para conformidade com GDPR.
 *            Na primeira execução, exibe tela de política de privacidade e termos de uso.
 * Como usar: Adicione ao GameObject do GameManager. O AdManager aguarda o evento OnConsentReady.
 * Dependências: Nenhuma (funciona standalone)
 *
 * Para integrar com UMP real:
 *   1. Instale o Google UMP SDK (incluído no pacote com.google.ads.mobile v9+)
 *   2. O símbolo GOOGLE_MOBILE_ADS será definido automaticamente
 *   3. Configure o App ID no Inspector
 */

using System;
using UnityEngine;
using UnityEngine.UI;

// Importações do UMP — disponíveis quando o pacote com.google.ads.mobile estiver instalado
#if UNITY_ANDROID && !UNITY_EDITOR
// using Google.MobileAds.Ump.Api;
#endif

public class GamePrivacyManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Chaves de PlayerPrefs
    // -----------------------------------------------------------------------

    private const string KeyPrivacyAccepted        = "privacy_accepted";
    private const string KeyPersonalizedAds        = "personalized_ads_consent";
    private const string KeyPrivacyVersion         = "privacy_version";

    // Versão da política — incremente quando a política mudar para forçar nova aceitação
    private const int CurrentPrivacyVersion = 1;

    // -----------------------------------------------------------------------
    // Configuração
    // -----------------------------------------------------------------------

    [Header("Configuração")]
    [Tooltip("URL da política de privacidade publicada (obrigatória para Play Store)")]
    [SerializeField] private string privacyPolicyUrl = "https://seusite.com/privacidade";

    [Tooltip("URL dos termos de uso")]
    [SerializeField] private string termsOfServiceUrl = "https://seusite.com/termos";

    [Tooltip("true = aceita consentimento automaticamente (desenvolvimento)")]
    [SerializeField] private bool debugMode = true;

    // -----------------------------------------------------------------------
    // UI do Popup de Privacidade
    // -----------------------------------------------------------------------

    [Header("UI de Consentimento")]
    [Tooltip("Painel de popup de privacidade (ativo na primeira execução)")]
    [SerializeField] private GameObject privacyPopupPanel;

    [SerializeField] private Button acceptAllButton;
    [SerializeField] private Button acceptEssentialButton;
    [SerializeField] private Button privacyPolicyButton;
    [SerializeField] private Button termsButton;

    [SerializeField] private Text privacyMessageText;

    // -----------------------------------------------------------------------
    // Evento
    // -----------------------------------------------------------------------

    /// <summary>
    /// Disparado quando o consentimento está pronto (aceito ou recusado).
    /// O AdManager assina este evento para inicializar o SDK.
    /// </summary>
    public event Action OnConsentReady;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private bool privacyAccepted = false;
    private bool personalizedAdsConsent = false;
    private bool isConsentReady = false;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        // Carrega estado salvo
        privacyAccepted = PlayerPrefs.GetInt(KeyPrivacyAccepted, 0) == 1;
        personalizedAdsConsent = PlayerPrefs.GetInt(KeyPersonalizedAds, 0) == 1;

        int savedVersion = PlayerPrefs.GetInt(KeyPrivacyVersion, 0);

        // Força nova aceitação se a política foi atualizada
        if (savedVersion < CurrentPrivacyVersion)
        {
            privacyAccepted = false;
            personalizedAdsConsent = false;
            // Persiste o reset imediatamente para evitar inconsistências se o app fechar antes do usuário responder
            PlayerPrefs.SetInt(KeyPrivacyAccepted, 0);
            PlayerPrefs.SetInt(KeyPersonalizedAds, 0);
            PlayerPrefs.Save();
        }
    }

    private void Start()
    {
        SetupButtons();

        if (debugMode)
        {
            // Em modo debug: aceita automaticamente sem mostrar popup
            Debug.Log("[GamePrivacyManager] Modo debug — consentimento aceito automaticamente.");
            AcceptAll();
            return;
        }

        // Verifica se precisa mostrar o popup de consentimento
        if (!privacyAccepted)
        {
            ShowPrivacyScreen();
        }
        else
        {
            // Já tem consentimento salvo: inicia fluxo UMP para verificar requisitos de GDPR
            CheckUmpConsent();
        }
    }

    // -----------------------------------------------------------------------
    // API pública
    // -----------------------------------------------------------------------

    /// <summary>
    /// Retorna true se o usuário já aceitou a política de privacidade.
    /// </summary>
    public bool IsPrivacyAccepted()
    {
        return privacyAccepted;
    }

    /// <summary>
    /// Retorna true se o usuário consentiu com anúncios personalizados.
    /// </summary>
    public bool HasConsentForPersonalizedAds()
    {
        return personalizedAdsConsent;
    }

    /// <summary>
    /// Exibe a tela de política de privacidade manualmente (ex: via botão nas configurações).
    /// </summary>
    public void ShowPrivacyScreen()
    {
        if (privacyPopupPanel != null)
        {
            privacyPopupPanel.SetActive(true);
            Debug.Log("[GamePrivacyManager] Popup de privacidade exibido.");
        }
        else
        {
            Debug.LogWarning("[GamePrivacyManager] privacyPopupPanel não configurado no Inspector.");
        }
    }

    // -----------------------------------------------------------------------
    // Configuração dos botões
    // -----------------------------------------------------------------------

    private void SetupButtons()
    {
        if (acceptAllButton != null)
            acceptAllButton.onClick.AddListener(AcceptAll);

        if (acceptEssentialButton != null)
            acceptEssentialButton.onClick.AddListener(AcceptEssentialOnly);

        if (privacyPolicyButton != null)
            privacyPolicyButton.onClick.AddListener(OpenPrivacyPolicy);

        if (termsButton != null)
            termsButton.onClick.AddListener(OpenTermsOfService);

        if (privacyMessageText != null)
        {
            privacyMessageText.text =
                "Usamos cookies e anúncios para melhorar sua experiência. " +
                "Ao continuar, você concorda com nossa Política de Privacidade e Termos de Uso. " +
                "Você pode escolher aceitar apenas anúncios essenciais (não personalizados).";
        }
    }

    // -----------------------------------------------------------------------
    // Ações de consentimento
    // -----------------------------------------------------------------------

    /// <summary>
    /// Aceita todos os termos, incluindo anúncios personalizados.
    /// </summary>
    private void AcceptAll()
    {
        privacyAccepted = true;
        personalizedAdsConsent = true;
        SaveConsentState();
        HidePrivacyPopup();
        NotifyConsentReady();
        Debug.Log("[GamePrivacyManager] Usuário aceitou todos os termos (anúncios personalizados).");
    }

    /// <summary>
    /// Aceita apenas o essencial (sem anúncios personalizados).
    /// </summary>
    private void AcceptEssentialOnly()
    {
        privacyAccepted = true;
        personalizedAdsConsent = false;
        SaveConsentState();
        HidePrivacyPopup();
        NotifyConsentReady();
        Debug.Log("[GamePrivacyManager] Usuário aceitou apenas itens essenciais (sem anúncios personalizados).");
    }

    private void OpenPrivacyPolicy()
    {
        Application.OpenURL(privacyPolicyUrl);
        Debug.Log($"[GamePrivacyManager] Abrindo política de privacidade: {privacyPolicyUrl}");
    }

    private void OpenTermsOfService()
    {
        Application.OpenURL(termsOfServiceUrl);
        Debug.Log($"[GamePrivacyManager] Abrindo termos de uso: {termsOfServiceUrl}");
    }

    // -----------------------------------------------------------------------
    // Verificação UMP (Google User Messaging Platform)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifica requisitos de consentimento via Google UMP para usuários da UE.
    /// Em regiões fora da UE, o consentimento já está salvo e é suficiente.
    /// </summary>
    private void CheckUmpConsent()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Descomente após instalar o Google UMP SDK (incluído no com.google.ads.mobile v9+):
        //
        // var requestParameters = new ConsentRequestParameters();
        //
        // ConsentInformation.Update(requestParameters, OnConsentInfoUpdated);
        //
        // Por ora, aceita direto se já tem consentimento salvo:
        NotifyConsentReady();
#else
        // No editor ou fora do Android: apenas notifica que está pronto
        NotifyConsentReady();
#endif
    }

    // Descomente este método ao integrar o UMP SDK:
    // private void OnConsentInfoUpdated(FormError error)
    // {
    //     if (error != null)
    //     {
    //         Debug.LogError($"[GamePrivacyManager] Erro ao verificar UMP: {error.Message}");
    //         NotifyConsentReady(); // Continua mesmo com erro
    //         return;
    //     }
    //
    //     if (ConsentInformation.IsConsentFormAvailable())
    //     {
    //         ConsentForm.Load(OnConsentFormLoaded);
    //     }
    //     else
    //     {
    //         NotifyConsentReady();
    //     }
    // }
    //
    // private void OnConsentFormLoaded(ConsentForm form, FormError loadError)
    // {
    //     if (loadError != null)
    //     {
    //         Debug.LogError($"[GamePrivacyManager] Erro ao carregar formulário UMP: {loadError.Message}");
    //         NotifyConsentReady();
    //         return;
    //     }
    //
    //     if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
    //     {
    //         form.Show(OnConsentFormDismissed);
    //     }
    //     else
    //     {
    //         NotifyConsentReady();
    //     }
    // }
    //
    // private void OnConsentFormDismissed(FormError dismissError)
    // {
    //     if (dismissError != null)
    //         Debug.LogError($"[GamePrivacyManager] Formulário UMP fechado com erro: {dismissError.Message}");
    //
    //     personalizedAdsConsent = ConsentInformation.ConsentStatus == ConsentStatus.Obtained;
    //     privacyAccepted = true;
    //     SaveConsentState();
    //     NotifyConsentReady();
    // }

    // -----------------------------------------------------------------------
    // Utilitários
    // -----------------------------------------------------------------------

    private void SaveConsentState()
    {
        PlayerPrefs.SetInt(KeyPrivacyAccepted, privacyAccepted ? 1 : 0);
        PlayerPrefs.SetInt(KeyPersonalizedAds, personalizedAdsConsent ? 1 : 0);
        PlayerPrefs.SetInt(KeyPrivacyVersion, CurrentPrivacyVersion);
        PlayerPrefs.Save();
    }

    private void HidePrivacyPopup()
    {
        if (privacyPopupPanel != null)
            privacyPopupPanel.SetActive(false);
    }

    private void NotifyConsentReady()
    {
        if (!isConsentReady)
        {
            isConsentReady = true;
            Debug.Log("[GamePrivacyManager] Consentimento pronto. Notificando sistemas...");
            OnConsentReady?.Invoke();
        }
    }
}
