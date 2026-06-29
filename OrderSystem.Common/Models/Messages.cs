namespace OrderSystem.Common.Models;

public class CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderCreatedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
