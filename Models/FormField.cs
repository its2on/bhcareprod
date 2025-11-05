using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barangay.Models
{
    /// <summary>
    /// Represents a single field within a form template
    /// </summary>
    public class FormField
    {
        [Key]
        public int FormFieldId { get; set; }

        [Required]
        public int FormTemplateId { get; set; }

        [Required]
        [MaxLength(200)]
        public string FieldName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FieldLabel { get; set; } = string.Empty;

        /// <summary>
        /// Optional title for the field (displayed above the label)
        /// </summary>
        [MaxLength(300)]
        public string? Title { get; set; }

        /// <summary>
        /// Field type: text, number, email, tel, date, time, datetime, 
        /// textarea, select, checkbox, radio, file, hidden, etc.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string FieldType { get; set; } = "text";

        /// <summary>
        /// Placeholder text for the field
        /// </summary>
        [MaxLength(500)]
        public string? Placeholder { get; set; }

        /// <summary>
        /// Default value for the field
        /// </summary>
        [MaxLength(1000)]
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Help text or description for the field
        /// </summary>
        [MaxLength(1000)]
        public string? HelpText { get; set; }

        /// <summary>
        /// Indicates if the field is required
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// Indicates if the field is read-only
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// Indicates if the field is disabled
        /// </summary>
        public bool IsDisabled { get; set; } = false;

        /// <summary>
        /// Display order of the field within the form
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Simple validation pattern (text-only, letters-only, alphanumeric, number, integer, decimal)
        /// </summary>
        [MaxLength(50)]
        public string? ValidationPattern { get; set; }

        /// <summary>
        /// Validation rules (JSON format)
        /// E.g., {"minLength": 3, "maxLength": 100, "pattern": "^[A-Za-z]+$"}
        /// </summary>
        public string? ValidationRules { get; set; }

        /// <summary>
        /// Custom CSS classes for the field
        /// </summary>
        [MaxLength(500)]
        public string? CssClasses { get; set; }

        /// <summary>
        /// Field width (e.g., "col-md-6", "col-12")
        /// </summary>
        [MaxLength(100)]
        public string? FieldWidth { get; set; } = "col-12";

        /// <summary>
        /// Conditional display logic (JSON format)
        /// E.g., {"dependsOn": "fieldName", "condition": "equals", "value": "Yes"}
        /// </summary>
        public string? ConditionalLogic { get; set; }

        /// <summary>
        /// Custom attributes (JSON format)
        /// E.g., {"data-toggle": "tooltip", "data-placement": "top"}
        /// </summary>
        public string? CustomAttributes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("FormTemplateId")]
        public virtual FormTemplate FormTemplate { get; set; } = null!;

        public virtual ICollection<FormFieldOption> FormFieldOptions { get; set; } = new List<FormFieldOption>();
    }
}
