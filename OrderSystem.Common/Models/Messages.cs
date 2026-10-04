using System.ComponentModel.DataAnnotations;

namespace OrderSystem.Common.Models;

public sealed class CreateOrderRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Product { get; set; } = string.Empty;

    [Range(1, 100_000)]
    public int Quantity { get; set; }

    [Range(0.01, 1_000_000_000)]
    public decimal Price { get; set; }
}

public class OrderCreatedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
