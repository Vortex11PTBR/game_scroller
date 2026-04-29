/*
 * GameFeedController.cs — Feed de jogos com abas de categorias
 * Propósito: Controla o feed vertical infinito com filtro por categoria e integra com GameCardUI.
 * Como usar: Adicione ao GameObject raiz do feed. Configure prefab de card e lista de jogos no Inspector.
 * Dependências: InfiniteScrollManager.cs, GameCardUI.cs, GameData.cs, GameSessionManager.cs
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameFeedController : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Configuração
    // -----------------------------------------------------------------------

    [Header("Dados de Jogos")]
    [SerializeField] private List<MiniGameData> allGames = new List<MiniGameData>();

    [Header("Prefab de Card")]
    [SerializeField] private GameObject gameCardPrefab;
    [SerializeField] private Transform cardsContainer;

    [Header("Abas de Categorias")]
    [SerializeField] private Transform categoryTabsContainer;
    [SerializeField] private GameObject categoryTabPrefab;    // prefab de botão de aba

    [Header("Scroll")]
    [SerializeField] private ScrollRect feedScrollRect;
    [SerializeField] private float cardHeight = 520f;

    [Header("Sessão")]
    [SerializeField] private GameSessionManager sessionManager;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private GameCategory selectedCategory = GameCategory.Todos;
    private List<MiniGameData> filteredGames = new List<MiniGameData>();
    private List<GameObject> activeCards     = new List<GameObject>();
    private List<Button> categoryButtons     = new List<Button>();

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Start()
    {
        if (sessionManager == null)
            sessionManager = FindObjectOfType<GameSessionManager>();

        BuildCategoryTabs();
        FilterAndRebuildFeed(GameCategory.Todos);
    }

    // -----------------------------------------------------------------------
    // Abas de categorias
    // -----------------------------------------------------------------------

    private void BuildCategoryTabs()
    {
        if (categoryTabsContainer == null || categoryTabPrefab == null)
            return;

        // Limpa abas existentes
        foreach (Transform child in categoryTabsContainer)
            Destroy(child.gameObject);

        categoryButtons.Clear();

        GameCategory[] categories = (GameCategory[])Enum.GetValues(typeof(GameCategory));

        foreach (GameCategory cat in categories)
        {
            GameObject tabGo = Instantiate(categoryTabPrefab, categoryTabsContainer);
            tabGo.name = $"Tab_{cat}";

            Text tabLabel = tabGo.GetComponentInChildren<Text>();
            if (tabLabel != null)
                tabLabel.text = MiniGameData.GetCategoryDisplayName(cat);

            Button tabButton = tabGo.GetComponent<Button>();
            if (tabButton != null)
            {
                GameCategory capturedCat = cat; // closure
                tabButton.onClick.AddListener(() => OnCategoryTabPressed(capturedCat));
                categoryButtons.Add(tabButton);
            }
        }

        UpdateCategoryTabVisuals();
    }

    private void OnCategoryTabPressed(GameCategory category)
    {
        if (category == selectedCategory)
            return;

        selectedCategory = category;
        UpdateCategoryTabVisuals();
        FilterAndRebuildFeed(category);

        Debug.Log($"[GameFeedController] Categoria selecionada: {MiniGameData.GetCategoryDisplayName(category)}");
    }

    private void UpdateCategoryTabVisuals()
    {
        GameCategory[] categories = (GameCategory[])Enum.GetValues(typeof(GameCategory));

        for (int i = 0; i < categoryButtons.Count && i < categories.Length; i++)
        {
            bool isSelected = categories[i] == selectedCategory;
            Image tabImg = categoryButtons[i].GetComponent<Image>();

            if (tabImg != null)
            {
                tabImg.color = isSelected
                    ? MiniGameData.GetCategoryColor(categories[i])
                    : new Color(0.25f, 0.25f, 0.25f);
            }

            Text tabText = categoryButtons[i].GetComponentInChildren<Text>();
            if (tabText != null)
            {
                tabText.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                tabText.color = Color.white;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Feed
    // -----------------------------------------------------------------------

    private void FilterAndRebuildFeed(GameCategory category)
    {
        selectedCategory = category;

        filteredGames.Clear();

        foreach (MiniGameData game in allGames)
        {
            if (game == null) continue;

            if (category == GameCategory.Todos || game.categoria == category)
                filteredGames.Add(game);
        }

        RebuildCards();

        // Volta ao topo
        if (feedScrollRect != null)
            feedScrollRect.verticalNormalizedPosition = 1f;
    }

    private void RebuildCards()
    {
        // Destrói cards existentes
        foreach (GameObject card in activeCards)
            Destroy(card);

        activeCards.Clear();

        if (gameCardPrefab == null || cardsContainer == null)
        {
            Debug.LogWarning("[GameFeedController] Prefab de card ou container não configurado.");
            return;
        }

        // Ajusta tamanho do container
        RectTransform containerRect = cardsContainer as RectTransform;
        if (containerRect != null)
        {
            containerRect.sizeDelta = new Vector2(
                containerRect.sizeDelta.x,
                filteredGames.Count * cardHeight
            );
        }

        // Cria os cards
        for (int i = 0; i < filteredGames.Count; i++)
        {
            MiniGameData data = filteredGames[i];
            GameObject cardGo = Instantiate(gameCardPrefab, cardsContainer);
            cardGo.name = $"GameCard_{data.id}";

            GameCardUI cardUI = cardGo.GetComponent<GameCardUI>();
            if (cardUI != null)
            {
                cardUI.Initialize(data, sessionManager);
            }

            // Posiciona o card no feed
            RectTransform cardRect = cardGo.GetComponent<RectTransform>();
            if (cardRect != null)
                cardRect.anchoredPosition = new Vector2(0, -i * cardHeight);

            activeCards.Add(cardGo);
        }

        Debug.Log($"[GameFeedController] Feed construído com {filteredGames.Count} jogos.");
    }

    // -----------------------------------------------------------------------
    // API pública
    // -----------------------------------------------------------------------

    /// <summary>Adiciona um jogo ao feed em tempo de execução.</summary>
    public void AddGame(MiniGameData data)
    {
        if (data == null || allGames.Contains(data))
            return;

        allGames.Add(data);
        FilterAndRebuildFeed(selectedCategory);
    }

    /// <summary>Remove um jogo do feed em tempo de execução.</summary>
    public void RemoveGame(MiniGameData data)
    {
        if (data == null)
            return;

        allGames.Remove(data);
        FilterAndRebuildFeed(selectedCategory);
    }

    public GameCategory GetSelectedCategory() => selectedCategory;
    public int GetFilteredGameCount()          => filteredGames.Count;
}
