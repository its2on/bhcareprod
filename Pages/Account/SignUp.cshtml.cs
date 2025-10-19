using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Barangay.Models;
using Barangay.Services;
using Barangay.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Barangay.Pages.Account
{
    public class ValidBirthDateAttribute : ValidationAttribute, IClientModelValidator
    {
        public ValidBirthDateAttribute()
        {
            ErrorMessage = "Please enter a valid birth date (not before 1900 and not in the future).";
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime birthDate)
            {
                if (birthDate > DateTime.Today || birthDate < new DateTime(1900, 1, 1))
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.Attributes.Add("data-val", "true");
            context.Attributes.Add("data-val-validbirthdate", ErrorMessage);
            context.Attributes.Add("data-val-validbirthdate-min", new DateTime(1900, 1, 1).ToString("yyyy-MM-dd"));
            context.Attributes.Add("data-val-validbirthdate-max", DateTime.Today.ToString("yyyy-MM-dd"));
        }
    }

    public class NotADummyNumberAttribute : ValidationAttribute, IClientModelValidator
    {
        public NotADummyNumberAttribute()
        {
             ErrorMessage = "Contact number appears to be a dummy number.";
        }
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var contactNumber = value as string;
            if (!string.IsNullOrEmpty(contactNumber))
            {
                string digits = new string(contactNumber.Where(char.IsDigit).ToArray());
                if (digits.Length > 9)
                {
                    string last9 = digits.Substring(digits.Length - 9);
                    if (last9.Distinct().Count() == 1) // All same digits
                    {
                        return new ValidationResult(ErrorMessage);
                    }
                }
            }
            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            context.Attributes.Add("data-val", "true");
            context.Attributes.Add("data-val-notadummynumber", ErrorMessage);
        }
    }

    public class NotGibberishNameAttribute : ValidationAttribute, IClientModelValidator
    {
        public NotGibberishNameAttribute()
        {
            ErrorMessage = "Please enter a valid name – avoid excessive repeated characters.";
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var name = value as string;
            if (string.IsNullOrWhiteSpace(name)) { return ValidationResult.Success; } // Let [Required] handle empty.

            // Check for 5+ repeated characters (increased from 3+)
            if (Regex.IsMatch(name, @"(.)\1{4}"))
            {
                return new ValidationResult(ErrorMessage);
            }

            // Basic check for keyboard mashing
            if (Regex.IsMatch(name, @"(asdf|jkl;)", RegexOptions.IgnoreCase))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            context.Attributes.Add("data-val", "true");
            context.Attributes.Add("data-val-notgibberishname", ErrorMessage);
        }
    }

    public static class HttpRequestExtensions
    {
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }

    public class SignUpModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SignUpModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IAntiforgery _antiforgery;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IDataEncryptionService _encryptionService;

        public SignUpModel(
            UserManager<ApplicationUser> userManager,
            ILogger<SignUpModel> logger,
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IAntiforgery antiforgery,
            RoleManager<IdentityRole> roleManager,
            IDataEncryptionService encryptionService)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _environment = environment;
            _antiforgery = antiforgery;
            _roleManager = roleManager;
            _encryptionService = encryptionService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Username is required")]
            [StringLength(15, ErrorMessage = "The {0} must be between {2} and {1} characters long.", MinimumLength = 3)]
            [RegularExpression(@"^[a-zA-Z0-9!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]+$", ErrorMessage = "Username can contain letters, numbers, and special characters.")]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Email address is required")]
            [EmailAddress(ErrorMessage = "Invalid email address format")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "First name is required")]
            [Display(Name = "First Name")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
            [RegularExpression(@"^[a-zA-Z'\s-]+$", ErrorMessage = "Name can only contain letters, spaces, apostrophes, and hyphens.")]
            [NotGibberishName]
            public string? FirstName { get; set; }

            [Display(Name = "Middle Name")]
            [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters.")]
            [RegularExpression(@"^[a-zA-Z'\s-]*$", ErrorMessage = "Name can only contain letters, spaces, apostrophes, and hyphens.")]
            [NotGibberishName]
            public string? MiddleName { get; set; }

            [Required(ErrorMessage = "Last name is required")]
            [Display(Name = "Last Name")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
            [RegularExpression(@"^[a-zA-Z'\s-]+$", ErrorMessage = "Name can only contain letters, spaces, apostrophes, and hyphens.")]
            [NotGibberishName]
            public string? LastName { get; set; }

            [Display(Name = "Suffix")]
            [RegularExpression(@"^(|Jr\.|Sr\.|I{2,3}|IV|V)$", ErrorMessage = "Suffix should be Jr., Sr., III, etc., or leave blank.")]
            public string? Suffix { get; set; }

            [Required(ErrorMessage = "Contact number is required")]
            [Display(Name = "Contact Number")]
            [RegularExpression(@"^(09|\+639)\d{9}$", ErrorMessage = "Contact number must be in the format 09XXXXXXXXX or +639XXXXXXXXX")]
            [NotADummyNumber(ErrorMessage = "Contact number appears to be a dummy number.")]
            public string ContactNumber { get; set; }

            [Required(ErrorMessage = "Complete address is required")]
            [Display(Name = "Complete Address")]
            [StringLength(200, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 200 characters.")]
            public string Address { get; set; }
            
            [Required(ErrorMessage = "Age is required")]
            [Display(Name = "Age")]
            [StringLength(3, MinimumLength = 1, ErrorMessage = "Age must be between 1 and 3 digits.")]
            [RegularExpression(@"^[0-9]+$", ErrorMessage = "Age must contain only numbers.")]
            public string Age { get; set; }
            
            [Required(ErrorMessage = "Birth date is required")]
            [Display(Name = "Birth Date")]
            [StringLength(10, MinimumLength = 10, ErrorMessage = "Birth date must be in YYYY-MM-DD format.")]
            [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Birth date must be in YYYY-MM-DD format.")]
            public string BirthDate { get; set; }
            
            [Required(ErrorMessage = "Gender is required")]
            [Display(Name = "Gender")]
            [RegularExpression(@"^(Male|Female|Other)$", ErrorMessage = "Please select a valid gender.")]
            public string Gender { get; set; }

            [Required(ErrorMessage = "Barangay is required")]
            [Display(Name = "Barangay")]
            [RegularExpression(@"^(158|159|160|161)$", ErrorMessage = "Please select a valid barangay (158, 159, 160, 161).")]
            public string Barangay { get; set; }
            
            [Display(Name = "Guardian's First Name")]
            [StringLength(50, ErrorMessage = "Guardian's first name cannot exceed 50 characters.")]
            public string? GuardianFirstName { get; set; }
            
            [Display(Name = "Guardian's Last Name")]
            [StringLength(50, ErrorMessage = "Guardian's last name cannot exceed 50 characters.")]
            public string? GuardianLastName { get; set; }

            [Display(Name = "Guardian's Residency Proof")]
            public IFormFile? GuardianResidencyProof { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
                ErrorMessage = "Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "Residency proof document is required")]
            [Display(Name = "Residency Proof")]
            public Microsoft.AspNetCore.Http.IFormFile ResidencyProof { get; set; }

            [Required(ErrorMessage = "You must agree to the data privacy terms")]
            [Display(Name = "Agree to Terms")]
            [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the data privacy terms")]
            public bool AgreeToTerms { get; set; } = false;

            [Required(ErrorMessage = "You must confirm your residency")]
            [Display(Name = "Confirm Residency")]
            [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm your residency in Barangay 161")]
            public bool ConfirmResidency { get; set; } = false;
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            
            // Initialize Input model with unchecked checkboxes
            Input = new InputModel
            {
                AgreeToTerms = false,
                ConfirmResidency = false
            };
        }

        // AJAX handler to check if username exists
        public async Task<IActionResult> OnGetCheckUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new JsonResult(new { exists = false });
            }
            
            var existingUser = await _userManager.FindByNameAsync(username);
            return new JsonResult(new { exists = existingUser != null });
        }
        
        // AJAX handler to check if email exists
        public async Task<IActionResult> OnGetCheckEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new JsonResult(new { exists = false });
            }
            
            try
            {
                // Only select Email field to improve performance
                var userEmails = await _userManager.Users
                    .Select(u => u.Email)
                    .ToListAsync();
                
                var exists = userEmails.Any(encryptedEmail => 
                {
                    try
                    {
                        var decryptedEmail = _encryptionService.Decrypt(encryptedEmail);
                        return decryptedEmail?.Equals(email, StringComparison.OrdinalIgnoreCase) == true;
                    }
                    catch
                    {
                        return false;
                    }
                });
                
                return new JsonResult(new { exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence");
                return new JsonResult(new { exists = false, error = true });
            }
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            try {
                _logger.LogInformation("OnPostAsync called");
                return await ProcessRegistration(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in OnPostAsync");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return Page();
            }
        }


        private async Task EnsureRoleExistsAndAssign(ApplicationUser user, string roleName)
        {
            try
            {
                // Check if role exists, create if it doesn't
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    _logger.LogInformation($"Role {roleName} does not exist. Creating it...");
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }

                // Assign role to user
                if (!await _userManager.IsInRoleAsync(user, roleName))
                {
                    var result = await _userManager.AddToRoleAsync(user, roleName);
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning($"Failed to assign role {roleName} to user {user.Email}. Errors: {string.Join(", ", result.Errors)}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while managing role {roleName} for user {user.Email}");
                throw;
            }
        }

        private async Task<IActionResult> ProcessRegistration(string returnUrl = null)
        {
            _logger.LogInformation("ProcessRegistration started");
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid: " + string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)));
                return Page();
            }

            // Check if email is suspended (with fallback if table doesn't exist)
            try
            {
                var suspension = await _context.EmailSuspensions
                    .FirstOrDefaultAsync(s => s.Email == Input.Email && s.IsActive);

                if (suspension != null && suspension.SuspensionEndDate > DateTime.UtcNow)
                {
                    var remainingTime = suspension.SuspensionEndDate.Value - DateTime.UtcNow;
                    var timeString = remainingTime.TotalHours >= 1 
                        ? $"{remainingTime.TotalHours:F0} hours"
                        : $"{remainingTime.TotalMinutes:F0} minutes";
                    
                    ModelState.AddModelError(string.Empty, 
                        $"Email verification is suspended due to multiple failed attempts. Please try again in {timeString}.");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EmailSuspensions table not found, skipping suspension check for {Email}", Input.Email);
                // Continue without suspension check if table doesn't exist
            }

            // Calculate age using current date
            var today = DateTime.Today;
            var birthDate = DateTime.TryParse(Input.BirthDate, out var parsedBirthDate) ? parsedBirthDate : DateTime.MinValue;
            var age = today.Year - birthDate.Year;
            if (birthDate > today.AddYears(-age)) age--;
            _logger.LogInformation($"Calculated age: {age}");

            // File extension variables
            string? fileExtension = null;
            string? guardianFileExtension = null;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };

            // Validate guardian information for users under 18
            if (age < 18)
            {
                _logger.LogInformation("User is under 18, checking for guardian information");
                
                // Make guardian name required for users under 18
                if (string.IsNullOrWhiteSpace(Input.GuardianFirstName) || string.IsNullOrWhiteSpace(Input.GuardianLastName))
                {
                    _logger.LogWarning("Guardian name information missing for underage user");
                    ModelState.AddModelError(string.Empty, "Guardian first name and last name are required for users under 18.");
                    return Page();
                }
                
                // Guardian proof: accept either guardian-specific proof OR fallback to user's residency proof
                if (Input.GuardianResidencyProof != null)
                {
                    // Validate guardian proof type/size
                    guardianFileExtension = Path.GetExtension(Input.GuardianResidencyProof.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(guardianFileExtension))
                    {
                        ModelState.AddModelError(string.Empty, "Invalid guardian file type. Please upload a JPG, JPEG, PNG, or PDF file.");
                        return Page();
                    }
                    
                    if (Input.GuardianResidencyProof.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(string.Empty, "Guardian residency proof file size must be less than 5MB.");
                        return Page();
                    }
                }
                else
                {
                    // No guardian-specific file uploaded; we'll fallback to the user's residency proof later
                    _logger.LogInformation("No guardian-specific proof uploaded. Will use user's residency proof if available.");
                }
            }

            // Validate residency proof file
            if (Input.ResidencyProof != null)
            {
                fileExtension = Path.GetExtension(Input.ResidencyProof.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError(string.Empty, "Invalid file type. Please upload a JPG, JPEG, PNG, or PDF file.");
                    return Page();
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Residency proof document is required.");
                return Page();
            }

            try
            {
                _logger.LogInformation("Creating user account");
                
                // Check if username already exists
                var existingUserByUsername = await _userManager.FindByNameAsync(Input.Username);
                if (existingUserByUsername != null)
                {
                    _logger.LogWarning($"Username {Input.Username} already exists");
                    ModelState.AddModelError(string.Empty, "This username is already taken. Please choose a different username.");
                    return Page();
                }
                
                // Check if email already exists (need to check encrypted emails)
                // Only select Email field to improve performance
                var userEmails = await _userManager.Users
                    .Select(u => u.Email)
                    .ToListAsync();
                
                var emailExists = userEmails.Any(encryptedEmail => 
                {
                    try
                    {
                        var decryptedEmail = _encryptionService.Decrypt(encryptedEmail);
                        return decryptedEmail?.Equals(Input.Email, StringComparison.OrdinalIgnoreCase) == true;
                    }
                    catch
                    {
                        return false;
                    }
                });
                
                if (emailExists)
                {
                    _logger.LogWarning($"Email {Input.Email} already exists");
                    ModelState.AddModelError(string.Empty, "An account with this email address already exists. Please use a different email or try logging in.");
                    return Page();
                }
                
                var user = new ApplicationUser
                {
                    UserName = Input.Username,
                    Email = _encryptionService.Encrypt(Input.Email), // Encrypt email
                    FirstName = _encryptionService.Encrypt(Input.FirstName),
                    MiddleName = _encryptionService.Encrypt(Input.MiddleName ?? ""),
                    LastName = _encryptionService.Encrypt(Input.LastName),
                    Suffix = _encryptionService.Encrypt(Input.Suffix ?? ""),
                    PhoneNumber = _encryptionService.Encrypt(Input.ContactNumber),
                    BirthDate = DateTime.TryParse(Input.BirthDate, out var parsedBirthDateValue) ? parsedBirthDateValue : DateTime.Now.AddYears(-25),
                    CreatedAt = DateTime.UtcNow,
                    HasAgreedToTerms = Input.AgreeToTerms,
                    AgreedAt = DateTime.UtcNow,
                    Gender = _encryptionService.Encrypt(Input.Gender), // Encrypt gender
                    Name = _encryptionService.Encrypt($"{Input.FirstName} {Input.LastName}"),
                    Barangay = _encryptionService.Encrypt(!string.IsNullOrWhiteSpace(Input.Barangay) ? $"Barangay {Input.Barangay}" : string.Empty), // Encrypt barangay
                    Address = _encryptionService.Encrypt(Input.Address ?? ""),
                    Age = _encryptionService.Encrypt(Input.Age ?? "") // Encrypt age
                };
                
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} created successfully");

                    // Save residency proof document for all users
                    if (Input.ResidencyProof != null)
                    {
                        try
                        {
                            // Ensure the uploads directory exists
                            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "residency_proofs");
                            Directory.CreateDirectory(uploadsFolder);

                            // Create unique filename with user ID and timestamp
                            fileExtension = Path.GetExtension(Input.ResidencyProof.FileName).ToLowerInvariant();
                            var uniqueFileName = $"{user.Id}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            var relativePath = $"/uploads/residency_proofs/{uniqueFileName}";

                            // Save file to disk
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await Input.ResidencyProof.CopyToAsync(fileStream);
                            }

                            // Ensure UserDocuments table exists
                            try
                            {
                                // Create record in UserDocuments table
                                var userDocument = new UserDocument
                                {
                                    UserId = user.Id,
                                    FileName = Input.ResidencyProof.FileName,
                                    FilePath = relativePath,
                                    FileSize = Input.ResidencyProof.Length,
                                    ContentType = Input.ResidencyProof.ContentType,
                                    FileType = Path.GetExtension(Input.ResidencyProof.FileName).TrimStart('.').ToLower(),
                                    Status = "Pending",
                                    UploadDate = DateTime.UtcNow
                                };

                                _context.UserDocuments.Add(userDocument);
                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation($"Saved residency proof document for user {user.Id}: {uniqueFileName}");
                            }
                            catch (Exception dbEx)
                            {
                                _logger.LogError(dbEx, "Error saving to UserDocuments table");
                                
                                // Store the file path in user's ProfilePicture field as fallback
                                user.ProfilePicture = relativePath;
                                await _userManager.UpdateAsync(user);
                                _logger.LogInformation($"Saved file path to user's ProfilePicture as fallback: {relativePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error saving residency proof document for user {user.Id}");
                            // Continue registration process even if document saving fails
                        }
                    }

                    // Save guardian information and residency proof for users under 18
                    if (age < 18)
                    {
                        try
                        {
                            _logger.LogInformation("Saving guardian information for user under 18");
                            string? guardianProofPath = null;
                            byte[]? guardianProofBytes = null;
                            bool useUserProofAsGuardianProof = false;
                            
                            // Handle guardian residency proof file if provided
                            if (Input.GuardianResidencyProof != null)
                            {
                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "guardian_proofs");
                                Directory.CreateDirectory(uploadsFolder);

                                guardianFileExtension = Path.GetExtension(Input.GuardianResidencyProof.FileName).ToLowerInvariant();
                                var uniqueFileName = $"{user.Id}_guardian_{DateTime.Now:yyyyMMddHHmmss}{guardianFileExtension}";
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                guardianProofPath = $"/uploads/guardian_proofs/{uniqueFileName}";

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await Input.GuardianResidencyProof.CopyToAsync(fileStream);
                                }
                                
                                _logger.LogInformation($"Saved guardian residency proof: {guardianProofPath}");

                                // Also load bytes for DB storage (so Admin can preview without file system access)
                                try
                                {
                                    guardianProofBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                                }
                                catch (Exception readEx)
                                {
                                    _logger.LogWarning(readEx, "Failed to read guardian proof bytes from saved file path");
                                }
                            }
                            else if (Input.ResidencyProof != null)
                            {
                                // If no specific guardian proof was provided, use the user's residency proof
                                // Find the user's document that was just saved
                                var userDocument = await _context.UserDocuments
                                    .FirstOrDefaultAsync(d => d.UserId == user.Id);
                                    
                                if (userDocument != null)
                                {
                                    guardianProofPath = userDocument.FilePath;
                                    useUserProofAsGuardianProof = true;
                                    _logger.LogInformation($"Using user's residency proof as guardian proof: {guardianProofPath}");

                                    // Attempt to load bytes from the saved user document path
                                    try
                                    {
                                        var absPath = Path.Combine(_environment.WebRootPath, guardianProofPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                                        if (System.IO.File.Exists(absPath))
                                        {
                                            guardianProofBytes = await System.IO.File.ReadAllBytesAsync(absPath);
                                        }
                                    }
                                    catch (Exception readEx)
                                    {
                                        _logger.LogWarning(readEx, "Failed to read bytes for user's residency proof when using as guardian proof");
                                    }
                                }
                            }

                            // Save guardian information in the database
                            var guardianInfo = new GuardianInformation
                            {
                                UserId = user.Id,
                                GuardianFirstName = _encryptionService.Encrypt(!string.IsNullOrWhiteSpace(Input.GuardianFirstName) ? 
                                    Input.GuardianFirstName : "Guardian"),
                                GuardianLastName = _encryptionService.Encrypt(!string.IsNullOrWhiteSpace(Input.GuardianLastName) ?
                                    Input.GuardianLastName : "Information"),
                                ResidencyProofPath = guardianProofPath,
                                ResidencyProof = guardianProofBytes ?? Array.Empty<byte>(),
                                CreatedAt = DateTime.UtcNow,
                                ProofType = useUserProofAsGuardianProof ? "UserResidencyProof" : "GuardianResidencyProof",
                                ConsentStatus = "Pending"
                            };

                            await _context.GuardianInformation.AddAsync(guardianInfo);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Guardian information saved successfully");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error saving guardian information");
                            // Continue registration process even if guardian info saving fails
                        }
                    }

                    TempData["SuccessMessage"] = "Registration submitted for admin approval. You will be able to log in after your account is approved.";
                    _logger.LogInformation("Registration completed successfully, redirecting to login");
                    return RedirectToPage("/Account/Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    _logger.LogWarning($"User creation error: {error.Code} - {error.Description}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
            }

            _logger.LogWarning("Registration failed, returning to page");
            return Page();
        }
    }
}
