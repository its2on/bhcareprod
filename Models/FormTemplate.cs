using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barangay.Models
{
    /// <summary>
    /// Represents a dynamic form template that can be configured by admin
    /// </summary>
    public class FormTemplate
    {
        [Key]
        public int FormTemplateId { get; set; }

        [Required]
        [MaxLength(200)]
        public string FormName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Unique identifier for the form (used in URLs, etc.)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FormKey { get; set; } = string.Empty;

        /// <summary>
        /// Category of the form (e.g., "Registration", "Assessment", "Medical")
        /// </summary>
        [MaxLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// Indicates if this form is active and can be used
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display order for listing forms
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Minimum age required to fill this form (null = no restriction)
        /// Used for age-based forms like HEEADSSS (10-19) and NCD (20+)
        /// </summary>
        public int? MinAge { get; set; }

        /// <summary>
        /// Maximum age allowed to fill this form (null = no restriction)
        /// Used for age-based forms like HEEADSSS (10-19) and NCD (20+)
        /// </summary>
        public int? MaxAge { get; set; }

        /// <summary>
        /// Indicates if this form should appear in appointment workflow
        /// True for clinical forms (HEEADSSS, NCD), False for general forms (surveys)
        /// </summary>
        public bool ShowInAppointmentFlow { get; set; } = false;

        /// <summary>
        /// Icon class for the form (Font Awesome, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? IconClass { get; set; }

        /// <summary>
        /// Custom CSS classes for the form
        /// </summary>
        [MaxLength(500)]
        public string? CssClasses { get; set; }

        /// <summary>
        /// Success message to display after form submission
        /// </summary>
        [MaxLength(1000)]
        public string? SuccessMessage { get; set; }

        /// <summary>
        /// Redirect URL after successful submission (optional)
        /// </summary>
        [MaxLength(500)]
        public string? RedirectUrl { get; set; }

        /// <summary>
        /// JSON configuration for advanced settings
        /// </summary>
        public string? JsonConfiguration { get; set; }

        /// <summary>
        /// Version number for form versioning
        /// </summary>
        public int Version { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(450)]
        public string? CreatedBy { get; set; }

        [MaxLength(450)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public virtual ICollection<FormField> FormFields { get; set; } = new List<FormField>();
        public virtual ICollection<FormSubmission> FormSubmissions { get; set; } = new List<FormSubmission>();
    }
}
