using UnityEngine;
using UnityEngine.UI;

public class SnapScrollExample : MonoBehaviour
{
    [SerializeField] private SnapScrollManager snapScrollManager;
    [SerializeField] private Text selectedGameText;
    [SerializeField] private Text gameIndexText;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    private void OnEnable()
    {
        if (snapScrollManager == null)
            snapScrollManager = GetComponentInParent<SnapScrollManager>();

        snapScrollManager.GameSelected += OnGameSelected;
        snapScrollManager.GameExited += OnGameExited;

        if (previousButton != null)
            previousButton.onClick.AddListener(OnPreviousPressed);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextPressed);
    }

    private void OnDisable()
    {
        if (snapScrollManager != null)
        {
            snapScrollManager.GameSelected -= OnGameSelected;
            snapScrollManager.GameExited -= OnGameExited;
        }

        if (previousButton != null)
            previousButton.onClick.RemoveListener(OnPreviousPressed);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextPressed);
    }

    private void OnGameSelected(int itemIndex, string gameId)
    {
        if (selectedGameText != null)
            selectedGameText.text = $"Jogo Selecionado: {gameId}";

        if (gameIndexText != null)
            gameIndexText.text = $"Índice: {itemIndex} / {snapScrollManager.GetItemCount() - 1}";

        Debug.Log($"[SnapScroll] Jogo selecionado: {gameId} (Índice: {itemIndex})");

        UpdateButtonStates();
    }

    private void OnGameExited(int itemIndex, string gameId)
    {
        Debug.Log($"[SnapScroll] Jogo saiu da tela: {gameId} (Índice: {itemIndex})");

        // Aqui você pode parar simulações, limpar recursos, etc.
        // Exemplo: StopPhysicsSimulation(gameId);
    }

    private void OnPreviousPressed()
    {
        int currentIndex = snapScrollManager.GetCurrentSelectedIndex();
        if (currentIndex > 0)
        {
            snapScrollManager.ScrollToItemAtIndex(currentIndex - 1);
        }
    }

    private void OnNextPressed()
    {
        int currentIndex = snapScrollManager.GetCurrentSelectedIndex();
        if (currentIndex < snapScrollManager.GetItemCount() - 1)
        {
            snapScrollManager.ScrollToItemAtIndex(currentIndex + 1);
        }
    }

    private void UpdateButtonStates()
    {
        int currentIndex = snapScrollManager.GetCurrentSelectedIndex();
        int itemCount = snapScrollManager.GetItemCount();

        if (previousButton != null)
            previousButton.interactable = currentIndex > 0;

        if (nextButton != null)
            nextButton.interactable = currentIndex < itemCount - 1;
    }
}
