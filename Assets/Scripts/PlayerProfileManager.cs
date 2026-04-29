/*
 * PlayerProfileManager.cs — Gerenciamento de perfil do jogador
 * Propósito: Salva, carrega e atualiza o perfil do jogador usando PlayerPrefs + JSON.
 * Como usar: Adicione ao GameObject do GameManager. Acesse via GameManager.Instance.ProfileManager
 *            ou diretamente via PlayerProfileManager.Instance.
 * Dependências: PlayerProfile.cs (Data/PlayerProfile.cs)
 */

using System;
using UnityEngine;

public class PlayerProfileManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Singleton leve (compartilhado via GameManager, mas acessível diretamente)
    // -----------------------------------------------------------------------

    public static PlayerProfileManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Estado
    // -----------------------------------------------------------------------

    private const string ProfileKey = "PlayerProfileManager.Profile";

    public PlayerProfile CurrentProfile { get; private set; }

    // -----------------------------------------------------------------------
    // Eventos
    // -----------------------------------------------------------------------

    public event Action<PlayerProfile> OnProfileLoaded;
    public event Action<PlayerProfile> OnProfileSaved;
    public event Action<int>           OnCoinsChanged;    // novo total de moedas
    public event Action<int>           OnCreditsChanged;  // novo total de créditos

    // -----------------------------------------------------------------------
    // Inicialização
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadProfile();
    }

    // -----------------------------------------------------------------------
    // Salvar / Carregar
    // -----------------------------------------------------------------------

    public void LoadProfile()
    {
        string json = PlayerPrefs.GetString(ProfileKey, string.Empty);

        if (string.IsNullOrEmpty(json))
        {
            CurrentProfile = PlayerProfile.CreateDefault();
            Debug.Log("[PlayerProfileManager] Nenhum perfil encontrado. Criando perfil padrão.");
        }
        else
        {
            try
            {
                CurrentProfile = JsonUtility.FromJson<PlayerProfile>(json);
                Debug.Log($"[PlayerProfileManager] Perfil carregado: {CurrentProfile.username}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerProfileManager] Erro ao carregar perfil: {ex.Message}. Criando novo.");
                CurrentProfile = PlayerProfile.CreateDefault();
            }
        }

        OnProfileLoaded?.Invoke(CurrentProfile);
    }

    public void SaveProfile()
    {
        try
        {
            string json = JsonUtility.ToJson(CurrentProfile);
            PlayerPrefs.SetString(ProfileKey, json);
            PlayerPrefs.Save();
            OnProfileSaved?.Invoke(CurrentProfile);
            Debug.Log($"[PlayerProfileManager] Perfil salvo: {CurrentProfile.username}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerProfileManager] Erro ao salvar perfil: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // Operações de moedas
    // -----------------------------------------------------------------------

    public bool AddCoins(int amount)
    {
        if (amount <= 0)
            return false;

        CurrentProfile.coins += amount;
        SaveProfile();
        OnCoinsChanged?.Invoke(CurrentProfile.coins);
        GameManager.NotifyCoinsChanged(CurrentProfile.coins);
        return true;
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || CurrentProfile.coins < amount)
        {
            Debug.LogWarning($"[PlayerProfileManager] Moedas insuficientes. Necessário: {amount}, disponível: {CurrentProfile.coins}");
            return false;
        }

        CurrentProfile.coins -= amount;
        SaveProfile();
        OnCoinsChanged?.Invoke(CurrentProfile.coins);
        GameManager.NotifyCoinsChanged(CurrentProfile.coins);
        return true;
    }

    // -----------------------------------------------------------------------
    // Operações de créditos
    // -----------------------------------------------------------------------

    public void AddCredits(int amount)
    {
        if (amount <= 0)
            return;

        CurrentProfile.credits += amount;
        SaveProfile();
        OnCreditsChanged?.Invoke(CurrentProfile.credits);
    }

    // -----------------------------------------------------------------------
    // Sessão de jogo
    // -----------------------------------------------------------------------

    public void RegisterGameSession(GameSessionResult result)
    {
        bool newHighScore = CurrentProfile.RegisterSession(result);

        // Adiciona XP pelo jogo
        int xpGained = Mathf.Max(10, result.score / 10);
        bool levelUp = CurrentProfile.AddXp(xpGained);

        // Adiciona moedas ganhas
        if (result.coinsEarned > 0)
            AddCoins(result.coinsEarned);

        SaveProfile();

        Debug.Log($"[PlayerProfileManager] Sessão registrada. Score: {result.score}, XP: +{xpGained}, LevelUp: {levelUp}");
    }

    // -----------------------------------------------------------------------
    // Atualização de dados de perfil
    // -----------------------------------------------------------------------

    public void UpdateUsername(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
            return;

        CurrentProfile.username = newUsername.Trim();
        SaveProfile();
    }

    public void UpdateAvatarColor(int colorIndex)
    {
        CurrentProfile.avatarColorIndex = colorIndex;
        SaveProfile();
    }

    public void ResetProfile()
    {
        CurrentProfile = PlayerProfile.CreateDefault();
        SaveProfile();
        OnProfileLoaded?.Invoke(CurrentProfile);
        Debug.Log("[PlayerProfileManager] Perfil reiniciado.");
    }
}
