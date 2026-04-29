/*
 * NotificationManager.cs — Gerenciamento de notificações push locais
 * Propósito: Agenda e cancela notificações locais usando Unity Mobile Notifications.
 *            Diferente do NotificationSystem.cs (toasts in-game), este gerencia notificações
 *            do sistema operacional que aparecem mesmo com o app fechado.
 * Como usar: Adicione ao GameManager. Chame os métodos conforme eventos do jogo.
 * Dependências: Nenhuma (funciona standalone)
 *
 * Para integrar com Unity Mobile Notifications:
 *   1. Instale o pacote com.unity.mobile.notifications via Package Manager
 *   2. Configure em Edit > Project Settings > Mobile Notifications
 *   3. O símbolo UNITY_NOTIFICATIONS será definido automaticamente após instalar o package
 *   4. Descomente os blocos #if UNITY_NOTIFICATIONS abaixo
 *
 * IMPORTANTE: No Android 13+ (API 33), é necessário solicitar permissão em runtime.
 *             Este script faz isso automaticamente via RequestPermission().
 */

using System;
using UnityEngine;

// Importações do Unity Mobile Notifications — disponíveis quando o pacote estiver instalado
#if UNITY_NOTIFICATIONS
using Unity.Notifications.Android;
using Unity.Notifications.iOS;
#endif

public class NotificationManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Chaves de PlayerPrefs
    // -----------------------------------------------------------------------

    private const string KeyLivesNotificationId     = "Notif.LivesRecoveredId";
    private const string KeyEngagementNotificationId = "Notif.EngagementId";
    private const string KeyLastActiveTimestamp      = "Notif.LastActiveTimestamp";

    // -----------------------------------------------------------------------
    // Configuração
    // -----------------------------------------------------------------------

    [Header("Configuração")]
    [Tooltip("true = apenas loga no Console, não agenda notificações reais")]
    [SerializeField] private bool debugMode = true;

    [Header("Notificação — Vida Recuperada")]
    [SerializeField] private string livesNotifTitle   = "Sua vida recuperou! 🎮";
    [SerializeField] private string livesNotifMessage = "Você tem vidas disponíveis. Volte para jogar!";

    [Header("Notificação — Reengajamento (24h)")]
    [SerializeField] private string engagementNotifTitle   = "Novos jogos te esperando! 🕹️";
    [SerializeField] private string engagementNotifMessage = "Volte para o Game Scroller e descubra novidades!";

    // Canal de notificação no Android (obrigatório para Android 8+)
    private const string ChannelId = "game_scroller_channel";

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Start()
    {
        // Atualiza timestamp de último acesso
        UpdateLastActiveTimestamp();

        // Cancela notificações agendadas (o usuário voltou ao app)
        CancelAllScheduledNotifications();

        // Solicita permissão no Android 13+ (API 33)
        RequestPermission();

        // Configura o canal de notificação (Android 8+)
        SetupNotificationChannel();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            // App foi para background: agenda notificações
            UpdateLastActiveTimestamp();
            ScheduleEngagementNotification();
        }
        else
        {
            // App voltou ao foreground: cancela notificações pendentes
            UpdateLastActiveTimestamp();
            CancelAllScheduledNotifications();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            UpdateLastActiveTimestamp();
    }

    // -----------------------------------------------------------------------
    // API pública
    // -----------------------------------------------------------------------

    /// <summary>
    /// Agenda uma notificação quando as vidas do jogador vão acabar.
    /// Deve ser chamado pelo LifeSystem quando detectar que as vidas estão recarregando.
    /// </summary>
    /// <param name="secondsUntilRecovery">Tempo em segundos até a vida ser recuperada</param>
    public void ScheduleLivesRecoveredNotification(float secondsUntilRecovery)
    {
        if (secondsUntilRecovery <= 0)
            return;

        if (debugMode)
        {
            Debug.Log($"[NotificationManager] [Debug] Notificação de vida seria agendada para {secondsUntilRecovery:F0}s.");
            return;
        }

#if UNITY_NOTIFICATIONS && UNITY_ANDROID
        ScheduleAndroidNotification(
            id: GetOrCreateNotificationId(KeyLivesNotificationId),
            title: livesNotifTitle,
            message: livesNotifMessage,
            delaySeconds: (int)secondsUntilRecovery
        );
        Debug.Log($"[NotificationManager] Notificação de vida agendada para {secondsUntilRecovery:F0}s.");
#elif UNITY_NOTIFICATIONS && UNITY_IOS
        ScheduleIosNotification(
            identifier: KeyLivesNotificationId,
            title: livesNotifTitle,
            message: livesNotifMessage,
            delaySeconds: (int)secondsUntilRecovery
        );
#else
        Debug.Log("[NotificationManager] Unity Mobile Notifications não instalado.");
#endif
    }

    /// <summary>
    /// Agenda uma notificação de reengajamento após 24h de inatividade.
    /// Chamado automaticamente quando o app vai para background.
    /// </summary>
    public void ScheduleEngagementNotification()
    {
        const int delaySeconds = 24 * 60 * 60; // 24 horas

        if (debugMode)
        {
            Debug.Log($"[NotificationManager] [Debug] Notificação de reengajamento seria agendada para 24h.");
            return;
        }

#if UNITY_NOTIFICATIONS && UNITY_ANDROID
        ScheduleAndroidNotification(
            id: GetOrCreateNotificationId(KeyEngagementNotificationId),
            title: engagementNotifTitle,
            message: engagementNotifMessage,
            delaySeconds: delaySeconds
        );
        Debug.Log("[NotificationManager] Notificação de reengajamento agendada para 24h.");
#elif UNITY_NOTIFICATIONS && UNITY_IOS
        ScheduleIosNotification(
            identifier: KeyEngagementNotificationId,
            title: engagementNotifTitle,
            message: engagementNotifMessage,
            delaySeconds: delaySeconds
        );
#else
        Debug.Log("[NotificationManager] Unity Mobile Notifications não instalado.");
#endif
    }

    /// <summary>
    /// Cancela todas as notificações agendadas (chamado quando o app abre).
    /// </summary>
    public void CancelAllScheduledNotifications()
    {
        if (debugMode)
        {
            Debug.Log("[NotificationManager] [Debug] Notificações seriam canceladas aqui.");
            return;
        }

#if UNITY_NOTIFICATIONS && UNITY_ANDROID
        AndroidNotificationCenter.CancelAllScheduledNotifications();
        Debug.Log("[NotificationManager] Todas as notificações Android canceladas.");
#elif UNITY_NOTIFICATIONS && UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        Debug.Log("[NotificationManager] Todas as notificações iOS canceladas.");
#endif
    }

    // -----------------------------------------------------------------------
    // Configuração do canal (Android 8+ obrigatório)
    // -----------------------------------------------------------------------

    private void SetupNotificationChannel()
    {
        if (debugMode)
            return;

#if UNITY_NOTIFICATIONS && UNITY_ANDROID
        var channel = new AndroidNotificationChannel
        {
            Id          = ChannelId,
            Name        = "Game Scroller",
            Description = "Notificações do Game Scroller (vidas, novidades)",
            Importance  = Importance.Default
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        Debug.Log("[NotificationManager] Canal de notificação registrado.");
#endif
    }

    // -----------------------------------------------------------------------
    // Permissão de notificação (Android 13+ / iOS)
    // -----------------------------------------------------------------------

    private void RequestPermission()
    {
        if (debugMode)
            return;

#if UNITY_NOTIFICATIONS && UNITY_ANDROID
        // Android 13+ requer permissão em runtime (POST_NOTIFICATIONS)
        if (UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Debug.Log("[NotificationManager] Permissão de notificação já concedida.");
            return;
        }

        UnityEngine.Android.Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        Debug.Log("[NotificationManager] Solicitando permissão de notificação (Android 13+).");
#elif UNITY_NOTIFICATIONS && UNITY_IOS
        // iOS: solicita permissão via Unity Mobile Notifications
        StartCoroutine(RequestIosPermission());
#endif
    }

#if UNITY_NOTIFICATIONS && UNITY_IOS
    private System.Collections.IEnumerator RequestIosPermission()
    {
        var authRequest = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound, true);
        yield return authRequest;

        if (authRequest.IsError)
            Debug.LogError($"[NotificationManager] Erro ao solicitar permissão iOS: {authRequest.Error}");
        else
            Debug.Log($"[NotificationManager] Permissão iOS: Granted={authRequest.Granted}");
    }
#endif

    // -----------------------------------------------------------------------
    // Agendamento Android
    // -----------------------------------------------------------------------

#if UNITY_NOTIFICATIONS && UNITY_ANDROID
    private void ScheduleAndroidNotification(int id, string title, string message, int delaySeconds)
    {
        // Cancela notificação anterior com mesmo ID antes de reagendar
        AndroidNotificationCenter.CancelScheduledNotification(id);

        var notification = new AndroidNotification
        {
            Title       = title,
            Text        = message,
            FireTime    = DateTime.Now.AddSeconds(delaySeconds),
            SmallIcon   = "icon_small",   // Configure em Player Settings > Android > Notification
            LargeIcon   = "icon_large"
        };

        AndroidNotificationCenter.SendNotificationWithExplicitID(notification, ChannelId, id);
    }
#endif

    // -----------------------------------------------------------------------
    // Agendamento iOS
    // -----------------------------------------------------------------------

#if UNITY_NOTIFICATIONS && UNITY_IOS
    private void ScheduleIosNotification(string identifier, string title, string message, int delaySeconds)
    {
        // Remove notificação anterior com mesmo identifier
        iOSNotificationCenter.RemoveScheduledNotification(identifier);

        var timeTrigger = new iOSNotificationTimeIntervalTrigger
        {
            TimeInterval = TimeSpan.FromSeconds(delaySeconds),
            Repeats      = false
        };

        var notification = new iOSNotification
        {
            Identifier           = identifier,
            Title                = title,
            Body                 = message,
            ShowInForeground     = false,
            Trigger              = timeTrigger
        };

        iOSNotificationCenter.ScheduleNotification(notification);
    }
#endif

    // -----------------------------------------------------------------------
    // Utilitários
    // -----------------------------------------------------------------------

    private void UpdateLastActiveTimestamp()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetString(KeyLastActiveTimestamp, now.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Obtém ou cria um ID numérico único para uma notificação Android.
    /// </summary>
    private int GetOrCreateNotificationId(string key)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            // Gera um ID baseado no hash da chave para ser determinístico
            int id = Math.Abs(key.GetHashCode() % 10000);
            PlayerPrefs.SetInt(key, id);
            PlayerPrefs.Save();
            return id;
        }
        return PlayerPrefs.GetInt(key);
    }
}
