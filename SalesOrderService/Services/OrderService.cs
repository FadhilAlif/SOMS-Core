using ClosedXML.Excel;
using SalesOrderService.Models;
using SalesOrderService.Models.DTOs;
using SalesOrderService.Repositories;

namespace SalesOrderService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository orderRepository, ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<OrderListResponse>> GetOrdersAsync(string? keyword, DateTime? orderDate)
    {
        _logger.LogInformation("Fetching orders. Keyword: '{Keyword}', OrderDate: '{OrderDate}'", keyword, orderDate);
        return await _orderRepository.GetOrdersAsync(keyword, orderDate);
    }

    public async Task<OrderDetailResponse?> GetOrderByIdAsync(int id)
    {
        _logger.LogInformation("Fetching order details for ID {Id}", id);

        var header = await _orderRepository.GetOrderHeaderByIdAsync(id);
        if (header == null)
        {
            return null;
        }

        var items = await _orderRepository.GetOrderItemsByOrderIdAsync(id);

        var response = new OrderDetailResponse
        {
            SalesSoId = header.SalesSoId,
            SoNo = header.SoNo,
            OrderDate = header.OrderDate,
            CustomerId = header.CustomerId,
            CustomerName = header.CustomerName,
            Address = header.Address,
            Items = items.Select(i => new OrderItemDetailDto
            {
                SalesSoLitemId = i.SalesSoLitemId,
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                Price = i.Price,
                // BR-07: Total dihitung di server (Quantity * Price)
                Total = i.Quantity * i.Price
            }).ToList()
        };

        // BR-08: Grand Total = SUM(semua TOTAL) dihitung di server
        response.GrandTotal = response.Items.Sum(x => x.Total);

        return response;
    }

    public async Task<ApiActionResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        _logger.LogInformation("Processing create order for SO No: {SoNo}", request.SoNo);

        // Validasi Business Rules
        var errors = await ValidateOrderAsync(request.SoNo, request.OrderDate, request.CustomerId, request.Address, request.Items, excludeId: null);
        if (errors.Count > 0)
        {
            return ApiActionResponse.Fail("Validasi gagal", errors);
        }

        var orderHeader = new SalesOrder
        {
            SoNo = request.SoNo.Trim(),
            OrderDate = request.OrderDate,
            CustomerId = request.CustomerId,
            Address = request.Address?.Trim()
        };

        var lineItems = request.Items.Select(item => new OrderLineItem
        {
            ItemName = item.ItemName.Trim(),
            Quantity = item.Quantity,
            Price = item.Price
        }).ToList();

        var newId = await _orderRepository.CreateOrderAsync(orderHeader, lineItems);
        _logger.LogInformation("Order created successfully with ID: {NewId}", newId);

        return ApiActionResponse.Created(newId, "Order berhasil dibuat");
    }

    public async Task<ApiActionResponse> UpdateOrderAsync(int id, UpdateOrderRequest request)
    {
        _logger.LogInformation("Processing update order for ID: {Id}, SO No: {SoNo}", id, request.SoNo);

        var existing = await _orderRepository.GetOrderHeaderByIdAsync(id);
        if (existing == null)
        {
            return ApiActionResponse.Fail($"Order dengan ID {id} tidak ditemukan.");
        }

        // Validasi Business Rules
        var errors = await ValidateOrderAsync(request.SoNo, request.OrderDate, request.CustomerId, request.Address, request.Items, excludeId: id);
        if (errors.Count > 0)
        {
            return ApiActionResponse.Fail("Validasi gagal", errors);
        }

        var orderHeader = new SalesOrder
        {
            SalesSoId = id,
            SoNo = request.SoNo.Trim(),
            OrderDate = request.OrderDate,
            CustomerId = request.CustomerId,
            Address = request.Address?.Trim()
        };

        var lineItems = request.Items.Select(item => new OrderLineItem
        {
            SalesSoId = id,
            ItemName = item.ItemName.Trim(),
            Quantity = item.Quantity,
            Price = item.Price
        }).ToList();

        var updated = await _orderRepository.UpdateOrderAsync(id, orderHeader, lineItems);
        if (!updated)
        {
            return ApiActionResponse.Fail($"Order dengan ID {id} tidak ditemukan.");
        }

        return ApiActionResponse.Ok("Order berhasil diperbarui");
    }

    public async Task<ApiActionResponse> DeleteOrderAsync(int id)
    {
        _logger.LogInformation("Processing atomic delete for order ID: {Id}", id);

        var existing = await _orderRepository.GetOrderHeaderByIdAsync(id);
        if (existing == null)
        {
            return ApiActionResponse.Fail($"Order dengan ID {id} tidak ditemukan.");
        }

        // BR-09: Delete order atomik
        var deleted = await _orderRepository.DeleteOrderAsync(id);
        if (!deleted)
        {
            return ApiActionResponse.Fail($"Order dengan ID {id} tidak ditemukan.");
        }

        return ApiActionResponse.Ok("Order berhasil dihapus");
    }

    public async Task<byte[]> ExportOrdersToExcelAsync(string? keyword, DateTime? orderDate)
    {
        _logger.LogInformation("Generating Excel export for active filter. Keyword: '{Keyword}', OrderDate: '{OrderDate}'", keyword, orderDate);

        // BR-10: Hanya mengekspor data yang sedang difilter
        var orders = (await _orderRepository.GetOrdersAsync(keyword, orderDate)).ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sales Orders");

        // Header Styling
        worksheet.Cell(1, 1).Value = "No";
        worksheet.Cell(1, 2).Value = "SO Number";
        worksheet.Cell(1, 3).Value = "Order Date";
        worksheet.Cell(1, 4).Value = "Customer Name";
        worksheet.Cell(1, 5).Value = "Address";
        worksheet.Cell(1, 6).Value = "Grand Total";

        var headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A"); // Dark Blue
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = 2;
        for (int i = 0; i < orders.Count; i++)
        {
            var ord = orders[i];
            worksheet.Cell(row, 1).Value = i + 1;
            worksheet.Cell(row, 2).Value = ord.SoNo;
            worksheet.Cell(row, 3).Value = ord.OrderDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 4).Value = ord.CustomerName;
            worksheet.Cell(row, 5).Value = ord.Address ?? "-";
            worksheet.Cell(row, 6).Value = ord.GrandTotal;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";

            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<List<string>> ValidateOrderAsync(
        string soNo, 
        DateTime orderDate, 
        int customerId, 
        string? address,
        List<OrderItemRequest> items, 
        int? excludeId)
    {
        var errors = new List<string>();

        // BR-01: SO Number wajib diisi, max 20 karakter, dan tidak boleh duplikat
        if (string.IsNullOrWhiteSpace(soNo))
        {
            errors.Add("SO Number wajib diisi.");
        }
        else if (soNo.Trim().Length > 20)
        {
            errors.Add("SO Number tidak boleh melebihi 20 karakter.");
        }
        else
        {
            var isDuplicate = await _orderRepository.IsSoNumberExistsAsync(soNo, excludeId);
            if (isDuplicate)
            {
                errors.Add($"SO Number '{soNo.Trim()}' sudah digunakan, silakan gunakan nomor lain.");
            }
        }

        // BR-02: Order Date tidak boleh kosong/default dan tidak boleh di masa depan (> hari ini)
        if (orderDate == default || orderDate < new DateTime(1753, 1, 1))
        {
            errors.Add("Order Date wajib diisi dengan tanggal yang valid.");
        }
        else if (orderDate.Date > DateTime.Today)
        {
            errors.Add("Order Date tidak boleh di masa depan (melebihi tanggal hari ini).");
        }

        // Validasi Address: max 500 karakter sesuai kolom VARCHAR(500)
        if (!string.IsNullOrWhiteSpace(address) && address.Trim().Length > 500)
        {
            errors.Add("Address tidak boleh melebihi 500 karakter.");
        }

        // BR-06: Customer ID harus valid (ada di DB)
        if (customerId <= 0)
        {
            errors.Add("Customer wajib dipilih.");
        }
        else
        {
            var customerExists = await _orderRepository.IsCustomerExistsAsync(customerId);
            if (!customerExists)
            {
                errors.Add($"Customer dengan ID {customerId} tidak ditemukan di master data.");
            }
        }

        // BR-03: Minimal 1 line item per order
        if (items == null || items.Count == 0)
        {
            errors.Add("Minimal 1 line item per order wajib disertakan.");
        }
        else
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var itemIndex = i + 1;

                if (string.IsNullOrWhiteSpace(item.ItemName))
                {
                    errors.Add($"Baris item ke-{itemIndex}: Nama item wajib diisi.");
                }
                else if (item.ItemName.Trim().Length > 100)
                {
                    errors.Add($"Baris item ke-{itemIndex}: Nama item tidak boleh melebihi 100 karakter.");
                }

                // BR-04: Quantity harus integer > 0
                if (item.Quantity <= 0)
                {
                    errors.Add($"Baris item ke-{itemIndex} ({item.ItemName}): Quantity harus lebih besar dari 0.");
                }

                // BR-05: Price harus > 0
                if (item.Price <= 0)
                {
                    errors.Add($"Baris item ke-{itemIndex} ({item.ItemName}): Price harus lebih besar dari 0.");
                }
            }
        }

        return errors;
    }
}
