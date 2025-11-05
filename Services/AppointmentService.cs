using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Identity;
using Barangay.Extensions;
using Barangay.Helpers;

namespace Barangay.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentService(
            ApplicationDbContext context,
            ILogger<AppointmentService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<Appointment?> CreateAppointmentAsync(Appointment appointment)
        {
            try
            {
                // Validate time slot availability
                if (!await IsTimeSlotAvailableAsync(appointment.DoctorId, appointment.AppointmentDate, appointment.AppointmentTime))
                {
                    throw new InvalidOperationException("The selected time slot is not available.");
                }

                // Set timestamps
                appointment.CreatedAt = DateTime.UtcNow;
                appointment.UpdatedAt = DateTime.UtcNow;

                // Add and save
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return appointment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment");
                return null;
            }
        }

        public async Task<bool> IsTimeSlotAvailableAsync(string doctorId, DateTime date, TimeSpan time)
        {
            return !await _context.Appointments
                .AnyAsync(a => a.DoctorId == doctorId &&
                              a.AppointmentDate.Date == date.Date &&
                              a.AppointmentTime == time &&
                              a.Status != AppointmentStatus.Cancelled);
        }

        public async Task<bool> HasConflictingAppointmentAsync(string patientId, DateTime date)
        {
            return await _context.Appointments
                .AnyAsync(a => a.PatientId == patientId &&
                              a.AppointmentDate.Date == date.Date &&
                              a.Status != AppointmentStatus.Cancelled);
        }

        public async Task<bool> IsDoctorAvailableAsync(string doctorId, DateTime date)
        {
            var doctor = await _context.StaffMembers
                .FirstOrDefaultAsync(d => d.UserId == doctorId);

            if (doctor == null)
                return false;

            // Check if the doctor works on this day
            var dayOfWeek = date.DayOfWeek.ToString();
            if (!doctor.WorkingDays.Split(',').Contains(dayOfWeek))
                return false;

            // Check if the doctor hasn't exceeded max daily appointments
            var maxDailyPatients = doctor.MaxDailyPatients > 0 ? doctor.MaxDailyPatients : 8;
            var currentAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId &&
                                a.AppointmentDate.Date == date.Date &&
                                a.Status != AppointmentStatus.Cancelled);

            return currentAppointments < maxDailyPatients;
        }

        public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            return await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public Task<IOrderedQueryable<Appointment>> GetAppointmentsForUserAsync(string userId, bool isDoctor)
        {
            var appointments = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => isDoctor ? a.DoctorId == userId : a.PatientId == userId)
                .OrderByDescending(a => a.CreatedAt);

            return Task.FromResult(appointments);
        }

        public async Task<bool> CheckTimeSlotAvailability(string doctorId, DateTime date, TimeSpan time)
        {
            return !await _context.Appointments
                .AnyAsync(a => a.DoctorId == doctorId &&
                              a.AppointmentDate.Date == date.Date &&
                              a.AppointmentTime == time &&
                              a.Status != AppointmentStatus.Cancelled);
        }
    }
} 