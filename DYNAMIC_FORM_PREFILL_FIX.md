# Dynamic Form Pre-fill and Section Break Fix

## Issues Identified

### 1. Pre-fill Not Working ❌
**Problem**: After booking an appointment, when redirected to the NCD Risk Assessment form, fields like Last Name, First Name, Age, and Sex were showing "Your answer" placeholder instead of being pre-filled with patient data.

**Root Cause**: Field names in the CMS Form Builder contain parentheses and special characters (e.g., "Apelyido (Last Name)", "Edad (Age)"), but the pre-fill dictionary only had simple lowercase keys like "apelyido" and "edad".

### 2. Section Breaks Not Rendering ❌
**Problem**: Section breaks/dividers created in the Form Builder were not displaying in the form.

**Root Cause**: The SubmitForm.cshtml view had no rendering logic for "section", "divider", or "heading" field types.

---

## Solutions Implemented

### Fix 1: Enhanced Field Name Matching

#### Backend Changes (`Pages/Forms/SubmitForm.cshtml.cs`)

**Added Multiple Field Name Variations**:

```csharp
// Last Name - now includes variations
PrefilledValues["last_name"] = lastName;
PrefilledValues["lastname"] = lastName;
PrefilledValues["apelyido"] = lastName;
PrefilledValues["apelyido(lastname)"] = lastName; // NEW: Handles "Apelyido (Last Name)"
PrefilledValues["apelyidolastname"] = lastName;    // NEW: Normalized (no spaces/parentheses)
PrefilledValues["surname"] = lastName;
```

**Similar updates for**:
- First Name: `unangpangalan`, `unangpangalan(firstname)`, `unangpangalanfirstname`
- Middle Name: `gitnangpangalan`, `gitnangpangalan(middlename)`, `gitnangpangalanmiddlename`
- Age: `edad`, `edad(age)`, `edadage`
- Sex: `kasarian`, `kasarian(sex)`, `kasariansex`

**Added Normalization Helper Method**:
```csharp
private string NormalizeFieldName(string fieldName)
{
    if (string.IsNullOrEmpty(fieldName)) return "";
    return System.Text.RegularExpressions.Regex.Replace(fieldName.ToLower(), "[^a-z0-9]", "");
}
```

#### Frontend Changes (`Pages/Forms/SubmitForm.cshtml`)

**Improved Matching Logic**:
```csharp
// Original field name
var fieldNameLower = field.FieldName.ToLower();

// Normalized version (removes spaces, parentheses, special chars)
var normalizedFieldName = System.Text.RegularExpressions.Regex.Replace(fieldNameLower, "[^a-z0-9]", "");

// Try exact match first
if (Model.PrefilledValues.TryGetValue(fieldNameLower, out var prefilledVal))
{
    prefilledValue = prefilledVal;
}
// Try normalized match
else if (Model.PrefilledValues.TryGetValue(normalizedFieldName, out prefilledVal))
{
    prefilledValue = prefilledVal;
}
```

**How It Works**:
1. **Field in CMS**: "Apelyido (Last Name)"
2. **Normalized**: "apelyidolastname" (removes `(`, `)`, spaces)
3. **Match Found**: Pre-fill dictionary has `apelyidolastname` key ✅
4. **Value Set**: User's last name fills the field automatically

---

### Fix 2: Section Break Rendering

**Added Section Break Detection**:
```csharp
// Skip section breaks - they're rendered separately
if (field.FieldType == "section" || field.FieldType == "divider" || field.FieldType == "heading")
{
    // Render section break
    <div class="form-section-break">
        @if (!string.IsNullOrEmpty(field.Title))
        {
            <h4 class="section-title">@field.Title</h4>
        }
        @if (!string.IsNullOrEmpty(field.FieldLabel))
        {
            <p class="section-description">@field.FieldLabel</p>
        }
    </div>
    continue; // Skip to next field
}
```

**Added Beautiful CSS Styling**:
```css
.form-section-break {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    padding: 24px 28px;
    margin: 24px 12px;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(102, 126, 234, 0.25);
}

.section-title {
    font-size: 20px;
    font-weight: 600;
    color: white;
}

.section-description {
    font-size: 14px;
    color: rgba(255, 255, 255, 0.9);
}
```

---

## Files Modified

### 1. **`Pages/Forms/SubmitForm.cshtml.cs`**
   - **Lines 435-543**: Added field name variations for better matching
   - **Lines 303-310**: Added `NormalizeFieldName()` helper method

### 2. **`Pages/Forms/SubmitForm.cshtml`**
   - **Lines 328-351**: Added section break CSS styling
   - **Lines 443-482**: Added section break rendering logic and normalized field matching

---

## How to Use in Form Builder (Admin)

### Creating Fields with Pre-fill

When creating form fields in the CMS Form Builder:

1. **Field Name Options** (any of these will auto-fill):
   - Last Name: `apelyido`, `last_name`, `Apelyido (Last Name)`, etc.
   - First Name: `unang_pangalan`, `first_name`, `Unang Pangalan (First Name)`, etc.
   - Middle Name: `gitnang_pangalan`, `middle_name`, etc.
   - Age: `edad`, `age`, `Edad (Age)`, etc.
   - Sex: `kasarian`, `sex`, `gender`, `Kasarian (Sex)`, etc.

2. **The system automatically**:
   - Fills the field with patient data
   - Makes the field read-only
   - Shows the actual value instead of "Your answer"

### Creating Section Breaks

To create section headers in forms:

1. **In Form Builder**: Add a new field
2. **Field Type**: Select `section`, `divider`, or `heading`
3. **Title**: Enter section heading (e.g., "Personal Information")
4. **Field Label**: Enter optional description
5. **Result**: Beautiful purple gradient section header appears in form

---

## Testing Checklist

### Test 1: Pre-fill Functionality ✅
1. Book an appointment as a patient
2. Get redirected to NCD Risk Assessment form
3. Verify fields are pre-filled:
   - ✅ Last Name (Apelyido)
   - ✅ First Name (Unang Pangalan)
   - ✅ Middle Name (Gitnang Pangalan)
   - ✅ Age (Edad)
   - ✅ Sex (Kasarian)
   - ✅ Fields are read-only (can't edit)

### Test 2: Section Breaks ✅
1. Go to Admin → Form Builder
2. Create/Edit the NCD Risk Assessment form
3. Add a field with type "section"
4. Set Title: "Personal Information"
5. Save and view form
6. Verify: Purple gradient section header appears

### Test 3: Field Naming Flexibility ✅
1. In Form Builder, create fields with various names:
   - "Apelyido (Last Name)" → Pre-fills ✅
   - "apelyido" → Pre-fills ✅
   - "last_name" → Pre-fills ✅
   - "Edad (Age)" → Pre-fills ✅
   - "edad" → Pre-fills ✅

---

## Pre-fill Data Sources

The system automatically pre-fills from:

1. **Appointment Data** (if linked via `appointmentId`):
   - Name, Age, Gender, Contact Number
   - Dependent info (if booking for someone else)
   - Family Number

2. **User Profile**:
   - FirstName, LastName, MiddleName
   - BirthDate, Age, Gender
   - Address, Barangay
   - Phone Number

3. **Patient Record** (if exists):
   - All patient demographic data
   - Medical history references

---

## Pre-filled vs Editable Fields

### Always Pre-filled & Read-only:
- ✅ Last Name, First Name, Middle Name
- ✅ Age, Date of Birth
- ✅ Sex/Gender
- ✅ Address, Barangay
- ✅ Contact Number
- ✅ Family Number
- ✅ Health Facility
- ✅ Date of Assessment (current date)

### User Must Fill:
- ❌ Religion (not auto-filled)
- ❌ Civil Status (not auto-filled)
- ❌ All clinical/medical questions
- ❌ Assessment-specific fields

---

## Technical Details

### Normalization Algorithm

**Input**: "Apelyido (Last Name)"

**Process**:
1. Convert to lowercase: `"apelyido (last name)"`
2. Remove non-alphanumeric: `"apelyidolastname"`
3. Match against dictionary key: `"apelyidolastname"` ✅

**Result**: Pre-fill value found and applied!

### Supported Field Types for Section Breaks

The following field types render as section breaks:
- `section`
- `divider`
- `heading`

All other types render as normal input fields.

---

## Example Form Structure

```
[SECTION: Personal Information]
- Apelyido (Last Name) → Pre-filled: "Garcia" [Read-only]
- Unang Pangalan (First Name) → Pre-filled: "Juan" [Read-only]
- Gitnang Pangalan (Middle Name) → Pre-filled: "Dela" [Read-only]
- Edad (Age) → Pre-filled: "35" [Read-only]
- Kasarian (Sex) → Pre-filled: "Male" [Read-only]

[SECTION: Medical History]
- Blood Pressure → User input [Editable]
- Existing Conditions → User input [Editable]
- Current Medications → User input [Editable]
```

---

## Troubleshooting

### Issue: Field not pre-filling

**Possible causes**:
1. Field name doesn't match any variation in dictionary
2. User has no data for that field
3. Field is not linked to appointment

**Solution**:
- Use standard field names: `apelyido`, `unang_pangalan`, `edad`, `kasarian`
- Or use normalized versions: `apelyidolastname`, `unangpangalanfirstname`

### Issue: Section break not showing

**Possible causes**:
1. Field type is not `section`, `divider`, or `heading`
2. CSS not loaded

**Solution**:
- Verify field type in Form Builder
- Check browser console for errors

---

## Summary

✅ **Pre-fill now works** with flexible field naming (handles parentheses and spaces)
✅ **Section breaks render** beautifully with gradient styling
✅ **Backward compatible** - old field names still work
✅ **User-friendly** - forms auto-populate from patient data
✅ **Admin-friendly** - flexible field naming in Form Builder

**No database migration needed** - These are code-only fixes!

---

**Status**: ✅ FIXED - Ready for Testing
**Impact**: Improves user experience by auto-filling patient data and organizing forms with section breaks
