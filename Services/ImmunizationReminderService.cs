using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using System.Net;
using System.Net.Mail;
using Barangay.Services;

namespace Barangay.Services
{
    public interface IImmunizationReminderService
    {
        Task SendImmunizationReminderAsync(string email, string patientName, string customMessage);
        Task SendVaccineUpdateNotificationAsync(string email, string childName, Models.ImmunizationRecord record);
    }

    public class ImmunizationReminderService : IImmunizationReminderService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImmunizationReminderService> _logger;
        private readonly IDataEncryptionService _encryptionService;

        public ImmunizationReminderService(
            IConfiguration configuration,
            ILogger<ImmunizationReminderService> logger,
            IDataEncryptionService encryptionService)
        {
            _configuration = configuration;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        public async Task SendImmunizationReminderAsync(string email, string patientName, string customMessage)
        {
            try
            {
                _logger.LogInformation("Starting to send immunization reminder email to {Email}", email);
                
                var smtpSection = _configuration.GetSection("EmailSettings");
                var smtpHost = smtpSection["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.TryParse(smtpSection["SmtpPort"], out int port) ? port : 587;
                var smtpUser = smtpSection["SmtpUsername"];
                var smtpPassword = smtpSection["SmtpPassword"];
                var fromEmail = smtpSection["FromEmail"] ?? "noreply@baesa.health.com";

                _logger.LogInformation("SMTP Configuration - Host: {Host}, Port: {Port}, User: {User}, Password Set: {PasswordSet}", 
                    smtpHost, smtpPort, smtpUser, !string.IsNullOrEmpty(smtpPassword));

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogError("SMTP credentials not configured. Username: {User}, Password Set: {PasswordSet}", 
                        smtpUser, !string.IsNullOrEmpty(smtpPassword));
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPassword),
                    EnableSsl = true,
                    Timeout = 30000 // 30 seconds timeout
                };

                var subject = "Immunization Schedule Confirmation - Baesa Health Center";
                var body = GenerateImmunizationReminderEmailBody(patientName, customMessage);

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Baesa Health Center"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(email);

                _logger.LogInformation("Attempting to send email to {Email} with subject: {Subject}", email, subject);
                await client.SendMailAsync(message);
                _logger.LogInformation("✅ Immunization reminder email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send immunization reminder email to {Email}. Error: {ErrorMessage}", email, ex.Message);
                _logger.LogError("Full exception details: {Exception}", ex.ToString());
            }
        }

        public async Task SendVaccineUpdateNotificationAsync(string email, string childName, Models.ImmunizationRecord record)
        {
            try
            {
                // NOTE: Record is already decrypted in the page model (ImmunizationRecords.cshtml.cs)
                // before being passed here, so we use it directly without re-decrypting
                
                var smtpSection = _configuration.GetSection("EmailSettings");
                var smtpHost = smtpSection["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.TryParse(smtpSection["SmtpPort"], out int port) ? port : 587;
                var smtpUser = smtpSection["SmtpUsername"];
                var smtpPassword = smtpSection["SmtpPassword"];
                var fromEmail = smtpSection["FromEmail"] ?? "noreply@baesa.health.com";

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured. Vaccine update notification email not sent.");
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPassword),
                    EnableSsl = true
                };

                var subject = $"Immunization Record Updated - {childName} - Baesa Health Center";
                var body = GenerateVaccineUpdateEmailBody(childName, record);

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Baesa Health Center"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(email);

                await client.SendMailAsync(message);
                _logger.LogInformation("Vaccine update notification email sent successfully to {Email} for child {ChildName}", email, childName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send vaccine update notification email to {Email} for child {ChildName}", email, childName);
            }
        }

        private string GenerateImmunizationReminderEmailBody(string patientName, string customMessage)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Immunization Reminder</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f8f9fa;
        }}
        .container {{
            background-color: #ffffff;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
            padding-bottom: 20px;
            border-bottom: 3px solid #007bff;
        }}
        .logo {{
            font-size: 24px;
            font-weight: bold;
            color: #007bff;
            margin-bottom: 10px;
        }}
        .subtitle {{
            color: #6c757d;
            font-size: 14px;
        }}
        .content {{
            margin-bottom: 30px;
        }}
        .greeting {{
            font-size: 18px;
            margin-bottom: 20px;
            color: #007bff;
        }}
        .message {{
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            border-left: 4px solid #28a745;
            margin: 20px 0;
            white-space: pre-line;
        }}
        .schedule-info {{
            background-color: #e7f3ff;
            padding: 20px;
            border-radius: 8px;
            margin: 20px 0;
            border-left: 4px solid #007bff;
        }}
        .schedule-title {{
            font-weight: bold;
            color: #007bff;
            margin-bottom: 10px;
        }}
        .schedule-item {{
            margin: 8px 0;
            display: flex;
            align-items: center;
        }}
        .schedule-item i {{
            margin-right: 10px;
            color: #28a745;
            width: 20px;
        }}
        .footer {{
            text-align: center;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            color: #6c757d;
            font-size: 14px;
        }}
        .contact-info {{
            background-color: #fff3cd;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
            border-left: 4px solid #ffc107;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>🏥 Baesa Health Center</div>
            <div class='subtitle'>Caloocan City</div>
        </div>
        
        <div class='content'>
            <div class='greeting'>Dear {patientName},</div>
            
            <div class='message'>{customMessage}</div>
            
            <div class='schedule-info'>
                <div class='schedule-title'>📅 Immunization Schedule</div>
                <div class='schedule-item'>
                    <i>📅</i>
                    <span><strong>Day:</strong> Every Wednesday</span>
                </div>
                <div class='schedule-item'>
                    <i>🕐</i>
                    <span><strong>Time:</strong> 8:00 AM - 12:00 PM</span>
                </div>
                <div class='schedule-item'>
                    <i>📍</i>
                    <span><strong>Location:</strong> Baesa Health Center</span>
                </div>
            </div>
            
            <div class='contact-info'>
                <strong>📞 Contact Information:</strong><br>
                For any questions or to schedule an appointment, please contact us at the health center.
            </div>
        </div>
        
        <div class='footer'>
            <p>Thank you for prioritizing your child's health and well-being.</p>
            <p><strong>Baesa Health Center Team</strong></p>
            <p><em>This is an automated message. Please do not reply to this email.</em></p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateVaccineUpdateEmailBody(string childName, Models.ImmunizationRecord record)
        {
            var vaccineInfo = GetVaccineInformation(record);
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Immunization Record Updated</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f8f9fa;
        }}
        .container {{
            background-color: #ffffff;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
            padding-bottom: 20px;
            border-bottom: 3px solid #28a745;
        }}
        .logo {{
            font-size: 24px;
            font-weight: bold;
            color: #28a745;
            margin-bottom: 10px;
        }}
        .subtitle {{
            color: #6c757d;
            font-size: 14px;
        }}
        .content {{
            margin-bottom: 30px;
        }}
        .greeting {{
            font-size: 18px;
            margin-bottom: 20px;
            color: #28a745;
        }}
        .update-notice {{
            background-color: #d4edda;
            padding: 20px;
            border-radius: 8px;
            border-left: 4px solid #28a745;
            margin: 20px 0;
        }}
        .child-info {{
            background-color: #e7f3ff;
            padding: 20px;
            border-radius: 8px;
            margin: 20px 0;
            border-left: 4px solid #007bff;
        }}
        .vaccine-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .vaccine-table th {{
            background-color: #007bff;
            color: white;
            padding: 12px;
            text-align: left;
            font-weight: bold;
        }}
        .vaccine-table td {{
            padding: 12px;
            border-bottom: 1px solid #dee2e6;
        }}
        .vaccine-table tr:nth-child(even) {{
            background-color: #f8f9fa;
        }}
        .vaccine-table tr:hover {{
            background-color: #e9ecef;
        }}
        .dose-badge {{
            background-color: #007bff;
            color: white;
            padding: 4px 8px;
            border-radius: 12px;
            font-size: 12px;
            font-weight: bold;
        }}
        .date-given {{
            color: #28a745;
            font-weight: bold;
        }}
        .not-given {{
            color: #dc3545;
            font-style: italic;
        }}
        .footer {{
            text-align: center;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            color: #6c757d;
            font-size: 14px;
        }}
        .contact-info {{
            background-color: #fff3cd;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
            border-left: 4px solid #ffc107;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>💉 Baesa Health Center</div>
            <div class='subtitle'>Immunization Record Update</div>
        </div>
        
        <div class='content'>
            <div class='greeting'>Dear Parent/Guardian,</div>
            
            <div class='update-notice'>
                <strong>📋 Immunization Record Updated</strong><br>
                Your child's immunization record has been successfully updated in our system. Please review the updated information below.
            </div>
            
            <div class='child-info'>
                <strong>👶 Child Information:</strong><br>
                <strong>Name:</strong> {childName}<br>
                <strong>Date of Birth:</strong> {record.DateOfBirth}<br>
                <strong>Family Number:</strong> {record.FamilyNumber}<br>
                <strong>Health Center:</strong> {record.HealthCenter}<br>
                <strong>Barangay:</strong> {record.Barangay}
            </div>
            
            <h3 style='color: #007bff; margin-top: 30px;'>💉 Updated Vaccine Information</h3>
            <table class='vaccine-table'>
                <thead>
                    <tr>
                        <th>Vaccine</th>
                        <th>Dose</th>
                        <th>Date Given</th>
                        <th>Remarks</th>
                    </tr>
                </thead>
                <tbody>
                    {vaccineInfo}
                </tbody>
            </table>
            
            <div class='contact-info'>
                <strong>📞 Contact Information:</strong><br>
                If you have any questions about your child's immunization record or need to schedule additional vaccinations, please contact us at the health center.
            </div>
        </div>
        
        <div class='footer'>
            <p>Thank you for keeping your child's immunization records up to date.</p>
            <p><strong>Baesa Health Center Team</strong></p>
            <p><em>This is an automated notification. Please do not reply to this email.</em></p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetVaccineInformation(Models.ImmunizationRecord record)
        {
            var vaccineRows = new List<string>();

            // BCG Vaccine
            if (!string.IsNullOrEmpty(record.BCGVaccineDate) || !string.IsNullOrEmpty(record.BCGVaccineRemarks))
            {
                vaccineRows.Add($@"
                    <tr>
                        <td><strong>BCG Vaccine</strong><br><small>At Birth</small></td>
                        <td><span class='dose-badge'>①</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.BCGVaccineDate) ? record.BCGVaccineDate : "Not given")}</td>
                        <td>{record.BCGVaccineRemarks ?? ""}</td>
                    </tr>");
            }

            // Hepatitis B Vaccine
            if (!string.IsNullOrEmpty(record.HepatitisBVaccineDate) || !string.IsNullOrEmpty(record.HepatitisBVaccineRemarks))
            {
                vaccineRows.Add($@"
                    <tr>
                        <td><strong>Hepatitis B Vaccine</strong><br><small>At Birth</small></td>
                        <td><span class='dose-badge'>①</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.HepatitisBVaccineDate) ? record.HepatitisBVaccineDate : "Not given")}</td>
                        <td>{record.HepatitisBVaccineRemarks ?? ""}</td>
                    </tr>");
            }

            // Pentavalent Vaccine
            if (!string.IsNullOrEmpty(record.Pentavalent1Date) || !string.IsNullOrEmpty(record.Pentavalent2Date) || !string.IsNullOrEmpty(record.Pentavalent3Date) ||
                !string.IsNullOrEmpty(record.Pentavalent1Remarks) || !string.IsNullOrEmpty(record.Pentavalent2Remarks) || !string.IsNullOrEmpty(record.Pentavalent3Remarks))
            {
                vaccineRows.Add($@"
                    <tr>
                        <td rowspan='3'><strong>Pentavalent Vaccine</strong><br><small>DPT, Hep B, HiB</small></td>
                        <td><span class='dose-badge'>①</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.Pentavalent1Date) ? record.Pentavalent1Date : "Not given")}</td>
                        <td>{record.Pentavalent1Remarks ?? ""}</td>
                    </tr>
                    <tr>
                        <td><span class='dose-badge'>②</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.Pentavalent2Date) ? record.Pentavalent2Date : "Not given")}</td>
                        <td>{record.Pentavalent2Remarks ?? ""}</td>
                    </tr>
                    <tr>
                        <td><span class='dose-badge'>③</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.Pentavalent3Date) ? record.Pentavalent3Date : "Not given")}</td>
                        <td>{record.Pentavalent3Remarks ?? ""}</td>
                    </tr>");
            }

            // OPV Vaccine
            if (!string.IsNullOrEmpty(record.OPV1Date) || !string.IsNullOrEmpty(record.OPV2Date) || !string.IsNullOrEmpty(record.OPV3Date) ||
                !string.IsNullOrEmpty(record.OPV1Remarks) || !string.IsNullOrEmpty(record.OPV2Remarks) || !string.IsNullOrEmpty(record.OPV3Remarks))
            {
                vaccineRows.Add($@"
                    <tr>
                        <td rowspan='3'><strong>OPV Vaccine</strong><br><small>Oral Polio Vaccine</small></td>
                        <td><span class='dose-badge'>①</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.OPV1Date) ? record.OPV1Date : "Not given")}</td>
                        <td>{record.OPV1Remarks ?? ""}</td>
                    </tr>
                    <tr>
                        <td><span class='dose-badge'>②</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.OPV2Date) ? record.OPV2Date : "Not given")}</td>
                        <td>{record.OPV2Remarks ?? ""}</td>
                    </tr>
                    <tr>
                        <td><span class='dose-badge'>③</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.OPV3Date) ? record.OPV3Date : "Not given")}</td>
                        <td>{record.OPV3Remarks ?? ""}</td>
                    </tr>");
            }

            // IPV Vaccine
            if (!string.IsNullOrEmpty(record.IPV1Date) || !string.IsNullOrEmpty(record.IPV2Date) ||
                !string.IsNullOrEmpty(record.IPV1Remarks) || !string.IsNullOrEmpty(record.IPV2Remarks))
            {
                vaccineRows.Add($@"
                    <tr>
                        <td rowspan='2'><strong>IPV Vaccine</strong><br><small>Inactivated Polio Vaccine</small></td>
                        <td><span class='dose-badge'>①</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.IPV1Date) ? record.IPV1Date : "Not given")}</td>
                        <td>{record.IPV1Remarks ?? ""}</td>
                    </tr>
                    <tr>
                        <td><span class='dose-badge'>②</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.IPV2Date) ? record.IPV2Date : "Not given")}</td>
                        <td>{record.IPV2Remarks ?? ""}</td>
                    </tr>");
            }

            // PCV Vaccine
            if (!string.IsNullOrEmpty(record.PCV1Date) || !string.IsNullOrEmpty(record.PCV2Date) || !string.IsNullOrEmpty(record.PCV3Date) ||
                !string.IsNullOrEmpty(record.PCV1Remarks) || !string.IsNullOrEmpty(record.PCV2Remarks) || !string.IsNullOrEmpty(record.PCV3Remarks))
            {
                vaccineRows.Add($@"
                    <tr>
                        <td rowspan='3'><strong>PCV Vaccine</strong><br><small>Pneumococcal Conjugate Vaccine</small></td>
                        <td><span class='dose-badge'>①</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.PCV1Date) ? record.PCV1Date : "Not given")}</td>
                        <td>{record.PCV1Remarks ?? ""}</td>
                    </tr>
                    <tr>
                        <td><span class='dose-badge'>②</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.PCV2Date) ? record.PCV2Date : "Not given")}</td>
                        <td>{record.PCV2Remarks ?? ""}</td>
                    </tr>
                    <tr>
                        <td><span class='dose-badge'>③</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.PCV3Date) ? record.PCV3Date : "Not given")}</td>
                        <td>{record.PCV3Remarks ?? ""}</td>
                    </tr>");
            }

            // MMR Vaccine
            if (!string.IsNullOrEmpty(record.MMR1Date) || !string.IsNullOrEmpty(record.MMR2Date) ||
                !string.IsNullOrEmpty(record.MMR1Remarks) || !string.IsNullOrEmpty(record.MMR2Remarks))
            {
                vaccineRows.Add($@"
                    <tr>
                        <td rowspan='2'><strong>MMR Vaccine</strong><br><small>Measles, Mumps, Rubella</small></td>
                        <td><span class='dose-badge'>①</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.MMR1Date) ? record.MMR1Date : "Not given")}</td>
                        <td>{record.MMR1Remarks ?? ""}</td>
                    </tr>
                    <tr>
                        <td><span class='dose-badge'>②</span></td>
                        <td class='date-given'>{(!string.IsNullOrEmpty(record.MMR2Date) ? record.MMR2Date : "Not given")}</td>
                        <td>{record.MMR2Remarks ?? ""}</td>
                    </tr>");
            }

            return string.Join("", vaccineRows);
        }

        private Models.ImmunizationRecord DecryptImmunizationRecord(Models.ImmunizationRecord record)
        {
            try
            {
                // Create a copy to avoid modifying the original
                var decrypted = new Models.ImmunizationRecord
                {
                    Id = record.Id,
                    // Decrypt ALL sensitive fields
                    ChildName = DecryptIfNeeded(record.ChildName),
                    DateOfBirth = DecryptIfNeeded(record.DateOfBirth),
                    FamilyNumber = DecryptIfNeeded(record.FamilyNumber),
                    MotherName = DecryptIfNeeded(record.MotherName),
                    FatherName = DecryptIfNeeded(record.FatherName),
                    Sex = record.Sex, // Not encrypted
                    PlaceOfBirth = DecryptIfNeeded(record.PlaceOfBirth),
                    BirthHeight = DecryptIfNeeded(record.BirthHeight),
                    BirthWeight = DecryptIfNeeded(record.BirthWeight),
                    Address = DecryptIfNeeded(record.Address),
                    HealthCenter = DecryptIfNeeded(record.HealthCenter),
                    Barangay = DecryptIfNeeded(record.Barangay),
                    Email = DecryptIfNeeded(record.Email),
                    ContactNumber = DecryptIfNeeded(record.ContactNumber),
                    CreatedBy = DecryptIfNeeded(record.CreatedBy),
                    UpdatedBy = DecryptIfNeeded(record.UpdatedBy),
                    CreatedAt = record.CreatedAt,
                    UpdatedAt = record.UpdatedAt,
                    // Copy vaccine dates (might need decryption too)
                    BCGVaccineDate = DecryptIfNeeded(record.BCGVaccineDate),
                    BCGVaccineRemarks = DecryptIfNeeded(record.BCGVaccineRemarks),
                    HepatitisBVaccineDate = DecryptIfNeeded(record.HepatitisBVaccineDate),
                    HepatitisBVaccineRemarks = DecryptIfNeeded(record.HepatitisBVaccineRemarks),
                    Pentavalent1Date = DecryptIfNeeded(record.Pentavalent1Date),
                    Pentavalent1Remarks = DecryptIfNeeded(record.Pentavalent1Remarks),
                    Pentavalent2Date = DecryptIfNeeded(record.Pentavalent2Date),
                    Pentavalent2Remarks = DecryptIfNeeded(record.Pentavalent2Remarks),
                    Pentavalent3Date = DecryptIfNeeded(record.Pentavalent3Date),
                    Pentavalent3Remarks = DecryptIfNeeded(record.Pentavalent3Remarks),
                    OPV1Date = DecryptIfNeeded(record.OPV1Date),
                    OPV1Remarks = DecryptIfNeeded(record.OPV1Remarks),
                    OPV2Date = DecryptIfNeeded(record.OPV2Date),
                    OPV2Remarks = DecryptIfNeeded(record.OPV2Remarks),
                    OPV3Date = DecryptIfNeeded(record.OPV3Date),
                    OPV3Remarks = DecryptIfNeeded(record.OPV3Remarks),
                    IPV1Date = DecryptIfNeeded(record.IPV1Date),
                    IPV1Remarks = DecryptIfNeeded(record.IPV1Remarks),
                    IPV2Date = DecryptIfNeeded(record.IPV2Date),
                    IPV2Remarks = DecryptIfNeeded(record.IPV2Remarks),
                    PCV1Date = DecryptIfNeeded(record.PCV1Date),
                    PCV1Remarks = DecryptIfNeeded(record.PCV1Remarks),
                    PCV2Date = DecryptIfNeeded(record.PCV2Date),
                    PCV2Remarks = DecryptIfNeeded(record.PCV2Remarks),
                    PCV3Date = DecryptIfNeeded(record.PCV3Date),
                    PCV3Remarks = DecryptIfNeeded(record.PCV3Remarks),
                    MMR1Date = DecryptIfNeeded(record.MMR1Date),
                    MMR1Remarks = DecryptIfNeeded(record.MMR1Remarks),
                    MMR2Date = DecryptIfNeeded(record.MMR2Date),
                    MMR2Remarks = DecryptIfNeeded(record.MMR2Remarks)
                };

                return decrypted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting immunization record");
                return record; // Return original if decryption fails
            }
        }

        private string? DecryptIfNeeded(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            try
            {
                // Check if value is encrypted (has the encryption marker)
                if (_encryptionService.IsEncrypted(value))
                {
                    return _encryptionService.Decrypt(value);
                }
                return value;
            }
            catch
            {
                // If decryption fails, return original value
                return value;
            }
        }
    }
}
