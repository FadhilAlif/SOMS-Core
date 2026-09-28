using CustomerService.Models;
using CustomerService.Repositories;

namespace CustomerService.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ICustomerRepository customerRepository, ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        _logger.LogInformation("Retrieving all customers from repository.");
        return await _customerRepository.GetAllCustomersAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int customerId)
    {
        _logger.LogInformation("Retrieving customer with ID: {CustomerId}", customerId);
        return await _customerRepository.GetCustomerByIdAsync(customerId);
    }
}
