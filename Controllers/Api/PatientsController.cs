using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using Barangay.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Barangay.Helpers;

namespace Barangay.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(ApplicationDbContext context, ILogger<PatientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string term)
        {
            if (string.IsNullOrEmpty(term))
                return Ok(new { patients = new List<object>() });

            try
            {
                // Get list of staff user IDs to exclude
                var staffUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == _context.Roles.Where(r => r.Name == "Doctor").Select(r => r.Id).FirstOrDefault()
                        || ur.RoleId == _context.Roles.Where(r => r.Name == "Nurse").Select(r => r.Id).FirstOrDefault()
                        || ur.RoleId == _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefault()
                        || ur.RoleId == _context.Roles.Where(r => r.Name == "Admin Staff").Select(r => r.Id).FirstOrDefault())
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                var appointmentPatients = await _context.Appointments
                    .Where(a => a.PatientName.Contains(term) 
                        && DateTimeHelper.IsDateGreaterThanOrEqual(a.AppointmentDate, DateTime.Today)
                        && a.PatientName != "System Administrator" 
                        && a.PatientId != "0e03f06e-ba88-46ed-b047-4974d8b8252a"
                        && !staffUserIds.Contains(a.PatientId))
                    .Select(a => new
                    {
                        a.PatientId,
                        a.PatientName,
                        AppointmentDate = DateTimeHelper.ToDateString(a.AppointmentDate),
                        AppointmentTime = a.AppointmentTime.FormatTime(),
                        a.Status
                    })
                    .ToListAsync();

                _logger.LogInformation($"Found {appointmentPatients.Count} patients in appointments matching '{term}'");

                var patients = await _context.Patients
                    .Where(p => (p.Name.Contains(term) || p.ContactNumber.Contains(term))
                        && p.Name != "System Administrator" 
                        && p.UserId != "0e03f06e-ba88-46ed-b047-4974d8b8252a"
                        && !staffUserIds.Contains(p.UserId))
                    .Select(p => new
                    {
                        p.UserId,
                        p.Name,
                        p.ContactNumber,
                        p.Gender,
                        p.BirthDate,
                        p.Address,
                        p.Status
                    })
                    .ToListAsync();

                return Ok(new { appointmentPatients, patients });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients with term: {Term}", term);
                return StatusCode(500, new { error = "Error searching patients" });
            }
        }

        [HttpGet("{id}/appointments")]
        public async Task<IActionResult> GetPatientAppointments(string id)
        {
            try
            {
                var appointments = await _context.Appointments
                    .Where(a => a.PatientId == id)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .Select(a => new
                    {
                        a.Id,
                        Date = DateTimeHelper.ToDateString(a.AppointmentDate),
                        Time = a.AppointmentTime.FormatTime(),
                        a.Status,
                        a.Description,
                        DoctorName = a.Doctor.FullName
                    })
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointments for patient: {PatientId}", id);
                return StatusCode(500, new { error = "Error retrieving appointments" });
            }
        }
    }
}