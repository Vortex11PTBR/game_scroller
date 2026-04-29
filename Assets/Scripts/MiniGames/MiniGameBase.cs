/*
 * MiniGameBase.cs — Classe base abstrata para todos os minijogos
 * Propósito: Define o contrato que todos os minijogos devem seguir, gerenciando ciclo de vida,
 *            score, eventos e integração com LifeSystem/GameEconomy.
 * Como usar: Herde desta classe em FlappyBirdGame, TetrisGame, etc. Implemente os métodos abstratos.
 * Dependências: GameData.cs, LifeSystem.cs, GameEconomy.cs
 */

using System;
using UnityEngine;

public abstract class MiniGameBase : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Dados do jogo
    // -----------------------------------------------------------------------

    public MiniGameData Data { get; set; }

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    public int CurrentScore { get; protected set; }
    public bool IsPlaying   { get; protected set; }
    public bool IsPaused    { get; protected set; }

    // -----------------------------------------------------------------------
    // Eventos públicos
    // -----------------------------------------------------------------------

    public event Action<int> OnScoreChanged;   // score atual
    public event Action<int> OnGameOver;        // score final
    public event Action OnGameStarted;

    // -----------------------------------------------------------------------
    // Referências opcionais para integração com sistemas do jogo
    // -----------------------------------------------------------------------

    [Header("Integração de Sistemas")]
    [SerializeField] protected LifeSystem lifeSystem;
    [SerializeField] protected GameEconomy gameEconomy;
    [SerializeField] protected int coinsPerScorePoint = 1; // moedas por ponto de score

    // -----------------------------------------------------------------------
    // Métodos abstratos — cada jogo deve implementar
    // -----------------------------------------------------------------------

    public abstract void StartGame();
    public abstract void PauseGame();
    public abstract void ResumeGame();
    protected abstract void OnPlayerDied();

    // -----------------------------------------------------------------------
    // Implementação comum
    // -----------------------------------------------------------------------

    protected virtual void Awake()
    {
        // Tenta encontrar sistemas se não forem injetados via Inspector
        if (lifeSystem == null)
            lifeSystem = FindObjectOfType<LifeSystem>();

        if (gameEconomy == null)
            gameEconomy = FindObjectOfType<GameEconomy>();
    }

    // Incrementa o score e dispara evento
    protected void AddScore(int points)
    {
        if (!IsPlaying || points <= 0)
            return;

        CurrentScore += points;
        OnScoreChanged?.Invoke(CurrentScore);

        Debug.Log($"[{GetType().Name}] Score: {CurrentScore}");
    }

    // Define o score absoluto
    protected void SetScore(int score)
    {
        CurrentScore = Mathf.Max(0, score);
        OnScoreChanged?.Invoke(CurrentScore);
    }

    // Chamado quando o jogador perde — gasta vida e dispara evento de game over
    protected virtual void TriggerGameOver()
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;
        IsPaused  = false;

        Debug.Log($"[{GetType().Name}] Game Over! Score final: {CurrentScore}");

        // Gasta uma vida
        if (lifeSystem != null)
        {
            lifeSystem.SpendLife();
            Debug.Log($"[{GetType().Name}] Vida gasta. Vidas restantes: {lifeSystem.CurrentLives}");
        }

        // Calcula moedas ganhas e registra na economia
        int coinsEarned = CalculateCoinsEarned();
        if (coinsEarned > 0 && gameEconomy != null)
        {
            gameEconomy.WatchRewardedAd("score_reward", null);
            Debug.Log($"[{GetType().Name}] Moedas ganhas: {coinsEarned}");
        }

        OnGameOver?.Invoke(CurrentScore);
    }

    // Calcula quantas moedas o jogador ganha pelo score atual
    protected virtual int CalculateCoinsEarned()
    {
        return Mathf.FloorToInt(CurrentScore * coinsPerScorePoint * 0.1f);
    }

    // Inicia o estado interno do jogo
    protected void BeginGame()
    {
        CurrentScore = 0;
        IsPlaying    = true;
        IsPaused     = false;

        OnScoreChanged?.Invoke(CurrentScore);
        OnGameStarted?.Invoke();

        Debug.Log($"[{GetType().Name}] Jogo iniciado");
    }
}
