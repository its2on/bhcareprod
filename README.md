# Barangay Health Care System - Database Implementation

This repository contains SQL scripts for implementing user permissions and prescription management in the Barangay Health Care System.

## 📋 Features Implemented

### 🧩 User Permissions Management
- SQL schema for Users, Permissions, and UserPermissions tables
- Stored procedures to manage user permissions
- Sample permission data

### 💊 Prescription Management
- Stored procedures for creating, updating, and retrieving prescriptions
- Integration with medical records
- Efficient data handling with JSON support

## 🛠️ Installation

### Prerequisites
- SQL Server (SSMS)
- An existing Barangay database
- SQL Server command-line tools (sqlcmd)

### Setup Steps

1. Ensure you have SQL Server installed and the Barangay database created.
2. Update server connection details in `run_scripts.bat`:
   ```batch
   set server=YOUR_SERVER_NAME
   set database=Barangay
   set username=YOUR_USERNAME (optional)
   set password=YOUR_PASSWORD (optional)
   ```

3. Run the installation batch file:
   ```
   run_scripts.bat
   ```

4. Optionally, run the sample data script separately:
   ```
   sqlcmd -S YOUR_SERVER_NAME -d Barangay -E -i permission_sample_data.sql
   ```

## 📚 Usage Guide

### User Permissions Management

#### Assign Permissions to a User
```sql
-- Replace with actual user ID and permission IDs
EXEC sp_AssignUserPermissions 
    @UserId = 'user-guid-here', 
    @PermissionIds = '1,2,3,5,8';
```

#### Get User Permissions
```sql
-- Replace with actual user ID
EXEC sp_GetUserPermissions 
    @UserId = 'user-guid-here';
```

#### Check if User Has a Specific Permission
```sql
-- Replace with actual user ID and permission name
EXEC sp_CheckUserPermission 
    @UserId = 'user-guid-here', 
    @PermissionName = 'Create Prescriptions';
```

### Prescription Management

#### Create a New Prescription
```sql
-- Example JSON format for medications
DECLARE @MedicationsJSON NVARCHAR(MAX) = N'[
    {
        "medicationName": "Paracetamol",
        "dosage": "500mg",
        "frequency": "Every 6 hours",
        "duration": "5 days",
        "instructions": "Take after meals",
        "medicalRecordId": 1
    },
    {
        "medicationName": "Vitamin C",
        "dosage": "1000mg",
        "frequency": "Once daily",
        "duration": "30 days",
        "instructions": "Take with breakfast",
        "medicalRecordId": 1
    }
]';

EXEC sp_CreatePrescription
    @PatientId = 1,
    @DoctorId = 1,
    @Status = 'Active',
    @Notes = 'Patient reports improvement in symptoms',
    @MedicationsJSON = @MedicationsJSON;
```

#### Update an Existing Prescription
```sql
-- Example JSON format for updated medications
DECLARE @UpdatedMedicationsJSON NVARCHAR(MAX) = N'[
    {
        "medicationName": "Paracetamol",
        "dosage": "500mg",
        "frequency": "Every 8 hours", -- Changed from 6 to 8 hours
        "duration": "7 days", -- Extended from 5 to 7 days
        "instructions": "Take after meals",
        "medicalRecordId": 1
    },
    {
        "medicationName": "Vitamin C",
        "dosage": "1000mg",
        "frequency": "Once daily",
        "duration": "30 days",
        "instructions": "Take with breakfast",
        "medicalRecordId": 1
    }
]';

EXEC sp_UpdatePrescription
    @PrescriptionId = 1,
    @Status = 'Active',
    @Notes = 'Updated dosage frequency for Paracetamol',
    @MedicationsJSON = @UpdatedMedicationsJSON;
```

#### Get Prescriptions for a Patient
```sql
EXEC sp_GetPatientPrescriptions
    @PatientId = 1;
```

#### Get Prescriptions by a Doctor
```sql
EXEC sp_GetDoctorPrescriptions
    @DoctorId = 1;
```

#### Get Prescription Details
```sql
EXEC sp_GetPrescriptionDetails
    @PrescriptionId = 1;
```

#### Get Prescriptions Related to a Medical Record
```sql
EXEC sp_GetMedicalRecordPrescriptions
    @MedicalRecordId = 1;
```

## 📝 Notes

- All stored procedures use transactions to ensure data integrity
- Errors are properly handled and returned with meaningful messages
- The code includes comments explaining each section's purpose
- Both Windows and SQL authentication are supported in the setup script

## 🔄 Integration with C#

To use these stored procedures in your C# backend, use ADO.NET or Entity Framework. Example C# methods are included in the full documentation.

# Permission-Based Navigation System

This system allows for dynamic rendering of navigation items based on user permissions in the Barangay Health Care System.

## Key Components

1. **Permission Model**
   - `Permission` - Represents a system permission with a Name, Description, and Category
   - `UserPermission` - Maps permissions to users

2. **Permission Service**
   - `PermissionService` - Core service that checks if users have specific permissions
   - `PermissionFixService` - Ensures all required permissions exist and assigns default permissions to roles

3. **Tag Helpers**
   - `PermissionTagHelper` - Conditionally renders content based on permissions
   - `NavPermissionTagHelper` - Specifically for navigation items

4. **View Components**
   - `NavMenuViewComponent` - Dynamically generates navigation menus based on role and permissions

5. **Extensions**
   - `PermissionExtensions` - Helper methods to check permissions in Razor views

## How It Works

1. **Permission Storage**
   - Permissions are stored in the `Permissions` table
   - User-specific permissions are stored in the `UserPermissions` table

2. **Permission Assignment**
   - Admins can assign permissions to users via the `ManagePermissions` page
   - The `GrantEssentialPermissions` button assigns default permissions based on role

3. **Navigation Rendering**
   - The `_SharedNavBar.cshtml` uses the `NavMenu` view component to render role-specific navigation
   - Each navigation item is only displayed if the user has the required permission

4. **Permission Checking**
   - At runtime, the system checks if a user has the required permissions before displaying navigation items
   - Permissions are cached in the session for better performance

## Usage Examples

### Tag Helper Usage
```html
<!-- Only visible if user has the "ManageAppointments" permission -->
<li nav-permission="ManageAppointments">
    <a class="nav-link" href="/Appointments">Appointments</a>
</li>

<!-- Only visible if user has the "Manage Permissions" permission -->
<div permission="Manage Permissions">
    This content requires the Manage Permissions permission.
</div>

<!-- Only visible if user does NOT have the "Delete Users" permission -->
<div permission-not="Delete Users">
    You cannot delete users.
</div>
```

### View Component Usage
```html
<!-- Render navigation menu for the Nurse role -->
@await Component.InvokeAsync("NavMenu", new { role = "Nurse" })
```

### Code-Based Permission Check
```csharp
// In a PageModel
var hasPermission = await _permissionService.UserHasPermissionAsync(userId, "ManageAppointments");

// In a Razor view with injected service
@inject PermissionService PermissionService
@{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var hasPermission = await PermissionService.UserHasPermissionAsync(userId, "ManageAppointments");
}
```

## Implementation Details

1. When a user logs in, their role-specific permissions are loaded
2. The `NavMenu` view component filters navigation items based on these permissions
3. The `PermissionTagHelper` hides/shows content based on permissions
4. When permissions are updated in the admin panel, the cache is cleared to ensure changes take effect immediately

## Testing Permissions

Use the `TestPermissions.cshtml` page to test if a user has specific permissions. # bhcare
