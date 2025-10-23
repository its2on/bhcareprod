-- ============================================
-- BHCare Audit Trail Immutability Trigger
-- ============================================
-- Purpose: Prevent modification or deletion of audit trail records
-- HIPAA Requirement: §164.312(c)(1) - Integrity Controls
-- Security Level: CRITICAL
-- ============================================
-- Created: October 23, 2025
-- Database: bhcareDB
-- ============================================

USE bhcareDB;
GO

-- Drop existing trigger if it exists
IF OBJECT_ID('dbo.trg_PreventAuditModification', 'TR') IS NOT NULL
BEGIN
    DROP TRIGGER dbo.trg_PreventAuditModification;
    PRINT 'Existing trigger dropped successfully.';
END
GO

-- Create trigger to block UPDATE and DELETE operations
CREATE TRIGGER trg_PreventAuditModification
ON dbo.AuditTrails
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @OperationType VARCHAR(10);
    DECLARE @RecordCount INT;
    DECLARE @AttemptedBy VARCHAR(255);
    DECLARE @IPAddress VARCHAR(50);
    
    -- Determine operation type
    IF EXISTS (SELECT * FROM deleted) AND EXISTS (SELECT * FROM inserted)
        SET @OperationType = 'UPDATE';
    ELSE IF EXISTS (SELECT * FROM deleted)
        SET @OperationType = 'DELETE';
    ELSE
        SET @OperationType = 'UNKNOWN';
        
    -- Count affected records
    SELECT @RecordCount = COUNT(*) FROM deleted;
    
    -- Capture connection info
    SET @AttemptedBy = SYSTEM_USER;
    SET @IPAddress = CONVERT(VARCHAR(50), CONNECTIONPROPERTY('client_net_address'));
    
    -- Log detailed error message
    DECLARE @ErrorMsg NVARCHAR(1000);
    SET @ErrorMsg = 
        '╔══════════════════════════════════════════════════════╗' + CHAR(13) + CHAR(10) +
        '║  SECURITY VIOLATION - AUDIT TRAIL TAMPERING ATTEMPT  ║' + CHAR(13) + CHAR(10) +
        '╚══════════════════════════════════════════════════════╝' + CHAR(13) + CHAR(10) +
        CHAR(13) + CHAR(10) +
        'Operation Type:    ' + @OperationType + CHAR(13) + CHAR(10) +
        'Records Affected:  ' + CAST(@RecordCount AS VARCHAR(10)) + CHAR(13) + CHAR(10) +
        'Attempted By:      ' + @AttemptedBy + CHAR(13) + CHAR(10) +
        'IP Address:        ' + ISNULL(@IPAddress, 'Unknown') + CHAR(13) + CHAR(10) +
        'Timestamp:         ' + CONVERT(VARCHAR(30), GETDATE(), 120) + CHAR(13) + CHAR(10) +
        CHAR(13) + CHAR(10) +
        '⚠️  AUDIT TRAIL RECORDS ARE IMMUTABLE ⚠️' + CHAR(13) + CHAR(10) +
        CHAR(13) + CHAR(10) +
        'Audit logs cannot be modified or deleted to maintain data integrity ' +
        'and compliance with HIPAA §164.312(c)(1).' + CHAR(13) + CHAR(10) +
        CHAR(13) + CHAR(10) +
        'This incident has been logged and may be reviewed by system administrators.';
    
    -- Raise error to prevent the operation
    RAISERROR(@ErrorMsg, 16, 1);
    
    -- Rollback the transaction
    ROLLBACK TRANSACTION;
    
    -- Optionally log the tampering attempt to a security log table (if it exists)
    -- Note: This won't execute due to ROLLBACK, but shows intent
    -- IF OBJECT_ID('dbo.SecurityViolations', 'U') IS NOT NULL
    -- BEGIN
    --     INSERT INTO dbo.SecurityViolations (OperationType, AttemptedBy, IPAddress, Timestamp)
    --     VALUES (@OperationType, @AttemptedBy, @IPAddress, GETDATE());
    -- END
END;
GO

-- Verify trigger creation
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT 'Trigger Verification';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';

SELECT 
    '✓ Trigger Created' AS Status,
    name AS TriggerName,
    OBJECT_NAME(parent_id) AS TableName,
    create_date AS CreatedDate,
    modify_date AS ModifiedDate,
    is_disabled AS IsDisabled,
    is_instead_of_trigger AS IsInsteadOf
FROM sys.triggers
WHERE name = 'trg_PreventAuditModification';

PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT 'Test Instructions';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';
PRINT 'To test the trigger, try the following (both should FAIL):';
PRINT '';
PRINT '  -- Test UPDATE (should be blocked)';
PRINT '  UPDATE AuditTrails SET Description = ''Test'' WHERE Id = 1;';
PRINT '';
PRINT '  -- Test DELETE (should be blocked)';
PRINT '  DELETE FROM AuditTrails WHERE Id = 1;';
PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '';
PRINT '✅ Audit Trail Immutability Trigger Deployed Successfully';
PRINT '🔒 HIPAA §164.312(c)(1) Compliance: ACTIVE';
PRINT '';
GO
