# BMI Calculator Implementation Summary

## Overview
This document describes the implementation of an automatic BMI calculator for the HEEADSSS Health Assessment Form in the CMS dynamic form system.

## Implementation Date
November 3, 2025

## Features Implemented

### 1. **Real-Time BMI Calculation**
- Automatically calculates BMI when Height (cm) and Weight (kg) are entered
- Uses the formula: **BMI = weight (kg) / (height (m)²)**
- Displays BMI with **2 decimal precision** (e.g., 22.49)

### 2. **Automatic BMI Classification**
The system automatically classifies BMI values into four categories:
- **Underweight**: BMI < 18.5 (displayed in yellow/warning color)
- **Normal**: BMI 18.5 - 24.9 (displayed in green/success color)
- **Overweight**: BMI 25 - 29.9 (displayed in orange color)
- **Obese**: BMI ≥ 30 (displayed in red/danger color)

### 3. **Field Protection**
- BMI field is set as **readonly** to prevent manual editing
- BMI Status field is also set as **readonly** when auto-populated
- Fields are visually indicated as readonly with gray background

### 4. **Smart Field Detection**
The calculator automatically detects fields using multiple strategies:
- Field name matching (e.g., "Height", "height", "Height_cm")
- Field placeholder text matching
- Field label text matching
- Supports multilingual field names (English: "Height", Spanish: "altura", Filipino: "timbang")

## Files Modified

### 1. **Pages/Forms/SubmitForm.cshtml**
- Added `initializeBMICalculator()` function
- Added `findFieldByKeywords()` helper function
- Integrated BMI calculator into the main dynamic form page

### 2. **Pages/Shared/_DynamicFormRenderer.cshtml**
- Added BMI calculator functionality to the dynamic form renderer partial view
- Ensures BMI calculation works in all contexts where dynamic forms are used

### 3. **Pages/User/HEEADSSSAssessment.cshtml**
- Updated BMI precision from 1 decimal to **2 decimals** (.toFixed(2))
- Maintains existing BMI classification functionality

### 4. **Pages/Nurse/EditHEEADSSSAssessment.cshtml**
- Updated BMI precision from 1 decimal to **2 decimals** (.toFixed(2))
- Maintains existing BMI classification functionality

### 5. **Pages/Nurse/CreateHEEADSSSAssessment.cshtml**
- Updated BMI precision from 1 decimal to **2 decimals** (.toFixed(2))
- Maintains existing BMI classification functionality

## How It Works

### User Flow Example
1. User enters **Height**: 170 cm
2. User enters **Weight**: 65 kg
3. System automatically calculates: 65 / (1.7)² = **22.49**
4. BMI field displays: **22.49** (readonly)
5. BMI Status field displays: **Normal** (in green color, readonly)

### Technical Implementation

#### Field Detection
```javascript
function findFieldByKeywords(form, keywords) {
    const allInputs = form.querySelectorAll('input, textarea, select');
    
    for (let input of allInputs) {
        const name = (input.name || '').toLowerCase();
        const placeholder = (input.placeholder || '').toLowerCase();
        const label = input.closest('.form-question')?.querySelector('label')?.textContent?.toLowerCase() || '';
        
        for (let keyword of keywords) {
            const lowerKeyword = keyword.toLowerCase();
            if (name.includes(lowerKeyword) || placeholder.includes(lowerKeyword) || label.includes(lowerKeyword)) {
                return input;
            }
        }
    }
    
    return null;
}
```

#### BMI Calculation Logic
```javascript
const heightInMeters = height / 100; // Convert cm to meters
const bmi = weight / (heightInMeters * heightInMeters);
const bmiRounded = bmi.toFixed(2); // 2 decimal precision
```

#### Event Listeners
- Uses JavaScript `input` and `change` event listeners
- Triggers real-time calculation without form submission
- Automatically calculates when either Height or Weight is modified

## Benefits

1. **Reduced Errors**: Eliminates manual calculation errors
2. **Improved UX**: Instant feedback without form submission
3. **Consistency**: Same calculation across all HEEADSSS forms
4. **Flexibility**: Works with various field naming conventions
5. **Visual Feedback**: Color-coded BMI status for quick assessment

## Compatibility

- Works with CMS dynamic forms system
- Compatible with all existing HEEADSSS assessment forms
- Supports both static and dynamic form templates
- No breaking changes to existing functionality

## Testing Recommendations

1. Test with various height and weight combinations
2. Verify BMI calculation accuracy:
   - Height: 170 cm, Weight: 65 kg → BMI: 22.49 (Normal)
   - Height: 150 cm, Weight: 40 kg → BMI: 17.78 (Underweight)
   - Height: 180 cm, Weight: 90 kg → BMI: 27.78 (Overweight)
   - Height: 160 cm, Weight: 85 kg → BMI: 33.20 (Obese)
3. Verify readonly fields cannot be manually edited
4. Test color-coding of BMI status field
5. Test with empty or invalid inputs (should clear BMI fields)

## Browser Console Logging

The implementation includes console logging for debugging:
- "BMI Calculator initialized: {...}" - Shows detected fields
- "BMI Calculated: Height=X, Weight=Y, BMI=Z, Status=..." - Shows calculation results
- "BMI Calculator: Required fields not found" - Warning when fields are missing

## Future Enhancements

Potential improvements for consideration:
1. Add BMI percentile calculation for adolescents
2. Show BMI history/trends for patients
3. Add WHO growth chart references
4. Support for imperial units (feet/inches, pounds)
5. Age-adjusted BMI categories for children/adolescents

## Support

For issues or questions regarding the BMI calculator:
- Check browser console for error messages
- Verify field names match expected patterns
- Ensure Height and Weight fields accept numeric input
- Confirm BMI and BMI Status fields exist in the form

---

**Implementation Status**: ✅ Complete
**Last Updated**: November 3, 2025
**Version**: 1.0

