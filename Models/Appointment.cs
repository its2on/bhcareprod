using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json.Serialization;
using Barangay.Extensions;
using Barangay.Helpers;
using Barangay.Attributes;

namespace Barangay.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(450)]
        public string PatientId { get; set; } = string.Empty;
        
        [StringLength(450)]
        public string? DoctorId { get; set; }
        
        [Required]
        [StringLength(1000)] // Increased for encrypted data
        [Encrypted]
        public string PatientName { get; set; } = string.Empty;
        
        // Dependent-related properties
        [StringLength(1000)] // Increased for encrypted data
        [Encrypted]
        public string? DependentFullName { get; set; }
        public int? DependentAge { get; set; }
        [StringLength(50)]
        public string? RelationshipToDependent { get; set; }
        
        // Booking for someone else properties
        public bool BookingForOther { get; set; }
        
        [StringLength(50)]
        public string? Relationship { get; set; }
        
        // Family Number (for grouping related patients)
        [StringLength(50)]
        public string? FamilyNumber { get; set; }
        
        // Basic Info
        [StringLength(10)]
        public string? Gender { get; set; }
        [StringLength(100)] // Increased for encrypted data
        [Encrypted]
        public string? ContactNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [StringLength(1000)] // Increased for encrypted data
        [Encrypted]
        public string? Address { get; set; }
        
        // Emergency Contact
        [StringLength(500)] // Increased for encrypted data
        [Encrypted]
        public string? EmergencyContact { get; set; }
        [StringLength(100)] // Increased for encrypted data
        [Encrypted]
        public string? EmergencyContactNumber { get; set; }
        
        // Medical Info
        [StringLength(2000)] // Increased for encrypted data
        [Encrypted]
        public string? Allergies { get; set; }
        [StringLength(2000)] // Increased for encrypted data
        [Encrypted]
        public string? MedicalHistory { get; set; }
        [StringLength(2000)] // Increased for encrypted data
        [Encrypted]
        public string? CurrentMedications { get; set; }
        public string? AttachmentsData { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }
        
        [Required]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentTime { get; set; }
        public string AppointmentTimeInput { get; set; } = string.Empty;
        
        [StringLength(2000)] // Increased for encrypted data
        [Encrypted]
        public string Description { get; set; } = string.Empty;
        
        [StringLength(2000)] // Increased for encrypted data
        [Encrypted]
        public string ReasonForVisit { get; set; } = string.Empty;
        
        [Required]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        
        public int AgeValue { get; set; }
        
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(50)]
        public string? Type { get; set; }
        
        /// <summary>
        /// Links this appointment to a consultation service
        /// If null, defaults to "General Consult" (for backward compatibility)
        /// </summary>
        public int? ServiceId { get; set; }
        
        [StringLength(500)]
        public string? AttachmentPath { get; set; }
        
        [StringLength(2000)] // Increased for encrypted data
        [Encrypted]
        public string? Prescription { get; set; }
        
        [StringLength(2000)] // Increased for encrypted data
        [Encrypted]
        public string? Instructions { get; set; }
        
        // Added ApplicationUserId property to match database schema
        [StringLength(450)]
        public string? ApplicationUserId { get; set; }
        
        // Navigation properties - Updated for Patient to use the Patient model
        [ForeignKey(nameof(PatientId))]
        [JsonIgnore]
        public virtual Patient Patient { get; set; } = null!;
        
        [ForeignKey(nameof(DoctorId))]
        [InverseProperty("DoctorAppointments")]
        public virtual ApplicationUser? Doctor { get; set; }
        
        [ForeignKey(nameof(ServiceId))]
        public virtual ConsultationService? ConsultationService { get; set; }
        
        // Navigation properties for attachments and files
        [JsonIgnore]
        public virtual ICollection<AppointmentAttachment> Attachments { get; set; } = new List<AppointmentAttachment>();
        
        [JsonIgnore]
        public virtual ICollection<AppointmentFile> Files { get; set; } = new List<AppointmentFile>();


        public string GetFormattedTime()
        {
            return DateTimeHelper.FormatTime(AppointmentTime);
        }

        public string GetFormattedDate()
        {
            return DateTimeHelper.FormatDate(AppointmentDate);
        }

        public string GetFormattedDateTime()
        {
            return DateTimeHelper.GetFormattedDateTime(AppointmentDate, AppointmentTime);
        }

        public bool IsDateEqual(DateTime date)
        {
            return DateTimeHelper.AreDatesEqual(AppointmentDate, date);
        }

        public bool IsDateBefore(DateTime date)
        {
            return DateTimeHelper.IsDateBefore(AppointmentDate, date);
        }

        public bool IsDateAfter(DateTime date)
        {
            return DateTimeHelper.IsDateAfter(AppointmentDate, date);
        }

        // Helper property for age
        public string Age => AgeValue.ToString();

        
        public DateTime GetAppointmentDateTime()
        {
            return AppointmentDate.Date.Add(AppointmentTime);
        }

        
        public void SetAppointmentDateTime(DateTime dateTime)
        {
            AppointmentDate = dateTime.Date;
            AppointmentTime = dateTime.TimeOfDay;
        }

        
        public bool IsDateGreaterThanOrEqual(DateTime date)
        {
            return DateTimeHelper.IsDateGreaterThanOrEqual(AppointmentDate, date);
        }

        public bool IsDateLessThanOrEqual(DateTime date)
        {
            return DateTimeHelper.IsDateLessThanOrEqual(AppointmentDate, date);
        }

        public bool IsTimeBefore(TimeSpan time)
        {
            return AppointmentTime < time;
        }

        public bool IsTimeAfter(TimeSpan time)
        {
            return AppointmentTime > time;
        }

        public bool IsTimeEqual(TimeSpan time)
        {
            return AppointmentTime == time;
        }
    }
}
