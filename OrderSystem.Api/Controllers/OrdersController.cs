using OrderSystem.Common.Models;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Mvc;

namespace OrderSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ServiceBusClient _sbClient;
    private readonly EventGridPublisherClient _egClient;
    private readonly Container _cosmosContainer;

    private readonly IConfiguration _config;

    public OrdersController(IConfiguration config)
    {
        _config = config;

        // Service Bus
        var sbConn = config["ServiceBus:ConnectionString"] ?? throw new InvalidOperationException("ServiceBus:ConnectionString mancante");
        _sbClient = new ServiceBusClient(sbConn);

        // Event Grid
        var egEndpoint = config["EventGrid:Endpoint"] ?? throw new InvalidOperationException("EventGrid:Endpoint mancante");
        var egKey = config["EventGrid:Key"] ?? throw new InvalidOperationException("EventGrid:Key mancante");
        _egClient = new EventGridPublisherClient(new Uri(egEndpoint), new Azure.AzureKeyCredential(egKey));

        // Cosmos DB
        var cosmosConn = config["Cosmos:ConnectionString"] ?? throw new InvalidOperationException("Cosmos:ConnectionString mancante");
        var cosmosClient = new CosmosClient(cosmosConn);
        _cosmosContainer = cosmosClient.GetContainer("OrderProcessingDb", "Orders");
    }

    // POST api/orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // 1. Crea l'ordine
        var order = new Order
        {
            CustomerName = request.CustomerName,
            Product = request.Product,
            Quantity = request.Quantity,
            Price = request.Price,
            Status = OrderStatus.Created
        };

        // 2. Salva su Cosmos DB
        await _cosmosContainer.CreateItemAsync(order, new PartitionKey(order.Id));

        // 3. Invia messaggio a Service Bus Queue
        var sender = _sbClient.CreateSender("orders-queue");
        var messageBody = BinaryData.FromObjectAsJson(order);
        await sender.SendMessageAsync(new ServiceBusMessage(messageBody));

        // 4. Pubblica evento su Event Grid
        var eventData = new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerName = order.CustomerName,
            Total = order.Total
        };
        var egEvent = new EventGridEvent(
            "OrderSystem.Api",
            "OrderCreated",
            "1.0",
            BinaryData.FromObjectAsJson(eventData));
        await _egClient.SendEventAsync(egEvent);

        return Ok(new { order.Id, order.Status, Message = "Ordine creato con successo!" });
    }

    // GET api/orders/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(string id)
    {
        try
        {
            var response = await _cosmosContainer.ReadItemAsync<Order>(id, new PartitionKey(id));
            return Ok(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { Message = "Ordine non trovato" });
        }
    }

    // GET api/orders
    [HttpGet]
    public async Task<IActionResult> GetAllOrders()
    {
        var query = _cosmosContainer.GetItemQueryIterator<Order>(
            new QueryDefinition("SELECT * FROM c ORDER BY c.CreatedAt DESC"));

        var orders = new List<Order>();
        while (query.HasMoreResults)
        {
            var page = await query.ReadNextAsync();
            orders.AddRange(page);
        }

        return Ok(orders);
    }
}
