using System;
using UnityEngine;

public class LifeSystem : MonoBehaviour
{
    private const int MaxLives = 20;
    private static readonly TimeSpan RecoveryInterval = TimeSpan.FromMinutes(15);

    private const string LivesKey = "LifeSystem.CurrentLives";
    private const string LastLifeLostKey = "LifeSystem.LastLifeLostUtc";

    public int CurrentLives => currentLives;
    public bool HasLives => CurrentLives > 0;

    private int currentLives;
    private DateTime? lastLifeLostUtc;

    private void Awake()
    {
        LoadState();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveState();
        }
    }

    private void OnApplicationQuit()
    {
        SaveState();
    }

    public bool SpendLife()
    {
        RefreshLives();

        if (currentLives <= 0)
        {
            return false;
        }

        bool wasFull = currentLives == MaxLives;
        currentLives--;

        if (currentLives < MaxLives && (wasFull || !lastLifeLostUtc.HasValue))
        {
            lastLifeLostUtc = DateTime.UtcNow;
        }

        SaveState();
        return true;
    }

    public void AddRewardLives(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("AddRewardLives requires a value greater than zero.");
            return;
        }

        RefreshLives();
        currentLives = Mathf.Min(MaxLives, currentLives + amount);

        if (currentLives >= MaxLives)
        {
            lastLifeLostUtc = null;
        }
        else if (!lastLifeLostUtc.HasValue)
        {
            lastLifeLostUtc = DateTime.UtcNow;
        }

        SaveState();
    }

    public void RefreshLives()
    {
        if (currentLives >= MaxLives || !lastLifeLostUtc.HasValue)
        {
            if (currentLives >= MaxLives && lastLifeLostUtc.HasValue)
            {
                lastLifeLostUtc = null;
                SaveState();
            }

            return;
        }

        TimeSpan elapsed = DateTime.UtcNow - lastLifeLostUtc.Value;

        if (elapsed < RecoveryInterval)
        {
            return;
        }

        int recoveredLives = (int)(elapsed.TotalMinutes / RecoveryInterval.TotalMinutes);
        currentLives = Mathf.Min(MaxLives, currentLives + recoveredLives);

        if (currentLives >= MaxLives)
        {
            lastLifeLostUtc = null;
        }
        else
        {
            lastLifeLostUtc = lastLifeLostUtc.Value.AddTicks(RecoveryInterval.Ticks * recoveredLives);
        }

        SaveState();
    }

    public TimeSpan GetTimeUntilNextLife()
    {
        RefreshLives();

        if (currentLives >= MaxLives || !lastLifeLostUtc.HasValue)
        {
            return TimeSpan.Zero;
        }

        TimeSpan elapsed = DateTime.UtcNow - lastLifeLostUtc.Value;
        TimeSpan remaining = RecoveryInterval - elapsed;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    private void LoadState()
    {
        currentLives = Mathf.Clamp(PlayerPrefs.GetInt(LivesKey, MaxLives), 0, MaxLives);

        string savedTimestamp = PlayerPrefs.GetString(LastLifeLostKey, string.Empty);
        if (DateTime.TryParse(savedTimestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedTimestamp))
        {
            lastLifeLostUtc = parsedTimestamp.ToUniversalTime();
        }
        else
        {
            lastLifeLostUtc = currentLives < MaxLives ? DateTime.UtcNow : null;
        }

        RefreshLives();
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(LivesKey, currentLives);

        if (lastLifeLostUtc.HasValue)
        {
            PlayerPrefs.SetString(LastLifeLostKey, lastLifeLostUtc.Value.ToString("O"));
        }
        else
        {
            PlayerPrefs.DeleteKey(LastLifeLostKey);
        }

        PlayerPrefs.Save();
    }
}
