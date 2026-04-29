# Ad Security Validator - Sistema de Validação de Anúncios Seguro

## 📋 Visão Geral

O `AdSecurityValidator` é um sistema robusto de segurança que:
- ✅ **Criptografa** localmente a contagem de anúncios (AES-256)
- ✅ **Impede chamadas** de anúncios quando limite diário é atingido
- ✅ **Valida integridade** com hash HMAC-SHA256
- ✅**Reseta automaticamente** a contagem à meia-noite (UTC)
- ✅ **Previne fraudes** comparando hashes entre cliente e servidor

## 🏗️ Scripts Inclusos

### 1. **AdSecurityValidator.cs** - Núcleo de Segurança
Gerencia criptografia, contagem de anúncios e geração de hashes.

**Configuração no Inspector:**
- `dailyAdLimit` - Limite de anúncios por dia (padrão: 50)
- `encryptionKey` - **⚠️ IMPORTANTE**: Chave única de criptografia (mude do padrão!)
- `debugMode` - Ativa logs detalhados

**Métodos Públicos - Controle de Anúncios:**
```csharp
bool CanShowAd()                    // Verifica se pode exibir novo anúncio
bool TryRecordAdView()              // Registra visualização (retorna sucesso)
int GetCurrentDailyCount()          // Retorna contagem atual
int GetRemainingAds()               // Retorna anúncios restantes
float GetDailyProgress()            // Retorna progresso (0-1)
void SetDailyLimit(int newLimit)   // Atualiza limite (do servidor)
void ResetDailyCount()              // Reseta contagem manualmente
```

**Métodos Públicos - Validação de Hash:**
```csharp
AdValidationHash GenerateValidationHash(
    string userId,
    string gameId,
    string secretSalt
)
// Gera hash único combinando dados do usuário

bool ValidateHash(
    AdValidationHash hash,
    string secretSalt
)
// Valida hash (útil para testes)
```

### 2. **AdValidationHash.cs** - Estrutura de Hash
Representa um hash de validação serializado em JSON.

**Propriedades:**
```csharp
string userId;           // ID único do usuário
string gameId;           // ID do jogo
string timestamp;        // Timestamp UTC do hash
string validationHash;   // Hash HMAC-SHA256
int adCount;            // Contagem de anúncios na hora
int dailyLimit;         // Limite diário na hora
```

**Métodos:**
```csharp
bool IsExpired(int maxAgeSeconds = 300)  // Verifica se hash expirou
string ToString()                         // Converte para JSON
static AdValidationHash FromJson(string) // Desserializa JSON
```

### 3. **AdSecurityExample.cs** - Exemplo de Implementação
Demonstra uso prático do sistema.

## 🔐 Sistema de Criptografia

### Armazenamento Local (PlayerPrefs)

```
┌─────────────────────────────────────┐
│  Armazenamento Criptografado:       │
│  ─────────────────────────────────  │
│  Key: "AdSecurity.DailyCount"       │
│  Value: Base64(AES-256-CBC)         │
│  ─────────────────────────────────  │
│  Key: "AdSecurity.LastResetDate"    │
│  Value: "2026-04-29"                │
└─────────────────────────────────────┘
```

### Processo de Criptografia

```
Plaintext: "15"
    ↓
UTF-8 Encode: [49, 53]
    ↓
AES-256-CBC Encrypt (com IV aleatório)
    ↓
IV + Ciphertext (combinados)
    ↓
Base64 Encode
    ↓
Ciphertext: "aBcD1234XyZ..."
```

## 📡 Hash de Validação

### Como é Gerado

```
Dados combinados:
  userId:gameId:secretSalt:timestamp
  "user_123:game_jump:secret_key_xyz:2026-04-29T12:39:54"
       ↓
  HMAC-SHA256 (chave: encryptionKey)
       ↓
  Hash: "a7f3e2c1b8d4f6..."
```

### Envio ao Servidor

```json
{
  "userId": "user_123",
  "gameId": "game_jump",
  "timestamp": "2026-04-29T12:39:54Z",
  "validationHash": "a7f3e2c1b8d4f6...",
  "adCount": 15,
  "dailyLimit": 50
}
```

### Validação no Servidor

```
Backend recebe:
{userId, gameId, timestamp, validationHash, adCount, ...}
    ↓
Recompõe dados: "user_123:game_jump:secret_salt_xyz:timestamp"
    ↓
Calcula HMAC-SHA256 com secretSalt do servidor
    ↓
Compara com validationHash recebido
    ↓
✓ Se igual: Contador confiável
✗ Se diferente: Fraude detectada → Rejeita
```

## 🛠️ Configuração no Editor Unity

### Passo 1: Adicionar à Cena

1. Crie um GameObject vazio chamado "AdSecurityValidator"
2. Adicione o componente `AdSecurityValidator`

### Passo 2: Configurar Propriedades Críticas

⚠️ **IMPORTANTE - Segurança:**

```
Daily Ad Limit: 50 (ou valor desejado)
Encryption Key: "minha_chave_secreta_unica_12345"
Debug Mode: false (desativar em produção)
```

**Gerar chave segura:**
```csharp
// No console C#:
using System;
string key = Guid.NewGuid().ToString("N").Substring(0, 32);
Debug.Log(key); // Use este valor como Encryption Key
```

### Passo 3: Configurar Exemplo (Opcional)

Se usar `AdSecurityExample`:

1. Crie um Canvas com:
   - Text para mostrar contagem
   - Button para "Mostrar Anúncio"
   - Button para "Validar Hash"

2. Configure no Inspector:
   - `userId`: "user_seu_id"
   - `gameId`: "game_seu_nome"
   - `serverSecretSalt`: Mesmo salt usado no backend
   - Atribua as referências de UI

## 🔄 Fluxo Completo

```
1. Usuário clica "Mostrar Anúncio"
   ↓
2. CanShowAd() verifica:
   ├─ Limite atingido? (contagem >= limite)
   ├─ Data resetada? (compara lastResetDate com hoje)
   └─ Contagem descriptografada OK?
   ↓
3. Se ✓ pode exibir:
   ├─ TryRecordAdView() incrementa contador
   ├─ EncryptData() criptografa novo valor
   ├─ Salva em PlayerPrefs
   ↓
4. Gera hash de validação:
   ├─ Combina: userId + gameId + salt + timestamp
   ├─ Calcula HMAC-SHA256
   ├─ Empacota em AdValidationHash
   ↓
5. Envia ao servidor via API/tRPC:
   ├─ POST /api/ad/validate
   ├─ Payload: JSON do AdValidationHash
   ↓
6. Servidor valida:
   ├─ Recompõe hash com seu secretSalt
   ├─ Compara com hash recebido
   ├─ Verifica contagem local vs servidor
   ├─ Registra recompensa
   ↓
7. Se ✓ válido:
   └─ Recompensa creditada
   
8. Se ✗ inválido:
   └─ Rejeita (possível fraude)
```

## 💻 Exemplo de Backend (Node.js/Express)

```javascript
const crypto = require('crypto');

const SECRET_SALT = 'server_secret_salt_123';
const ENCRYPTION_KEY = 'minha_chave_secreta_unica_12345';

app.post('/api/ad/validate', async (req, res) => {
  const { userId, gameId, timestamp, validationHash, adCount } = req.body;

  // 1. Validar que o hash não expirou
  const hashTime = new Date(timestamp);
  const ageSeconds = (Date.now() - hashTime) / 1000;
  
  if (ageSeconds > 300) {  // 5 minutos
    return res.status(400).json({
      success: false,
      message: 'Hash expirou'
    });
  }

  // 2. Recompor e validar hash
  const combinedData = `${userId}:${gameId}:${SECRET_SALT}:${timestamp}`;
  const expectedHash = crypto
    .createHmac('sha256', ENCRYPTION_KEY)
    .update(combinedData)
    .digest('hex')
    .toUpperCase();

  if (validationHash !== expectedHash) {
    return res.status(400).json({
      success: false,
      message: 'Hash inválido - possível fraude'
    });
  }

  // 3. Verificar limite diário no servidor
  const userRecord = await db.getUser(userId);
  const todayCount = await db.getAdCountToday(userId);
  
  if (todayCount >= userRecord.dailyAdLimit) {
    return res.status(400).json({
      success: false,
      message: 'Limite diário atingido no servidor'
    });
  }

  // 4. Registrar visualização
  await db.recordAdView({
    userId,
    gameId,
    timestamp: new Date(),
    clientAdCount: adCount,
    hashValidated: true
  });

  // 5. Creditou recompensa
  const rewardAmount = 50;
  await db.addCoins(userId, rewardAmount);

  res.json({
    success: true,
    message: 'Anúncio validado e recompensa creditada',
    coinsAwarded: rewardAmount
  });
});
```

## 🎯 Boas Práticas de Segurança

✅ **FAÇA:**
- Mude `encryptionKey` para uma chave única por jogo
- Use HTTPS em produção
- Valide hash NO SERVIDOR (nunca confie apenas no cliente)
- Implemente rate limiting (máximo X requisições/hora)
- Logar todas as tentativas de fraude
- Resetar salt secreto periodicamente

❌ **NUNCA:**
- Deixe chave de criptografia padrão ("default_key_change_this")
- Exponha `serverSecretSalt` no código cliente
- Confie completamente em contagem do cliente
- Desabilite validação de hash
- Use mesma chave para múltiplos games

## 🔒 Proteção Contra Fraudes

| Vetor de Fraude | Proteção |
|---|---|
| Modificar PlayerPrefs | Criptografia AES-256 |
| Roubar hash | Hash usa salt único + timestamp |
| Replay attack | Timestamp + validação de idade (5 min) |
| Bypass de limite | Servidor valida com contagem própria |
| Hash forjado | HMAC-SHA256 com chave secreta |

## 🐛 Debugging

Ative `Debug Mode` para ver:
```
[AdSecurityValidator] Contagem carregada: 15
[AdSecurityValidator] Pode exibir anúncio? true (15/50)
[AdSecurityValidator] Anúncio registrado. Contagem: 16/50
[AdSecurityValidator] Hash gerado: a7f3e2c1b8d4...
[AdSecurityValidator] Validação de hash: ✓ Válido
```

## 📊 Estrutura de Dados

```
PlayerPrefs (Criptografado):
├─ AdSecurity.DailyCount = Base64(AES256(count))
└─ AdSecurity.LastResetDate = "2026-04-29"

Banco de Dados (Servidor):
├─ users
│  ├─ id
│  ├─ userId
│  ├─ dailyAdLimit
│  └─ createdAt
├─ ad_views
│  ├─ id
│  ├─ userId
│  ├─ gameId
│  ├─ timestamp
│  ├─ hashValidated
│  └─ serverAdCount
```

## 🚀 Próximos Passos

1. **Alterar Encryption Key** no Inspector
2. **Configurar Backend** para validar hashes
3. **Implementar Rate Limiting** no servidor
4. **Monitorar Fraudes** com logs
5. **Testar** em dispositivos reais

## ⚠️ Importante para Produção

Antes de lançar:
- [ ] Alterar `encryptionKey` para valor único
- [ ] Configurar `serverSecretSalt` no backend
- [ ] Desativar `debugMode`
- [ ] Implementar validação de hash no servidor
- [ ] Testar com limite diário reduzido
- [ ] Monitorar logs de fraude
