using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Text;
using System.Globalization;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class FormResponsesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FormResponsesModel> _logger;

        public FormResponsesModel(ApplicationDbContext context, ILogger<FormResponsesModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public FormTemplate FormTemplate { get; set; } = null!;
        public List<FormSubmission> FormSubmissions { get; set; } = new List<FormSubmission>();
        public int TotalResponses { get; set; }
        public int CompletionRate { get; set; } = 100;
        public DateTime? LastResponseDate { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            FormTemplate = await _context.FormTemplates
                .Include(f => f.FormFields)
                .ThenInclude(ff => ff.FormFieldOptions)
                .Include(f => f.FormSubmissions)
                .FirstOrDefaultAsync(f => f.FormTemplateId == id);

            if (FormTemplate == null)
            {
                return NotFound();
            }

            FormSubmissions = await _context.FormSubmissions
                .Where(fs => fs.FormTemplateId == id)
                .OrderByDescending(fs => fs.SubmittedAt)
                .ToListAsync();

            TotalResponses = FormSubmissions.Count;
            LastResponseDate = FormSubmissions.FirstOrDefault()?.SubmittedAt;

            return Page();
        }

        public Dictionary<string, string> ParseFormData(string jsonData)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData) ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public List<string> GetFieldResponses(int fieldId)
        {
            var field = FormTemplate.FormFields.FirstOrDefault(f => f.FormFieldId == fieldId);
            if (field == null) return new List<string>();

            var responses = new List<string>();

            foreach (var submission in FormSubmissions)
            {
                var data = ParseFormData(submission.FormData);
                if (data.ContainsKey(field.FieldName) && !string.IsNullOrEmpty(data[field.FieldName]))
                {
                    responses.Add(data[field.FieldName]);
                }
            }

            return responses;
        }

        public int GetResponseCount(int fieldId)
        {
            return GetFieldResponses(fieldId).Count;
        }

        public Dictionary<string, int>? GetChartData(int fieldId)
        {
            var field = FormTemplate.FormFields.FirstOrDefault(f => f.FormFieldId == fieldId);
            if (field == null) return null;

            var responses = GetFieldResponses(fieldId);
            if (!responses.Any()) return null;

            var chartData = new Dictionary<string, int>();

            // For checkbox fields, responses may contain multiple values separated by comma
            if (field.FieldType == "checkbox")
            {
                foreach (var response in responses)
                {
                    var values = response.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(v => v.Trim());
                    foreach (var value in values)
                    {
                        if (!chartData.ContainsKey(value))
                        {
                            chartData[value] = 0;
                        }
                        chartData[value]++;
                    }
                }
            }
            else
            {
                // For radio and select
                foreach (var response in responses)
                {
                    if (!chartData.ContainsKey(response))
                    {
                        chartData[response] = 0;
                    }
                    chartData[response]++;
                }
            }

            return chartData.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public async Task<IActionResult> OnPostDeleteResponseAsync(int responseId)
        {
            var submission = await _context.FormSubmissions.FindAsync(responseId);

            if (submission == null)
            {
                return NotFound();
            }

            try
            {
                _context.FormSubmissions.Remove(submission);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Response {responseId} deleted by {User.Identity?.Name}");

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting response {responseId}");
                return BadRequest($"Error deleting response: {ex.Message}");
            }
        }

        public async Task<IActionResult> OnGetExportCSVAsync(int id)
        {
            var form = await _context.FormTemplates
                .Include(f => f.FormFields)
                .FirstOrDefaultAsync(f => f.FormTemplateId == id);

            if (form == null)
            {
                return NotFound();
            }

            var submissions = await _context.FormSubmissions
                .Where(fs => fs.FormTemplateId == id)
                .OrderBy(fs => fs.SubmittedAt)
                .ToListAsync();

            var csv = new StringBuilder();

            // Header row
            var headers = new List<string> { "Response ID", "Submitted At", "Status" };
            headers.AddRange(form.FormFields.OrderBy(f => f.DisplayOrder).Select(f => f.FieldLabel));
            csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            // Data rows
            foreach (var submission in submissions)
            {
                var data = ParseFormData(submission.FormData);
                var row = new List<string>
                {
                    submission.FormSubmissionId.ToString(),
                    submission.SubmittedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    submission.Status
                };

                foreach (var field in form.FormFields.OrderBy(f => f.DisplayOrder))
                {
                    var value = data.ContainsKey(field.FieldName) ? data[field.FieldName] : "";
                    row.Add($"\"{value.Replace("\"", "\"\"")}\"");
                }

                csv.AppendLine(string.Join(",", row));
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"{form.FormKey}_responses_{DateTime.Now:yyyyMMddHHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }

        public async Task<IActionResult> OnGetExportJSONAsync(int id)
        {
            var form = await _context.FormTemplates
                .Include(f => f.FormFields)
                .FirstOrDefaultAsync(f => f.FormTemplateId == id);

            if (form == null)
            {
                return NotFound();
            }

            var submissions = await _context.FormSubmissions
                .Where(fs => fs.FormTemplateId == id)
                .OrderBy(fs => fs.SubmittedAt)
                .ToListAsync();

            var exportData = new
            {
                formName = form.FormName,
                formKey = form.FormKey,
                exportedAt = DateTime.UtcNow,
                totalResponses = submissions.Count,
                responses = submissions.Select(s => new
                {
                    responseId = s.FormSubmissionId,
                    submittedAt = s.SubmittedAt,
                    status = s.Status,
                    data = ParseFormData(s.FormData)
                })
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            var fileName = $"{form.FormKey}_responses_{DateTime.Now:yyyyMMddHHmmss}.json";

            return File(bytes, "application/json", fileName);
        }

        public async Task<IActionResult> OnGetExportExcelAsync(int id)
        {
            // For Excel export, we'll return CSV with Excel-friendly format
            // For full Excel functionality, you would need a library like EPPlus or ClosedXML
            var form = await _context.FormTemplates
                .Include(f => f.FormFields)
                .FirstOrDefaultAsync(f => f.FormTemplateId == id);

            if (form == null)
            {
                return NotFound();
            }

            var submissions = await _context.FormSubmissions
                .Where(fs => fs.FormTemplateId == id)
                .OrderBy(fs => fs.SubmittedAt)
                .ToListAsync();

            var csv = new StringBuilder();

            // Add BOM for Excel UTF-8 recognition
            csv.Append('\uFEFF');

            // Header row
            var headers = new List<string> { "Response ID", "Submitted At", "Status" };
            headers.AddRange(form.FormFields.OrderBy(f => f.DisplayOrder).Select(f => f.FieldLabel));
            csv.AppendLine(string.Join("\t", headers));

            // Data rows
            foreach (var submission in submissions)
            {
                var data = ParseFormData(submission.FormData);
                var row = new List<string>
                {
                    submission.FormSubmissionId.ToString(),
                    submission.SubmittedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    submission.Status
                };

                foreach (var field in form.FormFields.OrderBy(f => f.DisplayOrder))
                {
                    var value = data.ContainsKey(field.FieldName) ? data[field.FieldName] : "";
                    row.Add(value);
                }

                csv.AppendLine(string.Join("\t", row));
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"{form.FormKey}_responses_{DateTime.Now:yyyyMMddHHmmss}.xls";

            return File(bytes, "application/vnd.ms-excel", fileName);
        }
    }
}

