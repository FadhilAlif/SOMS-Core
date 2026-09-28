using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using SalesOrderService.Models;
using SalesOrderService.Models.DTOs;

namespace SalesOrderService.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<IEnumerable<OrderListResponse>> GetOrdersAsync(string? keyword, DateTime? orderDate)
    {
        using var connection = CreateConnection();

        try
        {
            // Panggil Stored Procedure sp_GetOrders
            var parameters = new DynamicParameters();
            parameters.Add("@Keyword", string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim());
            parameters.Add("@OrderDate", orderDate.HasValue ? orderDate.Value.Date : (object?)null, DbType.Date);

            return await connection.QueryAsync<OrderListResponse>(
                "dbo.sp_GetOrders",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        catch
        {
            // Fallback inline SQL jika Stored Procedure belum dieksekusi di database
            const string sql = @"
                SELECT 
                    so.SALES_SO_ID AS SalesSoId,
                    so.SO_NO AS SoNo,
                    so.ORDER_DATE AS OrderDate,
                    c.COM_CUSTOMER_ID AS CustomerId,
                    c.CUSTOMER_NAME AS CustomerName,
                    so.ADDRESS AS Address,
                    ISNULL(SUM(li.QUANTITY * li.PRICE), 0) AS GrandTotal
                FROM SALES_SO so
                INNER JOIN COM_CUSTOMER c ON so.COM_CUSTOMER_ID = c.COM_CUSTOMER_ID
                LEFT JOIN SALES_SO_LITEM li ON so.SALES_SO_ID = li.SALES_SO_ID
                WHERE 
                    (@Keyword IS NULL OR LTRIM(RTRIM(@Keyword)) = '' OR 
                     so.SO_NO LIKE '%' + @Keyword + '%' OR 
                     c.CUSTOMER_NAME LIKE '%' + @Keyword + '%')
                    AND
                    (@OrderDate IS NULL OR CAST(so.ORDER_DATE AS DATE) = CAST(@OrderDate AS DATE))
                GROUP BY 
                    so.SALES_SO_ID,
                    so.SO_NO,
                    so.ORDER_DATE,
                    c.COM_CUSTOMER_ID,
                    c.CUSTOMER_NAME,
                    so.ADDRESS
                ORDER BY 
                    so.ORDER_DATE DESC, 
                    so.SALES_SO_ID DESC;";

            return await connection.QueryAsync<OrderListResponse>(sql, new
            {
                Keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(),
                OrderDate = orderDate?.Date
            });
        }
    }

    public async Task<SalesOrder?> GetOrderHeaderByIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                so.SALES_SO_ID AS SalesSoId,
                so.SO_NO AS SoNo,
                so.ORDER_DATE AS OrderDate,
                c.COM_CUSTOMER_ID AS CustomerId,
                c.CUSTOMER_NAME AS CustomerName,
                so.ADDRESS AS Address,
                ISNULL((SELECT SUM(li.QUANTITY * li.PRICE) 
                        FROM SALES_SO_LITEM li 
                        WHERE li.SALES_SO_ID = so.SALES_SO_ID), 0) AS GrandTotal
            FROM SALES_SO so
            INNER JOIN COM_CUSTOMER c ON so.COM_CUSTOMER_ID = c.COM_CUSTOMER_ID
            WHERE so.SALES_SO_ID = @Id;";

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SalesOrder>(sql, new { Id = id });
    }

    public async Task<IEnumerable<OrderLineItem>> GetOrderItemsByOrderIdAsync(int orderId)
    {
        const string sql = @"
            SELECT 
                SALES_SO_LITEM_ID AS SalesSoLitemId,
                SALES_SO_ID       AS SalesSoId,
                ITEM_NAME         AS ItemName,
                QUANTITY          AS Quantity,
                PRICE             AS Price
            FROM SALES_SO_LITEM
            WHERE SALES_SO_ID = @OrderId
            ORDER BY SALES_SO_LITEM_ID ASC;";

        using var connection = CreateConnection();
        return await connection.QueryAsync<OrderLineItem>(sql, new { OrderId = orderId });
    }

    public async Task<bool> IsSoNumberExistsAsync(string soNo, int? excludeId = null)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM SALES_SO 
            WHERE LOWER(SO_NO) = LOWER(@SoNo)
              AND (@ExcludeId IS NULL OR SALES_SO_ID != @ExcludeId);";

        using var connection = CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { SoNo = soNo.Trim(), ExcludeId = excludeId });
        return count > 0;
    }

    public async Task<bool> IsCustomerExistsAsync(int customerId)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM COM_CUSTOMER 
            WHERE COM_CUSTOMER_ID = @CustomerId;";

        using var connection = CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { CustomerId = customerId });
        return count > 0;
    }

    public async Task<int> CreateOrderAsync(SalesOrder order, IEnumerable<OrderLineItem> items)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            const string insertHeaderSql = @"
                INSERT INTO SALES_SO (SO_NO, ORDER_DATE, COM_CUSTOMER_ID, ADDRESS)
                VALUES (@SoNo, @OrderDate, @CustomerId, @Address);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var salesSoId = await connection.ExecuteScalarAsync<int>(insertHeaderSql, new
            {
                order.SoNo,
                order.OrderDate,
                order.CustomerId,
                order.Address
            }, transaction);

            const string insertItemSql = @"
                INSERT INTO SALES_SO_LITEM (SALES_SO_ID, ITEM_NAME, QUANTITY, PRICE)
                VALUES (@SalesSoId, @ItemName, @Quantity, @Price);";

            foreach (var item in items)
            {
                await connection.ExecuteAsync(insertItemSql, new
                {
                    SalesSoId = salesSoId,
                    item.ItemName,
                    item.Quantity,
                    item.Price
                }, transaction);
            }

            transaction.Commit();
            return salesSoId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateOrderAsync(int id, SalesOrder order, IEnumerable<OrderLineItem> items)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        try
        {
            const string updateHeaderSql = @"
                UPDATE SALES_SO 
                SET SO_NO = @SoNo,
                    ORDER_DATE = @OrderDate,
                    COM_CUSTOMER_ID = @CustomerId,
                    ADDRESS = @Address
                WHERE SALES_SO_ID = @Id;";

            var affectedRows = await connection.ExecuteAsync(updateHeaderSql, new
            {
                order.SoNo,
                order.OrderDate,
                order.CustomerId,
                order.Address,
                Id = id
            }, transaction);

            if (affectedRows == 0)
            {
                transaction.Rollback();
                return false;
            }

            // Hapus items lama
            const string deleteOldItemsSql = @"DELETE FROM SALES_SO_LITEM WHERE SALES_SO_ID = @Id;";
            await connection.ExecuteAsync(deleteOldItemsSql, new { Id = id }, transaction);

            // Masukkan items baru
            const string insertItemSql = @"
                INSERT INTO SALES_SO_LITEM (SALES_SO_ID, ITEM_NAME, QUANTITY, PRICE)
                VALUES (@SalesSoId, @ItemName, @Quantity, @Price);";

            foreach (var item in items)
            {
                await connection.ExecuteAsync(insertItemSql, new
                {
                    SalesSoId = id,
                    item.ItemName,
                    item.Quantity,
                    item.Price
                }, transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        try
        {
            // Coba panggil sp_DeleteOrder
            var parameters = new DynamicParameters();
            parameters.Add("@SalesSoId", id);
            parameters.Add("@RowsAffected", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("dbo.sp_DeleteOrder", parameters, commandType: CommandType.StoredProcedure);
            var rowsAffected = parameters.Get<int?>("@RowsAffected") ?? 0;
            return rowsAffected > 0;
        }
        catch
        {
            // Fallback manual database transaction
            using var transaction = connection.BeginTransaction();
            try
            {
                const string deleteItemsSql = @"DELETE FROM SALES_SO_LITEM WHERE SALES_SO_ID = @Id;";
                await connection.ExecuteAsync(deleteItemsSql, new { Id = id }, transaction);

                const string deleteHeaderSql = @"DELETE FROM SALES_SO WHERE SALES_SO_ID = @Id;";
                var affected = await connection.ExecuteAsync(deleteHeaderSql, new { Id = id }, transaction);

                transaction.Commit();
                return affected > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
