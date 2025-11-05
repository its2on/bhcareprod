# Appointment Booking System - Complete Flow Documentation

## Overview
This document describes the complete appointment booking flow in the BHCARE system, including time slot configuration, data storage, and workflow from patient booking through nurse and doctor involvement.

---

## 1. Time Slot Configuration

### Default Schedule
- **Start Time**: 8:00 AM (08:00:00)
- **End Time**: 5:00 PM (17:00:00)
- **Total Working Hours**: 9 hours (540 minutes)
- **Default Slots Per Day**: 100 slots
- **Average Slot Duration**: ~5.4 minutes per slot

### Configuration Files
1. **Model**: `Models/DoctorAvailability.cs`
   - `StartTime = new TimeSpan(8, 0, 0)` - 8:00 AM
   - `EndTime = new TimeSpan(17, 0, 0)` - 5:00 PM
   - `MaxAppointmentsPerDay = 100` - Maximum slots available
   - `SlotDurationMinutes = 5` - Calculated slot duration

2. **Database Seeder**: `Services/DatabaseSeeder.cs`
   - Creates default availability records for all doctors
   - Sets 8:00 AM - 5:00 PM schedule by default

3. **Migration**: `Migrations/20251103060000_UpdateDoctorAvailabilityTimeTo8AMto5PM.cs`
   - Updates existing database records to 8:00 AM - 5:00 PM
   - Sets MaxAppointmentsPerDay to 100
   - Sets SlotDurationMinutes to 5

### Working Days
- **Default Enabled**: Monday - Friday
- **Default Disabled**: Saturday, Sunday (Weekends)
- Configurable per doctor through Admin settings

---

## 2. Appointment Booking Flow

### Step 1: Patient Initiates Booking
**Page**: `Pages/BookAppointment.cshtml.cs`

1. **Patient selects**:
   - Date (validates against doctor availability and weekends)
   - Consultation Type:
     - General Consult (requires NCD or HEEADSSS assessment)
     - Immunization (no assessment required)
     - Prenatal & Family Planning (no assessment required)
     - DOTS Consult (no assessment required)
     - Dental (no assessment required)
   - Time Slot (from available slots)
   - Doctor (optional, system can assign)
   - Patient details (if booking for someone else)

2. **Available Slot Check**:
   - System queries `DoctorAvailabilities` table
   - Generates time slots based on doctor's schedule
   - Filters out already booked slots from `Appointments` table
   - Returns list of available slots

3. **Slot Validation**:
   ```csharp
   // Services/AppointmentSlotService.cs - Line 74-79
   var bookedAppointments = await _context.Appointments
       .Where(a => a.DoctorId == doctorId &&
                  a.AppointmentDate.Date == date.Date &&
                  a.Status != AppointmentStatus.Cancelled)
       .Select(a => a.AppointmentTime)
       .ToListAsync();
   ```

### Step 2: Temporary Appointment Creation
**Method**: `CreateTemporaryAppointmentAsync()` in `BookAppointment.cshtml.cs` (Line 640-887)

**Initial Status Determination**:
```csharp
// Line 758-760
var noAssessmentTypes = new[] { "immunization", "prenatal & family planning", "prenatal and family planning", "dots consult", "dental" };
var requiresAssessment = !noAssessmentTypes.Contains(selectedConsultationType.ToLower());
var initialStatus = requiresAssessment ? AppointmentStatus.Draft : AppointmentStatus.Pending;
```

**Appointment Record Created with**:
- `Status = Draft` (if assessment required) OR `Status = Pending` (if no assessment required)
- Patient details (encrypted)
- Doctor assignment
- Date and time slot
- Consultation type
- Family number (if provided)

**Database Tables Updated**:
1. **Appointments** - Main appointment record created
2. **Patients** - Patient record ensured to exist (FK constraint)
3. **FamilyMembers** - Created if booking for someone else
4. **Notifications** - Notification sent to doctor

### Step 3: Assessment Form (If Required)
**Applicable for**: General Consult only

#### Option A: NCD Risk Assessment
**Page**: `Pages/User/NCDRiskAssessment.cshtml.cs`
- Patient fills out health screening form
- Saves to `NCDRiskAssessments` table
- Links to appointment via `AppointmentId`
- **Status Update**: `Draft` → `InProgress` (Line 1026)

#### Option B: HEEADSSS Assessment
**Page**: `Pages/User/HEEADSSSAssessment.cshtml.cs`
- For pediatric/adolescent patients
- Comprehensive psychosocial assessment
- Saves to `HEEADSSSAssessments` table
- Links to appointment via `AppointmentId`
- **Status Update**: `Draft` → `InProgress` (Line 980)

### Step 4: Appointment Visible to Nurse
**Page**: `Pages/Nurse/Appointments.cshtml.cs`

**Nurse View Includes**:
```csharp
// Line 115-118
Appointments = appointments
    .Where(a => a.Status == AppointmentStatus.Pending || 
                a.Status == AppointmentStatus.InProgress || 
                a.Status == AppointmentStatus.Confirmed ||
                a.Status == AppointmentStatus.Completed)
```

**Nurse Dashboard**: `Pages/Nurse/NurseDashboard.cshtml.cs`
- Shows today's appointments
- Counts: Waiting, In Progress, Completed
- Does NOT show Draft appointments

### Step 5: Nurse Records Vital Signs
**Page**: `Pages/Nurse/VitalSigns.cshtml.cs`

**Process**:
1. Nurse selects patient from today's appointments list
2. Records vital signs:
   - Blood Pressure
   - Temperature
   - Pulse Rate
   - Respiratory Rate
   - Weight
   - Height
   - BMI (calculated automatically)
3. Vital signs saved to `VitalSigns` table
4. Linked to appointment via `AppointmentId`
5. **Status Update**: Remains `InProgress` or `Pending`

**Visible Appointments**:
```csharp
// Line 237-239
.Where(a => a.Status == AppointmentStatus.Pending || 
           a.Status == AppointmentStatus.Confirmed ||
           a.Status == AppointmentStatus.InProgress)
```

### Step 6: Doctor Consultation
**Page**: `Pages/Doctor/Appointments.cshtml.cs`

**Doctor Can**:
1. View all appointments assigned to them
2. See patient details (decrypted for authorized user)
3. Access assessment forms (NCD or HEEADSSS)
4. View vital signs recorded by nurse
5. Add consultation notes
6. Prescribe medications
7. Upload consultation documents
8. **Status Update**: `InProgress` → `Completed`

**Doctor Dashboard**: Shows queue of patients waiting

### Step 7: Appointment Completion
**Final Status**: `Completed`

**Data Stored in Database**:
1. **Appointments Table**:
   - All booking details
   - Status = Completed
   - Doctor notes
   - Prescriptions
   - Timestamps (CreatedAt, UpdatedAt)

2. **VitalSigns Table**:
   - Patient vital signs
   - Linked via AppointmentId
   - Nurse who recorded it

3. **NCDRiskAssessments or HEEADSSSAssessments Table**:
   - Complete assessment data
   - Linked via AppointmentId
   - Risk scores/results

4. **MedicalRecords Table** (if applicable):
   - Diagnosis
   - Treatment plans
   - Follow-up instructions

---

## 3. Appointment Status Flow

```
Patient Books Appointment
         ↓
    [Assessment Required?]
         ↓
    Yes           No
     ↓             ↓
  DRAFT      PENDING
     ↓             ↓
  [Patient Completes Assessment]
     ↓
 IN PROGRESS ← PENDING
     ↓
  [Nurse Records Vital Signs]
     ↓
 IN PROGRESS
     ↓
  [Doctor Consultation]
     ↓
  COMPLETED
```

### Status Definitions

| Status | Value | Description |
|--------|-------|-------------|
| **Pending** | 0 | Appointment booked, no assessment required OR assessment completed |
| **Confirmed** | 1 | Doctor confirmed the appointment |
| **InProgress** | 2 | Patient is being seen (assessment completed, in nurse/doctor queue) |
| **Completed** | 3 | Consultation finished |
| **Cancelled** | 4 | Appointment cancelled |
| **Urgent** | 5 | Requires immediate attention |
| **NoShow** | 6 | Patient didn't attend |
| **Draft** | 7 | Appointment created but assessment not completed |

---

## 4. Database Schema Overview

### Key Tables

#### 1. Appointments
**Primary appointment record**
- `Id` - Primary Key
- `PatientId` - FK to Patients (always logged-in user)
- `DoctorId` - FK to AspNetUsers (Doctor role)
- `PatientName` - Name of person receiving care
- `DependentFullName` - If booking for someone else
- `AppointmentDate` - Date of appointment
- `AppointmentTime` - Time slot (TimeSpan)
- `Status` - AppointmentStatus enum
- `Type` - Consultation type
- `FamilyNumber` - Family grouping identifier
- `BookingForOther` - Boolean flag
- `Relationship` - If booking for dependent

#### 2. DoctorAvailabilities
**Doctor schedule configuration**
- `Id` - Primary Key
- `DoctorId` - FK to AspNetUsers
- `Monday-Sunday` - Boolean flags for working days
- `StartTime` - Work start time (8:00 AM)
- `EndTime` - Work end time (5:00 PM)
- `MaxAppointmentsPerDay` - Slot capacity (100)
- `SlotDurationMinutes` - Calculated duration (5)
- `IsAvailable` - Doctor availability toggle

#### 3. VitalSigns
**Patient vital signs (recorded by nurse)**
- `Id` - Primary Key
- `PatientId` - FK to Patients
- `AppointmentId` - FK to Appointments
- `BloodPressure` - Encrypted
- `Temperature` - Encrypted
- `PulseRate` - Encrypted
- `RespiratoryRate` - Encrypted
- `Weight` - Encrypted
- `Height` - Encrypted
- `BMI` - Calculated
- `RecordedBy` - Nurse user ID
- `RecordedAt` - Timestamp

#### 4. NCDRiskAssessments
**Non-Communicable Disease screening**
- `Id` - Primary Key
- `PatientId` - FK to Patients
- `AppointmentId` - FK to Appointments
- Multiple health screening fields (encrypted)
- Risk scores and calculations

#### 5. HEEADSSSAssessments
**Adolescent psychosocial assessment**
- `Id` - Primary Key
- `PatientId` - FK to Patients
- `AppointmentId` - FK to Appointments
- HEEADSSS assessment fields (encrypted)
- Developmental screening data

---

## 5. Data Encryption

### Encrypted Fields
All sensitive patient data is encrypted using the `[Encrypted]` attribute:
- Patient names
- Contact numbers
- Addresses
- Medical history
- Assessment responses
- Vital signs
- Prescriptions

### Decryption
Data is automatically decrypted for authorized users:
- Patients can see their own data
- Doctors can see their assigned patients
- Nurses can see patients with today's appointments
- Admins have full access

---

## 6. Notifications

### Notification Triggers
1. **Appointment Created** → Notification to doctor
2. **Assessment Completed** → Patient confirmation
3. **Vital Signs Recorded** → Patient update
4. **Consultation Completed** → Patient notification
5. **Appointment Cancelled** → Both parties notified

---

## 7. Audit Trail

All major actions are logged in the `AuditLogs` table:
- Appointment creation
- Status changes
- Assessment submissions
- Vital signs recording
- Consultation completion
- Data access (who viewed what data)

---

## 8. Running the Migration

To apply the time slot changes to your database:

```bash
# Add the migration
dotnet ef migrations add UpdateDoctorAvailabilityTimeTo8AMto5PM

# Apply to database
dotnet ef database update
```

Or use Package Manager Console:
```powershell
Add-Migration UpdateDoctorAvailabilityTimeTo8AMto5PM
Update-Database
```

This will:
- Update all existing doctor availability records to 8:00 AM - 5:00 PM
- Set MaxAppointmentsPerDay to 100
- Set SlotDurationMinutes to 5

---

## 9. Testing the Complete Flow

### Test Scenario
1. **Patient logs in** → Goes to "Book Appointment"
2. **Selects date** → 11/11/2025
3. **Selects consultation type** → "General Consult"
4. **System shows available slots** → 100 slots from 8:00 AM - 5:00 PM
5. **Patient selects slot** → 8:00 AM - 8:06 AM
6. **Appointment created** → Status: Draft
7. **Patient fills NCD assessment** → Status: InProgress
8. **Nurse logs in** → Sees appointment in queue
9. **Nurse records vital signs** → Data saved to VitalSigns table
10. **Doctor logs in** → Sees patient in queue with assessment & vitals
11. **Doctor completes consultation** → Status: Completed

### Verify Data Storage
```sql
-- Check appointment
SELECT * FROM Appointments WHERE Id = [AppointmentId];

-- Check assessment
SELECT * FROM NCDRiskAssessments WHERE AppointmentId = [AppointmentId];

-- Check vital signs
SELECT * FROM VitalSigns WHERE AppointmentId = [AppointmentId];

-- Check audit log
SELECT * FROM AuditLogs WHERE EntityId = '[AppointmentId]' AND EntityType = 'Appointment';

-- Check notifications
SELECT * FROM Notifications WHERE UserId IN ([PatientId], [DoctorId]);
```

---

## 10. Troubleshooting

### Issue: Slots showing 7:30 AM instead of 8:00 AM
**Solution**: Run the migration to update existing database records

### Issue: No slots available
**Causes**:
- Doctor not available on selected day
- Weekend selected (Saturday/Sunday disabled by default)
- All slots already booked
- Database records not updated with new time configuration

### Issue: Appointment stuck in Draft status
**Causes**:
- Patient hasn't completed required assessment
- Assessment form failed to save
- Check `NCDRiskAssessments` or `HEEADSSSAssessments` table for linked record

### Issue: Nurse can't see appointment
**Causes**:
- Appointment status is Draft (nurses only see Pending, InProgress, Confirmed, Completed)
- Patient needs to complete assessment first
- Appointment date is not today

---

## Summary

The appointment booking system follows a structured workflow:
1. **Patient books** → Creates appointment (Draft or Pending)
2. **Assessment** → If required, changes Draft → InProgress
3. **Nurse records vitals** → Data linked to appointment
4. **Doctor consults** → Views all data, completes consultation
5. **All data stored** → Encrypted in database, audit logged

Time slots are now configured for **8:00 AM - 5:00 PM** with **100 available slots** per day.
