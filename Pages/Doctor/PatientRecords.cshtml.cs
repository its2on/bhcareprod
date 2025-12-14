using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Barangay.Models; // Add this for Patient model
using Barangay.Data;   // Add this for ApplicationDbContext
using System.Linq;     // Add this for ToList() extension method
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using Barangay.Services;

namespace Barangay.Pages.Doctor
{
    [Authorize(Roles = "Doctor,Head Doctor")]
    public class PatientRecordsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PatientRecordsModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;

        public PatientRecordsModel(
            ApplicationDbContext context, 
            ILogger<PatientRecordsModel> logger,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        // Original all patients list
        public List<Patient> Patients { get; set; } = new List<Patient>();
        
        // Filtered and paginated patients
        public List<Patient> PaginatedPatients { get; set; } = new List<Patient>();
        
        public string? ErrorMessage { get; set; }
        
        // Search and filter properties
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;
        
        [BindProperty(SupportsGet = true)]
        public string FilterBy { get; set; } = string.Empty;
        
        [BindProperty(SupportsGet = true)]
        public string FilterValue { get; set; } = string.Empty;
        
        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalPatients { get; set; }
        
        // New patient model for the Add Patient form with enhanced validation
        [BindProperty]
        public NewPatientViewModel NewPatient { get; set; } = new NewPatientViewModel();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                // Get all patients (with or without appointments)
                var query = _context.Patients
                    .Include(p => p.Appointments)
                    .Include(p => p.User)
                    .Where(p => p.UserId != "0e03f06e-ba88-46ed-b047-4974d8b8252a" && p.FullName != "System Administrator")
                    .AsQueryable();

                // Exclude staff members (doctor, nurse, admin) from patient list
                var staffUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == _context.Roles.Where(r => r.Name == "Doctor").Select(r => r.Id).FirstOrDefault()
                        || ur.RoleId == _context.Roles.Where(r => r.Name == "Nurse").Select(r => r.Id).FirstOrDefault()
                        || ur.RoleId == _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefault()
                        || ur.RoleId == _context.Roles.Where(r => r.Name == "Admin Staff").Select(r => r.Id).FirstOrDefault())
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(p => !staffUserIds.Contains(p.UserId));
                
                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var searchTermLower = SearchTerm.ToLower();
                    query = query.Where(p => 
                        p.FullName.ToLower().Contains(searchTermLower) || 
                        p.Email.ToLower().Contains(searchTermLower) || 
                        (p.ContactNumber != null && p.ContactNumber.Contains(searchTermLower)));
                }
                
                // Apply category filter if provided
                if (!string.IsNullOrWhiteSpace(FilterBy) && !string.IsNullOrWhiteSpace(FilterValue))
                {
                    switch (FilterBy.ToLower())
                    {
                        case "gender":
                            query = query.Where(p => p.Gender.ToLower() == FilterValue.ToLower());
                            break;
                        case "status":
                            query = query.Where(p => p.Status != null && p.Status.ToLower() == FilterValue.ToLower());
                            break;
                        case "age":
                            // Parse age range
                            switch (FilterValue)
                            {
                                case "0-18":
                                    query = query.Where(p => p.BirthDate >= DateTime.Today.AddYears(-18));
                                    break;
                                case "19-30":
                                    query = query.Where(p => p.BirthDate <= DateTime.Today.AddYears(-19) && 
                                                        p.BirthDate >= DateTime.Today.AddYears(-30));
                                    break;
                                case "31-50":
                                    query = query.Where(p => p.BirthDate <= DateTime.Today.AddYears(-31) && 
                                                        p.BirthDate >= DateTime.Today.AddYears(-50));
                                    break;
                                case "51-70":
                                    query = query.Where(p => p.BirthDate <= DateTime.Today.AddYears(-51) && 
                                                        p.BirthDate >= DateTime.Today.AddYears(-70));
                                    break;
                                case "71+":
                                    query = query.Where(p => p.BirthDate <= DateTime.Today.AddYears(-71));
                                    break;
                            }
                            break;
                    }
                }
                
                // Count total matching patients for pagination
                TotalPatients = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalPatients / (double)PageSize);
                
                // Ensure current page is valid
                if (CurrentPage < 1)
                {
                    CurrentPage = 1;
                }
                else if (CurrentPage > TotalPages && TotalPages > 0)
                {
                    CurrentPage = TotalPages;
                }
                
                // Get paginated results
                PaginatedPatients = await query
                    .OrderBy(p => p.FullName)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Also include guest patients from Appointments (BookingForOther = true)
                var guestAppointmentsQuery = _context.Appointments
                    .Where(a => a.BookingForOther == true && 
                               !string.IsNullOrEmpty(a.FamilyNumber) &&
                               a.Status != AppointmentStatus.Cancelled);
                
                // Apply search filter to guest appointments if provided
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var searchTermLower = SearchTerm.ToLower();
                    guestAppointmentsQuery = guestAppointmentsQuery.Where(a =>
                        a.PatientName.ToLower().Contains(searchTermLower) ||
                        (a.ContactNumber != null && a.ContactNumber.Contains(searchTermLower)));
                }
                
                // Get all guest appointments and deduplicate
                var allGuestAppointments = await guestAppointmentsQuery.ToListAsync();
                
                // Decrypt patient names if encrypted
                foreach (var appointment in allGuestAppointments)
                {
                    if (!string.IsNullOrEmpty(appointment.PatientName) && _encryptionService.IsEncrypted(appointment.PatientName))
                    {
                        appointment.PatientName = _encryptionService.DecryptForUser(appointment.PatientName, User);
                    }
                }
                
                var guestPatients = allGuestAppointments
                    .GroupBy(a => new { a.PatientName, a.FamilyNumber })
                    .Select(g => g.OrderByDescending(a => a.AppointmentDate).First())
                    .Select(a => new Patient
                    {
                        UserId = a.Id.ToString(), // Use appointment ID as patient ID
                        FullName = a.PatientName, // Now decrypted
                        Gender = a.Gender ?? "Not specified",
                        BirthDate = a.AgeValue > 0 ? DateTime.Today.AddYears(-a.AgeValue) : DateTime.Today,
                        ContactNumber = a.ContactNumber ?? "N/A",
                        Email = "Guest Patient",
                        Status = "Guest Patient",
                        FamilyNumber = a.FamilyNumber,
                        CreatedAt = a.CreatedAt
                    })
                    .ToList();
                
                // Add guest patients to the paginated results
                PaginatedPatients.AddRange(guestPatients);

                // Decrypt patient data for authorized users
                foreach (var patient in PaginatedPatients)
                {
                    patient.DecryptSensitiveData(_encryptionService, User);
                    if (patient.User != null)
                    {
                        patient.User.DecryptSensitiveData(_encryptionService, User);
                        
                        // Manually decrypt PhoneNumber since it's not marked with [Encrypted] attribute
                        if (!string.IsNullOrEmpty(patient.User.PhoneNumber) && _encryptionService.IsEncrypted(patient.User.PhoneNumber))
                        {
                            patient.User.PhoneNumber = patient.User.PhoneNumber.DecryptForUser(_encryptionService, User);
                        }
                    }
                }
                
                // Recalculate total patients including guest patients
                TotalPatients = PaginatedPatients.Count;
                _logger.LogInformation($"Total patients (including {guestPatients.Count} guest patients): {TotalPatients}");
                
                // Log the results for debugging
                _logger.LogInformation($"Loaded {PaginatedPatients.Count} patients (page {CurrentPage} of {TotalPages})");
                _logger.LogInformation($"Search: '{SearchTerm}', Filter: {FilterBy}={FilterValue}");
                
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading patient details. Please try again.";
                _logger.LogError(ex, "Error loading patient records");
                return Page();
            }
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Starting to process new patient creation");
            
            // Manual validation for patient email format
            if (!string.IsNullOrEmpty(NewPatient.Email))
            {
                if (!IsValidEmail(NewPatient.Email))
                {
                    ModelState.AddModelError("NewPatient.Email", "Please enter a valid email address in the format name@example.com.");
                }
                
                // Check if email is already used by another patient
                var existingUser = await _userManager.FindByEmailAsync(NewPatient.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("NewPatient.Email", "This email is already associated with another account.");
                }
            }
            
            // Manual validation for contact number format
            if (!string.IsNullOrEmpty(NewPatient.ContactNumber) && !IsValidPhoneNumber(NewPatient.ContactNumber))
            {
                ModelState.AddModelError("NewPatient.ContactNumber", "Please enter a valid contact number (digits, dashes, spaces, and parentheses only).");
            }
            
            // Manual validation for birth date
            if (NewPatient.BirthDate > DateTime.Today)
            {
                ModelState.AddModelError("NewPatient.BirthDate", "Birth date cannot be in the future.");
            }
            else if (NewPatient.BirthDate < DateTime.Today.AddYears(-120))
            {
                ModelState.AddModelError("NewPatient.BirthDate", "Birth date is too far in the past (maximum age is 120 years).");
            }
            
            // Handle form validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Validation errors: {Errors}", string.Join(", ", errors));
                
                // Set specific error message based on validation failures
                if (ModelState.ContainsKey("NewPatient.Email") && ModelState["NewPatient.Email"].Errors.Count > 0)
                {
                    TempData["ErrorMessage"] = "Please enter a valid email address.";
                }
                else if (ModelState.ContainsKey("NewPatient.ContactNumber") && ModelState["NewPatient.ContactNumber"].Errors.Count > 0)
                {
                    TempData["ErrorMessage"] = "Please enter a valid contact number.";
                }
                else if (ModelState.ContainsKey("NewPatient.BirthDate") && ModelState["NewPatient.BirthDate"].Errors.Count > 0)
                {
                    TempData["ErrorMessage"] = "Please enter a valid birth date.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Please correct the validation errors below.";
                }
                
                // Reload patients for the view
                await ReloadPatientsAsync();
                
                return Page();
            }
            
            try
            {
                // Create new user for patient
                var user = new ApplicationUser
                {
                    UserName = NewPatient.Email,
                    Email = _encryptionService.Encrypt(NewPatient.Email), // Encrypt email
                    PhoneNumber = _encryptionService.Encrypt(NewPatient.ContactNumber), // Encrypt phone number
                    EmailConfirmed = true, // Auto-confirm for admin-created accounts
                    FullName = NewPatient.FullName
                };
                
                // Generate a random password
                var password = GenerateRandomPassword();
                _logger.LogInformation("Creating new user account for patient: {PatientName}, Email: {Email}", 
                    NewPatient.FullName, NewPatient.Email);
                
                var result = await _userManager.CreateAsync(user, password);
                
                if (result.Succeeded)
                {
                    // Assign Patient role
                    await _userManager.AddToRoleAsync(user, "Patient");
                    
                    _logger.LogInformation("User account created successfully, adding patient record");
                    
                    // Create patient record with encrypted data
                    var patient = new Patient
                    {
                        UserId = user.Id,
                        FullName = _encryptionService.Encrypt(NewPatient.FullName),
                        Gender = NewPatient.Gender,
                        BirthDate = NewPatient.BirthDate,
                        Address = _encryptionService.Encrypt(NewPatient.Address),
                        ContactNumber = _encryptionService.Encrypt(NewPatient.ContactNumber),
                        Email = _encryptionService.Encrypt(NewPatient.Email),
                        EmergencyContact = _encryptionService.Encrypt(NewPatient.EmergencyContact),
                        EmergencyContactNumber = _encryptionService.Encrypt(NewPatient.EmergencyContactNumber),
                        Status = NewPatient.Status,
                        Allergies = _encryptionService.Encrypt(NewPatient.Allergies),
                        BloodType = NewPatient.BloodType,
                        MedicalHistory = _encryptionService.Encrypt(NewPatient.MedicalHistory),
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    // Save to database
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Patient record created successfully with ID: {PatientId}", patient.UserId);
                    
                    // TODO: Send email with credentials to the patient
                    
                    TempData["SuccessMessage"] = $"Patient {patient.FullName} added successfully. Temporary password: {password}";
                    _logger.LogInformation($"New patient created: {patient.FullName}, ID: {patient.UserId}");
                    
                    return RedirectToPage();
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                        _logger.LogWarning("User creation error: {Error}", error.Description);
                    }
                    TempData["ErrorMessage"] = "Error creating patient account. Please check the form and try again.";
                    
                    // Reload patients for the view
                    await ReloadPatientsAsync();
                    
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new patient");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the patient.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                
                // Reload patients for the view
                await ReloadPatientsAsync();
                
                return Page();
            }
        }
        
        
        private async Task ReloadPatientsAsync()
        {
            try
            {
                var query = _context.Patients
                    .Include(p => p.Appointments)
                    .Where(p => p.UserId != "0e03f06e-ba88-46ed-b047-4974d8b8252a" && p.FullName != "System Administrator")
                    .AsQueryable();
                
                // Exclude staff members (doctor, nurse, admin) from patient list
                var staffUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == _context.Roles.Where(r => r.Name == "Doctor").Select(r => r.Id).FirstOrDefault()
                        || ur.RoleId == _context.Roles.Where(r => r.Name == "Nurse").Select(r => r.Id).FirstOrDefault()
                        || ur.RoleId == _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefault()
                        || ur.RoleId == _context.Roles.Where(r => r.Name == "Admin Staff").Select(r => r.Id).FirstOrDefault())
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(p => !staffUserIds.Contains(p.UserId));
                
                // Apply any active filters
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var searchTermLower = SearchTerm.ToLower();
                    query = query.Where(p => 
                        p.FullName.ToLower().Contains(searchTermLower) || 
                        p.Email.ToLower().Contains(searchTermLower) || 
                        (p.ContactNumber != null && p.ContactNumber.Contains(searchTermLower)));
                }
                
                TotalPatients = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalPatients / (double)PageSize);
                
                PaginatedPatients = await query
                    .OrderBy(p => p.FullName)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();
                
                // Also include guest patients from Appointments
                var guestAppointmentsQuery = _context.Appointments
                    .Where(a => a.BookingForOther == true && 
                               !string.IsNullOrEmpty(a.FamilyNumber) &&
                               a.Status != AppointmentStatus.Cancelled);
                
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var searchTermLower = SearchTerm.ToLower();
                    guestAppointmentsQuery = guestAppointmentsQuery.Where(a =>
                        a.PatientName.ToLower().Contains(searchTermLower) ||
                        (a.ContactNumber != null && a.ContactNumber.Contains(searchTermLower)));
                }
                
                var allGuestAppointments = await guestAppointmentsQuery.ToListAsync();
                
                // Decrypt patient names if encrypted
                foreach (var appointment in allGuestAppointments)
                {
                    if (!string.IsNullOrEmpty(appointment.PatientName) && _encryptionService.IsEncrypted(appointment.PatientName))
                    {
                        appointment.PatientName = _encryptionService.DecryptForUser(appointment.PatientName, User);
                    }
                }
                
                var guestPatients = allGuestAppointments
                    .GroupBy(a => new { a.PatientName, a.FamilyNumber })
                    .Select(g => g.OrderByDescending(a => a.AppointmentDate).First())
                    .Select(a => new Patient
                    {
                        UserId = a.Id.ToString(),
                        FullName = a.PatientName, // Now decrypted
                        Gender = a.Gender ?? "Not specified",
                        BirthDate = a.AgeValue > 0 ? DateTime.Today.AddYears(-a.AgeValue) : DateTime.Today,
                        ContactNumber = a.ContactNumber ?? "N/A",
                        Email = "Guest Patient",
                        Status = "Guest Patient",
                        FamilyNumber = a.FamilyNumber,
                        CreatedAt = a.CreatedAt
                    })
                    .ToList();
                
                PaginatedPatients.AddRange(guestPatients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading patients after validation error");
                PaginatedPatients = new List<Patient>();
            }
        }
        

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^[\d\s\-\(\)]+$");
        }
        
        // Generate a random password that meets ASP.NET Identity requirements
        private string GenerateRandomPassword()
        {
            // Create password with 1 uppercase, 1 lowercase, 1 digit, 1 special character, and at least 8 chars
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            const string specialChars = "!@#$%^&*()_-+=";
            var random = new Random();
            
            var password = new char[10];
            
            // Ensure at least one uppercase letter
            password[0] = chars[random.Next(0, 26)];
            
            // Ensure at least one lowercase letter
            password[1] = chars[random.Next(26, 52)];
            
            // Ensure at least one digit
            password[2] = chars[random.Next(52, 62)];
            
            // Ensure at least one special character
            password[3] = specialChars[random.Next(specialChars.Length)];
            
            // Fill the rest with random characters
            for (int i = 4; i < 10; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }
            
            // Shuffle the password
            for (int i = 0; i < password.Length; i++)
            {
                int swapIndex = random.Next(password.Length);
                char temp = password[i];
                password[i] = password[swapIndex];
                password[swapIndex] = temp;
            }
            
            return new string(password);
        }
        
        // Inline editing handler
        public async Task<JsonResult> OnPostInlineEditAsync([FromBody] InlineEditModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.PatientId))
            {
                return new JsonResult(new { success = false, message = "Invalid patient information." });
            }

            try
            {
                var patient = await _context.Patients.FindAsync(model.PatientId);
                if (patient == null)
                {
                    return new JsonResult(new { success = false, message = "Patient not found." });
                }

                bool changesDetected = false;
                
                // Update only the fields that were edited
                foreach (var update in model.Updates)
                {
                    switch (update.Key.ToLower())
                    {
                        case "gender":
                            if (patient.Gender != update.Value)
                            {
                                patient.Gender = update.Value;
                                changesDetected = true;
                            }
                            break;
                        case "status":
                            if (patient.Status != update.Value)
                            {
                                patient.Status = update.Value;
                                changesDetected = true;
                            }
                            break;
                        // Add other fields as needed
                    }
                }

                if (changesDetected)
                {
                    patient.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated patient {patient.FullName} (ID: {patient.UserId}) with changes: {string.Join(", ", model.Updates.Select(u => $"{u.Key}={u.Value}"))}");
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", model.PatientId);
                return new JsonResult(new { success = false, message = "An error occurred while updating the patient." });
            }
        }
        
        public class InlineEditModel
        {
            [JsonPropertyName("patientId")]
            public string PatientId { get; set; } = string.Empty;
            
            [JsonPropertyName("updates")]
            public Dictionary<string, string> Updates { get; set; } = new Dictionary<string, string>();
        }
        
        public class NewPatientViewModel
        {
            [Required(ErrorMessage = "Full Name is required.")]
            [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters.")]
            public string FullName { get; set; } = string.Empty;
            
            [Required(ErrorMessage = "Gender is required.")]
            public string Gender { get; set; } = string.Empty;
            
            [Required(ErrorMessage = "Birth Date is required.")]
            [DataType(DataType.Date)]
            public DateTime BirthDate { get; set; } = DateTime.Today.AddYears(-30);
            
            [Required(ErrorMessage = "Address is required.")]
            [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters.")]
            public string Address { get; set; } = string.Empty;
            
            [Required(ErrorMessage = "Contact Number is required.")]
            [StringLength(20, ErrorMessage = "Contact Number cannot be longer than 20 characters.")]
            public string ContactNumber { get; set; } = string.Empty;
            
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
            [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters.")]
            public string Email { get; set; } = string.Empty;
            
            [Required(ErrorMessage = "Emergency Contact is required.")]
            [StringLength(100, ErrorMessage = "Emergency Contact cannot be longer than 100 characters.")]
            public string EmergencyContact { get; set; } = string.Empty;
            
            [Required(ErrorMessage = "Emergency Contact Number is required.")]
            [StringLength(20, ErrorMessage = "Emergency Contact Number cannot be longer than 20 characters.")]
            public string EmergencyContactNumber { get; set; } = string.Empty;
            
            [Required(ErrorMessage = "Status is required.")]
            public string Status { get; set; } = "Active";
            
            public string? Allergies { get; set; }
            
            public string? BloodType { get; set; }
            
            public string? MedicalHistory { get; set; }
        }
    }
}