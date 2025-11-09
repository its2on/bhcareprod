using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barangay.Models
{
    /// <summary>
    /// Represents a consultation service that can be dynamically managed by admin
    /// Examples: General Consult, Dental, Prenatal, DOTS, Immunization, etc.
    /// </summary>
    public class ConsultationService
    {
        [Key]
        public int ServiceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Unique identifier for the service (used in URLs, database references)
        /// Example: "general-consult", "dental", "prenatal", "dots"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ServiceKey { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Icon class for the service (Font Awesome, etc.)
        /// Example: "fa-solid fa-stethoscope", "fa-solid fa-tooth"
        /// </summary>
        [MaxLength(100)]
        public string? IconClass { get; set; }

        /// <summary>
        /// Color theme for the service (hex color code)
        /// Example: "#fd7e14", "#20c997", "#0d6efd"
        /// </summary>
        [MaxLength(20)]
        public string? ColorTheme { get; set; }

        /// <summary>
        /// Indicates if this service is active and available for booking
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display order for listing services
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Indicates if this service requires age-based assessment forms
        /// True for General Consult (triggers NCD/HEEADSSS based on age)
        /// False for specialized services (Dental, Prenatal, DOTS)
        /// </summary>
        public bool RequiresAgeBasedAssessment { get; set; } = false;

        /// <summary>
        /// Category of the service (e.g., "Clinical", "Preventive", "Maternal", "Specialized")
        /// </summary>
        [MaxLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// Minimum age requirement for this service (null = no restriction)
        /// </summary>
        public int? MinAge { get; set; }

        /// <summary>
        /// Maximum age requirement for this service (null = no restriction)
        /// </summary>
        public int? MaxAge { get; set; }

        /// <summary>
        /// Indicates if this service is available for walk-in patients
        /// </summary>
        public bool AllowsWalkIn { get; set; } = true;

        /// <summary>
        /// Average duration of consultation in minutes
        /// Used for scheduling and time slot management
        /// </summary>
        public int? AverageDurationMinutes { get; set; }

        /// <summary>
        /// Special instructions or notes for the service
        /// Displayed to patients during booking
        /// </summary>
        [MaxLength(1000)]
        public string? SpecialInstructions { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(450)]
        public string? CreatedBy { get; set; }

        [MaxLength(450)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        /// <summary>
        /// Forms that are linked to this service
        /// </summary>
        public virtual ICollection<FormTemplate> AssociatedForms { get; set; } = new List<FormTemplate>();

        /// <summary>
        /// Appointments booked for this service
        /// </summary>
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
