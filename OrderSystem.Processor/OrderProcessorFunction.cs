using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderSystem.Common.Models;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Cosmos;

namespace OrderSystem.Processor;

public class OrderProcessorFunction
{
    private readonly ILogger<OrderProcessorFunction> _logger;
    private readonly Container _cosmosContainer;

    public OrderProcessorFunction(ILogger<OrderProcessorFunction> logger)
    {
        _logger = logger;

        var cosmosClient = new CosmosClient(
            Environment.GetEnvironmentVariable("Cosmos__ConnectionString"));
        _cosmosContainer = cosmosClient.GetContainer("OrderDb", "Orders");
    }

    [Function("ProcessOrder")]
    public async Task Run(
        [ServiceBusTrigger("orders-queue", Connection = "ServiceBus__ConnectionString")]
        ServiceBusReceivedMessage message)
    {
        _logger.LogInformation("📨 Ricevuto ordine da Service Bus");

        var order = message.Body.ToObjectFromJson<Order>();

        // Aggiorna status a "Processing"
        order.Status = OrderStatus.Processing;
        await _cosmosContainer.UpsertItemAsync(order, new PartitionKey(order.Id));

        _logger.LogInformation("✅ Ordine {OrderId} in elaborazione...", order.Id);

        // Simula elaborazione (es. validazione, calcolo)
        await Task.Delay(500);

        // Completa
        order.Status = OrderStatus.Completed;
        await _cosmosContainer.UpsertItemAsync(order, new PartitionKey(order.Id));

        _logger.LogInformation("🎉 Ordine {OrderId} completato! Cliente: {Customer}, Totale: {Total:C}",
            order.Id, order.CustomerName, order.Total);
    }
}
