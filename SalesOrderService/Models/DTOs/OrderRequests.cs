namespace SalesOrderService.Models.DTOs;

public class CreateOrderRequest
{
    public string SoNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public string? Address { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class UpdateOrderRequest
{
    public string SoNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public string? Address { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
