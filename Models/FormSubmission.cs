using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barangay.Models
{
    /// <summary>
    /// Represents a form submission by a user
    /// </summary>
    public class FormSubmission
    {
        [Key]
        public int FormSubmissionId { get; set; }

        [Required]
        public int FormTemplateId { get; set; }

        /// <summary>
        /// User who submitted the form (optional - for anonymous forms)
        /// </summary>
        [MaxLength(450)]
        public string? UserId { get; set; }

        /// <summary>
        /// Appointment ID if this form is linked to an appointment
        /// Used for HEEADSSS, NCD, and other appointment-based forms
        /// </summary>
        public int? AppointmentId { get; set; }

        /// <summary>
        /// Submitted form data in JSON format
        /// Stores all field values as key-value pairs
        /// </summary>
        [Required]
        public string FormData { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the submitter
        /// </summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent string
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Status of the submission (e.g., "Submitted", "Processing", "Completed", "Rejected")
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Submitted";

        /// <summary>
        /// Notes or comments about the submission
        /// </summary>
        public string? Notes { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        [MaxLength(450)]
        public string? ProcessedBy { get; set; }

        // Navigation properties
        [ForeignKey("FormTemplateId")]
        public virtual FormTemplate FormTemplate { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }
    }
}
