using CustomerService.Models;
using CustomerService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/customers
    /// Mengambil semua data master pelanggan untuk dropdown dan referensi order
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCustomers()
    {
        try
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching customers.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Terjadi kesalahan internal saat mengambil data pelanggan.",
                errors = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// GET /api/customers/{id}
    /// Mengambil data satu pelanggan berdasarkan ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Pelanggan dengan ID {id} tidak ditemukan.",
                    errors = new[] { $"Customer with ID {id} does not exist." }
                });
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching customer {CustomerId}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Terjadi kesalahan internal saat mengambil data pelanggan.",
                errors = new[] { ex.Message }
            });
        }
    }
}
