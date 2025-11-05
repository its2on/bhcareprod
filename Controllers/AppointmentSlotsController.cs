using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Barangay.Services;
using Barangay.Helpers;
using System;
using System.Threading.Tasks;
using System.Linq;
using Barangay.Data;
using Microsoft.EntityFrameworkCore;

namespace Barangay.Controllers
{
    [ApiController]
    [Route("api/appointment-slots")]
    [Authorize]
    public class AppointmentSlotsController : ControllerBase
    {
        private readonly IAppointmentSlotService _slotService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentSlotsController> _logger;

        public AppointmentSlotsController(
            IAppointmentSlotService slotService,
            ApplicationDbContext context,
            ILogger<AppointmentSlotsController> logger)
        {
            _slotService = slotService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get available appointment slots for a specific doctor and date
        /// </summary>
        /// <param name="doctorId">Doctor ID</param>
        /// <param name="date">Date in yyyy-MM-dd format</param>
        /// <param name="consultationType">Consultation type (optional)</param>
        /// <returns>Available time slots</returns>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableSlots(
            [FromQuery] string doctorId,
            [FromQuery] string date,
            [FromQuery] string? consultationType = null)
        {
            try
            {
                if (string.IsNullOrEmpty(doctorId))
                {
                    return BadRequest(new { success = false, message = "Doctor ID is required" });
                }

                if (string.IsNullOrEmpty(date))
                {
                    return BadRequest(new { success = false, message = "Date is required" });
                }

                var parsedDate = DateTimeHelper.ParseDate(date);
                if (parsedDate == DateTime.MinValue)
                {
                    return BadRequest(new { success = false, message = "Invalid date format. Use yyyy-MM-dd" });
                }

                // Check if date is in the past
                if (parsedDate.Date < DateTime.Today)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Cannot book appointments in the past",
                        totalSlots = 0,
                        bookedSlots = 0,
                        availableSlots = 0,
                        isFullyBooked = true,
                        slots = Array.Empty<object>()
                    });
                }

                var result = await _slotService.GetAvailableSlotsAsync(doctorId, parsedDate, consultationType ?? string.Empty);

                if (!result.Success)
                {
                    if (result.IsWeekend && !await IsWeekendEnabled(doctorId))
                    {
                        return Ok(new
                        {
                            success = false,
                            message = "Doctor is not available on weekends",
                            isWeekend = true,
                            totalSlots = 0,
                            bookedSlots = 0,
                            availableSlots = 0,
                            slots = Array.Empty<object>()
                        });
                    }

                    return Ok(new
                    {
                        success = false,
                        message = result.Message,
                        totalSlots = 0,
                        bookedSlots = 0,
                        availableSlots = 0,
                        slots = Array.Empty<object>()
                    });
                }

                var response = new
                {
                    success = true,
                    message = result.Message,
                    totalSlots = result.TotalSlots,
                    bookedSlots = result.BookedSlots,
                    availableSlots = result.AvailableCount,
                    isFullyBooked = result.IsFullyBooked,
                    isWeekend = result.IsWeekend,
                    slots = result.AllSlots.Select(s => new
                    {
                        slotNumber = s.SlotNumber,
                        startTime = s.StartTime.ToString(@"hh\:mm"),
                        endTime = s.EndTime.ToString(@"hh\:mm"),
                        timeRange = s.FormattedTimeRange,
                        isBooked = s.IsBooked,
                        status = s.Status,
                        isAvailable = !s.IsBooked
                    }).ToList()
                };

                _logger.LogInformation($"Retrieved {result.AvailableCount} available slots for doctor {doctorId} on {date}");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting available slots: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving available slots"
                });
            }
        }

        /// <summary>
        /// Check if a specific time slot is available
        /// </summary>
        [HttpGet("check-availability")]
        public async Task<IActionResult> CheckSlotAvailability(
            [FromQuery] string doctorId,
            [FromQuery] string date,
            [FromQuery] string time)
        {
            try
            {
                if (string.IsNullOrEmpty(doctorId) || string.IsNullOrEmpty(date) || string.IsNullOrEmpty(time))
                {
                    return BadRequest(new { success = false, message = "Doctor ID, date, and time are required" });
                }

                var parsedDate = DateTimeHelper.ParseDate(date);
                if (parsedDate == DateTime.MinValue)
                {
                    return BadRequest(new { success = false, message = "Invalid date format" });
                }

                var parsedTime = DateTimeHelper.ParseTime(time);
                if (parsedTime == TimeSpan.Zero)
                {
                    return BadRequest(new { success = false, message = "Invalid time format" });
                }

                var isAvailable = await _slotService.IsSlotAvailableAsync(doctorId, parsedDate, parsedTime);
                var canBook = await _slotService.CanBookSlotAsync(doctorId, parsedDate);

                return Ok(new
                {
                    success = true,
                    isAvailable,
                    canBook,
                    message = !canBook ? "All slots are fully booked for this date" :
                             !isAvailable ? "This time slot is already booked" :
                             "Slot is available"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking slot availability");
                return StatusCode(500, new { success = false, message = "Error checking slot availability" });
            }
        }

        /// <summary>
        /// Get slot statistics for a doctor on a specific date
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetSlotStatistics(
            [FromQuery] string doctorId,
            [FromQuery] string date)
        {
            try
            {
                if (string.IsNullOrEmpty(doctorId) || string.IsNullOrEmpty(date))
                {
                    return BadRequest(new { success = false, message = "Doctor ID and date are required" });
                }

                var parsedDate = DateTimeHelper.ParseDate(date);
                if (parsedDate == DateTime.MinValue)
                {
                    return BadRequest(new { success = false, message = "Invalid date format" });
                }

                var availability = await _context.DoctorAvailabilities
                    .FirstOrDefaultAsync(da => da.DoctorId == doctorId);

                if (availability == null)
                {
                    return NotFound(new { success = false, message = "Doctor availability not found" });
                }

                var bookedCount = await _slotService.GetBookedSlotsCountAsync(doctorId, parsedDate);
                var maxSlots = availability.MaxAppointmentsPerDay;
                var availableCount = maxSlots - bookedCount;

                return Ok(new
                {
                    success = true,
                    totalSlots = maxSlots,
                    bookedSlots = bookedCount,
                    availableSlots = availableCount,
                    isFullyBooked = bookedCount >= maxSlots,
                    utilizationPercentage = maxSlots > 0 ? (bookedCount * 100.0 / maxSlots) : 0,
                    workingHours = new
                    {
                        start = availability.StartTime.ToString(@"hh\:mm"),
                        end = availability.EndTime.ToString(@"hh\:mm")
                    },
                    slotDuration = availability.SlotDurationMinutes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slot statistics");
                return StatusCode(500, new { success = false, message = "Error retrieving statistics" });
            }
        }

        private async Task<bool> IsWeekendEnabled(string doctorId)
        {
            var availability = await _context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == doctorId);

            return availability != null && (availability.Saturday || availability.Sunday);
        }
    }
}

