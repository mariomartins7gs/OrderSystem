using OrderSystem.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace OrderSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    // Memoria condivisa (static) — simile a un database in-memory
    private static readonly List<Order> _orders = new();
    private static readonly object _lock = new();

    // POST api/orders
    [HttpPost]
    public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
    {
        var order = new Order
        {
            CustomerName = request.CustomerName,
            Product = request.Product,
            Quantity = request.Quantity,
            Price = request.Price,
            Status = OrderStatus.Created
        };

        lock (_lock)
        {
            _orders.Add(order);
        }

        Console.WriteLine($"📦 ORDINE CREATO: {order.Id} — {order.CustomerName} — {order.Product} x{order.Quantity} — €{order.Total}");

        return Ok(new { order.Id, order.Status, order.Total, Message = "Ordine creato con successo! ✅" });
    }

    // GET api/orders/{id}
    [HttpGet("{id}")]
    public IActionResult GetOrder(string id)
    {
        lock (_lock)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order is null)
                return NotFound(new { Message = "Ordine non trovato" });

            return Ok(order);
        }
    }

    // GET api/orders
    [HttpGet]
    public IActionResult GetAllOrders()
    {
        lock (_lock)
        {
            return Ok(_orders.OrderByDescending(o => o.CreatedAt).ToList());
        }
    }
}
