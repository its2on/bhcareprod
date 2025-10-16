# Debug Steps for Appointment Status Issue

## 🔍 **Immediate Testing Steps**

### Step 1: Check Browser Console
1. Open the NCD Risk Assessment form
2. Open browser Developer Tools (F12)
3. Go to Console tab
4. Complete and submit the form
5. Look for these log messages:
   - `"=== APPOINTMENT ID DEBUG ==="`
   - `"AppointmentId from URL: [number]"`
   - `"AppointmentId being sent: [number]"`

### Step 2: Check Application Logs
Look for these specific log entries after form submission:
```
[Timestamp] Attempting to update appointment status. AppointmentId: [ID]
[Timestamp] Found appointment. Current status: [Status], PatientName: [Name]
[Timestamp] Appointment status updated from [OldStatus] to [NewStatus]. Rows affected: [Number]
[Timestamp] SUCCESS: Appointment [ID] is now visible to nurses and doctors
```

### Step 3: Check Database Directly
Run this SQL query to verify the appointment status:
```sql
SELECT Id, PatientName, Status, AppointmentDate, UpdatedAt, CreatedAt
FROM Appointments 
WHERE PatientId = '[YOUR_USER_ID]'
ORDER BY UpdatedAt DESC;
```

Status values:
- 0 = Pending
- 1 = Confirmed  
- 2 = InProgress (should be this after assessment)
- 7 = Draft

## 🚨 **Possible Issues and Solutions**

### Issue 1: AppointmentId Not Found
**Symptoms**: Log shows "CRITICAL: Appointment not found for ID: [ID]"
**Solution**: Check if the appointment exists in database
```sql
SELECT * FROM Appointments WHERE Id = [APPOINTMENT_ID];
```

### Issue 2: No AppointmentId in URL
**Symptoms**: Log shows "CRITICAL: No AppointmentId provided in assessment"
**Solution**: Ensure you access the form with `?appointmentId=[ID]` parameter

### Issue 3: Database Update Fails
**Symptoms**: Log shows "FAILED: No rows updated when changing appointment status"
**Solution**: Check database permissions and constraints

### Issue 4: Caching Issue
**Symptoms**: Status appears updated in logs but not in UI
**Solution**: 
1. Clear browser cache
2. Hard refresh (Ctrl+F5)
3. Check if using different browser sessions

## 🔧 **Quick Fixes to Try**

### Fix 1: Manual Status Update (SQL)
```sql
UPDATE Appointments 
SET Status = 2, UpdatedAt = GETDATE() 
WHERE Id = [YOUR_APPOINTMENT_ID];
```

### Fix 2: Check URL Parameter
Ensure the assessment URL includes the appointment ID:
```
/User/NCDRiskAssessment?appointmentId=123
```

### Fix 3: Verify User ID Match
```sql
SELECT a.Id, a.PatientId, a.Status, u.Email
FROM Appointments a
JOIN AspNetUsers u ON a.PatientId = u.Id
WHERE u.Email = '[YOUR_EMAIL]';
```

## 📋 **Test Checklist**

- [ ] Appointment ID appears in browser console logs
- [ ] Server logs show appointment found and updated
- [ ] Database shows Status = 2 after submission
- [ ] User dashboard refreshes after 1-second delay
- [ ] Nurse dashboard shows the appointment
- [ ] Doctor dashboard shows the appointment

## 🆘 **If Still Not Working**

1. **Check the exact error messages** in browser console and server logs
2. **Verify the appointment ID** is correct and exists in database
3. **Test with a fresh appointment** - create new appointment and try again
4. **Check user permissions** - ensure user owns the appointment
5. **Verify database connection** - ensure no connection issues during update

## 📞 **Next Steps**

After completing an assessment:
1. Note the exact appointment ID from the success message
2. Check that ID in the database
3. Refresh the appointments page
4. If still showing as Draft, check the server logs for error messages

The enhanced logging should now show exactly what's happening during the status update process.
