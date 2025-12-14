using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AuditTrailDiagnosticModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditTrailDiagnosticModel> _logger;

        public AuditTrailDiagnosticModel(ApplicationDbContext context, ILogger<AuditTrailDiagnosticModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public bool TableExists { get; set; }
        public string TableExistsMessage { get; set; }
        public int RecordCount { get; set; } = -1;
        public string RecordCountMessage { get; set; }
        public bool CanWrite { get; set; }
        public string WriteTestMessage { get; set; }
        public int TestRecordId { get; set; }
        public string ErrorMessage { get; set; }
        public List<AuditTrail> RecentLogs { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Test 1: Check if table exists
                _logger.LogInformation("=== DIAGNOSTIC: Testing AuditTrails table existence ===");
                try
                {
                    var testQuery = await _context.AuditTrails.Take(1).ToListAsync();
                    TableExists = true;
                    TableExistsMessage = "Table exists and is accessible";
                    _logger.LogInformation(" Table exists");
                }
                catch (Exception ex)
                {
                    TableExists = false;
                    TableExistsMessage = $"Table does not exist or is not accessible: {ex.Message}";
                    ErrorMessage = $"Table Error: {ex.Message}\n{ex.StackTrace}";
                    _logger.LogError(ex, " Table does not exist");
                    return;
                }

                // Test 2: Get record count
                _logger.LogInformation("=== DIAGNOSTIC: Counting records ===");
                try
                {
                    RecordCount = await _context.AuditTrails.CountAsync();
                    RecordCountMessage = $"Successfully counted {RecordCount} existing records";
                    _logger.LogInformation(" Record count: {Count}", RecordCount);
                }
                catch (Exception ex)
                {
                    RecordCountMessage = $"Failed to count records: {ex.Message}";
                    _logger.LogError(ex, " Failed to count records");
                }

                // Test 3: Try to write a test record
                _logger.LogInformation("=== DIAGNOSTIC: Testing write capability ===");
                try
                {
                    var testLog = new AuditTrail
                    {
                        PerformedBy = "SYSTEM_DIAGNOSTIC",
                        UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                        Role = "Admin",
                        ActionType = "Diagnostic",
                        Action = "Diagnostic test write",
                        EntityName = "System",
                        EntityId = "0",
                        Description = "This is a test record created by the diagnostic tool",
                        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        Timestamp = DateTime.UtcNow
                    };

                    _context.AuditTrails.Add(testLog);
                    var savedCount = await _context.SaveChangesAsync();

                    if (savedCount > 0)
                    {
                        CanWrite = true;
                        TestRecordId = testLog.Id;
                        WriteTestMessage = $"Successfully wrote test record with ID {testLog.Id}";
                        _logger.LogInformation(" Test write successful. ID: {Id}", testLog.Id);
                    }
                    else
                    {
                        CanWrite = false;
                        WriteTestMessage = "SaveChangesAsync returned 0 rows affected";
                        _logger.LogWarning(" SaveChangesAsync returned 0");
                    }
                }
                catch (Exception ex)
                {
                    CanWrite = false;
                    WriteTestMessage = $"Write test failed: {ex.Message}";
                    ErrorMessage = $"Write Error: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}\n\nStack Trace:\n{ex.StackTrace}";
                    _logger.LogError(ex, " Write test failed");
                }

                // Test 4: Get recent logs
                _logger.LogInformation("=== DIAGNOSTIC: Fetching recent logs ===");
                try
                {
                    RecentLogs = await _context.AuditTrails
                        .OrderByDescending(a => a.Timestamp)
                        .Take(5)
                        .ToListAsync();
                    _logger.LogInformation(" Fetched {Count} recent logs", RecentLogs.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, " Failed to fetch recent logs");
                }

                _logger.LogInformation("=== DIAGNOSTIC COMPLETE ===");
                _logger.LogInformation("Table Exists: {TableExists}", TableExists);
                _logger.LogInformation("Record Count: {Count}", RecordCount);
                _logger.LogInformation("Can Write: {CanWrite}", CanWrite);
                _logger.LogInformation("Test Record ID: {Id}", TestRecordId);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"General diagnostic error: {ex.Message}\n{ex.StackTrace}";
                _logger.LogError(ex, " Diagnostic failed with general error");
            }
        }
    }
}
