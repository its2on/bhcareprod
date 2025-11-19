
# ID Verification Implementation Guide  
### Update it With Government ID Dropdown, Format Validation, and Improved OCR Autofill

## 1. Add Government ID Type Dropdown
Create a dropdown labeled **“Select Government ID Type”**

**Supported IDs:**
1. Philippine National ID (PhilSys ID)
2. Driver’s License (LTO)
3. UMID
4. TIN ID
5. Postal ID
6. PhilHealth ID
6. SSS ID
8. Voter’s / COMELEC ID
9. Passport

User must select one before uploading an ID image.

## 2. Validate ID Number Format Based on Selected ID

### Philippine National ID (PhilSys / ePhilID)
Format: `####-####-####-####`  
- 16 digits total

### Driver’s License (LTO)
Supported patterns:  
- `L##-##-######`  
- `##-##-######`  
- `A## #######` (old format)

### UMID  
Format: `############`  
- 12–16 digits

### TIN ID  
Format: `###-###-###`  
- 9 digits

### Postal ID  
Format: `#### #### #### ####`  
- 16 digits

### PhilHealth ID  
Format: `##-#########-#`  
- 12 digits

### SSS ID  
Format: `##-#######-#`  
- 10 digits

### Voter’s / COMELEC ID  
Format: `A##-####-#######-#`  
- Alphanumeric

### Passport  
Format: `P########`  
- 1 letter + 7 digits

## 3. OCR Processing Instructions

Use OCR tools (Computer Vision, Tesseract, Gemini Vision OCR) to extract:

- First Name  
- Middle Name  
- Last Name  
- Suffix  
- Date of Birth  
- Gender  
- ID Number  
- Address  
- Raw Text

### OCR cleanup rules
- Convert “O” → “0”, “I” → “1” when numeric  
- Strip line breaks & extra spaces  
- Support MRZ detection (passports)

## 4. Improve Autofill Accuracy

### Name Parsing
If labels exist:
```
Surname:
Given Name(s):
Middle Name:
```
→ Map directly.

If full name detected in one line:  
`LASTNAME, FIRSTNAME MIDDLENAME`  
→ Split accordingly.

### Date of Birth Detection
Accept:
- `MM/DD/YYYY`
- `DD/MM/YYYY`
- `YYYY-MM-DD`

Automatically correct swapped month/day.

### Gender Detection
Accept:
- MALE / FEMALE  
- M / F  
- “SEX: M/F”

Normalize to:
- Male  
- Female  
- Other

## 5. OCR Prompt (Copy & Paste for Your OCR API)

```
Analyze the uploaded ID image based on the selected ID type.
Return the extracted information in this exact JSON format:

{
  "id_type": "<selected_id>",
  "id_number": "",
  "first_name": "",
  "middle_name": "",
  "last_name": "",
  "suffix": "",
  "date_of_birth": "",
  "gender": "",
  "address": "",
  "raw_text": ""
}

Validate the ID number format according to the chosen ID type.
Fix common OCR errors such as O→0, I→1, and remove whitespace.
Ensure name fields, birth date, and gender are accurately extracted.
If unsure, infer based on common Philippine ID layouts.
If a field cannot be extracted, return an empty string.
```

## 6. Expected System Behavior

1. User selects ID type  
2. User uploads image  
3. OCR extracts fields  
4. Form auto-fills  
5. ID number format is validated  
6. User receives error message if invalid
