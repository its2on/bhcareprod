using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Barangay.Extensions;
using Barangay.Helpers;

namespace Barangay.Models
{
    public class AppointmentModel
    {
        [Required(ErrorMessage = "Doctor is required")]
        public string? DoctorId { get; set; }
        public string? UserId { get; set; }
        public string? PatientName { get; set; }
        public int Age { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        public string AppointmentDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "Appointment time is required")]
        public string AppointmentTime { get; set; } = string.Empty;

        public string? ReasonForVisit { get; set; }
        public string? Description { get; set; }
        public string? Attachment { get; set; }

        public bool IsValidDate()
        {
            var date = DateTimeHelper.ParseDate(AppointmentDate);
            return date != DateTime.MinValue;
        }

        public bool IsValidTime()
        {
            return TimeSpan.TryParseExact(AppointmentTime, 
                new[] { "HH:mm", "hh:mm tt" }, 
                System.Globalization.CultureInfo.InvariantCulture, 
                out _);
        }

        public bool IsFutureDate()
        {
            var date = DateTimeHelper.ParseDate(AppointmentDate);
            return date > DateTime.Today;
        }

        public bool IsValidTimeForDate()
        {
            var date = DateTimeHelper.ParseDate(AppointmentDate);
            if (date == DateTime.MinValue) return false;

            if (!TimeSpan.TryParseExact(AppointmentTime, 
                new[] { "HH:mm", "hh:mm tt" }, 
                System.Globalization.CultureInfo.InvariantCulture, 
                out TimeSpan time))
            {
                return false;
            }

            if (DateTimeHelper.IsDateEqual(date, DateTime.Today) && time <= DateTime.Now.TimeOfDay)
            {
                return false;
            }

            return true;
        }

        
        public DateTime GetAppointmentDateTime()
        {
            if (!DateTime.TryParseExact(AppointmentDate, "yyyy-MM-dd", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                throw new FormatException("Invalid date format");
            }

            if (!TimeSpan.TryParseExact(AppointmentTime, "HH:mm", 
                CultureInfo.InvariantCulture, TimeSpanStyles.None, out TimeSpan time))
            {
                throw new FormatException("Invalid time format");
            }

            return date.Date.Add(time);
        }

        
        public string GetFormattedTime()
        {
            if (TimeSpan.TryParseExact(AppointmentTime, "HH:mm", 
                CultureInfo.InvariantCulture, TimeSpanStyles.None, out TimeSpan time))
            {
                return DateTime.Today.Add(time).ToString("h:mm tt");
            }
            return "Not scheduled";
        }

        
        public bool ValidateDateTime()
        {
            if (!DateTime.TryParseExact(AppointmentDate, "yyyy-MM-dd", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return false;
            }

            if (!TimeSpan.TryParseExact(AppointmentTime, "HH:mm", 
                CultureInfo.InvariantCulture, TimeSpanStyles.None, out _))
            {
                return false;
            }

            return true;
        }
    }
}