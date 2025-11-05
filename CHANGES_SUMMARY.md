# Changes Summary - Appointment Time Slot Update

## What Was Changed

### 1. Time Slot Configuration: 8:00 AM to 5:00 PM ✅

#### Updated Files:
1. **`Models/DoctorAvailability.cs`**
   - `StartTime`: 8:00 AM (was 7:30 AM)
   - `EndTime`: 5:00 PM (unchanged)
   - `MaxAppointmentsPerDay`: 100 slots (was 30)
   - `SlotDurationMinutes`: 5 minutes (was 18)
   - Working hours: 9 hours = 540 minutes
   - Calculation: 540 minutes ÷ 100 slots = 5.4 minutes per slot

2. **`Services/DatabaseSeeder.cs`**
   - Updated default values for new doctor records
   - Sets 8:00 AM - 5:00 PM schedule
   - Sets 100 slots per day

3. **`Migrations/20251103060000_UpdateDoctorAvailabilityTimeTo8AMto5PM.cs`**
   - Created SQL migration to update existing records
   - Updates all `DoctorAvailabilities` table records

## How to Apply Database Changes

### Option 1: Using EF Core CLI (Recommended)
```bash
# Navigate to project directory
cd "C:\Users\WIN 10\Desktop\BHCARE-main"

# Apply migration
dotnet ef database update --context ApplicationDbContext
```

### Option 2: Using Package Manager Console
```powershell
Update-Database -Context ApplicationDbContext
```

### Option 3: Run SQL Directly on Azure SQL Database
```sql
UPDATE DoctorAvailabilities 
SET StartTime = '08:00:00', 
    EndTime = '17:00:00',
    MaxAppointmentsPerDay = 100,
    SlotDurationMinutes = 5,
    LastUpdated = GETDATE()
WHERE StartTime != '08:00:00' OR EndTime != '17:00:00'
```

## Appointment Booking Flow - Complete Overview

### Patient Journey:
1. **Book Appointment** (`/BookAppointment`)
   - Select date (validates against doctor availability)
   - Select consultation type
   - Choose time slot (100 slots available: 8:00 AM - 5:00 PM)
   - Fill patient details

2. **Status After Booking**:
   - **Draft** (if General Consult - assessment required)
   - **Pending** (if Immunization, Prenatal, DOTS, Dental - no assessment)

3. **Complete Assessment** (if required):
   - NCD Risk Assessment (`/User/NCDRiskAssessment`)
   - OR HEEADSSS Assessment (`/User/HEEADSSSAssessment`)
   - **Status changes**: Draft → InProgress

### Nurse Workflow:
1. **View Appointments** (`/Nurse/Appointments`)
   - Sees: Pending, InProgress, Confirmed, Completed
   - Does NOT see: Draft (patient hasn't completed assessment)

2. **Record Vital Signs** (`/Nurse/VitalSigns`)
   - Select patient from today's list
   - Record: BP, Temp, Pulse, Respiratory Rate, Weight, Height
   - Data saved to `VitalSigns` table
   - Linked to appointment via `AppointmentId`

### Doctor Workflow:
1. **View Appointments** (`/Doctor/Appointments`)
   - Sees all assigned appointments
   - Access to:
     - Patient details (decrypted)
     - Assessment forms (NCD or HEEADSSS)
     - Vital signs (recorded by nurse)
   
2. **Consultation**:
   - Add consultation notes
   - Prescribe medications
   - Upload documents
   - **Status changes**: InProgress → Completed

## Database Tables Involved

### Core Tables:
1. **Appointments**
   - Main appointment record
   - Stores: Patient, Doctor, Date, Time, Status, Type
   - Encrypted fields: PatientName, ContactNumber, Address, ReasonForVisit

2. **DoctorAvailabilities**
   - Doctor schedule configuration
   - StartTime, EndTime, MaxAppointmentsPerDay
   - Working days (Monday-Sunday flags)

3. **VitalSigns**
   - Patient vitals recorded by nurse
   - Linked via `AppointmentId`
   - Encrypted fields: All vital measurements

4. **NCDRiskAssessments**
   - Health screening data
   - Linked via `AppointmentId`
   - Encrypted fields: All assessment responses

5. **HEEADSSSAssessments**
   - Psychosocial assessment
   - Linked via `AppointmentId`
   - Encrypted fields: All assessment responses

## Appointment Status Flow

```
Patient Books
     ↓
[Assessment Required?]
     ↓
YES              NO
 ↓                ↓
DRAFT -----→  PENDING
 ↓                ↓
[Patient Completes Assessment]
 ↓                
INPROGRESS ←-----┘
 ↓
[Nurse Records Vitals]
 ↓
INPROGRESS
 ↓
[Doctor Consultation]
 ↓
COMPLETED
```

### Status Values:
- **Draft (7)**: Appointment created, assessment incomplete
- **Pending (0)**: Booked and ready, or no assessment required
- **InProgress (2)**: Assessment completed, in queue for nurse/doctor
- **Confirmed (1)**: Doctor confirmed the appointment
- **Completed (3)**: Consultation finished
- **Cancelled (4)**: Appointment cancelled
- **Urgent (5)**: Requires immediate attention
- **NoShow (6)**: Patient didn't attend

## Data Storage & Encryption

### Encrypted Fields:
All sensitive data is encrypted at rest using the `[Encrypted]` attribute:
- Patient names
- Contact information
- Medical history
- Assessment responses
- Vital signs
- Prescriptions

### Decryption Rules:
- **Patients**: Can see their own data
- **Doctors**: Can see assigned patients' data
- **Nurses**: Can see today's appointments' data
- **Admins**: Full access to all data

## Testing After Changes

### Test Checklist:
1. ✅ **Login as Patient**
   - Go to Book Appointment
   - Select date: Tomorrow
   - Select consultation: "General Consult"
   - Check available slots: Should show 8:00 AM - 5:00 PM (100 slots)
   
2. ✅ **Book Appointment**
   - Select a time slot (e.g., 8:00 AM)
   - Fill details and submit
   - Status should be: **Draft**
   
3. ✅ **Complete Assessment**
   - Go to User Dashboard → "Complete Assessment"
   - Fill NCD Risk Assessment
   - Submit form
   - Status should change to: **InProgress**
   
4. ✅ **Login as Nurse**
   - Check Appointments page
   - Should see the appointment in the list
   - Go to Vital Signs
   - Record patient vitals
   - Verify saved to database
   
5. ✅ **Login as Doctor**
   - Check Appointments page
   - Click on appointment
   - Should see:
     - Patient details
     - Assessment form responses
     - Vital signs recorded by nurse
   - Complete consultation
   - Status should change to: **Completed**

### Verify Database:
```sql
-- Check appointment record
SELECT * FROM Appointments WHERE Id = [AppointmentId];

-- Check assessment
SELECT * FROM NCDRiskAssessments WHERE AppointmentId = [AppointmentId];

-- Check vital signs
SELECT * FROM VitalSigns WHERE AppointmentId = [AppointmentId];

-- Check doctor availability
SELECT * FROM DoctorAvailabilities;
-- Should show StartTime = 08:00:00, EndTime = 17:00:00, MaxAppointmentsPerDay = 100
```

## Files Created/Modified

### New Files:
1. `Migrations/20251103060000_UpdateDoctorAvailabilityTimeTo8AMto5PM.cs`
2. `APPOINTMENT_FLOW_DOCUMENTATION.md` (Complete documentation)
3. `CHANGES_SUMMARY.md` (This file)

### Modified Files:
1. `Models/DoctorAvailability.cs`
2. `Services/DatabaseSeeder.cs`
3. `Services/AppointmentSlotService.cs` (Fixed EF Core translation errors)
4. `Services/AppointmentService.cs` (Fixed EF Core translation errors)
5. `Controllers/AppointmentController.cs` (Fixed EF Core translation errors)
6. `Controllers/AppointmentsController.cs` (Fixed EF Core translation errors)
7. `Controllers/UserApiController.cs` (Fixed EF Core translation errors)
8. `Controllers/ReportsApiController.cs` (Fixed EF Core translation errors)
9. `Controllers/NurseApiController.cs` (Fixed EF Core translation errors)

## Previous Issue Fixed

### EF Core Translation Error:
**Error Message**:
```
System.InvalidOperationException: The LINQ expression 'DbSet<Appointment>()
    .Where(a => a.DoctorId == __doctorId_0 && DateTimeHelper.AreDatesEqual(
        date1: a.AppointmentDate,
        date2: __date_1) && (int)a.Status != 4)' could not be translated.
```

**Solution Applied**:
Replaced all instances of `DateTimeHelper.AreDatesEqual()` in LINQ queries with direct date comparisons:
- `DateTimeHelper.AreDatesEqual(a.AppointmentDate, date)` → `a.AppointmentDate.Date == date.Date`

This allows Entity Framework to properly translate the query to SQL.

## Next Steps

1. **Apply Migration**: Run the database update command above
2. **Test the Flow**: Follow the testing checklist
3. **Monitor Logs**: Check for any errors in appointment booking
4. **Verify Slots**: Confirm 100 slots are displayed from 8:00 AM to 5:00 PM

## Support

For detailed documentation, see:
- `APPOINTMENT_FLOW_DOCUMENTATION.md` - Complete system flow
- Migration file for SQL changes
- This summary for quick reference

---
**Date**: November 3, 2025
**Changes By**: System Update
**Status**: ✅ Ready for Testing
