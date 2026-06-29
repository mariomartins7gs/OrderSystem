# Order Processing System — Azure Serverless Demo

Sistema di gestione ordini completamente serverless su Azure.

```
┌──────────┐     ┌──────────────────┐     ┌──────────────┐     ┌──────────────┐
│  Client   │────▶│  App Service     │────▶│  Service Bus  │────▶│  Cosmos DB   │
│ (HTTP/S)  │     │  Web API (.NET)  │     │  Queue        │     │  NoSQL       │
└──────────┘     └──────────────────┘     └──────────────┘     └──────────────┘
                         │                                              ▲
                         ▼                                              │
                  ┌──────────────┐         ┌──────────────┐             │
                  │  Event Grid  │────────▶│  Func (Proc.) │─────────────┘
                  │  Topic       │         │  Queue Trigger│  aggiorna
                  └──────────────┘         └──────────────┘    status
```

## Resource Group
```
rg-orderprocessing-001
```

## Naming Convention
Tutte le risorse seguono: `{tipo}-orderprocessing-{suffisso}`

| Risorsa Azure | Nome | Tier |
|---------------|------|------|
| **Cosmos DB** | `cosmos-orderprocessing-001` | Serverless + Free Tier |
| **Service Bus** | `sb-orderprocessing-001` | Standard |
| **Event Grid Topic** | `evgt-orderprocessing-001` | Standard |
| **App Service (API)** | `app-orderprocessing-api` | Free F1 |
| **Function App** | `func-orderprocessing-001` | Consumption |
| **Cosmos Database** | `OrderProcessingDb` | auto-creato |
| **Cosmos Container** | `Orders` | auto-creato |
| **Service Bus Queue** | `orders-queue` | creata manualmente |

## Flusso di elaborazione

1. **POST** `/api/orders` → API riceve richiesta
2. **Salva** su Cosmos DB (`OrderProcessingDb` / `Orders`)
3. **Invia** messaggio a Service Bus (`orders-queue`)
4. **Pubblica** evento su Event Grid
5. **Function** processa in background (aggiorna status)
6. **GET** `/api/orders` → elenco completo

## Progetti (.NET 8)

| Progetto | Tipo | Descrizione |
|----------|------|-------------|
| `OrderSystem.Api` | Web API | CRUD ordini + Service Bus + Event Grid |
| `OrderSystem.Processor` | Azure Function (isolated) | Queue trigger: processa ordini |
| `OrderSystem.Common` | Class Library | Modelli condivisi (Order, enums, DTOs) |

## API Endpoints

| Metodo | Endpoint | Descrizione |
|--------|----------|-------------|
| `POST` | `/api/orders` | Crea ordine → salva Cosmos + SB + EG |
| `GET` | `/api/orders` | Elenco ordini (dal più recente) |
| `GET` | `/api/orders/{id}` | Dettaglio ordine per ID |

## Configurazione locale

```bash
# 1. Crea appsettings.Development.json (gitignorato)
cat > OrderSystem.Api/appsettings.Development.json << 'EOF'
{
  "ServiceBus": { "ConnectionString": "..." },
  "EventGrid": { "Endpoint": "...", "Key": "..." },
  "Cosmos": { "ConnectionString": "..." }
}
EOF

# 2. Esegui in Development mode
dotnet run --project OrderSystem.Api

# 3. Apri Swagger
open http://localhost:5000/swagger
```

## Lessons Learned — Per un'implementazione smooth

### ❌ Da evitare assolutamente

1. **Connection string in `appsettings.json`**
   - GitHub Secret Scanning blocca il push se vede chiavi/token
   - ✅ Soluzione: usare `appsettings.Development.json` (già in `.gitignore`) o Azure App Settings

2. **`C:\file\` paths su Windows**
   - Richiedono privilegi admin → `System.UnauthorizedAccessException`
   - ✅ Soluzione: `Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)`

3. **Cosmos DB `id` property**
   - Cosmos richiede `id` (lowercase) su ogni documento
   - ✅ Soluzione: configurare `CosmosClientOptions` con `PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase`

4. **Placeholder config + `git pull`**
   - Se fai pull, sovrascrive le tue connection string locali
   - ✅ Soluzione: file separati gitignorati o usare Azure App Settings

5. **Testare il codice prima di creare le risorse**
   - Perdi tempo a debuggare errori di connessione
   - ✅ Soluzione: crea le risorse Azure SUBITO, poi testa

### ✅ Best practices

1. **SDK Cosmos singleton** → usare Dependency Injection, non creare nel costruttore
2. **Auto-create DB/Container** → `CreateDatabaseIfNotExistsAsync` + `CreateContainerIfNotExistsAsync`
3. **Config via Azure App Settings** → meglio che file locali per deploy
4. **SSH key per GitHub** → più sicuro del token (push senza password)
5. **Swagger in dev** → `--environment Development` o rimuovi il check Environment.IsDevelopment

### 👣 Step-by-step per un progetto nuovo

| Step | Cosa fare | Tempo |
|------|-----------|-------|
| 1 | Creare Resource Group su Azure | 2 min |
| 2 | Creare Cosmos DB + Service Bus + Event Grid + App Service + Function | 15 min |
| 3 | Copiare tutte le connection string in un file locale `.gitignore` | 5 min |
| 4 | Scrivere il codice C# | 30-60 min |
| 5 | Testare in locale con `dotnet run --environment Development` | 5 min |
| 6 | Pushare su GitHub (senza secret) | 2 min |
| 7 | Deploy su Azure via VS Publish | 10 min |
| 8 | Test finale su Azure | 5 min |

## Deploy su Azure

### API (App Service)
```bash
# Da VS Code / Terminal
dotnet publish OrderSystem.Api -c Release -o ./publish/api
```

Poi su Azure Portal:
- `app-orderprocessing-api` → **Deployment Center** → collega GitHub
- Oppure **Deploy via ZIP** da VS → Publish

### Function App
```bash
dotnet publish OrderSystem.Processor -c Release -o ./publish/processor
```

### Config Environment Variables
Su Azure Portal → App Service / Function App → **Settings → Environment variables**:
```
ServiceBus__ConnectionString  =  <value>
EventGrid__Endpoint           =  <value>
EventGrid__Key                =  <value>
Cosmos__ConnectionString      =  <value>
```

## Test su Azure

Dopo il deploy:
```
https://app-orderprocessing-api.azurewebsites.net/swagger
```

## Licenza

Progetto didattico — ITS ICT Academy
