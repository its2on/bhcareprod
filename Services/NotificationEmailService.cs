using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Barangay.Services
{
    public interface INotificationEmailService
    {
        Task SendAppointmentReminderAsync(Appointment appointment, int hoursBeforeAppointment);
        Task SendImmunizationReminderAsync(ImmunizationRecord immunizationRecord, string vaccineName, DateTime dueDate);
        Task SendAppointmentConfirmationAsync(Appointment appointment);
        Task SendAppointmentCancellationAsync(Appointment appointment);
        Task SendAppointmentRescheduledAsync(Appointment appointment);
    }

    public class NotificationEmailService : INotificationEmailService
    {
        private readonly IEmailSender _emailSender;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationEmailService> _logger;
        private readonly IEncryptionService _encryptionService;

        public NotificationEmailService(
            IEmailSender emailSender,
            INotificationService notificationService,
            ApplicationDbContext context,
            ILogger<NotificationEmailService> logger,
            IEncryptionService encryptionService)
        {
            _emailSender = emailSender;
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        public async Task SendAppointmentReminderAsync(Appointment appointment, int hoursBeforeAppointment)
        {
            try
            {
                // Get patient information
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == appointment.PatientId);

                if (patient == null || patient.User == null)
                {
                    _logger.LogWarning($"Patient not found for appointment {appointment.Id}");
                    return;
                }

                var patientEmail = _encryptionService.Decrypt(patient.User.Email);
                var patientName = _encryptionService.Decrypt(appointment.PatientName);
                var appointmentDateTime = appointment.GetAppointmentDateTime();
                var formattedDate = appointmentDateTime.ToString("MMMM dd, yyyy");
                var formattedTime = appointmentDateTime.ToString("hh:mm tt");

                // Create in-app notification
                var title = $"Appointment Reminder - {hoursBeforeAppointment} hours";
                var message = $"Your appointment is scheduled for {formattedDate} at {formattedTime}. Please arrive 15 minutes early.";
                
                await _notificationService.CreateNotificationForUserAsync(
                    patient.UserId,
                    title,
                    message,
                    "Warning",
                    "/User/Appointments"
                );

                // Send email notification
                var emailSubject = "BHCARE - Appointment Reminder";
                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='background-color: #FF8C42; padding: 20px; color: white;'>
                            <h2>Appointment Reminder</h2>
                        </div>
                        <div style='padding: 20px;'>
                            <p>Dear {patientName},</p>
                            <p>This is a friendly reminder about your upcoming appointment:</p>
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #FF8C42; margin: 20px 0;'>
                                <p><strong>Date:</strong> {formattedDate}</p>
                                <p><strong>Time:</strong> {formattedTime}</p>
                                <p><strong>Reason:</strong> {_encryptionService.Decrypt(appointment.ReasonForVisit)}</p>
                            </div>
                            <p><strong>Important:</strong> Please arrive 15 minutes early to complete any necessary paperwork.</p>
                            <p>If you need to cancel or reschedule, please contact us as soon as possible.</p>
                            <p>Best regards,<br>BHCARE Team</p>
                        </div>
                    </body>
                    </html>
                ";

                await _emailSender.SendEmailAsync(patientEmail, emailSubject, emailBody);
                _logger.LogInformation($"Appointment reminder sent for appointment {appointment.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending appointment reminder for appointment {appointment.Id}");
            }
        }

        public async Task SendImmunizationReminderAsync(ImmunizationRecord immunizationRecord, string vaccineName, DateTime dueDate)
        {
            try
            {
                var parentEmail = _encryptionService.Decrypt(immunizationRecord.Email);
                var childName = _encryptionService.Decrypt(immunizationRecord.ChildName);
                var motherName = _encryptionService.Decrypt(immunizationRecord.MotherName);
                var formattedDueDate = dueDate.ToString("MMMM dd, yyyy");

                // Create in-app notification (if user account exists)
                var user = await _context.Users.FirstOrDefaultAsync(u => 
                    _encryptionService.Decrypt(u.Email) == parentEmail);

                if (user != null)
                {
                    var title = $"Immunization Due - {vaccineName}";
                    var message = $"{childName}'s {vaccineName} vaccination is due on {formattedDueDate}. Please schedule an appointment.";
                    
                    await _notificationService.CreateNotificationForUserAsync(
                        user.Id,
                        title,
                        message,
                        "Info",
                        "/User/Appointments"
                    );
                }

                // Send email notification
                var emailSubject = $"BHCARE - Immunization Reminder: {vaccineName}";
                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='background-color: #FF8C42; padding: 20px; color: white;'>
                            <h2>Immunization Reminder</h2>
                        </div>
                        <div style='padding: 20px;'>
                            <p>Dear {motherName},</p>
                            <p>This is a reminder that <strong>{childName}</strong> is due for the following vaccination:</p>
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #FF8C42; margin: 20px 0;'>
                                <p><strong>Vaccine:</strong> {vaccineName}</p>
                                <p><strong>Due Date:</strong> {formattedDueDate}</p>
                                <p><strong>Child:</strong> {childName}</p>
                            </div>
                            <p>Please schedule an appointment at your earliest convenience to ensure your child stays up-to-date with their immunizations.</p>
                            <p>Keeping your child's immunizations current is important for their health and protection against preventable diseases.</p>
                            <p>Best regards,<br>BHCARE Team</p>
                        </div>
                    </body>
                    </html>
                ";

                await _emailSender.SendEmailAsync(parentEmail, emailSubject, emailBody);
                _logger.LogInformation($"Immunization reminder sent for {childName} - {vaccineName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending immunization reminder for record {immunizationRecord.Id}");
            }
        }

        public async Task SendAppointmentConfirmationAsync(Appointment appointment)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == appointment.PatientId);

                if (patient == null || patient.User == null) return;

                var patientEmail = _encryptionService.Decrypt(patient.User.Email);
                var patientName = _encryptionService.Decrypt(appointment.PatientName);
                var appointmentDateTime = appointment.GetAppointmentDateTime();
                var formattedDate = appointmentDateTime.ToString("MMMM dd, yyyy");
                var formattedTime = appointmentDateTime.ToString("hh:mm tt");

                // Create in-app notification
                await _notificationService.CreateNotificationForUserAsync(
                    patient.UserId,
                    "Appointment Confirmed",
                    $"Your appointment has been confirmed for {formattedDate} at {formattedTime}.",
                    "Success",
                    "/User/Appointments"
                );

                // Send email
                var emailSubject = "BHCARE - Appointment Confirmation";
                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='background-color: #28a745; padding: 20px; color: white;'>
                            <h2>Appointment Confirmed</h2>
                        </div>
                        <div style='padding: 20px;'>
                            <p>Dear {patientName},</p>
                            <p>Your appointment has been successfully confirmed!</p>
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0;'>
                                <p><strong>Date:</strong> {formattedDate}</p>
                                <p><strong>Time:</strong> {formattedTime}</p>
                                <p><strong>Status:</strong> Confirmed</p>
                            </div>
                            <p>We look forward to seeing you!</p>
                            <p>Best regards,<br>BHCARE Team</p>
                        </div>
                    </body>
                    </html>
                ";

                await _emailSender.SendEmailAsync(patientEmail, emailSubject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending appointment confirmation for appointment {appointment.Id}");
            }
        }

        public async Task SendAppointmentCancellationAsync(Appointment appointment)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == appointment.PatientId);

                if (patient == null || patient.User == null) return;

                var patientEmail = _encryptionService.Decrypt(patient.User.Email);
                var patientName = _encryptionService.Decrypt(appointment.PatientName);

                // Create in-app notification
                await _notificationService.CreateNotificationForUserAsync(
                    patient.UserId,
                    "Appointment Cancelled",
                    "Your appointment has been cancelled. Please book a new appointment if needed.",
                    "Danger",
                    "/User/Appointments"
                );

                // Send email
                var emailSubject = "BHCARE - Appointment Cancelled";
                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='background-color: #dc3545; padding: 20px; color: white;'>
                            <h2>Appointment Cancelled</h2>
                        </div>
                        <div style='padding: 20px;'>
                            <p>Dear {patientName},</p>
                            <p>Your appointment has been cancelled.</p>
                            <p>If you would like to reschedule, please book a new appointment through our system.</p>
                            <p>Best regards,<br>BHCARE Team</p>
                        </div>
                    </body>
                    </html>
                ";

                await _emailSender.SendEmailAsync(patientEmail, emailSubject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending appointment cancellation for appointment {appointment.Id}");
            }
        }

        public async Task SendAppointmentRescheduledAsync(Appointment appointment)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == appointment.PatientId);

                if (patient == null || patient.User == null) return;

                var patientEmail = _encryptionService.Decrypt(patient.User.Email);
                var patientName = _encryptionService.Decrypt(appointment.PatientName);
                var appointmentDateTime = appointment.GetAppointmentDateTime();
                var formattedDate = appointmentDateTime.ToString("MMMM dd, yyyy");
                var formattedTime = appointmentDateTime.ToString("hh:mm tt");

                // Create in-app notification
                await _notificationService.CreateNotificationForUserAsync(
                    patient.UserId,
                    "Appointment Rescheduled",
                    $"Your appointment has been rescheduled to {formattedDate} at {formattedTime}.",
                    "Warning",
                    "/User/Appointments"
                );

                // Send email
                var emailSubject = "BHCARE - Appointment Rescheduled";
                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='background-color: #ffc107; padding: 20px; color: #000;'>
                            <h2>Appointment Rescheduled</h2>
                        </div>
                        <div style='padding: 20px;'>
                            <p>Dear {patientName},</p>
                            <p>Your appointment has been rescheduled to:</p>
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                                <p><strong>New Date:</strong> {formattedDate}</p>
                                <p><strong>New Time:</strong> {formattedTime}</p>
                            </div>
                            <p>We apologize for any inconvenience.</p>
                            <p>Best regards,<br>BHCARE Team</p>
                        </div>
                    </body>
                    </html>
                ";

                await _emailSender.SendEmailAsync(patientEmail, emailSubject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending appointment rescheduled notification for appointment {appointment.Id}");
            }
        }
    }
}
