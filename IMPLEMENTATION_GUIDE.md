# 2-STEP REGISTRATION IMPLEMENTATION GUIDE

## 🎯 Objective
Remove Step 3, consolidate to 2 steps:
- **Step 1:** Personal Info + ID Scanner + Residency Verification
- **Step 2:** Security + Terms + Submit

---

## 📊 Data Flow

```
User uploads ID in Step 1
    ↓
OnPostScanIdAsync() extracts: name, address, barangay
    ↓
JavaScript auto-fills form + stores in hidden fields
    ↓
User completes Step 2 (password + checkboxes)
    ↓
Form submits with Input.ResidencyProof (ID file)
    ↓
ProcessRegistration() checks Input.OcrDetectedBarangay
    ↓
If barangay is 158/159/160/161 → Auto-approve
```

---

## ✏️ Changes Required

### 1. Wizard Progress (lines 14-23)

**Replace 3-step with 2-step:**

```html
<div id="step1Indicator" class="step-indicator active">1. Personal & Verification</div>
<div id="step2Indicator" class="step-indicator">2. Security & Submit</div>
```

---

### 2. Step 1 ID Scanner (lines 46-90)

**Update card to show it does BOTH auto-fill AND residency verification:**

```html
<div class="card mb-4 border-primary">
    <div class="card-header bg-primary text-white">
        Quick Fill ID Scanner + Residency Verification
    </div>
    <div class="card-body">
        <div class="alert alert-info">
            <strong>Upload your ID to:</strong>
            <ul>
                <li>✅ Auto-fill your information</li>
                <li>✅ Verify residency (Barangay 158-161)</li>
                <li>✅ Enable instant approval</li>
            </ul>
        </div>
        
        <input type="file" id="idScannerInput" asp-for="Input.ResidencyProof" 
               accept="image/jpeg,image/jpg,image/png" class="form-control" />
        
        <button type="button" id="scanIdButton" class="btn btn-primary">
            Scan ID & Verify Residency
        </button>
        
        <div id="scannerResult" class="mt-3 d-none"></div>
        <div id="residencyVerificationResult" class="mt-3 d-none"></div>
    </div>
</div>

<!-- Hidden fields (CRITICAL) -->
<input type="hidden" asp-for="Input.OcrDetectedBarangay" id="OcrDetectedBarangay" />
<input type="hidden" asp-for="Input.OcrExtractedAddress" id="OcrExtractedAddress" />
<input type="hidden" asp-for="Input.OcrExtractedText" id="OcrExtractedText" />
```

---

### 3. Step 2 - Add Checkboxes (after password fields)

**Add BEFORE navigation buttons:**

```html
<div class="card mt-4 border-info">
    <div class="card-header bg-info text-white">
        Terms & Confirmation
    </div>
    <div class="card-body">
        <div class="form-check mb-3">
            <input type="checkbox" asp-for="Input.AgreeToTerms" 
                   id="privacyTerms" required />
            <label for="privacyTerms">
                I agree to the <a href="#" data-bs-toggle="modal" 
                   data-bs-target="#termsModal">Data Privacy Terms</a> *
            </label>
        </div>
        
        <div class="form-check mb-3">
            <input type="checkbox" asp-for="Input.ConfirmResidency" 
                   id="residencyConfirm" required />
            <label for="residencyConfirm">
                I confirm residency in Barangay 158, 159, 160, or 161 *
            </label>
        </div>
    </div>
</div>

<div class="d-flex justify-content-between mt-4">
    <button type="button" id="backToPersonal" class="btn btn-outline-secondary">Back</button>
    <button type="submit" class="btn btn-success btn-lg" id="signupButton">
        Register Account
    </button>
</div>
```

---

### 4. Delete Step 3

**DELETE lines 368-446 entirely** (the entire `<div id="section3">...</div>`)

---

### 5. JavaScript - Update scanIdButton Handler

**Key addition: Display residency verification result**

```javascript
scanIdButton.addEventListener('click', async function() {
    const file = idScannerInput.files[0];
    if (!file) return;
    
    // ... existing OCR call ...
    
    const result = await response.json();
    
    if (result.success) {
        // STORE OCR DATA (CRITICAL!)
        document.getElementById('OcrDetectedBarangay').value = result.barangay || '';
        document.getElementById('OcrExtractedAddress').value = result.address || '';
        document.getElementById('OcrExtractedText').value = result.extractedText || '';
        
        // Auto-fill fields
        if (result.firstName) document.querySelector('[name="Input.FirstName"]').value = result.firstName;
        if (result.barangay) document.getElementById('Input_Barangay').value = result.barangay;
        
        // Display residency status
        let residencyHtml = '';
        if (result.isBarangayValid && result.autoApproved) {
            residencyHtml = `<div class="alert alert-success">
                ✅ Residency Verified - Auto-Approval Enabled!
                <p>Barangay ${result.barangay} confirmed. Instant activation upon registration.</p>
            </div>`;
        } else if (result.barangay) {
            residencyHtml = `<div class="alert alert-warning">
                Barangay ${result.barangay} detected but not eligible (must be 158-161)
            </div>`;
        } else {
            residencyHtml = `<div class="alert alert-info">
                No barangay detected. Manual review required.
            </div>`;
        }
        
        document.getElementById('residencyVerificationResult').innerHTML = residencyHtml;
        document.getElementById('residencyVerificationResult').classList.remove('d-none');
    }
});
```

---

### 6. JavaScript - Update Navigation

**Remove all Step 3 references:**

```javascript
// REMOVE these:
// const section3 = document.getElementById('section3');
// const step3Indicator = document.getElementById('step3Indicator');
// const nextToVerification = document.getElementById('nextToVerification');
// const backToSecurity = document.getElementById('backToSecurity');

// Update navigation
nextToSecurity.addEventListener('click', () => {
    section1.classList.add('d-none');
    section2.classList.remove('d-none');
    registrationProgress.style.width = '100%';
});

backToPersonal.addEventListener('click', () => {
    section2.classList.add('d-none');
    section1.classList.remove('d-none');
    registrationProgress.style.width = '0%';
});
```

---

## ✅ Backend Verification

**No backend changes needed!** The existing code already:
- ✅ Reads `Input.ResidencyProof` (the ID file)
- ✅ Reads `Input.OcrDetectedBarangay` from hidden field
- ✅ Auto-approves if barangay is 158-161
- ✅ Saves to UserDocument table
- ✅ Updates ApplicationUser fields

---

## 📋 Testing Checklist

- [ ] Wizard shows 2 steps (not 3)
- [ ] Step 1: ID scanner auto-fills + shows residency status
- [ ] Step 2: Checkboxes present, submit button works
- [ ] Hidden fields populated after ID scan
- [ ] File saved to `/uploads/residency_proofs/`
- [ ] Auto-approval works for barangay 158-161
- [ ] Pending review for other barangays
- [ ] UserDocument record created
- [ ] ApplicationUser fields updated correctly

---

## 🎯 Summary

**Removed:** Step 3 entirely  
**Moved:** Checkboxes from Step 3 → Step 2  
**Enhanced:** Step 1 ID scanner now displays residency verification status  
**Result:** Cleaner 2-step flow, one ID upload does everything!
