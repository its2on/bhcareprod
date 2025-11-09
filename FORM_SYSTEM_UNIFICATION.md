# Form System Unification - Create vs Edit Pages

## Overview

The form system now uses **SubmitForm.cshtml** (dynamic form system) for **creating new forms**, while **Edit pages** remain for **editing existing assessments**.

## Form Access Flow

### Creating New Forms (Fill Up)

**For Nurses/Doctors/Admin:**
```
1. User clicks "Create New Assessment" button
   → /Nurse/CreateNCDAssessment?appointmentId=302
   OR /Nurse/CreateHEEADSSSAssessment?appointmentId=302

2. Create page redirects to:
   → /Forms/SubmitForm/ncd-risk-assessment-form?appointmentId=302
   OR /Forms/SubmitForm/heeadsss-assessment?appointmentId=302

3. SubmitForm.cshtml automatically loads:
   - Nurses: ncd-risk-assessment-form-nurse (if exists) OR ncd-risk-assessment-form
   - Doctors/Admin: ncd-risk-assessment-form-doctor (if exists) OR ncd-risk-assessment-form
   - Patients: ncd-risk-assessment-form
```

### Editing Existing Forms

**For Nurses/Doctors/Admin:**
```
1. User clicks "Edit" button on existing assessment
   → /Nurse/EditNCDAssessment?appointmentId=302
   OR /Nurse/EditHEEADSSSAssessment?appointmentId=302

2. Edit page loads existing assessment data from database
   - Uses hardcoded form structure
   - Pre-fills all fields with existing data
   - Allows editing and saving changes
```

## Key Differences

| Aspect | Create Pages (SubmitForm) | Edit Pages |
|--------|---------------------------|------------|
| **Purpose** | Fill up new forms | Edit existing assessments |
| **Form System** | Dynamic (FormTemplates) | Hardcoded (Razor pages) |
| **Role-Specific** | ✅ Yes (via form key suffix) | ❌ No (same form for all) |
| **Data Source** | FormTemplates table | NCDRiskAssessments/HEEADSSSAssessments tables |
| **Storage** | FormSubmissions table | NCDRiskAssessments/HEEADSSSAssessments tables |
| **Layout** | Role-based (Nurse/Doctor/Patient) | Role-based (Nurse/Doctor) |

## Form Key Naming Convention

### For SubmitForm.cshtml (Dynamic Forms)

**Standard Form (Patient):**
- `ncd-risk-assessment-form`
- `heeadsss-assessment`

**Nurse-Specific Form:**
- `ncd-risk-assessment-form-nurse` (if exists, otherwise uses standard)
- `heeadsss-assessment-nurse` (if exists, otherwise uses standard)

**Doctor-Specific Form:**
- `ncd-risk-assessment-form-doctor` (if exists, otherwise uses standard)
- `heeadsss-assessment-doctor` (if exists, otherwise uses standard)

## Current Implementation

### Create Pages (Now Redirect to SubmitForm)

**Files Updated:**
- `Pages/Nurse/CreateNCDAssessment.cshtml.cs` → Redirects to SubmitForm
- `Pages/Nurse/CreateHEEADSSSAssessment.cshtml.cs` → Redirects to SubmitForm
- `Pages/Doctor/CreateNCDAssessment.cshtml.cs` → Redirects to SubmitForm

**Behavior:**
- All Create pages now redirect to `/Forms/SubmitForm/{formKey}?appointmentId={id}`
- SubmitForm automatically loads role-specific forms
- Same form system used by patients, but with role-specific variations

### Edit Pages (Remain Separate)

**Files:**
- `Pages/Nurse/EditNCDAssessment.cshtml` - Hardcoded form for editing
- `Pages/Nurse/EditHEEADSSSAssessment.cshtml` - Hardcoded form for editing
- `Pages/Doctor/EditNCDAssessment.cshtml.cs` - Redirects to Nurse edit page
- `Pages/Doctor/EditHEEADSSSAssessment.cshtml.cs` - Redirects to Nurse edit page

**Behavior:**
- Load existing assessment data from database
- Use hardcoded form structure (not dynamic)
- Same form for all roles (Nurse/Doctor/Admin), but different layouts

## Answer to Your Question

**Q: Are EditHEEADSSSAssessment and EditNCDAssessment the same as SubmitForm.cshtml when doctors/nurses fill up forms?**

**A:** 
- **For Creating/Filling Up**: ✅ **YES** - Doctors and nurses now use SubmitForm.cshtml (via Create page redirects)
- **For Editing**: ❌ **NO** - Edit pages are separate hardcoded forms for editing existing assessments

**When doctors/nurses fill up forms:**
1. They click "Create New Assessment" → Redirects to SubmitForm.cshtml
2. SubmitForm loads role-specific form (`-nurse` or `-doctor` suffix) if it exists
3. If role-specific form doesn't exist, uses standard form
4. Form is saved to FormSubmissions table

**When doctors/nurses edit existing forms:**
1. They click "Edit" button → Goes to EditNCDAssessment/EditHEEADSSSAssessment
2. Edit page loads existing data from NCDRiskAssessments/HEEADSSSAssessments tables
3. Uses hardcoded form structure (not dynamic)
4. Changes are saved back to the same tables

## Benefits

1. **Unified Form System**: Create operations use the same dynamic form system
2. **Role-Specific Forms**: Nurses and doctors can have different forms via form key suffixes
3. **Backward Compatible**: Edit pages remain unchanged for existing workflows
4. **Flexible**: Admin can create role-specific forms in Form Builder

## Next Steps (Optional)

To make Edit pages also use dynamic forms:
1. Update Edit pages to load from FormSubmissions instead of NCDRiskAssessments/HEEADSSSAssessments
2. Or create a unified system that handles both create and edit in SubmitForm.cshtml

