# Addressables Dynamic Loader - Sistema de Carregamento Dinâmico

## 📋 Visão Geral

O sistema de **Addressables** carrega mini-games de forma assíncrona:
- ✅ Carrega prefabs remotamente via URL ou endereço Addressables
- ✅ Mostra **barra de progresso** durante download
- ✅ **Precarrega** próximos jogos enquanto usuário scrolla
- ✅ **Caching automático** para evitar recarregamentos
- ✅ Gerencia **fila de carregamento** com limite de concurrent loads
- ✅ Tratamento robusto de erros

## 🏗️ Scripts Inclusos

### 1. **AddressablesManager.cs** - Gerenciador Central
Núcleo que gerencia carregamento assíncrono com Addressables.

**Configuração no Inspector:**
- `preloadAssets` - Precarrega ativos na inicialização
- `maxConcurrentLoads` - Máximo de downloads simultâneos (padrão: 3)
- `debugMode` - Ativa logs detalhados

**Eventos Públicos:**
```csharp
event OnProgressChanged(string address, float progress)
// Disparado continuamente durante carregamento (0-1)

event OnAssetLoadFailed(string address, string error)
// Disparado quando falha carregamento
```

**Métodos Públicos:**
```csharp
LoadAssetAsync<T>(address, onComplete, onFailed, onProgress)
// Carrega ativo (genérico para qualquer tipo)

IsAssetLoaded(address)              // Verifica se já foi carregado
GetCachedAsset<T>(address)         // Retorna ativo em cache
UnloadAsset(address)                // Descarrega e libera memória
UnloadAll()                         // Descarrega todos os ativos

GetLoadQueueCount()                 // Retorna fila pendente
GetActiveLoadCount()                // Retorna carregamentos em progresso
```

### 2. **DynamicMiniGameLoader.cs** - Carregador de Mini-Games
Especializado em carregar mini-games com integração ao scroll.

**Configuração no Inspector:**
- `addressablesManager` - Referência ao gerenciador
- `spawnParent` - Transform onde instanciar (padrão: transform do script)
- `preloadOffset` - Distância de precarregamento (padrão: 1000px)
- `debugMode` - Ativa logs detalhados

**Eventos Públicos:**
```csharp
event OnGameLoaded(int index, string gameId, GameObject instance)
// Disparado quando prefab é carregado

event OnGameLoadFailed(int index, string address, string error)
// Disparado quando falha carregamento
```

**Métodos Públicos:**
```csharp
LoadGameDynamically(int index, string address, onLoaded)
// Carrega prefab sem instanciar

SpawnGameInstance(int index, string address, onSpawned)
// Carrega e instancia o prefab

UnloadGameAddress(string address)   // Descarrega um endereço

PreloadGameAtIndex(int index)       // Precarrega via Addressables
```

### 3. **LoadingProgressUI.cs** - UI de Progresso
Barra de carregamento visual com fade in/out.

**Configuração no Inspector:**
- `addressablesManager` - Referência ao gerenciador
- `canvasGroup` - CanvasGroup para fade (opacity)
- `progressBar` - Image com fillAmount (barra visual)
- `progressText` - Text para mostrar percentual (ex: "75%")
- `loadingMessageText` - Text para mensagem (ex: "Carregando minigame_2...")
- `fadeInDuration` - Tempo de fade in (padrão: 0.3s)
- `fadeOutDuration` - Tempo de fade out (padrão: 0.5s)
- `showProgressNumbers` - Mostra percentual (padrão: true)

**Métodos Públicos:**
```csharp
SetProgressBarColor(Color color)    // Muda cor da barra
SetLoadingMessage(string message)   // Define mensagem customizada
```

### 4. **AddressablesLoaderExample.cs** - Exemplo
Demonstra uso prático com UI interativa.

## 🛠️ Configuração no Editor Unity

### Passo 1: Configurar Window > Asset Management > Addressables

1. Abra **Window > Asset Management > Addressables > Groups**
2. Se não houver, clique **"Create Addressables Settings"**
3. Configure:
   - **Play Mode Script**: "Use Existing Build"
   - **Content Builders**: "Default Build Script"

### Passo 2: Marcar Prefabs como Addressables

1. Selecione seu prefab de mini-game
2. No Inspector, clique **"Addressable"** (checkbox)
3. Defina o endereço (ex: `minigame_0`, `minigame_1`, etc.)
4. Grupo: `Default` (ou crie grupo customizado)

**Exemplo de Endereços:**
```
minigame_0
minigame_1
minigame_jump
minigame_tap
minigame_dash
```

### Passo 3: Configurar Managers

1. Crie um GameObject vazio chamado "AddressablesManager"
2. Adicione o componente `AddressablesManager`
3. Configure:
   - **maxConcurrentLoads**: 3
   - **debugMode**: true (development)

### Passo 4: Adicionar Loader à Cena

Se já usa `InfiniteScrollManager`:

1. Selecione o GameObject com `InfiniteScrollManager`
2. Adicione componente `DynamicMiniGameLoader`
3. Configure:
   - **Addressables Manager**: Atribua o manager
   - **spawnParent**: Content do ScrollRect
   - **debugMode**: true

### Passo 5: Adicionar UI de Progresso

1. Crie um Canvas para loading (sobreposto)
2. Crie:
   - **Panel** (background semi-transparente)
   - **Image** com Fillable (para barra de progresso)
   - **Text** para percentual (ex: "75%")
   - **Text** para mensagem (ex: "Carregando...")

3. Adicione componente `LoadingProgressUI`
4. Configure:
   - **Canvas Group**: Atribua do panel
   - **Progress Bar**: Atribua a Image fillable
   - **Progress Text**: Atribua o Text de %
   - **Loading Message Text**: Atribua o Text de mensagem

## 📦 Fluxo de Carregamento

```
Usuário scrolla para próximo jogo
    ↓
InfiniteScrollManager.GameSelected disparado
    ↓
DynamicMiniGameLoader.PreloadGameAtIndex() chamado
    ↓
AddressablesManager.LoadAssetAsync() enfileira carregamento
    ↓
LoadingProgressUI detecta ProgressChanged event
    ↓
Barra de progresso fade in com LoadingProgressUI.ShowLoading()
    ↓
Enquanto carrega:
├─ progress (0-1) atualizado
├─ progressBar.fillAmount = progress
├─ progressText.text = "75%"
├─ loadingMessageText.text = "Carregando minigame_5..."
    ↓
Carregamento completo
    ↓
progress = 1.0
    ↓
Prefab adicionado ao cache
    ↓
GameLoaded event disparado
    ↓
Barra de progresso fade out
    ↓
Próximo jogo pronto para uso
```

## 🌐 Suporte a URLs Remotas

Para carregar de servidor remoto, use **RemoteUrl** em Addressables:

### No Editor:
1. **Window > Asset Management > Addressables > Groups**
2. Selecione grupo de prefabs
3. **Inspector > Profile > Remote.LoadPath**: Configure URL base
4. Exemplo: `https://seu-cdn.com/addressables/[BuildTarget]/`

### No Script:
```csharp
string remoteAddress = "minigame_jump";  // Addressables resolve URL automaticamente
addressablesManager.LoadAssetAsync<GameObject>(
    remoteAddress,
    (prefab) => Debug.Log("Carregado de CDN!"),
    (error) => Debug.LogError(error)
);
```

## 💻 Exemplo de Uso Avançado

```csharp
public class GameScroller : MonoBehaviour
{
    [SerializeField] private DynamicMiniGameLoader loader;

    private void Start()
    {
        loader.GameLoaded += OnGameLoaded;
        loader.GameLoadFailed += OnGameLoadFailed;
    }

    private void OnGameLoaded(int index, string gameId, GameObject instance)
    {
        Debug.Log($"Mini-game #{index} carregado: {gameId}");
        
        // Inicializar lógica do jogo
        MiniGameLogic logic = instance.GetComponent<MiniGameLogic>();
        if (logic != null)
            logic.Initialize(index);
    }

    private void OnGameLoadFailed(int index, string address, string error)
    {
        Debug.LogError($"Falha ao carregar {address}: {error}");
        // Mostrar UI de erro
        // Retry com exponential backoff
    }

    public void PreloadRange(int startIndex, int endIndex)
    {
        for (int i = startIndex; i <= endIndex; i++)
        {
            string address = $"minigame_{i}";
            loader.LoadGameDynamically(i, address, (prefab) => 
            {
                Debug.Log($"Precarregado: minigame_{i}");
            });
        }
    }
}
```

## 🔧 Customizações

### Mudar cor da barra de progresso
```csharp
LoadingProgressUI ui = GetComponent<LoadingProgressUI>();
ui.SetProgressBarColor(Color.green);
```

### Customizar mensagem de loading
```csharp
ui.SetLoadingMessage("Baixando seu mini-game incrível...");
```

### Aumentar limite de downloads simultâneos
```csharp
// No Inspector: maxConcurrentLoads = 5
// Cuidado com banda e memória!
```

### Implementar retry automático
```csharp
private int retryCount = 0;
private void LoadWithRetry(string address, int maxRetries = 3)
{
    addressablesManager.LoadAssetAsync<GameObject>(
        address,
        (prefab) => retryCount = 0,
        (error) =>
        {
            if (retryCount < maxRetries)
            {
                retryCount++;
                StartCoroutine(RetryAfterDelay(address, 2f));
            }
        }
    );
}

private IEnumerator RetryAfterDelay(string address, float delay)
{
    yield return new WaitForSeconds(delay);
    LoadWithRetry(address);
}
```

## 📊 Otimizações de Memória

**Liberar ativos não usados:**
```csharp
// Quando usuário sai de um mini-game:
addressablesManager.UnloadAsset("minigame_5");
addressablesManager.UnloadAsset("minigame_6");

// Liberar tudo:
addressablesManager.UnloadAll();
```

**Limite de concurrent loads:**
```
maxConcurrentLoads = 2-3 para mobile
maxConcurrentLoads = 5-10 para desktop
```

## ⚠️ Importante para Produção

Antes de lançar:
- [ ] Testar em conexão 3G/4G real
- [ ] Monitorar uso de memória (profiler)
- [ ] Configurar CDN para Assets remotos
- [ ] Implementar fallback offline (cache)
- [ ] Desativar debugMode
- [ ] Testar preload com scroll rápido
- [ ] Validar compressão de assets

## 🐛 Debugging

**Ative Debug Mode** para ver:
```
[AddressablesManager] Iniciando carregamento: minigame_5
[AddressablesManager] Ativo carregado com sucesso: minigame_5
[DynamicMiniGameLoader] Progresso de minigame_5: 75%
[LoadingProgressUI] Fade in: 1.0 segundos
```

**Monitorar progresso em tempo real:**
```csharp
addressablesManager.ProgressChanged += (address, progress) =>
{
    Debug.Log($"{address}: {progress:P0}");
};
```

## 📈 Performance

- ⚡ **Max concurrent**: 3 downloads simultâneos (default)
- 📦 **Cache**: Prefabs carregados permanecem em memória
- 🔄 **Preload**: Próximos 2 jogos são precarregados
- 💾 **Memory**: Unload ativos não usados
- 🌐 **Remote**: CDN com compressão recomendado

## 🎯 Boas Práticas

✅ **Faça:**
- Precarregue os próximos 2-3 items enquanto usuário está vendo
- Use maxConcurrentLoads = 3 em mobile
- Descarregue items muito antigos (5+ items atrás)
- Implemente fallback se download falhar
- Use compression de assets

❌ **Evite:**
- Carregar muito antecipadamente (desperdiça banda)
- maxConcurrentLoads > 5 em mobile
- Não descarregar ativos (memory leak)
- Confiar que internet sempre está disponível
- Mudar endereços sem versionamento
