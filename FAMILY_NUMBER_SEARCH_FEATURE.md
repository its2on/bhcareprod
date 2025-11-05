# Family Number Search and Generation Feature

## 📋 Overview

This feature allows users to easily search for existing family numbers or generate new ones during appointment booking. It replaces the previous simple input field with an intelligent search interface that helps users find their family members and reuse family numbers.

---

## ✨ Features Implemented

### 1. **Smart Search Interface**
- Searchable input with auto-suggest functionality
- Debounced search (300ms delay) to prevent excessive API calls
- Search works across both ApplicationUsers and Patients tables
- Minimum 2 characters required to trigger search

### 2. **Search Results Dropdown**
- Displays matching family members with:
  - Full Name
  - Family Number
  - Relationship indicator ("You" for current user, "Family Member" for others)
- Click to select and auto-fill
- Dropdown closes when clicking outside

### 3. **Family Number Display**
- Shows selected family number with a badge design
- Includes "Change" button to reset and search again
- Visual feedback with icons

### 4. **Generate New Family Number**
- "No family record found" message when search has no results
- "Generate New Family Number" button creates a unique family number
- Format: `{FirstLetterOfLastName}-{SequentialNumber}` (e.g., `S-001`, `S-002`)
- Automatically assigns to user's profile
- Updates both ApplicationUser and Patient records

### 5. **Auto-Initialization**
- Automatically checks if logged-in user has a family number
- Pre-fills the field if family number exists
- Shows search interface if no family number found

---

## 🔧 Technical Implementation

### Files Modified

#### **1. Pages/BookAppointment.cshtml**
**UI Changes:**
```html
<!-- Search Input -->
<input type="text" id="familyNumberSearch" placeholder="Search by name..." autocomplete="off">

<!-- Hidden field to store selected family number -->
<input type="hidden" id="familyNumber" name="familyNumber">

<!-- Search Results Dropdown -->
<div id="familySearchResults" class="dropdown-menu"></div>

<!-- Display Selected Family Number -->
<div id="familyNumberDisplay" style="display: none;">
    <span class="badge bg-primary" id="selectedFamilyNumber"></span>
    <button type="button" class="btn btn-sm btn-outline-secondary" id="clearFamilyNumber">
        <i class="fa-solid fa-times"></i> Change
    </button>
</div>

<!-- No Results Found -->
<div id="noFamilyFound" style="display: none;">
    <p class="text-muted">No family record found.</p>
    <button type="button" class="btn btn-warning" id="generateNewFamilyNumberBtn">
        Generate New Family Number
    </button>
</div>
```

**JavaScript Functions:**
- `initializeFamilyNumber()` - Auto-fills user's existing family number on page load
- `familyNumberSearch.on('input')` - Debounced AJAX search handler
- `displaySearchResults(members)` - Renders search results in dropdown
- `selectFamilyNumber(familyNumber, memberName)` - Handles selection and updates hidden field
- `clearFamilyNumber.on('click')` - Resets the family number section
- `generateNewFamilyNumberBtn.on('click')` - Calls API to generate new family number

#### **2. Pages/BookAppointment.cshtml.cs**

**New API Endpoints:**

```csharp
// GET: Search for family members by name
public async Task<JsonResult> OnGetSearchFamilyMembersAsync(string searchTerm)
{
    // Searches in:
    // - ApplicationUsers (AspNetUsers table) - registered users
    // - Patients table - dependents and family members
    
    // Returns: { success: true, members: [{ fullName, familyNumber, relationship }] }
}

// POST: Generate a brand new family number
public async Task<JsonResult> OnPostGenerateNewFamilyNumberAsync([FromBody] GenerateNewFamilyNumberRequest request)
{
    // Generates new family number: {FirstLetterOfLastName}-{SequentialNumber}
    // Saves to user profile and patient record
    
    // Returns: { success: true, familyNumber: "S-001", message: "..." }
}
```

**New DTO:**
```csharp
public class GenerateNewFamilyNumberRequest
{
    public string LastName { get; set; }
    public string FirstName { get; set; }
}
```

#### **3. Services/FamilyNumberService.cs**

**New Method:**
```csharp
public async Task<string> GenerateNewFamilyNumberAsync(string lastName)
{
    // Generates a completely new family number without checking for existing ones
    // Thread-safe implementation using SemaphoreSlim
    // Format: {Prefix}-{3-digit-number}
}
```

---

## 🔄 User Flow

### Scenario 1: User with Existing Family Number
1. User loads BookAppointment page
2. System automatically detects user's family number
3. Family number is pre-filled and displayed
4. User can click "Change" to search for a different family number

### Scenario 2: User Without Family Number (Searching)
1. User types a name in the search field (e.g., "Juan Dela Cruz")
2. After 300ms, system searches for matching family members
3. Dropdown shows results with names and family numbers
4. User clicks a result to select that family number
5. Selected family number is displayed and stored in hidden field

### Scenario 3: User Without Family Number (Generating New)
1. User types a name that doesn't match any existing records
2. System shows "No family record found" message
3. User clicks "Generate New Family Number" button
4. System generates unique family number (e.g., `D-001` for "Dela Cruz")
5. Family number is saved to user's profile
6. Selected family number is displayed

### Scenario 4: Booking for Someone Else
1. User checks "Booking for someone else" checkbox
2. Family number section is reset and shows search interface
3. User can search for dependent's family members
4. Or generate a new family number for the dependent

---

## 🎨 CSS Styling

The feature uses Bootstrap 5 classes with custom styles:

```css
#familyNumberSearch {
    border-radius: 8px;
    border: 1px solid #ced4da;
    padding: 10px;
}

#familySearchResults {
    max-height: 200px;
    overflow-y: auto;
    border: 1px solid #dee2e6;
    border-radius: 8px;
}

.dropdown-item {
    cursor: pointer;
    padding: 10px;
    border-bottom: 1px solid #f0f0f0;
}

.dropdown-item:hover {
    background-color: #f8f9fa;
}

#familyNumberDisplay .badge {
    font-size: 1rem;
    padding: 8px 12px;
}
```

---

## 🔍 Search Logic

### Search Criteria:
- **Minimum Length:** 2 characters
- **Search Fields (ApplicationUsers):**
  - FirstName
  - LastName
  - Full name combination (FirstName + " " + LastName)
- **Search Fields (Patients):**
  - FullName

### Search Filters:
- Only returns records with a FamilyNumber assigned
- Case-insensitive search
- Results limited to 10 records
- Deduplicates results by (fullName, familyNumber)

### Result Priority:
1. Current logged-in user (marked as "You")
2. Other registered users (marked as "Family Member")
3. Patient records (marked as "Family Member")

---

## 🧪 Testing Instructions

### Test 1: Auto-Fill Existing Family Number
1. Log in as a user who already has a family number
2. Navigate to Book Appointment
3. Select a consultation type that requires family number (Dental, Prenatal, DOTS)
4. ✅ **Expected:** Family number should auto-fill in the display section

### Test 2: Search for Existing Family Member
1. Log in as a new user without a family number
2. Navigate to Book Appointment
3. Select a consultation type that requires family number
4. Type a name of an existing family member (e.g., "Juan")
5. ✅ **Expected:** Dropdown shows matching results with family numbers
6. Click on a result
7. ✅ **Expected:** Family number is selected and displayed

### Test 3: Generate New Family Number
1. Log in as a new user without a family number
2. Navigate to Book Appointment
3. Select a consultation type that requires family number
4. Type a random name that doesn't exist (e.g., "ZZZ Test")
5. ✅ **Expected:** "No family record found" message appears
6. Click "Generate New Family Number"
7. ✅ **Expected:** 
   - New family number is generated (e.g., `T-001`)
   - Family number is displayed
   - Family number is saved to user profile

### Test 4: Change Family Number
1. After selecting or generating a family number
2. Click the "Change" button
3. ✅ **Expected:** 
   - Search interface reappears
   - Previous selection is cleared
   - User can search again

### Test 5: Booking for Someone Else
1. Check "Booking for someone else" checkbox
2. ✅ **Expected:** Family number section resets to search interface
3. Search for a family member or generate new
4. ✅ **Expected:** Works independently of user's own family number

### Test 6: Dropdown Closes on Outside Click
1. Open search dropdown by typing
2. Click outside the dropdown area
3. ✅ **Expected:** Dropdown closes automatically

### Test 7: Debounced Search
1. Type quickly in the search field
2. ✅ **Expected:** Search API is not called for every keystroke
3. Search only triggers 300ms after user stops typing

---

## 🚀 API Endpoints

### GET: Search Family Members
**URL:** `?handler=SearchFamilyMembers&searchTerm={name}`

**Request:**
```
GET /BookAppointment?handler=SearchFamilyMembers&searchTerm=Juan
```

**Response:**
```json
{
  "success": true,
  "members": [
    {
      "fullName": "Juan Dela Cruz",
      "familyNumber": "D-001",
      "relationship": "You"
    },
    {
      "fullName": "Maria Dela Cruz",
      "familyNumber": "D-001",
      "relationship": "Family Member"
    }
  ]
}
```

### POST: Generate New Family Number
**URL:** `?handler=GenerateNewFamilyNumber`

**Request:**
```json
POST /BookAppointment?handler=GenerateNewFamilyNumber
Content-Type: application/json

{
  "lastName": "Dela Cruz",
  "firstName": "Juan"
}
```

**Response:**
```json
{
  "success": true,
  "familyNumber": "D-002",
  "message": "New family number D-002 has been generated."
}
```

---

## 📝 Database Updates

### Tables Affected:
1. **AspNetUsers**
   - `FamilyNumber` column updated when generating new number
   
2. **Patients**
   - `FamilyNumber` column updated when generating new number
   - `UpdatedAt` timestamp updated

---

## ⚠️ Important Notes

1. **Old Family Member Generator Removed:** The previous family member input and generate button has been completely replaced with this new search interface.

2. **Thread-Safe Generation:** The `GenerateNewFamilyNumberAsync` method uses `SemaphoreSlim` to ensure only one family number is generated at a time, preventing duplicates.

3. **Case-Insensitive Search:** All searches are converted to lowercase for matching.

4. **Persistent Storage:** Generated family numbers are saved to both the user's profile and patient record for consistency.

5. **Future Bookings:** Once a family number is assigned, it will automatically appear in future appointment bookings.

---

## 🔗 Related Files

- `Pages/BookAppointment.cshtml` - UI and JavaScript
- `Pages/BookAppointment.cshtml.cs` - API endpoints and business logic
- `Services/FamilyNumberService.cs` - Family number generation service
- `Models/ApplicationUser.cs` - User model with FamilyNumber property
- `Models/Patient.cs` - Patient model with FamilyNumber property

---

## ✅ Status

**Implementation:** ✅ Complete  
**Build Status:** ✅ Success (0 errors, 2 warnings unrelated to this feature)  
**Testing Status:** ⏳ Pending user testing

---

## 🎯 Next Steps

1. **Run the application** and test all scenarios listed above
2. **Verify family numbers** are correctly assigned and searchable
3. **Check database** to ensure FamilyNumber is saved in both AspNetUsers and Patients tables
4. **Test with multiple users** to ensure search returns correct family members

If you encounter any issues during testing, please let me know and I'll help resolve them!

