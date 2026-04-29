/*
 * LivesUI.cs — Exibição e animação das vidas do jogador
 * Propósito: Mostra corações animados (cheios/vazios), timer de recuperação e botão + vida.
 * Como usar: Adicione ao HUD. Configure os slots de coração e integre com LifeSystem.
 * Dependências: LifeSystem.cs, AdManager.cs, PlayerProfileManager.cs
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LivesUI : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Configuração
    // -----------------------------------------------------------------------

    [Header("Corações")]
    [SerializeField] private Transform heartsContainer;
    [SerializeField] private GameObject heartPrefab;        // prefab com Image
    [SerializeField] private int maxHeartsDisplayed = 5;    // visual max
    [SerializeField] private Sprite heartFullSprite;
    [SerializeField] private Sprite heartEmptySprite;
    [SerializeField] private Color heartFullColor  = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color heartEmptyColor = new Color(0.4f, 0.4f, 0.4f);

    [Header("Timer")]
    [SerializeField] private Text timerText;
    [SerializeField] private GameObject timerPanel;

    [Header("Botão + Vida")]
    [SerializeField] private Button addLifeButton;
    [SerializeField] private Text livesCountText;

    // -----------------------------------------------------------------------
    // Referências
    // -----------------------------------------------------------------------

    [Header("Sistemas")]
    [SerializeField] private LifeSystem lifeSystem;

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private List<Image> heartImages  = new List<Image>();
    private Coroutine timerCoroutine;

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (lifeSystem == null)
            lifeSystem = FindObjectOfType<LifeSystem>();
    }

    private void Start()
    {
        BuildHearts();
        UpdateDisplay();

        if (addLifeButton != null)
            addLifeButton.onClick.AddListener(OnAddLifePressed);

        // Assina evento global do GameManager
        GameManager.OnLivesChanged += OnLivesChangedGlobal;
    }

    private void OnDestroy()
    {
        GameManager.OnLivesChanged -= OnLivesChangedGlobal;
    }

    private void OnEnable()
    {
        UpdateDisplay();
    }

    // -----------------------------------------------------------------------
    // Construção dos corações
    // -----------------------------------------------------------------------

    private void BuildHearts()
    {
        if (heartsContainer == null || heartPrefab == null)
            return;

        foreach (Transform child in heartsContainer)
            Destroy(child.gameObject);

        heartImages.Clear();

        for (int i = 0; i < maxHeartsDisplayed; i++)
        {
            GameObject heartGo = Instantiate(heartPrefab, heartsContainer);
            Image img = heartGo.GetComponent<Image>();
            if (img != null)
                heartImages.Add(img);
        }
    }

    // -----------------------------------------------------------------------
    // Atualização visual
    // -----------------------------------------------------------------------

    public void UpdateDisplay()
    {
        if (lifeSystem == null)
            return;

        lifeSystem.RefreshLives();
        int lives = lifeSystem.CurrentLives;

        // Atualiza corações
        for (int i = 0; i < heartImages.Count; i++)
        {
            bool isFull = i < lives;
            heartImages[i].sprite = isFull ? heartFullSprite  : heartEmptySprite;
            heartImages[i].color  = isFull ? heartFullColor   : heartEmptyColor;
        }

        // Texto de contagem
        if (livesCountText != null)
            livesCountText.text = $"{lives}";

        // Timer de recuperação
        bool isAtMax  = lives >= 20;
        // Simplify: hasTimer is simply the negation of isAtMax
        bool hasTimer = !isAtMax;

        if (timerPanel != null)
            timerPanel.SetActive(hasTimer);

        if (hasTimer)
        {
            if (timerCoroutine != null)
                StopCoroutine(timerCoroutine);

            timerCoroutine = StartCoroutine(TimerCountdown());
        }
    }

    private IEnumerator TimerCountdown()
    {
        while (lifeSystem != null && lifeSystem.CurrentLives < 20)
        {
            TimeSpan remaining = lifeSystem.GetTimeUntilNextLife();

            if (timerText != null)
            {
                timerText.text = remaining.TotalSeconds > 0
                    ? $"{remaining.Minutes:D2}:{remaining.Seconds:D2}"
                    : "Recuperando...";
            }

            // Verifica se ganhou uma vida
            int prevLives = lifeSystem.CurrentLives;
            lifeSystem.RefreshLives();
            if (lifeSystem.CurrentLives > prevLives)
                AnimateHeartRecovery(lifeSystem.CurrentLives - 1);

            yield return new WaitForSeconds(1f);
        }

        UpdateDisplay();
    }

    // -----------------------------------------------------------------------
    // Animações
    // -----------------------------------------------------------------------

    private void AnimateHeartRecovery(int heartIndex)
    {
        if (heartIndex < 0 || heartIndex >= heartImages.Count)
            return;

        StartCoroutine(PulseHeart(heartImages[heartIndex]));
        UpdateDisplay();
    }

    private IEnumerator PulseHeart(Image heart)
    {
        if (heart == null)
            yield break;

        float duration = 0.4f;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f;
            heart.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        heart.transform.localScale = Vector3.one;
    }

    // -----------------------------------------------------------------------
    // Botão + Vida
    // -----------------------------------------------------------------------

    private void OnAddLifePressed()
    {
        // Abre popup de sem vidas / opções
        NoLivesPopup popup = FindObjectOfType<NoLivesPopup>();
        if (popup != null)
            popup.Show();
        else
            Debug.LogWarning("[LivesUI] NoLivesPopup não encontrado na cena.");
    }

    // -----------------------------------------------------------------------
    // Eventos externos
    // -----------------------------------------------------------------------

    private void OnLivesChangedGlobal(int newLives)
    {
        UpdateDisplay();
    }
}
