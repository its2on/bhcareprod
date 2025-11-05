# 🔄 Form CMS Migration Guide

## Prerequisites

Before running the migration, ensure:
- ✅ All Form CMS model files are in place
- ✅ ApplicationDbContext has been updated
- ✅ EF Core tools are installed
- ✅ Database connection string is configured

---

## Step-by-Step Migration

### 1. Install EF Core Tools (if not already installed)

```bash
dotnet tool install --global dotnet-ef
```

Or update existing tools:

```bash
dotnet tool update --global dotnet-ef
```

### 2. Create Migration

```bash
# Navigate to project directory
cd "c:\Users\WIN 10\Desktop\BHCARE-main"

# Create migration
dotnet ef migrations add AddFormCMSTables --context ApplicationDbContext
```

This will create a new migration file in the `Migrations` folder.

### 3. Review Migration

Open the generated migration file and verify it includes:
- CreateTable for `FormTemplates`
- CreateTable for `FormFields`
- CreateTable for `FormFieldOptions`
- CreateTable for `FormSubmissions`
- CreateIndex statements for foreign keys and commonly queried columns

### 4. Apply Migration

```bash
# Update database
dotnet ef database update --context ApplicationDbContext
```

### 5. Verify Tables

Connect to your SQL Server database and verify the following tables were created:

```sql
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN (
    'FormTemplates',
    'FormFields', 
    'FormFieldOptions',
    'FormSubmissions'
)
ORDER BY TABLE_NAME;
```

---

## Manual SQL Script (Alternative)

If you prefer to run SQL directly, use this script:

```sql
-- Create FormTemplates table
CREATE TABLE [dbo].[FormTemplates] (
    [FormTemplateId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FormName] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [FormKey] NVARCHAR(100) NOT NULL UNIQUE,
    [Category] NVARCHAR(100) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [IconClass] NVARCHAR(100) NULL,
    [CssClasses] NVARCHAR(500) NULL,
    [SuccessMessage] NVARCHAR(1000) NULL,
    [RedirectUrl] NVARCHAR(500) NULL,
    [JsonConfiguration] NVARCHAR(MAX) NULL,
    [Version] INT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    [CreatedBy] NVARCHAR(450) NULL,
    [UpdatedBy] NVARCHAR(450) NULL
);

CREATE INDEX IX_FormTemplates_FormKey ON [dbo].[FormTemplates]([FormKey]);
CREATE INDEX IX_FormTemplates_IsActive ON [dbo].[FormTemplates]([IsActive]);

-- Create FormFields table
CREATE TABLE [dbo].[FormFields] (
    [FormFieldId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FormTemplateId] INT NOT NULL,
    [FieldName] NVARCHAR(200) NOT NULL,
    [FieldLabel] NVARCHAR(200) NOT NULL,
    [FieldType] NVARCHAR(50) NOT NULL DEFAULT 'text',
    [Placeholder] NVARCHAR(500) NULL,
    [DefaultValue] NVARCHAR(1000) NULL,
    [HelpText] NVARCHAR(1000) NULL,
    [IsRequired] BIT NOT NULL DEFAULT 0,
    [IsReadOnly] BIT NOT NULL DEFAULT 0,
    [IsDisabled] BIT NOT NULL DEFAULT 0,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [ValidationRules] NVARCHAR(MAX) NULL,
    [CssClasses] NVARCHAR(500) NULL,
    [FieldWidth] NVARCHAR(100) NULL DEFAULT 'col-12',
    [ConditionalLogic] NVARCHAR(MAX) NULL,
    [CustomAttributes] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    CONSTRAINT FK_FormFields_FormTemplates FOREIGN KEY ([FormTemplateId]) 
        REFERENCES [dbo].[FormTemplates]([FormTemplateId]) ON DELETE CASCADE
);

CREATE INDEX IX_FormFields_FormTemplateId ON [dbo].[FormFields]([FormTemplateId]);

-- Create FormFieldOptions table
CREATE TABLE [dbo].[FormFieldOptions] (
    [FormFieldOptionId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FormFieldId] INT NOT NULL,
    [OptionLabel] NVARCHAR(500) NOT NULL,
    [OptionValue] NVARCHAR(500) NOT NULL,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [IsDefault] BIT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IconClass] NVARCHAR(100) NULL,
    [GroupName] NVARCHAR(200) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_FormFieldOptions_FormFields FOREIGN KEY ([FormFieldId]) 
        REFERENCES [dbo].[FormFields]([FormFieldId]) ON DELETE CASCADE
);

CREATE INDEX IX_FormFieldOptions_FormFieldId ON [dbo].[FormFieldOptions]([FormFieldId]);

-- Create FormSubmissions table
CREATE TABLE [dbo].[FormSubmissions] (
    [FormSubmissionId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FormTemplateId] INT NOT NULL,
    [UserId] NVARCHAR(450) NULL,
    [FormData] NVARCHAR(MAX) NOT NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Submitted',
    [Notes] NVARCHAR(MAX) NULL,
    [SubmittedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ProcessedAt] DATETIME2 NULL,
    [ProcessedBy] NVARCHAR(450) NULL,
    CONSTRAINT FK_FormSubmissions_FormTemplates FOREIGN KEY ([FormTemplateId]) 
        REFERENCES [dbo].[FormTemplates]([FormTemplateId]) ON DELETE NO ACTION,
    CONSTRAINT FK_FormSubmissions_Users FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION
);

CREATE INDEX IX_FormSubmissions_FormTemplateId ON [dbo].[FormSubmissions]([FormTemplateId]);
CREATE INDEX IX_FormSubmissions_UserId ON [dbo].[FormSubmissions]([UserId]);
CREATE INDEX IX_FormSubmissions_SubmittedAt ON [dbo].[FormSubmissions]([SubmittedAt]);

GO
```

---

## Service Registration

Add the following to your `Program.cs` (after `var builder = WebApplication.CreateBuilder(args);`):

```csharp
// Register Dynamic Form Service
builder.Services.AddScoped<IDynamicFormService, DynamicFormService>();
```

---

## Rollback (If Needed)

If you need to rollback the migration:

```bash
# Rollback to previous migration
dotnet ef database update <PreviousMigrationName> --context ApplicationDbContext

# Or remove the last migration
dotnet ef migrations remove --context ApplicationDbContext
```

To completely remove all Form CMS tables:

```sql
-- Drop tables in reverse order (due to foreign keys)
DROP TABLE IF EXISTS [dbo].[FormSubmissions];
DROP TABLE IF EXISTS [dbo].[FormFieldOptions];
DROP TABLE IF EXISTS [dbo].[FormFields];
DROP TABLE IF EXISTS [dbo].[FormTemplates];
```

---

## Post-Migration Testing

### 1. Verify Admin Access

1. Login as Admin
2. Navigate to: `/Admin/FormManagement`
3. Verify page loads without errors

### 2. Create Test Form

1. Click "Add New Form"
2. Fill in form details:
   - Form Name: "Test Form"
   - Form Key: "test-form"
   - Category: "Testing"
3. Click "Create Form"
4. Add a few test fields

### 3. Test Form Rendering

Create a test page to render the form:

```cshtml
@page
@model TestFormModel
@inject IDynamicFormService FormService

@{
    var form = await FormService.GetFormByKeyAsync("test-form");
}

@if (form != null)
{
    @await Html.PartialAsync("_DynamicFormRenderer", form)
}
```

---

## Troubleshooting

### Error: "The entity type 'FormTemplate' requires a primary key to be defined"

**Solution**: Ensure `[Key]` attribute is present on ID properties.

### Error: "There is already an object named 'FormTemplates' in the database"

**Solution**: Tables already exist. Either:
- Drop existing tables manually
- Skip this migration
- Use a different migration name

### Error: "Could not create constraint or index"

**Solution**: Check for:
- Duplicate keys/indexes
- Invalid foreign key references
- Conflicting column names

---

## Success Verification Checklist

- [ ] All four tables created successfully
- [ ] Indexes created properly
- [ ] Foreign key constraints in place
- [ ] Admin page accessible (`/Admin/FormManagement`)
- [ ] Can create a new form
- [ ] Can add fields to form
- [ ] Can manage field options
- [ ] Form renders correctly
- [ ] No console errors

---

**Migration Complete!** 🎉

Your Form CMS is now ready to use.
