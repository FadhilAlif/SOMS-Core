using SalesOrderService.Models.DTOs;

namespace SalesOrderService.Services;

public interface IAuthService
{
    LoginResponse? Authenticate(LoginRequest request);
}
