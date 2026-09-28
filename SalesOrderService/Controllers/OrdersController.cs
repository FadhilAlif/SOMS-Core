using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesOrderService.Models.DTOs;
using SalesOrderService.Services;

namespace SalesOrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/orders
    /// Mengambil daftar order dengan filter opsional (keyword dan orderDate)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders([FromQuery] string? keyword, [FromQuery] DateTime? orderDate)
    {
        try
        {
            var orders = await _orderService.GetOrdersAsync(keyword, orderDate);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching orders list.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Terjadi kesalahan internal saat mengambil daftar order.",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// GET /api/orders/export
    /// Ekspor daftar order yang sedang aktif difilter ke file Excel (.xlsx)
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportOrders([FromQuery] string? keyword, [FromQuery] DateTime? orderDate)
    {
        try
        {
            var fileBytes = await _orderService.ExportOrdersToExcelAsync(keyword, orderDate);
            var fileName = $"SalesOrder_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while exporting orders to Excel.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Terjadi kesalahan saat mengekspor data ke Excel.",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// GET /api/orders/{id}
    /// Mengambil detail satu order beserta daftar line items dan kalkulasi total
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(int id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Order tidak ditemukan",
                    errors = new[] { $"Order dengan ID {id} tidak ditemukan di sistem." }
                });
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching order ID {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Terjadi kesalahan internal saat mengambil detail order.",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// POST /api/orders
    /// Membuat Sales Order baru beserta line items
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiActionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var result = await _orderService.CreateOrderAsync(request);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
            }

            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                salesSoId = result.SalesSoId,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating order.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Gagal membuat order baru karena kesalahan server.",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// PUT /api/orders/{id}
    /// Memperbarui Sales Order dan mengganti seluruh line items lama
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
    {
        try
        {
            var result = await _orderService.UpdateOrderAsync(id, request);
            if (!result.Success)
            {
                if (result.Message.Contains("tidak ditemukan", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new
                    {
                        success = false,
                        message = result.Message,
                        errors = result.Errors.Count > 0 ? result.Errors : new List<string> { result.Message }
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating order ID {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Gagal memperbarui order karena kesalahan server.",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// DELETE /api/orders/{id}
    /// Menghapus Sales Order beserta seluruh line items secara atomik (Dilindungi JWT Bearer Token)
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        try
        {
            var result = await _orderService.DeleteOrderAsync(id);
            if (!result.Success)
            {
                return NotFound(new
                {
                    success = false,
                    message = result.Message,
                    errors = new[] { $"Order dengan ID {id} tidak ditemukan." }
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting order ID {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Gagal menghapus order karena kesalahan server.",
                errors = new[] { ex.Message }
            });
        }
    }
}
