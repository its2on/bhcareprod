using System;
using System.ComponentModel.DataAnnotations;

namespace Barangay.Models
{
    public class Assessment
    {
        [Key]
        public int Id { get; set; }
        
        [Display(Name = "Family Number")]
        public string? FamilyNumber { get; set; }
        
        [Display(Name = "Reason for Consultation")]
        public string? ReasonForVisit { get; set; }
        
        [Display(Name = "Symptoms")]
        public string? Symptoms { get; set; }
        
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 