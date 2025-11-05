using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using Barangay.Helpers;

namespace Barangay.Services
{
    public interface IAppointmentSlotService
    {
        Task<SlotAvailabilityResult> GetAvailableSlotsAsync(string doctorId, DateTime date, string consultationType);
        Task<bool> IsSlotAvailableAsync(string doctorId, DateTime date, TimeSpan slotTime);
        Task<int> GetBookedSlotsCountAsync(string doctorId, DateTime date);
        Task<bool> CanBookSlotAsync(string doctorId, DateTime date);
        List<TimeSlotInfo> GenerateTimeSlots(DoctorAvailability availability, DateTime date);
    }

    public class AppointmentSlotService : IAppointmentSlotService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentSlotService> _logger;

        public AppointmentSlotService(
            ApplicationDbContext context,
            ILogger<AppointmentSlotService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get available appointment slots for a doctor on a specific date
        /// </summary>
        public async Task<SlotAvailabilityResult> GetAvailableSlotsAsync(string doctorId, DateTime date, string consultationType)
        {
            try
            {
                // Get doctor availability configuration
                var availability = await _context.DoctorAvailabilities
                    .FirstOrDefaultAsync(da => da.DoctorId == doctorId);

                if (availability == null)
                {
                    _logger.LogWarning($"No availability configuration found for doctor {doctorId}");
                    return new SlotAvailabilityResult
                    {
                        Success = false,
                        Message = "Doctor availability not configured",
                        AvailableSlots = new List<TimeSlotInfo>()
                    };
                }

                // Check if doctor is available on this day
                if (!availability.IsAvailableOnDate(date))
                {
                    var dayName = date.DayOfWeek.ToString();
                    return new SlotAvailabilityResult
                    {
                        Success = false,
                        Message = $"Doctor is not available on {dayName}s",
                        IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday,
                        AvailableSlots = new List<TimeSlotInfo>()
                    };
                }

                // Generate all possible time slots for the day
                var allSlots = GenerateTimeSlots(availability, date);

                // Get booked appointments for this doctor on this date
                var bookedAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId &&
                               a.AppointmentDate.Date == date.Date &&
                               a.Status != AppointmentStatus.Cancelled)
                    .Select(a => a.AppointmentTime)
                    .ToListAsync();

                var bookedCount = bookedAppointments.Count;
                var maxSlots = availability.MaxAppointmentsPerDay;

                // Mark booked slots
                foreach (var slot in allSlots)
                {
                    slot.IsBooked = bookedAppointments.Contains(slot.StartTime);
                }

                // Filter available slots
                var availableSlots = allSlots.Where(s => !s.IsBooked).ToList();

                return new SlotAvailabilityResult
                {
                    Success = true,
                    Message = bookedCount >= maxSlots ? "Fully Booked" : $"{availableSlots.Count} of {maxSlots} slots available",
                    TotalSlots = maxSlots,
                    BookedSlots = bookedCount,
                    AvailableSlots = availableSlots,
                    AllSlots = allSlots,
                    IsFullyBooked = bookedCount >= maxSlots,
                    IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting available slots for doctor {doctorId} on {date}");
                return new SlotAvailabilityResult
                {
                    Success = false,
                    Message = "Error retrieving available slots",
                    AvailableSlots = new List<TimeSlotInfo>()
                };
            }
        }

        /// <summary>
        /// Generate time slots by dividing working hours evenly based on MaxAppointmentsPerDay
        /// </summary>
        public List<TimeSlotInfo> GenerateTimeSlots(DoctorAvailability availability, DateTime date)
        {
            var slots = new List<TimeSlotInfo>();

            // Calculate slot duration if not already calculated
            if (availability.SlotDurationMinutes <= 0)
            {
                availability.CalculateSlotDuration();
            }

            var workingMinutes = (int)(availability.EndTime - availability.StartTime).TotalMinutes;
            var slotDuration = workingMinutes / availability.MaxAppointmentsPerDay;
            
            // Handle leftover minutes by distributing them
            var leftoverMinutes = workingMinutes % availability.MaxAppointmentsPerDay;

            var currentTime = availability.StartTime;
            
            for (int i = 0; i < availability.MaxAppointmentsPerDay; i++)
            {
                // Add an extra minute to first few slots if there are leftover minutes
                var thisSlotDuration = slotDuration + (i < leftoverMinutes ? 1 : 0);
                
                var slotStart = currentTime;
                var slotEnd = currentTime.Add(TimeSpan.FromMinutes(thisSlotDuration));

                // Ensure we don't exceed end time
                if (slotEnd > availability.EndTime)
                {
                    slotEnd = availability.EndTime;
                }

                slots.Add(new TimeSlotInfo
                {
                    SlotNumber = i + 1,
                    StartTime = slotStart,
                    EndTime = slotEnd,
                    FormattedTimeRange = FormatTimeRange(slotStart, slotEnd),
                    IsBooked = false,
                    Date = date
                });

                currentTime = slotEnd;

                // Stop if we've reached the end time
                if (currentTime >= availability.EndTime)
                {
                    break;
                }
            }

            _logger.LogInformation($"Generated {slots.Count} slots for doctor with {workingMinutes} working minutes divided by {availability.MaxAppointmentsPerDay} max appointments");

            return slots;
        }

        /// <summary>
        /// Check if a specific time slot is available
        /// </summary>
        public async Task<bool> IsSlotAvailableAsync(string doctorId, DateTime date, TimeSpan slotTime)
        {
            var existingAppointment = await _context.Appointments
                .AnyAsync(a => a.DoctorId == doctorId &&
                              a.AppointmentDate.Date == date.Date &&
                              a.AppointmentTime == slotTime &&
                              a.Status != AppointmentStatus.Cancelled);

            return !existingAppointment;
        }

        /// <summary>
        /// Get count of booked slots for a doctor on a specific date
        /// </summary>
        public async Task<int> GetBookedSlotsCountAsync(string doctorId, DateTime date)
        {
            return await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                           a.AppointmentDate.Date == date.Date &&
                           a.Status != AppointmentStatus.Cancelled)
                .CountAsync();
        }

        /// <summary>
        /// Check if any slot can be booked (not fully booked)
        /// </summary>
        public async Task<bool> CanBookSlotAsync(string doctorId, DateTime date)
        {
            var availability = await _context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == doctorId);

            if (availability == null || !availability.IsAvailableOnDate(date))
            {
                return false;
            }

            var bookedCount = await GetBookedSlotsCountAsync(doctorId, date);
            return bookedCount < availability.MaxAppointmentsPerDay;
        }

        private string FormatTimeRange(TimeSpan start, TimeSpan end)
        {
            var startTime = DateTime.Today.Add(start).ToString("h:mm tt");
            var endTime = DateTime.Today.Add(end).ToString("h:mm tt");
            return $"{startTime} - {endTime}";
        }
    }

    /// <summary>
    /// Result model for slot availability query
    /// </summary>
    public class SlotAvailabilityResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalSlots { get; set; }
        public int BookedSlots { get; set; }
        public int AvailableCount => TotalSlots - BookedSlots;
        public bool IsFullyBooked { get; set; }
        public bool IsWeekend { get; set; }
        public List<TimeSlotInfo> AvailableSlots { get; set; } = new();
        public List<TimeSlotInfo> AllSlots { get; set; } = new();
    }

    /// <summary>
    /// Information about a single time slot
    /// </summary>
    public class TimeSlotInfo
    {
        public int SlotNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string FormattedTimeRange { get; set; } = string.Empty;
        public bool IsBooked { get; set; }
        public DateTime Date { get; set; }
        public string Status => IsBooked ? "Booked" : "Available";
    }
}

