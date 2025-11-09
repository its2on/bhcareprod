# Role-Based Forms Guide

## Overview

The form submission system now supports different forms for different user roles. This allows you to create role-specific versions of forms that show different fields or layouts based on who is accessing them.

## How It Works

### Form Key Naming Convention

The system uses a naming convention to find role-specific forms:

1. **Patient Form**: `{formKey}` (e.g., `ncd-risk-assessment-form`)
2. **Nurse Form**: `{formKey}-nurse` (e.g., `ncd-risk-assessment-form-nurse`)
3. **Doctor Form**: `{formKey}-doctor` (e.g., `ncd-risk-assessment-form-doctor`)

### Form Loading Priority

When a user accesses a form, the system:

1. **Checks for role-specific form first**
   - Nurse → Looks for `{formKey}-nurse`
   - Doctor/Admin → Looks for `{formKey}-doctor`
   - Patient → Uses standard form

2. **Falls back to standard form**
   - If role-specific form doesn't exist, uses the standard `{formKey}` form
   - This ensures backward compatibility

## Creating Role-Specific Forms

### Step 1: Create the Standard Form (Patient)

1. Go to **Admin → Form Management**
2. Click **Add New Form**
3. Set **Form Key**: `ncd-risk-assessment-form`
4. Add all fields for patient use
5. Save

### Step 2: Create Nurse-Specific Form

1. Go to **Admin → Form Management**
2. Click **Add New Form**
3. Set **Form Key**: `ncd-risk-assessment-form-nurse` (add `-nurse` suffix)
4. Add additional fields or modify existing ones for nurse use
5. Save

### Step 3: Create Doctor-Specific Form

1. Go to **Admin → Form Management**
2. Click **Add New Form**
3. Set **Form Key**: `ncd-risk-assessment-form-doctor` (add `-doctor` suffix)
4. Add additional fields or modify existing ones for doctor use
5. Save

## Features

### Different Layouts

- **Patient**: Uses `_UserLayout.cshtml`
- **Nurse**: Uses `_NurseLayout.cshtml`
- **Doctor/Admin**: Uses `_DoctorLayout`

### Edit Permissions

- **Patient**: Forms become read-only after submission
- **Nurse/Doctor/Admin**: Can edit forms even after submission

### Navigation

After form submission:
- **Patient**: Redirects to `/User/Dashboard`
- **Nurse**: Redirects to `/Nurse/AppointmentDetails?id={appointmentId}`
- **Doctor/Admin**: Redirects to `/Doctor/Consultation?id={appointmentId}`

## Example: NCD Risk Assessment

### Standard Form (Patient)
- **Form Key**: `ncd-risk-assessment-form`
- **Fields**: Basic patient information, risk factors
- **Read-only after submission**: Yes

### Nurse Form
- **Form Key**: `ncd-risk-assessment-form-nurse`
- **Fields**: All patient fields + additional clinical notes, nurse observations
- **Read-only after submission**: No (nurses can edit)

### Doctor Form
- **Form Key**: `ncd-risk-assessment-form-doctor`
- **Fields**: All patient fields + diagnosis, treatment plan, doctor notes
- **Read-only after submission**: No (doctors can edit)

## URL Structure

All forms use the same URL structure:
```
/Forms/SubmitForm/{formKey}?appointmentId={id}
```

The system automatically loads the appropriate form based on the user's role.

## Benefits

1. **Role-Specific Fields**: Show different fields to different roles
2. **Better UX**: Each role sees forms tailored to their needs
3. **Backward Compatible**: If role-specific form doesn't exist, falls back to standard form
4. **Flexible**: Can create as many role-specific forms as needed

## Notes

- Role-specific forms are **optional** - if they don't exist, the standard form is used
- All forms must have the same base `formKey` (before the role suffix)
- Forms are linked to appointments via `appointmentId` parameter
- Form submissions are stored in the same `FormSubmissions` table regardless of role

