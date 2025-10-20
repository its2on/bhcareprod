using System.ComponentModel.DataAnnotations;
using Barangay.Attributes;
using Barangay.Services;

namespace Barangay.Models
{
    public class ImmunizationShortcutForm
    {
        public int Id { get; set; }

        [Required]
        [StringLength(4000)]
        [Encrypted]
        public string ChildName { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        [Encrypted]
        public string MotherName { get; set; } = string.Empty;

        [StringLength(4000)]
        [Encrypted]
        public string FatherName { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        [Encrypted]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        [Encrypted]
        public string Barangay { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(4000)]
        [Encrypted]
        public string Email { get; set; } = string.Empty;

        [StringLength(4000)]
        [Encrypted]
        public string ContactNumber { get; set; } = string.Empty;

        [StringLength(4000)]
        [Encrypted]
        public string PreferredDate { get; set; } = string.Empty;

        [StringLength(4000)]
        [Encrypted]
        public string PreferredTime { get; set; } = string.Empty;

        [StringLength(4000)]
        [Encrypted]
        public string Notes { get; set; } = string.Empty;

        [StringLength(4000)]
        [Encrypted]
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        
        [StringLength(4000)]
        [Encrypted]
        public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        
        [StringLength(4000)]
        [Encrypted]
        public string CreatedBy { get; set; } = string.Empty;
        
        [StringLength(4000)]
        [Encrypted]
        public string Status { get; set; } = "Pending"; // Pending, Scheduled, Completed
    }
}
