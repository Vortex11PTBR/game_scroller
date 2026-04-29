using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfiniteScrollManager : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentPanel;
    [SerializeField] private GameObject[] miniGamePrefabs;
    [SerializeField] private float cellHeight = 500f;
    [SerializeField] private float scrollThreshold = 100f;

    private List<GameObject> activeGames = new List<GameObject>();
    private Queue<GameObject> inactiveGames = new Queue<GameObject>();
    private int currentIndex = 0;
    private bool isInitialized = false;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;

        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>();

        if (contentPanel == null)
            contentPanel = scrollRect.content;

        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

        LoadInitialGames();
    }

    private void LoadInitialGames()
    {
        for (int i = 0; i < 3 && miniGamePrefabs.Length > 0; i++)
        {
            SpawnGameAtIndex(i);
        }
    }

    private void OnScrollValueChanged(Vector2 scrollPosition)
    {
        if (!isInitialized || miniGamePrefabs.Length == 0)
            return;

        UpdateVisibleGames();
    }

    private void UpdateVisibleGames()
    {
        float scrollPos = contentPanel.anchoredPosition.y;
        int visibleIndex = Mathf.Max(0, Mathf.FloorToInt(scrollPos / cellHeight));

        int targetIndex = Mathf.Max(0, visibleIndex - 1);

        if (Mathf.Abs(targetIndex - currentIndex) > 1)
        {
            UnloadAllGames();
            currentIndex = targetIndex;
            LoadGamesAroundIndex(currentIndex);
        }
        else if (targetIndex > currentIndex)
        {
            currentIndex = targetIndex;
            RemoveOldestGame();
            SpawnGameAtIndex(currentIndex + 2);
        }
        else if (targetIndex < currentIndex)
        {
            currentIndex = targetIndex;
            RemoveNewestGame();
            SpawnGameAtIndex(currentIndex);
        }
    }

    private void LoadGamesAroundIndex(int centerIndex)
    {
        for (int i = -1; i <= 1; i++)
        {
            int index = centerIndex + i;
            if (index >= 0)
            {
                SpawnGameAtIndex(index);
            }
        }
    }

    private void SpawnGameAtIndex(int index)
    {
        if (HasGameAtIndex(index))
            return;

        GameObject game = GetOrCreateGame(index);
        RectTransform rectTransform = game.GetComponent<RectTransform>();

        if (rectTransform == null)
            rectTransform = game.AddComponent<RectTransform>();

        game.SetActive(true);
        rectTransform.SetAsLastSibling();
        rectTransform.anchoredPosition = new Vector2(0, -index * cellHeight);
        rectTransform.sizeDelta = new Vector2(contentPanel.rect.width, cellHeight);

        activeGames.Add(game);

        if (contentPanel.sizeDelta.y < (index + 1) * cellHeight)
        {
            contentPanel.sizeDelta = new Vector2(contentPanel.sizeDelta.x, (index + 1) * cellHeight);
        }
    }

    private GameObject GetOrCreateGame(int index)
    {
        if (inactiveGames.Count > 0)
        {
            GameObject game = inactiveGames.Dequeue();
            game.transform.SetParent(contentPanel);
            return game;
        }

        int prefabIndex = index % miniGamePrefabs.Length;
        GameObject newGame = Instantiate(miniGamePrefabs[prefabIndex], contentPanel);
        newGame.name = $"MiniGame_{index}";

        return newGame;
    }

    private bool HasGameAtIndex(int index)
    {
        foreach (GameObject game in activeGames)
        {
            RectTransform rectTransform = game.GetComponent<RectTransform>();
            if (Mathf.Abs(rectTransform.anchoredPosition.y + index * cellHeight) < 1f)
                return true;
        }

        return false;
    }

    private void RemoveOldestGame()
    {
        if (activeGames.Count == 0)
            return;

        GameObject oldestGame = activeGames[0];
        activeGames.RemoveAt(0);
        DisableAndPoolGame(oldestGame);
    }

    private void RemoveNewestGame()
    {
        if (activeGames.Count == 0)
            return;

        GameObject newestGame = activeGames[activeGames.Count - 1];
        activeGames.RemoveAt(activeGames.Count - 1);
        DisableAndPoolGame(newestGame);
    }

    private void UnloadAllGames()
    {
        foreach (GameObject game in activeGames)
        {
            DisableAndPoolGame(game);
        }

        activeGames.Clear();
    }

    private void DisableAndPoolGame(GameObject game)
    {
        game.SetActive(false);
        inactiveGames.Enqueue(game);
    }

    public void ClearAll()
    {
        UnloadAllGames();
        currentIndex = 0;
        isInitialized = false;

        while (inactiveGames.Count > 0)
        {
            Destroy(inactiveGames.Dequeue());
        }
    }

    public int GetTotalGameCount()
    {
        return int.MaxValue;
    }

    public int GetCurrentVisibleIndex()
    {
        return currentIndex;
    }
}
