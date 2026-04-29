/*
 * PlayStoreDeepLinkHandler.cs — Interceptador de Deep Links do Game Scroller
 * Propósito: Intercepta e processa deep links do tipo gamescroller:// para navegação direta.
 *            Útil para compartilhamento de jogos e perfis entre usuários.
 * Como usar: Adicione ao GameManager. Configure as referências no Inspector.
 * Dependências: GameSessionManager.cs, ProfileScreenUI.cs
 *
 * Formatos de deep link suportados:
 *   gamescroller://game/{gameId}     → Abre um jogo específico
 *   gamescroller://profile/{userId}  → Abre o perfil de um jogador
 *
 * Para configurar no AndroidManifest:
 *   O intent-filter para o scheme "gamescroller" já está no AndroidManifest.xml
 *   (Assets/Plugins/Android/AndroidManifest.xml)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayStoreDeepLinkHandler : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Configuração
    // -----------------------------------------------------------------------

    private const string Scheme      = "gamescroller://";
    private const string GamePath    = "game/";
    private const string ProfilePath = "profile/";

    [Header("Referências")]
    [Tooltip("Referência ao GameSessionManager para iniciar jogos via deep link")]
    [SerializeField] private GameSessionManager sessionManager;

    [Tooltip("Referência ao painel de perfil para exibição via deep link")]
    [SerializeField] private GameObject profilePanel;

    [Tooltip("Referência ao texto de username no perfil (opcional)")]
    [SerializeField] private Text profileUserIdText;

    [Header("Dados de Jogo")]
    [Tooltip("Lista de dados de minijogos disponíveis (para buscar por ID via deep link)")]
    [SerializeField] private List<MiniGameData> allGames = new List<MiniGameData>();

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private string pendingDeepLink = null;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (sessionManager == null)
            sessionManager = FindObjectOfType<GameSessionManager>();

        // Registra o handler para deep links recebidos enquanto o app está aberto
        Application.deepLinkActivated += OnDeepLinkActivated;

        // Verifica se o app foi aberto via deep link (link recebido antes da inicialização)
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            pendingDeepLink = Application.absoluteURL;
            if (debugMode)
                Debug.Log($"[PlayStoreDeepLinkHandler] Deep link na abertura: {pendingDeepLink}");
        }
    }

    private void Start()
    {
        // Processa deep link pendente (recebido antes do Start)
        if (!string.IsNullOrEmpty(pendingDeepLink))
        {
            ProcessDeepLink(pendingDeepLink);
            pendingDeepLink = null;
        }
    }

    private void OnDestroy()
    {
        Application.deepLinkActivated -= OnDeepLinkActivated;
    }

    // -----------------------------------------------------------------------
    // Handler principal
    // -----------------------------------------------------------------------

    /// <summary>
    /// Chamado pelo sistema quando um deep link é ativado enquanto o app está aberto.
    /// </summary>
    private void OnDeepLinkActivated(string url)
    {
        if (debugMode)
            Debug.Log($"[PlayStoreDeepLinkHandler] Deep link recebido: {url}");

        ProcessDeepLink(url);
    }

    /// <summary>
    /// Analisa e processa a URL do deep link.
    /// </summary>
    private void ProcessDeepLink(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        if (!url.StartsWith(Scheme))
        {
            Debug.LogWarning($"[PlayStoreDeepLinkHandler] URL não reconhecida: {url}");
            return;
        }

        // Remove o scheme para obter o path
        string path = url.Substring(Scheme.Length);

        if (debugMode)
            Debug.Log($"[PlayStoreDeepLinkHandler] Processando path: {path}");

        // Identifica o tipo de deep link
        if (path.StartsWith(GamePath))
        {
            string gameId = path.Substring(GamePath.Length);
            HandleGameDeepLink(gameId);
        }
        else if (path.StartsWith(ProfilePath))
        {
            string userId = path.Substring(ProfilePath.Length);
            HandleProfileDeepLink(userId);
        }
        else
        {
            Debug.LogWarning($"[PlayStoreDeepLinkHandler] Path de deep link não reconhecido: {path}");
        }
    }

    // -----------------------------------------------------------------------
    // Handlers específicos
    // -----------------------------------------------------------------------

    /// <summary>
    /// Abre um jogo específico identificado pelo gameId.
    /// </summary>
    private void HandleGameDeepLink(string gameId)
    {
        if (string.IsNullOrEmpty(gameId))
        {
            Debug.LogWarning("[PlayStoreDeepLinkHandler] gameId vazio no deep link.");
            return;
        }

        if (debugMode)
            Debug.Log($"[PlayStoreDeepLinkHandler] Abrindo jogo via deep link: {gameId}");

        // Busca o jogo no banco de dados
        MiniGameData gameData = FindGameById(gameId);

        if (gameData == null)
        {
            Debug.LogWarning($"[PlayStoreDeepLinkHandler] Jogo não encontrado: {gameId}");
            return;
        }

        // Inicia a sessão de jogo via GameSessionManager
        if (sessionManager != null)
        {
            sessionManager.RequestStartGame(gameData);
            Debug.Log($"[PlayStoreDeepLinkHandler] Sessão iniciada para: {gameData.titulo}");
        }
        else
        {
            Debug.LogError("[PlayStoreDeepLinkHandler] GameSessionManager não configurado!");
        }
    }

    /// <summary>
    /// Abre o perfil de um jogador identificado pelo userId.
    /// </summary>
    private void HandleProfileDeepLink(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("[PlayStoreDeepLinkHandler] userId vazio no deep link.");
            return;
        }

        if (debugMode)
            Debug.Log($"[PlayStoreDeepLinkHandler] Abrindo perfil via deep link: {userId}");

        // Exibe o painel de perfil
        if (profilePanel != null)
        {
            profilePanel.SetActive(true);

            // Atualiza o texto com o userId (opcional)
            if (profileUserIdText != null)
                profileUserIdText.text = userId;
        }
        else
        {
            Debug.LogWarning("[PlayStoreDeepLinkHandler] profilePanel não configurado no Inspector.");
        }
    }

    // -----------------------------------------------------------------------
    // Geração de deep links (para compartilhamento)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Gera um deep link para compartilhar um jogo específico.
    /// Exemplo de uso: no botão "Compartilhar" da tela de resultado.
    /// </summary>
    public static string GetGameDeepLink(string gameId)
    {
        return $"{Scheme}{GamePath}{gameId}";
    }

    /// <summary>
    /// Gera um deep link para compartilhar um perfil.
    /// </summary>
    public static string GetProfileDeepLink(string userId)
    {
        return $"{Scheme}{ProfilePath}{userId}";
    }

    /// <summary>
    /// Compartilha um jogo usando o sistema nativo de compartilhamento do Android/iOS.
    /// </summary>
    public void ShareGame(string gameId, string gameTitle)
    {
        string deepLink = GetGameDeepLink(gameId);
        string shareText = $"Joguei {gameTitle} no Game Scroller! 🎮\n{deepLink}";

        if (debugMode)
        {
            Debug.Log($"[PlayStoreDeepLinkHandler] [Debug] Compartilhando: {shareText}");
            return;
        }

#if UNITY_ANDROID || UNITY_IOS
        // Compartilhamento nativo via plugin NativeShare (opcional):
        // new NativeShare()
        //     .SetText(shareText)
        //     .Share();
        //
        // Ou via Intent Android manual:
        Debug.Log($"[PlayStoreDeepLinkHandler] Compartilhando: {shareText}");
#endif
    }

    // -----------------------------------------------------------------------
    // Busca de dados de jogo
    // -----------------------------------------------------------------------

    private MiniGameData FindGameById(string gameId)
    {
        if (allGames == null || allGames.Count == 0)
        {
            Debug.LogWarning("[PlayStoreDeepLinkHandler] Lista de jogos não configurada no Inspector.");
            return null;
        }

        foreach (var game in allGames)
        {
            if (game != null && game.id == gameId)
                return game;
        }

        return null;
    }
}
