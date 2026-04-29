/*
 * PlayerProfile.cs — Perfil do jogador
 * Propósito: Modelo de dados que representa o perfil completo do jogador, incluindo moedas,
 *            créditos, XP, histórico e jogos criados.
 * Como usar: Instanciado e gerenciado pelo PlayerProfileManager.
 * Dependências: GameData.cs (GameSessionResult)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerProfile
{
    // -----------------------------------------------------------------------
    // Identidade
    // -----------------------------------------------------------------------

    public string userId;
    public string username;
    public string avatarUrl;
    public int avatarColorIndex;   // índice de cor selecionada para avatar

    // -----------------------------------------------------------------------
    // Economia
    // -----------------------------------------------------------------------

    public int coins;
    public int credits;            // créditos ganhos como criador

    // -----------------------------------------------------------------------
    // Progressão
    // -----------------------------------------------------------------------

    public int nivel;
    public int xp;
    public int totalLikes;

    // -----------------------------------------------------------------------
    // Jogos
    // -----------------------------------------------------------------------

    public int gamesCreated;
    public int gamesPlayed;
    public List<string> createdGameIds = new List<string>();

    // -----------------------------------------------------------------------
    // Histórico de sessões
    // -----------------------------------------------------------------------

    public List<GameSessionResult> sessionHistory = new List<GameSessionResult>();

    // -----------------------------------------------------------------------
    // Ranking local — top scores por jogo (gameId → score)
    // -----------------------------------------------------------------------

    public List<LocalHighScore> highScores = new List<LocalHighScore>();

    // -----------------------------------------------------------------------
    // Preferências
    // -----------------------------------------------------------------------

    public bool adsRemoved;

    // -----------------------------------------------------------------------
    // Métodos utilitários
    // -----------------------------------------------------------------------

    // Retorna o XP necessário para o próximo nível
    public int GetXpForNextLevel()
    {
        return (nivel + 1) * 500;
    }

    // Retorna o progresso de 0-1 no nível atual
    public float GetLevelProgress()
    {
        int xpForNext = GetXpForNextLevel();
        return xpForNext > 0 ? Mathf.Clamp01((float)xp / xpForNext) : 0f;
    }

    // Adiciona XP pelo jogo e sobe de nível se necessário; retorna true se subiu de nível
    public bool AddXp(int amount)
    {
        xp += amount;
        bool leveledUp = false;
        int xpNeeded = GetXpForNextLevel();
        while (xp >= xpNeeded)
        {
            xp -= xpNeeded;
            nivel++;
            leveledUp = true;
            xpNeeded = GetXpForNextLevel();
        }
        return leveledUp;
    }

    // Registra uma sessão no histórico e atualiza high score
    public bool RegisterSession(GameSessionResult result)
    {
        sessionHistory.Add(result);
        gamesPlayed++;

        // Limita histórico a 50 sessões
        if (sessionHistory.Count > 50)
            sessionHistory.RemoveAt(0);

        // Atualiza high score
        LocalHighScore existing = highScores.Find(h => h.gameId == result.gameId);
        if (existing == null)
        {
            highScores.Add(new LocalHighScore { gameId = result.gameId, score = result.score });
            result.isNewHighScore = true;
            return true;
        }
        else if (result.score > existing.score)
        {
            existing.score = result.score;
            result.isNewHighScore = true;
            return true;
        }

        return false;
    }

    // Retorna o high score para um jogo, ou 0 se não há registro
    public int GetHighScore(string gameId)
    {
        LocalHighScore entry = highScores.Find(h => h.gameId == gameId);
        return entry?.score ?? 0;
    }

    // Cria um perfil novo com valores padrão
    public static PlayerProfile CreateDefault()
    {
        return new PlayerProfile
        {
            userId      = Guid.NewGuid().ToString("N").Substring(0, 12),
            username    = "Jogador",
            avatarColorIndex = 0,
            coins       = 100,
            credits     = 0,
            nivel       = 1,
            xp          = 0,
            totalLikes  = 0,
            gamesCreated= 0,
            gamesPlayed = 0,
            adsRemoved  = false
        };
    }
}

// -----------------------------------------------------------------------
// Entrada de ranking local
// -----------------------------------------------------------------------

[Serializable]
public class LocalHighScore
{
    public string gameId;
    public int score;
}
