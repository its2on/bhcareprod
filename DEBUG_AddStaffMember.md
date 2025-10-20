# Debug Guide: Add Staff Member Form Not Submitting

## Common Issues & Solutions

### Issue 1: No Permissions Selected ❌
**Error:** Form won't submit, permissions section highlighted in red
**Solution:** 
1. Select a Role (Doctor, Nurse, or Admin Staff)
2. Click "Grant Essential Permissions" button
3. OR manually check at least one permission checkbox

### Issue 2: Phone Number Format ❌
**Error:** "Contact number must be in the format +639XXXXXXXXX"
**Solution:**
- Format: `+639123456789` (exactly 13 characters)
- Must start with `+639`
- Followed by exactly 9 digits
- Example: `+639171234567`

### Issue 3: Name Validation ❌
**Error:** "Cannot contain 3 or more repeated letters"
**Solution:**
- ❌ Wrong: "Jooohn" (3 o's in a row)
- ✅ Correct: "John"

### Issue 4: Working Days/Hours Not Selected ❌
**Error:** "Please select working days and hours"
**Solution:**
1. Check at least one day (Monday-Sunday)
2. Select Start Time (e.g., 8:00 AM)
3. Select End Time (e.g., 5:00 PM)

### Issue 5: Date of Birth ❌
**Error:** Invalid date
**Solution:**
- Must be at least 18 years old
- Use the date picker to select a valid date

## Step-by-Step Testing

1. **Fill ALL Required Fields:**
   - ✅ First Name: "Juan"
   - ✅ Last Name: "Dela Cruz"
   - ✅ Gender: Select "Male" or "Female"
   - ✅ Civil Status: Select one
   - ✅ Date of Birth: Select a date (18+ years old)
   - ✅ Address: "123 Main St, Manila"
   - ✅ Email: "juan.delacruz@example.com"
   - ✅ Contact Number: "+639171234567"
   - ✅ Role: Select "Doctor", "Nurse", or "Admin Staff"
   - ✅ Working Days: Check at least Monday
   - ✅ Working Hours: 8:00 AM to 5:00 PM
   - ✅ Password: "Test123!"

2. **Grant Permissions:**
   - Click the "Grant Essential Permissions" button
   - OR manually check permission boxes

3. **Submit:**
   - Click "Create Staff Member"
   - Watch for validation errors (red highlights)

## How to Find the Exact Error

### Method 1: Browser Console
1. Press F12 to open Developer Tools
2. Go to "Console" tab
3. Try to submit the form
4. Look for error messages in red

### Method 2: Visual Inspection
1. Try to submit the form
2. Look for RED highlighted fields
3. Scroll through the entire form
4. The first invalid field will be scrolled to automatically

### Method 3: Check Network Tab
1. Press F12 → Network tab
2. Try to submit
3. If you see a POST request to "AddStaffMember", the form submitted
4. If you DON'T see a POST request, there's a validation error

## Most Likely Issue

Based on the console errors you showed, the most likely issue is:

**YOU HAVEN'T SELECTED ANY PERMISSIONS**

### Quick Fix:
1. Select a Role (e.g., "Doctor")
2. Click the orange "Grant Essential Permissions" button
3. You should see checkboxes get checked automatically
4. Try submitting again

## If Still Not Working

Run this in the browser console to see ALL validation errors:

```javascript
const form = document.getElementById('staffForm');
const invalid = form.querySelectorAll(':invalid');
console.log('Invalid fields:', invalid);
invalid.forEach(field => {
    console.log('- ' + (field.name || field.id), field.validationMessage);
});
```

This will show you EXACTLY which fields are invalid and why.
