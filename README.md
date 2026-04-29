# 🎮 Game Scroller

Um app mobile 2D em Unity estilo **"Roblox 2D"**: o usuário faz scroll vertical (como TikTok/Instagram) e descobre minijogos por categoria. Pode criar seus próprios jogos, ganhar créditos e trocá-los por gift cards.

![Game Scroller Banner](docs/banner-placeholder.png)

---

## 📱 Visão Geral

| Funcionalidade | Status |
|---|---|
| Feed infinito com categorias | ✅ |
| Card de jogo com likes e thumbnail | ✅ |
| Sistema de vidas (regeneração automática) | ✅ |
| Flappy Bird | ✅ |
| Nave x Aliens (Space Shooter) | ✅ |
| Endless Runner estilo Mario | ✅ |
| Tetris clássico | ✅ |
| Economia de moedas | ✅ |
| Perfil do jogador (XP, nível) | ✅ |
| Loja de itens | ✅ |
| Anúncios recompensados (mock) | ✅ |
| Gift Card | ✅ |
| Editor de fases (grid) | ✅ |
| Notificações toast in-game | ✅ |

---

## ⚙️ Como Configurar no Unity

### Versão Recomendada
**Unity 2022.3 LTS** ou superior.

### Packages Necessários
- **TextMeshPro** — `com.unity.textmeshpro`
- **Addressables** — `com.unity.addressables` (para carregamento dinâmico de jogos)
- Unity UI (incluído por padrão)

> Para instalar: `Window > Package Manager`, busque pelos nomes acima.

### Passos de Configuração

1. **Clone o repositório** e abra o projeto no Unity Hub.
2. Importe os packages acima via Package Manager.
3. Abra a cena `MainScene` (ou crie uma conforme a estrutura abaixo).
4. Crie um `GameObject` vazio chamado **GameManager** e adicione os componentes:
   - `GameManager`
   - `LifeSystem`
   - `GameEconomy`
   - `PlayerProfileManager`
   - `AdManager`
   - `GameSessionManager`
   - `NotificationSystem`
5. Configure as referências no Inspector do `GameManager`.
6. Configure o Canvas principal com os painéis de UI.

---

## 🗂️ Estrutura de Cenas Sugerida

```
Scenes/
├── MainScene.unity         ← Cena principal com feed e UI geral
├── FlappyBird.unity        ← Cena isolada do Flappy Bird
├── SpaceShooter.unity      ← Cena isolada do Space Shooter
├── EndlessRunner.unity     ← Cena isolada do Endless Runner
└── Tetris.unity            ← Cena isolada do Tetris
```

### Hierarquia do MainScene

```
MainScene
├── [GameManager] — Scripts: GameManager, LifeSystem, GameEconomy, PlayerProfileManager, AdManager, GameSessionManager
│
├── [Canvas] — UI principal
│   ├── FeedPanel
│   │   ├── CategoryTabs          ← GameFeedController
│   │   └── ScrollView > Content  ← GameCardUI (instâncias)
│   │
│   ├── ProfilePanel              ← ProfileScreenUI
│   ├── ShopPanel                 ← ShopScreenUI
│   ├── LivesHUD                  ← LivesUI
│   ├── NoLivesPopup              ← NoLivesPopup
│   ├── LoadingPanel              ← LoadingProgressUI
│   ├── ResultPanel               ← GameSessionManager
│   └── ToastContainer            ← NotificationSystem
│
└── [MainCamera]
```

---

## 🃏 Como Criar Dados de Minijogos

1. No Project, clique com botão direito em `Assets/Data/`
2. Escolha **Create > GameScroller > MiniGame Data**
3. Preencha os campos:
   - `Id` — identificador único (ex: `flappy_bird`)
   - `Titulo` — nome exibido no feed
   - `Categoria` — enum: Acao, Puzzle, Plataforma, Arcade, Shooter, Corrida
   - `Autor` — nome do criador
   - `Address Key` — chave no Addressables (para jogos carregados dinamicamente)
   - `Thumbnail Sprite` — imagem de capa
4. Adicione o asset criado à lista `All Games` do **GameFeedController** no Inspector.

---

## 🎮 Como Adicionar um Novo Minijogo

1. **Crie o script** em `Assets/Scripts/MiniGames/MeuJogo.cs` herdando de `MiniGameBase`:

```csharp
public class MeuJogo : MiniGameBase
{
    public override void StartGame()
    {
        BeginGame();
        // sua lógica aqui
    }

    public override void PauseGame()  { IsPaused = true; }
    public override void ResumeGame() { IsPaused = false; }

    protected override void OnPlayerDied()
    {
        TriggerGameOver(); // gasta vida + dispara evento
    }
}
```

2. **Crie uma cena** para o jogo com o prefab/GameObject tendo o script `MeuJogo`.
3. **Crie um `MiniGameData`** asset conforme descrito acima.
4. Para carregamento dinâmico: configure o asset no **Addressables** com a chave definida em `addressKey`.

---

## 💰 Como Configurar a Economia

Abra o componente `GameEconomy` e configure:

| Campo | Descrição |
|---|---|
| `Api Base Url` | URL da sua API de economia (ex: `https://api.meujogo.com`) |
| `Creator Id` | ID do criador do jogo para rastreamento de receita |
| `Debug Mode` | `true` para ver logs detalhados |

### Endpoints esperados na API

```
POST /api/reward/watch-ad
Body: { creatorId, deviceId, adType, timestamp }

POST /api/creator/balance
Body: { creatorId, deviceId }
```

---

## 📢 Como Configurar Anúncios Reais

O `AdManager` atualmente simula anúncios (3s delay). Para usar anúncios reais:

### Unity Ads
```csharp
// Em AdManager.cs, no método SimulateRewardedAd():
// Substitua o yield return WaitForSeconds por:
Advertisement.Show("Rewarded_Android", new ShowOptions { resultCallback = HandleShowResult });
```

### AdMob (Google)
```csharp
// Instale o pacote Google Mobile Ads SDK
// Em AdManager.cs, substitua a simulação por:
rewardedAd.Show((Reward reward) => { onComplete?.Invoke(true); });
```

---

## 🏆 Sistema de Créditos do Criador

Quando um usuário publica um jogo e outros jogadores o jogam:
1. O jogo registra a sessão via `GameSessionManager`
2. A sessão é enviada à API via `GameEconomy.WatchRewardedAd()`
3. O criador acumula créditos no backend
4. Créditos podem ser trocados por gift cards via `GiftCardRedeemer`

---

## 🗺️ Roadmap

- [ ] Backend com Firebase (autenticação, leaderboard, publicação de jogos)
- [ ] Sistema social: seguir criadores, comentários
- [ ] Leaderboard global por jogo
- [ ] Notificações push (vida recuperada, like recebido)
- [ ] Mais minijogos: Snake, Breakout, Dino Runner
- [ ] Editor de fases mais robusto (tileset visual)
- [ ] Analytics de jogo (tempo médio, retenção)
- [ ] Localização (i18n) para múltiplos idiomas

---

## 📁 Estrutura de Scripts

```
Assets/Scripts/
├── GameManager.cs                    ← Singleton central
├── GameSessionManager.cs             ← Fluxo feed→jogo→resultado
├── PlayerProfileManager.cs           ← Salva/carrega perfil
├── AdManager.cs                      ← Mock de anúncios
│
├── Data/
│   ├── GameData.cs                   ← ScriptableObject + MiniGameData + enum
│   └── PlayerProfile.cs              ← Modelo do perfil do jogador
│
├── MiniGames/
│   ├── MiniGameBase.cs               ← Classe base abstrata
│   ├── FlappyBirdGame.cs             ← Flappy Bird
│   ├── SpaceShooterGame.cs           ← Nave x Aliens
│   ├── EndlessRunnerGame.cs          ← Endless Runner
│   └── TetrisGame.cs                 ← Tetris
│
├── UI/
│   ├── GameFeedController.cs         ← Feed com abas de categorias
│   ├── GameCardUI.cs                 ← Card de jogo individual
│   ├── ProfileScreenUI.cs            ← Tela de perfil
│   ├── ShopScreenUI.cs               ← Loja
│   ├── LivesUI.cs                    ← Corações de vida
│   ├── NoLivesPopup.cs               ← Popup sem vidas
│   └── NotificationSystem.cs         ← Toasts animados
│
└── [scripts existentes...]
    ├── InfiniteScrollManager.cs
    ├── SnapScrollManager.cs
    ├── GameEconomy.cs
    ├── LifeSystem.cs
    ├── GiftCardRedeemer.cs
    ├── GridPlacementSystem.cs
    ├── DynamicMiniGameLoader.cs
    └── ...
```

---

## 📄 Licença

Projeto privado — todos os direitos reservados.

---

*Feito com ❤️ em Unity 2022.3 LTS*
