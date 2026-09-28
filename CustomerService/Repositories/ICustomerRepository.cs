using CustomerService.Models;

namespace CustomerService.Repositories;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetAllCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(int customerId);
}
