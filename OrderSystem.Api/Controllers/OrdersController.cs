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

    public OrdersController(IConfiguration config)
    {
        // Service Bus
        var sbConn = config["ServiceBus:ConnectionString"] ?? throw new InvalidOperationException("ServiceBus:ConnectionString is missing.");
        _sbClient = new ServiceBusClient(sbConn);

        // Event Grid
        var egEndpoint = config["EventGrid:Endpoint"] ?? throw new InvalidOperationException("EventGrid:Endpoint is missing.");
        var egKey = config["EventGrid:Key"] ?? throw new InvalidOperationException("EventGrid:Key is missing.");
        _egClient = new EventGridPublisherClient(new Uri(egEndpoint), new Azure.AzureKeyCredential(egKey));

        // Cosmos DB (create the database and container when they do not exist).
        var cosmosConn = config["Cosmos:ConnectionString"] ?? throw new InvalidOperationException("Cosmos:ConnectionString is missing.");
        var cosmosOptions = new CosmosClientOptions()
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };
        var cosmosClient = new CosmosClient(cosmosConn, cosmosOptions);
        var database = cosmosClient.CreateDatabaseIfNotExistsAsync("OrderProcessingDb").Result;
        _cosmosContainer = database.Database.CreateContainerIfNotExistsAsync("Orders", "/id").Result;
    }

    // POST api/orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // 1. Create the order.
        var order = new Order
        {
            CustomerName = request.CustomerName,
            Product = request.Product,
            Quantity = request.Quantity,
            Price = request.Price,
            Status = OrderStatus.Created
        };

        // 2. Save it to Cosmos DB.
        await _cosmosContainer.CreateItemAsync(order, new PartitionKey(order.Id));

        // 3. Send a message to the Service Bus queue.
        await using var sender = _sbClient.CreateSender("orders-queue");
        var messageBody = BinaryData.FromObjectAsJson(order);
        await sender.SendMessageAsync(new ServiceBusMessage(messageBody));

        // 4. Publish an Event Grid notification for other subscribers.
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

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new
        {
            order.Id,
            order.Status,
            Message = "Order created successfully."
        });
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
            return NotFound(new { Message = "Order not found." });
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
