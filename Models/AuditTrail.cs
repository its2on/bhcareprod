using System;
using System.ComponentModel.DataAnnotations;

namespace Barangay.Models
{
    public class AuditTrail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string PerformedBy { get; set; } = string.Empty; // User email or name

        public string? UserId { get; set; } // FK to ApplicationUser

        [Required]
        public string Role { get; set; } = string.Empty; // Admin, Doctor, Nurse, Patient

        [Required]
        public string ActionType { get; set; } = string.Empty; // Create, Update, Delete, View, Login, Logout

        [Required]
        public string Action { get; set; } = string.Empty; // Human-readable action description

        public string EntityName { get; set; } = string.Empty; // e.g., "Prescription", "VitalSign", "User"
        public string EntityId { get; set; } = string.Empty; // ID of the affected entity

        public string? Description { get; set; } // Detailed description
        public string? IPAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Change tracking (JSON serialized)
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        // Enhanced tracking fields
        public string? RequestMethod { get; set; } // GET, POST, PUT, DELETE
        public string? RequestUrl { get; set; } // Full request URL
        public string? DeviceInfo { get; set; } // User agent / device information
        public string? Location { get; set; } // Geographic location (if available)
        public string? AdditionalContext { get; set; } // JSON with extra context
        public string Outcome { get; set; } = "Success"; // Success, Failed, Warning
        public string? SessionId { get; set; } // Session identifier

        // Navigation property
        public virtual ApplicationUser? User { get; set; }
    }
}
