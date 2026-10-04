using System.ComponentModel.DataAnnotations;
using OrderSystem.Common.Models;
using Xunit;

namespace OrderSystem.Common.Tests;

public class OrderContractTests
{
    [Fact]
    public void Order_total_is_derived_from_quantity_and_price()
    {
        var order = new Order { Quantity = 3, Price = 19.95m };

        Assert.Equal(59.85m, order.Total);
    }

    [Fact]
    public void Valid_create_request_has_no_validation_errors()
    {
        var request = new CreateOrderRequest
        {
            CustomerName = "Ada Lovelace",
            Product = "Analytical Engine",
            Quantity = 1,
            Price = 99.99m
        };

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_request_rejects_non_positive_quantities(int quantity)
    {
        var request = new CreateOrderRequest
        {
            CustomerName = "Ada Lovelace",
            Product = "Analytical Engine",
            Quantity = quantity,
            Price = 99.99m
        };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(CreateOrderRequest.Quantity)));
    }

    [Fact]
    public void Create_request_rejects_zero_price()
    {
        var request = new CreateOrderRequest
        {
            CustomerName = "Ada Lovelace",
            Product = "Analytical Engine",
            Quantity = 1,
            Price = 0m
        };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(CreateOrderRequest.Price)));
    }

    private static IReadOnlyCollection<ValidationResult> Validate(CreateOrderRequest request)
    {
        var context = new ValidationContext(request);
        var errors = new List<ValidationResult>();

        Validator.TryValidateObject(request, context, errors, validateAllProperties: true);

        return errors;
    }
}
