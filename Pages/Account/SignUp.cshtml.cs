using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Barangay.Models;
using Barangay.Services;
using Barangay.Data;
using Barangay.Helpers;
using static Barangay.Services.AzureVisionOcrService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
using System.Net.Http;

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
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LocalOcrService _ocrService;
        private readonly IEmailService _emailService;
        private readonly AzureVisionOcrService _azureVisionOcrService;

        public SignUpModel(
            UserManager<ApplicationUser> userManager,
            ILogger<SignUpModel> logger,
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IAntiforgery antiforgery,
            RoleManager<IdentityRole> roleManager,
            IDataEncryptionService encryptionService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            LocalOcrService ocrService,
            IEmailService emailService,
            AzureVisionOcrService azureVisionOcrService)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _environment = environment;
            _antiforgery = antiforgery;
            _roleManager = roleManager;
            _encryptionService = encryptionService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _ocrService = ocrService;
            _emailService = emailService;
            _azureVisionOcrService = azureVisionOcrService;
            GovtIdTypes = GovtIdTypes;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public List<SelectListItem> GovtIdTypes { get; set; } = new()
        {
            new SelectListItem("Philippine National ID", "PhilSys"),
            new SelectListItem("Driver's License", "DriversLicense"),
            new SelectListItem("UMID", "UMID"),
            new SelectListItem("TIN ID", "TIN"),
            new SelectListItem("Postal ID", "PostalID"),
            new SelectListItem("PhilHealth ID", "PhilHealth"),
            new SelectListItem("SSS ID", "SSS"),
            new SelectListItem("Voter's / COMELEC ID", "Voter"),
            new SelectListItem("Passport", "Passport")
        };

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            // Username is auto-generated from email, not required from user
            [StringLength(15, ErrorMessage = "The {0} must be between {2} and {1} characters long.", MinimumLength = 3)]
            [RegularExpression(@"^[a-zA-Z0-9!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]+$", ErrorMessage = "Username can contain letters, numbers, and special characters.")]
            [Display(Name = "Username")]
            public string? Username { get; set; }

            [Required(ErrorMessage = "Email address is required")]
            [EmailAddress(ErrorMessage = "Invalid email address format")]
            [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "First name is required")]
            [Display(Name = "First Name")]
            [StringLength(50, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 50 characters.")]
            [RegularExpression(@"^(?!(?:.*([1-9]).*\1))[A-Za-z'.\-\s1-9]{1,50}$", ErrorMessage = "First name can only contain letters, spaces, apostrophes, hyphens, periods, and at most one of each digit 1-9.")]
            [NotGibberishName]
            public string? FirstName { get; set; }

            [Display(Name = "Middle Name")]
            [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters.")]
            [RegularExpression(@"^(?!(?:.*([1-9]).*\1))[A-Za-z'.\-\s1-9]{0,50}$", ErrorMessage = "Middle name can only contain letters, spaces, apostrophes, hyphens, periods, and at most one of each digit 1-9.")]
            [NotGibberishName]
            public string? MiddleName { get; set; }

            [Required(ErrorMessage = "Last name is required")]
            [Display(Name = "Last Name")]
            [StringLength(50, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 50 characters.")]
            [RegularExpression(@"^(?!(?:.*([1-9]).*\1))[A-Za-z'.\-\s1-9]{1,50}$", ErrorMessage = "Last name can only contain letters, spaces, apostrophes, hyphens, periods, and at most one of each digit 1-9.")]
            [NotGibberishName]
            public string? LastName { get; set; }

            [Display(Name = "Suffix")]
            [StringLength(10, ErrorMessage = "Suffix cannot exceed 10 characters.")]
            [RegularExpression(@"^(?:|(?:Jr|Sr)\.?|[IVXLCDMivxlcdm]{1,4})$", ErrorMessage = "Suffix should be Jr, Sr, or a Roman numeral (II, III, IV, etc.).")]
            public string? Suffix { get; set; }

            [Required(ErrorMessage = "Contact number is required")]
            [Display(Name = "Contact Number")]
            [RegularExpression(@"^(?:09\d{9}|\+63\d{9,12})$", ErrorMessage = "Contact number must be 11 digits starting with 09 or 12-15 digits starting with +63.")]
            [NotADummyNumber(ErrorMessage = "Contact number appears to be a dummy number.")]
            public string ContactNumber { get; set; }

            [Required(ErrorMessage = "Complete address is required")]
            [Display(Name = "Complete Address")]
            [StringLength(200, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 200 characters.")]
            public string Address { get; set; }
            
            [Required(ErrorMessage = "Birth date is required")]
            [Display(Name = "Birth Date")]
            [StringLength(10, MinimumLength = 10, ErrorMessage = "Birth date must be in YYYY-MM-DD format.")]
            [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Birth date must be in YYYY-MM-DD format.")]
            public string BirthDate { get; set; }
            
            [Required(ErrorMessage = "Gender is required")]
            [Display(Name = "Gender")]
            [RegularExpression(@"^(Male|Female|Other)$", ErrorMessage = "Please select a valid gender.")]
            public string Gender { get; set; }
            [Required(ErrorMessage = "Select Government ID Type")]
            public string GovernmentIdType { get; set; }

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

            [Display(Name = "Residency Proof")]
            public Microsoft.AspNetCore.Http.IFormFile? ResidencyProof { get; set; }

            [Required(ErrorMessage = "You must agree to the data privacy terms")]
            [Display(Name = "Agree to Terms")]
            [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the data privacy terms")]
            public bool AgreeToTerms { get; set; } = false;

            [Required(ErrorMessage = "You must confirm your residency")]
            [Display(Name = "Confirm Residency")]
            [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm your residency in Barangay 161")]
            public bool ConfirmResidency { get; set; } = false;
            
            // Hidden fields for OCR-detected data (populated by client-side JavaScript)
            public string? OcrDetectedBarangay { get; set; }
            public string? OcrExtractedAddress { get; set; }
            public string? OcrExtractedText { get; set; }
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
        
        // AJAX handler to check if contact number exists
        public async Task<IActionResult> OnGetCheckContactNumberAsync(string contactNumber)
        {
            if (string.IsNullOrWhiteSpace(contactNumber))
            {
                return new JsonResult(new { exists = false });
            }
            
            try
            {
                // Normalize the input contact number (remove non-digits)
                var normalizedInput = new string(contactNumber.Where(char.IsDigit).ToArray());
                
                // Get all phone numbers from database
                var userPhoneNumbers = await _userManager.Users
                    .Select(u => u.PhoneNumber)
                    .ToListAsync();
                
                var exists = userPhoneNumbers.Any(encryptedPhone => 
                {
                    try
                    {
                        var decryptedPhone = _encryptionService.Decrypt(encryptedPhone);
                        if (string.IsNullOrEmpty(decryptedPhone)) return false;
                        
                        // Normalize the decrypted phone number
                        var normalizedDecrypted = new string(decryptedPhone.Where(char.IsDigit).ToArray());
                        
                        // Compare normalized numbers
                        return normalizedDecrypted.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase);
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
                _logger.LogError(ex, "Error checking contact number existence");
                return new JsonResult(new { exists = false, error = true });
            }
        }
        
        /// <summary>
        /// AJAX handler for automatic residency verification using Azure OCR
        /// </summary>
        public async Task<IActionResult> OnPostVerifyResidencyAsync()
        {
            try
            {
                _logger.LogInformation("=== AUTOMATIC RESIDENCY VERIFICATION START ===");

                var file = Request.Form.Files.GetFile("residencyProof");
                
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file uploaded for residency verification");
                    return new JsonResult(new 
                    { 
                        success = false, 
                        message = "Please select a document to upload." 
                    });
                }

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    _logger.LogWarning("Invalid file type: {FileType}", fileExtension);
                    return new JsonResult(new 
                    { 
                        success = false, 
                        message = "Invalid file type. Please upload PDF, PNG, JPG, or JPEG." 
                    });
                }

                // Validate file size (5MB max)
                if (file.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("File too large: {Size} bytes", file.Length);
                    return new JsonResult(new 
                    { 
                        success = false, 
                        message = "File size exceeds 5MB limit." 
                    });
                }

                _logger.LogInformation("Processing file: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

                // CRITICAL: Reject screenshots and non-ID documents by filename
                var fileNameUpper = file.FileName.ToUpperInvariant();
                var screenshotIndicators = new[] { "SCREENSHOT", "SCREEN_SHOT", "SCRN", "CAPTURE", "SNAP", "IMG_", "PHOTO_" };
                var isScreenshot = screenshotIndicators.Any(indicator => fileNameUpper.Contains(indicator));
                
                if (isScreenshot)
                {
                    _logger.LogWarning("❌ REJECTED: File appears to be a screenshot: {FileName}", file.FileName);
                    return new JsonResult(new 
                    { 
                        success = false, 
                        message = "Screenshots are not accepted. Please upload an actual Philippine ID document (Driver's License, National ID, PhilHealth ID, Postal ID, etc.). The document must be a clear photo or scan of the original ID card." 
                    });
                }

                // Perform OCR analysis with fallback logic
                OcrResult ocrResult = null;
                
                // Try Local OCR first (Tesseract)
                try
                {
                    using (var stream = file.OpenReadStream())
                    {
                        ocrResult = await _ocrService.AnalyzeResidencyDocumentAsync(stream, file.FileName);
                    }
                    
                    // Check if Local OCR failed due to native library issues
                    bool isNativeLibraryError = !ocrResult.Success && 
                        (ocrResult.Message?.Contains("Tesseract OCR native libraries") == true ||
                         ocrResult.Message?.Contains("libleptonica") == true ||
                         ocrResult.Message?.Contains("DllNotFoundException") == true);
                    
                    if (isNativeLibraryError)
                    {
                        _logger.LogWarning("Local OCR unavailable (native libraries not installed). Falling back to Azure Vision OCR.");
                        ocrResult = null; // Reset to try Azure OCR
                    }
                    else if (ocrResult.Success)
                    {
                        _logger.LogInformation("Local OCR succeeded. Barangay: {Barangay}", ocrResult.BarangayNumber);
                    }
                }
                catch (Exception ex)
                {
                    // Check if this is a native library issue
                    bool isNativeLibraryError = ex.Message.Contains("libleptonica") || 
                                                ex.Message.Contains("DllNotFoundException") ||
                                                ex.InnerException?.Message?.Contains("libleptonica") == true;
                    
                    if (isNativeLibraryError)
                    {
                        _logger.LogWarning(ex, "Local OCR unavailable (native libraries not installed). Falling back to Azure Vision OCR.");
                    }
                    else
                    {
                        _logger.LogWarning(ex, "Local OCR failed. Will try Azure Vision OCR as fallback.");
                    }
                }
                
                // Fallback to Azure Vision OCR if Local OCR failed or is unavailable
                if (ocrResult == null || !ocrResult.Success)
                {
                    try
                    {
                        _logger.LogInformation("Attempting Azure Vision OCR as fallback...");
                        using (var stream = file.OpenReadStream())
                        {
                            var azureResult = await _azureVisionOcrService.AnalyzeIdImageAsync(stream, file.FileName, usePreprocessing: true);
                            
                            if (azureResult != null && !string.IsNullOrEmpty(azureResult.ExtractedText))
                            {
                                // Convert Azure Vision result to OcrResult format
                                var validBarangaysList = new[] { "158", "159", "160", "161" };
                                var barangayNumber = azureResult.BarangayNumber?.Trim() ?? "";
                                bool isBarangayValid = !string.IsNullOrWhiteSpace(barangayNumber) && 
                                                      validBarangaysList.Contains(barangayNumber);
                                
                                // REJECT if Azure Vision already determined it's invalid (Success = false)
                                // This happens when barangay is not in the valid list
                                if (!azureResult.Success)
                                {
                                    _logger.LogWarning("Azure Vision OCR rejected ID: {Message}", azureResult.Message);
                                    ocrResult = new OcrResult
                                    {
                                        Success = false,
                                        BarangayNumber = barangayNumber,
                                        Message = azureResult.Message, // Use the rejection message from Azure Vision
                                        ExtractedText = azureResult.ExtractedText
                                    };
                                }
                                else
                                {
                                    ocrResult = new OcrResult
                                    {
                                        // Set Success to true if barangay is valid, false otherwise
                                        Success = isBarangayValid,
                                        BarangayNumber = barangayNumber,
                                        Message = isBarangayValid 
                                            ? $"Residency verified in Barangay {barangayNumber}"
                                            : !string.IsNullOrWhiteSpace(barangayNumber)
                                                ? $"The document shows Barangay {barangayNumber}, which is not eligible for automatic verification. Only Barangay 158, 159, 160, or 161 are eligible. Please upload a valid ID showing one of these barangays."
                                                : "Unable to verify residency. No valid Barangay number (158, 159, 160, or 161) found in the document. Please upload a valid ID showing Barangay 158, 159, 160, or 161.",
                                        ExtractedText = azureResult.ExtractedText
                                    };
                                }
                                
                                _logger.LogInformation("Azure Vision OCR completed. Barangay: {Barangay}, Success: {Success}, Valid: {Valid}", 
                                    barangayNumber, ocrResult.Success, isBarangayValid);
                            }
                            else
                            {
                                _logger.LogWarning("Azure Vision OCR did not extract any text");
                                if (ocrResult == null)
                                {
                                    ocrResult = new OcrResult
                                    {
                                        Success = false,
                                        Message = "Unable to extract text from the document. Please ensure the image is clear and readable."
                                    };
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Azure Vision OCR also failed");
                        if (ocrResult == null)
                        {
                            ocrResult = new OcrResult
                            {
                                Success = false,
                                Message = "OCR processing failed. Please try again or contact support."
                            };
                        }
                    }
                }

                // CRITICAL VALIDATION: Only accept barangays 158, 159, 160, or 161
                var validBarangays = new[] { "158", "159", "160", "161" };
                
                // Safety check: ensure ocrResult is not null
                if (ocrResult == null)
                {
                    _logger.LogError("Both Local OCR and Azure Vision OCR failed. No OCR result available.");
                    return new JsonResult(new 
                    { 
                        success = false, 
                        message = "OCR processing failed. Please try again or contact support." 
                    });
                }
                
                // Check if a barangay number was detected (even if not eligible)
                if (!string.IsNullOrWhiteSpace(ocrResult.BarangayNumber))
                {
                    var detectedBarangay = ocrResult.BarangayNumber.Trim();
                    
                    // Double-check that the detected barangay is EXACTLY in the eligible list
                    if (validBarangays.Contains(detectedBarangay) && ocrResult.Success)
                    {
                        _logger.LogInformation("=== OCR VERIFICATION SUCCESS ===");
                        _logger.LogInformation("Barangay: {Barangay} (VALIDATED)", detectedBarangay);
                        
                        return new JsonResult(new 
                        { 
                            success = true, 
                            barangay = detectedBarangay,
                            message = ocrResult.Message,
                            autoApproved = true
                        });
                    }
                    else
                    {
                        // Non-eligible barangay detected (e.g., 168, 162, etc.)
                        _logger.LogError("❌ REJECTED: Detected barangay {Barangay} is NOT in eligible list (158-161 only)", detectedBarangay);
                        _logger.LogError("OCR extracted text preview: {Text}", 
                            ocrResult.ExtractedText?.Length > 500 
                                ? ocrResult.ExtractedText.Substring(0, 500) + "..." 
                                : ocrResult.ExtractedText ?? "(null)");
                        
                        return new JsonResult(new 
                        { 
                            success = false, 
                            message = ocrResult.Message ?? $"The document shows Barangay {detectedBarangay}, which is not eligible for automatic verification. Only Barangay 158, 159, 160, or 161 are eligible. Your account will require manual review by an administrator.",
                            autoApproved = false,
                            detectedBarangay = detectedBarangay // Include for debugging
                        });
                    }
                }
                else
                {
                    // No barangay detected or OCR failed
                    _logger.LogWarning("=== OCR VERIFICATION FAILED ===");
                    _logger.LogWarning("Reason: {Message}", ocrResult.Message);
                    
                    return new JsonResult(new 
                    { 
                        success = false, 
                        message = ocrResult.Message,
                        autoApproved = false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automatic residency verification");
                return new JsonResult(new 
                { 
                    success = false, 
                    message = "An error occurred during verification. Please try again or contact support." 
                });
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
                _logger.LogError("=== MODEL STATE VALIDATION FAILED ===");
                
                // Log each validation error with field name
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        _logger.LogError("Field: {FieldName}, Error: {ErrorMessage}", 
                            modelState.Key, 
                            error.ErrorMessage);
                    }
                }
                
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                
                _logger.LogWarning("All errors: " + errors);
                
                // Add user-friendly error message
                TempData["ErrorMessage"] = "Please correct the highlighted errors before submitting.";
                TempData["ValidationErrors"] = errors;
                
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

            // Check if auto-approval is eligible via OCR-detected barangay (before requiring file)
            bool isAutoApprovalEligible = false;
            var validBarangays = new[] { "158", "159", "160", "161" };
            
            _logger.LogInformation("=== CHECKING AUTO-APPROVAL ELIGIBILITY ===");
            _logger.LogInformation("OcrDetectedBarangay: {OcrBarangay}", Input.OcrDetectedBarangay ?? "(null)");
            _logger.LogInformation("User Selected Barangay: {UserBarangay}", Input.Barangay ?? "(null)");
            
            if (!string.IsNullOrWhiteSpace(Input.OcrDetectedBarangay))
            {
                var detectedBarangay = Input.OcrDetectedBarangay.Trim();
                _logger.LogInformation("Checking OCR-detected barangay: {Barangay}", detectedBarangay);
                if (validBarangays.Contains(detectedBarangay))
                {
                    isAutoApprovalEligible = true;
                    _logger.LogInformation("✅ Auto-approval eligible: OCR-detected Barangay {Barangay}", detectedBarangay);
                }
                else
                {
                    _logger.LogInformation("❌ OCR-detected barangay {Barangay} is not in eligible list", detectedBarangay);
                }
            }
            
            // Also check user's selected barangay field as fallback
            if (!isAutoApprovalEligible && !string.IsNullOrWhiteSpace(Input.Barangay))
            {
                var userBarangay = Input.Barangay.Trim();
                _logger.LogInformation("Checking user selected barangay: {Barangay}", userBarangay);
                if (validBarangays.Contains(userBarangay))
                {
                    isAutoApprovalEligible = true;
                    _logger.LogInformation("✅ Auto-approval eligible: User selected Barangay {Barangay}", userBarangay);
                }
                else
                {
                    _logger.LogInformation("❌ User selected barangay {Barangay} is not in eligible list", userBarangay);
                }
            }
            
            _logger.LogInformation("Final auto-approval eligibility: {IsEligible}", isAutoApprovalEligible);
            
            // Validate residency proof file (only required if NOT auto-approval eligible)
            if (Input.ResidencyProof != null)
            {
                fileExtension = Path.GetExtension(Input.ResidencyProof.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError(string.Empty, "Invalid file type. Please upload a JPG, JPEG, PNG, or PDF file.");
                    return Page();
                }
            }
            else if (!isAutoApprovalEligible)
            {
                // Only require file if not auto-approved via OCR/barangay detection
                _logger.LogWarning("❌ Residency proof document is required - user is not auto-approval eligible");
                ModelState.AddModelError(string.Empty, "Residency proof document is required.");
                return Page();
            }
            else
            {
                _logger.LogInformation("✅ Residency proof file not required - user is auto-approval eligible");
            }

            try
            {
                _logger.LogInformation("Creating user account");
                
                // Auto-generate username from email if not provided
                string generatedUsername;
                if (string.IsNullOrWhiteSpace(Input.Username))
                {
                    // Extract username from email (part before @)
                    var emailPrefix = Input.Email.Split('@')[0];
                    // Remove any special characters
                    generatedUsername = new string(emailPrefix.Where(c => char.IsLetterOrDigit(c)).ToArray());
                    // Ensure it's not too long
                    if (generatedUsername.Length > 15)
                    {
                        generatedUsername = generatedUsername.Substring(0, 15);
                    }
                    
                    // Check if username exists, if so add random suffix
                    var baseUsername = generatedUsername;
                    int suffix = 1;
                    while (await _userManager.FindByNameAsync(generatedUsername) != null)
                    {
                        generatedUsername = $"{baseUsername}{suffix}";
                        suffix++;
                        // If base username is too long, truncate it to make room for suffix
                        if (generatedUsername.Length > 15)
                        {
                            baseUsername = baseUsername.Substring(0, 15 - suffix.ToString().Length);
                            generatedUsername = $"{baseUsername}{suffix}";
                        }
                    }
                    
                    _logger.LogInformation("Auto-generated username: {Username} from email: {Email}", generatedUsername, Input.Email);
                }
                else
                {
                    generatedUsername = Input.Username;
                    
                    // Check if username already exists
                    var existingUserByUsername = await _userManager.FindByNameAsync(generatedUsername);
                    if (existingUserByUsername != null)
                    {
                        _logger.LogWarning($"Username {generatedUsername} already exists");
                        ModelState.AddModelError(string.Empty, "This username is already taken. Please choose a different username.");
                        return Page();
                    }
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
                    UserName = generatedUsername,
                    Email = _encryptionService.Encrypt(Input.Email), // Encrypt email
                    FirstName = _encryptionService.Encrypt(Input.FirstName),
                    MiddleName = _encryptionService.Encrypt(Input.MiddleName ?? ""),
                    LastName = _encryptionService.Encrypt(Input.LastName),
                    Suffix = _encryptionService.Encrypt(Input.Suffix ?? ""),
                    PhoneNumber = _encryptionService.Encrypt(Input.ContactNumber),
                    BirthDate = DateTime.TryParse(Input.BirthDate, out var parsedBirthDateValue) ? parsedBirthDateValue : DateTime.Now.AddYears(-25),
                    CreatedAt = DateTimeHelper.ToUtc(DateTimeHelper.Now),
                    HasAgreedToTerms = Input.AgreeToTerms,
                    AgreedAt = DateTimeHelper.ToUtc(DateTimeHelper.Now),
                    Gender = _encryptionService.Encrypt(Input.Gender), // Encrypt gender
                    Name = _encryptionService.Encrypt($"{Input.FirstName} {Input.LastName}"),
                    Barangay = _encryptionService.Encrypt(!string.IsNullOrWhiteSpace(Input.Barangay) ? $"Barangay {Input.Barangay}" : string.Empty), // Encrypt barangay
                    Address = _encryptionService.Encrypt(Input.Address ?? ""),
                    Age = _encryptionService.Encrypt(age.ToString()), // Automatically calculate and encrypt age from birth date
                    
                    // Initialize status fields (will be updated by auto-approval if eligible)
                    Status = "Pending",
                    EncryptedStatus = "Pending", // Set both Status fields for login check
                    IsActive = false,
                    IsApproved = false,
                    VerificationStatus = "Pending Review"
                };
                
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} created successfully");

                    // Assign Patient role to the new user
                    try
                    {
                        _logger.LogInformation("Assigning Patient role to user");
                        
                        // Ensure Patient role exists
                        if (!await _roleManager.RoleExistsAsync("Patient"))
                        {
                            _logger.LogInformation("Patient role does not exist. Creating it...");
                            await _roleManager.CreateAsync(new IdentityRole("Patient"));
                        }
                        
                        // Assign role to user
                        var roleResult = await _userManager.AddToRoleAsync(user, "Patient");
                        if (roleResult.Succeeded)
                        {
                            _logger.LogInformation("Successfully assigned Patient role to user {Email}", user.Email);
                        }
                        else
                        {
                            _logger.LogError("Failed to assign Patient role. Errors: {Errors}", 
                                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        }
                    }
                    catch (Exception roleEx)
                    {
                        _logger.LogError(roleEx, "Error assigning Patient role to user");
                        // Don't fail registration if role assignment fails
                    }

                    // PRIORITY 1: Check OCR-detected barangay from ID scan FIRST (highest priority)
                    string detectedBarangay = null;
                    string approvalSource = null;
                    bool autoApproved = false;
                    var validBarangaysCheck = new[] { "158", "159", "160", "161" };
                    
                    _logger.LogInformation("=== CHECKING OCR-DETECTED BARANGAY (PRIORITY 1) ===");
                    if (!string.IsNullOrWhiteSpace(Input.OcrDetectedBarangay))
                    {
                        detectedBarangay = Input.OcrDetectedBarangay.Trim();
                        _logger.LogInformation("OCR-detected Barangay from frontend: {Barangay}", detectedBarangay);
                        
                        // CRITICAL VALIDATION: Validate that OCR-detected barangay is EXACTLY in eligible list
                        if (validBarangaysCheck.Contains(detectedBarangay))
                        {
                            autoApproved = true;
                            approvalSource = "System (Barangay Match via OCR)";
                            _logger.LogInformation("=== AUTO-APPROVAL FROM OCR BARANGAY DETECTION ===");
                            _logger.LogInformation("Barangay {Barangay} detected - auto-approving user ID {UserId}", detectedBarangay, user.Id);
                        }
                        else
                        {
                            _logger.LogError("❌ REJECTED: OCR-detected barangay {Barangay} is NOT in eligible list (158-161 only)", detectedBarangay);
                            _logger.LogError("OCR extracted text: {OcrText}", Input.OcrExtractedText ?? "(null)");
                            _logger.LogError("OCR extracted address: {OcrAddress}", Input.OcrExtractedAddress ?? "(null)");
                            detectedBarangay = null; // Reset if not eligible
                            autoApproved = false; // Ensure not approved
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No OCR-detected barangay provided in form submission");
                    }
                    
                    // PRIORITY 2: Fall back to user's Barangay field from profile if OCR didn't detect
                    if (!autoApproved)
                    {
                        string userBarangay = Input.Barangay?.Trim();
                        _logger.LogInformation("=== CHECKING USER BARANGAY FIELD (PRIORITY 2) ===");
                        _logger.LogInformation("User selected Barangay: {Barangay}", userBarangay);
                        
                        // Check if user is from eligible barangay (158, 159, 160, 161)
                        if (!string.IsNullOrEmpty(userBarangay))
                        {
                            if (userBarangay == "158" || userBarangay.Contains("158"))
                                detectedBarangay = "158";
                            else if (userBarangay == "159" || userBarangay.Contains("159"))
                                detectedBarangay = "159";
                            else if (userBarangay == "160" || userBarangay.Contains("160"))
                                detectedBarangay = "160";
                            else if (userBarangay == "161" || userBarangay.Contains("161"))
                                detectedBarangay = "161";
                            
                            if (detectedBarangay != null)
                            {
                                autoApproved = true;
                                approvalSource = "System (Profile Auto-Verified)";
                                _logger.LogInformation("=== AUTO-APPROVAL FROM PROFILE BARANGAY ===");
                            }
                        }
                    }
                    
                    // Save residency proof document if provided (optional for auto-approved users)
                    UserDocument userDocument = null;
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
                                userDocument = new UserDocument
                                {
                                    UserId = user.Id,
                                    FileName = Input.ResidencyProof.FileName,
                                    FilePath = relativePath,
                                    FileSize = Input.ResidencyProof.Length,
                                    ContentType = Input.ResidencyProof.ContentType,
                                    FileType = Path.GetExtension(Input.ResidencyProof.FileName).TrimStart('.').ToLower(),
                                    Status = autoApproved ? "Verified" : "Pending",
                                    UploadDate = DateTime.UtcNow
                                };

                                _context.UserDocuments.Add(userDocument);
                                await _context.SaveChangesAsync();
                                
                                _logger.LogInformation($"Saved residency proof document for user {user.Id}: {uniqueFileName}");
                            }
                            catch (Exception docEx)
                            {
                                _logger.LogError(docEx, "Error saving user document to database");
                            }
                        }
                        catch (Exception fileEx)
                        {
                            _logger.LogError(fileEx, "Error saving residency proof file");
                        }
                    }
                    
                    // AUTO-APPROVE if eligible barangay detected (from OCR or profile)
                    if (autoApproved && detectedBarangay != null)
                    {
                        _logger.LogInformation("Detected Barangay: {Barangay}, Source: {Source}", detectedBarangay, approvalSource);
                        
                        user.VerificationStatus = "Auto Verified";
                        user.IsApproved = true;
                        user.ApprovedBy = approvalSource;
                        user.ApprovedDate = DateTime.UtcNow;
                        user.VerifiedBarangay = detectedBarangay;
                        
                        // Store OCR-extracted data if available
                        if (!string.IsNullOrWhiteSpace(Input.OcrExtractedText))
                        {
                            user.OcrExtractedText = Input.OcrExtractedText;
                            _logger.LogInformation("Stored OCR-extracted text for audit trail");
                        }
                        else if (!string.IsNullOrWhiteSpace(Input.OcrExtractedAddress))
                        {
                            user.OcrExtractedText = $"Auto-verified from {approvalSource}: Address contains Barangay {detectedBarangay}. Extracted address: {Input.OcrExtractedAddress}";
                        }
                        else
                        {
                            user.OcrExtractedText = $"Auto-verified from {approvalSource}: Barangay {detectedBarangay}";
                        }
                        
                        user.DocumentVerifiedAt = DateTime.UtcNow;
                        user.Status = "Verified";
                        user.EncryptedStatus = "Verified"; // IMPORTANT: Update both Status and EncryptedStatus for login check
                        user.IsActive = true;
                        
                        // Update document status if file was uploaded
                        if (userDocument != null)
                        {
                            userDocument.Status = "Verified";
                            userDocument.ApprovedBy = null; // NULL for system auto-approval (no specific admin user)
                            userDocument.ApprovedAt = DateTime.UtcNow;
                        }
                        
                        _logger.LogInformation("=== SAVING AUTO-APPROVAL TO DATABASE ===");
                        _logger.LogInformation("Before save - User ID: {UserId}, IsApproved: {IsApproved}, Status: {Status}, VerificationStatus: {VerificationStatus}", 
                            user.Id, user.IsApproved, user.Status, user.VerificationStatus);
                        
                        var updateResult = await _userManager.UpdateAsync(user);
                        if (!updateResult.Succeeded)
                        {
                            _logger.LogError("Failed to update user: {Errors}", string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                        }
                        
                        await _context.SaveChangesAsync();
                        
                        // Verify the save by reloading from database
                        var savedUser = await _userManager.FindByIdAsync(user.Id);
                        _logger.LogInformation("=== USER AUTO-APPROVED ===");
                        _logger.LogInformation("After save - User ID: {UserId}, IsApproved: {IsApproved}, Status: {Status}, VerificationStatus: {VerificationStatus}", 
                            savedUser.Id, savedUser.IsApproved, savedUser.Status, savedUser.VerificationStatus);
                        _logger.LogInformation("Detected Barangay: {Barangay}, Source: {Source}", detectedBarangay, approvalSource);
                        
                        // Send approval email to user
                        try
                        {
                            var userEmail = _encryptionService.Decrypt(user.Email);
                            var firstName = _encryptionService.Decrypt(user.FirstName);
                            
                            var emailBody = $@"
                                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                    <h2 style='color: #4CAF50;'>✅ BHCare Account Approved</h2>
                                    <p>Hi <strong>{firstName}</strong>,</p>
                                    <p>Great news! Your residency in <strong>Barangay {detectedBarangay}</strong> has been automatically verified.</p>
                                    <p>Your BHCare account is now <strong>active</strong>. You can log in anytime at:</p>
                                    <p><a href='{Request.Scheme}://{Request.Host}/Account/Login' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Login Now</a></p>
                                    <p>Thank you for registering with BHCare!</p>
                                    <p style='color: #666; font-size: 12px; margin-top: 30px;'>
                                        BHCare Health Center<br/>
                                        Baesa, Quezon City
                                    </p>
                                </div>";
                            
                            await _emailService.SendEmailAsync(
                                userEmail, 
                                "BHCare Account Approved - Auto Verified", 
                                emailBody);
                            
                            _logger.LogInformation("Approval email sent to {Email}", userEmail);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send approval email to user");
                        }
                        
                        // Send notification to admin
                        try
                        {
                            var adminEmail = _configuration["AdminUser:Email"];
                            var fullName = $"{_encryptionService.Decrypt(user.FirstName)} {_encryptionService.Decrypt(user.LastName)}";
                            var userEmail = _encryptionService.Decrypt(user.Email);
                            
                            var adminEmailBody = $@"
                                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                    <h2 style='color: #2196F3;'>🤖 New Auto-Verified Account</h2>
                                    <p>A new user has been automatically verified and approved:</p>
                                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                        <tr style='background-color: #f5f5f5;'>
                                            <td style='padding: 10px; border: 1px solid #ddd;'><strong>Name:</strong></td>
                                            <td style='padding: 10px; border: 1px solid #ddd;'>{fullName}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 10px; border: 1px solid #ddd;'><strong>Email:</strong></td>
                                            <td style='padding: 10px; border: 1px solid #ddd;'>{userEmail}</td>
                                        </tr>
                                        <tr style='background-color: #f5f5f5;'>
                                            <td style='padding: 10px; border: 1px solid #ddd;'><strong>Barangay:</strong></td>
                                            <td style='padding: 10px; border: 1px solid #ddd;'>{detectedBarangay}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 10px; border: 1px solid #ddd;'><strong>Verification:</strong></td>
                                            <td style='padding: 10px; border: 1px solid #ddd;'>{approvalSource}</td>
                                        </tr>
                                        <tr style='background-color: #f5f5f5;'>
                                            <td style='padding: 10px; border: 1px solid #ddd;'><strong>Status:</strong></td>
                                            <td style='padding: 10px; border: 1px solid #ddd;'><span style='color: #4CAF50;'>✅ Active</span></td>
                                        </tr>
                                    </table>
                                    <p style='color: #666; font-size: 12px;'>This account was automatically approved based on {(approvalSource.Contains("OCR") ? "OCR-detected barangay from ID scan" : "profile barangay")}.</p>
                                </div>";
                            
                            await _emailService.SendEmailAsync(
                                adminEmail, 
                                "New Auto-Verified User Registration", 
                                adminEmailBody);
                            
                            _logger.LogInformation("Admin notification email sent");
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send notification email to admin");
                        }
                    }
                    else
                    {
                        // NOT ELIGIBLE - Set to pending
                        string userBarangayValue = Input.Barangay?.Trim() ?? "Unknown";
                        string ocrBarangayValue = Input.OcrDetectedBarangay?.Trim() ?? "None";
                        _logger.LogWarning("=== BARANGAY NOT ELIGIBLE FOR AUTO-APPROVAL ===");
                        _logger.LogWarning("OCR-detected barangay: {OcrBarangay}, User selected barangay: {UserBarangay} - not in eligible list (158-161)", 
                            ocrBarangayValue, userBarangayValue);
                        
                        // Ensure user is set to pending status
                        user.VerificationStatus = "Pending Review";
                        user.IsApproved = false;
                        user.Status = "Pending";
                        user.EncryptedStatus = "Pending"; // IMPORTANT: Update both Status fields
                        user.IsActive = false;
                        user.VerifiedBarangay = null; // Clear any invalid barangay
                        
                        // Store OCR text for admin review if available
                        if (!string.IsNullOrWhiteSpace(Input.OcrExtractedText))
                        {
                            user.OcrExtractedText = Input.OcrExtractedText.Length > 500 
                                ? Input.OcrExtractedText.Substring(0, 500) 
                                : Input.OcrExtractedText;
                        }
                        else if (!string.IsNullOrWhiteSpace(Input.OcrExtractedAddress))
                        {
                            user.OcrExtractedText = $"Address extracted: {Input.OcrExtractedAddress}";
                        }
                        
                        // Don't set ApprovedBy or ApprovedDate for pending users
                        user.ApprovedBy = null;
                        user.ApprovedDate = null;
                        
                        var updateResult = await _userManager.UpdateAsync(user);
                        if (!updateResult.Succeeded)
                        {
                            _logger.LogError("Failed to update user to pending status: {Errors}", 
                                string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                        }
                        
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("User set to Pending Review status");
                        
                        // Send "Pending Review" email notification
                        try
                        {
                            var userEmail = _encryptionService.Decrypt(user.Email);
                            var firstName = _encryptionService.Decrypt(user.FirstName);
                            
                            var emailBody = $@"
                                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                    <h2 style='color: #FF9800;'>⏳ BHCare Account - Pending Review</h2>
                                    <p>Hi <strong>{firstName}</strong>,</p>
                                    <p>Thank you for registering with BHCare! Your account has been created successfully.</p>
                                    <div style='background-color: #FFF3CD; border-left: 4px solid #FF9800; padding: 15px; margin: 20px 0;'>
                                        <strong>⏳ Your account is under review</strong>
                                        <p style='margin: 10px 0 0 0;'>Our admin team will verify your submitted documents and residency information. You will receive an email notification once your account is approved.</p>
                                    </div>
                                    <p><strong>What happens next?</strong></p>
                                    <ul>
                                        <li>Our admin team will review your ID and residency documents</li>
                                        <li>You will receive an email notification once your account is approved</li>
                                        <li>Once approved, you can log in and access all BHCare services</li>
                                    </ul>
                                    <p>We appreciate your patience during the review process.</p>
                                    <p style='color: #666; font-size: 12px; margin-top: 30px;'>
                                        BHCare Health Center<br/>
                                        Baesa, Quezon City
                                    </p>
                                </div>";
                            
                            await _emailService.SendEmailAsync(
                                userEmail, 
                                "BHCare Account - Pending Review", 
                                emailBody);
                            
                            _logger.LogInformation("Pending review email sent to {Email}", userEmail);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send pending review email to user");
                        }
                    }
                    
                    // Continue with registration success flow

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
                                var foundUserDocument = await _context.UserDocuments
                                    .FirstOrDefaultAsync(d => d.UserId == user.Id);
                                    
                                if (foundUserDocument != null)
                                {
                                    guardianProofPath = foundUserDocument.FilePath;
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

                    // Reload user to get updated verification status
                    user = await _userManager.FindByIdAsync(user.Id);
                    
                    _logger.LogInformation("Registration completed successfully");
                    _logger.LogInformation("User approval status: IsApproved={IsApproved}, VerificationStatus={Status}", 
                        user.IsApproved, user.VerificationStatus);
                    
                    // REDIRECT BASED ON APPROVAL STATUS
                    if (user.IsApproved && user.VerificationStatus == "Auto Verified")
                    {
                        // AUTO-APPROVED: Redirect to confirmation with success message
                        _logger.LogInformation("Redirecting auto-approved user to RegisterConfirmation");
                        TempData["SuccessMessage"] = $"🎉 Your account has been verified and approved automatically! Your residency in Barangay {user.VerifiedBarangay} was confirmed.";
                        TempData["AutoApproved"] = true;
                        TempData["VerifiedBarangay"] = user.VerifiedBarangay;
                        return RedirectToPage("/Account/RegisterConfirmation", new { auto = "true" });
                    }
                    else
                    {
                        // PENDING REVIEW: Redirect to pending approval page
                        _logger.LogInformation("Redirecting pending user to PendingApproval");
                        TempData["SuccessMessage"] = "Registration submitted successfully. Your residency document is under review. You will be notified once your account is approved.";
                        return RedirectToPage("/Account/PendingApproval");
                    }
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

        /// <summary>
        /// Enhanced handler for Azure Vision OCR ID Scanning with auto-fill
        /// Extracts: First Name, Middle Name, Last Name, Suffix, Contact Number, Address, Birth Date, Barangay
        /// </summary>
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostScanIdAsync(IFormFile idImage, bool usePreprocessing = true)
        {
            try
            {
                // Validate uploaded file
                if (idImage == null || idImage.Length == 0)
                {
                    return new JsonResult(new { success = false, message = "No image file uploaded" });
                }

                // Validate file size (max 5MB)
                const long maxFileSize = 5 * 1024 * 1024;
                if (idImage.Length > maxFileSize)
                {
                    return new JsonResult(new { success = false, message = "File size exceeds 5MB limit" });
                }

                // Validate file type (JPG, PNG)
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
                if (!allowedTypes.Contains(idImage.ContentType.ToLower()))
                {
                    return new JsonResult(new { success = false, message = "Only JPG and PNG files are supported" });
                }

                _logger.LogInformation("Processing ID image: {FileName}, Size: {Size} bytes", idImage.FileName, idImage.Length);

                // Try Local OCR first (Tesseract) as it often extracts more text including names
                // Then try Azure Vision OCR and combine results
                OcrResult localOcrResult = null;
                IdExtractionResult azureOcrResult = null;
                
                // Try Local OCR first
                try
                {
                    using (var stream = idImage.OpenReadStream())
                    {
                        localOcrResult = await _ocrService.AnalyzeResidencyDocumentAsync(stream, idImage.FileName);
                    }
                    _logger.LogInformation("Local OCR extracted text length: {Length}", localOcrResult?.ExtractedText?.Length ?? 0);
                }
                catch (Exception ex)
                {
                    // Check if this is a native library issue
                    bool isNativeLibraryError = ex.Message.Contains("libleptonica") || 
                                                ex.Message.Contains("DllNotFoundException") ||
                                                ex.InnerException?.Message?.Contains("libleptonica") == true;
                    
                    if (isNativeLibraryError)
                    {
                        _logger.LogWarning(ex, "Local OCR unavailable (native libraries not installed). Falling back to Azure Vision OCR.");
                    }
                    else
                    {
                        _logger.LogWarning(ex, "Local OCR failed. Will try Azure Vision OCR as fallback.");
                    }
                }
                
                // Try Azure Vision OCR
                try
                {
                    using (var stream = idImage.OpenReadStream())
                    {
                        azureOcrResult = await _azureVisionOcrService.AnalyzeIdImageAsync(stream, idImage.FileName, usePreprocessing);
                    }
                    _logger.LogInformation("Azure Vision OCR extracted text length: {Length}", azureOcrResult?.ExtractedText?.Length ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Azure Vision OCR failed");
                }

                // Combine results - merge text from all OCR sources for robust parsing
                // Local OCR often extracts more raw text, Azure Vision may have cleaner lines
                var textParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(localOcrResult?.ExtractedText))
                    textParts.Add(localOcrResult.ExtractedText);
                if (!string.IsNullOrWhiteSpace(azureOcrResult?.ExtractedText))
                    textParts.Add(azureOcrResult.ExtractedText);

                var combinedText = textParts.Count > 0
                    ? string.Join("\n", textParts.Distinct())
                    : string.Empty;
                
                // CRITICAL: If Azure Vision rejected the ID (Success = false), reject it completely
                // This happens when barangay is not in the valid list (158-161)
                bool shouldReject = azureOcrResult != null && !azureOcrResult.Success;
                
                var combinedResult = new IdExtractionResult
                {
                    // REJECT if Azure Vision rejected it, otherwise use OR logic
                    Success = shouldReject ? false : ((localOcrResult?.Success ?? false) || (azureOcrResult?.Success ?? false)),
                    Message = shouldReject 
                        ? azureOcrResult.Message 
                        : (azureOcrResult?.Message ?? localOcrResult?.Message ?? "Unable to extract data from the ID document."),
                    ExtractedText = combinedText,
                    BarangayNumber = azureOcrResult?.BarangayNumber ?? localOcrResult?.BarangayNumber ?? "",
                    IsBarangayValid = !string.IsNullOrEmpty(azureOcrResult?.BarangayNumber ?? localOcrResult?.BarangayNumber ?? "") &&
                                     new[] { "158", "159", "160", "161" }.Contains((azureOcrResult?.BarangayNumber ?? localOcrResult?.BarangayNumber ?? "").Trim())
                };
                
                // Parse the combined text for better name extraction (Local OCR often has more text)
                if (!string.IsNullOrEmpty(combinedText))
                {
                    _logger.LogInformation("Parsing combined text (length: {Length}) for name extraction", combinedText.Length);
                    var parsedData = _azureVisionOcrService.ParseIdDataFromText(combinedText);
                    
                    // Use parsed data from combined text (Local OCR often extracts the name better)
                    combinedResult.FirstName = parsedData.FirstName;
                    combinedResult.MiddleName = parsedData.MiddleName;
                    combinedResult.LastName = parsedData.LastName;
                    combinedResult.Suffix = parsedData.Suffix;
                    combinedResult.ContactNumber = parsedData.ContactNumber;
                    combinedResult.Address = parsedData.Address;
                    combinedResult.BirthDate = parsedData.BirthDate;
                    combinedResult.Gender = parsedData.Gender;
                    
                    _logger.LogInformation("Parsed from combined text - FirstName: {FirstName}, LastName: {LastName}, MiddleName: {MiddleName}, Suffix: {Suffix}, BirthDate: {BirthDate}, Gender: {Gender}",
                        parsedData.FirstName, parsedData.LastName, parsedData.MiddleName, parsedData.Suffix, parsedData.BirthDate, parsedData.Gender);
                    
                    // If Azure Vision found these fields and they're better (not address words), prefer them
                    // But only if our parsed data didn't find them or found wrong values
                    // === Fallback extraction for BirthDate & Gender if missing ===
                    if (string.IsNullOrWhiteSpace(combinedResult.BirthDate))
                    {
                        var dobRegex = new Regex(@"\b(\d{1,2})[/-](\d{1,2})[/-](\d{2,4})\b");
                        var dobMatch = dobRegex.Match(combinedText);
                        if (dobMatch.Success)
                        {
                            var month = dobMatch.Groups[1].Value.PadLeft(2,'0');
                            var day = dobMatch.Groups[2].Value.PadLeft(2,'0');
                            var year = dobMatch.Groups[3].Value;
                            if (year.Length==2) year = (int.Parse(year)>30?"19":"20")+year; // crude Y2K
                            combinedResult.BirthDate = $"{year}-{month}-{day}";
                        }
                    }
                    if (string.IsNullOrWhiteSpace(combinedResult.Gender))
                    {
                        var genderRegex = new Regex(@"\b(?:SEX|GENDER)[:\s]*(M|F|MALE|FEMALE)\b", RegexOptions.IgnoreCase);
                        var gMatch = genderRegex.Match(combinedText);
                        if (gMatch.Success)
                        {
                            var g = gMatch.Groups[1].Value.ToUpper();
                            combinedResult.Gender = (g.StartsWith("M")) ? "Male" : "Female";
                        }
                    }

                    if (string.IsNullOrEmpty(combinedResult.FirstName) && !string.IsNullOrEmpty(azureOcrResult?.FirstName))
                    {
                        // Only use Azure Vision if it's not an address word
                        if (!azureOcrResult.FirstName.Equals("BARANGAY", StringComparison.OrdinalIgnoreCase) &&
                            !azureOcrResult.FirstName.Equals("REPARO", StringComparison.OrdinalIgnoreCase))
                        {
                            combinedResult.FirstName = azureOcrResult.FirstName;
                        }
                    }
                    if (string.IsNullOrEmpty(combinedResult.LastName) && !string.IsNullOrEmpty(azureOcrResult?.LastName))
                    {
                        // Only use Azure Vision if it's not an address word
                        if (!azureOcrResult.LastName.Equals("BARANGAY", StringComparison.OrdinalIgnoreCase) &&
                            !azureOcrResult.LastName.Equals("REPARO", StringComparison.OrdinalIgnoreCase))
                        {
                            combinedResult.LastName = azureOcrResult.LastName;
                        }
                    }
                    if (!string.IsNullOrEmpty(azureOcrResult?.ContactNumber)) combinedResult.ContactNumber = azureOcrResult.ContactNumber;
                    if (!string.IsNullOrEmpty(azureOcrResult?.Address)) combinedResult.Address = azureOcrResult.Address;
                    if (!string.IsNullOrEmpty(azureOcrResult?.BirthDate)) combinedResult.BirthDate = azureOcrResult.BirthDate;

                    // If middle name, suffix, or gender are still missing but Azure has them, use Azure's values
                    if (string.IsNullOrEmpty(combinedResult.MiddleName) && !string.IsNullOrEmpty(azureOcrResult?.MiddleName))
                        combinedResult.MiddleName = azureOcrResult.MiddleName;
                    if (string.IsNullOrEmpty(combinedResult.Suffix) && !string.IsNullOrEmpty(azureOcrResult?.Suffix))
                        combinedResult.Suffix = azureOcrResult.Suffix;
                    if (string.IsNullOrEmpty(combinedResult.Gender) && !string.IsNullOrEmpty(azureOcrResult?.Gender))
                        combinedResult.Gender = azureOcrResult.Gender;
                }
                else
                {
                    // Fallback to Azure Vision result if Local OCR didn't extract text
                    combinedResult = azureOcrResult ?? combinedResult;
                }
                
                if (!combinedResult.Success)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = combinedResult.Message,
                        autoApproved = false,
                        firstName = "",
                        middleName = "",
                        lastName = "",
                        suffix = "",
                        contactNumber = "",
                        address = "",
                        birthDate = "",
                        barangay = ""
                    });
                }
                
                var ocrResult = combinedResult;

                // CRITICAL VALIDATION: Only accept barangays 158, 159, 160, or 161
                var validBarangays = new[] { "158", "159", "160", "161" };
                bool isBarangayValid = ocrResult.IsBarangayValid;

                // Format names properly (Title Case) before sending to frontend
                // Helper method to convert names to proper Title Case
                string NormalizeDate(string raw)
                {
                    if (string.IsNullOrWhiteSpace(raw)) return "";
                    raw = raw.Trim();
                    // Accept formats: YYYY/MM/DD, YYYY-MM-DD, MM/DD/YYYY, DD/MM/YYYY
                    // Replace separators with '-'
                    raw = raw.Replace("/", "-").Replace(".", "-");
                    // If already YYYY-MM-DD return if valid
                    if (System.Text.RegularExpressions.Regex.IsMatch(raw, "^\\d{4}-\\d{2}-\\d{2}$")) return raw;

                    // If MM-DD-YYYY or DD-MM-YYYY convert
                    var parts = raw.Split('-');
                    if (parts.Length == 3)
                    {
                        if (parts[2].Length == 4)
                        {
                            // assume MM-DD-YYYY or DD-MM-YYYY
                            var year = parts[2];
                            var month = parts[0].PadLeft(2, '0');
                            var day = parts[1].PadLeft(2, '0');
                            return $"{year}-{month}-{day}";
                        }
                        if (parts[0].Length == 4)
                        {
                            // YYYY-DD-MM (unlikely) just reorder to YYYY-MM-DD
                            var year = parts[0];
                            var month = parts[2].PadLeft(2, '0');
                            var day = parts[1].PadLeft(2, '0');
                            return $"{year}-{month}-{day}";
                        }
                    }
                    // Try spelled month e.g., OCTOBER 14 2003
                    var monthNames = System.Globalization.DateTimeFormatInfo.InvariantInfo.MonthNames.Where(m=>!string.IsNullOrEmpty(m)).ToList();
                    foreach (var m in monthNames)
                    {
                        if (raw.ToUpper().Contains(m.Substring(0,3).ToUpper()))
                        {
                            var regex = System.Text.RegularExpressions.Regex.Match(raw, $"{m}\\s+(\\d{{1,2}}),?\\s+(\\d{{4}})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (regex.Success)
                            {
                                var day = regex.Groups[1].Value.PadLeft(2,'0');
                                var year = regex.Groups[2].Value;
                                var monthNum = monthNames.IndexOf(m)+1;
                                return $"{year}-{monthNum.ToString().PadLeft(2,'0')}-{day}";
                            }
                        }
                    }
                    return raw; // fallback
                }

                string FormatName(string name)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        return "";
                    
                    // Split by spaces to handle multiple names (e.g., "RHYLLE LANDER")
                    var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var formattedWords = words.Select(word =>
                    {
                        if (string.IsNullOrWhiteSpace(word))
                            return word;
                        
                        // Convert to Title Case: First letter uppercase, rest lowercase
                        return char.ToUpper(word[0]) + (word.Length > 1 ? word.Substring(1).ToLower() : "");
                    });
                    
                    return string.Join(" ", formattedWords);
                }
                
                // Return all extracted fields for auto-fill, regardless of barangay validation
                // The frontend will handle showing the error message if barangay is invalid
                return new JsonResult(new 
                { 
                    success = true, 
                    message = ocrResult.Message,
                    autoApproved = isBarangayValid,
                    // Extracted fields for auto-fill - format names properly
                    firstName = FormatName(ocrResult.FirstName ?? ""),
                    middleName = FormatName(ocrResult.MiddleName ?? ""),
                    lastName = FormatName(ocrResult.LastName ?? ""),
                    suffix = !string.IsNullOrWhiteSpace(ocrResult.Suffix) ? ocrResult.Suffix.ToUpper() : "",
                    contactNumber = ocrResult.ContactNumber ?? "",
                    address = ocrResult.Address ?? "",
                    birthDate = NormalizeDate(combinedResult.BirthDate),
                    gender = ocrResult.Gender ?? "",
                    barangay = ocrResult.BarangayNumber ?? "",
                    isBarangayValid = isBarangayValid,
                    extractedText = ocrResult.ExtractedText ?? "",
                    // Flags to indicate which critical fields were actually detected from the ID
                    hasBirthDateFromId = !string.IsNullOrWhiteSpace(combinedResult.BirthDate),
                    hasGenderFromId = !string.IsNullOrWhiteSpace(ocrResult.Gender),
                    hasMiddleNameFromId = !string.IsNullOrWhiteSpace(ocrResult.MiddleName)
                });
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.Message.Contains("Timeout"))
            {
                _logger.LogError(ex, "OCR request timed out");
                return new JsonResult(new 
                { 
                    success = false, 
                    message = "The OCR request timed out. Please try again with a clearer image."
                });
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "OCR processing timed out");
                return new JsonResult(new 
                { 
                    success = false, 
                    message = "OCR processing took too long to complete. Please try again with a clearer image."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OCR request");
                return new JsonResult(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }


        /// <summary>
        /// Validates that the extracted text is from an actual Philippine ID document
        /// Rejects plain text, screenshots, or documents without ID markers
        /// STRICT VALIDATION: Requires actual ID document markers, not just address fields
        /// </summary>
        private bool IsValidPhilippineIdDocument(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var upperText = text.ToUpper();
            
            // CRITICAL: Check for screenshot indicators in text (screenshots often have UI elements)
            var screenshotIndicators = new[] { "SCREENSHOT", "SCREEN SHOT", "CAPTURE", "SNAP", "WINDOWS", "MACOS", "ANDROID", "IOS" };
            if (screenshotIndicators.Any(indicator => upperText.Contains(indicator)))
            {
                _logger.LogWarning("⚠️ Document validation failed: Screenshot indicators found in text");
                return false;
            }
            
            // Required Philippine ID markers - document MUST contain at least one STRONG ID marker
            // These are specific to actual ID documents, not just any document with an address
            var strongIdMarkers = new[]
            {
                // Republic of the Philippines markers (REQUIRED for most IDs)
                "REPUBLIC OF THE PHILIPPINES",
                "REPUBLIKA NG PILIPINAS",
                "REPUBLIC OF THE PHILIPPINE",
                
                // Driver's License markers (REQUIRED)
                "DRIVER'S LICENSE",
                "DRIVERS LICENSE",
                "DRIVER LICENSE",
                "LICENSE TO DRIVE",
                "LAND TRANSPORTATION OFFICE",
                "LTO",
                "DEPARTMENT OF TRANSPORTATION",
                
                // National ID markers (REQUIRED)
                "PHILSYS",
                "PHILIPPINE IDENTIFICATION SYSTEM",
                "PHILIPPINE NATIONAL ID",
                "NATIONAL ID",
                "PAMBANSANG PAGKAKAKILANLAN",
                "PHILIPPINE IDENTIFICATION CARD",
                
                // PhilHealth markers (REQUIRED)
                "PHILHEALTH",
                "PHIL-HEALTH",
                "PHILIPPINE HEALTH INSURANCE",
                "MEMBER ID",
                
                // UMID/SSS markers (REQUIRED)
                "UMID",
                "UNIFIED MULTI-PURPOSE ID",
                "GSIS",
                "SSS",
                "SOCIAL SECURITY",
                
                // Postal ID markers (REQUIRED)
                "POSTAL ID",
                "PHILIPPINE POSTAL",
                "PHLPOST",
                "POST OFFICE",
                
                // Passport markers (REQUIRED)
                "PASSPORT",
                "REPUBLIC OF THE PHILIPPINES PASSPORT",
                
                // TIN ID markers (REQUIRED)
                "TIN",
                "TAX IDENTIFICATION NUMBER",
                "BIR",
                "BUREAU OF INTERNAL REVENUE"
            };

            // Check for STRONG ID markers first (these are required for legitimate IDs)
            bool hasStrongIdMarker = strongIdMarkers.Any(marker => upperText.Contains(marker));
            
            // Also check for partial matches of strong markers (handle OCR errors)
            if (!hasStrongIdMarker)
            {
                hasStrongIdMarker = 
                    (upperText.Contains("REPUBLIC") && (upperText.Contains("PHILIPPINES") || upperText.Contains("PHILIPPINE"))) ||
                    ((upperText.Contains("DRIVER") || upperText.Contains("DRIVERS")) && upperText.Contains("LICENSE")) ||
                    upperText.Contains("PHILSYS") ||
                    upperText.Contains("PHILHEALTH") ||
                    upperText.Contains("UMID") ||
                    upperText.Contains("POSTAL ID") ||
                    upperText.Contains("PASSPORT") ||
                    ((upperText.Contains("TAX") || upperText.Contains("TIN")) && upperText.Contains("IDENTIFICATION"));
            }
            
            // CRITICAL: Must have a STRONG ID marker - screenshots won't have these
            if (!hasStrongIdMarker)
            {
                _logger.LogWarning("⚠️ Document validation failed: No strong Philippine ID markers found");
                _logger.LogWarning("Text preview: {Preview}", text.Substring(0, Math.Min(500, text.Length)));
                _logger.LogWarning("Screenshots and non-ID documents are rejected. Please upload an actual Philippine ID document.");
                return false;
            }

            // Additional validation: Check for ID-specific fields
            var idFields = new[]
            {
                "LAST NAME", "SURNAME", "APELYIDO", "APELLIDO",
                "FIRST NAME", "GIVEN NAME", "MGA PANGALAN",
                "DATE OF BIRTH", "BIRTH DATE", "KAPANGANAKAN",
                "ADDRESS", "TIRAHAN",
                "SEX", "GENDER", "KASARIAN"
            };

            // Document should have at least 2 ID fields (name + address or birth date)
            int fieldCount = idFields.Count(field => upperText.Contains(field));
            
            if (fieldCount < 2)
            {
                _logger.LogWarning("⚠️ Document validation failed: Insufficient ID fields found (found {Count}, need at least 2)", fieldCount);
                return false;
            }

            _logger.LogInformation("✅ Document validation passed: Philippine ID detected (markers: {Markers}, fields: {Fields})", 
                strongIdMarkers.Count(m => upperText.Contains(m)), fieldCount);
            return true;
        }

        private string DetectEligibleBarangayFromAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            // Case-insensitive regex pattern to match "Barangay 158", "Barangay 159", "Barangay 160", or "Barangay 161"
            // Uses word boundary to ensure exact match (e.g., "Barangay 160" not "Barangay 1600")
            var barangayPattern = @"\bBarangay\s*(158|159|160|161)\b";
            var match = Regex.Match(address, barangayPattern, RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count > 1)
            {
                var barangayNumber = match.Groups[1].Value;
                _logger.LogInformation($"Detected eligible barangay from OCR address: Barangay {barangayNumber}");
                return barangayNumber;
            }

            // Also try patterns without "Barangay" prefix (e.g., "BRGY 160", "160", etc.)
            var alternativePatterns = new[]
            {
                @"\bBRGY\.?\s*(158|159|160|161)\b",
                @"\b(158|159|160|161)\s*(?:ST|ND|RD|TH)?\s*BARANGAY\b",
                @"\bBARANGAY\s*(?:NO\.?|#)?\s*(158|159|160|161)\b"
            };

            foreach (var pattern in alternativePatterns)
            {
                match = Regex.Match(address, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var barangayNumber = match.Groups[1].Value;
                    _logger.LogInformation($"Detected eligible barangay from OCR address (alternative pattern): Barangay {barangayNumber}");
                    return barangayNumber;
                }
            }

            _logger.LogDebug($"No eligible barangay detected in address: {address.Substring(0, Math.Min(100, address.Length))}...");
            return null;
        }


        /// <summary>
        /// Parse extracted OCR text to identify name and address fields
        /// </summary>
        private (string FirstName, string LastName, string Address) ParseIdData(string text)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToList();

            string firstName = null;
            string lastName = null;
            string address = null;

            // Look for common ID patterns (Philippine National ID, Driver's License, etc.)
            foreach (var line in lines)
            {
                var upperLine = line.ToUpper();

                // Try to find last name (often labeled as "SURNAME" or "LAST NAME")
                if ((upperLine.Contains("SURNAME") || upperLine.Contains("LAST NAME") || upperLine.Contains("FAMILY NAME")) 
                    && string.IsNullOrEmpty(lastName))
                {
                    // Try to extract value after colon or on next line
                    var parts = line.Split(new[] { ':', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        lastName = parts[1].Trim();
                    }
                    else
                    {
                        // Value might be on next line
                        var currentIndex = lines.IndexOf(line);
                        if (currentIndex + 1 < lines.Count)
                        {
                            lastName = lines[currentIndex + 1].Trim();
                        }
                    }
                }

                // Try to find first name (often labeled as "GIVEN NAME" or "FIRST NAME")
                if ((upperLine.Contains("GIVEN NAME") || upperLine.Contains("FIRST NAME")) 
                    && string.IsNullOrEmpty(firstName))
                {
                    var parts = line.Split(new[] { ':', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        firstName = parts[1].Trim();
                    }
                    else
                    {
                        var currentIndex = lines.IndexOf(line);
                        if (currentIndex + 1 < lines.Count)
                        {
                            firstName = lines[currentIndex + 1].Trim();
                        }
                    }
                }

                // Try to find address
                if ((upperLine.Contains("ADDRESS") || upperLine.Contains("RESIDENCE")) 
                    && string.IsNullOrEmpty(address))
                {
                    var parts = line.Split(new[] { ':', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        address = parts[1].Trim();
                    }
                    else
                    {
                        // Address might span multiple lines
                        var currentIndex = lines.IndexOf(line);
                        if (currentIndex + 1 < lines.Count)
                        {
                            var addressLines = new List<string>();
                            for (int i = currentIndex + 1; i < Math.Min(currentIndex + 4, lines.Count); i++)
                            {
                                var nextLine = lines[i];
                                // Stop if we hit another field label
                                if (nextLine.ToUpper().Contains("DATE") || 
                                    nextLine.ToUpper().Contains("BIRTH") ||
                                    nextLine.ToUpper().Contains("SEX") ||
                                    nextLine.ToUpper().Contains("GENDER"))
                                {
                                    break;
                                }
                                addressLines.Add(nextLine);
                            }
                            address = string.Join(", ", addressLines);
                        }
                    }
                }
            }

            _logger.LogInformation($"Parsed data - FirstName: {firstName}, LastName: {lastName}, Address: {address}");
            return (firstName, lastName, address);
        }
    }
}
