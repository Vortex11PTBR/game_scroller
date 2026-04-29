/*
 * TetrisGame.cs — Minijogo Tetris clássico completo
 * Propósito: Tetris com grid 10x20, 7 peças com rotação, swipe/tap para controles,
 *            eliminação de linhas, níveis, preview da próxima peça, Game Over.
 * Como usar: Adicione ao GameObject raiz da cena do Tetris. Configure prefabs no Inspector.
 * Dependências: MiniGameBase.cs, LifeSystem.cs
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TetrisGame : MiniGameBase
{
    // -----------------------------------------------------------------------
    // Configuração do Grid
    // -----------------------------------------------------------------------

    [Header("Grid")]
    [SerializeField] private int gridWidth  = 10;
    [SerializeField] private int gridHeight = 20;
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject cellPrefab;   // quadrado colorido
    [SerializeField] private float cellSize = 0.5f;

    [Header("Preview")]
    [SerializeField] private Transform previewParent;

    // -----------------------------------------------------------------------
    // UI
    // -----------------------------------------------------------------------

    [Header("UI")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text linesText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private Button retryButton;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private GameObject[,] grid;
    private Color[,] gridColors;

    private TetrisPiece currentPiece;
    private TetrisPieceType nextPieceType;

    private float fallTimer  = 0f;
    private float fallSpeed  = 1f;   // segundos entre quedas
    private int   level      = 1;
    private int   linesCleared = 0;
    private const int LinesPerLevel = 10;

    private Coroutine gameCoroutine;

    // Swipe detection
    private Vector2 touchStart;
    private bool    touchStarted;

    // -----------------------------------------------------------------------
    // Peças do Tetris — formas clássicas
    // -----------------------------------------------------------------------

    private enum TetrisPieceType { I, O, T, S, Z, J, L }

    private static readonly int[,,] Shapes = new int[,,]
    {
        // I
        { {1,1,1,1}, {0,0,0,0}, {0,0,0,0}, {0,0,0,0} },
        // O
        { {1,1,0,0}, {1,1,0,0}, {0,0,0,0}, {0,0,0,0} },
        // T
        { {0,1,0,0}, {1,1,1,0}, {0,0,0,0}, {0,0,0,0} },
        // S
        { {0,1,1,0}, {1,1,0,0}, {0,0,0,0}, {0,0,0,0} },
        // Z
        { {1,1,0,0}, {0,1,1,0}, {0,0,0,0}, {0,0,0,0} },
        // J
        { {1,0,0,0}, {1,1,1,0}, {0,0,0,0}, {0,0,0,0} },
        // L
        { {0,0,1,0}, {1,1,1,0}, {0,0,0,0}, {0,0,0,0} }
    };

    private static readonly Color[] PieceColors = new Color[]
    {
        new Color(0f,   0.9f, 0.9f),  // I — ciano
        new Color(0.9f, 0.9f, 0f),    // O — amarelo
        new Color(0.6f, 0f,   0.9f),  // T — roxo
        new Color(0f,   0.9f, 0f),    // S — verde
        new Color(0.9f, 0f,   0f),    // Z — vermelho
        new Color(0f,   0f,   0.9f),  // J — azul
        new Color(0.9f, 0.5f, 0f)     // L — laranja
    };

    // -----------------------------------------------------------------------
    // MiniGameBase
    // -----------------------------------------------------------------------

    public override void StartGame()
    {
        ResetGame();
        BeginGame();

        nextPieceType = RandomPieceType();
        SpawnNextPiece();

        gameCoroutine = StartCoroutine(GameLoop());
        Debug.Log("[TetrisGame] Jogo iniciado.");
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
    // Loop principal
    // -----------------------------------------------------------------------

    private IEnumerator GameLoop()
    {
        while (IsPlaying)
        {
            yield return null;

            if (IsPaused) continue;

            fallTimer += Time.deltaTime;

            if (fallTimer >= fallSpeed)
            {
                fallTimer = 0f;
                MoveDown();
            }

            HandleInput();
        }
    }

    // -----------------------------------------------------------------------
    // Input
    // -----------------------------------------------------------------------

    private void HandleInput()
    {
        // Teclado (editor)
        if (Input.GetKeyDown(KeyCode.LeftArrow))  MoveLeft();
        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveRight();
        if (Input.GetKeyDown(KeyCode.UpArrow))    RotatePiece();
        if (Input.GetKeyDown(KeyCode.DownArrow))  MoveDown();
        if (Input.GetKeyDown(KeyCode.Space))      HardDrop();

        // Touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStart   = touch.position;
                touchStarted = true;
            }
            else if (touch.phase == TouchPhase.Ended && touchStarted)
            {
                touchStarted = false;
                Vector2 delta = touch.position - touchStart;

                if (delta.magnitude < 20f)
                {
                    // Tap = rotacionar
                    RotatePiece();
                }
                else if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    // Swipe horizontal
                    if (delta.x > 0) MoveRight();
                    else             MoveLeft();
                }
                else if (delta.y < -40f)
                {
                    // Swipe down = hard drop
                    HardDrop();
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // Movimentos da peça
    // -----------------------------------------------------------------------

    private void MoveLeft()
    {
        if (currentPiece == null) return;
        currentPiece.x--;
        if (!IsValidPosition(currentPiece))
            currentPiece.x++;
        else
            RedrawCurrentPiece();
    }

    private void MoveRight()
    {
        if (currentPiece == null) return;
        currentPiece.x++;
        if (!IsValidPosition(currentPiece))
            currentPiece.x--;
        else
            RedrawCurrentPiece();
    }

    private void MoveDown()
    {
        if (currentPiece == null) return;
        currentPiece.y--;
        if (!IsValidPosition(currentPiece))
        {
            currentPiece.y++;
            LockPiece();
        }
        else
        {
            RedrawCurrentPiece();
        }
    }

    private void HardDrop()
    {
        if (currentPiece == null) return;
        while (IsValidPosition(currentPiece))
            currentPiece.y--;

        currentPiece.y++;
        LockPiece();
    }

    private void RotatePiece()
    {
        if (currentPiece == null) return;

        int prevRotation = currentPiece.rotation;
        currentPiece.rotation = (currentPiece.rotation + 1) % 4;

        if (!IsValidPosition(currentPiece))
        {
            // Wall kick simples
            currentPiece.x++;
            if (!IsValidPosition(currentPiece))
            {
                currentPiece.x -= 2;
                if (!IsValidPosition(currentPiece))
                {
                    currentPiece.x++;
                    currentPiece.rotation = prevRotation;
                    return;
                }
            }
        }

        RedrawCurrentPiece();
    }

    // -----------------------------------------------------------------------
    // Locking e linhas
    // -----------------------------------------------------------------------

    private void LockPiece()
    {
        if (currentPiece == null) return;

        bool[,] shape = GetRotatedShape(currentPiece);
        Color color   = PieceColors[(int)currentPiece.type];

        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                if (!shape[r, c]) continue;

                int gx = currentPiece.x + c;
                int gy = currentPiece.y + r;

                if (gy >= gridHeight)
                {
                    // Overflow — game over
                    GameOver();
                    return;
                }

                // Preenche o grid
                if (gx >= 0 && gx < gridWidth && gy >= 0)
                {
                    gridColors[gy, gx] = color;
                    PlaceCellVisual(gx, gy, color);
                }
            }
        }

        // Destrói visual da peça em movimento
        currentPiece.DestroyVisuals();

        // Verifica linhas completas
        CheckLines();

        // Spawna próxima peça
        SpawnNextPiece();
    }

    private void CheckLines()
    {
        int cleared = 0;

        for (int y = gridHeight - 1; y >= 0; y--)
        {
            if (IsLineFull(y))
            {
                ClearLine(y);
                DropLinesAbove(y);
                y++;  // re-verifica mesma linha
                cleared++;
            }
        }

        if (cleared == 0) return;

        linesCleared += cleared;

        int points = cleared switch
        {
            1 => 100 * level,
            2 => 300 * level,
            3 => 500 * level,
            4 => 800 * level,
            _ => 100 * level
        };

        AddScore(points);
        UpdateUI();

        // Sobe de nível a cada 10 linhas
        int newLevel = (linesCleared / LinesPerLevel) + 1;
        if (newLevel > level)
        {
            level = newLevel;
            fallSpeed = Mathf.Max(0.1f, 1f - (level - 1) * 0.1f);
            NotificationSystem.Show($"Nível {level}! 🚀", NotificationSystem.NotificationType.Sucesso);
        }
    }

    private bool IsLineFull(int y)
    {
        for (int x = 0; x < gridWidth; x++)
            if (gridColors[y, x] == Color.clear)
                return false;

        return true;
    }

    private void ClearLine(int y)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            if (grid[y, x] != null)
            {
                Destroy(grid[y, x]);
                grid[y, x] = null;
            }

            gridColors[y, x] = Color.clear;
        }
    }

    private void DropLinesAbove(int clearedY)
    {
        for (int y = clearedY; y < gridHeight - 1; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                grid[y, x]       = grid[y + 1, x];
                gridColors[y, x] = gridColors[y + 1, x];

                if (grid[y, x] != null)
                    grid[y, x].transform.position = GridToWorld(x, y);
            }
        }

        // Limpa última linha
        for (int x = 0; x < gridWidth; x++)
        {
            grid[gridHeight - 1, x]       = null;
            gridColors[gridHeight - 1, x] = Color.clear;
        }
    }

    // -----------------------------------------------------------------------
    // Visual
    // -----------------------------------------------------------------------

    private void RedrawCurrentPiece()
    {
        if (currentPiece == null) return;

        currentPiece.DestroyVisuals();

        bool[,] shape = GetRotatedShape(currentPiece);
        Color color   = PieceColors[(int)currentPiece.type];

        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                if (!shape[r, c]) continue;

                int gx = currentPiece.x + c;
                int gy = currentPiece.y + r;

                if (gx < 0 || gx >= gridWidth || gy < 0 || gy >= gridHeight) continue;

                Vector3 worldPos = GridToWorld(gx, gy);
                GameObject cell  = CreateCell(worldPos, color, gridParent);
                currentPiece.visuals.Add(cell);
            }
        }
    }

    private void PlaceCellVisual(int gx, int gy, Color color)
    {
        if (gx < 0 || gx >= gridWidth || gy < 0 || gy >= gridHeight) return;

        Vector3 worldPos = GridToWorld(gx, gy);
        GameObject cell  = CreateCell(worldPos, color, gridParent);
        grid[gy, gx]     = cell;
    }

    private GameObject CreateCell(Vector3 pos, Color color, Transform parent)
    {
        if (cellPrefab == null)
        {
            // Cria um quadrado simples
            GameObject go = new GameObject("Cell");
            go.transform.SetParent(parent);
            go.transform.position = pos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = color;
            return go;
        }

        GameObject cell = Instantiate(cellPrefab, pos, Quaternion.identity, parent);
        SpriteRenderer cellSr = cell.GetComponent<SpriteRenderer>();
        if (cellSr != null) cellSr.color = color;
        return cell;
    }

    private Vector3 GridToWorld(int gx, int gy)
    {
        Vector3 origin = gridParent != null ? gridParent.position : Vector3.zero;
        return origin + new Vector3(gx * cellSize, gy * cellSize, 0);
    }

    // -----------------------------------------------------------------------
    // Spawn de peças
    // -----------------------------------------------------------------------

    private void SpawnNextPiece()
    {
        TetrisPieceType type = nextPieceType;
        nextPieceType = RandomPieceType();

        currentPiece = new TetrisPiece
        {
            type     = type,
            x        = gridWidth / 2 - 2,
            y        = gridHeight - 4,
            rotation = 0
        };

        if (!IsValidPosition(currentPiece))
        {
            GameOver();
            return;
        }

        RedrawCurrentPiece();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (previewParent == null) return;

        foreach (Transform child in previewParent)
            Destroy(child.gameObject);

        Color color      = PieceColors[(int)nextPieceType];
        TetrisPiece prev = new TetrisPiece { type = nextPieceType, x = 0, y = 0, rotation = 0 };
        bool[,] shape    = GetRotatedShape(prev);

        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                if (!shape[r, c]) continue;
                Vector3 pos = previewParent.position + new Vector3(c * cellSize, r * cellSize, 0);
                CreateCell(pos, color, previewParent);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Validação de posição
    // -----------------------------------------------------------------------

    private bool IsValidPosition(TetrisPiece piece)
    {
        bool[,] shape = GetRotatedShape(piece);

        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                if (!shape[r, c]) continue;

                int gx = piece.x + c;
                int gy = piece.y + r;

                if (gx < 0 || gx >= gridWidth || gy < 0)
                    return false;

                if (gy < gridHeight && gridColors[gy, gx] != Color.clear)
                    return false;
            }
        }

        return true;
    }

    // -----------------------------------------------------------------------
    // Rotação
    // -----------------------------------------------------------------------

    private bool[,] GetRotatedShape(TetrisPiece piece)
    {
        int typeIndex = (int)piece.type;
        bool[,] shape = new bool[4, 4];

        // Forma original
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
                shape[r, c] = Shapes[typeIndex, r, c] == 1;

        // Rotaciona N vezes
        for (int rot = 0; rot < piece.rotation; rot++)
            shape = RotateMatrix(shape);

        return shape;
    }

    private bool[,] RotateMatrix(bool[,] m)
    {
        int n = m.GetLength(0);
        bool[,] result = new bool[n, n];

        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                result[c, n - 1 - r] = m[r, c];

        return result;
    }

    // -----------------------------------------------------------------------
    // Game Over
    // -----------------------------------------------------------------------

    private void GameOver()
    {
        TriggerGameOver();

        if (gameCoroutine != null)
            StopCoroutine(gameCoroutine);

        ShowGameOverUI();
        Debug.Log($"[TetrisGame] Game Over! Score: {CurrentScore}, Nível: {level}");
    }

    private void ShowGameOverUI()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);

        int hs = GameManager.Instance?.ProfileManager?.CurrentProfile?.GetHighScore("tetris") ?? 0;

        if (gameOverScoreText != null)
            gameOverScoreText.text = $"Score: {CurrentScore:N0}\nNível: {level}\nLinhas: {linesCleared}\nMelhor: {hs:N0}";

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

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"{CurrentScore:N0}";
        if (levelText != null) levelText.text = $"Nível {level}";
        if (linesText != null) linesText.text = $"{linesCleared} linhas";
    }

    // -----------------------------------------------------------------------
    // Reset
    // -----------------------------------------------------------------------

    private void ResetGame()
    {
        // Limpa grid visual
        if (grid != null)
        {
            for (int y = 0; y < gridHeight; y++)
                for (int x = 0; x < gridWidth; x++)
                    if (grid[y, x] != null) Destroy(grid[y, x]);
        }

        grid       = new GameObject[gridHeight, gridWidth];
        gridColors = new Color[gridHeight, gridWidth];

        // Inicializa com Color.clear
        for (int y = 0; y < gridHeight; y++)
            for (int x = 0; x < gridWidth; x++)
                gridColors[y, x] = Color.clear;

        currentPiece?.DestroyVisuals();
        currentPiece = null;

        level        = 1;
        linesCleared = 0;
        fallSpeed    = 1f;
        fallTimer    = 0f;

        UpdateUI();

        if (highScoreText != null)
        {
            int hs = GameManager.Instance?.ProfileManager?.CurrentProfile?.GetHighScore("tetris") ?? 0;
            highScoreText.text = $"Melhor: {hs:N0}";
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Utilitários
    // -----------------------------------------------------------------------

    private TetrisPieceType RandomPieceType()
    {
        return (TetrisPieceType)UnityEngine.Random.Range(0, 7);
    }

    // -----------------------------------------------------------------------
    // Classe da peça em movimento
    // -----------------------------------------------------------------------

    private class TetrisPiece
    {
        public TetrisPieceType type;
        public int x, y, rotation;
        public List<GameObject> visuals = new List<GameObject>();

        public void DestroyVisuals()
        {
            foreach (var v in visuals)
                if (v != null) UnityEngine.Object.Destroy(v);

            visuals.Clear();
        }
    }
}
