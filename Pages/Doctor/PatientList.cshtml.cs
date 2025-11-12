using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Barangay.Pages.Doctor
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Doctor,Head Doctor")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "DoctorPatientList")]
    public class PatientListModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataEncryptionService _encryptionService;
        private readonly List<string> _predefinedBarangays = new List<string>
        {
            "Barangay 158",
            "Barangay 159",
            "Barangay 160",
            "Barangay 161"
        };

        public PatientListModel(ApplicationDbContext context, IDataEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedBarangay { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }

        public List<SelectListItem> BarangayList { get; set; }
        public IList<PatientViewModel> Patients { get; set; }
        public IList<FamilyGroupViewModel> FamilyGroups { get; set; }
        
        // Single match detection for refined search
        public bool IsSingleMatch { get; set; }
        public string SingleMatchPatientId { get; set; }
        public int TotalMatches { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get list of unique barangays from database (use Barangay only)
            var databaseBarangays = await _context.Patients
                .Select(p => p.User.Barangay)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .ToListAsync();

            // Combine predefined barangays with database barangays
            var allBarangays = _predefinedBarangays
                .Union(databaseBarangays)
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            BarangayList = allBarangays.Select(b => new SelectListItem { Value = b, Text = b }).ToList();

            // Build query
            var query = _context.Patients
                .Include(p => p.User)
                .Where(p => p.UserId != "0e03f06e-ba88-46ed-b047-4974d8b8252a" && p.FullName != "System Administrator")
                .AsQueryable();

            // Exclude staff members (doctor, nurse, admin) from patient list
            var staffUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == _context.Roles.Where(r => r.Name == "Doctor").Select(r => r.Id).FirstOrDefault()
                    || ur.RoleId == _context.Roles.Where(r => r.Name == "Nurse").Select(r => r.Id).FirstOrDefault()
                    || ur.RoleId == _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefault())
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            query = query.Where(p => !staffUserIds.Contains(p.UserId));

            // Apply filters
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                query = query.Where(p =>
                    p.User.FullName.Contains(SearchQuery) ||
                    p.User.Email.Contains(SearchQuery) ||
                    p.User.PhilHealthId.Contains(SearchQuery));
            }

            if (!string.IsNullOrEmpty(SelectedBarangay))
            {
                query = query.Where(p => p.User.Barangay == SelectedBarangay);
            }

            if (!string.IsNullOrEmpty(SelectedStatus))
            {
                query = query.Where(p => p.User.Status == SelectedStatus);
            }

            // Calculate pagination
            var totalPatients = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalPatients / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages));

            // Get paginated results
            var patients = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new PatientViewModel
                {
                    PatientId = p.UserId,
                    FullName = p.User.FullName,
                    Email = p.User.Email,
                    PhoneNumber = p.User.PhoneNumber,
                    Barangay = string.IsNullOrEmpty(p.User.Barangay) ? "Not specified" : p.User.Barangay,
                    Status = p.User.Status,
                    Age = p.User.Age
                })
                .ToListAsync();

            // Decrypt patient data for authorized users and retrieve family numbers
            foreach (var patient in patients)
            {
                // Get the full user object to decrypt
                var user = await _context.Users.FindAsync(patient.PatientId);
                if (user != null)
                {
                    // Decrypt user data
                    user.DecryptSensitiveData(_encryptionService, User);
                    
                    // Update the view model with decrypted data
                    patient.FullName = user.FullName;
                    patient.Email = user.Email;
                    patient.PhoneNumber = user.PhoneNumber;
                    patient.Age = user.Age;
                }

                // Retrieve family number from Patient record first, then fall back to assessments
                string familyNumber = "N/A";
                
                // Check Patient record first (primary source)
                var patientRecord = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == patient.PatientId);
                
                if (patientRecord != null && !string.IsNullOrEmpty(patientRecord.FamilyNumber))
                {
                    familyNumber = patientRecord.FamilyNumber;
                }
                else
                {
                    // Fall back to NCDRiskAssessment (get most recent)
                    var ncdAssessment = await _context.NCDRiskAssessments
                        .Where(n => n.UserId == patient.PatientId && !string.IsNullOrEmpty(n.FamilyNo))
                        .OrderByDescending(n => n.CreatedAt)
                        .FirstOrDefaultAsync();
                    
                    if (ncdAssessment != null)
                    {
                        // FamilyNo is plain text now - no decryption needed
                        familyNumber = ncdAssessment.FamilyNo ?? "N/A";
                    }
                    else
                    {
                        // Fall back to HEEADSSSAssessment (get most recent)
                        var heeadsssAssessment = await _context.HEEADSSSAssessments
                            .Where(h => h.UserId == patient.PatientId && !string.IsNullOrEmpty(h.FamilyNo))
                            .OrderByDescending(h => h.CreatedAt)
                            .FirstOrDefaultAsync();
                        
                        if (heeadsssAssessment != null)
                        {
                            // FamilyNo is plain text now - no decryption needed
                            familyNumber = heeadsssAssessment.FamilyNo ?? "N/A";
                        }
                    }
                }
                
                patient.FamilyNumber = familyNumber;
            }

            // Also include patients from Appointments (where BookingForOther = true)
            // These are guest patients who don't have their own Patient records
            // First get all guest appointments, then deduplicate in memory
            var allGuestAppointments = await _context.Appointments
                .Where(a => a.BookingForOther == true && 
                           !string.IsNullOrEmpty(a.FamilyNumber) &&
                           a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();
            
            // Decrypt patient names if encrypted
            foreach (var appointment in allGuestAppointments)
            {
                if (!string.IsNullOrEmpty(appointment.PatientName) && _encryptionService.IsEncrypted(appointment.PatientName))
                {
                    appointment.PatientName = _encryptionService.DecryptForUser(appointment.PatientName, User);
                }
            }
            
            // Convert to PatientViewModel and deduplicate
            var guestPatients = allGuestAppointments
                .Select(a => new PatientViewModel
                {
                    PatientId = a.Id.ToString(),
                    FullName = a.PatientName, // Now decrypted
                    Email = "Guest Patient",
                    PhoneNumber = a.ContactNumber ?? "N/A",
                    Barangay = "Guest",
                    Status = "Guest Patient",
                    Age = a.AgeValue.ToString(),
                    FamilyNumber = a.FamilyNumber
                })
                .GroupBy(p => new { p.FullName, p.FamilyNumber })
                .Select(g => g.First())
                .ToList();

            // Combine registered patients and guest patients
            var allPatients = patients.Concat(guestPatients).ToList();

            Patients = allPatients;

            // Group patients by family number
            FamilyGroups = GroupPatientsByFamily(allPatients);
            
            // Detect single match for refined search
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                TotalMatches = allPatients.Count;
                IsSingleMatch = TotalMatches == 1;
                
                if (IsSingleMatch)
                {
                    SingleMatchPatientId = allPatients.First().PatientId;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnGetSearchAsync(string searchQuery, string selectedBarangay, string selectedStatus)
        {
            // Build query (same logic as OnGetAsync)
            var query = _context.Patients
                .Include(p => p.User)
                .Where(p => p.UserId != "0e03f06e-ba88-46ed-b047-4974d8b8252a" && p.FullName != "System Administrator")
                .AsQueryable();

            // Exclude staff members
            var staffUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == _context.Roles.Where(r => r.Name == "Doctor").Select(r => r.Id).FirstOrDefault()
                    || ur.RoleId == _context.Roles.Where(r => r.Name == "Nurse").Select(r => r.Id).FirstOrDefault()
                    || ur.RoleId == _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefault())
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            query = query.Where(p => !staffUserIds.Contains(p.UserId));

            // Apply filters
            if (!string.IsNullOrEmpty(searchQuery))
            {
                // Search by patient name, email, PhilHealthId, or FamilyNumber
                // Also search in NCD and HEEADSSS assessments for family numbers
                var ncdFamilyNumbers = await _context.NCDRiskAssessments
                    .Where(n => n.FamilyNo != null && n.FamilyNo.Contains(searchQuery))
                    .Select(n => n.UserId)
                    .Distinct()
                    .ToListAsync();
                
                var heeadsssFamilyNumbers = await _context.HEEADSSSAssessments
                    .Where(h => h.FamilyNo != null && h.FamilyNo.Contains(searchQuery))
                    .Select(h => h.UserId)
                    .Distinct()
                    .ToListAsync();
                
                var matchingUserIds = ncdFamilyNumbers.Union(heeadsssFamilyNumbers).ToList();
                
                query = query.Where(p =>
                    p.User.FullName.Contains(searchQuery) ||
                    p.User.Email.Contains(searchQuery) ||
                    p.User.PhilHealthId.Contains(searchQuery) ||
                    (p.FamilyNumber != null && p.FamilyNumber.Contains(searchQuery)) ||
                    matchingUserIds.Contains(p.UserId));
            }

            if (!string.IsNullOrEmpty(selectedBarangay))
            {
                query = query.Where(p => p.User.Barangay == selectedBarangay);
            }

            if (!string.IsNullOrEmpty(selectedStatus))
            {
                query = query.Where(p => p.User.Status == selectedStatus);
            }

            // Get results
            var patients = await query
                .Select(p => new PatientViewModel
                {
                    PatientId = p.UserId,
                    FullName = p.User.FullName,
                    Email = p.User.Email,
                    PhoneNumber = p.User.PhoneNumber,
                    Barangay = string.IsNullOrEmpty(p.User.Barangay) ? "Not specified" : p.User.Barangay,
                    Status = p.User.Status,
                    Age = p.User.Age
                })
                .ToListAsync();

            // Decrypt patient data
            foreach (var patient in patients)
            {
                var user = await _context.Users.FindAsync(patient.PatientId);
                if (user != null)
                {
                    user.DecryptSensitiveData(_encryptionService, User);
                    patient.FullName = user.FullName;
                    patient.Email = user.Email;
                    patient.PhoneNumber = user.PhoneNumber;
                    patient.Age = user.Age;
                }

                // Get family number (same logic as OnGetAsync)
                string familyNumber = "N/A";
                
                // Check Patient record first (primary source)
                var patientRecord = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == patient.PatientId);
                
                if (patientRecord != null && !string.IsNullOrEmpty(patientRecord.FamilyNumber))
                {
                    familyNumber = patientRecord.FamilyNumber;
                }
                else
                {
                    // Fall back to NCDRiskAssessment (get most recent)
                    var ncdAssessment = await _context.NCDRiskAssessments
                        .Where(n => n.UserId == patient.PatientId && !string.IsNullOrEmpty(n.FamilyNo))
                        .OrderByDescending(n => n.CreatedAt)
                        .FirstOrDefaultAsync();
                    
                    if (ncdAssessment != null)
                    {
                        familyNumber = ncdAssessment.FamilyNo ?? "N/A";
                    }
                    else
                    {
                        // Fall back to HEEADSSSAssessment (get most recent)
                        var heeadsssAssessment = await _context.HEEADSSSAssessments
                            .Where(h => h.UserId == patient.PatientId && !string.IsNullOrEmpty(h.FamilyNo))
                            .OrderByDescending(h => h.CreatedAt)
                            .FirstOrDefaultAsync();
                        
                        if (heeadsssAssessment != null)
                        {
                            familyNumber = heeadsssAssessment.FamilyNo ?? "N/A";
                        }
                    }
                }
                
                patient.FamilyNumber = familyNumber;
            }

            // Include guest patients
            var guestPatientsQuery = _context.Appointments
                .Where(a => a.BookingForOther == true && 
                           !string.IsNullOrEmpty(a.FamilyNumber) &&
                           a.Status != AppointmentStatus.Cancelled)
                .AsQueryable();

            // Apply search query to guest patients if provided
            if (!string.IsNullOrEmpty(searchQuery))
            {
                guestPatientsQuery = guestPatientsQuery.Where(a =>
                    a.PatientName.Contains(searchQuery) ||
                    (a.ContactNumber != null && a.ContactNumber.Contains(searchQuery)) ||
                    (a.FamilyNumber != null && a.FamilyNumber.Contains(searchQuery)));
            }

            var guestPatients = await guestPatientsQuery
                .Select(a => new PatientViewModel
                {
                    PatientId = a.Id.ToString(),
                    FullName = a.PatientName,
                    Email = "Guest Patient",
                    PhoneNumber = a.ContactNumber ?? "N/A",
                    Barangay = "Guest",
                    Status = "Guest Patient",
                    Age = a.AgeValue.ToString(),
                    FamilyNumber = a.FamilyNumber
                })
                .ToListAsync();

            var allPatients = patients.Concat(guestPatients).ToList();

            // Group by family
            var familyGroups = GroupPatientsByFamily(allPatients);

            // Filter family groups by search query if needed (additional filtering after grouping)
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var searchLower = searchQuery.ToLower();
                familyGroups = familyGroups.Where(fg =>
                    (fg.FamilyNumber != null && fg.FamilyNumber.ToLower().Contains(searchLower)) ||
                    fg.FamilyMembers.Any(m => 
                        (m.FullName != null && m.FullName.ToLower().Contains(searchLower)) ||
                        (m.PhoneNumber != null && m.PhoneNumber.Contains(searchQuery)) ||
                        (m.Email != null && m.Email.ToLower().Contains(searchLower))
                    )
                ).ToList();
            }
            
            // Calculate match info for single-match detection
            var totalMatches = allPatients.Count;
            var isSingleMatch = totalMatches == 1;
            var singleMatchPatientId = isSingleMatch ? allPatients.First().PatientId : null;

            return new JsonResult(new { 
                familyGroups = familyGroups,
                totalMatches = totalMatches,
                isSingleMatch = isSingleMatch,
                singleMatchPatientId = singleMatchPatientId
            });
        }

        public string GetPageUrl(int pageNumber)
        {
            var queryParams = new Dictionary<string, string>
            {
                { "currentPage", pageNumber.ToString() }
            };

            if (!string.IsNullOrEmpty(SearchQuery))
                queryParams.Add("searchQuery", SearchQuery);

            if (!string.IsNullOrEmpty(SelectedBarangay))
                queryParams.Add("selectedBarangay", SelectedBarangay);

            if (!string.IsNullOrEmpty(SelectedStatus))
                queryParams.Add("selectedStatus", SelectedStatus);

            return Url.Page("./PatientList", queryParams);
        }

        private IList<FamilyGroupViewModel> GroupPatientsByFamily(IList<PatientViewModel> patients)
        {
            var familyGroups = new Dictionary<string, FamilyGroupViewModel>();

            foreach (var patient in patients)
            {
                var familyNumber = patient.FamilyNumber;
                
                // Skip patients without family numbers
                if (string.IsNullOrEmpty(familyNumber) || familyNumber == "N/A")
                    continue;

                if (!familyGroups.ContainsKey(familyNumber))
                {
                    familyGroups[familyNumber] = new FamilyGroupViewModel
                    {
                        FamilyNumber = familyNumber,
                        FamilyMembers = new List<PatientViewModel>(),
                        PrimaryContact = patient.PhoneNumber,
                        PrimaryBarangay = patient.Barangay
                    };
                }

                familyGroups[familyNumber].FamilyMembers.Add(patient);
                
                // Update primary contact and barangay if this patient has better info
                if (string.IsNullOrEmpty(familyGroups[familyNumber].PrimaryContact) && !string.IsNullOrEmpty(patient.PhoneNumber))
                {
                    familyGroups[familyNumber].PrimaryContact = patient.PhoneNumber;
                }
                
                if (string.IsNullOrEmpty(familyGroups[familyNumber].PrimaryBarangay) && !string.IsNullOrEmpty(patient.Barangay))
                {
                    familyGroups[familyNumber].PrimaryBarangay = patient.Barangay;
                }
            }
            
            // Try to decrypt any encrypted family numbers
            var result = familyGroups.Values.OrderBy(f => f.FamilyNumber).ToList();
            foreach (var group in result)
            {
                // Only try to decrypt if the family number appears to be encrypted (Base64 format)
                if (!string.IsNullOrEmpty(group.FamilyNumber) && 
                    group.FamilyNumber.Length > 20 &&
                    _encryptionService.IsEncrypted(group.FamilyNumber))
                {
                    try
                    {
                        // Try decryption
                        var decrypted = _encryptionService.Decrypt(group.FamilyNumber);
                        if (!string.IsNullOrEmpty(decrypted) && decrypted != group.FamilyNumber)
                        {
                            group.FamilyNumber = decrypted;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but continue with other records
                        Console.WriteLine($"Error decrypting family number: {ex.Message}");
                    }
                }
            }

            return result;
        }
    }

    public class PatientViewModel
    {
        public string PatientId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Barangay { get; set; }
        public string Status { get; set; }
        public string Age { get; set; }
        public string FamilyNumber { get; set; }
    }

    public class FamilyGroupViewModel
    {
        public string FamilyNumber { get; set; }
        public List<PatientViewModel> FamilyMembers { get; set; } = new List<PatientViewModel>();
        public string PrimaryContact { get; set; }
        public string PrimaryBarangay { get; set; }
        public int MemberCount => FamilyMembers.Count;
    }
} 