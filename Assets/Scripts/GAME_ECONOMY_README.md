# GameEconomy - Sistema de Economia do Jogo com API

## 📋 Visão Geral

O `GameEconomy` é um sistema robusto de economia que:
- Conecta ao servidor via JSON (POST requests)
- Gerencia "Moedas de Criador" (reward coins)
- **Previne autofarm** comparando ID do dispositivo com ID do criador
- Implementa retry automático com backoff
- Usa Object Pooling para requisições HTTP

## 🏗️ Scripts Inclusos

### 1. **GameEconomy.cs** - Núcleo do Sistema
Classe principal que gerencia todas as operações de economia.

**Configuração no Inspector:**
- `apiBaseUrl` - URL base da API (ex: `https://api.example.com`)
- `creatorId` - ID único do criador do jogo
- `debugMode` - Ativa logs detalhados

**Propriedades Públicas:**
```csharp
CoinsUpdated    // Event disparado quando moedas são atualizadas
RequestFailed   // Event disparado em erro de requisição
```

**Métodos Públicos:**
```csharp
WatchRewardedAd(string adType, Action<bool> onComplete)
// Envia ao servidor que o usuário assistiu um anúncio

CheckCreatorBalance(Action<CreatorBalanceResponse> onComplete)
// Verifica o saldo de moedas do criador

GetDeviceId()                    // Retorna ID único do dispositivo
IsCreatorDevice()                // Verifica se é o dispositivo do criador
SetCreatorId(string id)         // Configura ID do criador em runtime
```

### 2. **GameEconomyExample.cs** - Exemplo de Implementação
Script de exemplo que mostra como usar a API.

## 🔐 Sistema Anti-Farm

O servidor **DEVE** verificar:

```
1. deviceId ≠ creatorId
   └─ Se forem iguais: Rejeitar (é o criador tentando farmar)
   
2. Se deviceId já assistiu o mesmo anúncio hoje?
   └─ Limite de 5 anúncios por dispositivo/dia
   
3. Timestamp válido? (não muito antigo)
   └─ Máximo 5 minutos do horário do servidor
```

O cliente envia:
- `creatorId` - ID do criador do jogo
- `deviceId` - ID único do dispositivo (gerado 1x)
- `adType` - Tipo de anúncio (banner, interstitial, rewarded)
- `timestamp` - Hora UTC quando foi assistido

## 📡 Endpoints da API

### POST `/api/reward/watch-ad`

**Request:**
```json
{
  "creatorId": "creator_12345",
  "deviceId": "device_abcdef789",
  "adType": "rewarded",
  "timestamp": "2026-04-29T12:39:54.4880000Z"
}
```

**Response (Sucesso - 200 OK):**
```json
{
  "success": true,
  "message": "Anúncio registrado",
  "coinsEarned": 50,
  "newBalance": 250,
  "isCreatorDevice": false
}
```

**Response (Erro - 400 Bad Request):**
```json
{
  "success": false,
  "message": "Dispositivo do criador não pode ganhar moedas",
  "coinsEarned": 0,
  "newBalance": 0,
  "isCreatorDevice": true
}
```

### POST `/api/creator/balance`

**Request:**
```json
{
  "creatorId": "creator_12345",
  "deviceId": "device_abcdef789"
}
```

**Response:**
```json
{
  "success": true,
  "creatorId": "creator_12345",
  "balance": 250,
  "totalEarned": 1250,
  "adWatchCount": 25
}
```

## 🛠️ Configuração no Editor Unity

### Passo 1: Adicionar GameEconomy à Cena
1. Crie um GameObject vazio chamado "GameEconomy"
2. Adicione o componente `GameEconomy`

### Passo 2: Configurar Propriedades
1. **API Base URL**: `https://seu-servidor.com` (sem barra final)
2. **Creator ID**: ID único do criador (ex: `creator_abc123`)
3. **Debug Mode**: Ative para logs detalhados durante desenvolvimento

### Passo 3: Conectar UI (Opcional)
1. Use `GameEconomyExample` para referência
2. Crie botões para:
   - Assistir anúncio recompensado
   - Verificar saldo

### Passo 4: Obter Device ID do Criador
No console Unity, execute:
```csharp
Debug.Log("Seu Device ID: " + SystemInfo.deviceUniqueIdentifier);
```
Ou no script:
```csharp
GameEconomy economy = GetComponent<GameEconomy>();
Debug.Log("Device ID: " + economy.GetDeviceId());
```

## 💻 Exemplo de Implementação Backend (Node.js/Express)

```javascript
const CREATOR_IDS = ["creator_12345"];
const MAX_ADS_PER_DAY = 5;
const COINS_PER_AD = 50;

app.post("/api/reward/watch-ad", async (req, res) => {
  const { creatorId, deviceId, adType, timestamp } = req.body;

  // 1. Validações básicas
  if (!CREATOR_IDS.includes(creatorId)) {
    return res.status(400).json({
      success: false,
      message: "Creator ID inválido",
      isCreatorDevice: false
    });
  }

  // 2. ANTI-FARM: Verificar se é o dispositivo do criador
  const creator = await db.getCreator(creatorId);
  const isCreatorDevice = deviceId === creator.deviceId;

  if (isCreatorDevice) {
    return res.status(400).json({
      success: false,
      message: "Dispositivo do criador não pode ganhar moedas",
      coinsEarned: 0,
      newBalance: 0,
      isCreatorDevice: true
    });
  }

  // 3. Validar timestamp (máximo 5 minutos)
  const requestTime = new Date(timestamp);
  const now = new Date();
  if (Math.abs(now - requestTime) > 5 * 60 * 1000) {
    return res.status(400).json({
      success: false,
      message: "Timestamp inválido"
    });
  }

  // 4. Verificar limite diário
  const adCount = await db.getAdCountToday(creatorId, deviceId);
  if (adCount >= MAX_ADS_PER_DAY) {
    return res.status(400).json({
      success: false,
      message: `Limite diário atingido (${MAX_ADS_PER_DAY})`
    });
  }

  // 5. Registrar anúncio e atualizar moedas
  await db.recordAd({
    creatorId,
    deviceId,
    adType,
    timestamp: now,
    coinsAwarded: COINS_PER_AD
  });

  const newBalance = creator.balance + COINS_PER_AD;
  await db.updateCreatorBalance(creatorId, newBalance);

  res.json({
    success: true,
    message: "Anúncio registrado",
    coinsEarned: COINS_PER_AD,
    newBalance: newBalance,
    isCreatorDevice: false
  });
});

app.post("/api/creator/balance", async (req, res) => {
  const { creatorId } = req.body;

  const creator = await db.getCreator(creatorId);
  if (!creator) {
    return res.status(404).json({
      success: false,
      message: "Creator não encontrado"
    });
  }

  const adWatchCount = await db.getTotalAdCount(creatorId);

  res.json({
    success: true,
    creatorId: creatorId,
    balance: creator.balance,
    totalEarned: creator.totalEarned || 0,
    adWatchCount: adWatchCount
  });
});
```

## 🔄 Fluxo Completo

```
1. Usuário clica em "Assistir Anúncio"
   ↓
2. GameEconomy.WatchRewardedAd() enviado POST
   ├─ Payload inclui deviceId (único do dispositivo)
   ├─ Payload inclui creatorId (do desenvolvedor)
   └─ Payload inclui timestamp (UTC now)
   ↓
3. Servidor recebe e valida
   ├─ deviceId ≠ creatorId? ✓ (senão rejeita)
   ├─ Timestamp válido? ✓ (máximo 5 min)
   ├─ Limite diário? ✓ (máximo 5 anúncios/dia)
   └─ Banco de dados registra ad
   ↓
4. Saldo atualizado: balance + 50 moedas
   ↓
5. Servidor responde com novo saldo
   ↓
6. GameEconomy dispara event CoinsUpdated
   ↓
7. UI atualiza para mostrar novo saldo
```

## 🎯 Boas Práticas

✅ **Faça:**
- Sempre validar deviceId ≠ creatorId no servidor
- Limitar anúncios por dispositivo por dia
- Usar HTTPS em produção
- Logar todas as transações

❌ **Evite:**
- Confiar apenas em validação do cliente
- Aceitar timestamps muito antigos
- Permitir múltiplos pedidos simultâneos

## 🐛 Debugging

Ative `Debug Mode` no Inspector para ver:
```
[GameEconomy] Device ID: ...
[GameEconomy] Enviando requisição: {...}
[GameEconomy] Anúncio registrado! Moedas ganhas: 50
```

## 📊 Estrutura de Dados (Banco de Dados)

```
creators table:
├─ id (PK)
├─ creatorId (UNIQUE)
├─ deviceId (para anti-farm)
├─ balance
├─ totalEarned
└─ createdAt

ad_watches table:
├─ id (PK)
├─ creatorId (FK)
├─ deviceId
├─ adType
├─ coinsAwarded
├─ watchedAt (timestamp)
└─ createdAt
```
