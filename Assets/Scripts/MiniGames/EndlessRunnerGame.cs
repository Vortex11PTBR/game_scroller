/*
 * EndlessRunnerGame.cs — Minijogo Endless Runner estilo Mario
 * Propósito: Personagem corre automaticamente, tap/swipe para pular, obstáculos procedurais,
 *            moedas no caminho, velocidade crescente, score por distância.
 * Como usar: Adicione ao GameObject raiz da cena do Runner. Configure prefabs no Inspector.
 * Dependências: MiniGameBase.cs, LifeSystem.cs, NotificationSystem.cs
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndlessRunnerGame : MiniGameBase
{
    // -----------------------------------------------------------------------
    // Personagem
    // -----------------------------------------------------------------------

    [Header("Personagem")]
    [SerializeField] private Transform characterTransform;
    [SerializeField] private float jumpForce       = 9f;
    [SerializeField] private float gravity         = -20f;
    [SerializeField] private float groundY         = -3f;
    [SerializeField] private float doubleJumpMult  = 0.7f;

    // -----------------------------------------------------------------------
    // Mundo
    // -----------------------------------------------------------------------

    [Header("Obstáculos e Moedas")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private float spawnX          = 8f;
    [SerializeField] private float despawnX        = -9f;
    [SerializeField] private float minSpawnInterval = 0.8f;
    [SerializeField] private float maxSpawnInterval = 2f;
    [SerializeField] private float initialSpeed     = 4f;
    [SerializeField] private float speedIncrement   = 0.1f;  // por segundo
    [SerializeField] private float maxSpeed         = 12f;

    // -----------------------------------------------------------------------
    // UI
    // -----------------------------------------------------------------------

    [Header("UI")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text coinsCollectedText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button watchAdButton;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private float verticalVelocity = 0f;
    private bool  isGrounded       = true;
    private bool  hasDoubleJumped  = false;
    private float currentSpeed;
    private float distanceTraveled = 0f;
    private int   coinsCollected   = 0;

    private List<GameObject> activeObjects = new List<GameObject>();
    private Coroutine spawnCoroutine;

    // -----------------------------------------------------------------------
    // MiniGameBase
    // -----------------------------------------------------------------------

    public override void StartGame()
    {
        ResetGame();
        BeginGame();
        spawnCoroutine = StartCoroutine(SpawnRoutine());
        Debug.Log("[EndlessRunnerGame] Jogo iniciado.");
    }

    public override void PauseGame()
    {
        if (!IsPlaying) return;
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public override void ResumeGame()
    {
        if (!IsPlaying) return;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    protected override void OnPlayerDied() { }

    // -----------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------

    private void Update()
    {
        if (!IsPlaying || IsPaused) return;

        HandleInput();
        ApplyGravity();
        MoveWorldObjects();
        CheckObjectCollisions();
        UpdateSpeed();
        UpdateScore();
    }

    // -----------------------------------------------------------------------
    // Input
    // -----------------------------------------------------------------------

    private void HandleInput()
    {
        bool tapped = Input.GetMouseButtonDown(0) ||
                      (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (tapped)
            Jump();
    }

    private void Jump()
    {
        if (isGrounded)
        {
            verticalVelocity = jumpForce;
            isGrounded       = false;
            hasDoubleJumped  = false;
        }
        else if (!hasDoubleJumped)
        {
            verticalVelocity  = jumpForce * doubleJumpMult;
            hasDoubleJumped   = true;
        }
    }

    // -----------------------------------------------------------------------
    // Física
    // -----------------------------------------------------------------------

    private void ApplyGravity()
    {
        if (characterTransform == null) return;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 pos = characterTransform.position;
        pos.y += verticalVelocity * Time.deltaTime;

        if (pos.y <= groundY)
        {
            pos.y           = groundY;
            verticalVelocity = 0f;
            isGrounded       = true;
            hasDoubleJumped  = false;
        }

        characterTransform.position = pos;
    }

    // -----------------------------------------------------------------------
    // Mundo em movimento
    // -----------------------------------------------------------------------

    private void MoveWorldObjects()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            if (activeObjects[i] == null) { activeObjects.RemoveAt(i); continue; }

            activeObjects[i].transform.position += Vector3.left * currentSpeed * Time.deltaTime;

            if (activeObjects[i].transform.position.x < despawnX)
            {
                Destroy(activeObjects[i]);
                activeObjects.RemoveAt(i);
            }
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (IsPlaying)
        {
            float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(interval);

            if (!IsPlaying) break;

            // Spawna obstáculo ou moeda (70%/30%)
            if (Random.value < 0.7f)
                SpawnObstacle();
            else
                SpawnCoin();
        }
    }

    private void SpawnObstacle()
    {
        if (obstaclePrefab == null) return;

        GameObject obs = Instantiate(obstaclePrefab,
            new Vector3(spawnX, groundY + 0.5f, 0), Quaternion.identity);
        activeObjects.Add(obs);
    }

    private void SpawnCoin()
    {
        if (coinPrefab == null) return;

        float coinY = groundY + Random.Range(0.5f, 2.5f);
        GameObject coin = Instantiate(coinPrefab,
            new Vector3(spawnX, coinY, 0), Quaternion.identity);
        activeObjects.Add(coin);
    }

    // -----------------------------------------------------------------------
    // Colisões
    // -----------------------------------------------------------------------

    private void CheckObjectCollisions()
    {
        if (characterTransform == null) return;

        Vector3 charPos = characterTransform.position;
        const float collisionRadius = 0.4f;

        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            if (activeObjects[i] == null) { activeObjects.RemoveAt(i); continue; }

            float dist = Vector3.Distance(charPos, activeObjects[i].transform.position);
            if (dist > collisionRadius) continue;

            bool isCoin = activeObjects[i].CompareTag("Coin") ||
                          activeObjects[i].name.Contains("Coin");

            if (isCoin)
            {
                coinsCollected++;
                AddScore(5);
                Destroy(activeObjects[i]);
                activeObjects.RemoveAt(i);
                UpdateCoinUI();
            }
            else
            {
                // Colidiu com obstáculo
                Die();
                return;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Velocidade e score
    // -----------------------------------------------------------------------

    private void UpdateSpeed()
    {
        currentSpeed = Mathf.Min(currentSpeed + speedIncrement * Time.deltaTime, maxSpeed);
    }

    private void UpdateScore()
    {
        distanceTraveled += currentSpeed * Time.deltaTime;
        int newScore = Mathf.FloorToInt(distanceTraveled);

        if (newScore != CurrentScore)
        {
            SetScore(newScore);
            if (scoreText != null)
                scoreText.text = $"{CurrentScore}m";
        }
    }

    private void UpdateCoinUI()
    {
        if (coinsCollectedText != null)
            coinsCollectedText.text = $"🪙 {coinsCollected}";
    }

    // -----------------------------------------------------------------------
    // Game Over
    // -----------------------------------------------------------------------

    private void Die()
    {
        if (!IsPlaying) return;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        TriggerGameOver();
        ShowGameOverUI();

        Debug.Log($"[EndlessRunnerGame] Game Over! Distância: {distanceTraveled:F1}m, Moedas: {coinsCollected}");
    }

    private void ShowGameOverUI()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);

        int hs = GameManager.Instance?.ProfileManager?.CurrentProfile?.GetHighScore("endless_runner") ?? 0;

        if (gameOverScoreText != null)
            gameOverScoreText.text = $"Distância: {CurrentScore}m\nMoedas: {coinsCollected}\nMelhor: {hs}m";

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() =>
            {
                gameOverPanel.SetActive(false);
                StartGame();
            });
        }

        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnWatchAdPressed);
        }
    }

    private void OnWatchAdPressed()
    {
        AdManager ads = GameManager.Instance?.AdManager ?? FindObjectOfType<AdManager>();
        ads?.ShowRewardedAd(success =>
        {
            if (success)
            {
                GameManager.Instance?.LifeSystem?.AddRewardLives(1);
                GameManager.NotifyLivesChanged(GameManager.Instance?.LifeSystem?.CurrentLives ?? 0);
                NotificationSystem.Show("+1 Vida!", NotificationSystem.NotificationType.Sucesso);
                gameOverPanel?.SetActive(false);
                StartGame();
            }
        });
    }

    // -----------------------------------------------------------------------
    // Reset
    // -----------------------------------------------------------------------

    private void ResetGame()
    {
        foreach (var obj in activeObjects)
            if (obj != null) Destroy(obj);

        activeObjects.Clear();

        distanceTraveled = 0f;
        coinsCollected   = 0;
        currentSpeed     = initialSpeed;
        verticalVelocity = 0f;
        isGrounded       = true;
        hasDoubleJumped  = false;

        if (characterTransform != null)
            characterTransform.position = new Vector3(-3f, groundY, 0);

        if (scoreText != null)      scoreText.text      = "0m";
        if (coinsCollectedText != null) coinsCollectedText.text = "🪙 0";

        if (highScoreText != null)
        {
            int hs = GameManager.Instance?.ProfileManager?.CurrentProfile?.GetHighScore("endless_runner") ?? 0;
            highScoreText.text = $"Recorde: {hs}m";
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
}
