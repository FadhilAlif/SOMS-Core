using SalesOrderService.Models.DTOs;

namespace SalesOrderService.Services;

public interface IOrderService
{
    Task<IEnumerable<OrderListResponse>> GetOrdersAsync(string? keyword, DateTime? orderDate);
    Task<OrderDetailResponse?> GetOrderByIdAsync(int id);
    Task<ApiActionResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<ApiActionResponse> UpdateOrderAsync(int id, UpdateOrderRequest request);
    Task<ApiActionResponse> DeleteOrderAsync(int id);
    Task<byte[]> ExportOrdersToExcelAsync(string? keyword, DateTime? orderDate);
}