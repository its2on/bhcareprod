using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Extensions;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Barangay.Helpers;
using Barangay.Services;

namespace Barangay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReportsApiController> _logger;
        private readonly IPermissionService _permissionService;

        public ReportsApiController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<ReportsApiController> logger,
            IPermissionService permissionService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _permissionService = permissionService;
        }

        [HttpGet]
        [Route("data")]
        public async Task<IActionResult> GetReportData([FromQuery] string reportType = "all", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Check if user has permission to access reports
                bool canAccessReports = await _permissionService.UserHasPermissionAsync(User, "Access Reports") ||
                                      User.IsInRole("Admin") ||
                                      User.IsInRole("Doctor");
                                      
                if (!canAccessReports)
                {
                    _logger.LogWarning($"User {User.Identity?.Name} attempted to access reports without permission");
                    return Forbid();
                }
                
                _logger.LogInformation($"Generating report of type {reportType} from {startDate} to {endDate}");
                
                var now = DateTime.Now;
                startDate ??= now.AddDays(-30);
                endDate ??= now;

                // Statistics within the date range
                var totalConsultations = 0;
                var totalPrescriptions = 0;
                var newPatients = 0;
                var totalAppointments = 0;
                var avgConsultationTime = 0.0;
                var satisfactionRate = 0;

                try
                {
                    totalConsultations = await _context.MedicalRecords
                        .Where(m => m.Date >= startDate && m.Date <= endDate)
                        .CountAsync();
                    _logger.LogInformation($"Total consultations: {totalConsultations}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting consultations");
                }

                try
                {
                    totalPrescriptions = await _context.Prescriptions
                        .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                        .CountAsync();
                    _logger.LogInformation($"Total prescriptions: {totalPrescriptions}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting prescriptions");
                }

                try
                {
                    newPatients = await _context.Patients
                        .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                        .CountAsync();
                    _logger.LogInformation($"New patients: {newPatients}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting new patients");
                }

                try
                {
                    totalAppointments = await _context.Appointments
                        .Where(a => DateTimeHelper.IsDateGreaterThanOrEqual(a.AppointmentDate, startDate.Value) && DateTimeHelper.IsDateLessThanOrEqual(a.AppointmentDate, endDate.Value))
                        .CountAsync();
                    _logger.LogInformation($"Total appointments: {totalAppointments}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting appointments");
                }

                try
                {
                    avgConsultationTime = await _context.MedicalRecords
                        .Where(m => m.Date >= startDate && m.Date <= endDate && !string.IsNullOrEmpty(m.Duration))
                        .Select(m => int.Parse(m.Duration))
                        .DefaultIfEmpty(0)
                        .AverageAsync();
                    _logger.LogInformation($"Average consultation time: {avgConsultationTime}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating average consultation time");
                }

                var totalDoctors = 0;
                var totalNurses = 0;

                try
                {
                    totalDoctors = (await _userManager.GetUsersInRoleAsync("Doctor")).Count;
                    totalNurses = (await _userManager.GetUsersInRoleAsync("Nurse")).Count;
                    _logger.LogInformation($"Total doctors: {totalDoctors}, Total nurses: {totalNurses}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting staff members");
                }

                try
                {
                    var feedbacks = await _context.Feedbacks
                        .Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate)
                        .ToListAsync();

                    satisfactionRate = feedbacks.Any()
                        ? (int)Math.Round(feedbacks.Average(f => f.Rating) * 20) // Convert 1-5 scale to percentage
                        : 0;
                    _logger.LogInformation($"Satisfaction rate: {satisfactionRate}%");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating satisfaction rate");
                }

                var statistics = new
                {
                    consultations = totalConsultations,
                    newPatients = newPatients,
                    prescriptions = totalPrescriptions,
                    appointments = totalAppointments,
                    doctors = totalDoctors,
                    nurses = totalNurses,
                    avgConsultationTime = (int)Math.Round(avgConsultationTime),
                    satisfactionRate = satisfactionRate
                };

                // Consultation Trends (group by date)
                var consultationData = new List<object>();
                try
                {
                    var consultationTrends = await _context.MedicalRecords
                        .Where(m => m.Date >= startDate && m.Date <= endDate)
                        .GroupBy(m => m.Date.Date)
                        .Select(g => new { 
                            date = g.Key,
                            count = g.Count() 
                        })
                        .OrderBy(g => g.date)
                        .ToListAsync();

                    consultationData = consultationTrends.Select(t => new
                    {
                        label = t.date.ToString("MMM dd"),
                        value = t.count
                    }).ToList<object>();
                    _logger.LogInformation($"Retrieved {consultationTrends.Count} consultation trend records");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving consultation trends");
                }

                // Patient Demographics
                var demographics = new Dictionary<string, int>();
                try
                {
                    demographics = await _context.Patients
                        .Where(p => p.Gender != null)
                        .GroupBy(p => p.Gender)
                        .Select(g => new { gender = g.Key, count = g.Count() })
                        .ToDictionaryAsync(g => g.gender ?? "Other", g => g.count);

                    // Ensure all gender categories exist
                    var genderCategories = new[] { "Male", "Female", "Other" };
                    foreach (var gender in genderCategories)
                    {
                        if (!demographics.ContainsKey(gender))
                        {
                            demographics[gender] = 0;
                        }
                    }
                    _logger.LogInformation($"Retrieved demographics data with {demographics.Count} categories");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving patient demographics");
                    demographics = new Dictionary<string, int>
                    {
                        { "Male", 0 },
                        { "Female", 0 },
                        { "Other", 0 }
                    };
                }

                // Get top conditions from medical records
                var topConditions = new List<object>();
                try
                {
                    var conditions = await _context.MedicalRecords
                        .Where(m => m.Date >= startDate && m.Date <= endDate && !string.IsNullOrEmpty(m.Diagnosis))
                        .ToListAsync();
                    
                    // Decrypt and normalize conditions
                    var normalizedConditions = conditions
                        .GroupBy(m => ConditionNormalizer.NormalizeCondition(m.Diagnosis))
                        .Select(g => new { 
                            condition = g.Key, 
                            count = g.Count() 
                        })
                        .OrderByDescending(g => g.count)
                        .Take(10)
                        .ToList();

                    topConditions = normalizedConditions.Select(c => new { condition = c.condition, count = c.count }).Cast<object>().ToList();
                    _logger.LogInformation($"Retrieved {topConditions.Count} top conditions");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving top conditions");
                }

                // Get staff performance
                var staffPerformance = new List<object>();
                try
                {
                    var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
                    foreach (var doctor in doctors)
                    {
                        var consultations = await _context.MedicalRecords
                            .CountAsync(m => m.DoctorId == doctor.Id && m.Date >= startDate && m.Date <= endDate);
                        
                        var prescriptions = await _context.Prescriptions
                            .CountAsync(p => p.DoctorId == doctor.Id && p.CreatedAt >= startDate && p.CreatedAt <= endDate);
                        
                        staffPerformance.Add(new {
                            name = doctor.FullName ?? doctor.UserName,
                            role = "Doctor",
                            consultations,
                            prescriptions
                        });
                    }
                    _logger.LogInformation($"Retrieved performance data for {doctors.Count} doctors");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving staff performance");
                }

                var result = new
                {
                    statistics,
                    chartData = new
                    {
                        consultationData,
                        demographicsData = demographics
                    },
                    topConditions,
                    staffPerformance
                };

                _logger.LogInformation("Successfully generated report data");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report data");
                return StatusCode(500, new { error = "An error occurred while generating the report", details = ex.Message });
            }
        }

    }
} 