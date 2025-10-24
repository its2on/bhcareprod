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
                    // Fall back to NCDRiskAssessment
                    var ncdAssessment = await _context.NCDRiskAssessments
                        .FirstOrDefaultAsync(n => n.UserId == patient.PatientId);
                    
                    if (ncdAssessment != null && !string.IsNullOrEmpty(ncdAssessment.FamilyNo))
                    {
                        // Decrypt the family number
                        ncdAssessment.DecryptSensitiveData(_encryptionService, User);
                        familyNumber = ncdAssessment.FamilyNo ?? "N/A";
                    }
                    else
                    {
                        // Fall back to HEEADSSSAssessment
                        var heeadsssAssessment = await _context.HEEADSSSAssessments
                            .FirstOrDefaultAsync(h => h.UserId == patient.PatientId);
                        
                        if (heeadsssAssessment != null && !string.IsNullOrEmpty(heeadsssAssessment.FamilyNo))
                        {
                            // Decrypt the family number
                            heeadsssAssessment.DecryptSensitiveData(_encryptionService, User);
                            familyNumber = heeadsssAssessment.FamilyNo ?? "N/A";
                        }
                    }
                }
                
                patient.FamilyNumber = familyNumber;
            }

            // Also include patients from Appointments (where BookingForOther = true)
            // These are guest patients who don't have their own Patient records
            var guestPatients = await _context.Appointments
                .Where(a => a.BookingForOther == true && 
                           !string.IsNullOrEmpty(a.FamilyNumber) &&
                           a.Status != AppointmentStatus.Cancelled)
                .Select(a => new PatientViewModel
                {
                    PatientId = a.Id.ToString(), // Use appointment ID as identifier
                    FullName = a.PatientName,
                    Email = "Guest Patient",
                    PhoneNumber = a.ContactNumber ?? "N/A",
                    Barangay = "Guest",
                    Status = "Guest Patient",
                    Age = a.AgeValue.ToString(),
                    FamilyNumber = a.FamilyNumber
                })
                .ToListAsync();

            // Combine registered patients and guest patients
            var allPatients = patients.Concat(guestPatients).ToList();

            Patients = allPatients;

            // Group patients by family number
            FamilyGroups = GroupPatientsByFamily(allPatients);

            return Page();
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

            return familyGroups.Values.OrderBy(f => f.FamilyNumber).ToList();
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