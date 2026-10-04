using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderSystem.Common.Models;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;

namespace OrderSystem.Processor;

public class OrderProcessorFunction
{
    private readonly ILogger<OrderProcessorFunction> _logger;
    private readonly Container _cosmosContainer;

    public OrderProcessorFunction(ILogger<OrderProcessorFunction> logger)
    {
        _logger = logger;

        var cosmosConn = Environment.GetEnvironmentVariable("Cosmos__ConnectionString");
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

    [Function("ProcessOrder")]
    public async Task Run(
        [ServiceBusTrigger("orders-queue", Connection = "ServiceBus__ConnectionString")]
        ServiceBusReceivedMessage message)
    {
        _logger.LogInformation("Received order message from Service Bus.");

        var order = message.Body.ToObjectFromJson<Order>()
            ?? throw new InvalidOperationException("The Service Bus message did not contain a valid order.");

        // Move the order through the background-processing states.
        order.Status = OrderStatus.Processing;
        await _cosmosContainer.UpsertItemAsync(order, new PartitionKey(order.Id));

        _logger.LogInformation("Order {OrderId} is being processed.", order.Id);

        // Simulate work such as validation or calculation.
        await Task.Delay(500);

        order.Status = OrderStatus.Completed;
        await _cosmosContainer.UpsertItemAsync(order, new PartitionKey(order.Id));

        _logger.LogInformation("Order {OrderId} completed. Customer: {Customer}; total: {Total:C}.",
            order.Id, order.CustomerName, order.Total);
    }
}
