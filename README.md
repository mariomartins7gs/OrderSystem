# Order Processing System — Azure Serverless Demo

Full-stack serverless order management system built on Azure.  
**Student:** Mario Martins  
**Course:** ITS ICT Academy — Cloud & Azure  
**Status:** ✅ Completed — Live on Azure

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

## Architecture

5 Azure services, one pipeline:

| # | Service | Role | Name |
|---|---------|------|------|
| 1 | **App Service** | Web API (ASP.NET Core 8) | `app-orderprocessing-api` |
| 2 | **Service Bus** | Messaging queue | `sb-orderprocessing-001` |
| 3 | **Event Grid** | Event notification | `evgt-orderprocessing-001` |
| 4 | **Cosmos DB** | NoSQL database | `cosmos-orderprocessing-001` |
| 5 | **Azure Functions** | Background processing | `func-orderprocessing-001` |

## Data Flow

```
1. Client → POST /api/orders → API Controller
2. API saves order to Cosmos DB (OrderProcessingDb → Orders)
3. API sends message to Service Bus Queue (orders-queue)
4. API publishes event to Event Grid (OrderCreated)
5. Function (queue trigger) processes order in background
6. Client → GET /api/orders → reads from Cosmos DB
```

## GitHub

- **Repo:** [github.com/mariomartins7gs/OrderSystem](https://github.com/mariomartins7gs/OrderSystem)
- **CI/CD:** GitHub Actions → auto-deploy on push to `main`
- **Branches:** `main` (production)

## Project Structure

```
OrderSystem.sln
├── OrderSystem.Api/               ← ASP.NET Core Web API
│   ├── Controllers/OrdersController.cs
│   ├── Program.cs
│   └── appsettings.json
├── OrderSystem.Common/            ← Shared models
│   └── Models/Order.cs, Messages.cs
├── OrderSystem.Processor/         ← Azure Function
│   └── OrderProcessorFunction.cs
├── .github/workflows/             ← CI/CD pipeline
└── README.md
```

## Live Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `https://app-orderprocessing-api-[...].azurewebsites.net/api/orders` | Create order |
| `GET` | `https://app-orderprocessing-api-[...].azurewebsites.net/api/orders` | List orders |
| `GET` | `https://app-orderprocessing-api-[...].azurewebsites.net/api/orders/{id}` | Get order by ID |

## Testing

### Local
```bash
dotnet run --project OrderSystem.Api
# Swagger → http://localhost:5000/swagger
```

### Azure (live)
```bash
curl -X POST "https://app-orderprocessing-api-[...].azurewebsites.net/api/orders" \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Test","product":"Laptop","quantity":1,"price":999.99}'
```

## Lessons Learned

### ❌ Avoid
1. **Secrets in repo** — GitHub Secret Scanning blocks pushes. Use `appsettings.Development.json` (gitignored) or Azure App Settings
2. **Hardcoded paths** (`C:\file\`) — need admin. Use `Environment.SpecialFolder.MyDocuments`
3. **Cosmos id field** — Cosmos requires lowercase `id`. Configure `CosmosPropertyNamingPolicy.CamelCase`
4. **Swagger locked in Dev** — remove `if (env.IsDevelopment())` or set env variable
5. **Testing before resources** — create Azure resources first, then code

### ✅ Best Practices
1. **CI/CD from day 1** — GitHub Actions auto-deploys on push
2. **Auto-create Cosmos** — `CreateDatabaseIfNotExistsAsync` + `CreateContainerIfNotExistsAsync`
3. **Env vars over files** — Configure connection strings in Azure App Settings
4. **SSH key for GitHub** — more secure than tokens
5. **Separate config files** — `appsettings.json` (template) vs `appsettings.Development.json` (real secrets, gitignored)

## Deploy

Push to `main` → GitHub Actions builds + deploys automatically:
```
git add -A
git commit -m "Your message"
git push
```
