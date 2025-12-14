using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Barangay.Models;
using Barangay.Data;
using System.Linq;
using System.IO;

namespace Barangay.Services
{
    public interface IAppointmentReminderService
    {
        Task SendFollowUpReminderEmailAsync(int appointmentId, string patientEmail, string followUpReason);
        Task SendAppointmentReminderEmailAsync(int appointmentId, string patientEmail, string reminderType);
        Task SendThankYouEmailAsync(string patientEmail, string patientName, Appointment appointment);
        Task ProcessScheduledRemindersAsync();
    }

    public class AppointmentReminderService : IAppointmentReminderService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AppointmentReminderService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IPrescriptionPdfService _prescriptionPdfService;

        public AppointmentReminderService(
            IConfiguration configuration,
            ILogger<AppointmentReminderService> logger,
            ApplicationDbContext context,
            IPrescriptionPdfService prescriptionPdfService)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _prescriptionPdfService = prescriptionPdfService;
        }

        public async Task SendFollowUpReminderEmailAsync(int appointmentId, string patientEmail, string followUpReason)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment with ID {AppointmentId} not found", appointmentId);
                    return;
                }

                var smtpSection = _configuration.GetSection("EmailSettings");
                var smtpHost = smtpSection["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.TryParse(smtpSection["SmtpPort"], out int port) ? port : 587;
                var smtpUser = smtpSection["SmtpUsername"];
                var smtpPassword = smtpSection["SmtpPassword"];
                var fromEmail = smtpSection["FromEmail"] ?? "noreply@baesa.health.com";

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured. Email not sent.");
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new System.Net.NetworkCredential(smtpUser, smtpPassword),
                    EnableSsl = true
                };

                var subject = "Follow-up Appointment Reminder - Baesa Health Care";
                var body = GenerateFollowUpReminderEmailBody(appointment, followUpReason);

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Baesa Health Care"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(patientEmail);

                // Add PDF attachment if prescription details are included
                if (followUpReason.Contains("Prescription Details:"))
                {
                    try
                    {
                        var prescription = await CreatePrescriptionFromFollowUpReason(appointment, followUpReason);
                        var pdfBytes = await _prescriptionPdfService.GeneratePrescriptionPdfAsync(
                            prescription, 
                            appointment.Patient?.User?.FullName ?? "Patient",
                            appointment.Doctor?.FullName ?? "Doctor"
                        );
                        
                        var attachment = new Attachment(new MemoryStream(pdfBytes), "prescription.pdf", "application/pdf");
                        message.Attachments.Add(attachment);
                        
                        _logger.LogInformation("Prescription PDF attached to follow-up email for appointment {AppointmentId}", appointmentId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to attach prescription PDF to follow-up email for appointment {AppointmentId}", appointmentId);
                    }
                }

                await client.SendMailAsync(message);
                _logger.LogInformation("Follow-up reminder email sent successfully to {Email}", patientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send follow-up reminder email to {Email}", patientEmail);
            }
        }

        public async Task SendAppointmentReminderEmailAsync(int appointmentId, string patientEmail, string reminderType)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment with ID {AppointmentId} not found", appointmentId);
                    return;
                }

                var smtpSection = _configuration.GetSection("EmailSettings");
                var smtpHost = smtpSection["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.TryParse(smtpSection["SmtpPort"], out int port) ? port : 587;
                var smtpUser = smtpSection["SmtpUsername"];
                var smtpPassword = smtpSection["SmtpPassword"];
                var fromEmail = smtpSection["FromEmail"] ?? "noreply@baesa.health.com";

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured. Email not sent.");
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new System.Net.NetworkCredential(smtpUser, smtpPassword),
                    EnableSsl = true
                };

                var subject = reminderType == "24hr" 
                    ? "Appointment Reminder - Tomorrow - Baesa Health Care"
                    : "Appointment Reminder - 1 Hour - Baesa Health Care";
                
                var body = GenerateAppointmentReminderEmailBody(appointment, reminderType);

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Baesa Health Care"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(patientEmail);

                await client.SendMailAsync(message);
                _logger.LogInformation("{ReminderType} reminder email sent successfully to {Email}", reminderType, patientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {ReminderType} reminder email to {Email}", reminderType, patientEmail);
            }
        }

        public async Task SendThankYouEmailAsync(string patientEmail, string patientName, Appointment appointment)
        {
            try
            {
                var smtpSection = _configuration.GetSection("EmailSettings");
                var smtpHost = smtpSection["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.TryParse(smtpSection["SmtpPort"], out int port) ? port : 587;
                var smtpUser = smtpSection["SmtpUsername"];
                var smtpPassword = smtpSection["SmtpPassword"];
                var fromEmail = smtpSection["FromEmail"] ?? "noreply@baesa.health.com";

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured. Thank you email not sent.");
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new System.Net.NetworkCredential(smtpUser, smtpPassword),
                    EnableSsl = true
                };

                var subject = "Thank You for Visiting Baesa Health Care";
                var body = GenerateThankYouEmailBody(patientName, appointment);

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Baesa Health Care"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(patientEmail);

                // Add PDF attachment if prescription details are included
                if (appointment.Description != null && appointment.Description.Contains("Prescription:"))
                {
                    try
                    {
                        var prescription = await CreatePrescriptionFromAppointment(appointment);
                        var pdfBytes = await _prescriptionPdfService.GeneratePrescriptionPdfAsync(
                            prescription, 
                            patientName,
                            appointment.Doctor?.FullName ?? "Doctor"
                        );
                        
                        var attachment = new Attachment(new MemoryStream(pdfBytes), "prescription.pdf", "application/pdf");
                        message.Attachments.Add(attachment);
                        
                        _logger.LogInformation("Prescription PDF attached to thank you email for appointment {AppointmentId}", appointment.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to attach prescription PDF to thank you email for appointment {AppointmentId}", appointment.Id);
                    }
                }

                await client.SendMailAsync(message);
                _logger.LogInformation("Thank you email sent successfully to {Email}", patientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send thank you email to {Email}", patientEmail);
            }
        }

        public async Task ProcessScheduledRemindersAsync()
        {
            try
            {
                // Determine local time zone (configurable, with sensible fallbacks for Windows/Linux)
                TimeZoneInfo? tz = null;
                var tzCandidates = new[]
                {
                    _configuration["ReminderSettings:TimeZoneId"], // optional config
                    "Asia/Manila", // Linux/ICU id
                    "Singapore Standard Time" // Windows id that maps to GMT+8 (includes Manila)
                };
                foreach (var id in tzCandidates)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    try { tz = TimeZoneInfo.FindSystemTimeZoneById(id); break; } catch { /* try next */ }
                }

                var nowUtc = DateTime.UtcNow;
                var nowLocal = tz != null ? TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz) : DateTime.Now;
                var tomorrowLocal = nowLocal.Date.AddDays(1);
                var oneHourFromNowLocal = nowLocal.AddHours(1);

                _logger.LogInformation(
                    "[Reminders] Using TZ '{TimeZoneId}'. nowLocal={NowLocal:o}, tomorrowLocal={TomorrowLocal:d}, window={Start:t}-{End:t}",
                    tz?.Id ?? "SystemLocal", nowLocal, tomorrowLocal, nowLocal, oneHourFromNowLocal);

                // Get appointments for 24-hour reminders (tomorrow)
                var tomorrowAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Doctor)
                    .Where(a => a.AppointmentDate.Date == tomorrowLocal &&
                               a.Status == AppointmentStatus.Confirmed &&
                               !string.IsNullOrEmpty(a.Patient.User.Email))
                    .ToListAsync();

                // Get appointments for 1-hour reminders
                var oneHourAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Doctor)
                    .Where(a => a.AppointmentDate.Date == nowLocal.Date &&
                               a.AppointmentTime >= nowLocal.TimeOfDay &&
                               a.AppointmentTime <= oneHourFromNowLocal.TimeOfDay &&
                               a.Status == AppointmentStatus.Confirmed &&
                               !string.IsNullOrEmpty(a.Patient.User.Email))
                    .ToListAsync();

                // Send 24-hour reminders
                foreach (var appointment in tomorrowAppointments)
                {
                    await SendAppointmentReminderEmailAsync(appointment.Id, appointment.Patient.User.Email, "24hr");
                }

                // Send 1-hour reminders
                foreach (var appointment in oneHourAppointments)
                {
                    await SendAppointmentReminderEmailAsync(appointment.Id, appointment.Patient.User.Email, "1hr");
                }

                _logger.LogInformation(
                    "Processed scheduled reminders: {TomorrowCount} 24hr reminders, {OneHourCount} 1hr reminders (TZ={TimeZoneId})",
                    tomorrowAppointments.Count, oneHourAppointments.Count, tz?.Id ?? "SystemLocal");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled reminders");
            }
        }

        private string GenerateFollowUpReminderEmailBody(Appointment appointment, string followUpReason)
        {
            var patientName = appointment.Patient?.User?.FullName ?? "Patient";
            var doctorName = appointment.Doctor?.FullName ?? "Doctor";
            var appointmentDate = appointment.AppointmentDate.ToString("MMMM dd, yyyy");
            var appointmentTime = appointment.AppointmentTime.ToString(@"hh\:mm");

            // Check if prescription details are included in followUpReason
            var prescriptionSection = "";
            if (followUpReason.Contains("Prescription Details:"))
            {
                var parts = followUpReason.Split(new[] { "Prescription Details:" }, StringSplitOptions.None);
                var reason = parts[0].Trim();
                var prescription = parts.Length > 1 ? parts[1].Trim() : "";
                
                prescriptionSection = $@"
                <div class='prescription-section'>
                    <h4>💊 Prescription</h4>
                    <div class='prescription-box'>
                        <div class='prescription-header'>
                            <div class='clinic-name'>BAESA HEALTH CENTER</div>
                            <div class='department'>City Health Department</div>
                        </div>
                        <div class='prescription-divider'></div>
                        <div class='prescription-content'>
                            <div class='prescription-info'>
                                <div class='info-row'><strong>Patient:</strong> {patientName}</div>
                                <div class='info-row'><strong>Date:</strong> {appointmentDate}</div>
                                <div class='info-row'><strong>Dr.</strong> {doctorName}</div>
                            </div>
                            <div class='prescription-rx-section'>
                                <div class='rx-symbol'>Rx</div>
                                <div class='prescription-text'>
                                    {prescription.Replace("\n", "<br>")}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>";
                
                followUpReason = reason; // Use only the reason part for the main content
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Follow-up Appointment Reminder</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 8px; margin-bottom: 20px; }}
        .content {{ background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .highlight {{ background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 14px; }}
        .btn {{ display: inline-block; background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
        .prescription-section {{ margin: 20px 0; }}
        .prescription-box {{ 
            border: 2px solid #000; 
            background-color: white; 
            padding: 20px; 
            font-family: 'Times New Roman', serif; 
            font-size: 14px;
            max-width: 500px;
            margin: 0 auto;
            box-shadow: 0 4px 8px rgba(0,0,0,0.1);
        }}
        .prescription-header {{ 
            text-align: center; 
            margin-bottom: 15px; 
        }}
        .clinic-name {{ 
            font-size: 18px; 
            font-weight: bold; 
            color: #000; 
            margin-bottom: 5px;
        }}
        .department {{ 
            font-size: 12px; 
            color: #666; 
            font-style: italic;
        }}
        .prescription-divider {{ 
            border-bottom: 1px solid #000; 
            margin: 15px 0; 
        }}
        .prescription-content {{ 
            display: flex; 
            justify-content: space-between; 
            align-items: flex-start;
            gap: 20px; 
        }}
        .prescription-info {{ 
            font-size: 12px; 
            line-height: 1.4; 
            flex: 1;
        }}
        .info-row {{ 
            margin-bottom: 5px; 
        }}
        .prescription-rx-section {{ 
            flex: 1; 
            text-align: right;
        }}
        .rx-symbol {{ 
            font-size: 24px; 
            font-weight: bold; 
            margin-bottom: 10px;
            color: #000;
        }}
        .prescription-text {{ 
            font-size: 12px; 
            line-height: 1.5; 
            text-align: left;
            white-space: pre-line;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🏥 Baesa Health Care</h2>
            <p>Follow-up Appointment Reminder</p>
        </div>
        
        <div class='content'>
            <h3>Dear {patientName},</h3>
            
            <p>Thank you for choosing Baesa Health Care for your healthcare needs. We hope you are feeling better after your recent consultation.</p>
            
            <div class='highlight'>
                <h4>📅 Follow-up Appointment Scheduled</h4>
                <p><strong>Date:</strong> {appointmentDate}</p>
                <p><strong>Time:</strong> {appointmentTime}</p>
                <p><strong>Doctor:</strong> Dr. {doctorName}</p>
                <p><strong>Reason:</strong> {followUpReason}</p>
            </div>
            
            {prescriptionSection}
            
            <p>This follow-up appointment is important for:</p>
            <ul>
                <li>Monitoring your progress and recovery</li>
                <li>Reviewing your treatment plan</li>
                <li>Addressing any concerns or questions you may have</li>
                <li>Ensuring optimal health outcomes</li>
            </ul>
            
            <p><strong>Please remember to:</strong></p>
            <ul>
                <li>Arrive 15 minutes before your scheduled time</li>
                <li>Bring your health card and any relevant documents</li>
                <li>Note any changes in your condition since your last visit</li>
                <li>Prepare any questions you may have for the doctor</li>
            </ul>
            
            <p>If you need to reschedule or have any questions, please contact us as soon as possible.</p>
            
            <p>We look forward to seeing you and continuing to support your health journey.</p>
            
            <p>Best regards,<br>
            <strong>Baesa Health Care Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
            <p>For inquiries, please contact our health center directly.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateAppointmentReminderEmailBody(Appointment appointment, string reminderType)
        {
            var patientName = appointment.Patient?.User?.FullName ?? "Patient";
            var doctorName = appointment.Doctor?.FullName ?? "Doctor";
            var appointmentDate = appointment.AppointmentDate.ToString("MMMM dd, yyyy");
            var appointmentTime = appointment.AppointmentTime.ToString(@"hh\:mm");
            
            var timeMessage = reminderType == "24hr" 
                ? "Your appointment is scheduled for tomorrow"
                : "Your appointment is scheduled in 1 hour";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Appointment Reminder</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 8px; margin-bottom: 20px; }}
        .content {{ background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .highlight {{ background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .urgent {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🏥 Baesa Health Care</h2>
            <p>Appointment Reminder</p>
        </div>
        
        <div class='content'>
            <h3>Dear {patientName},</h3>
            
            <p>This is a friendly reminder that {timeMessage.ToLower()}.</p>
            
            <div class='highlight'>
                <h4>📅 Appointment Details</h4>
                <p><strong>Date:</strong> {appointmentDate}</p>
                <p><strong>Time:</strong> {appointmentTime}</p>
                <p><strong>Doctor:</strong> Dr. {doctorName}</p>
            </div>
            
            <div class='urgent'>
                <h4>Important Reminders</h4>
                <ul>
                    <li>Please arrive 15 minutes before your scheduled time</li>
                    <li>Bring your health card and any relevant medical documents</li>
                    <li>If you need to reschedule, please contact us immediately</li>
                    <li>Wear comfortable clothing for your examination</li>
                </ul>
            </div>
            
            <p>We look forward to seeing you and providing you with the best healthcare service.</p>
            
            <p>If you have any questions or need to make changes to your appointment, please don't hesitate to contact us.</p>
            
            <p>Best regards,<br>
            <strong>Baesa Health Care Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>This is an automated reminder. Please do not reply to this email.</p>
            <p>For inquiries, please contact our health center directly.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateThankYouEmailBody(string patientName, Appointment appointment)
        {
            var doctorName = appointment.Doctor?.FullName ?? "Doctor";
            var appointmentDate = appointment.AppointmentDate.ToString("MMMM dd, yyyy");
            var appointmentTime = appointment.AppointmentTime.ToString(@"hh\:mm");

            // Check if prescription details are included in appointment description
            var prescriptionSection = "";
            if (appointment.Description != null && appointment.Description.Contains("Prescription:"))
            {
                var prescription = appointment.Description.Split(new[] { "Prescription:" }, StringSplitOptions.None)[1].Trim();
                
                prescriptionSection = $@"
                <div class='prescription-section'>
                    <h4>💊 Prescription</h4>
                    <div class='prescription-box'>
                        <div class='prescription-header'>
                            <div class='clinic-name'>BAESA HEALTH CENTER</div>
                            <div class='department'>City Health Department</div>
                        </div>
                        <div class='prescription-divider'></div>
                        <div class='prescription-content'>
                            <div class='prescription-info'>
                                <div class='info-row'><strong>Patient:</strong> {patientName}</div>
                                <div class='info-row'><strong>Date:</strong> {appointmentDate}</div>
                                <div class='info-row'><strong>Dr.</strong> {doctorName}</div>
                            </div>
                            <div class='prescription-rx-section'>
                                <div class='rx-symbol'>Rx</div>
                                <div class='prescription-text'>
                                    {prescription.Replace("\n", "<br>")}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>";
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Thank You for Visiting Baesa Health Care</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 8px; margin-bottom: 20px; }}
        .content {{ background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .highlight {{ background-color: #e8f5e8; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #28a745; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 14px; }}
        .consultation-info {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .prescription-section {{ margin: 20px 0; }}
        .prescription-box {{ 
            border: 2px solid #000; 
            background-color: white; 
            padding: 20px; 
            font-family: 'Times New Roman', serif; 
            font-size: 14px;
            max-width: 500px;
            margin: 0 auto;
            box-shadow: 0 4px 8px rgba(0,0,0,0.1);
        }}
        .prescription-header {{ 
            text-align: center; 
            margin-bottom: 15px; 
        }}
        .clinic-name {{ 
            font-size: 18px; 
            font-weight: bold; 
            color: #000; 
            margin-bottom: 5px;
        }}
        .department {{ 
            font-size: 12px; 
            color: #666; 
            font-style: italic;
        }}
        .prescription-divider {{ 
            border-bottom: 1px solid #000; 
            margin: 15px 0; 
        }}
        .prescription-content {{ 
            display: flex; 
            justify-content: space-between; 
            align-items: flex-start;
            gap: 20px; 
        }}
        .prescription-info {{ 
            font-size: 12px; 
            line-height: 1.4; 
            flex: 1;
        }}
        .info-row {{ 
            margin-bottom: 5px; 
        }}
        .prescription-rx-section {{ 
            flex: 1; 
            text-align: right;
        }}
        .rx-symbol {{ 
            font-size: 24px; 
            font-weight: bold; 
            margin-bottom: 10px;
            color: #000;
        }}
        .prescription-text {{ 
            font-size: 12px; 
            line-height: 1.5; 
            text-align: left;
            white-space: pre-line;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🏥 Baesa Health Care</h2>
            <p>Thank You for Your Visit</p>
        </div>
        
        <div class='content'>
            <h3>Dear {patientName},</h3>
            
            <div class='highlight'>
                <h4>🙏 Thank You!</h4>
                <p>Thank you for choosing Baesa Health Care for your healthcare needs. We appreciate your trust in our medical services and hope you are feeling better after your consultation.</p>
            </div>
            
            <div class='consultation-info'>
                <h4>📋 Consultation Details</h4>
                <p><strong>Date:</strong> {appointmentDate}</p>
                <p><strong>Time:</strong> {appointmentTime}</p>
                <p><strong>Doctor:</strong> Dr. {doctorName}</p>
            </div>
            
            {prescriptionSection}
            
            <p>Your consultation has been completed successfully. We have recorded all the necessary information in your medical records for future reference.</p>
            
            <p><strong>Important Notes:</strong></p>
            <ul>
                <li>Please follow any treatment recommendations provided by your doctor</li>
                <li>Keep this consultation summary for your records</li>
                <li>Contact us if you have any questions or concerns</li>
                <li>Schedule a new appointment if you need further medical attention</li>
            </ul>
            
            <p>We are committed to providing you with the best healthcare service and look forward to serving you again in the future.</p>
            
            <p>Take care and stay healthy!</p>
            
            <p>Best regards,<br>
            <strong>Baesa Health Care Team</strong></p>
        </div>
        
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
            <p>For inquiries, please contact our health center directly.</p>
        </div>
    </div>
</body>
</html>";
        }

        private async Task<Prescription> CreatePrescriptionFromFollowUpReason(Appointment appointment, string followUpReason)
        {
            var prescriptionText = "";
            if (followUpReason.Contains("Prescription Details:"))
            {
                var parts = followUpReason.Split(new[] { "Prescription Details:" }, StringSplitOptions.None);
                prescriptionText = parts.Length > 1 ? parts[1].Trim() : "";
            }

            return new Prescription
            {
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                Diagnosis = "Consultation diagnosis",
                Notes = prescriptionText,
                Duration = 7,
                PrescriptionDate = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddDays(7),
                Status = PrescriptionStatus.Created
            };
        }

        private async Task<Prescription> CreatePrescriptionFromAppointment(Appointment appointment)
        {
            var prescriptionText = "";
            if (appointment.Description != null && appointment.Description.Contains("Prescription:"))
            {
                var parts = appointment.Description.Split(new[] { "Prescription:" }, StringSplitOptions.None);
                prescriptionText = parts.Length > 1 ? parts[1].Trim() : "";
            }

            return new Prescription
            {
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                Diagnosis = "Consultation diagnosis",
                Notes = prescriptionText,
                Duration = 7,
                PrescriptionDate = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddDays(7),
                Status = PrescriptionStatus.Created
            };
        }
    }
}
