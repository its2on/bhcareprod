using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Barangay.Attributes;

namespace Barangay.Models
{
    public class VitalSign
    {
        [Key]
        public int Id { get; set; }
        
        [StringLength(450)]
        public string? PatientId { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? Temperature { get; set; }
        
        [StringLength(20)]
        public string? BloodPressure { get; set; }
        
        [StringLength(50)]
        public string? HeartRate { get; set; }
        
        [StringLength(50)]
        public string? RespiratoryRate { get; set; }
        
        [StringLength(50)]
        public string? SpO2 { get; set; }
        
        [StringLength(50)]
        public string? Weight { get; set; }
        
        [StringLength(50)]
        public string? Height { get; set; }
        
        // Encrypted versions of the above fields
        [StringLength(1000)]
        [Encrypted]
        public string? EncryptedTemperature { get; set; }
        
        [StringLength(1000)]
        [Encrypted]
        public string? EncryptedBloodPressure { get; set; }
        
        [StringLength(1000)]
        [Encrypted]
        public string? EncryptedHeartRate { get; set; }
        
        [StringLength(1000)]
        [Encrypted]
        public string? EncryptedRespiratoryRate { get; set; }
        
        [StringLength(1000)]
        [Encrypted]
        public string? EncryptedSpO2 { get; set; }
        
        [StringLength(1000)]
        [Encrypted]
        public string? EncryptedWeight { get; set; }
        
        [StringLength(1000)]
        [Encrypted]
        public string? EncryptedHeight { get; set; }
        
        public DateTime RecordedAt { get; set; } = DateTime.Now;
        
        [StringLength(450)]
        public string? RecordedBy { get; set; } = string.Empty;
        
        [StringLength(200)]
        [Encrypted]
        public string? RecordedByName { get; set; }
        
        [StringLength(1000)]
        [Encrypted]
        public string? Notes { get; set; }
        
        // Navigation property
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        
        [NotMapped]
        public string PatientName 
        { 
            get 
            {
                return Patient?.FullName ?? "Unknown Patient"; 
            }
        }
    }
}