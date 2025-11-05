# 🚨 APPLY DATABASE MIGRATION - CRITICAL

## Error You're Seeing

```
Invalid column name 'MaxAge'.
Invalid column name 'MinAge'.
Invalid column name 'ShowInAppointmentFlow'.
Invalid column name 'AppointmentId'.
```

**This is EXPECTED!** The database needs to be updated with new columns.

---

## ✅ SOLUTION: Follow These Steps Exactly

### 1️⃣ **STOP the Application**

Close all browser windows with the app, then press `Ctrl + C` in the terminal where it's running.

Wait until you see the terminal prompt again.

---

### 2️⃣ **Create the Migration**

Copy and paste this command:

```powershell
dotnet ef migrations add AddAppointmentIntegrationToForms --context ApplicationDbContext
```

**Expected Output:**
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

---

### 3️⃣ **Apply the Migration to Database**

Copy and paste this command:

```powershell
dotnet ef database update --context ApplicationDbContext
```

**Expected Output:**
```
Build started...
Build succeeded.
Applying migration '20241029XXXXXX_AddAppointmentIntegrationToForms'.
Done.
```

---

### 4️⃣ **Verify Migration Success**

You should see a new file created in your `Migrations` folder:
- `XXXXXX_AddAppointmentIntegrationToForms.cs`

This file contains:
```csharp
migrationBuilder.AddColumn<int?>(
    name: "AppointmentId",
    table: "FormSubmissions",
    type: "int",
    nullable: true);

migrationBuilder.AddColumn<int?>(
    name: "MinAge",
    table: "FormTemplates",
    type: "int",
    nullable: true);

migrationBuilder.AddColumn<int?>(
    name: "MaxAge",
    table: "FormTemplates",
    type: "int",
    nullable: true);

migrationBuilder.AddColumn<bool>(
    name: "ShowInAppointmentFlow",
    table: "FormTemplates",
    type: "bit",
    nullable: false,
    defaultValue: false);
```

---

### 5️⃣ **Start the Application**

```powershell
dotnet run
```

**The error should be GONE!** ✅

---

## 🎉 After Successful Migration

Once the app starts without errors:

1. **Login as Admin**
2. **Go to Admin → Form Management**
3. **Click "Add New Form"**
4. **Create HEEADSSS Form:**
   - Form Name: `HEEADSSS Assessment`
   - Form Key: `heeadsss-assessment`
   - Min Age: `10`
   - Max Age: `19`
   - ☑ Show in Appointment Workflow
   - Icon: `fa-solid fa-user-friends`
   - Add all assessment questions
   - Save

5. **Create NCD Form:**
   - Form Name: `NCD Risk Assessment`
   - Form Key: `ncd-risk-assessment`
   - Min Age: `20`
   - Max Age: (leave empty)
   - ☑ Show in Appointment Workflow
   - Icon: `fa-solid fa-heartbeat`
   - Add all assessment questions
   - Save

6. **Test:**
   - Go to an appointment with a 15-year-old patient
   - You should see HEEADSSS form appear automatically!

---

## ⚠️ Troubleshooting

### If migration command fails with "Build failed":
```powershell
# First build the project
dotnet build

# Then try migration again
dotnet ef migrations add AddAppointmentIntegrationToForms --context ApplicationDbContext
```

### If you see "No DbContext named 'ApplicationDbContext' was found":
```powershell
# Make sure you're in the project directory
cd "C:\Users\WIN 10\Desktop\BHCARE-main"

# Then try again
dotnet ef migrations add AddAppointmentIntegrationToForms --context ApplicationDbContext
```

### If database update fails:
- Check your connection string in `appsettings.json`
- Make sure SQL Server is running
- Try restarting SQL Server

---

## 📊 What This Migration Does

### Changes to FormTemplates Table:
- ➕ Adds `MinAge` column (nullable int)
- ➕ Adds `MaxAge` column (nullable int)
- ➕ Adds `ShowInAppointmentFlow` column (bool, default false)

### Changes to FormSubmissions Table:
- ➕ Adds `AppointmentId` column (nullable int)
- ➕ Adds foreign key to Appointments table

### Result:
- Forms can now be age-restricted (e.g., HEEADSSS for 10-19 only)
- Forms can be marked to appear in appointment workflow
- Form submissions can be linked to specific appointments
- Complete audit trail maintained

---

## 🚀 You're Almost There!

**Just 3 commands away from success:**

```powershell
# 1. Stop app (Ctrl + C)

# 2. Create migration
dotnet ef migrations add AddAppointmentIntegrationToForms --context ApplicationDbContext

# 3. Apply migration
dotnet ef database update --context ApplicationDbContext

# 4. Start app
dotnet run
```

**Then create your forms and you're done!** 🎉

