# Form Edit Permissions Documentation

## Overview

This document explains the permission structure for editing HEEADSSS and NCD Assessment forms across different user roles.

## Permission Matrix

| Role | Edit Forms | View Forms | Notes |
|------|-----------|------------|-------|
| **Admin** | ✅ Yes (All forms) | ✅ Yes (All forms) | Can edit forms for all roles (patient, doctor, nurse) |
| **Doctor** | ✅ Yes (Assigned appointments) | ✅ Yes (Assigned appointments) | Can edit forms for their assigned appointments |
| **Head Doctor** | ✅ Yes (Assigned appointments) | ✅ Yes (Assigned appointments) | Same permissions as Doctor |
| **Nurse** | ✅ Yes (Assigned appointments) | ✅ Yes (Assigned appointments) | Can edit forms for their assigned appointments |
| **Head Nurse** | ✅ Yes (Assigned appointments) | ✅ Yes (Assigned appointments) | Same permissions as Nurse |
| **Patient** | ❌ No | ✅ Yes (Read-only) | Can only view their submitted forms, cannot edit |

## Form Edit Pages

### NCD Risk Assessment

**Edit Pages:**
- `/Nurse/EditNCDAssessment` - Main edit page (handles Nurse, Doctor, and Admin)
- `/Doctor/EditNCDAssessment` - Redirects to Nurse edit page

**Authorization:**
```csharp
[Authorize(Roles = "Nurse,Head Nurse,Doctor,Head Doctor,Admin")]
```

### HEEADSSS Assessment

**Edit Pages:**
- `/Nurse/EditHEEADSSSAssessment` - Main edit page (handles Nurse, Doctor, and Admin)
- `/Doctor/EditHEEADSSSAssessment` - Redirects to Nurse edit page

**Authorization:**
```csharp
[Authorize(Roles = "Nurse,Head Nurse,Doctor,Head Doctor,Admin")]
```

## Implementation Details

### Role Detection Methods

Both edit pages use the following helper methods:

```csharp
// Check if user is Admin (full permissions)
private bool IsAdminRole()
{
    return User.IsInRole("Admin");
}

// Check if user is Doctor role (includes Admin for layout purposes)
private bool IsDoctorRole()
{
    return User.IsInRole("Doctor") || User.IsInRole("Head Doctor") || User.IsInRole("Admin");
}

// Check if user is Nurse role
private bool IsNurseRole()
{
    return User.IsInRole("Nurse") || User.IsInRole("Head Nurse");
}
```

### Layout Selection

The forms automatically select the appropriate layout based on role:

- **Admin & Doctor**: Use `_DoctorLayout` (Doctor navigation and UI)
- **Nurse**: Use `_NurseLayout` (Nurse navigation and UI)

This allows different navigation and UI for each role while sharing the same form functionality.

### Navigation Redirects

The forms redirect users to the appropriate page after editing:

- **Admin & Doctor**: Redirect to `/Doctor/Consultation`
- **Nurse**: Redirect to `/Nurse/AppointmentDetails`

## Key Differences Between Roles

### Admin
- ✅ Can edit **all forms** for **all roles** (patient, doctor, nurse)
- ✅ Uses Doctor layout for consistency
- ✅ Full access to all appointment data
- ✅ Can override any permission restrictions

### Doctor
- ✅ Can edit forms for **their assigned appointments**
- ✅ Uses Doctor layout
- ✅ Can view and edit patient assessments
- ❌ Cannot edit forms for appointments not assigned to them

### Nurse
- ✅ Can edit forms for **their assigned appointments**
- ✅ Uses Nurse layout
- ✅ Can view and edit patient assessments
- ❌ Cannot edit forms for appointments not assigned to them

### Patient
- ❌ **Cannot access edit pages** (blocked by authorization)
- ✅ Can view their submitted forms (read-only)
- ✅ Can fill out new forms during appointment booking
- ❌ Cannot edit forms after submission

## Form Access Flow

### For Admin/Doctor/Nurse:

```
1. User navigates to Appointment Details/Consultation
2. Clicks "Edit" button on assessment form
3. System checks authorization:
   - Admin: ✅ Always allowed
   - Doctor: ✅ Allowed if appointment assigned to them
   - Nurse: ✅ Allowed if appointment assigned to them
4. Form loads with appropriate layout
5. User edits form and saves
6. Redirects to appropriate page based on role
```

### For Patient:

```
1. Patient navigates to their appointment
2. Can view submitted forms (read-only)
3. Cannot access edit pages (blocked by [Authorize] attribute)
4. Can fill out new forms during appointment booking
```

## Security Considerations

1. **Authorization at Page Level**: All edit pages use `[Authorize]` attribute to prevent unauthorized access
2. **Role-Based Access**: Only authorized roles can access edit pages
3. **Patient Protection**: Patients are explicitly excluded from edit pages
4. **Appointment Assignment**: Doctors and Nurses can only edit forms for their assigned appointments (enforced at business logic level)

## Code Locations

### Edit Pages
- `Pages/Nurse/EditNCDAssessment.cshtml.cs` - NCD Assessment edit logic
- `Pages/Nurse/EditNCDAssessment.cshtml` - NCD Assessment edit UI
- `Pages/Nurse/EditHEEADSSSAssessment.cshtml.cs` - HEEADSSS Assessment edit logic
- `Pages/Nurse/EditHEEADSSSAssessment.cshtml` - HEEADSSS Assessment edit UI

### Redirect Pages
- `Pages/Doctor/EditNCDAssessment.cshtml.cs` - Redirects to Nurse edit page
- `Pages/Doctor/EditHEEADSSSAssessment.cshtml.cs` - Redirects to Nurse edit page

## Notes

1. **Shared Implementation**: Both Nurse and Doctor use the same edit pages, with layout differences handled automatically
2. **Admin Inclusion**: Admin is included in `IsDoctorRole()` check for layout purposes, but has separate `IsAdminRole()` method for explicit admin checks
3. **Future Enhancement**: Consider adding appointment assignment validation to ensure Doctors/Nurses can only edit their assigned appointments

