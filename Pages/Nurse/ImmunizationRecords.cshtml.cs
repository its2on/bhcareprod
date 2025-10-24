using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    [Authorize(Policy = "PatientList")]
    public class ImmunizationRecordsModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly IImmunizationReminderService _immunizationReminderService;
        private readonly ILogger<ImmunizationRecordsModel> _logger;
        private readonly IDataEncryptionService _encryptionService;

        public ImmunizationRecordsModel(
            EncryptedDbContext context,
            IImmunizationReminderService immunizationReminderService,
            ILogger<ImmunizationRecordsModel> logger,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _immunizationReminderService = immunizationReminderService;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string SelectedBarangay { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string FamilyNumber { get; set; } = string.Empty;

        public List<ImmunizationRecord> Records { get; set; } = new List<ImmunizationRecord>();
        public List<SelectListItem> BarangayOptions { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Prepare barangay dropdown (158–161)
            BarangayOptions = new List<SelectListItem>
            {
                new SelectListItem("All Barangays", ""),
                new SelectListItem("158", "158"),
                new SelectListItem("159", "159"),
                new SelectListItem("160", "160"),
                new SelectListItem("161", "161"),
            };

            // Log user information for debugging
            _logger.LogInformation($"User Identity: {User.Identity?.Name}, IsAuthenticated: {User.Identity?.IsAuthenticated}");
            _logger.LogInformation($"User Roles: {string.Join(", ", User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value))}");
            _logger.LogInformation($"CanUserDecrypt: {_encryptionService.CanUserDecrypt(User)}");

            // Load records with AsNoTracking to avoid caching issues
            var records = await _context.ImmunizationRecords
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            _logger.LogInformation($"Loaded {records.Count} immunization records from database");

            // Decrypt all records for authorized users BEFORE applying search filters
            var decryptedRecords = new List<ImmunizationRecord>();
            
            foreach (var record in records)
            {
                try
                {
                    _logger.LogInformation($"Processing record ID: {record.Id}");
                    _logger.LogInformation($"  Before - FamilyNumber encrypted: {_encryptionService.IsEncrypted(record.FamilyNumber ?? "")}");
                    _logger.LogInformation($"  Before - ChildName encrypted: {_encryptionService.IsEncrypted(record.ChildName ?? "")}");
                    
                    // Force decryption by calling the method directly
                    var decryptedRecord = record.DecryptImmunizationData(_encryptionService, User);
                    
                    // Double-check and manually decrypt if still encrypted
                    if (_encryptionService.IsEncrypted(decryptedRecord.FamilyNumber ?? ""))
                    {
                        _logger.LogWarning($"  FamilyNumber still encrypted after DecryptImmunizationData, manually decrypting...");
                        decryptedRecord.FamilyNumber = _encryptionService.Decrypt(decryptedRecord.FamilyNumber);
                    }
                    
                    if (_encryptionService.IsEncrypted(decryptedRecord.ChildName ?? ""))
                    {
                        _logger.LogWarning($"  ChildName still encrypted after DecryptImmunizationData, manually decrypting...");
                        decryptedRecord.ChildName = _encryptionService.Decrypt(decryptedRecord.ChildName);
                    }
                    
                    if (_encryptionService.IsEncrypted(decryptedRecord.MotherName ?? ""))
                    {
                        decryptedRecord.MotherName = _encryptionService.Decrypt(decryptedRecord.MotherName);
                    }
                    
                    if (_encryptionService.IsEncrypted(decryptedRecord.FatherName ?? ""))
                    {
                        decryptedRecord.FatherName = _encryptionService.Decrypt(decryptedRecord.FatherName);
                    }
                    
                    if (_encryptionService.IsEncrypted(decryptedRecord.DateOfBirth ?? ""))
                    {
                        decryptedRecord.DateOfBirth = _encryptionService.Decrypt(decryptedRecord.DateOfBirth);
                    }
                    
                    if (_encryptionService.IsEncrypted(decryptedRecord.PlaceOfBirth ?? ""))
                    {
                        decryptedRecord.PlaceOfBirth = _encryptionService.Decrypt(decryptedRecord.PlaceOfBirth);
                    }
                    
                    if (_encryptionService.IsEncrypted(decryptedRecord.Address ?? ""))
                    {
                        decryptedRecord.Address = _encryptionService.Decrypt(decryptedRecord.Address);
                    }
                    
                    if (_encryptionService.IsEncrypted(decryptedRecord.Barangay ?? ""))
                    {
                        decryptedRecord.Barangay = _encryptionService.Decrypt(decryptedRecord.Barangay);
                    }
                    
                    if (_encryptionService.IsEncrypted(decryptedRecord.HealthCenter ?? ""))
                    {
                        decryptedRecord.HealthCenter = _encryptionService.Decrypt(decryptedRecord.HealthCenter);
                    }
                    
                    if (_encryptionService.IsEncrypted(decryptedRecord.Email ?? ""))
                    {
                        decryptedRecord.Email = _encryptionService.Decrypt(decryptedRecord.Email);
                    }
                    
                    if (_encryptionService.IsEncrypted(decryptedRecord.ContactNumber ?? ""))
                    {
                        decryptedRecord.ContactNumber = _encryptionService.Decrypt(decryptedRecord.ContactNumber);
                    }
                    
                    _logger.LogInformation($"  After - FamilyNumber: {decryptedRecord.FamilyNumber?.Substring(0, Math.Min(15, decryptedRecord.FamilyNumber?.Length ?? 0))}");
                    _logger.LogInformation($"  After - ChildName: {decryptedRecord.ChildName?.Substring(0, Math.Min(15, decryptedRecord.ChildName?.Length ?? 0))}");
                    _logger.LogInformation($"  After - FamilyNumber encrypted: {_encryptionService.IsEncrypted(decryptedRecord.FamilyNumber ?? "")}");
                    
                    decryptedRecords.Add(decryptedRecord);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to decrypt record ID: {record.Id}");
                    // Still add the record even if decryption fails
                    decryptedRecords.Add(record);
                }
            }
            
            Records = decryptedRecords;
            _logger.LogInformation($"Decrypted {Records.Count} records total");

            // Apply search filters AFTER decryption
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                Records = Records.Where(r => 
                    (!string.IsNullOrEmpty(r.ChildName) && r.ChildName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) || 
                    (!string.IsNullOrEmpty(r.MotherName) && r.MotherName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.FatherName) && r.FatherName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (!string.IsNullOrEmpty(SelectedBarangay))
            {
                // Normalize to handle both "161" and "Barangay 161"
                string norm(string? b) => (b ?? string.Empty).Trim().Replace("Barangay ", "", StringComparison.OrdinalIgnoreCase);
                var target = norm(SelectedBarangay);
                Records = Records.Where(r => norm(r.Barangay) == target).ToList();
            }

            if (!string.IsNullOrEmpty(FamilyNumber))
            {
                Records = Records.Where(r => !string.IsNullOrEmpty(r.FamilyNumber) && r.FamilyNumber.Contains(FamilyNumber, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            _logger.LogInformation($"Final filtered records count: {Records.Count}");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var record = await _context.ImmunizationRecords.FindAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            _context.ImmunizationRecords.Remove(record);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Immunization record deleted successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync(int id, string childName, string dateOfBirth, 
            string motherName, string fatherName, string sex, string address, string barangay, 
            string healthCenter, string email, string contactNumber,
            string bcgVaccineDate, string bcgVaccineRemarks,
            string hepatitisBVaccineDate, string hepatitisBVaccineRemarks,
            string pentavalent1Date, string pentavalent1Remarks,
            string pentavalent2Date, string pentavalent2Remarks,
            string pentavalent3Date, string pentavalent3Remarks,
            string opv1Date, string opv1Remarks,
            string opv2Date, string opv2Remarks,
            string opv3Date, string opv3Remarks,
            string ipv1Date, string ipv1Remarks,
            string ipv2Date, string ipv2Remarks,
            string pcv1Date, string pcv1Remarks,
            string pcv2Date, string pcv2Remarks,
            string pcv3Date, string pcv3Remarks,
            string mmr1Date, string mmr1Remarks,
            string mmr2Date, string mmr2Remarks)
        {
            var record = await _context.ImmunizationRecords.FindAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            // Update basic information
            record.ChildName = childName;
            record.DateOfBirth = dateOfBirth;
            record.MotherName = motherName;
            record.FatherName = fatherName ?? string.Empty;
            record.Sex = sex;
            record.Address = address ?? string.Empty;
            record.Barangay = barangay;
            record.HealthCenter = healthCenter ?? string.Empty;
            record.Email = email ?? string.Empty;
            record.ContactNumber = contactNumber ?? string.Empty;

            // Update vaccine information
            record.BCGVaccineDate = bcgVaccineDate;
            record.BCGVaccineRemarks = bcgVaccineRemarks ?? string.Empty;
            record.HepatitisBVaccineDate = hepatitisBVaccineDate;
            record.HepatitisBVaccineRemarks = hepatitisBVaccineRemarks ?? string.Empty;
            
            // Pentavalent doses
            record.Pentavalent1Date = pentavalent1Date;
            record.Pentavalent1Remarks = pentavalent1Remarks ?? string.Empty;
            record.Pentavalent2Date = pentavalent2Date;
            record.Pentavalent2Remarks = pentavalent2Remarks ?? string.Empty;
            record.Pentavalent3Date = pentavalent3Date;
            record.Pentavalent3Remarks = pentavalent3Remarks ?? string.Empty;
            
            // OPV doses
            record.OPV1Date = opv1Date;
            record.OPV1Remarks = opv1Remarks ?? string.Empty;
            record.OPV2Date = opv2Date;
            record.OPV2Remarks = opv2Remarks ?? string.Empty;
            record.OPV3Date = opv3Date;
            record.OPV3Remarks = opv3Remarks ?? string.Empty;
            
            // IPV doses
            record.IPV1Date = ipv1Date;
            record.IPV1Remarks = ipv1Remarks ?? string.Empty;
            record.IPV2Date = ipv2Date;
            record.IPV2Remarks = ipv2Remarks ?? string.Empty;
            
            // PCV doses
            record.PCV1Date = pcv1Date;
            record.PCV1Remarks = pcv1Remarks ?? string.Empty;
            record.PCV2Date = pcv2Date;
            record.PCV2Remarks = pcv2Remarks ?? string.Empty;
            record.PCV3Date = pcv3Date;
            record.PCV3Remarks = pcv3Remarks ?? string.Empty;
            
            // MMR doses
            record.MMR1Date = mmr1Date;
            record.MMR1Remarks = mmr1Remarks ?? string.Empty;
            record.MMR2Date = mmr2Date;
            record.MMR2Remarks = mmr2Remarks ?? string.Empty;

            record.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            record.UpdatedBy = User.Identity?.Name ?? "Unknown";

            await _context.SaveChangesAsync();

            // Send email notification if email is provided
            if (!string.IsNullOrEmpty(email))
            {
                try
                {
                    await _immunizationReminderService.SendVaccineUpdateNotificationAsync(email, childName, record);
                    TempData["SuccessMessage"] = $"Immunization record for {childName} updated successfully. Email notification sent to {email}.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send vaccine update notification email to {Email}", email);
                    TempData["SuccessMessage"] = $"Immunization record for {childName} updated successfully. However, email notification could not be sent.";
                }
            }
            else
            {
                TempData["SuccessMessage"] = $"Immunization record for {childName} updated successfully.";
            }

            return RedirectToPage();
        }
    }
}