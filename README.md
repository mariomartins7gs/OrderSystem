# Order System — Azure Serverless Demo

Sistema di gestione ordini serverless su Azure.

## Architettura

```
Client ──▶ API (App Service) ──▶ Service Bus ──▶ Function ──▶ Cosmos DB
               │                                               ▲
               └────────────── Event Grid ──────────────────────┘
```

## Servizi Azure

| Servizio | Ruolo |
|----------|-------|
| **App Service** | API REST (ASP.NET Core) |
| **Service Bus** | Coda messaggi |
| **Event Grid** | Notifica eventi |
| **Cosmos DB** | Database NoSQL |
| **Azure Functions** | Elaborazione in background |

## Progetti

| Progetto | Tipo | Descrizione |
|----------|------|-------------|
| `OrderSystem.Api` | Web API | CRUD ordini + invio messaggi |
| `OrderSystem.Processor` | Azure Function | Processa ordini in background |
| `OrderSystem.Common` | Class Library | Modelli condivisi |

## Come eseguire

1. Apri `OrderSystem.sln` in Visual Studio
2. Imposta le connection string in `appsettings.json` e `local.settings.json`
3. Avvia **Azurite** (emulatore storage locale)
4. Avvia il progetto

## API Endpoints

| Metodo | Endpoint | Descrizione |
|--------|----------|-------------|
| `POST` | `/api/orders` | Crea nuovo ordine |
| `GET` | `/api/orders` | Elenco ordini |
| `GET` | `/api/orders/{id}` | Dettaglio ordine |
