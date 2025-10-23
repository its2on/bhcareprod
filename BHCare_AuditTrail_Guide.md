# 🧾 Comprehensive Audit Trail Implementation (BHCare System)

## 📘 Overview
This Audit Trail feature records **who did what, when, where, and to whom** — across **all user roles** in BHCare:
- **System Administrator**
- **Doctor**
- **Nurse**
- **Patient/User**

It ensures transparency, accountability, and compliance with healthcare data security standards.

---

## 🧱 Step 1: Create the Enhanced AuditTrail Model

**File:** `Models/AuditTrail.cs`

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace BHCare.Models
{
    public class AuditTrail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PerformedBy { get; set; }

        public string Role { get; set; }

        [Required]
        public string ActionType { get; set; }

        [Required]
        public string Action { get; set; }

        public string EntityName { get; set; }
        public string EntityId { get; set; }

        public string Description { get; set; }
        public string IPAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string OldValues { get; set; }
        public string NewValues { get; set; }
    }
}
```

---

## ⚙️ Step 2: Add to ApplicationDbContext

**File:** `Data/ApplicationDbContext.cs`

```csharp
public DbSet<AuditTrail> AuditTrails { get; set; }
```

Run migrations:

```bash
dotnet ef migrations add AddEnhancedAuditTrail
dotnet ef database update
```

---

## 🧩 Step 3: Centralized Audit Logging Helper

**File:** `Services/AuditTrailService.cs`

```csharp
using BHCare.Data;
using BHCare.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BHCare.Services
{
    public class AuditTrailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditTrailService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string actionType, string action, string entityName, string entityId, string oldValues = null, string newValues = null, string description = null)
        {
            var user = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Unknown User";
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var role = _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value ?? "Unassigned";

            var log = new AuditTrail
            {
                PerformedBy = user,
                Role = role,
                ActionType = actionType,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                IPAddress = ip,
                OldValues = oldValues,
                NewValues = newValues
            };

            _context.AuditTrails.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
```

**Register in Program.cs**
```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditTrailService>();
```

---

## 🩺 Step 4: Log Events for Each Role

### 🧑‍💼 Admin
```csharp
await _auditTrail.LogAsync("Create", "Added Staff Member", "ApplicationUser", newStaff.Id, null, $"Added user {newStaff.Email}", "Admin created new staff account.");
```

### 🩺 Doctor
```csharp
await _auditTrail.LogAsync("Create", "Added Prescription", "Prescription", prescription.Id.ToString(), null, $"Prescribed {prescription.MedicineName}", "Doctor added prescription for patient.");
```

### 💉 Nurse
```csharp
await _auditTrail.LogAsync("Create", "Added Immunization Record", "Immunization", immunization.Id.ToString(), null, immunization.ToString(), "Nurse created a new immunization record.");
```

### 👤 Patient
```csharp
await _auditTrail.LogAsync("Update", "Updated Profile", "PatientProfile", patient.Id.ToString(), oldProfileJson, newProfileJson, "Patient updated personal information.");
```

---

## 🧮 Step 5: Create the Audit Logs Page

**File:** `Pages/Admin/AuditTrail.cshtml`

```html
@page
@model BHCare.Pages.Admin.AuditTrailModel
@{
    ViewData["Title"] = "Audit Logs";
}

<div class="card shadow-sm p-4">
    <h2>Audit Trail Logs</h2>

    <form method="get" class="row g-3 mb-3">
        <div class="col-md-4">
            <input type="text" name="search" placeholder="Search by user, role, or action" class="form-control" />
        </div>
        <div class="col-md-3">
            <select name="role" class="form-select">
                <option value="">All Roles</option>
                <option>Admin</option>
                <option>Doctor</option>
                <option>Nurse</option>
                <option>Patient</option>
            </select>
        </div>
        <div class="col-md-2">
            <button class="btn btn-primary">Filter</button>
        </div>
    </form>

    <table class="table table-hover">
        <thead>
            <tr>
                <th>Date/Time</th>
                <th>User</th>
                <th>Role</th>
                <th>Action</th>
                <th>Entity</th>
                <th>Description</th>
                <th>IP</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var log in Model.AuditLogs)
            {
                <tr>
                    <td>@log.Timestamp.ToString("MMM dd, yyyy hh:mm tt")</td>
                    <td>@log.PerformedBy</td>
                    <td>@log.Role</td>
                    <td>@log.Action</td>
                    <td>@log.EntityName</td>
                    <td>@log.Description</td>
                    <td>@log.IPAddress</td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

---

## 🧠 Step 6: Code-Behind for Filtering

**File:** `Pages/Admin/AuditTrail.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BHCare.Data;
using BHCare.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BHCare.Pages.Admin
{
    public class AuditTrailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public AuditTrailModel(ApplicationDbContext context) => _context = context;

        public IList<AuditTrail> AuditLogs { get; set; }

        public async Task OnGetAsync(string search, string role)
        {
            var query = _context.AuditTrails.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(a => a.PerformedBy.Contains(search) || a.Action.Contains(search));

            if (!string.IsNullOrEmpty(role))
                query = query.Where(a => a.Role == role);

            AuditLogs = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(500)
                .ToListAsync();
        }
    }
}
```

---

## 🛡️ Step 7: Secure the Audit Logs Page

```csharp
[Authorize(Roles = "Admin")]
public class AuditTrailModel : PageModel { ... }
```

---

## 🧩 Step 8: Add Navigation Link

**File:** `_AdminSidebar.cshtml`

```html
<li class="nav-item">
    <a class="nav-link" asp-page="/Admin/AuditTrail">
        <i class="fas fa-clipboard-list"></i> Audit Trail
    </a>
</li>
```

---

## ✅ Result

| Timestamp | User | Role | Action | Entity | Description | IP |
|------------|------|------|--------|---------|--------------|----|
| Oct 22, 2025 11:10 PM | admin@bhcare.com | Admin | Added Staff Member | ApplicationUser | Created new doctor account | 127.0.0.1 |
| Oct 22, 2025 11:12 PM | doc@bhcare.com | Doctor | Added Prescription | Prescription | Added new medicine for patient | 192.168.1.2 |
| Oct 22, 2025 11:15 PM | nurse@bhcare.com | Nurse | Added Immunization Record | Immunization | Gave Hepatitis B vaccine | 192.168.1.3 |
| Oct 22, 2025 11:20 PM | patient@bhcare.com | Patient | Updated Profile | PatientProfile | Changed contact number | 192.168.1.4 |

---

## 🧾 Summary

| Feature | Included |
|----------|-----------|
| All Roles (Admin, Doctor, Nurse, Patient) | ✅ |
| IP Address Tracking | ✅ |
| Entity Reference | ✅ |
| Old/New Values | ✅ |
| Role-based Filtering | ✅ |
| Secure Admin-only Access | ✅ |
| Centralized Logging Service | ✅ |
| Scalable & Maintainable | ✅ |
