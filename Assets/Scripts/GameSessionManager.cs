/*
 * GameSessionManager.cs — Gerencia o fluxo feed → jogo → resultado
 * Propósito: Controla abertura/fechamento de minijogos, verifica vidas, mostra resultado pós-jogo.
 * Como usar: Adicione ao GameManager. Use RequestStartGame(data) para iniciar uma sessão de jogo.
 * Dependências: GameManager, LifeSystem, PlayerProfileManager, AdManager, MiniGameData
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameSessionManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Painéis de UI
    // -----------------------------------------------------------------------

    [Header("Painéis")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject noLivesPanel; // referência ao NoLivesPopup

    [Header("UI de Resultado")]
    [SerializeField] private Text resultTitleText;
    [SerializeField] private Text resultScoreText;
    [SerializeField] private Text resultCoinsText;
    [SerializeField] private Text resultHighScoreText;
    [SerializeField] private Button resultBackButton;
    [SerializeField] private Button resultShareButton;
    [SerializeField] private Button resultRetryButton;
    [SerializeField] private Image resultNewHighScoreBadge;

    [Header("UI de Carregamento")]
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private Text loadingText;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private MiniGameData currentGameData;
    private MiniGameBase currentGame;
    private float sessionStartTime;
    private bool isInSession = false;

    // -----------------------------------------------------------------------
    // Eventos
    // -----------------------------------------------------------------------

    public event Action<MiniGameData> OnSessionStarted;
    public event Action<GameSessionResult> OnSessionEnded;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        HideAllPanels();
    }

    private void Start()
    {
        if (resultBackButton != null)
            resultBackButton.onClick.AddListener(OnBackToFeedPressed);

        if (resultShareButton != null)
            resultShareButton.onClick.AddListener(OnSharePressed);

        if (resultRetryButton != null)
            resultRetryButton.onClick.AddListener(OnRetryPressed);
    }

    // -----------------------------------------------------------------------
    // API pública
    // -----------------------------------------------------------------------

    /// <summary>
    /// Solicita iniciar um jogo. Verifica vidas antes de prosseguir.
    /// </summary>
    public void RequestStartGame(MiniGameData data, MiniGameBase gameInstance = null)
    {
        if (data == null)
        {
            Debug.LogError("[GameSessionManager] MiniGameData é nulo.");
            return;
        }

        currentGameData  = data;
        currentGame      = gameInstance;

        // Verifica vidas
        if (!CheckLives())
        {
            ShowNoLivesPanel();
            return;
        }

        StartCoroutine(StartGameRoutine());
    }

    /// <summary>
    /// Chamado quando o minijogo atual termina.
    /// </summary>
    public void OnGameFinished(int finalScore)
    {
        if (!isInSession)
            return;

        isInSession = false;

        float playTime = Time.time - sessionStartTime;
        int coinsEarned = Mathf.FloorToInt(finalScore * 0.1f);

        GameSessionResult result = new GameSessionResult
        {
            gameId       = currentGameData?.id ?? "unknown",
            gameTitle    = currentGameData?.titulo ?? "Desconhecido",
            score        = finalScore,
            coinsEarned  = coinsEarned,
            playTimeSeconds = playTime,
            sessionDate  = DateTime.UtcNow
        };

        // Registra no perfil do jogador
        GameManager.Instance?.ProfileManager?.RegisterGameSession(result);

        ShowResultPanel(result);
        OnSessionEnded?.Invoke(result);

        Debug.Log($"[GameSessionManager] Sessão encerrada. Score: {finalScore}, Moedas: {coinsEarned}");
    }

    // -----------------------------------------------------------------------
    // Internos
    // -----------------------------------------------------------------------

    private bool CheckLives()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.HasLives();

        return true;
    }

    private IEnumerator StartGameRoutine()
    {
        ShowLoadingPanel("Carregando...");
        yield return null;

        // Simula progresso de carregamento
        for (float progress = 0f; progress < 1f; progress += 0.05f)
        {
            if (loadingProgressBar != null)
                loadingProgressBar.value = progress;

            if (loadingText != null)
                loadingText.text = $"Carregando... {Mathf.RoundToInt(progress * 100)}%";

            yield return new WaitForSeconds(0.02f);
        }

        HideLoadingPanel();

        // Inicia o jogo
        isInSession    = true;
        sessionStartTime = Time.time;

        if (currentGame != null)
            currentGame.StartGame();

        OnSessionStarted?.Invoke(currentGameData);
        GameManager.NotifyGameStarted(currentGameData);

        Debug.Log($"[GameSessionManager] Sessão iniciada: {currentGameData?.titulo}");
    }

    private void ShowResultPanel(GameSessionResult result)
    {
        if (resultPanel == null)
            return;

        resultPanel.SetActive(true);

        if (resultTitleText != null)
            resultTitleText.text = result.isNewHighScore ? "🏆 Novo Recorde!" : "Resultado";

        if (resultScoreText != null)
            resultScoreText.text = $"Score: {result.score:N0}";

        if (resultCoinsText != null)
            resultCoinsText.text = $"+{result.coinsEarned} moedas";

        int highScore = GameManager.Instance?.ProfileManager?.CurrentProfile?.GetHighScore(result.gameId) ?? 0;
        if (resultHighScoreText != null)
            resultHighScoreText.text = $"Recorde: {highScore:N0}";

        if (resultNewHighScoreBadge != null)
            resultNewHighScoreBadge.gameObject.SetActive(result.isNewHighScore);

        // Mostra botão de retry apenas se tiver vidas
        if (resultRetryButton != null)
            resultRetryButton.interactable = CheckLives();
    }

    private void ShowNoLivesPanel()
    {
        if (noLivesPanel != null)
            noLivesPanel.SetActive(true);
        else
            Debug.LogWarning("[GameSessionManager] Painel de sem vidas não configurado no Inspector.");
    }

    private void ShowLoadingPanel(string message)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText != null)
                loadingText.text = message;
            if (loadingProgressBar != null)
                loadingProgressBar.value = 0f;
        }
    }

    private void HideLoadingPanel()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    private void HideAllPanels()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (resultPanel  != null) resultPanel.SetActive(false);
        if (noLivesPanel != null) noLivesPanel.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Botões de resultado
    // -----------------------------------------------------------------------

    private void OnBackToFeedPressed()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        Debug.Log("[GameSessionManager] Voltando ao feed.");
    }

    private void OnSharePressed()
    {
        // Compartilhamento nativo — plataforma específica
        Debug.Log($"[GameSessionManager] Compartilhando resultado: {currentGameData?.titulo}");

        #if UNITY_ANDROID || UNITY_IOS
        // Exemplo de compartilhamento via plugin nativo:
        // NativeShare.Share($"Joguei {currentGameData?.titulo} no Game Scroller! Baixe agora.");
        #endif
    }

    private void OnRetryPressed()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        RequestStartGame(currentGameData, currentGame);
    }
}
