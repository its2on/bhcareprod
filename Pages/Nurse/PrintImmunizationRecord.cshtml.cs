using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    public class PrintImmunizationRecordModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly IDataEncryptionService _encryptionService;
        private readonly ILogger<PrintImmunizationRecordModel> _logger;

        public PrintImmunizationRecordModel(
            EncryptedDbContext context,
            IDataEncryptionService encryptionService,
            ILogger<PrintImmunizationRecordModel> logger)
        {
            _context = context;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public ImmunizationRecord? Record { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Loading immunization record {id} for printing");

                // Clear any potential caching
                _context.ChangeTracker.Clear();

                // Load the record
                Record = await _context.ImmunizationRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (Record == null)
                {
                    _logger.LogWarning($"Immunization record {id} not found");
                    return NotFound();
                }

                // Decrypt all fields for printing
                _logger.LogInformation($"Decrypting immunization record {id}");
                
                // Decrypt basic information
                if (!string.IsNullOrEmpty(Record.ChildName) && _encryptionService.IsEncrypted(Record.ChildName))
                    Record.ChildName = _encryptionService.Decrypt(Record.ChildName);

                if (!string.IsNullOrEmpty(Record.FamilyNumber) && _encryptionService.IsEncrypted(Record.FamilyNumber))
                    Record.FamilyNumber = _encryptionService.Decrypt(Record.FamilyNumber);

                if (!string.IsNullOrEmpty(Record.DateOfBirth) && _encryptionService.IsEncrypted(Record.DateOfBirth))
                {
                    var decryptedDate = _encryptionService.Decrypt(Record.DateOfBirth);
                    if (DateTime.TryParse(decryptedDate, out DateTime parsedDate))
                    {
                        Record.DateOfBirth = parsedDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        Record.DateOfBirth = decryptedDate;
                    }
                }

                if (!string.IsNullOrEmpty(Record.MotherName) && _encryptionService.IsEncrypted(Record.MotherName))
                    Record.MotherName = _encryptionService.Decrypt(Record.MotherName);

                if (!string.IsNullOrEmpty(Record.FatherName) && _encryptionService.IsEncrypted(Record.FatherName))
                    Record.FatherName = _encryptionService.Decrypt(Record.FatherName);

                if (!string.IsNullOrEmpty(Record.Sex) && _encryptionService.IsEncrypted(Record.Sex))
                    Record.Sex = _encryptionService.Decrypt(Record.Sex);

                if (!string.IsNullOrEmpty(Record.BirthHeight) && _encryptionService.IsEncrypted(Record.BirthHeight))
                    Record.BirthHeight = _encryptionService.Decrypt(Record.BirthHeight);

                if (!string.IsNullOrEmpty(Record.BirthWeight) && _encryptionService.IsEncrypted(Record.BirthWeight))
                    Record.BirthWeight = _encryptionService.Decrypt(Record.BirthWeight);

                if (!string.IsNullOrEmpty(Record.PlaceOfBirth) && _encryptionService.IsEncrypted(Record.PlaceOfBirth))
                    Record.PlaceOfBirth = _encryptionService.Decrypt(Record.PlaceOfBirth);

                if (!string.IsNullOrEmpty(Record.Address) && _encryptionService.IsEncrypted(Record.Address))
                    Record.Address = _encryptionService.Decrypt(Record.Address);

                if (!string.IsNullOrEmpty(Record.HealthCenter) && _encryptionService.IsEncrypted(Record.HealthCenter))
                    Record.HealthCenter = _encryptionService.Decrypt(Record.HealthCenter);

                if (!string.IsNullOrEmpty(Record.Barangay) && _encryptionService.IsEncrypted(Record.Barangay))
                    Record.Barangay = _encryptionService.Decrypt(Record.Barangay);

                if (!string.IsNullOrEmpty(Record.Email) && _encryptionService.IsEncrypted(Record.Email))
                    Record.Email = _encryptionService.Decrypt(Record.Email);

                if (!string.IsNullOrEmpty(Record.ContactNumber) && _encryptionService.IsEncrypted(Record.ContactNumber))
                    Record.ContactNumber = _encryptionService.Decrypt(Record.ContactNumber);

                // Decrypt vaccine information
                if (!string.IsNullOrEmpty(Record.BCGVaccineDate) && _encryptionService.IsEncrypted(Record.BCGVaccineDate))
                    Record.BCGVaccineDate = _encryptionService.Decrypt(Record.BCGVaccineDate);

                if (!string.IsNullOrEmpty(Record.BCGVaccineRemarks) && _encryptionService.IsEncrypted(Record.BCGVaccineRemarks))
                    Record.BCGVaccineRemarks = _encryptionService.Decrypt(Record.BCGVaccineRemarks);

                if (!string.IsNullOrEmpty(Record.HepatitisBVaccineDate) && _encryptionService.IsEncrypted(Record.HepatitisBVaccineDate))
                    Record.HepatitisBVaccineDate = _encryptionService.Decrypt(Record.HepatitisBVaccineDate);

                if (!string.IsNullOrEmpty(Record.HepatitisBVaccineRemarks) && _encryptionService.IsEncrypted(Record.HepatitisBVaccineRemarks))
                    Record.HepatitisBVaccineRemarks = _encryptionService.Decrypt(Record.HepatitisBVaccineRemarks);

                // Pentavalent
                if (!string.IsNullOrEmpty(Record.Pentavalent1Date) && _encryptionService.IsEncrypted(Record.Pentavalent1Date))
                    Record.Pentavalent1Date = _encryptionService.Decrypt(Record.Pentavalent1Date);

                if (!string.IsNullOrEmpty(Record.Pentavalent1Remarks) && _encryptionService.IsEncrypted(Record.Pentavalent1Remarks))
                    Record.Pentavalent1Remarks = _encryptionService.Decrypt(Record.Pentavalent1Remarks);

                if (!string.IsNullOrEmpty(Record.Pentavalent2Date) && _encryptionService.IsEncrypted(Record.Pentavalent2Date))
                    Record.Pentavalent2Date = _encryptionService.Decrypt(Record.Pentavalent2Date);

                if (!string.IsNullOrEmpty(Record.Pentavalent2Remarks) && _encryptionService.IsEncrypted(Record.Pentavalent2Remarks))
                    Record.Pentavalent2Remarks = _encryptionService.Decrypt(Record.Pentavalent2Remarks);

                if (!string.IsNullOrEmpty(Record.Pentavalent3Date) && _encryptionService.IsEncrypted(Record.Pentavalent3Date))
                    Record.Pentavalent3Date = _encryptionService.Decrypt(Record.Pentavalent3Date);

                if (!string.IsNullOrEmpty(Record.Pentavalent3Remarks) && _encryptionService.IsEncrypted(Record.Pentavalent3Remarks))
                    Record.Pentavalent3Remarks = _encryptionService.Decrypt(Record.Pentavalent3Remarks);

                // OPV
                if (!string.IsNullOrEmpty(Record.OPV1Date) && _encryptionService.IsEncrypted(Record.OPV1Date))
                    Record.OPV1Date = _encryptionService.Decrypt(Record.OPV1Date);

                if (!string.IsNullOrEmpty(Record.OPV1Remarks) && _encryptionService.IsEncrypted(Record.OPV1Remarks))
                    Record.OPV1Remarks = _encryptionService.Decrypt(Record.OPV1Remarks);

                if (!string.IsNullOrEmpty(Record.OPV2Date) && _encryptionService.IsEncrypted(Record.OPV2Date))
                    Record.OPV2Date = _encryptionService.Decrypt(Record.OPV2Date);

                if (!string.IsNullOrEmpty(Record.OPV2Remarks) && _encryptionService.IsEncrypted(Record.OPV2Remarks))
                    Record.OPV2Remarks = _encryptionService.Decrypt(Record.OPV2Remarks);

                if (!string.IsNullOrEmpty(Record.OPV3Date) && _encryptionService.IsEncrypted(Record.OPV3Date))
                    Record.OPV3Date = _encryptionService.Decrypt(Record.OPV3Date);

                if (!string.IsNullOrEmpty(Record.OPV3Remarks) && _encryptionService.IsEncrypted(Record.OPV3Remarks))
                    Record.OPV3Remarks = _encryptionService.Decrypt(Record.OPV3Remarks);

                // IPV
                if (!string.IsNullOrEmpty(Record.IPV1Date) && _encryptionService.IsEncrypted(Record.IPV1Date))
                    Record.IPV1Date = _encryptionService.Decrypt(Record.IPV1Date);

                if (!string.IsNullOrEmpty(Record.IPV1Remarks) && _encryptionService.IsEncrypted(Record.IPV1Remarks))
                    Record.IPV1Remarks = _encryptionService.Decrypt(Record.IPV1Remarks);

                if (!string.IsNullOrEmpty(Record.IPV2Date) && _encryptionService.IsEncrypted(Record.IPV2Date))
                    Record.IPV2Date = _encryptionService.Decrypt(Record.IPV2Date);

                if (!string.IsNullOrEmpty(Record.IPV2Remarks) && _encryptionService.IsEncrypted(Record.IPV2Remarks))
                    Record.IPV2Remarks = _encryptionService.Decrypt(Record.IPV2Remarks);

                // PCV
                if (!string.IsNullOrEmpty(Record.PCV1Date) && _encryptionService.IsEncrypted(Record.PCV1Date))
                    Record.PCV1Date = _encryptionService.Decrypt(Record.PCV1Date);

                if (!string.IsNullOrEmpty(Record.PCV1Remarks) && _encryptionService.IsEncrypted(Record.PCV1Remarks))
                    Record.PCV1Remarks = _encryptionService.Decrypt(Record.PCV1Remarks);

                if (!string.IsNullOrEmpty(Record.PCV2Date) && _encryptionService.IsEncrypted(Record.PCV2Date))
                    Record.PCV2Date = _encryptionService.Decrypt(Record.PCV2Date);

                if (!string.IsNullOrEmpty(Record.PCV2Remarks) && _encryptionService.IsEncrypted(Record.PCV2Remarks))
                    Record.PCV2Remarks = _encryptionService.Decrypt(Record.PCV2Remarks);

                if (!string.IsNullOrEmpty(Record.PCV3Date) && _encryptionService.IsEncrypted(Record.PCV3Date))
                    Record.PCV3Date = _encryptionService.Decrypt(Record.PCV3Date);

                if (!string.IsNullOrEmpty(Record.PCV3Remarks) && _encryptionService.IsEncrypted(Record.PCV3Remarks))
                    Record.PCV3Remarks = _encryptionService.Decrypt(Record.PCV3Remarks);

                // MMR
                if (!string.IsNullOrEmpty(Record.MMR1Date) && _encryptionService.IsEncrypted(Record.MMR1Date))
                    Record.MMR1Date = _encryptionService.Decrypt(Record.MMR1Date);

                if (!string.IsNullOrEmpty(Record.MMR1Remarks) && _encryptionService.IsEncrypted(Record.MMR1Remarks))
                    Record.MMR1Remarks = _encryptionService.Decrypt(Record.MMR1Remarks);

                if (!string.IsNullOrEmpty(Record.MMR2Date) && _encryptionService.IsEncrypted(Record.MMR2Date))
                    Record.MMR2Date = _encryptionService.Decrypt(Record.MMR2Date);

                if (!string.IsNullOrEmpty(Record.MMR2Remarks) && _encryptionService.IsEncrypted(Record.MMR2Remarks))
                    Record.MMR2Remarks = _encryptionService.Decrypt(Record.MMR2Remarks);

                // Audit fields
                if (!string.IsNullOrEmpty(Record.CreatedBy) && _encryptionService.IsEncrypted(Record.CreatedBy))
                    Record.CreatedBy = _encryptionService.Decrypt(Record.CreatedBy);

                if (!string.IsNullOrEmpty(Record.UpdatedBy) && _encryptionService.IsEncrypted(Record.UpdatedBy))
                    Record.UpdatedBy = _encryptionService.Decrypt(Record.UpdatedBy);

                if (!string.IsNullOrEmpty(Record.CreatedAt) && _encryptionService.IsEncrypted(Record.CreatedAt))
                    Record.CreatedAt = _encryptionService.Decrypt(Record.CreatedAt);

                if (!string.IsNullOrEmpty(Record.UpdatedAt) && _encryptionService.IsEncrypted(Record.UpdatedAt))
                    Record.UpdatedAt = _encryptionService.Decrypt(Record.UpdatedAt);

                if (!string.IsNullOrEmpty(Record.Status) && _encryptionService.IsEncrypted(Record.Status))
                    Record.Status = _encryptionService.Decrypt(Record.Status);

                _logger.LogInformation($"Successfully loaded and decrypted immunization record {id}");

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading immunization record {id} for printing");
                return NotFound();
            }
        }
    }
}
