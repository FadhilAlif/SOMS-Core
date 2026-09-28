using System.Data;
using CustomerService.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CustomerService.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly string _connectionString;

    public CustomerRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        const string sql = @"
            SELECT 
                COM_CUSTOMER_ID AS CustomerId,
                CUSTOMER_NAME   AS CustomerName
            FROM COM_CUSTOMER
            ORDER BY CUSTOMER_NAME ASC;";

        using var connection = CreateConnection();
        return await connection.QueryAsync<Customer>(sql);
    }

    public async Task<Customer?> GetCustomerByIdAsync(int customerId)
    {
        const string sql = @"
            SELECT 
                COM_CUSTOMER_ID AS CustomerId,
                CUSTOMER_NAME   AS CustomerName
            FROM COM_CUSTOMER
            WHERE COM_CUSTOMER_ID = @CustomerId;";

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { CustomerId = customerId });
    }
}
