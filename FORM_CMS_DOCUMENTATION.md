# 📋 Form CMS Documentation

## Overview
The Form CMS (Content Management System) allows administrators to create, manage, and deploy dynamic forms without modifying source code. This system provides a flexible solution for managing various types of forms such as registration forms, surveys, assessments, and more.

---

## 🎯 Features

### Admin Features
- ✅ Create and manage form templates
- ✅ Add/edit/delete form fields dynamically
- ✅ Configure field types (text, email, number, date, dropdown, radio, checkbox, file upload, etc.)
- ✅ Set field validation rules
- ✅ Manage dropdown options
- ✅ Toggle form active/inactive status
- ✅ View form submissions
- ✅ Export form data

### Form Features
- ✅ Dynamic form rendering
- ✅ Client-side validation
- ✅ Responsive design (Bootstrap 5)
- ✅ Multiple field types support
- ✅ Conditional field display (planned)
- ✅ File upload support
- ✅ Custom CSS classes
- ✅ Success messages and redirects

---

## 🗄️ Database Schema

### Tables Created

#### 1. **FormTemplates**
Stores form definitions and metadata.

| Column | Type | Description |
|--------|------|-------------|
| FormTemplateId | int (PK) | Unique identifier |
| FormName | nvarchar(200) | Display name of the form |
| FormKey | nvarchar(100) | Unique key for URL routing |
| Description | nvarchar(1000) | Form description |
| Category | nvarchar(100) | Form category |
| IsActive | bit | Active status |
| DisplayOrder | int | Sort order |
| IconClass | nvarchar(100) | Font Awesome icon class |
| SuccessMessage | nvarchar(1000) | Success message after submission |
| RedirectUrl | nvarchar(500) | Redirect URL after submission |
| Version | int | Version number |
| CreatedAt | datetime2 | Creation timestamp |
| UpdatedAt | datetime2 | Last update timestamp |

#### 2. **FormFields**
Stores individual form fields.

| Column | Type | Description |
|--------|------|-------------|
| FormFieldId | int (PK) | Unique identifier |
| FormTemplateId | int (FK) | Parent form template |
| FieldName | nvarchar(200) | Field name (for data storage) |
| FieldLabel | nvarchar(200) | Display label |
| FieldType | nvarchar(50) | Field type (text, email, select, etc.) |
| Placeholder | nvarchar(500) | Placeholder text |
| HelpText | nvarchar(1000) | Help text |
| IsRequired | bit | Required field flag |
| IsReadOnly | bit | Read-only flag |
| DisplayOrder | int | Sort order |
| FieldWidth | nvarchar(100) | Bootstrap column class |
| ValidationRules | nvarchar(max) | JSON validation rules |

#### 3. **FormFieldOptions**
Stores options for select, radio, and checkbox fields.

| Column | Type | Description |
|--------|------|-------------|
| FormFieldOptionId | int (PK) | Unique identifier |
| FormFieldId | int (FK) | Parent form field |
| OptionLabel | nvarchar(500) | Display label |
| OptionValue | nvarchar(500) | Stored value |
| DisplayOrder | int | Sort order |
| IsDefault | bit | Default selection |
| IsActive | bit | Active status |

#### 4. **FormSubmissions**
Stores form submission data.

| Column | Type | Description |
|--------|------|-------------|
| FormSubmissionId | int (PK) | Unique identifier |
| FormTemplateId | int (FK) | Parent form template |
| UserId | nvarchar(450) (FK) | Submitter user ID (optional) |
| FormData | nvarchar(max) | JSON submission data |
| IpAddress | nvarchar(45) | Submitter IP address |
| Status | nvarchar(50) | Submission status |
| SubmittedAt | datetime2 | Submission timestamp |

---

## 🚀 Setup Instructions

### 1. Run Database Migration

```bash
# Add migration
dotnet ef migrations add AddFormCMSTables

# Update database
dotnet ef database update
```

### 2. Register Services

Add the following to your `Program.cs` or `Startup.cs`:

```csharp
// Add service registration
builder.Services.AddScoped<IDynamicFormService, DynamicFormService>();
```

### 3. Configure Authorization

Ensure that only Admin and SuperAdmin roles can access Form Management:
- The pages are already decorated with `[Authorize(Roles = "Admin,SuperAdmin")]`

---

## 📖 Usage Guide

### For Administrators

#### Creating a New Form

1. **Navigate to Form Management**
   - Go to Admin Portal → System Tools → Form Management

2. **Click "Add New Form"**
   - Fill in form details:
     - Form Name (e.g., "Patient Registration")
     - Form Key (e.g., "patient-registration")
     - Category (e.g., "Registration")
     - Description
     - Icon Class (optional)
     - Success Message

3. **Add Form Fields**
   - After creating the form, you'll be redirected to "Manage Form Fields"
   - Click "Add Field" to add fields
   - Configure field properties:
     - Field Label
     - Field Name (used for data storage)
     - Field Type
     - Required/Optional
     - Validation rules

4. **Configure Field Options** (for dropdowns, radio, checkboxes)
   - Click "Manage Options" for select/radio/checkbox fields
   - Add options with labels and values
   - Set default selections

5. **Activate the Form**
   - Toggle form status to "Active"
   - Form is now available for use

#### Managing Existing Forms

- **Edit Form**: Modify form properties
- **Manage Fields**: Add, edit, delete, or reorder fields
- **View Submissions**: See all form submissions
- **Delete Form**: Remove form and all associated data

---

### For Developers

#### Rendering a Dynamic Form

**Method 1: Using Partial View**

```cshtml
@{
    var formService = new DynamicFormService(DbContext);
    var form = await formService.GetFormByKeyAsync("patient-registration");
}

@if (form != null)
{
    @await Html.PartialAsync("_DynamicFormRenderer", form)
}
```

**Method 2: In Page Model**

```csharp
public class MyPageModel : PageModel
{
    private readonly IDynamicFormService _formService;

    public FormTemplate MyForm { get; set; }

    public MyPageModel(IDynamicFormService formService)
    {
        _formService = formService;
    }

    public async Task OnGetAsync()
    {
        MyForm = await _formService.GetFormByKeyAsync("patient-registration");
    }
}
```

```cshtml
@page
@model MyPageModel

<div class="container">
    @if (Model.MyForm != null)
    {
        @await Html.PartialAsync("_DynamicFormRenderer", Model.MyForm)
    }
</div>
```

#### Handling Form Submissions

Form submissions are automatically handled by the API controller at `/api/DynamicForm/Submit`.

You can also retrieve submissions programmatically:

```csharp
var submissions = await _formService.GetFormSubmissionsAsync(formTemplateId);

foreach (var submission in submissions)
{
    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(submission.FormData);
    // Process submission data
}
```

---

## 🎨 Field Types Supported

| Field Type | HTML Input Type | Use Case |
|------------|----------------|----------|
| text | text | General text input |
| email | email | Email addresses |
| tel | tel | Phone numbers |
| number | number | Numeric values |
| date | date | Date selection |
| time | time | Time selection |
| datetime-local | datetime-local | Date and time |
| textarea | textarea | Multi-line text |
| select | select | Dropdown list |
| radio | radio | Single choice from options |
| checkbox | checkbox | Multiple choices |
| file | file | File upload |
| hidden | hidden | Hidden values |

---

## 🔧 Advanced Configuration

### Custom Validation Rules

You can store JSON validation rules in the `ValidationRules` field:

```json
{
  "minLength": 3,
  "maxLength": 100,
  "pattern": "^[A-Za-z]+$",
  "min": 0,
  "max": 150
}
```

### Conditional Logic (Planned)

Store conditional display rules in the `ConditionalLogic` field:

```json
{
  "dependsOn": "marital_status",
  "condition": "equals",
  "value": "Married",
  "action": "show"
}
```

### Custom CSS Classes

Add custom styling by specifying CSS classes in:
- **Form Level**: `FormTemplate.CssClasses`
- **Field Level**: `FormField.CssClasses`

---

## 📊 Example Use Cases

### 1. Patient Registration Form

```
Fields:
- Full Name (text, required)
- Date of Birth (date, required)
- Gender (radio: Male/Female/Other)
- Contact Number (tel, required)
- Email (email)
- Address (textarea)
- Medical History (textarea)
```

### 2. Health Survey

```
Fields:
- Have you experienced symptoms? (radio: Yes/No)
- Select symptoms (checkbox: Fever, Cough, Headache, etc.)
- Severity (select: Mild/Moderate/Severe)
- Duration (number)
- Additional notes (textarea)
```

### 3. Appointment Feedback

```
Fields:
- Rate your experience (select: 1-5 stars)
- Doctor name (select)
- Service quality (radio)
- Comments (textarea)
- Would you recommend us? (radio: Yes/No)
```

---

## 🔒 Security Considerations

1. **Authorization**: Only Admin/SuperAdmin can create/edit forms
2. **Validation**: All forms use client-side and server-side validation
3. **SQL Injection**: EF Core parameterized queries prevent SQL injection
4. **XSS Protection**: Form data is properly sanitized
5. **CSRF Protection**: Anti-forgery tokens are used

---

## 🐛 Troubleshooting

### Form not appearing
- Check if form is marked as "Active"
- Verify form key is correct
- Ensure database has been migrated

### Fields not saving
- Check field name is unique within form
- Verify required fields are filled
- Check database connection

### Submissions not working
- Ensure API endpoint is accessible
- Check browser console for JavaScript errors
- Verify anti-forgery token is present

---

## 🔄 Future Enhancements

- [ ] Drag-and-drop field reordering
- [ ] Conditional field display
- [ ] Form templates/duplication
- [ ] Import/export forms (JSON)
- [ ] Advanced validation rules editor
- [ ] Multi-language support
- [ ] Form analytics dashboard
- [ ] Email notifications on submission
- [ ] PDF export of submissions
- [ ] Form versioning and rollback

---

## 📞 Support

For questions or issues, please contact the development team or create an issue in the repository.

---

**Last Updated**: October 2025  
**Version**: 1.0.0
