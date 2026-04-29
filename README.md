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
| Anúncios recompensados (AdMob real + mock debug) | ✅ |
| Anúncios interstitiais e banner (AdMob) | ✅ |
| Compras in-app Unity IAP (Google Play) | ✅ |
| Consentimento GDPR/LGPD (GamePrivacyManager) | ✅ |
| Avaliação in-app (Google Play Review) | ✅ |
| Deep Links de compartilhamento | ✅ |
| Notificações push locais | ✅ |
| AndroidManifest.xml para Play Store | ✅ |
| Build settings automatizado | ✅ |
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

## 📱 Publicação na Play Store

### Pré-requisitos

**Unity 2022.3 LTS** ou superior.

#### Packages necessários para Play Store

Instale via `Window > Package Manager`:

| Package | ID | Versão mínima | Finalidade |
|---|---|---|---|
| Google Mobile Ads | `com.google.ads.mobile` | v9.0+ | AdMob (anúncios) |
| Unity IAP | `com.unity.purchasing` | v4.9+ | Compras in-app |
| Google Play In-App Review | `com.google.play.review` | v1.8+ | Avaliação in-app |
| Unity Mobile Notifications | `com.unity.mobile.notifications` | v2.3+ | Notificações push locais |
| Google UMP SDK | incluído no `com.google.ads.mobile` | — | Consentimento GDPR |

> **Packages básicos** (já necessários):
> - TextMeshPro — `com.unity.textmeshpro`
> - Addressables — `com.unity.addressables`

---

### Configuração AdMob

1. Crie uma conta em [admob.google.com](https://admob.google.com)
2. Clique em **Adicionar App** → selecione Android → obtenha o **App ID**
3. Crie **Ad Units** (um de cada tipo):
   - Rewarded (vida grátis)
   - Interstitial (entre jogos)
   - Banner (topo/base do feed)
4. No Unity, selecione o GameObject **GameManager** e, no componente `AdManager`:
   - Cole o **App ID** em `Admob App Id`
   - Cole os **Ad Unit IDs** nos campos correspondentes
   - Defina `Debug Mode = false` antes de publicar
5. No `Assets/Plugins/Android/AndroidManifest.xml`:
   - Substitua `ca-app-pub-XXXXXXXXXXXXXXXX~XXXXXXXXXX` pelo seu **App ID** real

> **IDs de teste** (usar apenas em desenvolvimento):
> - App: `ca-app-pub-3940256099942544~3347511713`
> - Rewarded: `ca-app-pub-3940256099942544/5224354917`
> - Interstitial: `ca-app-pub-3940256099942544/1033173712`
> - Banner: `ca-app-pub-3940256099942544/6300978111`

---

### Configuração Unity IAP

1. Em `Window > Package Manager`, instale `com.unity.purchasing`
2. Ative em `Edit > Project Settings > Services > In-App Purchasing`
3. No [Google Play Console](https://play.google.com/console), crie os produtos:
   - `coins_100` — R$ 1,99 (consumível)
   - `coins_500` — R$ 4,99 (consumível)
   - `coins_2000` — R$ 14,99 (consumível)
   - `coins_10000` — R$ 49,99 (consumível)
   - `remove_ads` — R$ 9,99 (não-consumível)
   - `starter_pack` — R$ 2,99 (consumível)
4. Os IDs em `ShopScreenUI.cs` já estão configurados — confirme que coincidem com o Play Console

---

### Configuração de Consentimento (GDPR/LGPD)

1. No componente `GamePrivacyManager`:
   - Preencha `Privacy Policy Url` com a URL da sua política de privacidade
   - Preencha `Terms Of Service Url` com a URL dos seus termos de uso
   - Defina `Debug Mode = false` em produção
2. Crie o painel de UI de privacidade e conecte ao campo `Privacy Popup Panel`
3. A política de privacidade **deve estar publicada** em uma URL acessível antes de enviar para revisão

---

### Build para Play Store

#### Passo 1 — Configurar automaticamente

Execute no menu do Unity:
```
GameScroller > Setup > Configure Player Settings
```
Isso configura automaticamente:
- Bundle Identifier: `com.SEUNOME.gamescroller` *(substitua pelo seu)*
- Versão: 1.0.0, Bundle Version Code: 1
- Target Architecture: ARM64 *(obrigatório para Play Store)*
- Scripting Backend: IL2CPP *(obrigatório para Play Store)*
- API Level: mínimo 22, alvo 34 (Android 14)
- Orientação: Portrait
- App Bundle: `.aab`

#### Passo 2 — Configurar Keystore

Em `Edit > Project Settings > Player > Publishing Settings`:
- Crie ou selecione seu Keystore (`.jks`)
- **IMPORTANTE**: guarde o Keystore e as senhas com segurança — não é possível publicar atualizações sem ele

#### Passo 3 — Build

```
GameScroller > Build > Android Release (.aab)
```
O arquivo `.aab` será salvo em `Builds/Android/GameScroller-release.aab`

#### Passo 4 — Upload

1. Acesse o [Google Play Console](https://play.google.com/console)
2. Vá em **Produção > Versões**
3. Clique em **Criar nova versão**
4. Faça upload do `.aab`
5. Preencha as notas de versão

---

### Checklist Play Store

Antes de enviar para revisão, confirme:

- [ ] Política de privacidade publicada em URL acessível
- [ ] Bundle Identifier personalizado (sem `SEUNOME`)
- [ ] App IDs do AdMob substituídos (sem `XXXXXXX`)
- [ ] Keystore configurado e salvo em segurança
- [ ] Ícone do app: 512×512px (PNG, sem canal alfa nas bordas)
- [ ] Feature graphic: 1024×500px
- [ ] Screenshots: mínimo 2, recomendado 8 (portrait)
- [ ] Descrição em PT-BR preenchida
- [ ] Classificação de conteúdo respondida (questionário no Play Console)
- [ ] App Bundle assinado com Keystore de produção
- [ ] `Debug Mode = false` em todos os scripts
- [ ] Testes no dispositivo físico antes do upload

---

### Estrutura de Scripts — Play Store

```
Assets/Scripts/
├── AdManager.cs                      ← Anúncios AdMob (real + debug)
├── GamePrivacyManager.cs             ← Consentimento GDPR/LGPD
├── GooglePlayReviewManager.cs        ← Avaliação in-app
├── PlayStoreDeepLinkHandler.cs       ← Deep links de compartilhamento
├── NotificationManager.cs            ← Notificações push locais
│
├── UI/
│   └── ShopScreenUI.cs               ← Loja com Unity IAP
│
├── Editor/
│   └── BuildSettings.cs              ← Utilitário de build (menus)
│
└── Plugins/Android/
    └── AndroidManifest.xml           ← Manifest configurado para Play Store
```



Projeto privado — todos os direitos reservados.

---

*Feito com ❤️ em Unity 2022.3 LTS*
