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
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CustomerName { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total => Quantity * Price;
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
