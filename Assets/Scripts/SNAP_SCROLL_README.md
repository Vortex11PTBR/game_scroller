# Snap Scroll Manager - Sistema de Scroll com Snap Vertical

## 📋 Visão Geral

O `SnapScrollManager` é um sistema de scroll vertical que:
- ✅ Identifica qual objeto está mais próximo do **centro da tela**
- ✅ Centraliza automaticamente ao **soltar o toque**
- ✅ Dispara eventos `OnGameSelected` e `OnGameExited`
- ✅ Otimiza CPU desativando items fora da tela
- ✅ Suporta animação suave de snap com easing customizável

## 🏗️ Scripts Inclusos

### 1. **SnapScrollManager.cs** - Núcleo do Sistema
Gerencia o snap scrolling vertical e detecção de items.

**Configuração no Inspector:**
- `scrollRect` - ScrollRect da cena
- `snapDuration` - Tempo de animação do snap (padrão: 0.5s)
- `snapEase` - Curva de animação (EaseInOut padrão)
- `snapThreshold` - Distância mínima para snap (padrão: 50px)

**Eventos Públicos:**
```csharp
event OnGameSelected(int itemIndex, string gameId)
// Disparado quando um item é centralizado

event OnGameExited(int itemIndex, string gameId)
// Disparado quando um item sai da tela
```

**Métodos Públicos:**
```csharp
GetCurrentSelectedIndex()           // Retorna índice do item selecionado
GetCurrentSelectedItem()            // Retorna o SnapScrollItem selecionado
GetItemAtIndex(int index)          // Retorna item em índice específico
GetItemCount()                      // Retorna quantidade total de items
AddItem(SnapScrollItem item)       // Adiciona novo item dinâmico
RemoveItemAtIndex(int index)       // Remove item em índice
ScrollToItemAtIndex(int index)     // Scroll programático para item
```

### 2. **SnapScrollItem.cs** - Componente Base para cada Item
Script que cada item filho deve ter.

**Configuração no Inspector:**
- `gameId` - ID único do jogo (ex: "game_1", "minigame_dash")
- `canvasGroup` - CanvasGroup para controlar transparência
- `highlightImage` - Image para destaque visual
- `selectedAlpha` - Opacidade quando selecionado (padrão: 1.0)
- `deselectedAlpha` - Opacidade quando deseleccionado (padrão: 0.6)
- `selectedScale` - Escala quando selecionado (padrão: 1.1)
- `deselectedScale` - Escala quando deseleccionado (padrão: 0.9)
- `scaleDuration` - Tempo da animação de escala (padrão: 0.3s)

**Métodos Públicos:**
```csharp
Select()                    // Seleciona o item (chamado automaticamente)
Deselect()                  // Deseleciona o item
GetGameId()                // Retorna ID do jogo
GetItemIndex()             // Retorna índice do item
IsSelected()               // Retorna se está selecionado
SetGameId(string newId)    // Altera ID do jogo
```

**Para Customizar Comportamento:**
```csharp
protected virtual void OnSelected()     // Override para lógica ao selecionar
protected virtual void OnDeselected()   // Override para lógica ao desselecionar
```

### 3. **SnapScrollExample.cs** - Exemplo de Implementação
Mostra como usar o manager e responder aos eventos.

## 🛠️ Configuração no Editor Unity

### Passo 1: Preparar Hierarquia UI

```
Canvas
├── ScrollRect (componente ScrollRect)
│   └── Content (RectTransform - filho do ScrollRect)
│       ├── MiniGame_1 (SnapScrollItem)
│       ├── MiniGame_2 (SnapScrollItem)
│       └── MiniGame_3 (SnapScrollItem)
└── ControlPanel
    ├── SelectedGameText (Text)
    ├── PreviousButton (Button)
    └── NextButton (Button)
```

### Passo 2: Configurar ScrollRect

1. Selecione o ScrollRect no hierarquiador
2. Configure as propriedades:
   - **Scroll Direction**: Vertical
   - **Movement Type**: Elastic (recomendado)
   - **Elasticity**: 0.1
   - **Inertia**: Ligado
   - **Deceleration Rate**: 0.95
   - **Scroll Sensitivity**: 10-20

### Passo 3: Preparar Prefab de Item

Para cada item filho do ScrollRect:

1. Crie um GameObject com:
   - **RectTransform** (altura = tamanho desejado)
   - **Image** (background)
   - **Text** (título do jogo)
   - **CanvasGroup** (para controle de opacidade)
   - **SnapScrollItem** (script)

2. Configure **SnapScrollItem**:
   - **Game Id**: ID único (ex: "game_jumping")
   - **Canvas Group**: Atribua o CanvasGroup
   - **Highlight Image**: Image de destaque (opcional)
   - **Selected Alpha**: 1.0
   - **Deselected Alpha**: 0.6
   - **Selected Scale**: 1.1
   - **Deselected Scale**: 0.9

### Passo 4: Anexar SnapScrollManager

1. Crie um GameObject vazio como controlador
2. Adicione o componente `SnapScrollManager`
3. Configure:
   - **ScrollRect**: Atribua o ScrollRect
   - **Snap Duration**: 0.5
   - **Snap Ease**: EaseInOut
   - **Snap Threshold**: 50

### Passo 5: Conectar Eventos (UI)

```csharp
// No Inspector ou via código:
snapScrollManager.GameSelected += OnGameSelected;
snapScrollManager.GameExited += OnGameExited;
```

## 🎮 Fluxo de Funcionamento

```
Usuário toca e arrasta ScrollRect
    ↓
SnapScrollManager detecta isDragging = true
    ↓
ScrollRect se move normalmente (sem snap)
    ↓
Usuário solta o dedo
    ↓
isDragging = false
    ↓
GetClosestItemIndex() calcula item mais próximo do centro
    ↓
AnimateSnapToItem() anima o scroll para centralizar
    ↓
SelectItemAtIndex() atualiza seleção
    ↓
GameSelected event disparado com novo índice e gameId
    ↓
GameExited event disparado com índice/gameId anterior
    ↓
SnapScrollItem.Select() e Deselect() rodados
    ↓
Opacidade e escala animadas
```

## 📊 Arquitetura de Eventos

```
Usuário clica "Prev/Next"
    ↓
ScrollToItemAtIndex(targetIndex)
    ↓
SnapToItemAtIndex(targetIndex)
    ↓
AnimateSnapToItem(targetIndex)
    ↓
SelectItemAtIndex(targetIndex)
    ↓
GameExited?.Invoke(oldIndex, oldGameId)
GameSelected?.Invoke(newIndex, newGameId)
    ↓
SnapScrollExample.OnGameSelected() chamado
SnapScrollExample.OnGameExited() chamado
    ↓
UI atualizada com novo jogo
```

## 💡 Exemplo de Uso Avançado

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private SnapScrollManager snapScroll;

    private void Start()
    {
        snapScroll.GameSelected += HandleGameSelected;
        snapScroll.GameExited += HandleGameExited;
    }

    private void HandleGameSelected(int itemIndex, string gameId)
    {
        // Inicializar o mini-game
        LoadGameScene(gameId);
        
        // Atualizar UI
        UpdateGameInfo(gameId);
        
        // Iniciar lógica do jogo
        StartGameLogic(gameId);
    }

    private void HandleGameExited(int itemIndex, string gameId)
    {
        // Salvar progresso
        SaveGameProgress(gameId);
        
        // Pausar simulações
        PauseGameSimulation(gameId);
        
        // Liberar recursos
        UnloadGameAssets(gameId);
    }

    public void NavigateToGame(int index)
    {
        snapScroll.ScrollToItemAtIndex(index);
    }
}
```

## 🔧 Customizações Possíveis

### Mudar velocidade de snap
```csharp
snapScrollManager.snapDuration = 0.3f;  // Mais rápido
```

### Customizar easing
```csharp
snapEase = AnimationCurve.EaseInOut(0, 0, 1, 1);  // Suave
snapEase = AnimationCurve.Linear(0, 0, 1, 1);     // Linear
```

### Herdar SnapScrollItem para lógica customizada
```csharp
public class CustomGameItem : SnapScrollItem
{
    protected override void OnSelected()
    {
        // Seu código aqui
        PlaySelectSound();
        ParticleEffect();
    }
    
    protected override void OnDeselected()
    {
        // Seu código aqui
        StopMusic();
    }
}
```

### Scroll Horizontal
Modifique `GetClosestItemIndex()` para usar posição X em vez de Y:
```csharp
float distance = Mathf.Abs(viewportCenter.x - itemWorldPos.x);
```

## ⚠️ Requisitos

- **LeanTween**: Script usa `LeanTween.scale()` para animações
  - Se não tiver, substitua por `StartCoroutine()` customizado
  - Ou instale via Asset Store

## 🐛 Debugging

Adicione logs na derivação:
```csharp
private void OnGameSelected(int itemIndex, string gameId)
{
    Debug.Log($"Game Selected: {gameId} (Index: {itemIndex})");
}

private void OnGameExited(int itemIndex, string gameId)
{
    Debug.Log($"Game Exited: {gameId} (Index: {itemIndex})");
}
```

## 🎯 Boas Práticas

✅ **Faça:**
- Use `GameExited` para liberar recursos (áudio, física, etc)
- Use `GameSelected` para inicializar novo jogo
- Defina IDs descritivos (ex: "game_jumping", não "g1")
- Teste em dispositivos reais (scroll pode ser diferente)

❌ **Evite:**
- Não modifique `scrollRect.enabled` diretamente (use manager)
- Não mude posições de items durante snap
- Não instantie items novos durante o jogo (use pool)

## 📈 Performance

- ✨ Eventos otimizados: Disparados apenas na mudança
- ⚡ Sem updates constantes: Baseado em eventos
- 💾 Memory-efficient: Items deseleccionados podem ser desativados
- 🔄 Object pooling: Reutilize items com `RemoveItemAtIndex()` e `AddItem()`
