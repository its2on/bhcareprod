using Barangay.Data;
using Barangay.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Barangay.Services
{
    /// <summary>
    /// Service for managing and rendering dynamic forms
    /// </summary>
    public interface IDynamicFormService
    {
        Task<FormTemplate?> GetFormByKeyAsync(string formKey);
        Task<FormTemplate?> GetFormByIdAsync(int formId);
        Task<List<FormTemplate>> GetActiveFormsAsync();
        Task<FormSubmission> SaveSubmissionAsync(int formTemplateId, string? userId, Dictionary<string, string> formData, string? ipAddress = null, string? userAgent = null);
        Task<List<FormSubmission>> GetFormSubmissionsAsync(int formTemplateId);
    }

    public class DynamicFormService : IDynamicFormService
    {
        private readonly ApplicationDbContext _context;

        public DynamicFormService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FormTemplate?> GetFormByKeyAsync(string formKey)
        {
            return await _context.FormTemplates
                .Include(f => f.FormFields.OrderBy(ff => ff.DisplayOrder))
                    .ThenInclude(ff => ff.FormFieldOptions.OrderBy(ffo => ffo.DisplayOrder))
                .FirstOrDefaultAsync(f => f.FormKey == formKey && f.IsActive);
        }

        public async Task<FormTemplate?> GetFormByIdAsync(int formId)
        {
            return await _context.FormTemplates
                .Include(f => f.FormFields.OrderBy(ff => ff.DisplayOrder))
                    .ThenInclude(ff => ff.FormFieldOptions.OrderBy(ffo => ffo.DisplayOrder))
                .FirstOrDefaultAsync(f => f.FormTemplateId == formId);
        }

        public async Task<List<FormTemplate>> GetActiveFormsAsync()
        {
            return await _context.FormTemplates
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.FormName)
                .ToListAsync();
        }

        public async Task<FormSubmission> SaveSubmissionAsync(
            int formTemplateId, 
            string? userId, 
            Dictionary<string, string> formData,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var submission = new FormSubmission
            {
                FormTemplateId = formTemplateId,
                UserId = userId,
                FormData = JsonSerializer.Serialize(formData),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                SubmittedAt = DateTime.UtcNow,
                Status = "Submitted"
            };

            _context.FormSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            return submission;
        }

        public async Task<List<FormSubmission>> GetFormSubmissionsAsync(int formTemplateId)
        {
            return await _context.FormSubmissions
                .Where(fs => fs.FormTemplateId == formTemplateId)
                .Include(fs => fs.User)
                .OrderByDescending(fs => fs.SubmittedAt)
                .ToListAsync();
        }
    }
}
