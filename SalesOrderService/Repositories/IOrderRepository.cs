using SalesOrderService.Models;
using SalesOrderService.Models.DTOs;

namespace SalesOrderService.Repositories;

public interface IOrderRepository
{
    Task<IEnumerable<OrderListResponse>> GetOrdersAsync(string? keyword, DateTime? orderDate);
    Task<SalesOrder?> GetOrderHeaderByIdAsync(int id);
    Task<IEnumerable<OrderLineItem>> GetOrderItemsByOrderIdAsync(int orderId);
    Task<bool> IsSoNumberExistsAsync(string soNo, int? excludeId = null);
    Task<bool> IsCustomerExistsAsync(int customerId);
    Task<int> CreateOrderAsync(SalesOrder order, IEnumerable<OrderLineItem> items);
    Task<bool> UpdateOrderAsync(int id, SalesOrder order, IEnumerable<OrderLineItem> items);
    Task<bool> DeleteOrderAsync(int id);
}
