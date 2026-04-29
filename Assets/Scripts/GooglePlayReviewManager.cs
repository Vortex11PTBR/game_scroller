/*
 * GooglePlayReviewManager.cs — Solicitação de avaliação in-app via Google Play
 * Propósito: Solicita avaliação do app no Google Play após condições específicas serem cumpridas.
 *            Usa a Google Play In-App Review API para exibir o popup nativo de avaliação.
 * Como usar: Adicione ao GameManager. Chame TryRequestReview() no fim de cada sessão bem-sucedida.
 * Dependências: Nenhuma (funciona standalone)
 *
 * Para integrar a API real:
 *   1. Instale o pacote com.google.play.review via Package Manager
 *      (Google Play Plugins for Unity: https://github.com/google/play-unity-plugins)
 *   2. Descomente os blocos #if GOOGLE_PLAY_REVIEW abaixo
 *   3. Defina o símbolo GOOGLE_PLAY_REVIEW em Player Settings > Scripting Define Symbols
 */

using UnityEngine;

public class GooglePlayReviewManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Chaves de PlayerPrefs
    // -----------------------------------------------------------------------

    private const string KeySessionCount         = "ReviewManager.SessionCount";
    private const string KeyLastReviewTimestamp  = "ReviewManager.LastReviewTimestamp";

    // -----------------------------------------------------------------------
    // Configuração
    // -----------------------------------------------------------------------

    [Header("Configuração de Avaliação")]
    [Tooltip("Número de sessões bem-sucedidas antes de solicitar avaliação pela primeira vez")]
    [SerializeField] private int sessionsBeforeFirstReview = 5;

    [Tooltip("Intervalo mínimo entre solicitações de avaliação (em dias)")]
    [SerializeField] private int minDaysBetweenReviews = 7;

    [Tooltip("true = apenas loga no Console (não mostra popup real)")]
    [SerializeField] private bool debugMode = true;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private int sessionCount;
    private long lastReviewTimestamp;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        sessionCount        = PlayerPrefs.GetInt(KeySessionCount, 0);
        lastReviewTimestamp = long.Parse(PlayerPrefs.GetString(KeyLastReviewTimestamp, "0"));
    }

    // -----------------------------------------------------------------------
    // API pública
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifica as condições e solicita avaliação se adequado.
    /// Deve ser chamado pelo GameSessionManager ao final de cada sessão bem-sucedida.
    /// </summary>
    public void TryRequestReview()
    {
        // Incrementa contador de sessões
        sessionCount++;
        PlayerPrefs.SetInt(KeySessionCount, sessionCount);
        PlayerPrefs.Save();

        Debug.Log($"[GooglePlayReviewManager] Sessão registrada. Total: {sessionCount}");

        // Verifica se atingiu o número mínimo de sessões
        if (sessionCount < sessionsBeforeFirstReview)
        {
            Debug.Log($"[GooglePlayReviewManager] Aguardando mais sessões ({sessionCount}/{sessionsBeforeFirstReview}).");
            return;
        }

        // Verifica intervalo desde a última solicitação
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long secondsSinceLastReview = now - lastReviewTimestamp;
        long minSecondsBetweenReviews = minDaysBetweenReviews * 24 * 60 * 60;

        if (lastReviewTimestamp > 0 && secondsSinceLastReview < minSecondsBetweenReviews)
        {
            int daysRemaining = Mathf.CeilToInt((minSecondsBetweenReviews - secondsSinceLastReview) / 86400f);
            Debug.Log($"[GooglePlayReviewManager] Aguardando intervalo. Próxima solicitação em {daysRemaining} dia(s).");
            return;
        }

        // Condições atendidas: solicitar avaliação
        RequestReview();
    }

    // -----------------------------------------------------------------------
    // Solicitação de avaliação
    // -----------------------------------------------------------------------

    private void RequestReview()
    {
        // Atualiza timestamp da última solicitação
        lastReviewTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetString(KeyLastReviewTimestamp, lastReviewTimestamp.ToString());
        PlayerPrefs.Save();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!debugMode)
        {
            RequestReviewReal();
            return;
        }
#endif

        // Modo debug ou Editor: apenas loga
        Debug.Log("[GooglePlayReviewManager] [Debug] Solicitação de avaliação seria exibida aqui.");
        Debug.Log($"[GooglePlayReviewManager] Condições atendidas: {sessionCount} sessões, último review há {lastReviewTimestamp} seg.");
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Solicita avaliação via Google Play In-App Review API.
    /// Requer o pacote com.google.play.review instalado.
    /// </summary>
    private void RequestReviewReal()
    {
        // Descomente após instalar o pacote com.google.play.review:
        //
        // var reviewManager = new ReviewManager();
        // var requestFlowOperation = reviewManager.RequestReviewFlow();
        // requestFlowOperation.Completed += (operation) =>
        // {
        //     if (operation.Error != ReviewErrorCode.NoError)
        //     {
        //         Debug.LogError($"[GooglePlayReviewManager] Erro ao solicitar review: {operation.Error}");
        //         return;
        //     }
        //
        //     var playReviewInfo = operation.GetResult();
        //     var launchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);
        //
        //     launchFlowOperation.Completed += (launchOperation) =>
        //     {
        //         if (launchOperation.Error != ReviewErrorCode.NoError)
        //             Debug.LogError($"[GooglePlayReviewManager] Erro ao exibir review: {launchOperation.Error}");
        //         else
        //             Debug.Log("[GooglePlayReviewManager] Fluxo de avaliação concluído.");
        //     };
        // };
        //
        // Por ora, apenas loga:
        Debug.Log("[GooglePlayReviewManager] Google Play In-App Review: pacote não instalado ainda. Instale com.google.play.review.");
    }
#endif

    // -----------------------------------------------------------------------
    // Utilitários
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reseta os contadores de review (útil para testes).
    /// </summary>
    [ContextMenu("Resetar Contadores de Review")]
    public void ResetReviewCounters()
    {
        sessionCount = 0;
        lastReviewTimestamp = 0;
        PlayerPrefs.DeleteKey(KeySessionCount);
        PlayerPrefs.DeleteKey(KeyLastReviewTimestamp);
        PlayerPrefs.Save();
        Debug.Log("[GooglePlayReviewManager] Contadores resetados.");
    }
}
