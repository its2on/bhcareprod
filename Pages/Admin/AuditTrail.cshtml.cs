using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin,System Administrator")]
    public class AuditTrailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditTrailModel> _logger;
        private readonly IAuditTrailService _auditTrail;

        public AuditTrailModel(ApplicationDbContext context, ILogger<AuditTrailModel> logger, IAuditTrailService auditTrail)
        {
            _context = context;
            _logger = logger;
            _auditTrail = auditTrail;
        }

        public List<AuditTrail> AuditLogs { get; set; } = new();
        public string SearchTerm { get; set; }
        public string RoleFilter { get; set; }
        public string ActionTypeFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 50;

        // Summary statistics
        public int TotalActions { get; set; }
        public int ActionsToday { get; set; }
        public int FailedActions { get; set; }
        public int ActiveUsers { get; set; }
        public string OutcomeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        public async Task OnGetAsync(string search, string role, string actionType, 
                                     DateTime? fromDate, DateTime? toDate, string outcome = null)
        {
            string requestId = Guid.NewGuid().ToString().Substring(0, 8);
            _logger.LogInformation($"[{requestId}] ========== AuditTrail OnGetAsync START ==========");
            _logger.LogInformation($"[{requestId}] Page property from URL: {Page}");
            _logger.LogInformation($"[{requestId}] Request URL: {HttpContext.Request.Path}{HttpContext.Request.QueryString}");
            
            SearchTerm = search;
            RoleFilter = role;
            ActionTypeFilter = actionType;
            FromDate = fromDate;
            ToDate = toDate;
            OutcomeFilter = outcome;
            CurrentPage = Page;
            
            _logger.LogInformation($"[{requestId}] CurrentPage assigned: {CurrentPage}, Skip={(CurrentPage - 1) * PageSize}, Take={PageSize}");

            // Calculate summary statistics
            DateTime today = DateTime.Today;
            TotalActions = await _context.AuditTrails.CountAsync();
            ActionsToday = await _context.AuditTrails.CountAsync(a => a.Timestamp >= today);
            FailedActions = await _context.AuditTrails.CountAsync(a => a.Outcome == "Failed");
            ActiveUsers = await _context.AuditTrails
                .Where(a => a.Timestamp >= today)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync();

            var query = _context.AuditTrails.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.PerformedBy.Contains(search) 
                                      || a.Action.Contains(search)
                                      || a.EntityName.Contains(search)
                                      || a.Description.Contains(search));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(a => a.Role == role);
            }

            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(a => a.ActionType == actionType);
            }

            if (!string.IsNullOrEmpty(outcome))
            {
                query = query.Where(a => a.Outcome == outcome);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.AddDays(1).AddSeconds(-1);
                query = query.Where(a => a.Timestamp <= endDate);
            }

            // Get total count
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            // Get paginated results
            AuditLogs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
            
            _logger.LogInformation($"[{requestId}] Query executed: Retrieved {AuditLogs.Count} records. TotalCount={TotalCount}, TotalPages={TotalPages}");
            if (AuditLogs.Any())
            {
                _logger.LogInformation($"[{requestId}] First record timestamp: {AuditLogs.First().Timestamp}, Last record timestamp: {AuditLogs.Last().Timestamp}");
                _logger.LogInformation($"[{requestId}] First record ID: {AuditLogs.First().Id}, Action: {AuditLogs.First().Action}");
            }
            
            _logger.LogInformation($"[{requestId}] FINAL VALUES BEFORE RENDER: CurrentPage={CurrentPage}, Page={Page}, TotalPages={TotalPages}");
            _logger.LogInformation($"[{requestId}] ========== AuditTrail OnGetAsync END ==========");
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            var auditLog = await _context.AuditTrails.FindAsync(id);
            if (auditLog == null)
            {
                return NotFound();
            }
            return new JsonResult(auditLog);
        }

        public async Task<IActionResult> OnGetExportCsvAsync(string search, string role, string actionType, 
                                                              DateTime? fromDate, DateTime? toDate, string outcome)
        {
            var query = _context.AuditTrails.AsQueryable();

            // Apply same filters
            if (!string.IsNullOrEmpty(search))
                query = query.Where(a => a.PerformedBy.Contains(search) || a.Action.Contains(search));
            if (!string.IsNullOrEmpty(role))
                query = query.Where(a => a.Role == role);
            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(a => a.ActionType == actionType);
            if (!string.IsNullOrEmpty(outcome))
                query = query.Where(a => a.Outcome == outcome);
            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value.AddDays(1).AddSeconds(-1));

            var logs = await query.OrderByDescending(a => a.Timestamp).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,User,Role,Action Type,Action,Entity,Description,IP Address,Outcome,Request Method,Request URL");

            foreach (var log in logs)
            {
                csv.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.PerformedBy}\",\"{log.Role}\",\"{log.ActionType}\",\"{log.Action}\",\"{log.EntityName}\",\"{log.Description}\",\"{log.IPAddress}\",\"{log.Outcome}\",\"{log.RequestMethod}\",\"{log.RequestUrl}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            
            // Log audit trail for export
            await _auditTrail.LogAsync(
                "Export",
                $"Exported audit trail to CSV ({logs.Count} records)",
                "AuditTrail",
                null,
                null,
                JsonConvert.SerializeObject(new {
                    RecordCount = logs.Count,
                    Filters = new {
                        Search = search,
                        Role = role,
                        ActionType = actionType,
                        FromDate = fromDate?.ToString("yyyy-MM-dd"),
                        ToDate = toDate?.ToString("yyyy-MM-dd"),
                        Outcome = outcome
                    }
                })
            );
            
            return File(bytes, "text/csv", $"AuditTrail_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}
