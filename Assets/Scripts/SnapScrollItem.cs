using UnityEngine;

public class SnapScrollItem : MonoBehaviour
{
    [SerializeField] private string gameId;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image highlightImage;
    [SerializeField] private float selectedAlpha = 1f;
    [SerializeField] private float deselectedAlpha = 0.6f;
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float deselectedScale = 0.9f;
    [SerializeField] private float scaleDuration = 0.3f;

    private int itemIndex;
    private bool isSelected = false;
    private RectTransform rectTransform;

    private void OnEnable()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (highlightImage == null)
            highlightImage = GetComponent<Image>();
    }

    public void Initialize(int index)
    {
        itemIndex = index;

        if (string.IsNullOrEmpty(gameId))
            gameId = $"game_{index}";

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    public void Select()
    {
        if (isSelected)
            return;

        isSelected = true;

        if (canvasGroup != null)
            canvasGroup.alpha = selectedAlpha;

        if (highlightImage != null)
            highlightImage.enabled = true;

        LeanTween.scale(gameObject, Vector3.one * selectedScale, scaleDuration)
            .setEase(LeanTweenType.easeOutCubic);

        OnSelected();
    }

    public void Deselect()
    {
        if (!isSelected)
            return;

        isSelected = false;

        if (canvasGroup != null)
            canvasGroup.alpha = deselectedAlpha;

        if (highlightImage != null)
            highlightImage.enabled = false;

        LeanTween.scale(gameObject, Vector3.one * deselectedScale, scaleDuration)
            .setEase(LeanTweenType.easeOutCubic);

        OnDeselected();
    }

    protected virtual void OnSelected()
    {
        // Override em subclasses para lógica customizada
    }

    protected virtual void OnDeselected()
    {
        // Override em subclasses para lógica customizada
    }

    public string GetGameId()
    {
        return gameId;
    }

    public int GetItemIndex()
    {
        return itemIndex;
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    public void SetGameId(string newGameId)
    {
        gameId = newGameId;
    }

    private void OnDestroy()
    {
        LeanTween.cancel(gameObject);
    }
}
