/*
 * FlappyBirdGame.cs — Minijogo Flappy Bird completo
 * Propósito: Implementa o Flappy Bird: pássaro com gravidade, geração procedural de canos,
 *            score, detecção de colisão e integração com LifeSystem/GameEconomy.
 * Como usar: Adicione a um GameObject raiz na cena do Flappy Bird. Configure os prefabs no Inspector.
 * Dependências: MiniGameBase.cs, LifeSystem.cs, GameEconomy.cs, GameSessionManager.cs
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlappyBirdGame : MiniGameBase
{
    // -----------------------------------------------------------------------
    // Configuração do pássaro
    // -----------------------------------------------------------------------

    [Header("Pássaro")]
    [SerializeField] private Transform birdTransform;
    [SerializeField] private float jumpForce    = 6f;
    [SerializeField] private float gravity      = -15f;
    [SerializeField] private float maxFallSpeed = -10f;
    [SerializeField] private float birdRotationSpeed = 5f;

    // -----------------------------------------------------------------------
    // Configuração dos canos
    // -----------------------------------------------------------------------

    [Header("Canos")]
    [SerializeField] private GameObject pipePrefab;        // prefab com dois sprites (cima/baixo)
    [SerializeField] private float pipeSpawnInterval  = 1.8f;
    [SerializeField] private float pipeSpeed          = 3f;
    [SerializeField] private float pipeMinGap         = 2.5f;
    [SerializeField] private float pipeMaxGap         = 4f;
    [SerializeField] private float pipeMinY           = -2f;
    [SerializeField] private float pipeMaxY           = 2f;
    [SerializeField] private float pipeSpawnX         = 8f;
    [SerializeField] private float pipeDespawnX       = -10f;

    // -----------------------------------------------------------------------
    // UI
    // -----------------------------------------------------------------------

    [Header("UI do Jogo")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private Text gameOverBestText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private GameObject tapToStartPanel;

    // -----------------------------------------------------------------------
    // Estado interno
    // -----------------------------------------------------------------------

    private float birdVelocity  = 0f;
    private bool  isAlive       = true;
    private bool  waitingForTap = true;

    private List<GameObject> activePipes = new List<GameObject>();
    private Coroutine spawnCoroutine;

    // Pontuação por cano passado
    private const int ScorePerPipe = 1;

    // -----------------------------------------------------------------------
    // MiniGameBase — implementação
    // -----------------------------------------------------------------------

    public override void StartGame()
    {
        ResetGame();
        BeginGame();

        waitingForTap = true;

        if (tapToStartPanel != null)
            tapToStartPanel.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Debug.Log("[FlappyBirdGame] Jogo iniciado. Aguardando primeiro toque.");
    }

    public override void PauseGame()
    {
        if (!IsPlaying) return;
        IsPaused = true;
        Time.timeScale = 0f;
        Debug.Log("[FlappyBirdGame] Jogo pausado.");
    }

    public override void ResumeGame()
    {
        if (!IsPlaying) return;
        IsPaused = false;
        Time.timeScale = 1f;
        Debug.Log("[FlappyBirdGame] Jogo retomado.");
    }

    protected override void OnPlayerDied()
    {
        // Chamado internamente via TriggerGameOver()
    }

    // -----------------------------------------------------------------------
    // Unity Update
    // -----------------------------------------------------------------------

    private void Update()
    {
        if (!IsPlaying || IsPaused)
            return;

        HandleInput();

        if (!waitingForTap)
        {
            ApplyPhysics();
            CheckBounds();
        }

        UpdateBirdRotation();
    }

    // -----------------------------------------------------------------------
    // Input
    // -----------------------------------------------------------------------

    private void HandleInput()
    {
        bool tapped = Input.GetMouseButtonDown(0) ||
                      (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (!tapped) return;

        if (waitingForTap)
        {
            waitingForTap = false;

            if (tapToStartPanel != null)
                tapToStartPanel.SetActive(false);

            spawnCoroutine = StartCoroutine(SpawnPipes());
        }

        Jump();
    }

    private void Jump()
    {
        birdVelocity = jumpForce;
    }

    // -----------------------------------------------------------------------
    // Física do pássaro
    // -----------------------------------------------------------------------

    private void ApplyPhysics()
    {
        birdVelocity += gravity * Time.deltaTime;
        birdVelocity  = Mathf.Max(birdVelocity, maxFallSpeed);

        if (birdTransform != null)
            birdTransform.position += Vector3.up * birdVelocity * Time.deltaTime;
    }

    private void UpdateBirdRotation()
    {
        if (birdTransform == null) return;

        float targetAngle = Mathf.Clamp(birdVelocity * 8f, -90f, 30f);
        Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);
        birdTransform.rotation = Quaternion.Lerp(birdTransform.rotation, targetRot,
            birdRotationSpeed * Time.deltaTime);
    }

    private void CheckBounds()
    {
        if (birdTransform == null) return;

        // Morreu ao sair da tela (chão ou teto)
        if (birdTransform.position.y < -5f || birdTransform.position.y > 5f)
            Die();
    }

    // -----------------------------------------------------------------------
    // Geração de canos
    // -----------------------------------------------------------------------

    private IEnumerator SpawnPipes()
    {
        while (IsPlaying && !IsPaused)
        {
            SpawnPipePair();
            yield return new WaitForSeconds(pipeSpawnInterval);
        }
    }

    private void SpawnPipePair()
    {
        if (pipePrefab == null) return;

        float gap    = Random.Range(pipeMinGap, pipeMaxGap);
        float centerY = Random.Range(pipeMinY, pipeMaxY);

        GameObject pipe = Instantiate(pipePrefab);
        pipe.transform.position = new Vector3(pipeSpawnX, centerY, 0);

        PipeController controller = pipe.GetComponent<PipeController>();
        if (controller == null) controller = pipe.AddComponent<PipeController>();

        controller.Initialize(gap, pipeSpeed, pipeDespawnX, OnBirdPassedPipe, this);

        activePipes.Add(pipe);
    }

    private void OnBirdPassedPipe()
    {
        AddScore(ScorePerPipe);
        UpdateScoreUI();
    }

    // -----------------------------------------------------------------------
    // Colisão
    // -----------------------------------------------------------------------

    // Chamado pelo PipeController quando detecta sobreposição com o pássaro
    public void OnPipeCollision()
    {
        Die();
    }

    private void Die()
    {
        if (!isAlive) return;

        isAlive = false;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        TriggerGameOver();
        ShowGameOverUI();

        Debug.Log("[FlappyBirdGame] Pássaro morreu!");
    }

    // -----------------------------------------------------------------------
    // UI
    // -----------------------------------------------------------------------

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = CurrentScore.ToString();
    }

    private void ShowGameOverUI()
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        if (gameOverScoreText != null)
            gameOverScoreText.text = $"Score: {CurrentScore}";

        int highScore = GameManager.Instance?.ProfileManager?.CurrentProfile?.GetHighScore("flappy_bird") ?? 0;

        if (gameOverBestText != null)
            gameOverBestText.text = $"Melhor: {Mathf.Max(CurrentScore, highScore)}";

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryPressed);
            retryButton.interactable = GameManager.Instance?.HasLives() ?? true;
        }

        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(OnWatchAdPressed);
        }
    }

    private void OnRetryPressed()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        StartGame();
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
                OnRetryPressed();
            }
        });
    }

    // -----------------------------------------------------------------------
    // Reset
    // -----------------------------------------------------------------------

    private void ResetGame()
    {
        isAlive   = true;
        birdVelocity = 0f;

        // Remove canos existentes
        foreach (GameObject pipe in activePipes)
        {
            if (pipe != null) Destroy(pipe);
        }
        activePipes.Clear();

        // Reposiciona pássaro
        if (birdTransform != null)
            birdTransform.position = new Vector3(-2f, 0f, 0f);

        UpdateScoreUI();

        if (highScoreText != null)
        {
            int hs = GameManager.Instance?.ProfileManager?.CurrentProfile?.GetHighScore("flappy_bird") ?? 0;
            highScoreText.text = $"Melhor: {hs}";
        }
    }
}

// ---------------------------------------------------------------------------
// Controlador de par de canos
// ---------------------------------------------------------------------------

public class PipeController : MonoBehaviour
{
    private float speed;
    private float despawnX;
    private System.Action onPassed;
    private bool scored = false;

    // Referências cacheadas em Initialize (evita FindObjectOfType no Update)
    private Transform birdTransform;
    private FlappyBirdGame flappyGame;

    // Limites das pipes (calculados uma vez)
    private float topPipeBottom;
    private float bottomPipeTop;

    public void Initialize(float gap, float moveSpeed, float destroyX, System.Action onPassCallback, FlappyBirdGame game)
    {
        speed     = moveSpeed;
        despawnX  = destroyX;
        onPassed  = onPassCallback;
        flappyGame = game;

        // Ajusta filhos (top e bottom pipe)
        Transform top    = transform.Find("TopPipe");
        Transform bottom = transform.Find("BottomPipe");

        if (top != null)    top.localPosition    = new Vector3(0, gap / 2f + 5f, 0);
        if (bottom != null) bottom.localPosition = new Vector3(0, -(gap / 2f + 5f), 0);

        // Calcula limites para colisão
        topPipeBottom    = top    != null ? top.position.y    - 5f  : float.MaxValue;
        bottomPipeTop    = bottom != null ? bottom.position.y + 5f  : float.MinValue;

        birdTransform = GameObject.FindWithTag("Bird")?.transform;
    }

    private void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        // Marca ponto ao passar pelo pássaro
        if (!scored && birdTransform != null && transform.position.x < birdTransform.position.x)
        {
            scored = true;
            onPassed?.Invoke();
        }

        // Remove quando sair da tela
        if (transform.position.x < despawnX)
        {
            Destroy(gameObject);
            return;
        }

        // Detecção de colisão simples (bounding box)
        CheckBirdCollision();
    }

    private void CheckBirdCollision()
    {
        if (birdTransform == null || flappyGame == null) return;

        float distX = Mathf.Abs(transform.position.x - birdTransform.position.x);
        if (distX > 1.2f) return; // não está perto horizontalmente

        float birdY = birdTransform.position.y;

        if (birdY > topPipeBottom || birdY < bottomPipeTop)
            flappyGame.OnPipeCollision();
    }
}
