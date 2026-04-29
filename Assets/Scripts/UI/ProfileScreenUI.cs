/*
 * ProfileScreenUI.cs — Tela de perfil do jogador
 * Propósito: Exibe e permite editar dados do perfil: username, avatar, stats, jogos criados.
 * Como usar: Adicione ao painel de perfil. Configure referências no Inspector.
 * Dependências: PlayerProfileManager.cs, GameData.cs, GiftCardRedeemer.cs
 */

using UnityEngine;
using UnityEngine.UI;

public class ProfileScreenUI : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Avatar
    // -----------------------------------------------------------------------

    [Header("Avatar")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private Text usernameText;
    [SerializeField] private InputField usernameInput;
    [SerializeField] private Button editUsernameButton;
    [SerializeField] private Button confirmUsernameButton;
    [SerializeField] private Color[] avatarColors;

    // -----------------------------------------------------------------------
    // Stats
    // -----------------------------------------------------------------------

    [Header("Estatísticas")]
    [SerializeField] private Text coinsText;
    [SerializeField] private Text creditsText;
    [SerializeField] private Text gamesCreatedText;
    [SerializeField] private Text gamesPlayedText;
    [SerializeField] private Text levelText;
    [SerializeField] private Slider xpProgressBar;
    [SerializeField] private Text xpText;

    // -----------------------------------------------------------------------
    // Botões de ação
    // -----------------------------------------------------------------------

    [Header("Ações")]
    [SerializeField] private Button myGamesButton;
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button redeemGiftCardButton;

    // -----------------------------------------------------------------------
    // Painéis referenciados (ativados/desativados)
    // -----------------------------------------------------------------------

    [Header("Painéis")]
    [SerializeField] private GameObject myGamesPanel;
    [SerializeField] private GameObject giftCardPanel;
    [SerializeField] private GameObject gridEditorPanel;

    // -----------------------------------------------------------------------
    // Referências de sistema
    // -----------------------------------------------------------------------

    [Header("Sistema")]
    [SerializeField] private PlayerProfileManager profileManager;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (profileManager == null)
            profileManager = FindObjectOfType<PlayerProfileManager>();
    }

    private void Start()
    {
        SetupButtons();
        RefreshDisplay();

        // Assina evento de mudança de moedas
        GameManager.OnCoinsChanged += OnCoinsChanged;

        if (profileManager != null)
            profileManager.OnProfileSaved += _ => RefreshDisplay();
    }

    private void OnDestroy()
    {
        GameManager.OnCoinsChanged -= OnCoinsChanged;
    }

    private void OnEnable()
    {
        RefreshDisplay();
    }

    // -----------------------------------------------------------------------
    // Setup de botões
    // -----------------------------------------------------------------------

    private void SetupButtons()
    {
        if (editUsernameButton != null)
            editUsernameButton.onClick.AddListener(OnEditUsernamePressed);

        if (confirmUsernameButton != null)
            confirmUsernameButton.onClick.AddListener(OnConfirmUsernamePressed);

        if (myGamesButton != null)
            myGamesButton.onClick.AddListener(OnMyGamesPressed);

        if (createGameButton != null)
            createGameButton.onClick.AddListener(OnCreateGamePressed);

        if (redeemGiftCardButton != null)
            redeemGiftCardButton.onClick.AddListener(OnRedeemGiftCardPressed);

        // Esconde input de username por padrão
        if (usernameInput != null)  usernameInput.gameObject.SetActive(false);
        if (confirmUsernameButton != null) confirmUsernameButton.gameObject.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Atualização de display
    // -----------------------------------------------------------------------

    public void RefreshDisplay()
    {
        PlayerProfile profile = profileManager?.CurrentProfile;

        if (profile == null)
            return;

        // Username e avatar
        if (usernameText != null)
            usernameText.text = profile.username;

        if (avatarImage != null && avatarColors != null && avatarColors.Length > 0)
        {
            int idx = Mathf.Clamp(profile.avatarColorIndex, 0, avatarColors.Length - 1);
            avatarImage.color = avatarColors[idx];
        }

        // Stats
        if (coinsText != null)
            coinsText.text = $"🪙 {profile.coins:N0}";

        if (creditsText != null)
            creditsText.text = $"💳 {profile.credits:N0} créditos";

        if (gamesCreatedText != null)
            gamesCreatedText.text = $"{profile.gamesCreated} criados";

        if (gamesPlayedText != null)
            gamesPlayedText.text = $"{profile.gamesPlayed} jogados";

        // Nível e XP
        if (levelText != null)
            levelText.text = $"Nível {profile.nivel}";

        if (xpProgressBar != null)
            xpProgressBar.value = profile.GetLevelProgress();

        if (xpText != null)
            xpText.text = $"{profile.xp} / {profile.GetXpForNextLevel()} XP";
    }

    // -----------------------------------------------------------------------
    // Ações de botões
    // -----------------------------------------------------------------------

    private void OnEditUsernamePressed()
    {
        if (usernameInput == null) return;

        usernameInput.text = profileManager?.CurrentProfile?.username ?? "";
        usernameInput.gameObject.SetActive(true);
        if (usernameText != null) usernameText.gameObject.SetActive(false);
        if (editUsernameButton != null)   editUsernameButton.gameObject.SetActive(false);
        if (confirmUsernameButton != null) confirmUsernameButton.gameObject.SetActive(true);
        usernameInput.Select();
    }

    private void OnConfirmUsernamePressed()
    {
        string newName = usernameInput != null ? usernameInput.text.Trim() : "";

        if (!string.IsNullOrEmpty(newName))
        {
            profileManager?.UpdateUsername(newName);
            NotificationSystem.Show("Username atualizado!", NotificationSystem.NotificationType.Sucesso);
        }

        if (usernameInput != null)       usernameInput.gameObject.SetActive(false);
        if (usernameText != null)        usernameText.gameObject.SetActive(true);
        if (editUsernameButton != null)  editUsernameButton.gameObject.SetActive(true);
        if (confirmUsernameButton != null) confirmUsernameButton.gameObject.SetActive(false);

        RefreshDisplay();
    }

    private void OnMyGamesPressed()
    {
        if (myGamesPanel != null)
            myGamesPanel.SetActive(!myGamesPanel.activeSelf);
    }

    private void OnCreateGamePressed()
    {
        if (gridEditorPanel != null)
            gridEditorPanel.SetActive(true);
        else
            Debug.LogWarning("[ProfileScreenUI] Painel do editor de grid não configurado.");
    }

    private void OnRedeemGiftCardPressed()
    {
        if (giftCardPanel != null)
            giftCardPanel.SetActive(true);
        else
            Debug.LogWarning("[ProfileScreenUI] Painel de gift card não configurado.");
    }

    // -----------------------------------------------------------------------
    // Eventos externos
    // -----------------------------------------------------------------------

    private void OnCoinsChanged(int newTotal)
    {
        if (coinsText != null)
            coinsText.text = $"🪙 {newTotal:N0}";
    }
}
