# Developer Mode - Implementation Guide

## 📋 Overview
Added a **Developer Mode** toggle switch (renamed from Testing Mode) to the Admin Dashboard for system-wide testing and development purposes. The mode can be toggled ON/OFF from the admin panel and applies to various modules including vital signs recording.

---

## ✨ Features Added

### 1. **Developer Mode Toggle Switch**
- **Location:** Admin Dashboard (`/Admin/AdminDashboard`) - **ONLY**
- Positioned beside the "Add Staff Member" button in the header
- **Icon:** Flask icon (🧪) with "Dev Mode" label
- Toggle ON/OFF with a single click
- Visual indicator: Yellow "ON" badge when enabled
- State persists across page refreshes using TempData
- **Centralized Control:** All pages check this single switch

### 2. **Restrictions Bypassed in Testing Mode**

#### **Normal Mode (Testing Mode OFF)**
- ✅ Only shows today's appointments
- ✅ Only loads patients with today's appointments
- ✅ Filters out patients who already have vital signs today
- ✅ Only updates today's appointment status

#### **Testing Mode (Testing Mode ON)**
- ✅ Shows ALL appointments (past, present, future)
- ✅ Loads ALL patients with any appointments
- ✅ Allows recording multiple vital signs for same patient
- ✅ Updates any "In Progress" appointment status
- ✅ No date validation

---

## 🎯 How to Use

### **Enable Developer Mode**
1. Navigate to `/Admin/AdminDashboard`
2. Locate the **Dev Mode** toggle switch (beside "Add Staff Member" button)
3. Click the toggle switch to enable
4. Page reloads with warning banner: "Developer Mode Enabled"
5. Mode is now active across all pages (VitalSigns, etc.)

### **Disable Developer Mode**
1. Return to `/Admin/AdminDashboard`
2. Click the **Dev Mode** toggle switch to disable
3. System returns to normal operation with all restrictions

### **Visual Indicators on Other Pages**
- Pages like `/Nurse/VitalSigns` will show a warning banner when Dev Mode is active
- Banner includes a link back to Admin Dashboard to toggle off

---

## 📊 UI Changes in Testing Mode

### **Admin Dashboard**
- **Toggle Switch:** Shows "ON" badge when enabled
- **Warning Banner:** Yellow alert below header indicating Dev Mode is active

### **Vital Signs Page**
- **Warning Banner:**
  ```
  ⚠️ Developer Mode Active: All date and appointment restrictions are disabled. 
  You can record vital signs for any patient on any day.
  Toggle this mode from the Admin Dashboard.
  ```
- **Appointments Table Header:** "All Appointments (Testing Mode)" instead of "Today's Appointments"
- **New Column:** Date column shows appointment dates
- **Sorting:** Appointments sorted by date then time

---

## 🔧 Technical Implementation

### **Files Modified**

#### **1. AdminDashboard.cshtml.cs** (Backend)
- Added `TestingMode` property
- Modified `OnGetAsync()` to accept `testingMode` parameter
- Stores/retrieves testing mode from TempData

#### **2. AdminDashboard.cshtml** (Frontend)
- Added Dev Mode toggle switch beside "Add Staff Member" button
- Added warning banner when Developer Mode is active
- Added JavaScript `toggleTestingMode()` function

#### **3. VitalSigns.cshtml.cs** (Backend)
- Added `TestingMode` property
- Modified `OnGetAsync()` to read `testingMode` from TempData
- Updated `LoadTodayAppointmentsAsync()` to bypass date filter
- Updated `LoadPatientsWithTodayAppointmentsAsync()` to bypass date filter
- Modified `OnPostAddVitalSignAsync()` to bypass today's date check
- Added `AppointmentDate` to `TodayAppointmentViewModel`

#### **4. VitalSigns.cshtml** (Frontend)
- Added warning banner for when Developer Mode is active
- Banner includes link to Admin Dashboard to toggle mode
- Updated table header to show "All Appointments" in testing mode
- Added date column when testing mode is active

---

## 📝 Code Changes Summary

### **Key Methods Modified**

```csharp
// VitalSigns.cshtml.cs

// Property added
public bool TestingMode { get; set; }

// OnGet accepts testingMode parameter
public async Task<IActionResult> OnGetAsync(string patientId, bool? testingMode)

// LoadTodayAppointmentsAsync bypasses date filter
if (!TestingMode)
{
    query = query.Where(a => a.AppointmentDate.Date == Today);
}

// LoadPatientsWithTodayAppointmentsAsync bypasses date filter
if (!TestingMode)
{
    query = query.Where(a => a.AppointmentDate.Date == Today);
}

// OnPostAddVitalSignAsync bypasses today check
if (!TestingMode)
{
    appointmentQuery = appointmentQuery.Where(a => a.AppointmentDate.Date == Today);
}
```

---

## ⚠️ Important Notes

### **For Testing Only**
- This mode is designed for testing/development purposes
- Should NOT be used in production without proper access controls
- Consider adding role-based restrictions (e.g., only System Admin can enable)

### **Data Integrity**
- Testing mode allows recording vital signs outside normal workflow
- Appointments may be marked as "Completed" even if not today
- Be cautious when using in production environment

### **State Persistence**
- Testing mode state is stored in `TempData`
- Persists across redirects within same session
- Cleared when session ends or browser closes

---

## 🚀 Testing Scenarios

### **Scenario 1: Record Vital Signs on Weekend**
1. Enable Testing Mode on Saturday
2. All appointments (including weekday ones) appear
3. Select any patient and record vital signs
4. Save successfully without date validation

### **Scenario 2: Record Multiple Vital Signs for Same Patient**
1. Enable Testing Mode
2. Record vital signs for Patient A
3. Patient A remains in dropdown
4. Record vital signs for Patient A again
5. Both records saved successfully

### **Scenario 3: Record Vital Signs for Past Appointments**
1. Enable Testing Mode
2. Appointments from previous weeks appear
3. Select any past appointment
4. Record and save vital signs successfully

---

## 🔄 Reverting to Normal Mode

### **To Re-enable Restrictions:**
1. Simply toggle Testing Mode OFF
2. Page immediately enforces:
   - Today's date only
   - No duplicate vital signs per patient per day
   - Appointment status updates only for today

### **To Remove Testing Mode Completely:**
1. Remove toggle switch from `VitalSigns.cshtml`
2. Remove `TestingMode` property from `VitalSigns.cshtml.cs`
3. Revert conditional logic in:
   - `LoadTodayAppointmentsAsync()`
   - `LoadPatientsWithTodayAppointmentsAsync()`
   - `OnPostAddVitalSignAsync()`

---

## 📞 Support

For questions or issues:
- Check console logs: `_logger.LogInformation` statements added
- Verify TempData persistence: Check `TempData["TestingMode"]`
- Review restrictions: Each conditional checks `TestingMode` property

---

**Created:** October 25, 2025  
**Version:** 1.0  
**Status:** ✅ Testing Mode Active
