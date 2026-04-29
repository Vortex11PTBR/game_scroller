# Grid Placement System - Editor de Fases 2D

## 📋 Visão Geral

O `GridPlacementSystem` permite criar um editor de fases com:
- ✅ **Grid Snapping**: Posicionar objetos em coordenadas inteiras
- ✅ **Anti-Sobreposição**: Impede objetos no mesmo slot
- ✅ **Exportação JSON**: Salvar fase como JSON para banco de dados
- ✅ **Importação JSON**: Carregar fase salva do banco
- ✅ **Debug Visual**: Visualizar grid com Gizmos
- ✅ **Touch + Mouse**: Funciona em mobile e editor

## 🏗️ Scripts Inclusos

### 1. **GridPlacementSystem.cs** - Núcleo do Sistema
Gerencia o grid, posicionamento e exportação de dados.

**Configuração no Inspector:**
- `gridWidth` - Largura do grid (padrão: 10)
- `gridHeight` - Altura do grid (padrão: 20)
- `cellSize` - Tamanho de cada célula em unidades (padrão: 1.0)
- `placeableAssets` - Array de prefabs para colocar
- `showDebugGrid` - Mostra gizmos do grid
- `gridColor` - Cor dos gizmos
- `debugMode` - Ativa logs detalhados

**Eventos Públicos:**
```csharp
event OnObjectPlaced(PlacedObject obj)
// Disparado quando um objeto é colocado

event OnObjectRemoved(Vector2Int position)
// Disparado quando um objeto é removido

event OnGridUpdated()
// Disparado quando grid é modificado
```

**Métodos Públicos - Posicionamento:**
```csharp
bool TryPlaceObject(Vector2Int gridPos, int assetIndex)
// Tenta colocar objeto (retorna sucesso/falha)

bool RemoveObject(Vector2Int gridPos)
// Remove objeto em posição

bool RemoveObjectById(string objectId)
// Remove objeto por ID

bool IsGridPositionOccupied(Vector2Int gridPos)
// Verifica se posição tem objeto

bool IsValidGridPosition(Vector2Int gridPos)
// Valida se posição está dentro do grid
```

**Métodos Públicos - Conversão:**
```csharp
Vector2Int WorldToGridPosition(Vector3 worldPos)
// Converte coordenadas mundo para grid

Vector3 GridToWorldPosition(Vector2Int gridPos)
// Converte coordenadas grid para mundo
```

**Métodos Públicos - Consulta:**
```csharp
PlacedObject GetObjectAtPosition(Vector2Int gridPos)
// Retorna objeto em posição (ou null)

List<PlacedObject> GetAllPlacedObjects()
// Retorna lista de todos os objetos

int GetPlacedObjectCount()
// Retorna quantidade de objetos
```

**Métodos Públicos - Export/Import:**
```csharp
string ExportToJSON()
// Exporta fase como JSON string

bool ImportFromJSON(string json)
// Importa fase a partir de JSON

void ClearGrid()
// Limpa todos os objetos
```

### 2. **GridEditorUI.cs** - Interface do Editor
UI para exportar, importar e gerenciar fase.

**Configuração no Inspector:**
- `gridSystem` - Referência ao sistema
- `gridInfoText` - Text mostrando quantidade de objetos
- `assetSelectorDropdown` - Dropdown para selecionar asset
- `exportButton` - Botão para exportar JSON
- `importButton` - Botão para importar JSON
- `clearButton` - Botão para limpar grid
- `jsonInputField` - InputField para JSON
- `selectedAssetText` - Text mostrando asset selecionado

### 3. **GridEditorExample.cs** - Exemplo de Uso
Demonstra métodos programáticos.

## 📊 Estrutura de Dados

### PlacedObject (em tempo de execução)
```csharp
public class PlacedObject
{
    public string id;                    // GUID único
    public Vector2Int gridPosition;      // Posição no grid
    public Vector3 worldPosition;        // Posição no mundo
    public int assetIndex;              // Índice no array de assets
    public string assetId;              // Nome do prefab
    public GameObject instance;         // Instância do objeto
    public DateTime placedAtTime;       // Quando foi colocado
}
```

### Formato JSON (Prisma)
```json
{
  "gridWidth": 10,
  "gridHeight": 20,
  "cellSize": 1.0,
  "placedObjects": [
    {
      "id": "a1b2c3d4",
      "assetId": "obstacle_wall",
      "assetIndex": 0,
      "gridX": 5,
      "gridY": 3,
      "worldX": 5.5,
      "worldY": 3.5,
      "placedAtTime": "2026-04-29T12:48:00Z"
    },
    {
      "id": "e5f6g7h8",
      "assetId": "collectible_coin",
      "assetIndex": 1,
      "gridX": 7,
      "gridY": 8,
      "worldX": 7.5,
      "worldY": 8.5,
      "placedAtTime": "2026-04-29T12:48:05Z"
    }
  ]
}
```

## 🛠️ Configuração no Editor Unity

### Passo 1: Preparar Cena

1. Crie um GameObject vazio chamado "GridSystem"
2. Adicione o componente `GridPlacementSystem`

### Passo 2: Configurar Grid

Configure no Inspector:
- **Grid Width**: 10 (largura)
- **Grid Height**: 20 (altura)
- **Cell Size**: 1.0 (tamanho da célula)
- **Show Debug Grid**: true (para visualizar)
- **Grid Color**: white (ou cor preferida)

### Passo 3: Adicionar Assets

1. Crie prefabs para cada tipo de objeto (parede, moeda, inimigo, etc)
2. No Inspector de `GridPlacementSystem`, expanda **Placeable Assets**
3. Defina tamanho do array
4. Atribua cada prefab:
   - Asset 0: "Parede"
   - Asset 1: "Moeda"
   - Asset 2: "Inimigo"
   - etc.

**Importante**: Cada prefab deve ter um tamanho ≤ cellSize

### Passo 4: Criar UI (Opcional)

1. Crie um Canvas com:
   - **Panel** para background
   - **Text** para info (ex: "Objetos: 5")
   - **Dropdown** para selecionar asset
   - **Button** "Exportar"
   - **Button** "Importar"
   - **Button** "Limpar"
   - **InputField** para JSON

2. Adicione componente `GridEditorUI` a um GameObject
3. Configure as referências

### Passo 5: Criar Câmera

Configure câmera ortográfica:
- **Projection**: Orthographic
- **Size**: Ajuste para visualizar grid completo
- **Position**: (5, 10, -10) ou similar

## 🎮 Como Usar

### No Editor (Mouse)
1. Selecione um asset no dropdown
2. Clique no grid para colocar
3. Clique em "Exportar" para salvar JSON

### No Mobile (Touch)
1. Toque no grid para colocar objeto
2. (Implementar remoção com long press)

### Programaticamente
```csharp
GridPlacementSystem grid = GetComponent<GridPlacementSystem>();

// Colocar objeto
grid.TryPlaceObject(new Vector2Int(5, 3), 0);  // Asset 0 em (5,3)

// Remover objeto
grid.RemoveObject(new Vector2Int(5, 3));

// Exportar para JSON
string json = grid.ExportToJSON();
SaveToDatabase(json);

// Importar de JSON
string loadedJson = LoadFromDatabase();
grid.ImportFromJSON(loadedJson);
```

## 💾 Integração com Banco de Dados (Prisma)

### Schema Prisma
```prisma
model GamePhase {
  id        String   @id @default(cuid())
  name      String
  data      String   @db.Text
  createdAt DateTime @default(now())
  updatedAt DateTime @updatedAt
  gameId    String
  game      Game     @relation(fields: [gameId], references: [id])
}
```

### Salvando no Backend
```csharp
public async Task SavePhase(string phaseName, string gridJSON)
{
    string json = JsonConvert.SerializeObject(new
    {
        name = phaseName,
        data = gridJSON,
        gameId = "seu_game_id"
    });

    var response = await api.PostAsync("/api/phases", json);
    return response.IsSuccessStatusCode;
}
```

### Backend (Node.js/Prisma)
```javascript
app.post("/api/phases", async (req, res) => {
  const { name, data, gameId } = req.body;

  try {
    const phase = await prisma.gamePhase.create({
      data: {
        name,
        data,
        gameId
      }
    });

    res.json({ success: true, phaseId: phase.id });
  } catch (error) {
    res.status(400).json({ success: false, error: error.message });
  }
});

app.get("/api/phases/:id", async (req, res) => {
  try {
    const phase = await prisma.gamePhase.findUnique({
      where: { id: req.params.id }
    });

    res.json(phase);
  } catch (error) {
    res.status(404).json({ error: "Phase not found" });
  }
});
```

## 📌 Fluxo de Uso

```
Abrir Editor
    ↓
Visualiza grid vazio
    ↓
Seleciona asset no dropdown
    ↓
Clica/toca no grid
    ↓
IsValidGridPosition() ✓
IsGridPositionOccupied() ✗
    ↓
TryPlaceObject() cria instância
    ↓
GridCell marcada como ocupada
    ↓
PlacedObject adicionado à lista
    ↓
ObjectPlaced event disparado
    ↓
Repete até terminar fase
    ↓
Clica "Exportar"
    ↓
ExportToJSON() serializa dados
    ↓
Copia JSON da InputField
    ↓
POST /api/phases com JSON
    ↓
Banco salva fase
    ↓
Fase pode ser carregada depois
```

## 🔧 Customizações

### Mudar tamanho do grid em runtime
```csharp
// Não há setter, seria necessário adicionar método
// Por enquanto, altere via Inspector antes de Play
```

### Implementar remoção por click longo
```csharp
// Adicionar em HandleInput():
if (Input.GetMouseButtonDown(1))  // Clique direito
{
    Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
    Vector2Int gridPos = WorldToGridPosition(worldPos);
    RemoveObject(gridPos);
}
```

### Adicionar prefab preview
```csharp
private GameObject previewInstance;

private void UpdatePreview(Vector2Int gridPos)
{
    if (IsGridPositionOccupied(gridPos))
    {
        // Mostrar X vermelho
    }
    else
    {
        // Mostrar prévia do objeto
    }
}
```

## 🎨 Exemplo Completo de Fase

JSON exportado:
```json
{
  "gridWidth": 10,
  "gridHeight": 15,
  "cellSize": 1.0,
  "placedObjects": [
    {
      "id": "wall_001",
      "assetId": "block_stone",
      "assetIndex": 0,
      "gridX": 0,
      "gridY": 0,
      "worldX": 0.5,
      "worldY": 0.5,
      "placedAtTime": "2026-04-29T12:00:00Z"
    },
    {
      "id": "coin_001",
      "assetId": "collectible_coin",
      "assetIndex": 1,
      "gridX": 5,
      "gridY": 7,
      "worldX": 5.5,
      "worldY": 7.5,
      "placedAtTime": "2026-04-29T12:01:00Z"
    },
    {
      "id": "enemy_001",
      "assetId": "enemy_goblin",
      "assetIndex": 2,
      "gridX": 8,
      "gridY": 5,
      "worldX": 8.5,
      "worldY": 5.5,
      "placedAtTime": "2026-04-29T12:02:00Z"
    }
  ]
}
```

## ⚠️ Limitações Atuais

- Não suporta objetos de múltiplas células (1x1 apenas)
- Sem undo/redo
- Sem validação de fase (ex: spawn point obrigatório)
- Sem busca/filtro de assets
- Grid é sempre 2D (não 3D)

## 🚀 Próximas Melhorias

- [ ] Suporte a objetos multi-célula (2x2, 3x2, etc)
- [ ] Sistema de undo/redo
- [ ] Validação de fase
- [ ] Ferramentas de paint (preencher área)
- [ ] Suporte a rotação de objetos
- [ ] Layers/height levels
- [ ] Teste de playabilidade inline
