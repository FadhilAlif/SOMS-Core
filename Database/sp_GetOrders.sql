-- =============================================
-- Stored Procedure: sp_GetOrders
-- Description: Mengambil daftar Sales Order dengan filter opsional (keyword & orderDate)
-- =============================================

USE SOMS_DB;
GO

IF OBJECT_ID('dbo.sp_GetOrders', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetOrders;
GO

CREATE PROCEDURE dbo.sp_GetOrders
    @Keyword   VARCHAR(100) = NULL,
    @OrderDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        so.SALES_SO_ID AS salesSoId,
        so.SO_NO AS soNo,
        so.ORDER_DATE AS orderDate,
        c.COM_CUSTOMER_ID AS customerId,
        c.CUSTOMER_NAME AS customerName,
        so.ADDRESS AS address,
        ISNULL(SUM(li.QUANTITY * li.PRICE), 0) AS grandTotal
    FROM SALES_SO so
    INNER JOIN COM_CUSTOMER c ON so.COM_CUSTOMER_ID = c.COM_CUSTOMER_ID
    LEFT JOIN SALES_SO_LITEM li ON so.SALES_SO_ID = li.SALES_SO_ID
    WHERE 
        (@Keyword IS NULL OR LTRIM(RTRIM(@Keyword)) = '' OR 
         so.SO_NO LIKE '%' + @Keyword + '%' OR 
         c.CUSTOMER_NAME LIKE '%' + @Keyword + '%')
        AND
        (@OrderDate IS NULL OR CAST(so.ORDER_DATE AS DATE) = @OrderDate)
    GROUP BY 
        so.SALES_SO_ID,
        so.SO_NO,
        so.ORDER_DATE,
        c.COM_CUSTOMER_ID,
        c.CUSTOMER_NAME,
        so.ADDRESS
    ORDER BY 
        so.ORDER_DATE DESC, 
        so.SALES_SO_ID DESC;
END;
GO
