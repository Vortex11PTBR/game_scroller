/*
 * GameData.cs — Dados centrais do Game Scroller
 * Propósito: Define o enum de categorias, o ScriptableObject MiniGameData e outros modelos de dados.
 * Como usar: Crie assets MiniGameData via Assets > Create > GameScroller > MiniGame Data no Editor do Unity.
 * Dependências: Nenhuma (arquivo de dados puro).
 */

using System;
using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------
// Categoria dos minijogos
// ---------------------------------------------------------------------------

public enum GameCategory
{
    Todos,
    Acao,
    Puzzle,
    Plataforma,
    Arcade,
    Shooter,
    Corrida
}

// ---------------------------------------------------------------------------
// ScriptableObject — dados de um minijogo
// ---------------------------------------------------------------------------

[CreateAssetMenu(fileName = "NewMiniGame", menuName = "GameScroller/MiniGame Data")]
public class MiniGameData : ScriptableObject
{
    [Header("Identificação")]
    [SerializeField] public string id;
    [SerializeField] public string titulo;
    [SerializeField, TextArea] public string descricao;
    [SerializeField] public GameCategory categoria;
    [SerializeField] public string autor;

    [Header("Estatísticas")]
    [SerializeField] public int likes;
    [SerializeField] public int plays;

    [Header("Assets")]
    [SerializeField] public string addressKey;          // chave no Addressables
    [SerializeField] public Sprite thumbnailSprite;     // thumbnail do jogo
    [SerializeField] public Color corCategoria = Color.white; // cor temática

    [Header("Criador")]
    [SerializeField] public bool isCreatorMade;         // feito por usuário da plataforma

    // Retorna a cor padrão da categoria para uso nos cards
    public static Color GetCategoryColor(GameCategory category)
    {
        switch (category)
        {
            case GameCategory.Acao:      return new Color(0.9f, 0.2f, 0.2f);
            case GameCategory.Puzzle:    return new Color(0.2f, 0.5f, 0.9f);
            case GameCategory.Plataforma:return new Color(0.2f, 0.8f, 0.3f);
            case GameCategory.Arcade:    return new Color(0.9f, 0.6f, 0.1f);
            case GameCategory.Shooter:   return new Color(0.6f, 0.2f, 0.9f);
            case GameCategory.Corrida:   return new Color(0.1f, 0.8f, 0.8f);
            default:                     return new Color(0.5f, 0.5f, 0.5f);
        }
    }

    // Nome da categoria em português para exibição
    public static string GetCategoryDisplayName(GameCategory category)
    {
        switch (category)
        {
            case GameCategory.Todos:      return "Todos";
            case GameCategory.Acao:       return "Ação";
            case GameCategory.Puzzle:     return "Puzzle";
            case GameCategory.Plataforma: return "Plataforma";
            case GameCategory.Arcade:     return "Arcade";
            case GameCategory.Shooter:    return "Shooter";
            case GameCategory.Corrida:    return "Corrida";
            default:                      return "Outros";
        }
    }
}

// ---------------------------------------------------------------------------
// Sessão de jogo — resultado de uma partida
// ---------------------------------------------------------------------------

[Serializable]
public class GameSessionResult
{
    public string gameId;
    public string gameTitle;
    public int score;
    public int coinsEarned;
    public float playTimeSeconds;
    public DateTime sessionDate;
    public bool isNewHighScore;
}

// ---------------------------------------------------------------------------
// Pacote de moedas para a loja
// ---------------------------------------------------------------------------

[Serializable]
public class CoinPackage
{
    public string label;
    public int coins;
    public string priceDisplay; // ex: "R$ 4,99"
    public Color cardColor;
    public bool isBestValue;
}
