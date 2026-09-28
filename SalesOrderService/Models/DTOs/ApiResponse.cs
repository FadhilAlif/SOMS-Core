namespace SalesOrderService.Models.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string message = "Sukses") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors?.ToList() ?? new List<string>() };
}

public class ApiActionResponse
{
    public bool Success { get; set; }
    public int? SalesSoId { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ApiActionResponse Created(int salesSoId, string message = "Order berhasil dibuat") =>
        new() { Success = true, SalesSoId = salesSoId, Message = message };

    public static ApiActionResponse Ok(string message = "Operasi berhasil") =>
        new() { Success = true, Message = message };

    public static ApiActionResponse Fail(string message, IEnumerable<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors?.ToList() ?? new List<string>() };
}
