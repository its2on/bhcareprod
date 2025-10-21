using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    [Authorize(Policy = "PatientList")]
    public class CreateImmunizationRecordModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly ILogger<CreateImmunizationRecordModel> _logger;
        private readonly IImmunizationReminderService _immunizationReminderService;
        private readonly IDataEncryptionService _encryptionService;

        public CreateImmunizationRecordModel(
            EncryptedDbContext context,
            ILogger<CreateImmunizationRecordModel> logger,
            IImmunizationReminderService immunizationReminderService,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _immunizationReminderService = immunizationReminderService;
            _encryptionService = encryptionService;
        }

        [BindProperty]
        public ImmunizationRecord Record { get; set; } = new();

        public Models.Appointment Appointment { get; set; }

        public async Task<IActionResult> OnGetAsync(int? appointmentId)
        {
            // Set default values
            Record.HealthCenter = "Baesa Health Center";
            Record.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            Record.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            Record.CreatedBy = User.Identity?.Name ?? "Unknown";
            Record.UpdatedBy = User.Identity?.Name ?? "Unknown";

            // If appointmentId is provided, pre-fill the form with appointment data
            if (appointmentId.HasValue)
            {
                Appointment = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId.Value);

                if (Appointment != null)
                {
                    _logger.LogInformation("Pre-filling immunization record form from appointment ID: {AppointmentId}", appointmentId.Value);

                    // Child information from appointment
                    Record.ChildName = Appointment.PatientName ?? "";
                    
                    // DateOfBirth - the form will handle date conversion
                    if (Appointment.DateOfBirth.HasValue)
                    {
                        Record.DateOfBirth = Appointment.DateOfBirth.Value.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        Record.DateOfBirth = "";
                    }
                    
                    Record.Sex = Appointment.Gender ?? "";
                    
                    // Parent/Guardian information (from the patient's user record)
                    if (Appointment.Patient?.User != null)
                    {
                        var parentUser = Appointment.Patient.User;
                        
                        // Set mother/father name (assuming the booker is the parent)
                        Record.MotherName = parentUser.FullName ?? "";
                        Record.Email = parentUser.Email ?? "";
                        Record.ContactNumber = Appointment.ContactNumber ?? parentUser.PhoneNumber ?? "";
                        
                        // Use user's address if available
                        Record.Address = parentUser.Address ?? Appointment.Address ?? "";
                        Record.Barangay = parentUser.Barangay ?? "";
                    }
                    else
                    {
                        // Fallback: use appointment contact info
                        Record.ContactNumber = Appointment.ContactNumber ?? "";
                        Record.Address = Appointment.Address ?? "";
                    }
                    
                    _logger.LogInformation("Immunization record pre-filled for child: {ChildName}", Record.ChildName);
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Log the form data for debugging
            _logger.LogInformation("Immunization record submission - ChildName: {ChildName}, DateOfBirth: {DateOfBirth}, MotherName: {MotherName}, Email: {Email}", 
                Record.ChildName, Record.DateOfBirth, Record.MotherName, Record.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Page();
            }

            try
            {
                // Set default values if missing
                if (string.IsNullOrEmpty(Record.HealthCenter))
                    Record.HealthCenter = "Baesa Health Center";
                
                // Set audit fields
                Record.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Record.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Record.CreatedBy = User.Identity?.Name ?? "Unknown";
                Record.UpdatedBy = User.Identity?.Name ?? "Unknown";

                _logger.LogInformation("Adding ImmunizationRecord to context - ChildName: {ChildName}", Record.ChildName);
                _context.ImmunizationRecords.Add(Record);
                
                _logger.LogInformation("Saving changes to database...");
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved ImmunizationRecord to database");

                // Send email notification to parent
                if (!string.IsNullOrEmpty(Record.Email))
                {
                    await SendImmunizationRecordConfirmationEmailAsync();
                }

                TempData["SuccessMessage"] = $"Immunization record for {Record.ChildName} has been created successfully. Confirmation email sent to {Record.Email}.";
                _logger.LogInformation("Immunization record created for child {ChildName} by user {User}", 
                    Record.ChildName, User.Identity?.Name);

                return RedirectToPage("/Nurse/ImmunizationRecords");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating immunization record for child {ChildName}", Record.ChildName);
                ModelState.AddModelError("", "An error occurred while saving the immunization record. Please try again.");
                return Page();
            }
        }

        private async Task SendImmunizationRecordConfirmationEmailAsync()
        {
            try
            {
                // Decrypt the data before using it in the email
                Record.DecryptSensitiveData(_encryptionService, User);
                
                var confirmationMessage = $@"Dear {Record.MotherName},

Your child's immunization record has been successfully created at Baesa Health Center.

CHILD INFORMATION:
- Name: {Record.ChildName}
- Date of Birth: {Record.DateOfBirth}
- Sex: {Record.Sex}
- Family Number: {Record.FamilyNumber}

HEALTH CENTER DETAILS:
- Health Center: {Record.HealthCenter}
- Barangay: {Record.Barangay}
- Record Created: {Record.CreatedAt}

IMMUNIZATION SCHEDULE:
Your child's vaccination schedule has been recorded. Please ensure to bring your child for all scheduled vaccinations according to the official immunization schedule.

IMPORTANT REMINDERS:
- Keep this email as proof of your child's immunization record
- Bring this record for future visits
- Contact us if you have any questions about your child's vaccination schedule
- Immunization services are available every Wednesday from 8:00 AM to 12:00 PM

For any questions or concerns, please contact Baesa Health Center.

Best regards,
Baesa Health Center Team";

                await _immunizationReminderService.SendImmunizationReminderAsync(
                    Record.Email, 
                    Record.MotherName, 
                    confirmationMessage);

                _logger.LogInformation("Immunization record confirmation email sent to {Email} for child {ChildName}", 
                    Record.Email, Record.ChildName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send immunization record confirmation email to {Email}", Record.Email);
                // Don't fail the form submission if email fails
            }
        }
    }
}
