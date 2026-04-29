/*
 * NotificationSystem.cs — Sistema de toast notifications in-game
 * Propósito: Exibe notificações animadas (toasts) em fila: moedas ganhas, vida recuperada, etc.
 * Como usar: Adicione ao GameManager ou Canvas raiz. Chame ShowNotification() de qualquer sistema.
 * Dependências: UnityEngine.UI, coroutines
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationSystem : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Tipos de notificação
    // -----------------------------------------------------------------------

    public enum NotificationType
    {
        Sucesso,  // verde
        Info,     // azul
        Alerta,   // amarelo
        Erro      // vermelho
    }

    // -----------------------------------------------------------------------
    // Configuração
    // -----------------------------------------------------------------------

    [Header("Prefab do Toast")]
    [SerializeField] private GameObject toastPrefab;    // prefab com Text e Image
    [SerializeField] private Transform toastContainer;  // RectTransform de ancoragem

    [Header("Animação")]
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float slideDuration   = 0.25f;
    [SerializeField] private float slideDistance   = 80f;

    [Header("Cores por Tipo")]
    [SerializeField] private Color colorSucesso  = new Color(0.15f, 0.72f, 0.3f);
    [SerializeField] private Color colorInfo     = new Color(0.15f, 0.5f, 0.9f);
    [SerializeField] private Color colorAlerta   = new Color(0.95f, 0.75f, 0.1f);
    [SerializeField] private Color colorErro     = new Color(0.9f, 0.2f, 0.2f);

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private Queue<(string message, NotificationType type)> notificationQueue =
        new Queue<(string, NotificationType)>();

    private bool isShowing = false;

    // -----------------------------------------------------------------------
    // Singleton simples (não DontDestroyOnLoad — usa o do GameManager)
    // -----------------------------------------------------------------------

    public static NotificationSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    // -----------------------------------------------------------------------
    // API pública
    // -----------------------------------------------------------------------

    public static void Show(string message, NotificationType type = NotificationType.Info)
    {
        if (Instance != null)
            Instance.ShowNotification(message, type);
        else
            Debug.LogWarning($"[NotificationSystem] Instância não encontrada. Mensagem perdida: {message}");
    }

    public void ShowNotification(string message, NotificationType type = NotificationType.Info)
    {
        notificationQueue.Enqueue((message, type));

        if (!isShowing)
            StartCoroutine(ProcessQueue());
    }

    // -----------------------------------------------------------------------
    // Internos
    // -----------------------------------------------------------------------

    private IEnumerator ProcessQueue()
    {
        isShowing = true;

        while (notificationQueue.Count > 0)
        {
            var (message, type) = notificationQueue.Dequeue();
            yield return StartCoroutine(ShowToast(message, type));
            yield return new WaitForSeconds(0.1f);
        }

        isShowing = false;
    }

    private IEnumerator ShowToast(string message, NotificationType type)
    {
        if (toastPrefab == null || toastContainer == null)
        {
            Debug.LogWarning($"[NotificationSystem] Prefab ou container não configurado: {message}");
            yield break;
        }

        GameObject toast = Instantiate(toastPrefab, toastContainer);
        CanvasGroup cg   = toast.GetComponent<CanvasGroup>();
        if (cg == null) cg = toast.AddComponent<CanvasGroup>();

        // Configura texto e cor
        Text label = toast.GetComponentInChildren<Text>();
        if (label != null) label.text = message;

        Image background = toast.GetComponent<Image>();
        if (background != null) background.color = GetColor(type);

        RectTransform rect = toast.GetComponent<RectTransform>();

        // Entrada: slide + fade in
        float elapsed = 0f;
        Vector2 hiddenPos = rect.anchoredPosition + Vector2.up * slideDistance;
        Vector2 shownPos  = rect.anchoredPosition;

        cg.alpha = 0f;
        rect.anchoredPosition = hiddenPos;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / slideDuration;
            cg.alpha = t;
            rect.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, EaseOut(t));
            yield return null;
        }

        cg.alpha = 1f;
        rect.anchoredPosition = shownPos;

        yield return new WaitForSeconds(displayDuration);

        // Saída: fade out + slide
        elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / slideDuration;
            cg.alpha = 1f - t;
            rect.anchoredPosition = Vector2.Lerp(shownPos, hiddenPos, EaseIn(t));
            yield return null;
        }

        Destroy(toast);
    }

    private Color GetColor(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Sucesso: return colorSucesso;
            case NotificationType.Info:    return colorInfo;
            case NotificationType.Alerta:  return colorAlerta;
            case NotificationType.Erro:    return colorErro;
            default:                       return colorInfo;
        }
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    private static float EaseIn(float t)  => t * t;
}
