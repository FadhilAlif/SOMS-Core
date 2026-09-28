namespace SalesOrderService.Models;

public class SalesOrder
{
    public int SalesSoId { get; set; }
    public string SoNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal GrandTotal { get; set; }
}

public class OrderLineItem
{
    public int SalesSoLitemId { get; set; }
    public int SalesSoId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total => Quantity * Price;
}
