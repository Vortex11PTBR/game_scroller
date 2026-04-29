/*
 * NoLivesPopup.cs — Popup modal quando o jogador não tem vidas
 * Propósito: Mostra opções para recuperar vida: assistir anúncio, gastar moedas ou esperar.
 * Como usar: Adicione ao Canvas. O GameSessionManager/LivesUI chama Show() quando necessário.
 * Dependências: LifeSystem.cs, AdManager.cs, PlayerProfileManager.cs
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NoLivesPopup : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // UI
    // -----------------------------------------------------------------------

    [Header("Painel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Textos")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text coinsOptionText;

    [Header("Botões")]
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button spendCoinsButton;
    [SerializeField] private Button closeButton;

    [Header("Coração Partido (animação)")]
    [SerializeField] private Transform brokenHeartTransform;

    [Header("Custo em Moedas")]
    [SerializeField] private int coinCostForFiveLives = 100;

    // -----------------------------------------------------------------------
    // Referências
    // -----------------------------------------------------------------------

    [Header("Sistemas")]
    [SerializeField] private LifeSystem lifeSystem;
    [SerializeField] private AdManager adManager;

    // -----------------------------------------------------------------------
    // Eventos
    // -----------------------------------------------------------------------

    public event Action OnLifeGranted;
    public event Action OnClosed;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private Coroutine timerCoroutine;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (lifeSystem == null)
            lifeSystem = FindObjectOfType<LifeSystem>();

        if (adManager == null && GameManager.Instance != null)
            adManager = GameManager.Instance.AdManager;

        if (adManager == null)
            adManager = FindObjectOfType<AdManager>();
    }

    private void Start()
    {
        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAdPressed);

        if (spendCoinsButton != null)
            spendCoinsButton.onClick.AddListener(OnSpendCoinsPressed);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClosePressed);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        UpdateCoinsButtonLabel();
    }

    // -----------------------------------------------------------------------
    // Mostrar / Esconder
    // -----------------------------------------------------------------------

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = "Sem Vidas! 💔";

        UpdateCoinsButtonLabel();

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(UpdateTimer());

        if (brokenHeartTransform != null)
            StartCoroutine(AnimateBrokenHeart());

        Debug.Log("[NoLivesPopup] Popup exibido.");
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        OnClosed?.Invoke();
    }

    // -----------------------------------------------------------------------
    // Opções do jogador
    // -----------------------------------------------------------------------

    private void OnWatchAdPressed()
    {
        if (adManager == null)
        {
            Debug.LogWarning("[NoLivesPopup] AdManager não encontrado.");
            return;
        }

        if (!adManager.IsAdReady())
        {
            NotificationSystem.Show("Anúncio não disponível agora. Tente novamente.", NotificationSystem.NotificationType.Alerta);
            return;
        }

        watchAdButton.interactable = false;

        adManager.ShowRewardedAd(success =>
        {
            watchAdButton.interactable = true;

            if (success)
            {
                lifeSystem?.AddRewardLives(1);
                GameManager.NotifyLivesChanged(lifeSystem?.CurrentLives ?? 0);
                NotificationSystem.Show("+1 Vida! ❤", NotificationSystem.NotificationType.Sucesso);
                OnLifeGranted?.Invoke();
                Hide();
            }
            else
            {
                NotificationSystem.Show("Erro ao carregar anúncio.", NotificationSystem.NotificationType.Erro);
            }
        });
    }

    private void OnSpendCoinsPressed()
    {
        PlayerProfileManager pm = GameManager.Instance?.ProfileManager
                                  ?? FindObjectOfType<PlayerProfileManager>();

        if (pm == null)
        {
            Debug.LogWarning("[NoLivesPopup] PlayerProfileManager não encontrado.");
            return;
        }

        if (pm.SpendCoins(coinCostForFiveLives))
        {
            lifeSystem?.AddRewardLives(5);
            GameManager.NotifyLivesChanged(lifeSystem?.CurrentLives ?? 0);
            NotificationSystem.Show("+5 Vidas! ❤❤❤❤❤", NotificationSystem.NotificationType.Sucesso);
            OnLifeGranted?.Invoke();
            Hide();
        }
        else
        {
            NotificationSystem.Show("Moedas insuficientes!", NotificationSystem.NotificationType.Erro);
        }
    }

    private void OnClosePressed()
    {
        Hide();
    }

    // -----------------------------------------------------------------------
    // Timer de próxima vida
    // -----------------------------------------------------------------------

    private IEnumerator UpdateTimer()
    {
        while (true)
        {
            if (lifeSystem != null)
            {
                lifeSystem.RefreshLives();
                TimeSpan remaining = lifeSystem.GetTimeUntilNextLife();

                if (timerText != null)
                {
                    timerText.text = remaining.TotalSeconds > 0
                        ? $"Próxima vida em: {remaining.Minutes:D2}:{remaining.Seconds:D2}"
                        : "Próxima vida pronta!";
                }

                // Fecha popup automaticamente se ganhar vida
                if (lifeSystem.HasLives)
                {
                    NotificationSystem.Show("Vida recuperada! ❤", NotificationSystem.NotificationType.Sucesso);
                    OnLifeGranted?.Invoke();
                    Hide();
                    yield break;
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    // -----------------------------------------------------------------------
    // Animação do coração partido
    // -----------------------------------------------------------------------

    private IEnumerator AnimateBrokenHeart()
    {
        if (brokenHeartTransform == null)
            yield break;

        while (panelRoot != null && panelRoot.activeSelf)
        {
            float t = 0f;
            float duration = 0.8f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float scale = 1f + Mathf.Sin(t / duration * Mathf.PI) * 0.1f;
                brokenHeartTransform.localScale = Vector3.one * scale;
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    // -----------------------------------------------------------------------
    // Utilitários
    // -----------------------------------------------------------------------

    private void UpdateCoinsButtonLabel()
    {
        if (coinsOptionText != null)
            coinsOptionText.text = $"Gastar {coinCostForFiveLives} moedas → +5 vidas";
    }
}
