using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barangay.Models
{
    public class DoctorAvailability
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DoctorId { get; set; }

        [Required]
        public bool IsAvailable { get; set; }

        [Required]
        public bool Monday { get; set; } = true;
        
        [Required]
        public bool Tuesday { get; set; } = true;
        
        [Required]
        public bool Wednesday { get; set; } = true;
        
        [Required]
        public bool Thursday { get; set; } = true;
        
        [Required]
        public bool Friday { get; set; } = true;
        
    [Required]
    public bool Saturday { get; set; } = false;  // DISABLE WEEKENDS BY DEFAULT
    
    [Required]
    public bool Sunday { get; set; } = false;    // DISABLE WEEKENDS BY DEFAULT

    [Required]
    public TimeSpan StartTime { get; set; } = new TimeSpan(8, 0, 0); // 8:00 AM

    [Required]
    public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0); // 5:00 PM

    /// <summary>
    /// Maximum number of appointment slots available per day
    /// System will divide working hours evenly by this number
    /// </summary>
    [Required]
    [Range(1, 200, ErrorMessage = "Max appointments per day must be between 1 and 200")]
    public int MaxAppointmentsPerDay { get; set; } = 100;

    /// <summary>
    /// Slot duration in minutes (calculated: working minutes / MaxAppointmentsPerDay)
    /// 8:00 AM to 5:00 PM = 540 minutes / 100 slots = 5.4 minutes per slot
    /// </summary>
    public int SlotDurationMinutes { get; set; } = 5;

    [Required]
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    [ForeignKey("DoctorId")]
    public virtual ApplicationUser Doctor { get; set; }

    /// <summary>
    /// Calculate slot duration based on working hours and max appointments per day
    /// </summary>
    public void CalculateSlotDuration()
    {
        var workingMinutes = (int)(EndTime - StartTime).TotalMinutes;
        SlotDurationMinutes = workingMinutes / MaxAppointmentsPerDay;
    }

    /// <summary>
    /// Check if doctor is available on a specific date
    /// </summary>
    public bool IsAvailableOnDate(DateTime date)
    {
        if (!IsAvailable) return false;

        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => Monday,
            DayOfWeek.Tuesday => Tuesday,
            DayOfWeek.Wednesday => Wednesday,
            DayOfWeek.Thursday => Thursday,
            DayOfWeek.Friday => Friday,
            DayOfWeek.Saturday => Saturday,
            DayOfWeek.Sunday => Sunday,
            _ => false
        };
    }
}
} 