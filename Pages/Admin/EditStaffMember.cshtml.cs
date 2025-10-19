using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Barangay.Pages.Admin
{
    public class EditStaffMemberModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditStaffMemberModel> _logger;

        public EditStaffMemberModel(ApplicationDbContext context, ILogger<EditStaffMemberModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }

        [BindProperty]
        public StaffInputModel Input { get; set; }

        // Department field removed
        public List<SelectListItem> Positions { get; set; }
        public List<SelectListItem> Roles { get; set; }

        public class StaffInputModel
        {
            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Display(Name = "Middle Name")]
            public string? MiddleName { get; set; }

            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required]
            [Display(Name = "Gender")]
            public string Gender { get; set; }

            [Required]
            [Display(Name = "Date of Birth")]
            [DataType(DataType.Date)]
            public DateTime DateOfBirth { get; set; }

            [Required]
            [Display(Name = "Address")]
            public string Address { get; set; }

            [Required]
            [Display(Name = "Civil Status")]
            public string CivilStatus { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Position { get; set; }

            public string Specialization { get; set; }

            public string LicenseNumber { get; set; }

            [Required]
            [Phone]
            [Display(Name = "Contact Number")]
            public string ContactNumber { get; set; }

            [Required]
            [Display(Name = "Working Days")]
            public string WorkingDays { get; set; }

            [Required]
            [Display(Name = "Working Hours")]
            public string WorkingHours { get; set; }

            public int MaxDailyPatients { get; set; } = 20;

            public bool IsActive { get; set; } = true;

            [Required]
            public string Role { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Staff member ID is null or empty");
                return NotFound();
            }

            _logger.LogInformation($"Attempting to find staff member with ID: {id}");

            // Check if the ID is a user ID (GUID) or a staff member ID (int)
            StaffMember? staffMember = null;
            
            // First try to find by staff ID (numeric)
            if (int.TryParse(id, out int staffId))
            {
                _logger.LogInformation($"Looking up staff member by numeric ID: {staffId}");
                staffMember = await _context.StaffMembers.FindAsync(staffId);
            }
            
            // If not found and it looks like a GUID, try to find by UserId
            if (staffMember == null && Guid.TryParse(id, out _))
            {
                _logger.LogInformation($"Looking up staff member by User ID (GUID): {id}");
                
                // Get all staff members to debug
                var allStaffMembers = await _context.StaffMembers.ToListAsync();
                _logger.LogInformation($"Total staff members in database: {allStaffMembers.Count}");
                foreach (var staff in allStaffMembers)
                {
                    _logger.LogInformation($"Staff ID: {staff.Id}, User ID: {staff.UserId}, Name: {staff.Name}");
                }
                
                staffMember = await _context.StaffMembers
                    .FirstOrDefaultAsync(m => m.UserId == id);
                    
                // If still not found, try with case insensitive comparison
                if (staffMember == null)
                {
                    _logger.LogInformation($"Trying case-insensitive User ID lookup for: {id}");
                    staffMember = await _context.StaffMembers
                        .FirstOrDefaultAsync(m => m.UserId.ToLower() == id.ToLower());
                }
            }

            if (staffMember == null)
            {
                _logger.LogWarning($"Staff member not found: {id}");
                TempData["ErrorMessage"] = $"Staff member with ID {id} could not be found. The staff member may have been deleted or the ID is invalid.";
                return RedirectToPage("/Admin/StaffList");
            }

            _logger.LogInformation($"Found staff member: {staffMember.Id}, Name: {staffMember.Name}");

            // Populate the input model
            Input = new StaffInputModel
            {
                FirstName = staffMember.FirstName,
                MiddleName = staffMember.MiddleName,
                LastName = staffMember.LastName,
                Gender = staffMember.Gender,
                DateOfBirth = staffMember.DateOfBirth,
                Address = staffMember.Address,
                CivilStatus = staffMember.CivilStatus,
                Email = staffMember.Email,
                Position = staffMember.Position ?? "Staff",
                Specialization = staffMember.Specialization ?? "",
                LicenseNumber = staffMember.LicenseNumber ?? "",
                ContactNumber = staffMember.ContactNumber ?? "",
                WorkingDays = staffMember.WorkingDays ?? "Monday-Friday",
                WorkingHours = staffMember.WorkingHours ?? "9:00 AM - 5:00 PM",
                MaxDailyPatients = staffMember.MaxDailyPatients,
                IsActive = staffMember.IsActive,
                Role = staffMember.Role
            };

            // Populate dropdown lists
            PopulateDropdowns();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns();
                return Page();
            }

            // Use the same improved lookup logic as in OnGetAsync
            StaffMember? staffMember = null;
            
            // First try to find by staff ID (numeric)
            if (int.TryParse(Id, out int staffId))
            {
                staffMember = await _context.StaffMembers.FindAsync(staffId);
            }
            
            // If not found and it looks like a GUID, try to find by UserId
            if (staffMember == null && Guid.TryParse(Id, out _))
            {
                staffMember = await _context.StaffMembers
                    .FirstOrDefaultAsync(m => m.UserId == Id);
                    
                // If still not found, try with case insensitive comparison
                if (staffMember == null)
                {
                    staffMember = await _context.StaffMembers
                        .FirstOrDefaultAsync(m => m.UserId.ToLower() == Id.ToLower());
                }
            }

            if (staffMember == null)
            {
                _logger.LogWarning($"Staff member not found: {Id}");
                TempData["ErrorMessage"] = $"Staff member with ID {Id} could not be found. The staff member may have been deleted or the ID is invalid.";
                return RedirectToPage("/Admin/StaffList");
            }

            // Update staff member properties
            staffMember.FirstName = Input.FirstName;
            staffMember.MiddleName = Input.MiddleName;
            staffMember.LastName = Input.LastName;
            staffMember.Gender = Input.Gender;
            staffMember.DateOfBirth = Input.DateOfBirth;
            staffMember.Address = Input.Address;
            staffMember.CivilStatus = Input.CivilStatus;
            staffMember.Email = Input.Email;
            staffMember.Position = Input.Position;
            staffMember.Specialization = Input.Specialization;
            staffMember.LicenseNumber = Input.LicenseNumber;
            staffMember.ContactNumber = Input.ContactNumber;
            staffMember.WorkingDays = Input.WorkingDays;
            staffMember.WorkingHours = Input.WorkingHours;
            staffMember.MaxDailyPatients = Input.MaxDailyPatients;
            staffMember.IsActive = Input.IsActive;
            staffMember.Role = Input.Role;

            // Sync with DoctorAvailability if this is a doctor
            if (staffMember.Role == "Doctor")
            {
                _logger.LogInformation($"Syncing DoctorAvailability for doctor {staffMember.UserId} with working days: {staffMember.WorkingDays}");
                await SyncDoctorAvailabilityAsync(staffMember);
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Staff member updated: {staffMember.Id}");
                
                if (staffMember.Role == "Doctor")
                {
                    TempData["SuccessMessage"] = $"Staff member updated successfully! Doctor availability has been synced for weekend appointments.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Staff member updated successfully!";
                }
                return RedirectToPage("/Admin/StaffList");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating staff member: {Id}");
                ModelState.AddModelError(string.Empty, "An error occurred while updating the staff member. Please try again.");
                PopulateDropdowns();
                return Page();
            }
        }

        private void PopulateDropdowns()
        {
            // Department dropdown removed

            // Populate positions
            Positions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Doctor", Text = "Doctor" },
                new SelectListItem { Value = "Nurse", Text = "Nurse" },
                new SelectListItem { Value = "Staff", Text = "Staff" },
                new SelectListItem { Value = "Administrator", Text = "Administrator" }
            };

            // Populate roles
            Roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Doctor", Text = "Doctor" },
                new SelectListItem { Value = "Nurse", Text = "Nurse" },
                new SelectListItem { Value = "Staff", Text = "Staff" },
                new SelectListItem { Value = "Admin", Text = "Admin" },
                new SelectListItem { Value = "User", Text = "User" }
            };
        }

        private async Task SyncDoctorAvailabilityAsync(StaffMember staffMember)
        {
            try
            {
                // Find or create DoctorAvailability record
                var doctorAvailability = await _context.DoctorAvailabilities
                    .FirstOrDefaultAsync(da => da.DoctorId == staffMember.UserId);

                if (doctorAvailability == null)
                {
                    // Create new DoctorAvailability record
                    doctorAvailability = new DoctorAvailability
                    {
                        DoctorId = staffMember.UserId,
                        IsAvailable = staffMember.IsActive,
                        LastUpdated = DateTime.Now
                    };
                    _context.DoctorAvailabilities.Add(doctorAvailability);
                }
                else
                {
                    // Update existing record
                    doctorAvailability.IsAvailable = staffMember.IsActive;
                    doctorAvailability.LastUpdated = DateTime.Now;
                }

                // Parse working days from StaffMember
                var workingDays = staffMember.WorkingDays?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

                // Update day availability
                doctorAvailability.Monday = workingDays.Contains("Monday");
                doctorAvailability.Tuesday = workingDays.Contains("Tuesday");
                doctorAvailability.Wednesday = workingDays.Contains("Wednesday");
                doctorAvailability.Thursday = workingDays.Contains("Thursday");
                doctorAvailability.Friday = workingDays.Contains("Friday");
                doctorAvailability.Saturday = workingDays.Contains("Saturday");
                doctorAvailability.Sunday = workingDays.Contains("Sunday");

                // Parse working hours from StaffMember
                if (!string.IsNullOrEmpty(staffMember.WorkingHours))
                {
                    var timeMatch = System.Text.RegularExpressions.Regex.Match(
                        staffMember.WorkingHours, 
                        @"(\d{1,2}):(\d{2})\s*(AM|PM)?\s*-\s*(\d{1,2}):(\d{2})\s*(AM|PM)?", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (timeMatch.Success)
                    {
                        var startHour = int.Parse(timeMatch.Groups[1].Value);
                        var startMinute = int.Parse(timeMatch.Groups[2].Value);
                        var startPeriod = timeMatch.Groups[3].Value.ToUpper();
                        var endHour = int.Parse(timeMatch.Groups[4].Value);
                        var endMinute = int.Parse(timeMatch.Groups[5].Value);
                        var endPeriod = timeMatch.Groups[6].Value.ToUpper();

                        // Convert to 24-hour format
                        if (startPeriod == "PM" && startHour != 12) startHour += 12;
                        else if (startPeriod == "AM" && startHour == 12) startHour = 0;

                        if (endPeriod == "PM" && endHour != 12) endHour += 12;
                        else if (endPeriod == "AM" && endHour == 12) endHour = 0;

                        doctorAvailability.StartTime = new TimeSpan(startHour, startMinute, 0);
                        doctorAvailability.EndTime = new TimeSpan(endHour, endMinute, 0);
                    }
                }

                _logger.LogInformation($"Synced DoctorAvailability for doctor {staffMember.UserId}: " +
                    $"Mon={doctorAvailability.Monday}, Tue={doctorAvailability.Tuesday}, " +
                    $"Wed={doctorAvailability.Wednesday}, Thu={doctorAvailability.Thursday}, " +
                    $"Fri={doctorAvailability.Friday}, Sat={doctorAvailability.Saturday}, " +
                    $"Sun={doctorAvailability.Sunday}, Hours={doctorAvailability.StartTime}-{doctorAvailability.EndTime}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing DoctorAvailability for staff member {staffMember.Id}");
            }
        }
    }
} 