using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SalesOrderService.Models.DTOs;

namespace SalesOrderService.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public LoginResponse? Authenticate(LoginRequest request)
    {
        // Kredensial default untuk Technical Test: admin / Admin@123
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        bool isValid = (request.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) && request.Password == "Admin@123")
                    || (request.Username.Equals("soms_admin", StringComparison.OrdinalIgnoreCase) && request.Password == "Password123!");

        if (!isValid)
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return null;
        }

        var jwtKey = _configuration["Jwt:Key"] 
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "SOMS.SalesOrderService";
        var audience = _configuration["Jwt:Audience"] ?? "SOMS.Client";
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryInMinutes"], out var mins) ? mins : 120;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, request.Username),
                new Claim(ClaimTypes.Name, request.Username),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation("JWT token issued successfully for user: {Username}", request.Username);

        return new LoginResponse
        {
            Success = true,
            Token = tokenString,
            TokenType = "Bearer",
            ExpiresIn = expiryMinutes * 60,
            Message = "Autentikasi berhasil"
        };
    }
}
