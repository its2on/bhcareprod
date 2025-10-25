using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Barangay.Models;
using Barangay.Services;
using Barangay.ViewModels;
using Barangay.Helpers;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// Ensure this is the first line (no statements before namespace)
namespace Barangay.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            IEncryptionService encryptionService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            // This method handles the redirect from access denied pages
            // For user-specific pages that need custom handling
            if (!string.IsNullOrEmpty(returnUrl))
            {
                if (returnUrl.Contains("/User/Records"))
                {
                    return RedirectToPage("/User/Records");
                }
                else if (returnUrl.Contains("/User/Prescriptions"))
                {
                    return RedirectToPage("/User/Prescriptions");
                }
                else if (returnUrl.Contains("/User/Help"))
                {
                    return RedirectToPage("/User/Help");
                }
                else if (returnUrl.Contains("/User/Settings"))
                {
                    return RedirectToPage("/User/Settings");
                }
                else if (returnUrl.Contains("/User/Appointments"))
                {
                    return RedirectToPage("/User/Appointments");
                }
            }
            
            // Default fallback - redirect to the access denied page
            return RedirectToPage("/Account/AccessDenied", new { returnUrl });
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            // Ensure roles exist
            var roles = new[] { "Admin", "Doctor", "Patient", "Staff" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Calculate age from BirthDate if provided
                if (model.BirthDate.HasValue)
                {
                    var today = DateTime.Today;
                    int age = today.Year - model.BirthDate.Value.Year;
                    if (model.BirthDate.Value > today.AddYears(-age)) age--;

                    // Check if guardian info is required for users under 18
                    if (age < 18 && (string.IsNullOrWhiteSpace(model.GuardianFullName) || string.IsNullOrWhiteSpace(model.GuardianContactNumber)))
                    {
                        ModelState.AddModelError("", "Guardian information is required for users under 18.");
                        return View(model);
                    }
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = _encryptionService.Encrypt(model.Email), // Encrypt email
                    FullName = model.FullName,
                    BirthDate = model.BirthDate ?? DateTime.Now,
                    Gender = model.Gender,
                    PhoneNumber = _encryptionService.Encrypt(model.PhoneNumber), // Encrypt phone number
                    Address = _encryptionService.Encrypt(model.Address), // Encrypt address
                    CreatedAt = DateTimeHelper.ToUtc(DateTimeHelper.Now)
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        if (!await _roleManager.RoleExistsAsync(model.Role))
                        {
                            await _roleManager.CreateAsync(new IdentityRole(model.Role));
                        }
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }

                    TempData["SuccessMessage"] = "User created successfully!";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check for guardian information for users under 18
                var today = DateTime.Today;
                int age = today.Year - model.BirthDate.Year;
                if (model.BirthDate > today.AddYears(-age)) age--;

                if (age < 18 && (string.IsNullOrWhiteSpace(model.GuardianFullName) || string.IsNullOrWhiteSpace(model.GuardianContactNumber)))
                {
                    ModelState.AddModelError("", "Guardian information is required for users under 18.");
                    return View(model);
                }

                // Fix for CS8601: Possible null reference assignment
                var user = new ApplicationUser
                {
                    UserName = model.Email ?? string.Empty,
                    Email = _encryptionService.Encrypt(model.Email ?? string.Empty), // Encrypt email
                    FullName = model.FullName ?? string.Empty,
                    BirthDate = model.BirthDate,
                    Gender = model.Gender ?? string.Empty,
                    Address = _encryptionService.Encrypt(model.Address ?? string.Empty) // Encrypt address
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Add user to role if needed
                    await _userManager.AddToRoleAsync(user, "Patient");
                    
                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    return RedirectToAction("Index", "Home");
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }
    }
}