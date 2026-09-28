namespace SalesOrderService.Models.DTOs;

public class OrderListResponse
{
    public int SalesSoId { get; set; }
    public string SoNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal GrandTotal { get; set; }
}
