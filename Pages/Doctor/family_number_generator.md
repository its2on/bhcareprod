# 🧩 Family Number Generator Enhancement & Storage Integration

## 🎯 Objective
Enhance the existing **Family Number Generator** on the **Book Appointment** page to properly generate, assign, and store Family Numbers in the **Doctor/PatientList** table while ensuring correct grouping of related family members.

---

## 🧠 Overview
- Family Numbers are based on the **first letter of the patient’s last name** (e.g., `T-001`, `T-002`).
- Related family members should share the same Family Number.
- Unrelated patients with the same last name must have **unique Family Numbers**.
- The Family Number should be **stored in the Doctor/PatientList table** for reference across all medical modules.

---

## ⚙️ Logic Flow

### **1. Booking for Someone Else**
When a user checks **“Booking for someone else?”**, show an additional checkbox:
- **Label:** “Same Family?”
- **Purpose:** Determines if the booking is for a family member under the same Family Number.

### **2. Family Number Assignment Rules**
- If **Same Family?** is checked:
  - Retrieve the existing Family Number from **Doctor/PatientList** for that last name.
  - Assign the same Family Number to the new patient.
- If **Same Family?** is unchecked:
  - Generate a **new Family Number** using the first letter of the last name and incrementing the numeric suffix.
  - Example:
    - First patient with last name **Takeshi** → `T-001`
    - Next unrelated patient with last name **Takeshi** → `T-002`
- If a patient already has a Family Number, display it and disable editing.

### **3. Storage Behavior**
- On appointment submission:
  - If the patient exists in **Doctor/PatientList**, update the Family Number only if missing.
  - If not, create a new entry in **Doctor/PatientList** with the Family Number.
- Family Number must be accessible in all relevant modules:
  - Dental
  - Prenatal
  - DOTS
  - Immunization

---

## 🗂️ Database Integration

### **Doctor/PatientList Table**
| Column | Type | Description |
|--------|------|-------------|
| `Id` | int | Primary key |
| `FullName` | nvarchar | Patient’s full name |
| `FamilyNumber` | nvarchar(10) | Auto-generated Family Number |
| `Relationship` | nvarchar(50) | Relationship to primary patient |
| `CreatedDate` | datetime | Record creation timestamp |

---

## 💻 Backend Logic (C# / EF Core)
```csharp
var prefix = lastName.Substring(0, 1).ToUpper();
var lastFamNumber = _context.PatientList
    .Where(p => p.FamilyNumber.StartsWith(prefix))
    .OrderByDescending(p => p.FamilyNumber)
    .Select(p => p.FamilyNumber)
    .FirstOrDefault();

var nextNumber = 1;
if (lastFamNumber != null)
{
    var numPart = int.Parse(lastFamNumber.Split('-')[1]);
    nextNumber = numPart + 1;
}

var generatedFamilyNumber = $"{prefix}-{nextNumber:D3}";
```

- If **Same Family?** is checked → reuse Family Number from related entry.
- If unchecked → generate new Family Number as above.
- Store the value in **Appointments** and **Doctor/PatientList** tables.

---

## 🧾 Audit Trail Requirements
Every Family Number action should be logged with:
| Field | Description |
|--------|-------------|
| **Action Type** | Generated / Reused / Updated |
| **User** | Name or ID of the staff performing the action |
| **Family Number** | The affected Family Number (e.g., `T-001`) |
| **Timestamp** | Date and time of action |

---

## 🖥️ Frontend (Razor)
- Add a **“Same Family?”** checkbox beside the Family Number field.
- Use JavaScript or jQuery to toggle between generation and retrieval.
- Display the auto-generated Family Number dynamically.

---

## ✅ Expected Outcome
- Family Number generation is consistent, unique, and relational.
- Patients from the same family share the same identifier.
- Doctor/PatientList table maintains centralized Family Number storage.
- Audit trail records all Family Number events for transparency.