using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;

namespace Barangay.Pages.Admin
{
    [Authorize(Policy = "AccessAdminDashboard")]
    public class UserVerificationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IUserVerificationService _verificationService;
        private readonly ILogger<UserVerificationModel> _logger;
        private readonly INotificationService _notificationService;
        
        public UserVerificationModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IUserVerificationService verificationService,
            INotificationService notificationService,
            ILogger<UserVerificationModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _verificationService = verificationService;
            _notificationService = notificationService;
            _logger = logger;
        }
        
        public List<ApplicationUser> PendingUsers { get; set; } = new();
        public List<UserDocument> UserDocuments { get; set; } = new();
        public int PendingUsersCount { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public bool ShowAllUsers { get; set; }
        
        public async Task<IActionResult> OnGetAsync(int page = 1)
        {
            try
            {
                CurrentPage = page < 1 ? 1 : page;
                
                // Create the base query
                var query = _userManager.Users.AsQueryable();
                
                // Apply filters
                if (!ShowAllUsers)
                {
                    query = query.Where(u => u.Status == "Pending");
                }
                
                // Apply search
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    query = query.Where(u => 
                        u.UserName.Contains(SearchTerm) || 
                        u.Email.Contains(SearchTerm) || 
                        u.FullName.Contains(SearchTerm) ||
                        u.FirstName.Contains(SearchTerm) ||
                        u.LastName.Contains(SearchTerm));
                }
                
                // Get total count for pagination
                TotalCount = await query.CountAsync();
                
                // Calculate total pages
                TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
                
                // Apply pagination
                PendingUsers = await query
                    .OrderByDescending(u => u.JoinDate)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();
                
                // Get count of pending users for badge
                PendingUsersCount = await _userManager.Users
                    .CountAsync(u => u.Status == "Pending");
                
                // Get user documents
                var userIds = PendingUsers.Select(u => u.Id).ToList();
                UserDocuments = await _context.UserDocuments
                    .Where(d => userIds.Contains(d.UserId))
                    .ToListAsync();
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user verification page");
                TempData["ErrorMessage"] = "An error occurred while loading the page. Please try again.";
                PendingUsers = new List<ApplicationUser>();
                UserDocuments = new List<UserDocument>();
                return Page();
            }
        }
        
        public async Task<IActionResult> OnPostApproveAsync(string userId, string notes)
        {
            try
            {
                // Get current admin ID
                var adminId = User.Identity.Name;
                
                // Approve user
                var result = await _verificationService.ApproveUserAsync(userId, adminId);
                
                if (result)
                {
                    // Get the user for notification
                    var user = await _userManager.FindByIdAsync(userId);
                    
                    // Create notification for user
                    if (user != null)
                    {
                        await _notificationService.CreateNotificationForUserAsync(
                            userId: userId,
                            title: "Account Approved",
                            message: "Your account has been approved.",
                            type: "Success",
                            link: "https://bhcare.software"
                        );
                    }
                    
                    TempData["SuccessMessage"] = "User has been approved successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve user. Please try again.";
                }
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user: {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while approving the user. Please try again.";
                return RedirectToPage();
            }
        }
        
        public async Task<IActionResult> OnPostRejectAsync(string userId, string reason)
        {
            try
            {
                // Get current admin ID
                var adminId = User.Identity.Name;
                
                // Reject user
                var result = await _verificationService.RejectUserAsync(userId, adminId, reason);
                
                if (result)
                {
                    // Get the user for notification
                    var user = await _userManager.FindByIdAsync(userId);
                    
                    // Create notification for user
                    if (user != null)
                    {
                        await _notificationService.CreateNotificationForUserAsync(
                            userId: userId,
                            title: "Account Rejected",
                            message: $"Your account verification was not approved. Reason: {reason}",
                            type: "Danger",
                            link: "/Account/Login"
                        );
                    }
                    
                    TempData["SuccessMessage"] = "User has been rejected.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reject user. Please try again.";
                }
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting user: {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while rejecting the user. Please try again.";
                return RedirectToPage();
            }
        }
    }
} 