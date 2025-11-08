using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Barangay.Attributes;

namespace Barangay.Models
{
    public class ApplicationUser : IdentityUser
    {
        // User-friendly identifier (auto-incremented)
        [Display(Name = "User ID")]
        public int UserNumber { get; set; }
        
        // Calculate a numeric ID from the GUID if UserNumber is not available
        [NotMapped]
        public int FallbackUserNumber
        {
            get
            {
                if (UserNumber > 0)
                    return UserNumber;
                    
                // If UserNumber column doesn't exist or is 0, calculate a positive number from the GUID
                if (!string.IsNullOrEmpty(Id))
                {
                    try
                    {
                        // Take first 4 bytes of GUID and convert to a positive integer
                        byte[] guidBytes = Guid.Parse(Id).ToByteArray();
                        int hash = BitConverter.ToInt32(guidBytes, 0);
                        return Math.Abs(hash % 1000000); // Use modulo to keep within 6 digits
                    }
                    catch
                    {
                        // If parsing fails, return a hash code
                        return Math.Abs(Id.GetHashCode() % 1000000);
                    }
                }
                
                return 0;
            }
        }
        
        // Helper property to get the effective UserNumber regardless of column existence
        [NotMapped]
        public int EffectiveUserNumber => UserNumber > 0 ? UserNumber : FallbackUserNumber;
        
        [Encrypted]
        public string Name { get; set; } = string.Empty;
        public string EncryptedStatus { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string EncryptedFullName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
        public string WorkingDays { get; set; } = string.Empty;
        public string WorkingHours { get; set; } = string.Empty;
        public int MaxDailyPatients { get; set; } = 20;
        public DateTime? BirthDate { get; set; }
        [Encrypted]
        public string Gender { get; set; } = string.Empty;
        [Encrypted]
        public string Age { get; set; } = string.Empty;
        [Encrypted]
        public string Address { get; set; } = string.Empty;
        // Specific barangay selection (e.g., "Barangay 158")
        [Encrypted]
        public string Barangay { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string ProfilePicture { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
        [Encrypted]
        public string PhilHealthId { get; set; } = string.Empty;
        public DateTime LastActive { get; set; } = DateTime.Now;
        public DateTime JoinDate { get; set; } = DateTime.Now;
        public UserType UserType { get; set; } = UserType.Patient;
        
        // Terms agreement tracking
        public bool HasAgreedToTerms { get; set; } = false;
        public DateTime? AgreedAt { get; set; }
        
        // First login tracking for staff accounts
        public bool IsFirstLogin { get; set; } = false;
        
        // Password change tracking - true if user has ever changed their password from default
        public bool HasChangedPassword { get; set; } = false;
        public DateTime? LastPasswordChangeDate { get; set; }
        
        // Notification settings
        public bool AppointmentReminders { get; set; } = true;
        public bool PrescriptionAlerts { get; set; } = true;
        public bool HealthTips { get; set; } = false;
        
        // Modified properties to be nullable
        [Encrypted]
        public string FirstName { get; set; } = string.Empty;
        [Encrypted]
        public string MiddleName { get; set; } = string.Empty;
        [Encrypted]
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }
        public string? Occupation { get; set; }
        public string? CivilStatus { get; set; }
        public string? Religion { get; set; }
        
        // Family Number for patient grouping (format: X-001 where X is first letter of last name)
        [StringLength(20)]
        [Encrypted]
        public string? FamilyNumber { get; set; }

        // For backward compatibility - removed NotMapped attribute to match database column
        public string FullName 
        { 
            get => $"{FirstName ?? ""} {MiddleName ?? ""} {LastName ?? ""}".Trim();
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    FirstName = string.Empty;
                    MiddleName = string.Empty;
                    LastName = string.Empty;
                    Name = string.Empty;
                    return;
                }

                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    FirstName = parts[0];
                    MiddleName = parts[1];
                    LastName = string.Join(" ", parts.Skip(2));
                }
                else if (parts.Length == 2)
                {
                    FirstName = parts[0];
                    LastName = parts[1];
                    MiddleName = string.Empty;
                }
                else if (parts.Length == 1)
                {
                    FirstName = parts[0];
                    MiddleName = string.Empty;
                    LastName = string.Empty;
                }
                Name = value;
            }
        }
        
        /// <summary>
        /// Returns a formatted version of the GUID-based ID
        /// </summary>
        [NotMapped]
        public string FormattedId 
        {
            get
            {
                if (string.IsNullOrEmpty(Id))
                    return string.Empty;
                    
                try
                {
                    // Always use the EffectiveUserNumber property which handles both cases
                    return $"BHC-{EffectiveUserNumber:D6}";
                }
                catch
                {
                    // If there's an error accessing properties, fall back to GUID
                    return $"BHC-{Id.Substring(0, 6)}";
                }
            }
        }

        // Navigation properties for appointments
        [InverseProperty("Doctor")]
        [JsonIgnore]
        public virtual ICollection<Appointment> DoctorAppointments { get; set; } = new List<Appointment>();
        
        // Navigation properties for medical records - only keep Doctor relationship
        [InverseProperty("Doctor")]
        [JsonIgnore]
        public virtual ICollection<MedicalRecord> DoctorMedicalRecords { get; set; } = new List<MedicalRecord>();
        
        // Navigation properties for prescriptions - only keep Doctor relationship
        [InverseProperty("Doctor")]
        [JsonIgnore]
        public virtual ICollection<Prescription> DoctorPrescriptions { get; set; } = new List<Prescription>();

        // Navigation property for user documents
        [InverseProperty(nameof(UserDocument.User))]
        [JsonIgnore]
        public virtual ICollection<UserDocument> UserDocuments { get; set; } = new List<UserDocument>();
        
        // Navigation property for approved documents
        [InverseProperty(nameof(UserDocument.Approver))]
        [JsonIgnore]
        public virtual ICollection<UserDocument> ApprovedDocuments { get; set; } = new List<UserDocument>();

        // Navigation property for user permissions
        [JsonIgnore]
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        
        // Navigation property for guardian information (for users under 18)
        [InverseProperty(nameof(GuardianInformation.User))]
        [JsonIgnore]
        public virtual ICollection<GuardianInformation> GuardianConsents { get; set; } = new List<GuardianInformation>();
        
        // Automatic Residency Verification (OCR-based)
        public string? VerificationStatus { get; set; } = "Pending Review"; // Pending Review, Auto Verified, Manual Verified, Rejected
        public bool IsApproved { get; set; } = false;
        public string? ApprovedBy { get; set; } // "System (Auto)" or Admin UserID
        public DateTime? ApprovedDate { get; set; }
        public string? VerifiedBarangay { get; set; } // Extracted from OCR (158, 159, 160, or 161)
        public string? OcrExtractedText { get; set; } // Full OCR text for audit
        public DateTime? DocumentVerifiedAt { get; set; }
    }
}


