using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barangay.Models
{
    /// <summary>
    /// Represents an option for select, radio, or checkbox fields
    /// </summary>
    public class FormFieldOption
    {
        [Key]
        public int FormFieldOptionId { get; set; }

        [Required]
        public int FormFieldId { get; set; }

        [Required]
        [MaxLength(500)]
        public string OptionLabel { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string OptionValue { get; set; } = string.Empty;

        /// <summary>
        /// Display order of the option
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Indicates if this option is selected by default
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Indicates if this option is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional icon class for the option
        /// </summary>
        [MaxLength(100)]
        public string? IconClass { get; set; }

        /// <summary>
        /// Optional group/category for the option
        /// </summary>
        [MaxLength(200)]
        public string? GroupName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("FormFieldId")]
        public virtual FormField FormField { get; set; } = null!;
    }
}
