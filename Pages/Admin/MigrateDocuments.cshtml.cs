using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class MigrateDocumentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MigrateDocumentsModel> _logger;
        private readonly IWebHostEnvironment _environment;
        
        public MigrateDocumentsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<MigrateDocumentsModel> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _environment = environment;
        }
        
        [TempData]
        public string Message { get; set; }
        
        [TempData]
        public bool Success { get; set; }
        
        public List<ApplicationUser> LegacyUsers { get; set; } = new();
        
        public int LegacyCount { get; set; }
        public int MigratedCount { get; set; }
        public int NoDocumentCount { get; set; }
        public bool Analyzed { get; set; }
        
        public async Task<IActionResult> OnGetAsync()
        {
            await LoadStatisticsAsync();
            return Page();
        }
        
        public async Task<IActionResult> OnPostAnalyzeAsync()
        {
            await AnalyzeDocumentsAsync();
            Analyzed = true;
            Success = true;
            Message = "Document analysis completed successfully.";
            return Page();
        }
        
        public async Task<IActionResult> OnPostMigrateAllAsync()
        {
            try
            {
                await AnalyzeDocumentsAsync();
                
                int migratedCount = 0;
                int errorCount = 0;
                
                foreach (var user in LegacyUsers)
                {
                    var result = await MigrateUserDocumentAsync(user.Id);
                    if (result.success)
                    {
                        migratedCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                }
                
                Success = migratedCount > 0;
                Message = $"Migration completed. Successfully migrated {migratedCount} documents. Failed: {errorCount}.";
                
                await LoadStatisticsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch migration");
                Success = false;
                Message = $"Error during migration: {ex.Message}";
                return Page();
            }
        }
        
        public async Task<IActionResult> OnPostMigrateOneAsync(string userId)
        {
            var result = await MigrateUserDocumentAsync(userId);
            Success = result.success;
            Message = result.message;
            
            await LoadStatisticsAsync();
            return Page();
        }
        
        public async Task<IActionResult> OnPostClearLegacyAsync()
        {
            try
            {
                await AnalyzeDocumentsAsync();
                
                int clearedCount = 0;
                
                foreach (var user in await _userManager.Users
                    .Where(u => !string.IsNullOrEmpty(u.ProfilePicture) && 
                                u.ProfilePicture.Contains("/uploads/residency_proofs/"))
                    .ToListAsync())
                {
                    // Check if this document is already in UserDocuments
                    var existingDoc = await _context.UserDocuments
                        .FirstOrDefaultAsync(d => d.UserId == user.Id);
                    
                    if (existingDoc != null)
                    {
                        // If already migrated, just clear ProfilePicture
                        user.ProfilePicture = null;
                        await _userManager.UpdateAsync(user);
                        clearedCount++;
                    }
                }
                
                Success = true;
                Message = $"Successfully cleared ProfilePicture field for {clearedCount} users.";
                
                await LoadStatisticsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing legacy data");
                Success = false;
                Message = $"Error clearing legacy data: {ex.Message}";
                return Page();
            }
        }
        
        private async Task LoadStatisticsAsync()
        {
            // Count of users with legacy documents in ProfilePicture
            LegacyCount = await _userManager.Users
                .CountAsync(u => !string.IsNullOrEmpty(u.ProfilePicture) && 
                                u.ProfilePicture.Contains("/uploads/residency_proofs/"));
                
            // Count of users with documents in UserDocuments
            MigratedCount = await _context.UserDocuments.CountAsync();
            
            // Count of users with no document
            NoDocumentCount = await _userManager.Users
                .CountAsync(u => string.IsNullOrEmpty(u.ProfilePicture) && 
                               !_context.UserDocuments.Any(d => d.UserId == u.Id));
        }
        
        private async Task AnalyzeDocumentsAsync()
        {
            // Get users with legacy documents in ProfilePicture
            LegacyUsers = await _userManager.Users
                .Where(u => !string.IsNullOrEmpty(u.ProfilePicture) && 
                           u.ProfilePicture.Contains("/uploads/residency_proofs/"))
                .ToListAsync();
        }
        
        private async Task<(bool success, string message)> MigrateUserDocumentAsync(string userId)
        {
            try
            {
                // Find user with the legacy document
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return (false, $"User not found with ID: {userId}");
                }
                
                // Check if user has a ProfilePicture to migrate
                if (string.IsNullOrEmpty(user.ProfilePicture) || !user.ProfilePicture.Contains("/uploads/residency_proofs/"))
                {
                    return (false, $"No valid ProfilePicture to migrate for user: {user.Email}");
                }
                
                // Check if a document already exists in UserDocuments
                var existingDoc = await _context.UserDocuments
                    .FirstOrDefaultAsync(d => d.UserId == userId);
                
                if (existingDoc != null)
                {
                    return (false, $"Document already exists in UserDocuments for user: {user.Email}");
                }
                
                // Get file details
                string filePath = user.ProfilePicture;
                string fileName = Path.GetFileName(filePath);
                string physicalPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                
                // Check if file exists
                if (!System.IO.File.Exists(physicalPath))
                {
                    _logger.LogWarning($"File does not exist at path: {physicalPath}");
                    // Create document entry anyway but mark as missing
                    var missingDoc = new UserDocument
                    {
                        UserId = user.Id,
                        FileName = fileName,
                        FilePath = filePath,
                        Status = "Missing",
                        FileSize = 0,
                        ContentType = "unknown",
                        FileType = Path.GetExtension(fileName)?.TrimStart('.')?.ToLower() ?? "unknown"
                    };
                    
                    _context.UserDocuments.Add(missingDoc);
                    await _context.SaveChangesAsync();
                    
                    // Clear ProfilePicture
                    user.ProfilePicture = null;
                    await _userManager.UpdateAsync(user);
                    
                    return (true, $"Document entry created but file is missing for user: {user.Email}");
                }
                
                // Get file info
                var fileInfo = new FileInfo(physicalPath);
                string contentType = GetContentType(Path.GetExtension(physicalPath));
                
                // Create new UserDocument
                var userDocument = new UserDocument
                {
                    UserId = user.Id,
                    FileName = fileName,
                    FilePath = filePath,
                    Status = "Pending",
                    FileSize = fileInfo.Length,
                    ContentType = contentType,
                    FileType = Path.GetExtension(fileName)?.TrimStart('.')?.ToLower() ?? "unknown"
                };
                
                _context.UserDocuments.Add(userDocument);
                
                // Clear ProfilePicture
                user.ProfilePicture = null;
                await _userManager.UpdateAsync(user);
                
                await _context.SaveChangesAsync();
                
                return (true, $"Successfully migrated document for user: {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error migrating document for user {userId}");
                return (false, $"Error: {ex.Message}");
            }
        }
        
        
        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }
    }
} 