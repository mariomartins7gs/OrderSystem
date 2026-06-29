# Order Processing System — Azure Serverless Demo

## Resource Group
```
rg-orderprocessing-001
```

## Naming Convention
Tutte le risorse seguono: `{tipo}-orderprocessing-{suffisso}`

| Risorsa Azure | Nome proposto |
|---------------|---------------|
| **Resource Group** | `rg-orderprocessing-001` |
| **Cosmos DB** | `cosmos-orderprocessing-001` |
| **Service Bus** | `sb-orderprocessing-001` |
| **Event Grid Topic** | `evgt-orderprocessing-001` |
| **App Service (API)** | `app-orderprocessing-api` |
| **Function App** | `func-orderprocessing-001` |
| **Cosmos DB Database** | `OrderProcessingDb` |
| **Cosmos DB Container** | `Orders` |
| **Service Bus Queue** | `orders-queue` |

## Architettura

```
┌──────────┐     ┌──────────────────┐     ┌──────────────┐     ┌──────────────┐
│  Client   │────▶│  App Service     │────▶│  Service Bus  │────▶│  Cosmos DB   │
│ (HTTP/S)  │     │  Web API (.NET)  │     │  Queue        │     │  NoSQL       │
└──────────┘     └──────────────────┘     └──────────────┘     └──────────────┘
                         │                                              ▲
                         │                                              │
                         ▼                                              │
                  ┌──────────────┐         ┌──────────────┐             │
                  │  Event Grid  │────────▶│  Func (Proc.) │─────────────┘
                  │  Topic       │         │  Queue Trigger│  salva/stato
                  └──────────────┘         └──────────────┘
```

## Flusso di elaborazione

1. **POST** `/api/orders` → API riceve richiesta
2. **Salva** su Cosmos DB (`OrderProcessingDb` / `Orders`)
3. **Invia** messaggio a Service Bus (`orders-queue`)
4. **Pubblica** evento su Event Grid
5. **Function** processa in background (aggiorna status)
6. **GET** `/api/orders` → elenco completo

## Servizi Azure coinvolti

| # | Servizio | Ruolo | Nome risorsa |
|---|----------|-------|-------------|
| 1️⃣ | **App Service** | API REST (ASP.NET Core) | `app-orderprocessing-api` |
| 2️⃣ | **Service Bus** | Coda messaggi | `sb-orderprocessing-001` |
| 3️⃣ | **Event Grid** | Notifica eventi | `evgt-orderprocessing-001` |
| 4️⃣ | **Cosmos DB** | Database NoSQL | `cosmos-orderprocessing-001` |
| 5️⃣ | **Azure Functions** | Elaborazione background | `func-orderprocessing-001` |

## Progetti (.NET 8)

| Progetto | Tipo | Descrizione |
|----------|------|-------------|
| `OrderSystem.Api` | Web API | CRUD ordini + Service Bus + Event Grid |
| `OrderSystem.Processor` | Azure Function | Queue trigger: processa ordini |
| `OrderSystem.Common` | Class Library | Modelli condivisi (Order, enums, DTOs) |

## Configurazione

1. Apri `OrderSystem.sln` in Visual Studio / VS Code
2. Aggiorna `appsettings.json` con le connection string reali
3. Aggiorna `local.settings.json` per la Function
4. Avvia **Azurite** (emulatore storage) per test locale
5. Esegui: `dotnet run --project OrderSystem.Api`

## API Endpoints

| Metodo | Endpoint | Auth | Descrizione |
|--------|----------|------|-------------|
| `POST` | `/api/orders` | — | Crea ordine → SB + EG + Cosmos |
| `GET` | `/api/orders` | — | Elenco ordini (desc) |
| `GET` | `/api/orders/{id}` | — | Dettaglio ordine |

## Deploy su Azure

```bash
# Publish API
dotnet publish OrderSystem.Api -c Release -o ./publish/api
# Publish Function
dotnet publish OrderSystem.Processor -c Release -o ./publish/processor
```

## Licenza

Progetto didattico — ITS ICT Academy
