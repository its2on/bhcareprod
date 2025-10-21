using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Barangay.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

        public NotificationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service is starting.");

            // Wait a bit before first run
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckUpcomingAppointmentsAsync();
                    await CheckUpcomingImmunizationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking notifications.");
                }

                // Wait for the next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Notification Background Service is stopping.");
        }

        private async Task CheckUpcomingAppointmentsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationEmailService = scope.ServiceProvider.GetRequiredService<INotificationEmailService>();
            var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

            try
            {
                var now = DateTime.Now;
                var tomorrow = now.AddDays(1);
                var in24Hours = now.AddHours(24);
                var in48Hours = now.AddHours(48);

                // Get all confirmed appointments in the next 48 hours
                var upcomingAppointments = await context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Where(a => a.Status == AppointmentStatus.Confirmed)
                    .Where(a => a.AppointmentDate >= now.Date && a.AppointmentDate <= in48Hours.Date)
                    .ToListAsync();

                foreach (var appointment in upcomingAppointments)
                {
                    var appointmentDateTime = appointment.GetAppointmentDateTime();
                    var hoursUntilAppointment = (appointmentDateTime - now).TotalHours;

                    // Check if we should send a notification
                    bool shouldNotify = false;
                    int notificationHours = 0;

                    // Send reminder 24 hours before
                    if (hoursUntilAppointment <= 24 && hoursUntilAppointment > 23)
                    {
                        shouldNotify = true;
                        notificationHours = 24;
                    }
                    // Send reminder 2 hours before
                    else if (hoursUntilAppointment <= 2 && hoursUntilAppointment > 1)
                    {
                        shouldNotify = true;
                        notificationHours = 2;
                    }

                    if (shouldNotify)
                    {
                        // Check if we already sent this notification (prevent duplicates)
                        var existingNotification = await context.Notifications
                            .Where(n => n.UserId == appointment.Patient.UserId)
                            .Where(n => n.Title.Contains($"Appointment Reminder - {notificationHours} hours"))
                            .Where(n => n.CreatedAt >= now.AddHours(-2)) // Within last 2 hours
                            .AnyAsync();

                        if (!existingNotification)
                        {
                            await notificationEmailService.SendAppointmentReminderAsync(appointment, notificationHours);
                            _logger.LogInformation($"Sent {notificationHours}-hour reminder for appointment {appointment.Id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking upcoming appointments");
            }
        }

        private async Task CheckUpcomingImmunizationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationEmailService = scope.ServiceProvider.GetRequiredService<INotificationEmailService>();
            var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

            try
            {
                var now = DateTime.Now;
                var activeRecords = await context.ImmunizationRecords
                    .Where(r => r.Status == "Active")
                    .ToListAsync();

                foreach (var record in activeRecords)
                {
                    try
                    {
                        // Parse date of birth
                        var dobString = encryptionService.Decrypt(record.DateOfBirth);
                        if (!DateTime.TryParse(dobString, out DateTime dob))
                            continue;

                        var childAgeInMonths = (int)((now - dob).TotalDays / 30.44);

                        // Check each vaccine and see if it's due soon
                        await CheckVaccineDue(record, "BCG", record.BCGVaccineDate, 0, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "Hepatitis B", record.HepatitisBVaccineDate, 0, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "Pentavalent 1", record.Pentavalent1Date, 6, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "Pentavalent 2", record.Pentavalent2Date, 10, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "Pentavalent 3", record.Pentavalent3Date, 14, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "OPV 1", record.OPV1Date, 6, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "OPV 2", record.OPV2Date, 10, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "OPV 3", record.OPV3Date, 14, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "IPV 1", record.IPV1Date, 14, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "IPV 2", record.IPV2Date, 18, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "PCV 1", record.PCV1Date, 6, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "PCV 2", record.PCV2Date, 10, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "PCV 3", record.PCV3Date, 14, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "MMR 1", record.MMR1Date, 12, notificationEmailService, encryptionService);
                        await CheckVaccineDue(record, "MMR 2", record.MMR2Date, 15, notificationEmailService, encryptionService);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error checking immunization record {record.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking upcoming immunizations");
            }
        }

        private async Task CheckVaccineDue(
            ImmunizationRecord record,
            string vaccineName,
            string vaccineDateEncrypted,
            int recommendedAgeInWeeks,
            INotificationEmailService notificationEmailService,
            IEncryptionService encryptionService)
        {
            try
            {
                // If vaccine already administered, skip
                if (!string.IsNullOrEmpty(vaccineDateEncrypted))
                {
                    var vaccineDate = encryptionService.Decrypt(vaccineDateEncrypted);
                    if (!string.IsNullOrEmpty(vaccineDate) && vaccineDate != "N/A")
                        return;
                }

                // Calculate due date based on date of birth
                var dobString = encryptionService.Decrypt(record.DateOfBirth);
                if (!DateTime.TryParse(dobString, out DateTime dob))
                    return;

                var dueDate = dob.AddDays(recommendedAgeInWeeks * 7);
                var now = DateTime.Now;
                var daysUntilDue = (dueDate - now).TotalDays;

                // Send notification 7 days before due date
                if (daysUntilDue <= 7 && daysUntilDue >= 6)
                {
                    await notificationEmailService.SendImmunizationReminderAsync(record, vaccineName, dueDate);
                    _logger.LogInformation($"Sent immunization reminder for {vaccineName} - Record {record.Id}");
                }
                // Send notification if overdue (1 day after due date)
                else if (daysUntilDue < 0 && daysUntilDue >= -1)
                {
                    await notificationEmailService.SendImmunizationReminderAsync(record, vaccineName, dueDate);
                    _logger.LogInformation($"Sent overdue immunization reminder for {vaccineName} - Record {record.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking vaccine due: {vaccineName}");
            }
        }
    }
}
