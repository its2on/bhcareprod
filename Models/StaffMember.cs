using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barangay.Models
{
    public class StaffMember
    {
        [Key]
        public int Id { get; set; }
        
        [Required(AllowEmptyStrings = true)]
        public string UserId { get; set; } = string.Empty;
        
        // Separate name fields
        [Required]
        [Display(Name = "First Name")]
        [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ'\-\s]+$", ErrorMessage = "First name may only contain letters, spaces, hyphen (-), and apostrophe (')")]
        public string FirstName { get; set; } = string.Empty;
        
        [Display(Name = "Middle Name")]
        [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ'\-\s]*$", ErrorMessage = "Middle name may only contain letters, spaces, hyphen (-), and apostrophe (')")]
        public string? MiddleName { get; set; }
        
        [Required]
        [Display(Name = "Last Name")]
        [RegularExpression(@"^[A-Za-zÀ-ÖØ-öø-ÿ'\-\s]+$", ErrorMessage = "Last name may only contain letters, spaces, hyphen (-), and apostrophe (')")]
        public string LastName { get; set; } = string.Empty;
        
        // Computed full name for backward compatibility
        [Display(Name = "Full Name")]
        [NotMapped]
        public string Name 
        { 
            get => string.IsNullOrEmpty(MiddleName) 
                ? $"{FirstName} {LastName}" 
                : $"{FirstName} {MiddleName} {LastName}";
            set { } // Setter for model binding compatibility
        }
        
        [Required]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }
        
        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Civil Status")]
        public string CivilStatus { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public string? Department { get; set; }
        
        [Required]
        public string? Position { get; set; }
        
        public string? Specialization { get; set; }
        
        public string? LicenseNumber { get; set; }
        
        [Required]
        [Phone]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }
        
        [Required]
        [Display(Name = "Working Days")]
        public string? WorkingDays { get; set; }
        
        [Required]
        [Display(Name = "Working Hours")]
        public string? WorkingHours { get; set; }
        
        public DateTime JoinDate { get; set; } = DateTime.Now;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public int MaxDailyPatients { get; set; } = 20;
        
        public bool IsActive { get; set; } = true;
        
        [Required]
        public string Role { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<StaffPermission> StaffPermissions { get; set; } = new List<StaffPermission>();
    }
}
