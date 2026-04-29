# Sistema de Scroll Vertical Infinito - Guia de Configuração

## 📋 Visão Geral
O sistema carrega e descarrega Prefabs de mini-games dinamicamente, mantendo apenas **3 ativos** (anterior, atual, próximo) para economizar memória.

## 🏗️ Scripts Inclusos

### 1. **InfiniteScrollManager.cs**
Gerencia o scroll infinito e o ciclo de vida dos mini-games.

**Componentes Obrigatórios:**
- `ScrollRect` - componente de scroll
- `contentPanel` (RectTransform) - painel de conteúdo do scroll

**Parâmetros Configuráveis:**
- `miniGamePrefabs` - array de prefabs para os mini-games
- `cellHeight` - altura de cada mini-game (padrão: 500)
- `scrollThreshold` - sensibilidade de carregamento

**Métodos Públicos:**
```csharp
ClearAll()                  // Limpa todos os mini-games
GetTotalGameCount()         // Retorna quantidade total (infinita)
GetCurrentVisibleIndex()    // Retorna índice atual visível
```

### 2. **MiniGame.cs**
Componente base para cada mini-game. Inicializa dados do jogo quando for ativado.

**Propriedades:**
- `titleText` - título do mini-game
- `descriptionText` - descrição
- `backgroundImage` - fundo colorido aleatoriamente

## 🛠️ Configuração no Editor Unity

### Passo 1: Preparar Hierarquia UI
1. Crie um `Canvas` (se não existir)
2. Dentro do Canvas, crie um `ScrollRect` vazio
3. Adicione um filho `Content` (RectTransform) ao ScrollRect

### Passo 2: Configurar ScrollRect
1. Selecione o ScrollRect
2. Configure:
   - **Scroll Direction**: Vertical
   - **Vertical Scrollbar**: (opcional)
   - **Movement Type**: Elastic ou Scroll
   - **Scroll Sensitivity**: ~10

### Passo 3: Criar Prefab de Mini-Game
1. Crie um `GameObject` vazio como prefab base
2. Adicione um `Image` para o background
3. Adicione um `Button` (com Text) para o conteúdo
4. Anexe o script `MiniGame.cs`
5. Configure as referências (Title Text, Description Text, Background Image)
6. **Importante**: O prefab não precisa de Layout Group; o manager configura tamanho e posição

### Passo 4: Anexar InfiniteScrollManager
1. Selecione o Canvas ou um GameObject pai
2. Adicione o componente `InfiniteScrollManager`
3. Configure:
   - **ScrollRect**: Atribua o ScrollRect da cena
   - **ContentPanel**: Atribua o Content RectTransform
   - **miniGamePrefabs**: Atribua 1 ou mais prefabs (ex: MiniGame_Prefab)
   - **cellHeight**: 500 (ou o tamanho desejado)

## 🎮 Fluxo de Funcionamento

```
Usuário scrolla down
    ↓
OnScrollValueChanged() detecta mudança
    ↓
UpdateVisibleGames() calcula índice visível
    ↓
Carrega mini-game (atual + próximo) se necessário
Descarrega mini-game mais antigo
    ↓
Mantém sempre 3 ativos (anterior, atual, próximo)
```

## 💾 Otimizações de Memória

- **Object Pooling**: Mini-games inativos ficam na fila para reuso
- **3 Jogos Máximos**: Nunca mantém mais que 3 instanciados
- **Reciclagem de Prefabs**: Mesmos prefabs são reutilizados em padrão cíclico

## 📝 Exemplo de Uso

```csharp
// Obter o gerenciador (já configurado na cena)
InfiniteScrollManager manager = GetComponent<InfiniteScrollManager>();

// Limpar tudo quando sair da cena
manager.ClearAll();

// Saber qual mini-game está visível
int currentIndex = manager.GetCurrentVisibleIndex();
Debug.Log($"Visualizando mini-game #{currentIndex}");
```

## 🐛 Dicas de Debug

1. **Scroll não funciona**: Verifique se o ScrollRect tem um Layout Group (Vertical Layout Group)
2. **Jogos não carregam**: Confirme que os prefabs estão atribuídos
3. **Memória alta**: Aumente `cellHeight` se os jogos são muito grandes, ou reduza a qualidade de imagens
4. **Performance baixa**: Reduza a complexidade visual dos prefabs ou aumente `scrollThreshold`

## 🔧 Customizações Possíveis

### Mudar quantidade de jogos mantidos ativos
No `InfiniteScrollManager.cs`, altere a lógica em `LoadGamesAroundIndex()`:
```csharp
for (int i = -1; i <= 1; i++)  // Mude os limites para ±2, etc.
```

### Suporte a scroll horizontal
Modifique `UpdateVisibleGames()` para usar `scrollPosition.x` em vez de `.y`

### Reações ao mudar de jogo
Adicione um `UnityEvent` chamado quando o índice muda:
```csharp
public UnityEvent<int> onGameIndexChanged;
```
