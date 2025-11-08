using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace Barangay.Pages.Admin
{
    // Extension methods for common utility functions
    public static class DateTimeExtensions
    {
        // Calculate age based on a reference date (for consistent age calculation)
        public static int CalculateAge(this DateTime birthDate, DateTime referenceDate)
        {
            int age = referenceDate.Year - birthDate.Year;
            
            // Adjust age if birthday hasn't occurred yet in the reference year
            if (birthDate.Date > referenceDate.AddYears(-age))
                age--;
                
            return age;
        }

        
        // Check if a person is a minor (under 18) based on a reference date
        public static bool IsMinor(this DateTime birthDate, DateTime referenceDate)
        {
            return birthDate.CalculateAge(referenceDate) < 18;
        }
    }

    [Authorize(Policy = "AccessAdminDashboard")]
    [ValidateAntiForgeryToken]
    public class UserManagementModel : AdminPageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserManagementModel> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IEmailService _emailService;
        private readonly IAuditTrailService _auditTrail;

        public UserManagementModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            ILogger<UserManagementModel> logger,
            IWebHostEnvironment environment,
            IDataEncryptionService encryptionService,
            IEmailService emailService,
            IAuditTrailService auditTrail)
            : base(notificationService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _environment = environment;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _auditTrail = auditTrail;
        }

        public List<ApplicationUser> Users { get; set; } = new();
        public List<UserDocument> UserDocuments { get; set; } = new();
        public List<GuardianInformation> GuardianInformation { get; set; } = new();
        
        [BindProperty(SupportsGet = true)]
        public string LastNameFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string SuffixFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "all";
        
        public int TotalUsers { get; set; }
        public int PendingUsers { get; set; }
        public int VerifiedUsers { get; set; }
        public int RejectedUsers { get; set; }
        
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }

        // Model for batch operations
        public class BatchOperationModel
        {
            public List<string> UserIds { get; set; } = new();
        }

        // Model for batch operation response
        public class BatchOperationResponse
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public int ProcessedCount { get; set; }
            public int FailedCount { get; set; }
            public List<string>? Errors { get; set; } = new();
        }

        // DTO for UpdateUserStatus JSON payload
        public class UpdateUserStatusRequest
        {
            public string? UserId { get; set; }
            public string? Status { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string status = "all")
        {
            try
            {
                StatusFilter = status;

                // Get the IDs of users with special roles to exclude
                var excludedRoleNames = new[] { "Admin", "System Administrator", "Admin Staff", "System Admin", "Staff Admin", "Doctor", "Nurse", "Head Nurse", "Head Doctor" };
                var excludedRoles = await _context.Roles
                    .Where(r => excludedRoleNames.Contains(r.Name))
                    .ToListAsync();
                    
                var excludedUserIds = await _context.UserRoles
                    .Where(ur => excludedRoles.Select(r => r.Id).Contains(ur.RoleId))
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                // Build the query
                var query = _userManager.Users
                    .Include(u => u.UserDocuments)
                    .Include(u => u.GuardianConsents)
                    .Where(u => !excludedUserIds.Contains(u.Id));

                // Apply status filter
                if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
                {
                    query = query.Where(u => u.Status.ToLower() == status.ToLower());
                }

                // Get users
                Users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                // Decrypt user data for authorized users
                foreach (var user in Users)
                {
                    user.DecryptSensitiveData(_encryptionService, User);
                    
                    // Manually decrypt Email since it's not marked with [Encrypted] attribute
                    if (!string.IsNullOrEmpty(user.Email) && _encryptionService.IsEncrypted(user.Email))
                    {
                        user.Email = user.Email.DecryptForUser(_encryptionService, User);
                    }
                    
                    // Manually decrypt PhoneNumber since it's not marked with [Encrypted] attribute
                    if (!string.IsNullOrEmpty(user.PhoneNumber) && _encryptionService.IsEncrypted(user.PhoneNumber))
                    {
                        user.PhoneNumber = user.PhoneNumber.DecryptForUser(_encryptionService, User);
                    }
                    
                    // Decrypt guardian names
                    if (user.GuardianConsents != null)
                    {
                        foreach (var guardian in user.GuardianConsents)
                        {
                            if (!string.IsNullOrEmpty(guardian.GuardianFirstName) && _encryptionService.IsEncrypted(guardian.GuardianFirstName))
                            {
                                guardian.GuardianFirstName = _encryptionService.Decrypt(guardian.GuardianFirstName);
                            }
                            if (!string.IsNullOrEmpty(guardian.GuardianLastName) && _encryptionService.IsEncrypted(guardian.GuardianLastName))
                            {
                                guardian.GuardianLastName = _encryptionService.Decrypt(guardian.GuardianLastName);
                            }
                        }
                    }
                }
                    
                // Reference date for age calculation
                var referenceDate = new DateTime(2025, 6, 16);
                
                // Find users under 18 years old
                var underageUserIds = Users
                    .Where(u => u.BirthDate.HasValue && u.BirthDate.Value.IsMinor(referenceDate))
                    .Select(u => u.Id)
                    .ToList();
                
                _logger.LogInformation($"Found {underageUserIds.Count} users under 18 years old");
                
                // Load guardian information only for underage users
                if (underageUserIds.Any())
                {
                    try
                    {
                        // Check if GuardianInformation table exists and has data
                        GuardianInformation = await _context.GuardianInformation
                            .Where(g => underageUserIds.Contains(g.UserId))
                            .AsNoTracking()
                            .ToListAsync();
                            
                        // Decrypt guardian names
                        foreach (var guardian in GuardianInformation)
                        {
                            if (!string.IsNullOrEmpty(guardian.GuardianFirstName) && _encryptionService.IsEncrypted(guardian.GuardianFirstName))
                            {
                                guardian.GuardianFirstName = _encryptionService.Decrypt(guardian.GuardianFirstName);
                            }
                            if (!string.IsNullOrEmpty(guardian.GuardianLastName) && _encryptionService.IsEncrypted(guardian.GuardianLastName))
                            {
                                guardian.GuardianLastName = _encryptionService.Decrypt(guardian.GuardianLastName);
                            }
                        }
                            
                        _logger.LogInformation($"Loaded {GuardianInformation.Count} guardian information records");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading guardian information");
                        GuardianInformation = new List<GuardianInformation>();
                    }
                }
                else
                {
                    GuardianInformation = new List<GuardianInformation>();
                }

                // Get counts for different statuses (excluding special roles)
                TotalUsers = await query.CountAsync();
                PendingUsers = await query.CountAsync(u => u.Status.ToLower() == "pending");
                VerifiedUsers = await query.CountAsync(u => u.Status.ToLower() == "verified");
                RejectedUsers = await query.CountAsync(u => u.Status.ToLower() == "rejected");

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management page");
                TempData["ErrorMessage"] = "An error occurred while loading the page. Please try again.";
                return Page();
            }
        }
        
        public async Task<IActionResult> OnPostApproveAsync(string id, string notes)
        {
            try
            {
                _logger.LogInformation($"Attempting to approve user with ID: {id}");
                
                // Verify current user has admin role
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("Current user not found during approval process");
                    TempData["ErrorMessage"] = "Authentication error. Please login again.";
                    return RedirectToPage();
                }
                
                _logger.LogInformation($"Admin user: {currentUser.Email} attempting to approve user");
                
                // Find the user to approve
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {id}");
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToPage();
                }
                
                _logger.LogInformation($"Found user to approve: {user.Email}, Current Status: {user.Status}");
                
                // Check for residency proof
                UserDocument? document = null;
                try
                {
                    document = await _context.UserDocuments
                        .FirstOrDefaultAsync(d => d.UserId == id);
                    
                    if (document == null)
                    {
                        _logger.LogWarning($"No residency proof document found for user: {user.Email}");
                        TempData["WarningMessage"] = "Proceeding with approval despite missing residency proof document.";
                    }
                    else
                    {
                        _logger.LogInformation($"Found residency document for user: {document.FileName}");
                        // Update document status
                        document.Status = "Verified";
                        document.ApprovedAt = DateTime.UtcNow;
                        document.ApprovedBy = currentUser.Id;
                        
                        // Ensure no NULL values
                        document.FileName = document.FileName ?? "";
                        document.FilePath = document.FilePath ?? "";
                        document.ContentType = document.ContentType ?? "application/octet-stream";
                        document.Status = document.Status ?? "Verified";
                        document.ApprovedBy = document.ApprovedBy ?? "";
                        
                        _context.UserDocuments.Update(document);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing document for user: {user.Email}");
                    TempData["WarningMessage"] = "Unable to process residency document, but proceeding with approval.";
                }

                // Check if user is underage and automatically approve guardian consent if exists
                DateTime? birthDate = user.BirthDate;
                if (birthDate != null)
                {
                    // Use reference date of today to calculate age
                    var today = DateTime.Today;
                    int age = today.Year - birthDate.Value.Year;
                    
                    // Adjust age if birthday hasn't occurred yet this year
                    if (birthDate.Value.Date > today.AddYears(-age)) 
                        age--;
                    
                    // If user is under 18, approve guardian consent
                    if (age < 18)
                    {
                        _logger.LogInformation($"User {user.Email} is underage ({age} years old). Checking for guardian information.");
                        
                        try
                        {
                            // Find guardian information
                            var guardian = await _context.GuardianInformation
                                .FirstOrDefaultAsync(g => g.UserId == id);
                                
                            if (guardian != null)
                            {
                                _logger.LogInformation($"Found guardian information for user {user.Email}. Updating consent status to Approved.");
                                
                                // Update guardian consent status to Approved
                                guardian.ConsentStatus = "Approved";
                                _context.GuardianInformation.Update(guardian);
                                
                                // Create notification for guardian consent approval
                                await _notificationService.CreateNotificationForUserAsync(
                                    userId: id,
                                    title: "Guardian Consent Approved",
                                    message: "Your guardian consent has been approved along with your account verification.",
                                    type: "Success",
                                    link: "/Account/Profile"
                                );
                            }
                            else
                            {
                                _logger.LogWarning($"No guardian information found for underage user {user.Email}");
                                TempData["WarningMessage"] = "User is underage but no guardian information found. Account approved anyway.";
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing guardian information for underage user: {user.Email}");
                            TempData["WarningMessage"] = "Error processing guardian information, but proceeding with approval.";
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"User {user.Email} is not underage ({age} years old). No guardian consent needed.");
                    }
                }

                // Update user status
                user.Status = "Verified";
                user.IsActive = true;
                user.EncryptedStatus = "Verified";
                
                _logger.LogInformation($"Attempting to update user status to: Verified");
                var updateResult = await _userManager.UpdateAsync(user);
                
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        _logger.LogError($"User update error: {error.Code} - {error.Description}");
                    }
                    TempData["ErrorMessage"] = "Failed to update user status: " + 
                        string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }
                
                _logger.LogInformation($"Successfully updated user status for {user.Email}");
                
                // Update patient status if exists
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == id);
                    
                if (patient != null)
                {
                    _logger.LogInformation($"Updating associated patient record for user: {user.Email}");
                    patient.Status = "Active";
                    _context.Patients.Update(patient);
                }
                
                // Update document status
                if (document != null)
                {
                    document.Status = "Approved";
                    document.ApprovedAt = DateTime.UtcNow;
                    document.ApprovedBy = User.Identity?.Name ?? "System";
                    _logger.LogInformation($"Updating document status to Approved, approved by: {User.Identity?.Name ?? "System"}");
                    
                    if (!string.IsNullOrEmpty(notes))
                    {
                        _logger.LogInformation($"Admin notes for approval: {notes}");
                    }
                    
                    _context.UserDocuments.Update(document);
                }
                else
                {
                    _logger.LogInformation("No document found to update for approval");
                }
                
                // Update guardian consent status if user is a minor
                var guardianInfo = await _context.GuardianInformation
                    .FirstOrDefaultAsync(g => g.UserId == id);
                    
                if (guardianInfo != null)
                {
                    guardianInfo.ConsentStatus = "Approved";
                    _context.GuardianInformation.Update(guardianInfo);
                    _logger.LogInformation($"Guardian consent approved for user {id}");
                }
                
                // Assign role if needed
                if (!await _userManager.IsInRoleAsync(user, "PATIENT"))
                {
                    _logger.LogInformation($"Assigning PATIENT role to user: {user.Email}");
                    var roleResult = await _userManager.AddToRoleAsync(user, "PATIENT");
                    
                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            _logger.LogWarning($"Role assignment error: {error.Code} - {error.Description}");
                        }
                        // Continue even if role assignment fails - log but don't return
                    }
                }
                
                _logger.LogInformation($"Saving changes to database");
                await _context.SaveChangesAsync();

                // AUDIT: Log user approval
                await _auditTrail.LogAsync(
                    "Update",
                    $"Approved user account: {user.Email}",
                    "ApplicationUser",
                    user.Id,
                    "Pending",
                    "Verified",
                    $"Admin approved user account for {user.Email} - Status changed from Pending to Verified"
                );

                // Create notification
                _logger.LogInformation($"Creating notification for user approval");
                await _notificationService.CreateNotificationAsync(
                    title: "User Approved",
                    message: $"User {user.UserName} ({user.FirstName} {user.LastName}) has been approved and verified.",
                    type: "Success",
                    link: "/Admin/UserManagement"
                );
                
                // Create notification for the approved user
                await _notificationService.CreateNotificationForUserAsync(
                    userId: id,
                    title: "Account Approved",
                    message: "Your account has been approved.",
                    type: "Success",
                    link: "https://bhcare.software"
                );
                
                // Send approval email notification
                try
                {
                    // Decrypt user name components
                    var decryptedFirstName = !string.IsNullOrEmpty(user.FirstName) && _encryptionService.IsEncrypted(user.FirstName) 
                        ? _encryptionService.Decrypt(user.FirstName) 
                        : user.FirstName;
                    var decryptedLastName = !string.IsNullOrEmpty(user.LastName) && _encryptionService.IsEncrypted(user.LastName) 
                        ? _encryptionService.Decrypt(user.LastName) 
                        : user.LastName;
                    
                    var userName = $"{decryptedFirstName} {decryptedLastName}".Trim();
                    var userEmail = user.Email;
                    
                    // Decrypt email if it's encrypted
                    if (!string.IsNullOrEmpty(userEmail) && _encryptionService.IsEncrypted(userEmail))
                    {
                        userEmail = _encryptionService.Decrypt(userEmail);
                    }
                    
                    if (!string.IsNullOrEmpty(userEmail) && userEmail.Contains("@"))
                    {
                        var emailSubject = "Account Approved - Baesa Health Care";
                        var emailBody = GenerateApprovalEmailBody(userName, userEmail);
                        
                        await _emailService.SendEmailAsync(userEmail, emailSubject, emailBody);
                        _logger.LogInformation($"Approval email sent successfully to {userEmail}");
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot send approval email - user email is null, empty, or invalid for user ID: {id}. Email: {userEmail}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send approval email to user {user.Email}. Approval process continues.");
                    // Don't fail the approval process if email fails
                }
                
                // Update pending users count for notification badge
                PendingUsers = await _userManager.Users
                    .CountAsync(u => u.Status == "Pending");
                    
                ViewData["PendingUsersCount"] = PendingUsers;
                
                // Set success message
                TempData["SuccessMessage"] = "User has been approved successfully.";
                TempData["UpdateNotificationBadge"] = "true"; // Flag to update notification badge on redirect
                
                _logger.LogInformation($"User approval process completed successfully for: {user.Email}");
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user approval process for UserId {UserId}: {ErrorMessage}", 
                    id, ex.Message);
                    
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerError}", ex.InnerException.Message);
                }
                
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                
                TempData["ErrorMessage"] = "An error occurred while approving the user. Please try again.";
                return RedirectToPage();
            }
        }

        // Returns a JSON response with guardian proof and metadata for the modal
        public async Task<IActionResult> OnGetGuardianProofAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return new JsonResult(new { success = false, message = "Missing userId." });
                }

                var guardian = await _context.GuardianInformation
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.UserId == userId);

                if (guardian == null)
                {
                    return new JsonResult(new { success = false, message = "No guardian information found." });
                }

                string? proofPath = null;
                bool hasProof = false;

                // Prefer stored file path if it exists under wwwroot
                if (!string.IsNullOrWhiteSpace(guardian.ResidencyProofPath))
                {
                    try
                    {
                        var relative = guardian.ResidencyProofPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                        var absolute = Path.Combine(_environment.WebRootPath, relative);
                        if (System.IO.File.Exists(absolute))
                        {
                            proofPath = guardian.ResidencyProofPath;
                            hasProof = true;
                        }
                    }
                    catch { /* ignore and fall back */ }
                }

                // Fallback to bytes via API endpoint if available
                if (!hasProof && guardian.ResidencyProof != null && guardian.ResidencyProof.Length > 0)
                {
                    proofPath = $"/api/Admin/GetGuardianProof/{guardian.GuardianId}";
                    hasProof = true;
                }

                return new JsonResult(new {
                    success = true,
                    guardianFirstName = guardian.GuardianFirstName ?? guardian.FirstName ?? string.Empty,
                    guardianLastName = guardian.GuardianLastName ?? guardian.LastName ?? string.Empty,
                    consentStatus = guardian.ConsentStatus ?? "Pending",
                    proofType = guardian.ProofType ?? "GuardianResidencyProof",
                    createdAt = guardian.CreatedAt,
                    hasProof,
                    proofPath = proofPath ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetGuardianProofAsync for user {UserId}", userId);
                return new JsonResult(new { success = false, message = "Error loading guardian information." });
            }
        }

        // Handles fetch('/Admin/UserManagement?handler=UpdateGuardianConsent') JSON requests
        public async Task<IActionResult> OnPostUpdateGuardianConsentAsync()
        {
            try
            {
                // Read JSON body
                string body;
                using (var reader = new StreamReader(Request.Body))
                {
                    body = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return new JsonResult(new { success = false, message = "Empty request body." });
                }

                var payload = System.Text.Json.JsonSerializer.Deserialize<UpdateUserStatusRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload == null || string.IsNullOrWhiteSpace(payload.UserId) || string.IsNullOrWhiteSpace(payload.Status))
                {
                    return new JsonResult(new { success = false, message = "Invalid payload. Missing userId or status." });
                }

                var userId = payload.UserId.Trim();
                var newStatus = payload.Status.Trim();

                var guardian = await _context.GuardianInformation.FirstOrDefaultAsync(g => g.UserId == userId);
                if (guardian == null)
                {
                    return new JsonResult(new { success = false, message = "Guardian record not found." });
                }

                // Normalize and set status
                switch (newStatus.ToLowerInvariant())
                {
                    case "approved":
                        guardian.ConsentStatus = "Approved";
                        break;
                    case "rejected":
                        guardian.ConsentStatus = "Rejected";
                        break;
                    case "pending":
                        guardian.ConsentStatus = "Pending";
                        break;
                    default:
                        return new JsonResult(new { success = false, message = $"Unsupported status '{newStatus}'." });
                }

                _context.GuardianInformation.Update(guardian);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = $"Guardian consent {guardian.ConsentStatus.ToLower()} successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateGuardianConsent handler");
                return new JsonResult(new { success = false, message = "An error occurred while updating guardian consent." });
            }
        }

        public int CalculateAge(DateTime birthDate, DateTime referenceDate)
        {
            int age = referenceDate.Year - birthDate.Year;
            
            // Adjust age if birthday hasn't occurred yet in the reference year
            if (birthDate.Date > referenceDate.AddYears(-age))
                age--;
                
            return age;
        }

        private string GenerateApprovalEmailBody(string userName, string userEmail)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #ffffff; padding: 30px; border: 1px solid #dee2e6; }}
                        .success-badge {{ background-color: #d4edda; color: #155724; padding: 10px; border-radius: 5px; text-align: center; margin: 20px 0; }}
                        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 5px 5px; font-size: 14px; color: #6c757d; }}
                        .cta-button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>✅ Account Approved - Baesa Health Care</h2>
                        </div>
                        
                        <div class='content'>
                            <h3>Congratulations, {userName}!</h3>
                            
                            <div class='success-badge'>
                                <strong>🎉 Your account has been approved and verified!</strong>
                            </div>
                            
                            <p>We are pleased to inform you that your Baesa Health Care account has been successfully approved. You can now access all the features and services available in our system.</p>
                            
                            <p><strong>What you can do now:</strong></p>
                            <ul>
                                <li>✅ Access your personal health dashboard</li>
                                <li>✅ Schedule medical appointments</li>
                                <li>✅ View your medical records</li>
                                <li>✅ Request health services</li>
                                <li>✅ Receive health notifications</li>
                            </ul>
                            
                            <p>To get started, please log in to your account using your registered email: <strong>{userEmail}</strong></p>
                            
                            <div style='text-align: center;'>
                                <a href='https://your-domain.com/Account/Login' class='cta-button'>Login to Your Account</a>
                            </div>
                            
                            <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
                            
                            <p>Thank you for choosing Baesa Health Care for your health needs!</p>
                        </div>
                        
                        <div class='footer'>
                            <p>This is an automated message from Baesa Health Care System</p>
                            <p>Please do not reply to this email</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerateRejectionEmailBody(string userName, string userEmail, string reason = "")
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #ffffff; padding: 30px; border: 1px solid #dee2e6; }}
                        .warning-badge {{ background-color: #f8d7da; color: #721c24; padding: 10px; border-radius: 5px; text-align: center; margin: 20px 0; }}
                        .guide-section {{ background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #007bff; }}
                        .step {{ margin: 15px 0; padding: 10px; background-color: white; border-radius: 5px; border-left: 3px solid #28a745; }}
                        .step-number {{ background-color: #007bff; color: white; border-radius: 50%; width: 25px; height: 25px; display: inline-flex; align-items: center; justify-content: center; font-weight: bold; margin-right: 10px; }}
                        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 5px 5px; font-size: 14px; color: #6c757d; }}
                        .cta-button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 10px 5px; }}
                        .cta-button.secondary {{ background-color: #6c757d; }}
                        .requirements {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>❌ Account Application Status - Barangay Health Care</h2>
                        </div>
                        
                        <div class='content'>
                            <h3>Dear {userName},</h3>
                            
                            <div class='warning-badge'>
                                <strong>⚠️ Your account application requires attention</strong>
                            </div>
                            
                            <p>We regret to inform you that your Barangay Health Care account application has been reviewed and requires additional information or documentation.</p>
                            
                            {(string.IsNullOrEmpty(reason) ? "" : $"<p><strong>Reason:</strong> {reason}</p>")}
                            
                            <div class='guide-section'>
                                <h4 style='color: #007bff; margin-top: 0;'>📋 Step-by-Step Guide to Get Approved</h4>
                                
                                <div class='step'>
                                    <span class='step-number'>1</span>
                                    <strong>Review Your Personal Information</strong>
                                    <ul style='margin: 10px 0 0 35px;'>
                                        <li>Ensure your full name matches your government ID</li>
                                        <li>Verify your birth date is correct</li>
                                        <li>Check that your contact information is accurate</li>
                                        <li>Confirm your address is complete and valid</li>
                                    </ul>
                                </div>
                                
                                <div class='step'>
                                    <span class='step-number'>2</span>
                                    <strong>Upload Clear Identity Documents</strong>
                                    <ul style='margin: 10px 0 0 35px;'>
                                        <li>Use a valid government-issued ID (Driver's License, Passport, National ID, etc.)</li>
                                        <li>Ensure the document is not expired</li>
                                        <li>Take clear, well-lit photos of your ID</li>
                                        <li>Make sure all text is readable and not blurry</li>
                                        <li>Avoid shadows, glare, or reflections</li>
                                    </ul>
                                </div>
                                
                                <div class='step'>
                                    <span class='step-number'>3</span>
                                    <strong>Provide Residency Proof</strong>
                                    <ul style='margin: 10px 0 0 35px;'>
                                        <li>Upload a recent utility bill (electricity, water, internet)</li>
                                        <li>Bank statement with your current address</li>
                                        <li>Barangay certificate of residency</li>
                                        <li>Lease agreement or property title</li>
                                        <li>Document must be dated within the last 3 months</li>
                                    </ul>
                                </div>
                                
                                <div class='step'>
                                    <span class='step-number'>4</span>
                                    <strong>Complete All Required Fields</strong>
                                    <ul style='margin: 10px 0 0 35px;'>
                                        <li>Fill out all mandatory information</li>
                                        <li>Provide accurate emergency contact details</li>
                                        <li>Answer all health-related questions honestly</li>
                                        <li>Agree to terms and conditions</li>
                                    </ul>
                                </div>
                                
                                <div class='step'>
                                    <span class='step-number'>5</span>
                                    <strong>Submit for Review</strong>
                                    <ul style='margin: 10px 0 0 35px;'>
                                        <li>Double-check all information before submitting</li>
                                        <li>Ensure all documents are properly uploaded</li>
                                        <li>Wait for our team to review your application</li>
                                        <li>You will receive an email notification once reviewed</li>
                                    </ul>
                                </div>
                            </div>
                            
                            <div class='requirements'>
                                <h5 style='color: #856404; margin-top: 0;'>📄 Document Requirements Checklist</h5>
                                <ul style='margin: 10px 0;'>
                                    <li>✅ Valid government-issued ID (not expired)</li>
                                    <li>✅ Clear, readable photo of your ID</li>
                                    <li>✅ Recent residency proof (within 3 months)</li>
                                    <li>✅ Complete personal information</li>
                                    <li>✅ Accurate contact details</li>
                                    <li>✅ Emergency contact information</li>
                                </ul>
                            </div>
                            
                            <p><strong>Common Reasons for Rejection:</strong></p>
                            <ul>
                                <li>📷 Blurry or unclear document photos</li>
                                <li>📅 Expired identification documents</li>
                                <li>🏠 Missing or outdated residency proof</li>
                                <li>✏️ Incomplete or inaccurate personal information</li>
                                <li>📞 Invalid or missing contact information</li>
                            </ul>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='https://your-domain.com/Account/Register' class='cta-button'>🔄 Reapply Now</a>
                                <a href='https://your-domain.com/Contact' class='cta-button secondary'>📞 Contact Support</a>
                            </div>
                            
                            <p><strong>Need Help?</strong> Our support team is here to assist you. Contact us at:</p>
                            <ul>
                                <li>📧 Email: bhcare@barangay161.ph</li>
                                <li>📞 Phone: (02) 8123-4567</li>
                                <li>🏢 Visit: Barangay 161 Health Center</li>
                                <li>⏰ Hours: Monday-Friday, 8:00 AM - 5:00 PM</li>
                            </ul>
                            
                            <p>We appreciate your interest in using our health care services and look forward to assisting you once your application is complete.</p>
                        </div>
                        
                        <div class='footer'>
                            <p>This is an automated message from Baesa Health Care System</p>
                            <p>Please do not reply to this email</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        // Handles fetch('/Admin/UserManagement?handler=UpdateUserStatus') JSON requests
        public async Task<IActionResult> OnPostUpdateUserStatusAsync()
        {
            try
            {
                // Read JSON body
                string body;
                using (var reader = new StreamReader(Request.Body))
                {
                    body = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return new JsonResult(new { success = false, message = "Empty request body." });
                }

                var payload = System.Text.Json.JsonSerializer.Deserialize<UpdateUserStatusRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload == null || string.IsNullOrWhiteSpace(payload.UserId) || string.IsNullOrWhiteSpace(payload.Status))
                {
                    return new JsonResult(new { success = false, message = "Invalid payload. Missing userId or status." });
                }

                var user = await _userManager.FindByIdAsync(payload.UserId);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found." });
                }

                // Capture old status for audit trail
                var oldStatus = user.Status;

                var status = payload.Status.Trim();
                _logger.LogInformation($"UpdateUserStatus handler: setting user {user.Email} to status '{status}'");

                switch (status.ToLowerInvariant())
                {
                    case "verified":
                        user.Status = "Verified";
                        user.IsActive = true;
                        user.EncryptedStatus = "Verified";
                        // If the user is under 18, auto-approve guardian consent
                        try
                        {
                            if (user.BirthDate.HasValue)
                            {
                                var today = DateTime.Today;
                                var userBirthDate = user.BirthDate.Value;
                                var age = today.Year - userBirthDate.Year;
                                if (userBirthDate.Date > today.AddYears(-age)) age--;

                                if (age < 18)
                                {
                                    var guardian = await _context.GuardianInformation.FirstOrDefaultAsync(g => g.UserId == user.Id);
                                    if (guardian != null)
                                    {
                                        guardian.ConsentStatus = "Approved";
                                        _context.GuardianInformation.Update(guardian);
                                        _logger.LogInformation("Auto-approved guardian consent for underage user {UserId}", user.Id);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("No guardian record found to auto-approve for underage user {UserId}", user.Id);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to auto-approve guardian consent for user {UserId}", user.Id);
                        }
                        break;
                    case "rejected":
                        user.Status = "Rejected";
                        user.IsActive = false;
                        user.EncryptedStatus = "Inactive";
                        break;
                    case "pending":
                        user.Status = "Pending";
                        user.IsActive = false;
                        user.EncryptedStatus = "Inactive";
                        break;
                    default:
                        return new JsonResult(new { success = false, message = $"Unsupported status '{status}'." });
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to update user {UserId}: {Errors}", user.Id, errors);
                    return new JsonResult(new { success = false, message = $"Failed to update user: {errors}" });
                }

                // Update related patient row if exists
                try
                {
                    var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (patient != null)
                    {
                        patient.Status = user.IsActive ? "Active" : "Inactive";
                        _context.Patients.Update(patient);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to update patient status for user {UserId}", user.Id);
                }

                // Assign PATIENT role on verify (best-effort)
                if (status.Equals("verified", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (!await _userManager.IsInRoleAsync(user, "PATIENT"))
                        {
                            var roleResult = await _userManager.AddToRoleAsync(user, "PATIENT");
                            if (!roleResult.Succeeded)
                            {
                                _logger.LogWarning("Failed to assign PATIENT role to {UserId}", user.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error assigning PATIENT role to {UserId}", user.Id);
                    }
                }

                await _context.SaveChangesAsync();

                // AUDIT: Log user status change
                var actionDescription = status.ToLowerInvariant() switch
                {
                    "verified" => "Approved user account",
                    "rejected" => "Rejected user account",
                    "pending" => "Set user account to pending",
                    _ => "Updated user account status"
                };
                
                await _auditTrail.LogAsync(
                    "Update",
                    $"{actionDescription}: {user.Email}",
                    "ApplicationUser",
                    user.Id,
                    oldStatus,
                    user.Status,
                    $"Admin changed user status from {oldStatus} to {user.Status}"
                );

                // Handle suspension logic for rejections
                SuspensionResult? suspensionResult = null;
                if (status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
                {
                    suspensionResult = await HandleUserSuspension(payload.UserId, user);
                }

                // Send email notification for status changes
                try
                {
                    // Decrypt user name components
                    var decryptedFirstName = !string.IsNullOrEmpty(user.FirstName) && _encryptionService.IsEncrypted(user.FirstName) 
                        ? _encryptionService.Decrypt(user.FirstName) 
                        : user.FirstName;
                    var decryptedLastName = !string.IsNullOrEmpty(user.LastName) && _encryptionService.IsEncrypted(user.LastName) 
                        ? _encryptionService.Decrypt(user.LastName) 
                        : user.LastName;
                    
                    var userName = $"{decryptedFirstName} {decryptedLastName}".Trim();
                    var userEmail = user.Email;
                    
                    // Decrypt email if it's encrypted
                    if (!string.IsNullOrEmpty(userEmail) && _encryptionService.IsEncrypted(userEmail))
                    {
                        userEmail = _encryptionService.Decrypt(userEmail);
                    }
                    
                    if (!string.IsNullOrEmpty(userEmail) && userEmail.Contains("@"))
                    {
                        if (status.Equals("verified", StringComparison.OrdinalIgnoreCase))
                        {
                            // Send approval email
                            var emailSubject = "Account Approved - Baesa Health Care";
                            var emailBody = GenerateApprovalEmailBody(userName, userEmail);
                            
                            await _emailService.SendEmailAsync(userEmail, emailSubject, emailBody);
                            _logger.LogInformation($"Approval email sent successfully to {userEmail}");
                        }
                        else if (status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
                        {
                            // Send rejection email with suspension info
                            var emailSubject = "Account Application Status - Baesa Health Care";
                            var emailBody = suspensionResult?.IsSuspended == true
                                ? GenerateSuspensionEmailBody(userName, userEmail, suspensionResult.DenialCount, suspensionResult.SuspensionPeriod, suspensionResult.SuspensionEndDate)
                                : GenerateRejectionEmailBody(userName, userEmail);
                            
                            await _emailService.SendEmailAsync(userEmail, emailSubject, emailBody);
                            _logger.LogInformation($"Rejection email sent successfully to {userEmail}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot send status email - user email is null, empty, or invalid for user ID: {payload.UserId}. Email: {userEmail}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send status email to user {user.Email}. Status update process continues.");
                    // Don't fail the status update process if email fails
                }

                var successMessage = status.Equals("verified", StringComparison.OrdinalIgnoreCase)
                    ? "User has been approved successfully."
                    : status.Equals("rejected", StringComparison.OrdinalIgnoreCase)
                        ? suspensionResult?.IsSuspended == true
                            ? $"User rejected and suspended for {suspensionResult.SuspensionPeriod}. Denial count: {suspensionResult.DenialCount}"
                            : "User has been rejected."
                        : "User status updated.";

                return new JsonResult(new { success = true, message = successMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUserStatus handler");
                return new JsonResult(new { success = false, message = "An error occurred while updating user status." });
            }
        }

        public async Task<IActionResult> OnPostDeleteUserAsync()
        {
            try
            {
                // Read JSON body
                string body;
                using (var reader = new StreamReader(Request.Body))
                {
                    body = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return new JsonResult(new { success = false, message = "Empty request body." });
                }

                var payload = System.Text.Json.JsonSerializer.Deserialize<UpdateUserStatusRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload == null || string.IsNullOrWhiteSpace(payload.UserId))
                {
                    return new JsonResult(new { success = false, message = "Invalid payload. Missing userId." });
                }

                var userId = payload.UserId.Trim();
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found." });
                }

                _logger.LogInformation($"Deleting user: {user.Email} (ID: {userId})");

                // Delete related data first (to avoid foreign key constraints)
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Delete user documents
                    var userDocuments = await _context.UserDocuments.Where(d => d.UserId == userId).ToListAsync();
                    _context.UserDocuments.RemoveRange(userDocuments);

                    // Delete guardian information
                    var guardianInfo = await _context.GuardianInformation.Where(g => g.UserId == userId).ToListAsync();
                    _context.GuardianInformation.RemoveRange(guardianInfo);

                    // Delete patient records
                    var patients = await _context.Patients.Where(p => p.UserId == userId).ToListAsync();
                    _context.Patients.RemoveRange(patients);

                    // Delete appointments
                    var appointments = await _context.Appointments.Where(a => a.PatientId == userId).ToListAsync();
                    _context.Appointments.RemoveRange(appointments);

                    // Delete medical records
                    var medicalRecords = await _context.MedicalRecords.Where(m => m.PatientId == userId).ToListAsync();
                    _context.MedicalRecords.RemoveRange(medicalRecords);

                    // Delete prescriptions (with error handling for schema mismatch)
                    try
                    {
                        var prescriptions = await _context.Prescriptions.Where(p => p.PatientId == userId).ToListAsync();
                        _context.Prescriptions.RemoveRange(prescriptions);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete prescriptions for user {UserId}, continuing with other deletions", userId);
                    }

                    // Delete assessments
                    var ncdAssessments = await _context.NCDRiskAssessments.Where(n => n.UserId == userId).ToListAsync();
                    _context.NCDRiskAssessments.RemoveRange(ncdAssessments);

                    var heeadsssAssessments = await _context.HEEADSSSAssessments.Where(h => h.UserId == userId).ToListAsync();
                    _context.HEEADSSSAssessments.RemoveRange(heeadsssAssessments);

                    // Delete notifications
                    var notifications = await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
                    _context.Notifications.RemoveRange(notifications);

                    // Delete Identity-related data
                    try
                    {
                        // Delete user claims
                        var userClaims = await _context.UserClaims.Where(c => c.UserId == userId).ToListAsync();
                        _context.UserClaims.RemoveRange(userClaims);

                        // Delete user roles
                        var userRoles = await _context.UserRoles.Where(r => r.UserId == userId).ToListAsync();
                        _context.UserRoles.RemoveRange(userRoles);

                        // Delete user logins
                        var userLogins = await _context.UserLogins.Where(l => l.UserId == userId).ToListAsync();
                        _context.UserLogins.RemoveRange(userLogins);

                        // Delete user tokens
                        var userTokens = await _context.UserTokens.Where(t => t.UserId == userId).ToListAsync();
                        _context.UserTokens.RemoveRange(userTokens);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete Identity-related data for user {UserId}, continuing with user deletion", userId);
                    }

                    // Save changes to related data
                    await _context.SaveChangesAsync();

                    // Delete the user
                    var deleteResult = await _userManager.DeleteAsync(user);
                    if (!deleteResult.Succeeded)
                    {
                        var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to delete user {UserId}: {Errors}", userId, errors);
                        await transaction.RollbackAsync();
                        return new JsonResult(new { success = false, message = $"Failed to delete user: {errors}" });
                    }

                    await transaction.CommitAsync();
                    
                    // AUDIT: Log user deletion
                    await _auditTrail.LogAsync(
                        "Delete",
                        $"Deleted user account: {user.Email}",
                        "ApplicationUser",
                        userId,
                        null,
                        null,
                        $"Admin permanently deleted user account for {user.Email}"
                    );
                    
                    _logger.LogInformation($"User {user.Email} deleted successfully");
                    return new JsonResult(new { success = true, message = "User account deleted successfully." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error deleting user {UserId}", userId);
                    return new JsonResult(new { success = false, message = "An error occurred while deleting the user account." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteUser handler");
                return new JsonResult(new { success = false, message = "An error occurred while deleting the user account." });
            }
        }
        
        private async Task<SuspensionResult> HandleUserSuspension(string userId, ApplicationUser user)
        {
            try
            {
                // Get or create suspension record
                var suspension = await _context.UserSuspensions
                    .FirstOrDefaultAsync(s => s.UserId == userId);
                
                if (suspension == null)
                {
                    suspension = new UserSuspension
                    {
                        UserId = userId,
                        DenialCount = 0,
                        LastDenialDate = DateTime.UtcNow,
                        IsActive = false
                    };
                    _context.UserSuspensions.Add(suspension);
                }
                
                // Increment denial count
                suspension.DenialCount++;
                suspension.LastDenialDate = DateTime.UtcNow;
                suspension.UpdatedAt = DateTime.UtcNow;
                
                // Determine suspension based on denial count
                var result = new SuspensionResult
                {
                    DenialCount = suspension.DenialCount,
                    IsSuspended = false
                };
                
                if (suspension.DenialCount >= 3)
                {
                    suspension.IsActive = true;
                    suspension.SuspensionStartDate = DateTime.UtcNow;
                    
                    if (suspension.DenialCount == 3)
                    {
                        // 24 hours suspension
                        suspension.SuspensionEndDate = DateTime.UtcNow.AddHours(24);
                        suspension.SuspensionLevel = "24h";
                        suspension.SuspensionReason = "3 denials - 24 hour suspension";
                        result.SuspensionPeriod = "24 hours";
                    }
                    else if (suspension.DenialCount == 5)
                    {
                        // 3 days suspension
                        suspension.SuspensionEndDate = DateTime.UtcNow.AddDays(3);
                        suspension.SuspensionLevel = "3d";
                        suspension.SuspensionReason = "5 denials - 3 day suspension";
                        result.SuspensionPeriod = "3 days";
                    }
                    else if (suspension.DenialCount >= 10)
                    {
                        // 1 month suspension
                        suspension.SuspensionEndDate = DateTime.UtcNow.AddMonths(1);
                        suspension.SuspensionLevel = "1m";
                        suspension.SuspensionReason = "10+ denials - 1 month suspension";
                        result.SuspensionPeriod = "1 month";
                    }
                    
                    result.IsSuspended = true;
                    result.SuspensionEndDate = suspension.SuspensionEndDate ?? DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user suspension for user {UserId}", userId);
                return new SuspensionResult
                {
                    DenialCount = 1,
                    IsSuspended = false
                };
            }
        }
        
        private string GenerateSuspensionEmailBody(string userName, string userEmail, int denialCount, string suspensionPeriod, DateTime suspensionEndDate)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Account Suspended - Barangay Health Care</title>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
                    .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 20px; border-radius: 10px; box-shadow: 0 0 20px rgba(0,0,0,0.1); }}
                    .header {{ background: linear-gradient(135deg, #dc3545, #c82333); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; margin: -20px -20px 30px -20px; }}
                    .header h1 {{ margin: 0; font-size: 28px; font-weight: 300; }}
                    .content {{ padding: 0 20px; }}
                    .alert {{ background: #fff3cd; border: 1px solid #ffeaa7; color: #856404; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    .suspension-info {{ background: #f8d7da; border: 1px solid #f5c6cb; color: #721c24; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    .suspension-info h3 {{ margin-top: 0; color: #721c24; }}
                    .count {{ font-size: 24px; font-weight: bold; color: #dc3545; }}
                    .period {{ font-size: 20px; font-weight: bold; color: #dc3545; }}
                    .end-date {{ font-size: 18px; font-weight: bold; color: #6c757d; }}
                    .guide-section {{ background: #e7f3ff; border-left: 4px solid #007bff; padding: 20px; margin: 20px 0; }}
                    .step {{ margin: 15px 0; padding: 15px; background: white; border-radius: 8px; border-left: 3px solid #007bff; }}
                    .step-number {{ background: #007bff; color: white; width: 25px; height: 25px; border-radius: 50%; display: inline-block; text-align: center; line-height: 25px; margin-right: 10px; font-weight: bold; }}
                    .requirements {{ background: #d4edda; border: 1px solid #c3e6cb; color: #155724; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 14px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🚫 Account Suspended</h1>
                        <p>Barangay Health Care System</p>
                    </div>
                    
                    <div class='content'>
                        <p>Dear <strong>{userName}</strong>,</p>
                        
                        <div class='suspension-info'>
                            <h3>⚠️ Account Suspension Notice</h3>
                            <p>Your account has been <strong>suspended</strong> due to multiple application rejections.</p>
                            
                            <div style='text-align: center; margin: 20px 0;'>
                                <div class='count'>Denial Count: {denialCount}</div>
                                <div class='period'>Suspension Period: {suspensionPeriod}</div>
                                <div class='end-date'>Suspension Ends: {suspensionEndDate:MMMM dd, yyyy 'at' h:mm tt}</div>
                            </div>
                            
                            <p><strong>You cannot submit new applications until your suspension period ends.</strong></p>
                        </div>
                        
                        <div class='alert'>
                            <h4>📋 Why Was My Account Suspended?</h4>
                            <p>Your account was suspended because your application has been rejected <strong>{denialCount} times</strong>. This is to ensure the quality and integrity of our health care system.</p>
                            
                            <p><strong>Suspension Schedule:</strong></p>
                            <ul>
                                <li>3 rejections = 24-hour suspension</li>
                                <li>5 rejections = 3-day suspension</li>
                                <li>10+ rejections = 1-month suspension</li>
                            </ul>
                        </div>
                        
                        <div class='guide-section'>
                            <h4 style='color: #007bff; margin-top: 0;'>📋 How to Get Approved After Suspension</h4>
                            
                            <div class='step'>
                                <span class='step-number'>1</span>
                                <strong>Wait for Suspension to End</strong>
                                <p>Your suspension will automatically end on <strong>{suspensionEndDate:MMMM dd, yyyy}</strong>. You will receive an email notification when you can reapply.</p>
                            </div>
                            
                            <div class='step'>
                                <span class='step-number'>2</span>
                                <strong>Review Previous Rejections</strong>
                                <ul style='margin: 10px 0 0 35px;'>
                                    <li>Check your email for previous rejection reasons</li>
                                    <li>Identify common issues in your applications</li>
                                    <li>Prepare better documentation</li>
                                </ul>
                            </div>
                            
                            <div class='step'>
                                <span class='step-number'>3</span>
                                <strong>Prepare High-Quality Documents</strong>
                                <ul style='margin: 10px 0 0 35px;'>
                                    <li>Use a valid, non-expired government ID</li>
                                    <li>Take clear, well-lit photos of your documents</li>
                                    <li>Ensure all text is readable and not blurry</li>
                                    <li>Upload recent residency proof (within 3 months)</li>
                                </ul>
                            </div>
                            
                            <div class='step'>
                                <span class='step-number'>4</span>
                                <strong>Complete Application Carefully</strong>
                                <ul style='margin: 10px 0 0 35px;'>
                                    <li>Double-check all personal information</li>
                                    <li>Ensure contact details are accurate</li>
                                    <li>Provide complete emergency contact information</li>
                                    <li>Answer all questions honestly and completely</li>
                                </ul>
                            </div>
                            
                            <div class='step'>
                                <span class='step-number'>5</span>
                                <strong>Submit After Suspension Ends</strong>
                                <p>Once your suspension period ends, you can submit a new application. Make sure to follow all guidelines carefully to avoid further rejections.</p>
                            </div>
                        </div>
                        
                        <div class='requirements'>
                            <h5 style='color: #155724; margin-top: 0;'>📄 Required Documents Checklist</h5>
                            <ul style='margin: 10px 0;'>
                                <li>✅ Valid government-issued ID (not expired)</li>
                                <li>✅ Clear, readable photo of your ID</li>
                                <li>✅ Recent residency proof (within 3 months)</li>
                                <li>✅ Complete personal information</li>
                                <li>✅ Accurate contact details</li>
                                <li>✅ Emergency contact information</li>
                            </ul>
                        </div>
                        
                        <p><strong>Common Rejection Reasons:</strong></p>
                        <ul>
                            <li>📷 Blurry or unclear document photos</li>
                            <li>📅 Expired identification documents</li>
                            <li>🏠 Missing or outdated residency proof</li>
                            <li>✏️ Incomplete or inaccurate personal information</li>
                            <li>📞 Invalid or missing contact information</li>
                        </ul>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <p><strong>Suspension End Date:</strong> {suspensionEndDate:MMMM dd, yyyy 'at' h:mm tt}</p>
                            <p style='color: #6c757d; font-size: 14px;'>You can reapply after this date</p>
                        </div>
                        
                        <p><strong>Need Help?</strong> Our support team is here to assist you. Contact us at:</p>
                        <ul>
                            <li>📧 Email: bhcare@barangay161.ph</li>
                            <li>📞 Phone: (02) 8123-4567</li>
                            <li>🏢 Visit: Barangay 161 Health Center</li>
                            <li>⏰ Hours: Monday-Friday, 8:00 AM - 5:00 PM</li>
                        </ul>
                    </div>
                    
                    <div class='footer'>
                        <p>© 2025 - Barangay Health System | Barangay 161, Manila</p>
                        <p>This is an automated message. Please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}