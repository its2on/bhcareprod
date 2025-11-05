# Philippine National ID (PhilID) - Scanner Reference Guide

## Real ID Example Analysis

Based on the provided Philippine National ID sample, this document serves as a reference for the ID scanner implementation.

---

## 📋 **ID Card Structure**

### **Header Section**
```
REPUBLIKA NG PILIPINAS
Republic of the Philippines
PAMBANSANG PAGKAKAKILANLAN
Philippine Identification Card
```

### **ID Number**
```
2715-4704-3603-2051
Format: XXXX-XXXX-XXXX-XXXX (16 digits with hyphens)
```

---

## 👤 **Personal Information Fields**

### **1. Last Name (Apelyido)**
```
Label: "Apelyido/Last Name"
Example Value: REBOREDO
Format: ALL CAPS, alphabetic characters
```

**Extraction Pattern:**
- Label variations: `APELYIDO`, `APELLIDO`, `LAST NAME`, `SURNAME`
- Position: Usually before "Mga Pangalan" section
- OCR considerations: May appear as `APELIYDO`, `APEL1YDO` (1 instead of I)

---

### **2. Given Names (Mga Pangalan)**
```
Label: "Mga Pangalan/Given Names"
Example Value: RHYLLE LANDER
Format: ALL CAPS, can contain multiple names
```

**Extraction Pattern:**
- Label variations: `MGA PANGALAN`, `MGAPANGALAN`, `GIVEN NAMES`, `GIVEN NAME`
- Position: After last name, before middle name
- OCR considerations: May appear as `MGA PANGALAR` (R instead of N)
- **Important:** This field contains ONLY the given/first names, NOT the middle name

**Mapping to Form:**
- In this example: `RHYLLE LANDER` should ALL go to **First Name** field ✓
- **Implementation (FIXED):** Keeps ALL words from "Mga Pangalan" as First Name
- Middle name comes from separate "Gitnang Apelyido" field

---

### **3. Middle Name (Gitnang Apelyido)**
```
Label: "Gitnang Apelyido/Middle Name"
Example Value: MONTERO
Format: ALL CAPS, usually single name
```

**Extraction Pattern:**
- Label variations: `GITNANG APELYIDO`, `MIDDLE NAME`, `GITNANG APELIYDO`
- Position: After given names section
- OCR considerations: May appear as `G1TNANG` (1 instead of I)

---

### **4. Date of Birth (Petsa ng Kapanganakan)**
```
Label: "Petsa ng Kapanganakan/Date of Birth"
Example Value: JUNE 12, 2003
Format: MONTH DD, YYYY (month name format)
Alternative Formats: 
  - 12/06/2003 (DD/MM/YYYY)
  - 06/12/2003 (MM/DD/YYYY)
  - 12 JUNE 2003
```

**Extraction Pattern:**
- Label variations: `PETSA NG KAPANGANAKAN`, `DATE OF BIRTH`, `BIRTH DATE`, `KAPANGANAKAN`
- OCR considerations: May appear as `PETSA NG KAPANGANAKA1N` (1 instead of N)
- **Parser must handle:** Month names (JUNE, JANUARY, etc.)
- **Output format:** YYYY-MM-DD (e.g., 2003-06-12)

---

### **5. Address (Tirahan)**
```
Label: "Tirahan/Address"
Example Value: 391 ALPHA HOMES RUBYVILLE SUBD. BARANGAY 160, CITY OF CALOOCAN, NCR, THIRD DISTRICT
Format: Multi-line, contains street, barangay, city, region, district
```

**Extraction Pattern:**
- Label variations: `TIRAHAN`, `ADDRESS`, `RESIDENCE`, `PERMANENT ADDRESS`
- Position: Usually at bottom of card
- Format: House number, street, subdivision, barangay, city, region, district
- OCR considerations: May span 2-3 lines

**Address Components:**
- **Street Address:** `391 ALPHA HOMES RUBYVILLE SUBD.`
- **Barangay:** `BARANGAY 160` ← **This must be extracted separately**
- **City:** `CITY OF CALOOCAN`
- **Region:** `NCR`
- **District:** `THIRD DISTRICT`

---

## 🏘️ **Barangay Extraction (Critical)**

### **From the Example:**
```
Full Address: 391 ALPHA HOMES RUBYVILLE SUBD. BARANGAY 160, CITY OF CALOOCAN, NCR, THIRD DISTRICT
Extracted Barangay: 160
```

### **Extraction Patterns:**
```regex
Pattern 1: (?:BARANGAY|BRGY\.?|BRG\.?)\s*(158|159|160|161)
Pattern 2: \b(158|159|160|161)\b
```

### **Supported Barangays:**
- 158
- 159
- 160 ← **This example**
- 161

### **Real-World Variations:**
```
✓ "BARANGAY 160"
✓ "BRGY 160"
✓ "BRGY. 160"
✓ "BRG. 160"
✓ "160" (standalone)
✓ "BARANGAY160" (no space)
```

---

## 🎯 **Expected Extraction Results for This Example**

Based on the sample ID provided:

```json
{
  "FirstName": "RHYLLE LANDER",
  "MiddleName": "MONTERO",
  "LastName": "REBOREDO",
  "BirthDate": "2003-06-12",
  "Address": "391 ALPHA HOMES RUBYVILLE SUBD. BARANGAY 160, CITY OF CALOOCAN, NCR, THIRD DISTRICT",
  "Barangay": "160",
  "Gender": null,
  "ContactNumber": null,
  "IdNumber": "2715-4704-3603-2051"
}
```

✅ **CORRECT EXTRACTION** (after fix applied on Nov 2, 2025)

**Note:** 
- Gender and ContactNumber are not visible on the front of this ID (may be on back or not present)
- **Important:** "RHYLLE LANDER" stays together as First Name (from "Mga Pangalan")
- "MONTERO" comes from separate "Gitnang Apelyido" field

---

## 🔍 **Common OCR Challenges with This ID**

### **1. Character Confusion**
- **I vs 1 vs l:** `PILIPINAS` may become `P1L1P1NAS`
- **O vs 0:** `MONTERO` may become `M0NTER0`
- **S vs 5:** `HOMES` may become `HOME5`

### **2. Label Spacing**
- `MGA PANGALAN` may become `MGAPANGALAN` (no space)
- `GITNANG APELYIDO` may become `GITNANGAPELYIDO`

### **3. Multi-line Address**
The address often spans multiple lines:
```
Line 1: 391 ALPHA HOMES RUBYVILLE SUBD.
Line 2: BARANGAY 160, CITY OF CALOOCAN
Line 3: NCR, THIRD DISTRICT
```

**Solution:** Collect all lines after "Tirahan/Address" label until next field

### **4. Date Format Variations**
OCR may produce:
- `JUNE 12, 2003` ✓ (correct)
- `JUNE12,2003` (missing space)
- `JUNE 1Z, 2003` (2 misread as Z)
- `JUN 12, 2003` (abbreviated)

---

## 📊 **Field Priority and Weights**

Based on this reference, field importance for the signup form:

| Field | Priority | Weight | Required |
|-------|----------|--------|----------|
| **Last Name** | Critical | 30% | ✅ Yes |
| **First Name** | Critical | 30% | ✅ Yes |
| **Birth Date** | High | 20% | ✅ Yes |
| **Address** | High | 15% | ✅ Yes |
| **Middle Name** | Medium | 10% | ❌ No |
| **Barangay** | High | 15% | ✅ Yes |
| **Gender** | Low | 5% | ✅ Yes |
| **Contact** | Low | 5% | ✅ Yes |

---

## 🧪 **Test Cases Based on This ID**

### **Test Case 1: Perfect Scan**
```
Input: Clear, well-lit image of this ID
Expected Output:
  - FirstName: "RHYLLE LANDER"
  - MiddleName: "MONTERO"
  - LastName: "REBOREDO"
  - BirthDate: "2003-06-12"
  - Address: "391 ALPHA HOMES RUBYVILLE SUBD. BARANGAY 160, CITY OF CALOOCAN, NCR, THIRD DISTRICT"
  - Barangay: "160"
  - Confidence: >90%
```

### **Test Case 2: OCR Errors**
```
Input: Slightly blurry image with OCR errors
OCR Output: "REBER0D0" (O instead of O), "MGA PANGALAR" (R instead of N)
Expected: Fuzzy matching corrects to "REBOREDO" and finds "MGA PANGALAN"
```

### **Test Case 3: Address Extraction**
```
Input: Multi-line address
Expected: Collects all address lines and extracts "160" as barangay
```

### **Test Case 4: Date Parsing**
```
Input: "JUNE 12, 2003"
Expected: Converts to "2003-06-12"
```

---

## 🎨 **Visual Layout Reference**

```
┌─────────────────────────────────────────────────────┐
│ [Coat of Arms]    REPUBLIKA NG PILIPINAS   [Fingerprint] │
│              Republic of the Philippines            │
│         PAMBANSANG PAGKAKAKILANLAN                  │
│         Philippine Identification Card              │
│                                                     │
│ 2715-4704-3603-2051                                │
│                                                     │
│ [Photo]           Apelyido/Last Name               │
│                   REBOREDO                         │
│                                                     │
│                   Mga Pangalan/Given Names         │
│                   RHYLLE LANDER                    │
│                                                     │
│                   Gitnang Apelyido/Middle Name     │
│                   MONTERO                          │
│                                                     │
│                   Petsa ng Kapanganakan/Date of Birth │
│                   JUNE 12, 2003                    │
│                                                     │
│ Tirahan/Address                                    │
│ 391 ALPHA HOMES RUBYVILLE SUBD. BARANGAY 160,    │
│ CITY OF CALOOCAN, NCR, THIRD DISTRICT             │
│                                                     │
│                                          [PHL Logo] │
└─────────────────────────────────────────────────────┘
```

---

## 💡 **Recommendations Based on This Reference**

### **1. Label Detection Improvements**
✅ **Already Implemented:**
- Fuzzy matching with Levenshtein distance
- Multiple label variations
- OCR error correction (0→O, 1→I, 5→S, 8→B)

### **2. Given Names Handling**
✅ **FIXED (November 2, 2025):**

**Previous Issue:**
Implementation was splitting "RHYLLE LANDER" as:
- First Name: "RHYLLE"
- Middle Name: "LANDER" ❌ WRONG

**Current Implementation (FIXED):**
- **First Name:** "RHYLLE LANDER" (ALL words from "Mga Pangalan")
- **Middle Name:** "MONTERO" (from separate "Gitnang Apelyido" field)

**How It Works:**
1. Extracts "Mga Pangalan" → keeps ALL words as First Name
2. Separately looks for "Gitnang Apelyido" label → extracts as Middle Name
3. This correctly reflects Philippine ID structure where:
   - "Mga Pangalan" = Given/First names (can be multiple)
   - "Gitnang Apelyido" = Middle name (separate field)

### **3. Address Field Enhancement**
✅ **Already Implemented:**
- Multi-line collection
- Barangay extraction (158, 159, 160, 161)
- Fuzzy label matching

### **4. Date Format Support**
✅ **Already Implemented:**
- Month name parsing (JUNE → 06)
- Multiple date formats
- Converts to YYYY-MM-DD

---

## 📚 **Filipino/Tagalog Terms Reference**

For better fuzzy matching of labels:

| English | Filipino/Tagalog | Common OCR Errors |
|---------|------------------|-------------------|
| Last Name | Apelyido | APELIYDO, APELLIDO, APEL1YDO |
| Given Names | Mga Pangalan | MGAPANGALAN, MGA PANGALAR, MGA PANGALA1N |
| Middle Name | Gitnang Apelyido | G1TNANG APELYIDO, GITNANG APELIYDO |
| Date of Birth | Petsa ng Kapanganakan | PETSA NG KAPANGANAKA1N |
| Address | Tirahan | T1RAHAN, TIRARAN |
| Barangay | Barangay | BARANGAY, BRGY, BRG |
| Sex/Gender | Kasarian | KASAR1AN, KASARIAN |

---

## 🔧 **Implementation Checklist**

Based on this reference ID:

- [x] Extract Last Name (Apelyido)
- [x] Extract Given Names (Mga Pangalan)
- [x] Extract Middle Name (Gitnang Apelyido)
- [x] Extract Birth Date (Petsa ng Kapanganakan)
- [x] Extract Address (Tirahan)
- [x] Extract Barangay from Address
- [x] Parse date to YYYY-MM-DD format
- [x] Handle multi-line address
- [x] Fuzzy matching for labels
- [x] OCR error correction
- [x] Confidence scoring
- [ ] Extract ID Number (optional)
- [ ] Extract Gender if visible (back of card?)
- [ ] Extract Contact Number if available

---

## 📞 **Support for Other Document Types**

While this reference shows a **PhilID (National ID)**, the system should also support:

1. **Driver's License**
2. **Postal ID**
3. **PhilHealth ID**
4. **SSS ID**
5. **UMID**
6. **Passport**

Each has different layouts but similar field types. The PhilID format is the primary reference.

---

## ✅ **Validation Rules Based on This Example**

### **Name Fields**
- ✓ Must be alphabetic (with spaces, hyphens, apostrophes)
- ✓ Minimum 2 characters
- ✓ No numbers (OCR errors should be corrected)
- ✓ ALL CAPS format (can be converted to proper case)

### **Date of Birth**
- ✓ Must be valid date
- ✓ Cannot be future date
- ✓ Reasonable age range (not >120 years)
- ✓ For this example: June 12, 2003 = Age 21 (as of 2024)

### **Address**
- ✓ Must contain barangay number (158, 159, 160, or 161)
- ✓ Minimum length: 20 characters
- ✓ Should contain city/municipality
- ✓ For this example: Valid - contains "BARANGAY 160" and "CITY OF CALOOCAN"

### **Barangay**
- ✓ Must be one of: 158, 159, 160, 161
- ✓ For this example: 160 ✓

---

## 📸 **Scanning Tips for Users**

Based on this ID sample:

1. ✅ **Good lighting** - Avoid shadows on text areas
2. ✅ **Flat surface** - Card should be flat, not bent
3. ✅ **Focus on text** - Ensure all text is sharp and clear
4. ✅ **No glare** - Avoid reflective light on card surface
5. ✅ **Full card visible** - Capture entire card including borders
6. ✅ **High resolution** - Minimum 1200x800 pixels recommended

---

## 🎯 **Summary**

This Philippine National ID example demonstrates:

✅ **Perfect Reference ID** with:
- Clear text labels in both English and Filipino
- All required fields present
- Barangay number clearly visible (160)
- Standard PhilID format
- Realistic address structure

✅ **Current Implementation Status:**
- Extraction logic: **Fully Implemented** ✓
- Fuzzy matching: **Fully Implemented** ✓
- OCR correction: **Fully Implemented** ✓
- Barangay extraction: **Fully Implemented** ✓
- Date parsing: **Fully Implemented** ✓

✅ **Ready for Testing:**
- Use this ID as test case
- Expected confidence: >90%
- All fields should extract correctly

---

**Reference ID Details:**
- **Owner:** REBOREDO, RHYLLE LANDER MONTERO
- **DOB:** June 12, 2003
- **Location:** Barangay 160, Caloocan City
- **ID Type:** Philippine National ID (PhilID)
- **ID Number:** 2715-4704-3603-2051

This ID serves as the **primary reference** for testing and validating the ID scanner implementation.

