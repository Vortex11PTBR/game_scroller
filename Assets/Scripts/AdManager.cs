/*
 * AdManager.cs — Sistema de anúncios mock para o Game Scroller
 * Propósito: Simula comportamento de anúncios reais (rewarded e interstitial) no Editor Unity.
 *            Preparado para integração futura com Unity Ads ou AdMob.
 * Como usar: Adicione ao GameObject do GameManager. Configure no Inspector.
 * Dependências: AdSecurityValidator.cs (opcional)
 */

using System;
using System.Collections;
using UnityEngine;

public class AdManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Configuração
    // -----------------------------------------------------------------------

    [Header("Configuração Mock")]
    [SerializeField] private float rewardedAdSimulatedDuration = 3f; // segundos de simulação
    [SerializeField] private float interstitialAdSimulatedDuration = 2f;
    [SerializeField] private bool debugMode = true;

    [Header("Integração")]
    [SerializeField] private AdSecurityValidator securityValidator;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private bool isShowingAd = false;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (securityValidator == null)
            securityValidator = FindObjectOfType<AdSecurityValidator>();
    }

    // -----------------------------------------------------------------------
    // API pública
    // -----------------------------------------------------------------------

    /// <summary>
    /// Exibe um anúncio recompensado. Aguarda 3s e retorna true via callback.
    /// INTEGRAÇÃO REAL: Substitua o corpo por UnityAds.ShowRewardedVideo() ou AdMob.ShowRewardedAd().
    /// </summary>
    public void ShowRewardedAd(Action<bool> onComplete)
    {
        if (isShowingAd)
        {
            Debug.LogWarning("[AdManager] Já existe um anúncio sendo exibido.");
            onComplete?.Invoke(false);
            return;
        }

        if (securityValidator != null && !securityValidator.CanShowAd())
        {
            Debug.LogWarning("[AdManager] Limite diário de anúncios atingido.");
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(SimulateRewardedAd(onComplete));
    }

    /// <summary>
    /// Exibe um anúncio intersticial entre jogos.
    /// INTEGRAÇÃO REAL: Substitua o corpo por UnityAds.ShowInterstitial() ou AdMob equivalente.
    /// </summary>
    public void ShowInterstitialAd(Action onComplete = null)
    {
        if (isShowingAd)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(SimulateInterstitialAd(onComplete));
    }

    /// <summary>
    /// Verifica se um anúncio está pronto para ser exibido.
    /// INTEGRAÇÃO REAL: Substitua por Advertisement.IsReady() ou similar.
    /// </summary>
    public bool IsAdReady()
    {
        if (securityValidator != null)
            return securityValidator.CanShowAd();

        return true; // sempre pronto em modo debug
    }

    // -----------------------------------------------------------------------
    // Simulações internas
    // -----------------------------------------------------------------------

    private IEnumerator SimulateRewardedAd(Action<bool> onComplete)
    {
        isShowingAd = true;

        if (debugMode)
            Debug.Log("[AdManager] Simulando anúncio recompensado...");

        // Registra visualização no validador de segurança
        securityValidator?.TryRecordAdView();

        // INTEGRAÇÃO REAL: aqui você chamaria o SDK de anúncios e aguardaria o callback
        yield return new WaitForSeconds(rewardedAdSimulatedDuration);

        isShowingAd = false;

        if (debugMode)
            Debug.Log("[AdManager] Anúncio recompensado finalizado — recompensa concedida.");

        onComplete?.Invoke(true);
    }

    private IEnumerator SimulateInterstitialAd(Action onComplete)
    {
        isShowingAd = true;

        if (debugMode)
            Debug.Log("[AdManager] Simulando anúncio intersticial...");

        securityValidator?.TryRecordAdView();

        // INTEGRAÇÃO REAL: aqui você chamaria o SDK de anúncios
        yield return new WaitForSeconds(interstitialAdSimulatedDuration);

        isShowingAd = false;

        if (debugMode)
            Debug.Log("[AdManager] Anúncio intersticial finalizado.");

        onComplete?.Invoke();
    }
}
