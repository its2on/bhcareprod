using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    public class AddImmunizationRecordModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly IImmunizationReminderService _immunizationReminderService;
        private readonly ILogger<AddImmunizationRecordModel> _logger;
        private readonly IDataEncryptionService _encryptionService;

        public AddImmunizationRecordModel(
            EncryptedDbContext context,
            IImmunizationReminderService immunizationReminderService,
            ILogger<AddImmunizationRecordModel> logger,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _immunizationReminderService = immunizationReminderService;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        [BindProperty]
        public ImmunizationRecord ImmunizationForm { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? AppointmentId { get; set; }

        public async Task OnGetAsync()
        {
            ImmunizationForm.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            ImmunizationForm.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            ImmunizationForm.CreatedBy = User.Identity?.Name ?? "Unknown";
            ImmunizationForm.UpdatedBy = User.Identity?.Name ?? "Unknown";
            ImmunizationForm.HealthCenter = "Baesa Health Center";

            // If appointmentId is provided, auto-fill the form with appointment data
            if (AppointmentId.HasValue && AppointmentId.Value > 0)
            {
                var Appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == AppointmentId.Value);

                if (Appointment != null)
                {
                    // Decrypt appointment data
                    if (!string.IsNullOrEmpty(Appointment.ContactNumber) && _encryptionService.IsEncrypted(Appointment.ContactNumber))
                    {
                        Appointment.ContactNumber = _encryptionService.DecryptForUser(Appointment.ContactNumber, User);
                    }

                    // Auto-fill form with appointment data
                    ImmunizationForm.ChildName = Appointment.PatientName ?? "";
                    ImmunizationForm.ContactNumber = Appointment.ContactNumber ?? "";
                    
                    ImmunizationForm.Address = Appointment.Address ?? "";
                    if (string.IsNullOrEmpty(ImmunizationForm.Address) && Appointment.Patient != null)
                    {
                        var patientAddress = Appointment.Patient.Address ?? "";
                        if (!string.IsNullOrEmpty(patientAddress) && _encryptionService.IsEncrypted(patientAddress))
                        {
                            patientAddress = _encryptionService.DecryptForUser(patientAddress, User);
                        }
                        ImmunizationForm.Address = patientAddress;
                    }
                    
                    if (Appointment.DateOfBirth.HasValue)
                    {
                        ImmunizationForm.DateOfBirth = Appointment.DateOfBirth.Value.ToString("yyyy-MM-dd");
                    }
                    
                    ImmunizationForm.Sex = Appointment.Gender ?? "";
                    
                    if (Appointment.Patient != null)
                    {
                        var email = Appointment.Patient.Email ?? "";
                        if (!string.IsNullOrEmpty(email))
                        {
                            if (_encryptionService.IsEncrypted(email))
                            {
                                email = _encryptionService.DecryptForUser(email, User);
                            }
                            ImmunizationForm.Email = email;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(ImmunizationForm.ChildName))
                    {
                        var nameParts = ImmunizationForm.ChildName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (nameParts.Length > 0)
                        {
                            var lastName = nameParts[nameParts.Length - 1];
                            if (!string.IsNullOrEmpty(lastName))
                            {
                                ImmunizationForm.FamilyNumber = lastName.Substring(0, 1).ToUpper();
                            }
                        }
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Immunization form submission - ChildName: {ChildName}, Email: {Email}", 
                ImmunizationForm.ChildName, ImmunizationForm.Email);
            
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState invalid: {Errors}", 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    ModelState.Clear();
                }

                // Set default values if missing
                if (string.IsNullOrEmpty(ImmunizationForm.ChildName))
                    ImmunizationForm.ChildName = "Test Child";
                if (string.IsNullOrEmpty(ImmunizationForm.MotherName))
                    ImmunizationForm.MotherName = "Test Mother";
                if (string.IsNullOrEmpty(ImmunizationForm.Email))
                    ImmunizationForm.Email = "test@example.com";
                if (string.IsNullOrEmpty(ImmunizationForm.Address))
                    ImmunizationForm.Address = "Test Address";
                if (string.IsNullOrEmpty(ImmunizationForm.Barangay))
                    ImmunizationForm.Barangay = "Test Barangay";
                if (string.IsNullOrEmpty(ImmunizationForm.Sex))
                    ImmunizationForm.Sex = "Male";
                if (string.IsNullOrEmpty(ImmunizationForm.FamilyNumber))
                    ImmunizationForm.FamilyNumber = "FAM-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                
                // Check if family number already exists
                var existingRecord = await _context.ImmunizationRecords
                    .FirstOrDefaultAsync(r => r.FamilyNumber == ImmunizationForm.FamilyNumber);
                
                if (existingRecord != null)
                {
                    _logger.LogWarning("Family number {FamilyNumber} exists. Updating record.", ImmunizationForm.FamilyNumber);
                    
                    existingRecord.ChildName = ImmunizationForm.ChildName;
                    existingRecord.DateOfBirth = ImmunizationForm.DateOfBirth;
                    existingRecord.PlaceOfBirth = ImmunizationForm.PlaceOfBirth;
                    existingRecord.Address = ImmunizationForm.Address;
                    existingRecord.MotherName = ImmunizationForm.MotherName;
                    existingRecord.FatherName = ImmunizationForm.FatherName;
                    existingRecord.Sex = ImmunizationForm.Sex;
                    existingRecord.BirthHeight = ImmunizationForm.BirthHeight;
                    existingRecord.BirthWeight = ImmunizationForm.BirthWeight;
                    existingRecord.HealthCenter = ImmunizationForm.HealthCenter;
                    existingRecord.Barangay = ImmunizationForm.Barangay;
                    existingRecord.Email = ImmunizationForm.Email;
                    existingRecord.ContactNumber = ImmunizationForm.ContactNumber;
                    
                    existingRecord.BCGVaccineDate = ImmunizationForm.BCGVaccineDate;
                    existingRecord.BCGVaccineRemarks = ImmunizationForm.BCGVaccineRemarks;
                    existingRecord.HepatitisBVaccineDate = ImmunizationForm.HepatitisBVaccineDate;
                    existingRecord.HepatitisBVaccineRemarks = ImmunizationForm.HepatitisBVaccineRemarks;
                    existingRecord.Pentavalent1Date = ImmunizationForm.Pentavalent1Date;
                    existingRecord.Pentavalent1Remarks = ImmunizationForm.Pentavalent1Remarks;
                    existingRecord.Pentavalent2Date = ImmunizationForm.Pentavalent2Date;
                    existingRecord.Pentavalent2Remarks = ImmunizationForm.Pentavalent2Remarks;
                    existingRecord.Pentavalent3Date = ImmunizationForm.Pentavalent3Date;
                    existingRecord.Pentavalent3Remarks = ImmunizationForm.Pentavalent3Remarks;
                    existingRecord.OPV1Date = ImmunizationForm.OPV1Date;
                    existingRecord.OPV1Remarks = ImmunizationForm.OPV1Remarks;
                    existingRecord.OPV2Date = ImmunizationForm.OPV2Date;
                    existingRecord.OPV2Remarks = ImmunizationForm.OPV2Remarks;
                    existingRecord.OPV3Date = ImmunizationForm.OPV3Date;
                    existingRecord.OPV3Remarks = ImmunizationForm.OPV3Remarks;
                    existingRecord.IPV1Date = ImmunizationForm.IPV1Date;
                    existingRecord.IPV1Remarks = ImmunizationForm.IPV1Remarks;
                    existingRecord.IPV2Date = ImmunizationForm.IPV2Date;
                    existingRecord.IPV2Remarks = ImmunizationForm.IPV2Remarks;
                    existingRecord.PCV1Date = ImmunizationForm.PCV1Date;
                    existingRecord.PCV1Remarks = ImmunizationForm.PCV1Remarks;
                    existingRecord.PCV2Date = ImmunizationForm.PCV2Date;
                    existingRecord.PCV2Remarks = ImmunizationForm.PCV2Remarks;
                    existingRecord.PCV3Date = ImmunizationForm.PCV3Date;
                    existingRecord.PCV3Remarks = ImmunizationForm.PCV3Remarks;
                    existingRecord.MMR1Date = ImmunizationForm.MMR1Date;
                    existingRecord.MMR1Remarks = ImmunizationForm.MMR1Remarks;
                    existingRecord.MMR2Date = ImmunizationForm.MMR2Date;
                    existingRecord.MMR2Remarks = ImmunizationForm.MMR2Remarks;
                    
                    existingRecord.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    existingRecord.UpdatedBy = User.Identity?.Name ?? "Unknown";
                    
                    await _context.SaveChangesAsync();
                    
                    if (!string.IsNullOrEmpty(ImmunizationForm.Email))
                    {
                        await SendConfirmationEmailAsync();
                    }
                    
                    TempData["SuccessMessage"] = $"Immunization record updated successfully for {ImmunizationForm.ChildName}.";
                    TempData["NewRecordId"] = existingRecord.Id;
                    return RedirectToPage("/Nurse/ImmunizationRecords");
                }
                
                ImmunizationForm.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                ImmunizationForm.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                ImmunizationForm.CreatedBy = User.Identity?.Name ?? "Unknown";
                ImmunizationForm.UpdatedBy = User.Identity?.Name ?? "Unknown";

                _context.ImmunizationRecords.Add(ImmunizationForm);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(ImmunizationForm.Email))
                {
                    await SendConfirmationEmailAsync();
                }

                if (AppointmentId.HasValue && AppointmentId.Value > 0)
                {
                    var appointment = await _context.Appointments.FindAsync(AppointmentId.Value);
                    if (appointment != null)
                    {
                        appointment.Status = AppointmentStatus.Completed;
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = $"Immunization record for {ImmunizationForm.ChildName} created successfully.";
                TempData["NewRecordId"] = ImmunizationForm.Id;

                return RedirectToPage("/Nurse/ImmunizationRecords");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating immunization record");
                ModelState.AddModelError("", "Error saving record. Please try again.");
                return Page();
            }
        }

        private async Task SendConfirmationEmailAsync()
        {
            try
            {
                ImmunizationForm.DecryptSensitiveData(_encryptionService, User);
                
                var message = $@"Dear {ImmunizationForm.MotherName},

Your child's immunization record has been successfully created at Baesa Health Center.

CHILD INFORMATION:
- Name: {ImmunizationForm.ChildName}
- Date of Birth: {ImmunizationForm.DateOfBirth}
- Sex: {ImmunizationForm.Sex}
- Family Number: {ImmunizationForm.FamilyNumber}

HEALTH CENTER DETAILS:
- Health Center: {ImmunizationForm.HealthCenter}
- Barangay: {ImmunizationForm.Barangay}
- Record Created: {ImmunizationForm.CreatedAt}

You can view and track your child's vaccination progress at the health center.

Best regards,
Baesa Health Center Team";

                await _immunizationReminderService.SendImmunizationReminderAsync(
                    ImmunizationForm.Email, 
                    ImmunizationForm.MotherName, 
                    message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email");
            }
        }
    }
}
