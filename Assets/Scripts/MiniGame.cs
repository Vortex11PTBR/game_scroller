using UnityEngine;
using UnityEngine.UI;

public class MiniGame : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Image backgroundImage;

    private int gameIndex = 0;

    private void OnEnable()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        gameIndex = -Mathf.FloorToInt(rectTransform.anchoredPosition.y / 500f);

        if (titleText != null)
            titleText.text = $"Mini-Game #{gameIndex}";

        if (descriptionText != null)
            descriptionText.text = $"Toque para jogar o mini-game {gameIndex}!";

        if (backgroundImage != null)
        {
            Color randomColor = new Color(
                Random.value,
                Random.value,
                Random.value,
                1f
            );
            backgroundImage.color = randomColor;
        }
    }

    public void OnGamePressed()
    {
        Debug.Log($"Mini-Game {gameIndex} foi acionado!");
    }
}
