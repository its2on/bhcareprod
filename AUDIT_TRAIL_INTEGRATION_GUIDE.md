# 🔍 BHCare Audit Trail Integration Guide

## 📋 Quick Implementation Summary

### Phase 1: Core Setup (30 minutes)
1. Create AuditTrail model
2. Create AuditTrailService
3. Update ApplicationDbContext
4. Register service in Program.cs
5. Run migrations

### Phase 2: Integration Points (2-3 hours)
6. Add logging to Admin operations
7. Add logging to Doctor operations
8. Add logging to Nurse operations
9. Add logging to Patient operations
10. Add authentication event logging

### Phase 3: UI & Reports (1 hour)
11. Create audit trail viewer page
12. Add export functionality

---

## 🏗️ STEP 1: Create AuditTrail Model

**Create:** `Models/AuditTrail.cs`

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace Barangay.Models
{
    public class AuditTrail
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string PerformedBy { get; set; }
        public string UserId { get; set; }
        
        [Required]
        public string Role { get; set; }
        
        [Required]
        public string ActionType { get; set; } // Create, Update, Delete, View, Login, Logout
        
        [Required]
        public string Action { get; set; }
        
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string Description { get; set; }
        public string IPAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        
        public virtual ApplicationUser User { get; set; }
    }
}
```

---

## 🔧 STEP 2: Create AuditTrailService

**Create:** `Services/AuditTrailService.cs`

```csharp
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Barangay.Services
{
    public interface IAuditTrailService
    {
        Task LogAsync(string actionType, string action, string entityName, string entityId, 
                      string oldValues = null, string newValues = null, string description = null);
    }

    public class AuditTrailService : IAuditTrailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuditTrailService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, 
                                UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task LogAsync(string actionType, string action, string entityName, string entityId, 
                                   string oldValues = null, string newValues = null, string description = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return;

                var user = httpContext.User;
                var userName = user?.Identity?.Name ?? "System";
                var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
                var role = user?.FindFirstValue(ClaimTypes.Role) ?? user?.FindFirstValue("role") ?? "Unknown";
                var ipAddress = httpContext.Connection?.RemoteIpAddress?.ToString();

                var auditLog = new AuditTrail
                {
                    PerformedBy = userName,
                    UserId = userId,
                    Role = role,
                    ActionType = actionType,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Description = description,
                    IPAddress = ipAddress,
                    OldValues = oldValues,
                    NewValues = newValues
                };

                _context.AuditTrails.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audit logging error: {ex.Message}");
            }
        }
    }
}
```

---

## 💾 STEP 3: Update ApplicationDbContext

**File:** `Data/ApplicationDbContext.cs`

**Add after line 68:**
```csharp
public DbSet<AuditTrail> AuditTrails { get; set; }
```

**Add in OnModelCreating after line 424:**
```csharp
// Configure AuditTrail
builder.Entity<AuditTrail>()
    .HasOne(at => at.User)
    .WithMany()
    .HasForeignKey(at => at.UserId)
    .OnDelete(DeleteBehavior.NoAction)
    .IsRequired(false);

builder.Entity<AuditTrail>().HasIndex(at => at.Timestamp);
builder.Entity<AuditTrail>().HasIndex(at => at.EntityName);
builder.Entity<AuditTrail>().HasIndex(at => at.UserId);
```

---

## ⚙️ STEP 4: Register Service

**File:** `Program.cs` - Add after line 125:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditTrailService, AuditTrailService>();
```

---

## 🗄️ STEP 5: Run Migration

```powershell
dotnet ef migrations add AddAuditTrailSystem
dotnet ef database update
```

---

## 🎯 STEP 6-9: Integration Points by Role

### **ADMIN ROLE - Key Integration Points**

#### **1. User Creation** - `Pages/Admin/AddStaffMember.cshtml.cs`

**Inject service:**
```csharp
private readonly IAuditTrailService _auditTrail;

public AddStaffMemberModel(..., IAuditTrailService auditTrail)
{
    _auditTrail = auditTrail;
}
```

**After user creation (around line 400-450 in OnPostAsync):**
```csharp
await _auditTrail.LogAsync("Create", $"Created staff member: {user.Email}", 
    "ApplicationUser", user.Id, null, 
    JsonConvert.SerializeObject(new { Email = user.Email, FullName = user.FullName, Role = StaffMember.Role }),
    $"Admin created new {StaffMember.Role} account");
```

#### **2. Role Assignment** - `Pages/Admin/AssignRoles.cshtml.cs`
```csharp
await _auditTrail.LogAsync("Update", $"Changed user role to {newRole}", 
    "ApplicationUser", userId, oldRole, newRole, "Admin modified user role");
```

#### **3. Document Approval** - `Pages/Admin/UserVerification.cshtml.cs`
```csharp
await _auditTrail.LogAsync("Update", $"{(approved ? "Approved" : "Rejected")} document", 
    "UserDocument", documentId.ToString(), "Pending", approved ? "Approved" : "Rejected", 
    "Admin reviewed verification document");
```

---

### **DOCTOR ROLE - Key Integration Points**

#### **1. Add Prescription** - `Pages/Doctor/Prescriptions/AddMedication.cshtml.cs`

**Inject service:**
```csharp
private readonly IAuditTrailService _auditTrail;

public AddMedicationModel(..., IAuditTrailService auditTrail)
{
    _auditTrail = auditTrail;
}
```

**After line 127 (after SaveChangesAsync):**
```csharp
await _auditTrail.LogAsync("Create", $"Added prescription: {medication.Name}", 
    "PrescriptionMedication", prescriptionMedication.Id.ToString(), null,
    JsonConvert.SerializeObject(new { MedicationName = medication.Name, Dosage = Medication.Dosage, 
        Frequency = Medication.Frequency, PatientId = prescription.PatientId }),
    $"Doctor prescribed {medication.Name}");
```

#### **2. Medical Record** - `Pages/Doctor/Consultation.cshtml.cs`
```csharp
await _auditTrail.LogAsync("Create", "Created consultation record", "MedicalRecord", 
    medicalRecord.Id.ToString(), null, 
    JsonConvert.SerializeObject(new { PatientId = medicalRecord.PatientId, Diagnosis = medicalRecord.Diagnosis }),
    "Doctor completed consultation");
```

#### **3. View Patient Records** - `Pages/Doctor/PatientDetails.cshtml.cs` (OnGetAsync)
```csharp
await _auditTrail.LogAsync("View", "Viewed patient medical records", "Patient", patientId, 
    null, null, "Doctor accessed confidential medical information");
```

---

### **NURSE ROLE - Key Integration Points**

#### **1. Vital Signs** - `Pages/Nurse/VitalSigns.cshtml.cs`

**Inject service:**
```csharp
private readonly IAuditTrailService _auditTrail;

public VitalSignsModel(..., IAuditTrailService auditTrail)
{
    _auditTrail = auditTrail;
}
```

**After recording vital signs (in OnPostAsync after SaveChanges):**
```csharp
await _auditTrail.LogAsync("Create", "Recorded patient vital signs", "VitalSign", 
    vitalSign.Id.ToString(), null,
    JsonConvert.SerializeObject(new { PatientId = NewVitalSign.PatientId, 
        BloodPressure = NewVitalSign.BloodPressure, Temperature = NewVitalSign.Temperature }),
    "Nurse recorded vital signs");
```

#### **2. Immunization** - `Pages/Nurse/ImmunizationRecords.cshtml.cs`
```csharp
await _auditTrail.LogAsync("Create", $"Added immunization: {immunization.VaccineName}", 
    "ImmunizationRecord", immunization.Id.ToString(), null,
    JsonConvert.SerializeObject(new { VaccineName = immunization.VaccineName, DateAdministered = immunization.DateAdministered }),
    "Nurse administered vaccine");
```

---

### **PATIENT ROLE - Key Integration Points**

#### **1. Profile Update** - `Pages/User/Profile.cshtml.cs`
```csharp
await _auditTrail.LogAsync("Update", "Updated personal profile", "ApplicationUser", userId, 
    oldProfileJson, newProfileJson, "Patient updated personal information");
```

#### **2. Appointment Booking** - `Pages/BookAppointment.cshtml.cs`
```csharp
await _auditTrail.LogAsync("Create", $"Booked appointment with Dr. {doctorName}", "Appointment", 
    appointment.Id.ToString(), null,
    JsonConvert.SerializeObject(new { AppointmentDate = appointment.AppointmentDate, Type = appointment.Type }),
    "Patient booked new appointment");
```

#### **3. Assessment Submission** - `Pages/User/NCDRiskAssessment.cshtml.cs`
```csharp
await _auditTrail.LogAsync("Create", "Submitted NCD Risk Assessment", "NCDRiskAssessment", 
    assessment.Id.ToString(), null, "[Assessment data]", "Patient completed NCD assessment");
```

---

### **AUTHENTICATION EVENTS**

#### **Login** - `Pages/Account/Login.cshtml.cs` (after successful login)
```csharp
await _auditTrail.LogAsync("Login", "User logged in successfully", "Authentication", 
    user.Id, null, null, $"User {user.Email} logged into system");
```

#### **Logout** - `Pages/Account/Logout.cshtml.cs` (before logout)
```csharp
await _auditTrail.LogAsync("Logout", "User logged out", "Authentication", 
    User.FindFirstValue(ClaimTypes.NameIdentifier), null, null, "User ended session");
```

#### **Failed Login** - `Pages/Account/Login.cshtml.cs` (after failed attempt)
```csharp
await _auditTrail.LogAsync("LoginFailed", $"Failed login attempt for {email}", "Authentication", 
    null, null, null, $"Invalid login attempt from IP: {ipAddress}");
```

---

## 📊 STEP 10-11: Create Audit Trail Viewer

See separate file: `Pages/Admin/AuditTrail.cshtml.cs` implementation needed.

**Key Features:**
- Filter by role, action type, date range, user
- Pagination (50 records per page)
- Search functionality
- View change details (old vs new values)
- Export to CSV/PDF
- Role-based badge colors
- IP address tracking

---

## 🔐 Security & Privacy Considerations

### **1. Data Retention Policy**
```sql
-- Archive logs older than 1 year
CREATE PROCEDURE ArchiveOldAuditLogs
AS
BEGIN
    DELETE FROM AuditTrails WHERE Timestamp < DATEADD(YEAR, -1, GETDATE())
END
```

### **2. Sensitive Data Handling**
- **DO NOT** log passwords or encryption keys
- **DO NOT** log full SSN or credit card numbers
- **Mask** sensitive fields in OldValues/NewValues JSON
- **Encrypt** IPAddress field if required by regulations

### **3. Access Control**
- Only Admin role can view audit logs
- Implement audit log for audit log access (meta-audit)
- Consider read-only database user for audit queries

---

## ⚡ Performance Optimizations

### **1. Async Fire-and-Forget** (Optional)
```csharp
// For high-traffic scenarios, log asynchronously
Task.Run(async () => await _auditTrail.LogAsync(...)).ConfigureAwait(false);
```

### **2. Database Indexes** (Already included)
- Index on Timestamp (for date range queries)
- Index on UserId (for user-specific queries)
- Index on EntityName (for entity-specific queries)

### **3. Archiving Strategy**
- Move logs older than 6 months to archive table
- Use partitioning for large datasets
- Implement cleanup job

---

## 📈 Summary of Database Changes

### **New Table: AuditTrails**
- Primary Key: Id (int, auto-increment)
- Foreign Key: UserId → AspNetUsers.Id
- Indexes: Timestamp, UserId, EntityName, ActionType

### **Migration Command:**
```powershell
dotnet ef migrations add AddAuditTrailSystem
dotnet ef database update
```

---

## ✅ Integration Checklist

- [ ] AuditTrail model created
- [ ] AuditTrailService created and registered
- [ ] ApplicationDbContext updated
- [ ] Migration run successfully
- [ ] Admin operations logging added
- [ ] Doctor operations logging added
- [ ] Nurse operations logging added
- [ ] Patient operations logging added
- [ ] Authentication events logging added
- [ ] Audit Trail viewer page created
- [ ] Export functionality implemented
- [ ] Security review completed
- [ ] Performance testing done

---

## 🚀 Quick Start Commands

```powershell
# 1. Create migration
dotnet ef migrations add AddAuditTrailSystem

# 2. Update database
dotnet ef database update

# 3. Test audit logging
# Navigate to any page and perform an action, then check Admin/AuditTrail page
```

---

## 📞 Support & Enhancements

### **Potential Enhancements:**
1. **Real-time notifications** - Alert admins of suspicious activities
2. **Analytics dashboard** - Visualize audit data with charts
3. **Compliance reports** - Generate HIPAA/GDPR compliance reports
4. **Advanced search** - Full-text search across all fields
5. **Audit log integrity** - Implement hash chains to prevent tampering
6. **API audit** - Log all API endpoint calls
7. **File access audit** - Track document downloads and views

---

**End of Integration Guide**
