/*
 * SpaceShooterGame.cs — Minijogo Nave x Aliens completo
 * Propósito: Nave na parte inferior move lateralmente, dispara automaticamente.
 *            Aliens descem em formação, aceleram com o tempo. Wave system progressivo.
 * Como usar: Adicione ao GameObject raiz da cena de Space Shooter. Configure prefabs no Inspector.
 * Dependências: MiniGameBase.cs, LifeSystem.cs, NotificationSystem.cs
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpaceShooterGame : MiniGameBase
{
    // -----------------------------------------------------------------------
    // Nave do jogador
    // -----------------------------------------------------------------------

    [Header("Nave do Jogador")]
    [SerializeField] private Transform shipTransform;
    [SerializeField] private float shipSpeed        = 6f;
    [SerializeField] private float shipMinX         = -4f;
    [SerializeField] private float shipMaxX         = 4f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate         = 0.4f;
    [SerializeField] private Transform firePoint;

    // -----------------------------------------------------------------------
    // Aliens
    // -----------------------------------------------------------------------

    [Header("Aliens")]
    [SerializeField] private GameObject alienPrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private int aliensPerRow    = 6;
    [SerializeField] private int alienRows       = 3;
    [SerializeField] private float alienSpacingX = 1.2f;
    [SerializeField] private float alienSpacingY = 0.9f;
    [SerializeField] private float alienStartY   = 3f;
    [SerializeField] private float alienDescentSpeed = 0.5f;
    [SerializeField] private float alienHorizontalSpeed = 1f;
    [SerializeField] private float bombDropInterval = 3f;

    // -----------------------------------------------------------------------
    // UI
    // -----------------------------------------------------------------------

    [Header("UI")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text waveText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private Button retryButton;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private int currentWave = 1;
    private float nextFireTime = 0f;
    private Vector2 dragStartPos;
    private bool isDragging = false;
    private float shipTargetX;

    private List<GameObject> aliens  = new List<GameObject>();
    private List<GameObject> bullets = new List<GameObject>();
    private List<GameObject> bombs   = new List<GameObject>();

    private float alienDirectionX = 1f;
    private Coroutine bombCoroutine;
    private float waveSpeedMultiplier = 1f;

    private const int PointsPerAlien = 10;
    private const int WaveBonus     = 50;

    // -----------------------------------------------------------------------
    // MiniGameBase
    // -----------------------------------------------------------------------

    public override void StartGame()
    {
        ResetGame();
        BeginGame();
        SpawnWave(1);
        bombCoroutine = StartCoroutine(AlienBombRoutine());
        Debug.Log("[SpaceShooterGame] Jogo iniciado.");
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

    // Game over é disparado via Die() → TriggerGameOver(); este override é intencional vazio.
    protected override void OnPlayerDied() { }

    // -----------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------

    private void Update()
    {
        if (!IsPlaying || IsPaused) return;

        HandleShipInput();
        MoveShip();
        HandleFiring();
        MoveAliens();
        MoveBullets();
        MoveBombs();
        CheckBulletAlienCollisions();
        CheckBombShipCollision();

        // Próxima wave ao eliminar todos os aliens
        if (aliens.Count == 0 && IsPlaying)
            StartCoroutine(NextWaveRoutine());
    }

    // -----------------------------------------------------------------------
    // Input da nave — arrastar
    // -----------------------------------------------------------------------

    private void HandleShipInput()
    {
        // Mouse (editor)
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPos = Input.mousePosition;
            isDragging   = true;
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            float delta = (Input.mousePosition.x - dragStartPos.x) / Screen.width * 8f;
            shipTargetX = Mathf.Clamp(shipTargetX + delta * Time.deltaTime * 3f, shipMinX, shipMaxX);
            dragStartPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // Touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                dragStartPos = touch.position;
                isDragging   = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                float delta = touch.deltaPosition.x / Screen.width * 8f;
                shipTargetX = Mathf.Clamp(shipTargetX + delta, shipMinX, shipMaxX);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
            }
        }
    }

    private void MoveShip()
    {
        if (shipTransform == null) return;

        float currentX = shipTransform.position.x;
        float newX = Mathf.MoveTowards(currentX, shipTargetX, shipSpeed * Time.deltaTime);
        shipTransform.position = new Vector3(newX, shipTransform.position.y, 0);
    }

    // -----------------------------------------------------------------------
    // Disparo
    // -----------------------------------------------------------------------

    private void HandleFiring()
    {
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + fireRate;
        FireBullet();
    }

    private void FireBullet()
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : shipTransform.position + Vector3.up * 0.5f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullets.Add(bullet);
    }

    // -----------------------------------------------------------------------
    // Movimento dos projéteis
    // -----------------------------------------------------------------------

    private void MoveBullets()
    {
        float bulletSpeed = 10f;
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            if (bullets[i] == null) { bullets.RemoveAt(i); continue; }
            bullets[i].transform.position += Vector3.up * bulletSpeed * Time.deltaTime;

            if (bullets[i].transform.position.y > 6f)
            {
                Destroy(bullets[i]);
                bullets.RemoveAt(i);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Movimento dos aliens
    // -----------------------------------------------------------------------

    private void MoveAliens()
    {
        if (aliens.Count == 0) return;

        float speed = alienHorizontalSpeed * waveSpeedMultiplier;

        // Move horizontalmente
        foreach (var alien in aliens)
        {
            if (alien == null) continue;
            alien.transform.position += Vector3.right * alienDirectionX * speed * Time.deltaTime;
        }

        // Checa bordas para inverter direção e descer
        bool hitEdge = false;
        foreach (var alien in aliens)
        {
            if (alien == null) continue;
            if (alien.transform.position.x > 4.5f || alien.transform.position.x < -4.5f)
            {
                hitEdge = true;
                break;
            }
        }

        if (hitEdge)
        {
            alienDirectionX *= -1f;

            // Desce todos
            foreach (var alien in aliens)
            {
                if (alien != null)
                    alien.transform.position += Vector3.down * alienDescentSpeed;
            }

            // Game over se chegou muito baixo
            foreach (var alien in aliens)
            {
                if (alien != null && alien.transform.position.y < -3.5f)
                {
                    Die();
                    return;
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // Bombas dos aliens
    // -----------------------------------------------------------------------

    private IEnumerator AlienBombRoutine()
    {
        while (IsPlaying && !IsPaused)
        {
            yield return new WaitForSeconds(bombDropInterval / waveSpeedMultiplier);

            if (aliens.Count > 0 && bombPrefab != null)
            {
                int randomIndex = Random.Range(0, aliens.Count);
                if (aliens[randomIndex] != null)
                {
                    GameObject bomb = Instantiate(bombPrefab, aliens[randomIndex].transform.position, Quaternion.identity);
                    bombs.Add(bomb);
                }
            }
        }
    }

    private void MoveBombs()
    {
        float bombSpeed = 4f;
        for (int i = bombs.Count - 1; i >= 0; i--)
        {
            if (bombs[i] == null) { bombs.RemoveAt(i); continue; }
            bombs[i].transform.position += Vector3.down * bombSpeed * Time.deltaTime;

            if (bombs[i].transform.position.y < -6f)
            {
                Destroy(bombs[i]);
                bombs.RemoveAt(i);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Colisões
    // -----------------------------------------------------------------------

    private void CheckBulletAlienCollisions()
    {
        for (int b = bullets.Count - 1; b >= 0; b--)
        {
            if (bullets[b] == null) { bullets.RemoveAt(b); continue; }

            for (int a = aliens.Count - 1; a >= 0; a--)
            {
                if (aliens[a] == null) { aliens.RemoveAt(a); continue; }

                float dist = Vector3.Distance(bullets[b].transform.position, aliens[a].transform.position);
                if (dist < 0.6f)
                {
                    Destroy(bullets[b]);
                    bullets.RemoveAt(b);
                    Destroy(aliens[a]);
                    aliens.RemoveAt(a);

                    AddScore(PointsPerAlien);
                    UpdateScoreUI();
                    break;
                }
            }
        }
    }

    private void CheckBombShipCollision()
    {
        if (shipTransform == null) return;

        for (int i = bombs.Count - 1; i >= 0; i--)
        {
            if (bombs[i] == null) { bombs.RemoveAt(i); continue; }

            float dist = Vector3.Distance(bombs[i].transform.position, shipTransform.position);
            if (dist < 0.6f)
            {
                Destroy(bombs[i]);
                bombs.RemoveAt(i);
                Die();
                return;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Waves
    // -----------------------------------------------------------------------

    private void SpawnWave(int wave)
    {
        currentWave     = wave;
        waveSpeedMultiplier = 1f + (wave - 1) * 0.2f;

        if (waveText != null)
            waveText.text = $"Wave {wave}";

        float startX = -(aliensPerRow * alienSpacingX) / 2f;

        for (int row = 0; row < alienRows; row++)
        {
            for (int col = 0; col < aliensPerRow; col++)
            {
                if (alienPrefab == null) continue;

                Vector3 pos = new Vector3(
                    startX + col * alienSpacingX,
                    alienStartY - row * alienSpacingY,
                    0
                );

                GameObject alien = Instantiate(alienPrefab, pos, Quaternion.identity);
                aliens.Add(alien);
            }
        }

        Debug.Log($"[SpaceShooterGame] Wave {wave} iniciada com {aliens.Count} aliens.");
    }

    private IEnumerator NextWaveRoutine()
    {
        IsPlaying = false; // pausa breve
        AddScore(WaveBonus);
        UpdateScoreUI();

        NotificationSystem.Show($"Wave {currentWave} completa! +{WaveBonus} pontos", NotificationSystem.NotificationType.Sucesso);
        yield return new WaitForSeconds(2f);

        IsPlaying = true;
        SpawnWave(currentWave + 1);
    }

    // -----------------------------------------------------------------------
    // Game Over
    // -----------------------------------------------------------------------

    private void Die()
    {
        if (!IsPlaying) return;

        TriggerGameOver();

        if (bombCoroutine != null)
            StopCoroutine(bombCoroutine);

        ShowGameOverUI();
    }

    private void ShowGameOverUI()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);

        if (gameOverScoreText != null)
            gameOverScoreText.text = $"Score: {CurrentScore}\nWave: {currentWave}";

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() =>
            {
                gameOverPanel.SetActive(false);
                StartGame();
            });
        }
    }

    // -----------------------------------------------------------------------
    // UI
    // -----------------------------------------------------------------------

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {CurrentScore}";
    }

    // -----------------------------------------------------------------------
    // Reset
    // -----------------------------------------------------------------------

    private void ResetGame()
    {
        foreach (var a in aliens)  if (a != null) Destroy(a);
        foreach (var b in bullets) if (b != null) Destroy(b);
        foreach (var b in bombs)   if (b != null) Destroy(b);

        aliens.Clear();
        bullets.Clear();
        bombs.Clear();

        alienDirectionX = 1f;
        currentWave = 1;
        waveSpeedMultiplier = 1f;
        nextFireTime = 0f;

        if (shipTransform != null)
        {
            shipTargetX = 0f;
            shipTransform.position = new Vector3(0, -4f, 0);
        }

        UpdateScoreUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
}
