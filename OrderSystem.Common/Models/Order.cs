using System.Text.Json.Serialization;

namespace OrderSystem.Common.Models;

public enum OrderStatus
{
    Created,
    Processing,
    Completed,
    Failed
}

public class Order
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("product")]
    public string Product { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("total")]
    public decimal Total => Quantity * Price;

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; } = OrderStatus.Created;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
