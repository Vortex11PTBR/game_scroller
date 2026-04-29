/*
 * GameManager.cs — Singleton central do Game Scroller
 * Propósito: Ponto de acesso global a todos os sistemas do jogo. Persiste entre cenas.
 * Como usar: Adicione a um GameObject vazio na cena inicial. Configure as referências no Inspector.
 * Dependências: LifeSystem, GameEconomy, PlayerProfileManager, AdManager, NotificationSystem,
 *               GameSessionManager
 */

using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Singleton
    // -----------------------------------------------------------------------

    public static GameManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Referências para sistemas
    // -----------------------------------------------------------------------

    [Header("Sistemas Principais")]
    [SerializeField] private LifeSystem lifeSystem;
    [SerializeField] private GameEconomy gameEconomy;
    [SerializeField] private PlayerProfileManager profileManager;
    [SerializeField] private AdManager adManager;
    [SerializeField] private NotificationSystem notificationSystem;
    [SerializeField] private GameSessionManager sessionManager;

    // -----------------------------------------------------------------------
    // Eventos globais
    // -----------------------------------------------------------------------

    public static event Action<MiniGameData> OnGameStarted;
    public static event Action<int>          OnGameEnded;       // score final
    public static event Action<int>          OnCoinsChanged;    // novo total de moedas
    public static event Action<int>          OnLivesChanged;    // novas vidas

    // -----------------------------------------------------------------------
    // Propriedades de acesso
    // -----------------------------------------------------------------------

    public LifeSystem          LifeSystem        => lifeSystem;
    public GameEconomy         GameEconomy       => gameEconomy;
    public PlayerProfileManager ProfileManager   => profileManager;
    public AdManager           AdManager         => adManager;
    public NotificationSystem  NotificationSystem => notificationSystem;
    public GameSessionManager  SessionManager    => sessionManager;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        // Garante que só existe uma instância
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSystems();
    }

    private void InitializeSystems()
    {
        Debug.Log("[GameManager] Inicializando sistemas...");

        // Resolve referências ausentes nos filhos
        if (lifeSystem == null)
            lifeSystem = GetComponentInChildren<LifeSystem>();

        if (gameEconomy == null)
            gameEconomy = GetComponentInChildren<GameEconomy>();

        if (profileManager == null)
            profileManager = GetComponentInChildren<PlayerProfileManager>();

        if (adManager == null)
            adManager = GetComponentInChildren<AdManager>();

        if (notificationSystem == null)
            notificationSystem = GetComponentInChildren<NotificationSystem>();

        if (sessionManager == null)
            sessionManager = GetComponentInChildren<GameSessionManager>();

        Debug.Log("[GameManager] Sistemas inicializados com sucesso.");
    }

    // -----------------------------------------------------------------------
    // Métodos de disparo de eventos globais
    // -----------------------------------------------------------------------

    public static void NotifyGameStarted(MiniGameData data)
    {
        OnGameStarted?.Invoke(data);
    }

    public static void NotifyGameEnded(int finalScore)
    {
        OnGameEnded?.Invoke(finalScore);
    }

    public static void NotifyCoinsChanged(int newTotal)
    {
        OnCoinsChanged?.Invoke(newTotal);
    }

    public static void NotifyLivesChanged(int newTotal)
    {
        OnLivesChanged?.Invoke(newTotal);
    }

    // -----------------------------------------------------------------------
    // Utilitário — verifica se há vidas disponíveis
    // -----------------------------------------------------------------------

    public bool HasLives()
    {
        if (lifeSystem == null)
            return true; // seguro: permite jogar se sistema não estiver configurado

        lifeSystem.RefreshLives();
        return lifeSystem.HasLives;
    }
}
