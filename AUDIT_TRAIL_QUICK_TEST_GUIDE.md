# 🧪 Audit Trail - Quick Testing Guide

**Test Date:** October 23, 2025  
**Estimated Time:** 15 minutes  
**Target:** Verify all enhancements are working correctly

---

## 🚀 **PRE-TEST SETUP**

```powershell
# 1. Navigate to project
cd "c:\Users\WIN 10\Desktop\BHCARE-main"

# 2. Run application
dotnet run

# 3. Open browser
https://localhost:5001
```

---

## ✅ **TEST 1: Vital Signs Logging (3 min)**

### **Objective:** Verify vital signs are being logged

### **Steps:**
1. Log in as **Nurse**
2. Navigate to `/Nurse/VitalSigns`
3. Select a patient appointment
4. Record vital signs:
   - Temperature: `37.5`
   - Blood Pressure: `120/80`
   - Heart Rate: `72`
   - Respiratory Rate: `16`
   - SpO2: `98`
   - Weight: `65`
   - Height: `165`
5. Click **Save**
6. Log out
7. Log in as **Admin**
8. Navigate to `/Admin/AuditTrail`
9. Search for "Vital signs"

### **Expected Result:**
```
✅ Should see entry:
   - Action: "Vital signs recorded"
   - Entity: "VitalSign"
   - Role: "Nurse"
   - Outcome: [✓ Success] (green badge)
   - IP Address: (your IP)
   - Device: Chrome (or your browser)
```

---

## ✅ **TEST 2: IP Address Capture (2 min)**

### **Objective:** Verify IP addresses are captured correctly

### **Steps:**
1. Still logged in as Admin
2. Look at any audit log entry
3. Check the **Location** column
4. Click **View** button on any entry
5. Look at **IP Address** field in modal

### **Expected Result:**
```
✅ Location column should show:
   - Your actual IP (e.g., "192.168.1.100")
   - OR "::1" if testing locally
   - NOT empty or "Not available"

✅ Modal should show:
   - IP Address: (same IP)
   - Location: (IP or "Not available")
```

---

## ✅ **TEST 3: Color Visibility (2 min)**

### **Objective:** Verify new colors are applied correctly

### **Steps:**
1. Still on `/Admin/AuditTrail`
2. Observe the interface colors

### **Expected Result:**
```
✅ Success badges: Bright green (#22c55e)
✅ Failed badges: Bright red (#ef4444)
✅ Role badges: Neutral orange (#fb923c) - all same color
✅ Search button: Orange (#f97316)
✅ Reset button: Gray (#9ca3af)
✅ Export PDF button: Deep red (#dc2626)
✅ Table header: Light gray (#f9fafb) - not purple
```

**Visual Check:**
- [ ] All role badges use **same orange color**
- [ ] Success badges are **easy to read** (bright green)
- [ ] Failed badges are **easy to read** (bright red)
- [ ] Search button is **orange**, not blue

---

## ✅ **TEST 4: Button Hover Effects (1 min)**

### **Objective:** Verify hover animations work

### **Steps:**
1. Hover mouse over **Search** button
2. Hover over **Reset** button
3. Hover over **Export PDF** button
4. Hover over **View** button on any row

### **Expected Result:**
```
✅ All buttons should:
   - Scale slightly (grow 2%)
   - Reduce opacity to 90%
   - Show smooth animation (0.2s)
   - Look professional
```

---

## ✅ **TEST 5: PDF Export (4 min)**

### **Objective:** Verify enhanced PDF export

### **Steps:**
1. Add some filters:
   - Role: **Nurse**
   - Outcome: **Success**
   - Date From: **today**
2. Click **Search**
3. Click **Export PDF** button
4. Wait for download
5. Open the PDF file

### **Expected Result:**
```
✅ PDF should have:

Header:
   - "Barangay Health Monitoring System (BHCare)" in orange
   - "AUDIT TRAIL REPORT"
   - Generated date and user

Filters:
   - "Role Filter: Nurse"
   - "Outcome Filter: Success"
   - "Date Range: [date] to [date]"

Detailed Entries (for each log):
   - Timestamp: Oct 23, 2025 HH:MM:SS UTC
   - User: nurse@example.com
   - Role: Nurse (in orange)
   - Action: [action description]
   - Outcome: Success (in green)
   - Resource: [entity name]
   - IP Address: [IP]
   - Device: Chrome
   - Location: [IP or location]
   - Session ID: [truncated ID]
   - Description: (if available)

Footer:
   - Page numbers (Page X of Y)
   - "BHCare © 2025 - Confidential Document"
   - Generation timestamp
```

**File Check:**
- [ ] Filename: `BHCare_AuditTrail_YYYYMMDD.pdf`
- [ ] Header is orange and centered
- [ ] Filters are displayed correctly
- [ ] All log entries are detailed
- [ ] Colors work (green/red for outcomes)
- [ ] Footer is present on all pages

---

## ✅ **TEST 6: Search and Reset Alignment (1 min)**

### **Objective:** Verify buttons are aligned side-by-side

### **Steps:**
1. Look at the filter section
2. Find Search and Reset buttons

### **Expected Result:**
```
✅ Should see:
   [Search] [Reset]  ← side-by-side, same height
   
✅ NOT:
   [   Search   ]
   [   Reset    ]  ← vertical stack (OLD)
```

---

## ✅ **TEST 7: Modal Details (2 min)**

### **Objective:** Verify detailed modal works

### **Steps:**
1. Click **View** on any audit log row
2. Modal should open
3. Review all fields

### **Expected Result:**
```
✅ Modal should show:
   - Audit ID: [number]
   - Session ID: [full session ID]
   - Request Method: GET/POST
   - Response Code: Success/Failed
   - Request URL: [full URL]
   - User Role: [role]
   - Performed By: [email]
   - Full Device Information: [complete user agent]
   - Location: [IP or location]
   - IP Address: [IP]
   - Additional Context: (JSON if available)
```

**Visual Check:**
- [ ] Modal has purple gradient header
- [ ] All fields populated (no "undefined")
- [ ] IP address is showing
- [ ] Session ID is present
- [ ] Close button works

---

## 📊 **SUMMARY CHECKLIST**

After completing all tests, verify:

### **Functionality:**
- [ ] Vital signs are being logged
- [ ] All audit entries have timestamps
- [ ] User information is captured
- [ ] Entity types are correct

### **IP/Location:**
- [ ] IP addresses are showing (not empty)
- [ ] Location column displays IP if no GeoIP
- [ ] Modal shows IP address
- [ ] No "::1" issues (unless local testing)

### **UI Colors:**
- [ ] Success badges: Bright green
- [ ] Failed badges: Bright red
- [ ] Role badges: All same orange
- [ ] Search button: Orange
- [ ] Reset button: Gray
- [ ] Export PDF: Deep red
- [ ] Table header: Light gray (not purple)

### **Buttons:**
- [ ] Search and Reset are side-by-side
- [ ] Hover effects work on all buttons
- [ ] Buttons are same height
- [ ] Rounded corners (8px)

### **PDF Export:**
- [ ] PDF downloads successfully
- [ ] Header is formatted correctly
- [ ] Filters are shown
- [ ] Detailed entries present
- [ ] Footer on all pages
- [ ] Filename format correct

### **Modal:**
- [ ] Opens smoothly
- [ ] All fields populated
- [ ] IP address visible
- [ ] Session ID present
- [ ] Close button works

---

## ❌ **TROUBLESHOOTING**

### **Issue: No IP Address Showing**

**Solution:**
- Check if you're testing locally (::1 is normal)
- For production: Ensure proxy headers configured
- Verify `X-Forwarded-For` header in production

### **Issue: Colors Not Showing**

**Solution:**
- Hard refresh browser (Ctrl+F5)
- Clear browser cache
- Check CSS variables in DevTools

### **Issue: PDF Not Downloading**

**Solution:**
- Check browser console for errors
- Verify jsPDF library loaded
- Ensure no pop-up blocker

### **Issue: Vital Signs Not Logged**

**Solution:**
- Check if `IAuditTrailService` is registered
- Verify `_auditTrail.LogAsync` is called
- Check Application Insights for errors

---

## 🎉 **TEST COMPLETION**

If all tests pass:

```
✅ Functionality: ALL WORKING
✅ IP/Location: ALL WORKING
✅ UI Colors: ALL UPDATED
✅ PDF Export: ALL ENHANCED
✅ Buttons: ALL ALIGNED
✅ Modal: ALL DISPLAYING

OVERALL: READY FOR PRODUCTION ✅
```

---

## 📞 **NEXT STEPS**

After successful testing:

1. ✅ Document any issues found
2. ✅ Train admin staff on new features
3. ✅ Deploy to production
4. ✅ Monitor audit logs for 24 hours
5. ✅ Collect user feedback

---

**Testing Guide Created:** October 23, 2025, 2:55 AM  
**Estimated Test Time:** 15 minutes  
**Success Rate Target:** 100%  
**Status:** ✅ **READY FOR TESTING**
