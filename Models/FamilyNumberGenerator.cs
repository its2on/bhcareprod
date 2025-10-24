using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barangay.Models
{
    /// <summary>
    /// Model for managing family number generation with atomic operations
    /// </summary>
    [Table("FamilyNumberCounters")]
    public class FamilyNumberCounter
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(10)]
        public string Prefix { get; set; } = string.Empty;
        
        [Required]
        public int LastNumber { get; set; } = 0;
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Concurrency control
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Request model for family number generation
    /// </summary>
    public class GenerateFamilyNumberRequest
    {
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? HealthFacility { get; set; }
        
        [StringLength(20)]
        public string? PatientCategory { get; set; }
        
        public bool SameFamily { get; set; } = false;
    }

    /// <summary>
    /// Response model for family number generation
    /// </summary>
    public class GenerateFamilyNumberResponse
    {
        public bool Success { get; set; }
        public string FamilyNumber { get; set; } = string.Empty;
        public bool IsPreexisting { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public int SequenceNumber { get; set; }
    }
}

