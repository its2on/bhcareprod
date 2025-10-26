using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Barangay.Attributes;

namespace Barangay.Models
{
    public class Patient
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
        
        [Required]
        [StringLength(1000)] // Increased for encrypted data
        [Encrypted]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }
        
        [Required]
        [StringLength(1000)] // Increased for encrypted data
        [Encrypted]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        [Phone]
        [StringLength(100)] // Increased for encrypted data
        [Encrypted]
        public string ContactNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)] // Increased for encrypted data
        [Encrypted]
        public string EmergencyContact { get; set; } = string.Empty;
        
        [Required]
        [Phone]
        [StringLength(100)] // Increased for encrypted data
        [Encrypted]
        public string EmergencyContactNumber { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(500)] // Increased for encrypted data
        [Encrypted]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(50)]
        [Encrypted]
        public string? FamilyNumber { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; }
        
        [StringLength(20)]
        public string? Room { get; set; }
        
        [StringLength(2000)] // Increased for encrypted data
        [Encrypted]
        public string? Diagnosis { get; set; }
        
        [StringLength(2000)] // Increased for encrypted data
        [Encrypted]
        public string? Alert { get; set; }
        
        public TimeSpan? Time { get; set; }
        
        [StringLength(2000)] // Increased for encrypted data
        [Encrypted]
        public string? Allergies { get; set; }
        
        [Column(TypeName = "text")]
        [Encrypted]
        public string? MedicalHistory { get; set; }
        
        [Column(TypeName = "text")]
        [Encrypted]
        public string? CurrentMedications { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Weight { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Height { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? BloodType { get; set; }

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
    }
}

