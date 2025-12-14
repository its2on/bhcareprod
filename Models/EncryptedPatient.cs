using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Barangay.Services;

namespace Barangay.Models
{
    public class EncryptedPatient
    {
        [Key]
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<VitalSign> VitalSigns { get; set; } = new List<VitalSign>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        
        // Encrypted fields - stored as encrypted in database
        [Required]
        [StringLength(1000)] // Increased length to accommodate encrypted data
        public string EncryptedFullName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)] // Increased length for encrypted gender
        public string EncryptedGender { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }
        
        [Required]
        [StringLength(1000)] // Increased length for encrypted address
        public string EncryptedAddress { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)] // Increased length for encrypted contact
        public string EncryptedContactNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)] // Increased length for encrypted emergency contact
        public string EncryptedEmergencyContact { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)] // Increased length for encrypted emergency contact number
        public string EncryptedEmergencyContactNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)] // Increased length for encrypted email
        public string EncryptedEmail { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? Status { get; set; }
        
        [StringLength(20)]
        public string? Room { get; set; }
        
        [StringLength(2000)] // Increased length for encrypted diagnosis
        public string? EncryptedDiagnosis { get; set; }
        
        [StringLength(2000)] // Increased length for encrypted alert
        public string? EncryptedAlert { get; set; }
        
        public TimeSpan? Time { get; set; }
        
        [StringLength(2000)] // Increased length for encrypted allergies
        public string? EncryptedAllergies { get; set; }
        
        [Column(TypeName = "text")]
        public string? EncryptedMedicalHistory { get; set; }
        
        [Column(TypeName = "text")]
        public string? EncryptedCurrentMedications { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Weight { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Height { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? BloodType { get; set; }

        // Decrypted properties for application use
        [NotMapped]
        public string FullName 
        { 
            get => DecryptField(EncryptedFullName);
            set => EncryptedFullName = EncryptField(value);
        }

        [NotMapped]
        public string Gender 
        { 
            get => DecryptField(EncryptedGender);
            set => EncryptedGender = EncryptField(value);
        }

        [NotMapped]
        public string Address 
        { 
            get => DecryptField(EncryptedAddress);
            set => EncryptedAddress = EncryptField(value);
        }

        [NotMapped]
        public string ContactNumber 
        { 
            get => DecryptField(EncryptedContactNumber);
            set => EncryptedContactNumber = EncryptField(value);
        }

        [NotMapped]
        public string EmergencyContact 
        { 
            get => DecryptField(EncryptedEmergencyContact);
            set => EncryptedEmergencyContact = EncryptField(value);
        }

        [NotMapped]
        public string EmergencyContactNumber 
        { 
            get => DecryptField(EncryptedEmergencyContactNumber);
            set => EncryptedEmergencyContactNumber = EncryptField(value);
        }

        [NotMapped]
        public string Email 
        { 
            get => DecryptField(EncryptedEmail);
            set => EncryptedEmail = EncryptField(value);
        }

        [NotMapped]
        public string? Diagnosis 
        { 
            get => DecryptField(EncryptedDiagnosis);
            set => EncryptedDiagnosis = EncryptField(value);
        }

        [NotMapped]
        public string? Alert 
        { 
            get => DecryptField(EncryptedAlert);
            set => EncryptedAlert = EncryptField(value);
        }

        [NotMapped]
        public string? Allergies 
        { 
            get => DecryptField(EncryptedAllergies);
            set => EncryptedAllergies = EncryptField(value);
        }

        [NotMapped]
        public string? MedicalHistory 
        { 
            get => DecryptField(EncryptedMedicalHistory);
            set => EncryptedMedicalHistory = EncryptField(value);
        }

        [NotMapped]
        public string? CurrentMedications 
        { 
            get => DecryptField(EncryptedCurrentMedications);
            set => EncryptedCurrentMedications = EncryptField(value);
        }

        [NotMapped]
        public int Age
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Year;
                
                if (BirthDate.Date > today.AddYears(-age).Date)
                    age--;
                    
                return age;
            }
        }

        [NotMapped]
        public string Name
        {
            get => FullName;
            set => FullName = value;
        }

        [NotMapped]
        public decimal? BMI
        {
            get
            {
                if (!Height.HasValue || !Weight.HasValue || Height.Value <= 0)
                    return null;
                
                // Convert height from cm to meters
                var heightInMeters = Height.Value / 100;
                return Math.Round(Weight.Value / (heightInMeters * heightInMeters), 2);
            }
        }


        private string EncryptField(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // This will be injected via dependency injection in the actual implementation
            // For now, we'll use a placeholder that will be replaced by the service
            return value; // Placeholder - will be replaced by encryption service
        }

        private string DecryptField(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // This will be injected via dependency injection in the actual implementation
            // For now, we'll use a placeholder that will be replaced by the service
            return value; // Placeholder - will be replaced by decryption service
        }
    }
}
