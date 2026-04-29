/*
 * GameCardUI.cs — Card visual de cada jogo no feed
 * Propósito: Exibe thumbnail, título, autor, likes, plays, badge de categoria e botão Jogar.
 *            Inclui animação de entrada (scale punch) e botão de like.
 * Como usar: Adicione ao prefab do card de jogo. Chame Initialize(data, sessionManager).
 * Dependências: GameData.cs, GameSessionManager.cs, PlayerProfileManager.cs
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameCardUI : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Elementos visuais
    // -----------------------------------------------------------------------

    [Header("Thumbnail")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image categoryColorOverlay;

    [Header("Textos")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text authorText;
    [SerializeField] private Text playsText;
    [SerializeField] private Text categoryBadgeText;
    [SerializeField] private Text likesText;
    [SerializeField] private Text livesText;      // ex: "❤ 1 vida"

    [Header("Badge de Categoria")]
    [SerializeField] private Image categoryBadgeImage;

    [Header("Botões")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button likeButton;
    [SerializeField] private Image likeButtonIcon;  // ícone de coração

    [Header("Cores de Like")]
    [SerializeField] private Color likedColor   = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color unlikedColor = Color.white;

    [Header("Animação de Entrada")]
    [SerializeField] private bool animateOnEnable = true;
    [SerializeField] private float punchScale      = 1.12f;
    [SerializeField] private float punchDuration   = 0.25f;

    // -----------------------------------------------------------------------
    // Dados
    // -----------------------------------------------------------------------

    private MiniGameData gameData;
    private GameSessionManager sessionManager;
    private bool isLiked = false;
    private int localLikes = 0;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    public void Initialize(MiniGameData data, GameSessionManager session = null)
    {
        gameData       = data;
        sessionManager = session;
        localLikes     = data != null ? data.likes : 0;

        ApplyData();
        SetupButtons();

        if (animateOnEnable)
            StartCoroutine(PunchScaleAnimation());
    }

    private void ApplyData()
    {
        if (gameData == null)
            return;

        // Thumbnail
        if (thumbnailImage != null)
        {
            if (gameData.thumbnailSprite != null)
            {
                thumbnailImage.sprite = gameData.thumbnailSprite;
                thumbnailImage.color  = Color.white;
            }
            else
            {
                thumbnailImage.sprite = null;
                thumbnailImage.color  = MiniGameData.GetCategoryColor(gameData.categoria);
            }
        }

        // Overlay colorido opcional
        if (categoryColorOverlay != null)
            categoryColorOverlay.color = MiniGameData.GetCategoryColor(gameData.categoria) * 0.4f;

        // Textos
        if (titleText  != null) titleText.text  = gameData.titulo;
        if (authorText != null) authorText.text  = $"por {gameData.autor}";
        if (playsText  != null) playsText.text   = FormatCount(gameData.plays) + " jogadas";
        if (likesText  != null) likesText.text   = FormatCount(localLikes);
        if (livesText  != null) livesText.text   = "❤ 1 vida";

        // Badge de categoria
        if (categoryBadgeText != null)
            categoryBadgeText.text = MiniGameData.GetCategoryDisplayName(gameData.categoria);

        if (categoryBadgeImage != null)
            categoryBadgeImage.color = MiniGameData.GetCategoryColor(gameData.categoria);

        // Estado de like inicial
        UpdateLikeVisual();
    }

    private void SetupButtons()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayPressed);

        if (likeButton != null)
            likeButton.onClick.AddListener(OnLikePressed);
    }

    // -----------------------------------------------------------------------
    // Ações do jogador
    // -----------------------------------------------------------------------

    private void OnPlayPressed()
    {
        if (gameData == null)
            return;

        Debug.Log($"[GameCardUI] Jogando: {gameData.titulo}");

        if (sessionManager != null)
        {
            sessionManager.RequestStartGame(gameData);
        }
        else if (GameManager.Instance?.SessionManager != null)
        {
            GameManager.Instance.SessionManager.RequestStartGame(gameData);
        }
        else
        {
            Debug.LogWarning("[GameCardUI] GameSessionManager não encontrado.");
        }
    }

    private void OnLikePressed()
    {
        isLiked = !isLiked;
        localLikes += isLiked ? 1 : -1;

        if (likesText != null)
            likesText.text = FormatCount(localLikes);

        UpdateLikeVisual();
        StartCoroutine(LikeButtonBounce());

        Debug.Log($"[GameCardUI] Like em '{gameData?.titulo}': {(isLiked ? "curtido" : "descurtido")}");
    }

    private void UpdateLikeVisual()
    {
        if (likeButtonIcon != null)
            likeButtonIcon.color = isLiked ? likedColor : unlikedColor;
    }

    // -----------------------------------------------------------------------
    // Animações
    // -----------------------------------------------------------------------

    private IEnumerator PunchScaleAnimation()
    {
        transform.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            float scale = Mathf.Lerp(0f, punchScale, EaseOut(t));
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        // Recua levemente para o tamanho normal
        elapsed = 0f;
        float bounceBack = punchDuration * 0.4f;
        while (elapsed < bounceBack)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bounceBack;
            float scale = Mathf.Lerp(punchScale, 1f, t);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    private IEnumerator LikeButtonBounce()
    {
        if (likeButton == null)
            yield break;

        RectTransform rt = likeButton.GetComponent<RectTransform>();
        if (rt == null) yield break;

        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.35f;
            rt.localScale = Vector3.one * scale;
            yield return null;
        }

        rt.localScale = Vector3.one;
    }

    // -----------------------------------------------------------------------
    // Utilitários
    // -----------------------------------------------------------------------

    private static string FormatCount(int count)
    {
        if (count >= 1_000_000) return $"{count / 1_000_000f:0.#}M";
        if (count >= 1_000)     return $"{count / 1_000f:0.#}K";
        return count.ToString();
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
}
