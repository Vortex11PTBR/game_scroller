using UnityEngine;
using UnityEngine.UI;

public class LoadingProgressUI : MonoBehaviour
{
    [SerializeField] private AddressablesManager addressablesManager;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image progressBar;
    [SerializeField] private Text progressText;
    [SerializeField] private Text loadingMessageText;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private bool showProgressNumbers = true;

    private float currentProgress = 0f;
    private string currentLoadingAddress = "";
    private bool isVisible = false;

    private void OnEnable()
    {
        if (addressablesManager == null)
            addressablesManager = FindObjectOfType<AddressablesManager>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (progressBar != null)
            progressBar.fillAmount = 0f;

        canvasGroup.alpha = 0f;
        isVisible = false;

        addressablesManager.ProgressChanged += OnProgressChanged;
        addressablesManager.LoadFailed += OnLoadFailed;
    }

    private void OnDisable()
    {
        if (addressablesManager != null)
        {
            addressablesManager.ProgressChanged -= OnProgressChanged;
            addressablesManager.LoadFailed -= OnLoadFailed;
        }
    }

    private void OnProgressChanged(string address, float progress)
    {
        currentProgress = progress;
        currentLoadingAddress = address;

        if (progress > 0f && !isVisible)
        {
            ShowLoading();
        }

        UpdateProgressVisuals();

        if (progress >= 1f)
        {
            HideLoading();
        }
    }

    private void OnLoadFailed(string address, string error)
    {
        if (loadingMessageText != null)
            loadingMessageText.text = "Erro ao carregar!";

        HideLoading(1f);
    }

    private void UpdateProgressVisuals()
    {
        if (progressBar != null)
            progressBar.fillAmount = currentProgress;

        if (progressText != null && showProgressNumbers)
            progressText.text = $"{currentProgress:P0}";

        if (loadingMessageText != null)
        {
            string shortAddress = currentLoadingAddress.Length > 20
                ? currentLoadingAddress.Substring(0, 17) + "..."
                : currentLoadingAddress;

            loadingMessageText.text = $"Carregando {shortAddress}...";
        }
    }

    private void ShowLoading()
    {
        if (isVisible)
            return;

        isVisible = true;
        LeanTween.alphaCanvas(canvasGroup, 1f, fadeInDuration)
            .setEase(LeanTweenType.easeOutCubic);
    }

    private void HideLoading(float delay = 0f)
    {
        if (!isVisible)
            return;

        isVisible = false;
        LeanTween.delayedCall(delay, () =>
        {
            LeanTween.alphaCanvas(canvasGroup, 0f, fadeOutDuration)
                .setEase(LeanTweenType.easeInCubic);
        });
    }

    public void SetProgressBarColor(Color color)
    {
        if (progressBar != null)
            progressBar.color = color;
    }

    public void SetLoadingMessage(string message)
    {
        if (loadingMessageText != null)
            loadingMessageText.text = message;
    }

    private void OnDestroy()
    {
        LeanTween.cancel(gameObject);
    }
}
