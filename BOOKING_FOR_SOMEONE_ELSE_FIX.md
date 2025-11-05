# ✅ Booking for Someone Else - Complete Fix

## 🐛 Problem Identified

When booking an appointment for someone else (dependent), the HEEADSSS and NCD assessment forms were displaying the **logged-in user's information** instead of the **dependent's information**. Additionally, the family number was not showing in the appointment context.

### Root Cause:

The `BookAppointment` page was storing dependent information in the wrong database fields:
- ❌ Stored dependent's name in `PatientName` (should be booker's name)
- ❌ Did not populate `DependentFullName` field
- ❌ Did not populate `DependentAge` field

The forms (HEEADSSS and NCD) were correctly checking for `DependentFullName`, but since it was never set, they fell back to showing the logged-in user's data.

---

## ✅ Fixes Applied

### 1. **Fixed BookAppointment to Store Dependent Data Correctly**

**File:** `Pages/BookAppointment.cshtml.cs`

#### Before:
```csharp
var patientName = bookingForOther ? bookingModel.FullName : user.FullName;
var newAppointment = new Models.Appointment
{
    PatientName = patientName,  // ❌ Wrong: storing dependent's name here
    // DependentFullName was never set
    // ...
};
```

#### After:
```csharp
// FIXED: Store patient data differently based on who the appointment is for
string patientName;
string? dependentFullName = null;
int? dependentAge = null;

if (bookingForOther)
{
    // For dependent bookings: store booker's name in PatientName, dependent's info in Dependent* fields
    patientName = user.FullName; // Booker's name
    dependentFullName = bookingModel.FullName; // Dependent's name
    dependentAge = bookingModel.Age;
    dependentBirthday = bookingModel.Birthday;
}
else
{
    // For self bookings: only PatientName is used
    patientName = user.FullName;
}

var newAppointment = new Models.Appointment
{
    PatientName = patientName, // ✅ Always the booker's name
    DependentFullName = dependentFullName, // ✅ Dependent's name (null if booking for self)
    DependentAge = dependentAge, // ✅ Dependent's age (null if booking for self)
    AgeValue = patientAge, // ✅ Age of the person receiving care
    DateOfBirth = patientBirthday, // ✅ DOB of the person receiving care
    BookingForOther = bookingForOther,
    Relationship = bookingForOther ? bookingModel.Relationship : null,
    FamilyNumber = familyNumber,
    // ...
};
```

**What Changed:**
- `PatientName` now **always** stores the logged-in user (booker) name
- `DependentFullName` stores the dependent's name when `BookingForOther = true`
- `DependentAge` stores the dependent's age when `BookingForOther = true`
- `AgeValue` and `DateOfBirth` store the information of whoever is receiving care (dependent or self)

---

### 2. **Added Appointment Context Display to HEEADSSS Form**

**Files:** 
- `Pages/User/HEEADSSSAssessment.cshtml.cs` (code-behind)
- `Pages/User/HEEADSSSAssessment.cshtml` (view)

#### Added to Code-Behind:
```csharp
// Store appointment context data for display in the view
ViewData["AppointmentContext_DisplayName"] = !string.IsNullOrEmpty(appointment.DependentFullName) 
    ? appointment.DependentFullName 
    : appointment.PatientName;
ViewData["AppointmentContext_DisplayAge"] = appointment.DependentAge ?? appointment.AgeValue;
ViewData["AppointmentContext_BookedBy"] = appointment.PatientName;
ViewData["AppointmentContext_AppointmentDate"] = appointment.AppointmentDate.ToString("MMM dd, yyyy");
ViewData["AppointmentContext_FamilyNumber"] = appointment.FamilyNumber;
ViewData["AppointmentContext_BookingForOther"] = appointment.BookingForOther;
ViewData["AppointmentContext_Relationship"] = appointment.Relationship;
```

#### Added to View (Before the Form):
```html
<div class="alert alert-primary" role="alert">
    <i class="fas fa-clipboard-list me-2"></i>
    <strong>Appointment Context</strong>
    <div class="mt-2">
        <div><strong>Patient:</strong> John Doe</div>
        <div><strong>Age:</strong> 15 years old</div>
        <div><strong>Appointment Date:</strong> Nov 15, 2025</div>
        <div><strong>Family Number:</strong> G-0001</div>
        <div><strong>Booked by:</strong> Jane Garcia (Mother)</div>
    </div>
</div>
```

**What It Shows:**
- ✅ **Patient:** The name of the person the appointment is for (dependent or self)
- ✅ **Age:** The age of the patient
- ✅ **Appointment Date:** When the appointment is scheduled
- ✅ **Family Number:** The family number assigned (if available)
- ✅ **Booked by:** Who booked the appointment and their relationship (only shows if booking for someone else)

---

### 3. **Added Appointment Context Display to NCD Form**

**Files:** 
- `Pages/User/NCDRiskAssessment.cshtml.cs` (code-behind)
- `Pages/User/NCDRiskAssessment.cshtml` (view)

Same implementation as HEEADSSS form above. The appointment context box now appears at the top of the NCD Risk Assessment form as well.

---

## 📊 How It Works Now

### Scenario 1: Booking for Self

**User Action:** Rick Garcia books an appointment for himself

**Database Storage:**
```
Appointment {
    PatientName: "Rick Garcia"
    DependentFullName: null
    DependentAge: null
    AgeValue: 22
    BookingForOther: false
    Relationship: null
    FamilyNumber: "G-0001"
}
```

**Form Display:**
```
┌─────────────────────────────────┐
│  Appointment Context            │
│  Patient: Rick Garcia           │
│  Age: 22 years old              │
│  Appointment Date: Nov 15, 2025 │
│  Family Number: G-0001          │
└─────────────────────────────────┘
```

### Scenario 2: Booking for Someone Else

**User Action:** Rick Garcia books an appointment for his son, John Garcia (age 15)

**Database Storage:**
```
Appointment {
    PatientName: "Rick Garcia"  // Booker
    DependentFullName: "John Garcia"  // Dependent
    DependentAge: 15
    AgeValue: 15
    BookingForOther: true
    Relationship: "Son"
    FamilyNumber: "G-0001"
}
```

**Form Display:**
```
┌─────────────────────────────────┐
│  Appointment Context            │
│  Patient: John Garcia           │
│  Age: 15 years old              │
│  Appointment Date: Nov 15, 2025 │
│  Family Number: G-0001          │
│  Booked by: Rick Garcia (Son)   │
└─────────────────────────────────┘
```

---

## 🎨 Visual Examples

### Before Fix:
When Rick books for his son John (age 15), the form showed:
```
Patient: Rick Garcia  ❌ WRONG (showed booker, not dependent)
Age: 22 years old     ❌ WRONG (showed booker's age)
```

### After Fix:
When Rick books for his son John (age 15), the form now shows:
```
Patient: John Garcia     ✅ CORRECT (shows dependent)
Age: 15 years old        ✅ CORRECT (shows dependent's age)
Family Number: G-0001    ✅ ADDED (family number now visible)
Booked by: Rick Garcia (Son)  ✅ ADDED (shows who booked it)
```

---

## 🧪 Testing Instructions

### Test 1: Booking for Self

1. **Login** as a user (e.g., Rick Garcia)
2. **Go to** Book Appointment page
3. **Keep** "Booking for someone else" **UNCHECKED**
4. **Fill out** appointment details with your own information
5. **Select** a family number or generate one
6. **Submit** the appointment
7. **Click** "Complete Form" when it appears in your appointments
8. **Verify** the appointment context shows:
   - ✅ Your name as the patient
   - ✅ Your age
   - ✅ Your family number
   - ✅ No "Booked by" line (since you booked for yourself)

### Test 2: Booking for Someone Else

1. **Login** as a user (e.g., Rick Garcia)
2. **Go to** Book Appointment page
3. **CHECK** "Booking for someone else"
4. **Fill out** dependent's information:
   - Full Name: John Garcia
   - Age: 15
   - Gender: Male
   - Relationship: Son
5. **Select** a family number or generate one
6. **Submit** the appointment
7. **Click** "Complete Form" when it appears in your appointments
8. **Verify** the appointment context shows:
   - ✅ **Dependent's name** as the patient (John Garcia)
   - ✅ **Dependent's age** (15 years old)
   - ✅ **Family number** (G-0001 or similar)
   - ✅ **"Booked by" line** showing your name and relationship (Rick Garcia - Son)

### Test 3: Verify Form Fields

After opening the HEEADSSS or NCD form:

1. **Check** that all form fields are **pre-filled** with the **correct person's information**:
   - ✅ Patient Name field shows dependent's name (not booker's name)
   - ✅ Age field shows dependent's age
   - ✅ Birthday field shows dependent's birthday
   - ✅ Contact number shows the provided contact

2. **Check** that readonly fields are **grayed out** and cannot be edited

3. **Fill out** the assessment form

4. **Submit** the form

5. **Verify** the submission is associated with the correct appointment and patient

---

## 🔍 Data Flow Diagram

```
┌─────────────────────┐
│  BookAppointment    │
│  Page               │
└──────────┬──────────┘
           │
           │ User selects "Booking for someone else"
           │ and enters dependent info
           │
           ▼
┌─────────────────────────────────────────┐
│  BookAppointment.cshtml.cs              │
│  CreateTemporaryAppointmentAsync()      │
│                                         │
│  if (bookingForOther) {                 │
│    PatientName = user.FullName          │ ← Booker
│    DependentFullName = dependent.Name   │ ← Dependent
│    DependentAge = dependent.Age         │ ← Dependent
│  } else {                               │
│    PatientName = user.FullName          │ ← User
│    DependentFullName = null             │
│  }                                      │
└──────────┬──────────────────────────────┘
           │
           │ Saves to database
           │
           ▼
┌─────────────────────┐
│  Appointments Table │
│  (Database)         │
└──────────┬──────────┘
           │
           │ User clicks "Complete Form"
           │
           ▼
┌─────────────────────────────────────────┐
│  HEEADSSSAssessment / NCDRiskAssessment │
│  OnGetAsync()                           │
│                                         │
│  Loads appointment from database        │
│                                         │
│  displayName = DependentFullName ??     │
│                PatientName              │ ← Shows dependent if exists
│                                         │
│  Stores in ViewData for display         │
└──────────┬──────────────────────────────┘
           │
           │ Renders view
           │
           ▼
┌─────────────────────┐
│  Assessment Form    │
│  (HEEADSSS/NCD)     │
│                     │
│  ┌────────────────┐ │
│  │ Appointment    │ │
│  │ Context Box    │ │ ← Shows correct patient info
│  │                │ │
│  │ Patient: John  │ │
│  │ Age: 15        │ │
│  │ Booked by: Rick│ │
│  └────────────────┘ │
│                     │
│  [Form Fields...]   │
└─────────────────────┘
```

---

## ✅ Summary of Changes

### Files Modified:

1. **`Pages/BookAppointment.cshtml.cs`**
   - Fixed dependent data storage logic
   - Now correctly populates `DependentFullName` and `DependentAge` fields

2. **`Pages/User/HEEADSSSAssessment.cshtml.cs`**
   - Added ViewData entries for appointment context display

3. **`Pages/User/HEEADSSSAssessment.cshtml`**
   - Added appointment context display box at the top of the form

4. **`Pages/User/NCDRiskAssessment.cshtml.cs`**
   - Added ViewData entries for appointment context display

5. **`Pages/User/NCDRiskAssessment.cshtml`**
   - Added appointment context display box at the top of the form

### Database Schema:

No database migrations required! The `Appointment` model already had these fields:
- ✅ `DependentFullName` (existed but was not being used)
- ✅ `DependentAge` (existed but was not being used)
- ✅ `BookingForOther` (existed and was being used correctly)
- ✅ `Relationship` (existed and was being used correctly)
- ✅ `FamilyNumber` (existed and was being used correctly)

We just fixed the code to **properly use** these existing fields!

---

## 🎉 Result

**Before:**
- ❌ Forms showed booker's information instead of dependent's
- ❌ Family number was not visible
- ❌ No indication of who booked the appointment

**After:**
- ✅ Forms correctly show dependent's information
- ✅ Family number is displayed in appointment context
- ✅ Clear indication of who booked the appointment and their relationship
- ✅ Users can see at a glance who the form is for

---

## 📝 Build Status

**Status:** ✅ **Build succeeded** (0 errors, 2 warnings)

The warnings are minor nullability warnings in unrelated files and don't affect functionality.

**Ready to test!** 🚀

