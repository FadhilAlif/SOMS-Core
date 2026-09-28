using Microsoft.AspNetCore.Mvc;
using SalesOrderService.Models.DTOs;
using SalesOrderService.Services;

namespace SalesOrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// POST /api/auth/token
    /// Mendapatkan JWT Bearer Token untuk otentikasi API
    /// Kredensial default: admin / Admin@123
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetToken([FromBody] LoginRequest request)
    {
        var response = _authService.Authenticate(request);
        if (response == null)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Username atau password salah.",
                errors = new[] { "Kredensial tidak valid. Gunakan admin / Admin@123 untuk pengujian." }
            });
        }

        return Ok(response);
    }

    /// <summary>
    /// POST /api/auth/login (Alias endpoint)
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        return GetToken(request);
    }
}
