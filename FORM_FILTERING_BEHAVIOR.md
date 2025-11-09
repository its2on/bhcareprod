# Form Filtering Behavior - How Forms Appear in BookAppointment

## ✅ Correct Implementation

### How Forms Are Shown Based on Service Selection

When a user books an appointment and selects a consultation type, the system shows forms using this logic:

```csharp
// Shows forms that match ANY of these conditions:
1. Forms linked to the selected service (ServiceId = selected service)
2. General forms (ServiceId = null) - shown for ALL services
3. Forms within age range (if Min/Max Age is set)
```

---

## 📋 Examples

### Example 1: User Books **Dental** Appointment

**User Action:**
- Selects Consultation Type: **Dental**
- User age: 30

**Forms Shown:**
1. ✅ **Dental-specific forms** (created with Service Type = Dental)
2. ✅ **General forms** (created with Service Type = None)
3. ✅ **Age-appropriate general forms** (e.g., form with Min Age = 18, Max Age = 60)

**Forms NOT Shown:**
- ❌ Prenatal-specific forms
- ❌ DOTS-specific forms
- ❌ General Consult-specific forms
- ❌ NCD/HEEADSSS (these only appear for General Consult)

---

### Example 2: User Books **General Consult** Appointment

**User Action:**
- Selects Consultation Type: **General Consult**
- User age: 25

**Forms Shown:**
1. ✅ **NCD Risk Assessment** (built-in, age 20+)
2. ✅ **General Consult-specific forms** (created with Service Type = General Consult)
3. ✅ **General forms** (created with Service Type = None)

**Forms NOT Shown:**
- ❌ Dental-specific forms
- ❌ Prenatal-specific forms
- ❌ DOTS-specific forms
- ❌ HEEADSSS (user is 25, this is for age 10-19)

---

### Example 3: User Books **Prenatal** Appointment

**User Action:**
- Selects Consultation Type: **Prenatal & Family Planning**
- User age: 28

**Forms Shown:**
1. ✅ **Prenatal-specific forms** (created with Service Type = Prenatal)
2. ✅ **General forms** (created with Service Type = None)

**Forms NOT Shown:**
- ❌ Dental-specific forms
- ❌ DOTS-specific forms
- ❌ General Consult-specific forms
- ❌ NCD/HEEADSSS (these only trigger for General Consult)

---

## 🎨 Admin: Creating Forms

### Scenario A: Create Dental Assessment Form

**Steps:**
1. Go to `/Admin/FormBuilder`
2. Create form: **"Dental Checkup Form"**
3. Select **Service Type: Dental**
4. Check ✅ "Show in Appointment Workflow"
5. Add questions (e.g., "Do you have tooth pain?")
6. Save

**Result:**
- Form will appear **ONLY** when users book **Dental** appointments
- Will NOT appear for other services

---

### Scenario B: Create Patient Satisfaction Survey (General)

**Steps:**
1. Go to `/Admin/FormBuilder`
2. Create form: **"Patient Satisfaction Survey"**
3. Select **Service Type: None (Standalone Form)**
4. Check ✅ "Show in Appointment Workflow"
5. Add questions (e.g., "How satisfied are you?")
6. Save

**Result:**
- Form will appear for **ALL** consultation types (Dental, Prenatal, DOTS, General Consult)
- Shown alongside service-specific forms

---

### Scenario C: Create Adult Health Survey (Age-Based General)

**Steps:**
1. Go to `/Admin/FormBuilder`
2. Create form: **"Adult Health Survey"**
3. Select **Service Type: None (Standalone Form)**
4. Check ✅ "Show in Appointment Workflow"
5. Set **Min Age: 18**
6. Set **Max Age: 100**
7. Save

**Result:**
- Form will appear for **ALL** consultation types
- But only for users aged 18-100
- Will NOT appear for children/teens

---

## 🔄 Complete Flow Example

### Admin Creates Multiple Forms:

| Form Name | Service Type | Min Age | Max Age | Show in Workflow |
|-----------|--------------|---------|---------|------------------|
| Dental Checkup | Dental | - | - | ✅ Yes |
| Prenatal Assessment | Prenatal & Family Planning | - | - | ✅ Yes |
| Patient Survey | None | - | - | ✅ Yes |
| Adult Health Form | None | 18 | 100 | ✅ Yes |
| Teen Mental Health | None | 13 | 19 | ✅ Yes |

### User Books Dental (Age 25):

**Forms Shown:**
1. ✅ Dental Checkup (service-specific)
2. ✅ Patient Survey (general, no service link)
3. ✅ Adult Health Form (general, age 18-100)

**Forms NOT Shown:**
- ❌ Prenatal Assessment (different service)
- ❌ Teen Mental Health (age 13-19, user is 25)

---

### User Books Prenatal (Age 16):

**Forms Shown:**
1. ✅ Prenatal Assessment (service-specific)
2. ✅ Patient Survey (general, no service link)
3. ✅ Teen Mental Health (general, age 13-19)

**Forms NOT Shown:**
- ❌ Dental Checkup (different service)
- ❌ Adult Health Form (age 18-100, user is 16)

---

## 🔑 Key Points

### ✅ DO:
- Link forms to specific services when they're service-specific (e.g., Dental Assessment → Dental)
- Leave Service Type as "None" for general forms that apply to all services
- Use Min/Max Age for age-appropriate general forms
- Enable "Show in Appointment Workflow" for forms to appear during booking

### ❌ DON'T:
- Create duplicate forms for each service if it's a general form
- Forget to set "Show in Appointment Workflow" (form won't appear)
- Set Service Type for general surveys/questionnaires

---

## 🧪 Testing Checklist

### Test 1: Service-Specific Form
- [x] Create Dental form with Service Type = Dental
- [x] Book Dental appointment
- [x] Verify Dental form appears
- [x] Book Prenatal appointment
- [x] Verify Dental form does NOT appear

### Test 2: General Form
- [x] Create Survey with Service Type = None
- [x] Book Dental appointment
- [x] Verify Survey appears
- [x] Book Prenatal appointment
- [x] Verify Survey appears
- [x] Book General Consult
- [x] Verify Survey appears

### Test 3: Age-Based General Form
- [x] Create Adult form with Min Age = 18, Service Type = None
- [x] Book appointment with age 25
- [x] Verify form appears
- [x] Book appointment with age 15
- [x] Verify form does NOT appear

---

## 💡 Summary

**The system is flexible:**
- **Service-specific forms** = Only show for that service
- **General forms (no service)** = Show for ALL services
- **Age restrictions** = Work for both service-specific and general forms

**This allows:**
- Dental to have unique Dental forms + general surveys
- Prenatal to have unique Prenatal forms + general surveys
- All services share common forms (satisfaction surveys, etc.)
- Age-based filtering works across all scenarios

---

**Last Updated:** November 9, 2024  
**Version:** 1.0
