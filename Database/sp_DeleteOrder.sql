-- =============================================
-- Stored Procedure: sp_DeleteOrder
-- Description: Hapus order beserta seluruh line item secara atomik dengan Transaksi
-- =============================================

USE SOMS_DB;
GO

IF OBJECT_ID('dbo.sp_DeleteOrder', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteOrder;
GO

CREATE PROCEDURE dbo.sp_DeleteOrder
    @SalesSoId INT,
    @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Hapus line items terlebih dahulu (FK constraint)
        DELETE FROM SALES_SO_LITEM
        WHERE SALES_SO_ID = @SalesSoId;

        -- 2. Hapus header sales order
        DELETE FROM SALES_SO
        WHERE SALES_SO_ID = @SalesSoId;

        SET @RowsAffected = @@ROWCOUNT;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO
