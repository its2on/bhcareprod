# Prefilled Demographic Fields Implementation

## Overview

This document describes the implementation of prefilled demographic fields for both **NCD** and **HEEADSSS** assessment forms using the dynamic form system (`/Forms/SubmitForm/{formKey}`).

## Implementation Date
November 2, 2025

---

## 1. Feature Summary

### Purpose
- **Reduce redundancy**: Automatically fills in patient information from their CMS profile
- **Ensure data consistency**: Uses authoritative data from `ApplicationUser` and `Patient` tables
- **Improve UX**: Users don't need to re-enter information they've already provided during registration
- **Support dependent bookings**: Correctly prefills information for the person receiving care (whether self or dependent)

### Key Characteristics
- ✅ Demographic fields are prefilled from the patient's existing profile
- ✅ Most prefilled fields are readonly (non-editable) to prevent data inconsistency
- ✅ Fields not stored in CMS (Religion, Civil Status) remain fully editable
- ✅ Works for both self-bookings and dependent bookings
- ✅ Applies to ALL dynamic forms (NCD, HEEADSSS, and any future forms)

---

## 2. Files Modified

### Backend (C#)

#### `Pages/Forms/SubmitForm.cshtml.cs`

**New Properties:**
```csharp
public ApplicationUser? CurrentUser { get; set; }
public Patient? PatientData { get; set; }
public Dictionary<string, string> PrefilledValues { get; set; }
public HashSet<string> ReadonlyFields { get; set; }
```

**New Methods:**
1. `LoadPrefillDataAsync()` - Loads user and patient data, handles decryption, determines if booking is for a dependent
2. `BuildPrefilledValues()` - Builds the prefilled values dictionary and readonly fields set based on field names

**New Dependencies:**
- `IDataEncryptionService` - Added to constructor for decrypting sensitive user data

**Changes:**
- Added `await LoadPrefillDataAsync()` call at the end of `OnGetAsync()`

### Frontend (Razor)

#### `Pages/Forms/SubmitForm.cshtml`

**Changes in Form Field Rendering:**
```csharp
// Before each field is rendered:
var fieldNameLower = field.FieldName.ToLower();
var prefilledValue = field.DefaultValue ?? "";
var isPrefilledReadonly = field.IsReadOnly;

// Check prefilled values
if (Model.PrefilledValues.TryGetValue(fieldNameLower, out var prefilledVal))
{
    prefilledValue = prefilledVal;
}

// Check if readonly due to prefilling
if (Model.ReadonlyFields.Contains(fieldNameLower))
{
    isPrefilledReadonly = true;
}
```

**Updated Attributes:**
- Changed `value="@field.DefaultValue"` → `value="@prefilledValue"`
- Changed `readonly="@field.IsReadOnly"` → `readonly="@isPrefilledReadonly"`
- Changed `disabled="@field.IsReadOnly"` → `disabled="@isPrefilledReadonly"`

---

## 3. Field Mapping Configuration

The following field name variants are automatically recognized and prefilled:

### Health Facility
- **Field Names**: `health_facility`, `healthfacility`, `facility`
- **Source**: Hard-coded: `"Baesa Health Center"`
- **Status**: ✅ Prefilled + Readonly

### Family Number
- **Field Names**: `family_no`, `familyno`, `family_number`, `familynumber`
- **Source**: `Appointment.FamilyNumber` → `ApplicationUser.FamilyNumber` → `Patient.FamilyNumber`
- **Status**: ✅ Prefilled + Readonly

### Last Name (Apelyido)
- **Field Names**: `last_name`, `lastname`, `apelyido`, `surname`
- **Source**: `ApplicationUser.LastName`
- **Status**: ✅ Prefilled + Readonly

### First Name (Unang Pangalan)
- **Field Names**: `first_name`, `firstname`, `unang_pangalan`, `given_name`
- **Source**: `ApplicationUser.FirstName`
- **Status**: ✅ Prefilled + Readonly

### Middle Name (Gitnang Pangalan)
- **Field Names**: `middle_name`, `middlename`, `gitnang_pangalan`
- **Source**: `ApplicationUser.MiddleName`
- **Status**: ✅ Prefilled + Readonly

### Full Name
- **Field Names**: `full_name`, `fullname`, `name`, `pangalan`
- **Source**: `Patient.FullName` → `ApplicationUser.FullName`
- **Status**: ✅ Prefilled + Readonly

### Address
- **Field Names**: `address`, `tirahan`
- **Source**: `Patient.Address` → `ApplicationUser.Address`
- **Status**: ✅ Prefilled + Readonly

### Barangay
- **Field Names**: `barangay`
- **Source**: `ApplicationUser.Barangay`
- **Status**: ✅ Prefilled + Readonly

### Contact Number (Telepono)
- **Field Names**: `contact_number`, `contactnumber`, `phone`, `phone_number`, `telepono`
- **Source**: `Patient.ContactNumber` → `ApplicationUser.PhoneNumber`
- **Status**: ✅ Prefilled + Readonly

### Birthday (Kaarawan)
- **Field Names**: `birthday`, `birthdate`, `birth_date`, `date_of_birth`, `kaarawan`
- **Source**: `Patient.BirthDate` → `ApplicationUser.BirthDate`
- **Format**: `yyyy-MM-dd` (ISO date format)
- **Status**: ✅ Prefilled + Readonly

### Age (Edad)
- **Field Names**: `age`, `edad`
- **Source**: Calculated from `BirthDate` using `CalculateAge()` method
- **Status**: ✅ Prefilled + Readonly

### Gender (Kasarian)
- **Field Names**: `gender`, `sex`, `kasarian`
- **Source**: `Patient.Gender` → `ApplicationUser.Gender`
- **Status**: ✅ Prefilled + Readonly

### Date of Assessment
- **Field Names**: `date_of_assessment`, `dateofassessment`, `assessment_date`, `assessmentdate`, `petsa`
- **Source**: `DateTime.Now` (system-generated)
- **Format**: `yyyy-MM-dd`
- **Status**: ✅ Prefilled + Readonly

### Religion (Relihiyon)
- **Field Names**: `religion`, `relihiyon`
- **Status**: ❌ NOT Prefilled - Remains Editable

### Civil Status (Katayuang Sibil)
- **Field Names**: `civil_status`, `civilstatus`, `katayuang_sibil`, `marital_status`
- **Status**: ❌ NOT Prefilled - Remains Editable

---

## 4. Data Source Priority

### For Self-Bookings (`BookingForOther = false`):

1. **Patient Table** (if patient record exists)
   - `Patient.FullName`
   - `Patient.Address`
   - `Patient.ContactNumber`
   - `Patient.Gender`
   - `Patient.BirthDate`

2. **ApplicationUser Table** (fallback)
   - `ApplicationUser.FirstName`
   - `ApplicationUser.LastName`
   - `ApplicationUser.MiddleName`
   - `ApplicationUser.Address`
   - `ApplicationUser.Barangay`
   - `ApplicationUser.PhoneNumber`
   - `ApplicationUser.Gender`
   - `ApplicationUser.BirthDate`
   - `ApplicationUser.FamilyNumber`

3. **Appointment Table** (fallback for family number)
   - `Appointment.FamilyNumber`

### For Dependent Bookings (`BookingForOther = true`):

1. **Appointment Table** (primary source for dependent data)
   - `Appointment.DependentFullName` (dependent's full name)
   - `Appointment.DependentAge` (dependent's age)
   - `Appointment.DateOfBirth` (dependent's birthday)
   - `Appointment.Gender` (dependent's gender)
   - `Appointment.ContactNumber` (dependent's contact)
   - `Appointment.FamilyNumber`

2. **ApplicationUser Table** (for booker's address/barangay)
   - `ApplicationUser.Address` (used as dependent's address)
   - `ApplicationUser.Barangay`

**Name Parsing for Dependents:**
```csharp
var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
firstName = nameParts.Length > 0 ? nameParts[0] : "";
middleName = nameParts.Length > 2 ? nameParts[1] : "";
lastName = nameParts.Length > 1 ? nameParts[nameParts.Length - 1] : "";
```

---

## 5. How It Works

### Flow Diagram
```
User opens form 
    ↓
OnGetAsync() is called
    ↓
Form template loaded
    ↓
Appointment data loaded (if appointmentId provided)
    ↓
LoadPrefillDataAsync() is called
    ↓
    ├─→ Get current user (ApplicationUser)
    ├─→ Decrypt sensitive fields
    ├─→ Get patient data (Patient)
    ├─→ Check if booking for dependent
    └─→ Call BuildPrefilledValues()
         ↓
         ├─→ Determine which data to use (user vs. dependent)
         ├─→ Build PrefilledValues dictionary
         └─→ Build ReadonlyFields HashSet
    ↓
Page renders
    ↓
For each form field:
    ├─→ Check if field name exists in PrefilledValues
    ├─→ Use prefilled value if found
    ├─→ Check if field name exists in ReadonlyFields
    └─→ Apply readonly attribute if found
```

### Code Execution Path

#### Step 1: Page Load
```csharp
public async Task<IActionResult> OnGetAsync(string formKey, int? appointmentId = null)
{
    // Load form template
    FormTemplate = await _context.FormTemplates
        .Include(f => f.FormFields)
        .ThenInclude(ff => ff.FormFieldOptions)
        .FirstOrDefaultAsync(f => f.FormKey == formKey && f.IsActive);
    
    // Load appointment if appointmentId provided
    if (appointmentId.HasValue)
    {
        Appointment = await _context.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId.Value);
    }
    
    // Load prefill data
    await LoadPrefillDataAsync();
    
    return Page();
}
```

#### Step 2: Load User & Patient Data
```csharp
private async Task LoadPrefillDataAsync()
{
    // Get current user
    var user = await _userManager.GetUserAsync(User);
    
    // Decrypt user data
    user = user.DecryptSensitiveData(_encryptionService, User);
    
    // Get patient data
    PatientData = await _context.Patients
        .FirstOrDefaultAsync(p => p.UserId == user.Id);
    
    // Decrypt patient data if exists
    if (PatientData != null)
    {
        PatientData = PatientData.DecryptSensitiveData(_encryptionService, User);
    }
    
    // Determine if booking for dependent
    bool isForDependent = Appointment?.BookingForOther == true;
    
    // Build prefilled values
    BuildPrefilledValues(user, PatientData, isForDependent, ...);
}
```

#### Step 3: Build Prefill Dictionary
```csharp
private void BuildPrefilledValues(ApplicationUser user, Patient? patient, 
    bool isForDependent, string? dependentName, int? dependentAge, 
    DateTime? dependentBirthday, string? dependentGender)
{
    // Determine which data to use
    if (isForDependent && !string.IsNullOrEmpty(dependentName))
    {
        // Use dependent's data from Appointment
        fullName = dependentName;
        age = dependentAge ?? 0;
        gender = dependentGender ?? "";
        // ...
    }
    else
    {
        // Use user's data
        firstName = user.FirstName ?? "";
        lastName = user.LastName ?? "";
        // ...
    }
    
    // Add to dictionary with all possible field name variants
    PrefilledValues["first_name"] = firstName;
    PrefilledValues["firstname"] = firstName;
    PrefilledValues["unang_pangalan"] = firstName;
    
    // Mark as readonly
    ReadonlyFields.Add("first_name");
    ReadonlyFields.Add("firstname");
    ReadonlyFields.Add("unang_pangalan");
}
```

#### Step 4: Render with Prefill Values
```cshtml
@foreach (var field in Model.FormTemplate.FormFields.OrderBy(f => f.DisplayOrder))
{
    // Check for prefilled value
    var fieldNameLower = field.FieldName.ToLower();
    var prefilledValue = field.DefaultValue ?? "";
    var isPrefilledReadonly = field.IsReadOnly;
    
    if (Model.PrefilledValues.TryGetValue(fieldNameLower, out var prefilledVal))
    {
        prefilledValue = prefilledVal;
    }
    
    if (Model.ReadonlyFields.Contains(fieldNameLower))
    {
        isPrefilledReadonly = true;
    }
    
    // Render field with prefilled value
    <input type="text" 
           name="@field.FieldName" 
           value="@prefilledValue"
           readonly="@isPrefilledReadonly" />
}
```

---

## 6. Security & Data Handling

### Encryption/Decryption
All sensitive fields are decrypted before being used for prefilling:
- Uses `IDataEncryptionService` 
- Calls `DecryptSensitiveData()` extension method
- Manually decrypts `Email` and `PhoneNumber` fields

### User Authorization
- Only authenticated users can access forms
- Users can only see their own data
- Data is decrypted based on current user's permissions

### Data Consistency
- Readonly fields prevent accidental data modification
- Form submission stores data in `FormSubmissions` table
- Original patient/user records remain unchanged

---

## 7. Testing Checklist

### ✅ Self-Booking Scenario
1. User logs in
2. Books appointment for themselves
3. Opens HEEADSSS or NCD form
4. **Expected**:
   - Name fields show user's name
   - Age shows user's age
   - Gender shows user's gender
   - All demographic fields are readonly
   - Religion and Civil Status are editable

### ✅ Dependent Booking Scenario
1. User logs in
2. Books appointment for someone else (e.g., child)
3. Opens HEEADSSS or NCD form
4. **Expected**:
   - Name fields show **dependent's** name (not booker's)
   - Age shows **dependent's** age
   - Gender shows **dependent's** gender
   - Family number is displayed
   - "Booked by" information shows in appointment context
   - All demographic fields are readonly
   - Religion and Civil Status are editable

### ✅ Field Validation
1. Try to edit a prefilled readonly field
   - **Expected**: Field cannot be edited (grayed out)
2. Try to edit Religion field
   - **Expected**: Field can be edited freely
3. Try to edit Civil Status field
   - **Expected**: Field can be edited freely

### ✅ Form Submission
1. Fill out editable fields (Religion, Civil Status, assessment questions)
2. Submit form
3. **Expected**:
   - Form submits successfully
   - All data (including prefilled readonly fields) is saved to `FormSubmissions` table
   - Success modal displays
   - User redirects to dashboard

---

## 8. Troubleshooting

### Issue: Prefilled values not showing

**Possible Causes:**
1. Field name in Form Builder doesn't match any recognized variants
2. User/Patient data is missing or null
3. Decryption failed

**Solution:**
1. Check server logs for `=== PREFILL DATA LOADED ===` messages
2. Verify field name in Form Builder matches one of the recognized variants (case-insensitive)
3. Check if user has complete profile data

### Issue: Field should be readonly but isn't

**Possible Cause:**
Field name not in `ReadonlyFields` HashSet

**Solution:**
1. Check field name matches exactly (case-insensitive)
2. Add additional field name variants to `BuildPrefilledValues()` method if needed

### Issue: Dependent's data not showing

**Possible Causes:**
1. `BookingForOther` flag not set correctly
2. `DependentFullName`, `DependentAge`, etc. not saved during appointment creation

**Solution:**
1. Check server logs for `Is For Dependent:` message
2. Verify `Appointment.BookingForOther = true`
3. Verify `Appointment.DependentFullName` is not null
4. Check `BookAppointment.cshtml.cs` saves dependent data correctly

---

## 9. Future Enhancements

### Potential Improvements
1. **Dynamic Field Name Recognition**: Use AI/ML to recognize field names based on semantic meaning
2. **Partial Edit Mode**: Allow staff to edit certain readonly fields with proper authorization
3. **Profile Update Prompt**: If form reveals outdated patient info, prompt user to update their profile
4. **Multi-language Support**: Add support for more Tagalog/Filipino field name variants
5. **Guardian Information**: For minors (age < 18), auto-prefill parent/guardian information if available in the system

### Additional Field Types to Consider
- Emergency Contact Name & Number
- PhilHealth ID
- Blood Type
- Allergies (as reference, not editable)
- Current Medications (as reference)

---

## 10. Maintenance Notes

### Adding New Prefilled Fields

To add support for a new prefilled field:

1. **Identify the field name variants** (e.g., "email", "email_address", "e-mail")

2. **Update `BuildPrefilledValues()` method**:
```csharp
// New Field - Email
var email = patient?.Email ?? user.Email ?? "";
PrefilledValues["email"] = email;
PrefilledValues["email_address"] = email;
PrefilledValues["e_mail"] = email;
ReadonlyFields.Add("email");
ReadonlyFields.Add("email_address");
ReadonlyFields.Add("e_mail");
```

3. **Test with form containing the new field**

### Changing Readonly Behavior

To make a field editable (remove readonly):
1. Remove field name variants from `ReadonlyFields.Add()` calls
2. Field will still be prefilled but users can edit it

To prevent prefilling (make field always empty):
1. Remove field name variants from both `PrefilledValues` and `ReadonlyFields` dictionaries

---

## 11. Related Files & Components

### Backend Components
- `Pages/Forms/SubmitForm.cshtml.cs` - Main implementation
- `Services/IDataEncryptionService.cs` - Encryption service interface
- `Extensions/EncryptionExtensions.cs` - Decryption helper methods
- `Models/ApplicationUser.cs` - User model
- `Models/Patient.cs` - Patient model
- `Models/Appointment.cs` - Appointment model

### Frontend Components
- `Pages/Forms/SubmitForm.cshtml` - Form rendering view
- `Pages/BookAppointment.cshtml` - Appointment booking (creates appointment record)
- `Pages/BookAppointment.cshtml.cs` - Appointment creation logic

### Database Tables
- `AspNetUsers` - User accounts (ApplicationUser)
- `Patients` - Patient records
- `Appointments` - Appointment records
- `FormTemplates` - Form definitions
- `FormFields` - Field definitions
- `FormSubmissions` - Submitted form data

---

## 12. API & Logging

### Key Log Messages

**Prefill Data Load:**
```
=== PREFILL DATA LOADED ===
User: [username], Patient Data: [true/false]
Is For Dependent: [true/false], Dependent Name: [name or N/A]
```

**Prefill Values Built:**
```
Built prefill values: LastName=[lastname], FirstName=[firstname], Age=[age], Gender=[gender]
Prefilled [count] fields
```

**Appointment Context:**
```
=== SUBMIT FORM - APPOINTMENT CONTEXT ===
Appointment ID: [id]
BookingForOther: [true/false]
PatientName (Booker): [name]
DependentFullName: [name or NULL]
DependentAge: [age or NULL]
...
```

### How to Enable Debug Logging

In `appsettings.json` or `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Barangay.Pages.Forms.SubmitFormModel": "Debug"
    }
  }
}
```

---

## Summary

✅ **Implemented prefilled demographic fields for dynamic forms**
✅ **Supports both self-bookings and dependent bookings**
✅ **Readonly fields prevent data inconsistency**  
✅ **Religion and Civil Status remain editable as specified**
✅ **Works with all dynamic forms (NCD, HEEADSSS, future forms)**
✅ **Comprehensive field name variant support (English & Tagalog)**
✅ **Secure decryption of sensitive data**
✅ **Extensive logging for troubleshooting**

**Status**: ✅ **READY FOR TESTING**

---

**Implementation Date**: November 2, 2025
**Implemented By**: AI Assistant
**Reviewed By**: [Pending]
**Tested By**: [Pending]

