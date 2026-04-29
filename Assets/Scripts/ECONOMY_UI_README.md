# Economy UI Controller - Sistema de UI para Economia do Jogo

## 📋 Visão Geral

O sistema de **Economy UI** fornece:
- ✅ **Animação de moedas voando** da posição do anúncio até o contador
- ✅ **Formatação de números** (1500 → 1.5K, 1000000 → 1M)
- ✅ **Botão de Gift Card** desabilitado até atingir saldo mínimo
- ✅ **Atualização suave** de saldo com animação
- ✅ **Events** para integração com outras sistemas

## 🏗️ Scripts Inclusos

### 1. **CoinAnimator.cs** - Animador de Moedas
Anima moedas voando de um ponto para outro.

**Configuração no Inspector:**
- `coinPrefab` - Prefab da moeda (Image com CanvasGroup)
- `animationDuration` - Duração da animação (padrão: 1s)
- `movementCurve` - Curva de movimento (EaseInOut padrão)
- `randomSpread` - Espalhamento aleatório (padrão: 0.5)
- `randomRotation` - Rotação aleatória das moedas
- `debugMode` - Ativa logs detalhados

**Métodos Públicos:**
```csharp
void AnimateCoinsFlying(
    Vector3 startWorldPos,      // Posição inicial (mundo)
    Vector3 endWorldPos,        // Posição final (mundo)
    int coinCount = 5,          // Quantidade de moedas
    Action onComplete = null    // Callback ao terminar
)
// Anima múltiplas moedas voando
```

### 2. **NumberFormatter.cs** - Formatador de Números
Classe estática com métodos de formatação.

**Métodos Públicos (Estáticos):**
```csharp
string FormatCurrency(int amount, int decimalPlaces = 1)
// 1500 → "1.5K", 1000000 → "1.0M"

string FormatCurrency(long amount, int decimalPlaces = 1)
// Mesmo que acima para long

string FormatWithSeparator(int amount)
// 1500000 → "1,500,000"

string FormatCompact(int amount, bool useSymbol = true)
// 1500 → "1500" ou "1.5K"

string FormatPercent(float percent, int decimalPlaces = 1)
// 75.5f → "75.5%"

string FormatTime(float seconds)
// 125f → "2m", 3700f → "1h"
```

### 3. **EconomyUIController.cs** - Controlador Principal
Gerencia saldo, animações e display de moedas.

**Configuração no Inspector:**
- `balanceText` - Text para mostrar número exato
- `balanceFormattedText` - Text para mostrar número formatado
- `coinIcon` - Image do ícone de moeda
- `coinAnimator` - Referência ao CoinAnimator
- `updateAnimationDuration` - Duração da animação de atualização (0.5s)
- `useCompactFormat` - Usar formato compacto (1.5K) ou completo

**Eventos Públicos:**
```csharp
event OnBalanceChanged(int newBalance, int previousBalance)
// Disparado quando saldo é atualizado
```

**Métodos Públicos:**
```csharp
void SetBalance(int newBalance, bool animate = false)
// Define saldo exato

void AddBalance(int amount, Vector3 sourceWorldPos = default)
// Adiciona moedas com animação

void RemoveBalance(int amount, bool animate = false)
// Remove moedas

int GetCurrentBalance()
// Retorna saldo atual

void SetBalanceTextFormat(bool useCompact)
// Muda formato de exibição
```

### 4. **GiftCardRedeemer.cs** - Sistema de Resgate
Gerencia botão de gift card com validação de saldo.

**Configuração no Inspector:**
- `economyUI` - Referência ao EconomyUIController
- `redeemButton` - Botão de resgate
- `redeemButtonImage` - Image do botão
- `redeemButtonText` - Text do botão
- `redeemStatusText` - Text de status/feedback
- `minimumBalanceRequired` - Saldo mínimo (padrão: 5000)
- `giftCardValue` - Valor do gift card
- `enabledButtonColor` - Cor quando habilitado (branco)
- `disabledButtonColor` - Cor quando desabilitado (cinza)
- `statusDisplayDuration` - Tempo mostrando status (3s)

**Eventos Públicos:**
```csharp
event OnGiftCardRedeemed(int giftCardValue, int newBalance)
// Disparado quando gift card é resgatado

event OnRedeemFailed(string reason)
// Disparado quando falha o resgate
```

**Métodos Públicos:**
```csharp
void SetMinimumBalance(int newMinimum)
// Atualiza saldo mínimo (ex: do servidor)

int GetMinimumBalance()
// Retorna saldo mínimo

bool CanRedeem()
// Verifica se pode resgatar agora
```

### 5. **EconomyUIExample.cs** - Exemplo
Demonstra uso dos componentes.

## 🛠️ Configuração no Editor Unity

### Passo 1: Preparar Prefab de Moeda

1. Crie um GameObject chamado "Coin"
2. Adicione um **Image** com ícone de moeda
3. Adicione **CanvasGroup** (para opacidade)
4. Salve como prefab em Assets/Prefabs/Coin

**Configuração da Image:**
- Source Image: ícone de moeda
- Tamanho: 30x30 pixels

### Passo 2: Preparar Canvas de Economia

Crie uma hierarquia assim:

```
Canvas
├── EconomyPanel
│   ├── CoinIcon (Image)
│   ├── BalanceText (Text) - "1000"
│   └── BalanceFormattedText (Text) - "1.0K"
├── GiftCardPanel
│   ├── RedeemButton (Button)
│   │   └── Text - "Resgatar (5.0K)"
│   └── StatusText (Text)
```

### Passo 3: Adicionar Componentes

1. Selecione o Canvas
2. Adicione **CoinAnimator**:
   - Coin Prefab: Atribua o prefab de moeda
   - Animation Duration: 1.0

3. Selecione **EconomyPanel**
4. Adicione **EconomyUIController**:
   - Balance Text: Atribua o BalanceText
   - Balance Formatted Text: Atribua o BalanceFormattedText
   - Coin Icon: Atribua a CoinIcon
   - Coin Animator: Atribua o CoinAnimator
   - Use Compact Format: true

5. Selecione **GiftCardPanel**
6. Adicione **GiftCardRedeemer**:
   - Economy UI: Atribua o EconomyUIController
   - Redeem Button: Atribua o RedeemButton
   - Redeem Button Image: Atribua a Image do botão
   - Redeem Button Text: Atribua o Text do botão
   - Redeem Status Text: Atribua o StatusText
   - Minimum Balance Required: 5000
   - Gift Card Value: 0 (ou valor específico)
   - Enabled Button Color: white
   - Disabled Button Color: gray

### Passo 4: Configurar Botão

1. Selecione **RedeemButton**
2. Configure:
   - Interactable: false (inicialmente)
   - Colors > Disabled: gray (cinzento)

## 🎮 Fluxo de Uso

### Adicionando Moedas (ex: anúncio)

```csharp
EconomyUIController economyUI = GetComponent<EconomyUIController>();

// Simular moedas voando de anúncio até contador
Vector3 adWorldPos = adButton.transform.position;  // Posição do botão de anúncio
economyUI.AddBalance(100, adWorldPos);  // 100 moedas com animação
```

**O que acontece:**
```
1. 5-10 moedas animam de adWorldPos até balanceText
2. Cada moeda leva 0.9-1s com fade out
3. Saldo anima de 1000 → 1100
4. Text mostra "1.1K"
5. OnBalanceChanged event é disparado
6. GiftCardRedeemer verifica se pode habilitar botão
```

### Resgatando Gift Card

```
Usuário tem 4500 moedas
    ↓
RedeemButton está DESABILITADO (cinza)
    ↓
Usuário assiste 2 anúncios (+100 moedas)
    ↓
Saldo = 4600 < 5000 (mínimo)
    ↓
RedeemButton continua DESABILITADO
    ↓
Usuário assiste 1 anúncio (+100 moedas)
    ↓
Saldo = 4700 < 5000 (mínimo)
    ↓
RedeemButton continua DESABILITADO
    ↓
Saldo atinge 5000
    ↓
RedeemButton fica HABILITADO (branco)
    ↓
Usuário clica RedeemButton
    ↓
Processamento... (1s)
    ↓
Saldo = 4700 (5000 debitado)
    ↓
Servidor valida resgate
    ↓
Gift Card gerado
    ↓
"Gift Card resgatado!" (verde por 3s)
    ↓
RedeemButton volta DESABILITADO (se saldo < mínimo)
```

## 💡 Exemplos de Código

### Exemplo 1: Adicionando moedas com animação
```csharp
economyUI.AddBalance(250, sourcePosition);
// Anima 25 moedas de sourcePosition para contador
// Saldo aumenta de forma suave
```

### Exemplo 2: Formatando números
```csharp
NumberFormatter.FormatCurrency(1500);      // "1.5K"
NumberFormatter.FormatCurrency(1000000);   // "1.0M"
NumberFormatter.FormatCurrency(1500, 2);   // "1.50K"
NumberFormatter.FormatWithSeparator(1500); // "1,500"
NumberFormatter.FormatCompact(1500);       // "1.5K"
NumberFormatter.FormatPercent(75.5f);      // "75.5%"
NumberFormatter.FormatTime(125f);          // "2m"
```

### Exemplo 3: Integração com GameEconomy
```csharp
public class AdRewardHandler : MonoBehaviour
{
    [SerializeField] private EconomyUIController economyUI;
    [SerializeField] private GameEconomy gameEconomy;

    public void OnAdCompleted(int coinsEarned)
    {
        // Animar moedas voando do anúncio
        Vector3 adPos = adButton.transform.position;
        economyUI.AddBalance(coinsEarned, adPos);

        // Enviar validação ao servidor
        gameEconomy.WatchRewardedAd("video", (success) =>
        {
            if (!success)
            {
                // Reverter se falhar no servidor
                economyUI.RemoveBalance(coinsEarned);
            }
        });
    }
}
```

### Exemplo 4: Atualizar saldo mínimo do servidor
```csharp
public class ServerGiftCardConfig : MonoBehaviour
{
    [SerializeField] private GiftCardRedeemer redeemer;

    public void UpdateMinimumBalance(int newMinimum)
    {
        redeemer.SetMinimumBalance(newMinimum);
        Debug.Log($"Saldo mínimo atualizado: {newMinimum}");
    }
}
```

## 🔄 Integração com Outros Sistemas

### Com GameEconomy (Anúncios)
```csharp
economyUI.AddBalance(50, adPosition);  // Animar moedas
gameEconomy.CheckCreatorBalance((balance) =>
{
    economyUI.SetBalance(balance.balance);  // Sincronizar com servidor
});
```

### Com SnapScrollManager (Seleção de Jogo)
```csharp
snapScroll.GameSelected += (index, gameId) =>
{
    // Mostrar economia relativa ao jogo
    economyUI.SetBalance(playerGameStats[gameId].balance);
};
```

## 🎨 Customizações

### Mudar cor do botão dinamicamente
```csharp
redeemer.enabledButtonColor = Color.green;
redeemer.disabledButtonColor = Color.red;
```

### Usar formato diferente
```csharp
economyUI.SetBalanceTextFormat(false);  // "1,500,000" em vez de "1.5M"
```

### Personalizar animação de moedas
```csharp
coinAnimator.animationDuration = 2f;  // Mais lento
coinAnimator.randomSpread = 2f;       // Mais espalhado
```

## 📊 Padrões de Números

| Entrada | Formato | Output |
|---------|---------|--------|
| 500 | FormatCurrency | "500" |
| 1500 | FormatCurrency | "1.5K" |
| 1500 | FormatCurrency(_, 2) | "1.50K" |
| 1500000 | FormatCurrency | "1.5M" |
| 1500000000 | FormatCurrency | "1.5B" |
| 1500 | FormatWithSeparator | "1,500" |
| 1500 | FormatCompact(_, false) | "1.5" |

## ⚠️ Importante

- Certifique-se de que o **Coin Prefab** tem **CanvasGroup**
- O **animationDuration** deve ser > 0
- **minimumBalanceRequired** deve ser > 0
- Confirme que a **Canvas** está no Canvas do jogo

## 🚀 Próximos Passos

- [ ] Implementar integração real com servidor
- [ ] Adicionar som de moedas voando
- [ ] Efeito de partículas de brilho
- [ ] Histórico de transações
- [ ] Múltiplas moedas (ouro, prata, etc)
- [ ] Animação de "insuficiente" quando tenta resgatar sem saldo
