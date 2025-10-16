using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Barangay.Data;
using Barangay.Services;
using System.Collections.Generic;

namespace Barangay.Migrations
{
    public partial class EncryptExistingDataMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration will be handled programmatically in the application
            // to ensure proper encryption of existing data
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration cannot be reversed as it would require decryption
            // which should be handled through the application's encryption service
        }
    }

    public class EncryptExistingDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataEncryptionService _encryptionService;

        public EncryptExistingDataService(ApplicationDbContext context, IDataEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task EncryptAllExistingDataAsync()
        {
            try
            {
                Console.WriteLine("Starting encryption of existing data...");

                // Encrypt Patients data
                await EncryptPatientsDataAsync();
                
                // Encrypt Medical Records data
                await EncryptMedicalRecordsDataAsync();
                
                // Encrypt Prescriptions data
                await EncryptPrescriptionsDataAsync();
                
                // Encrypt Appointments data
                await EncryptAppointmentsDataAsync();
                
                // Encrypt ApplicationUsers data
                await EncryptApplicationUsersDataAsync();
                
                // Encrypt Vital Signs data
                await EncryptVitalSignsDataAsync();
                
                // Encrypt Lab Results data
                await EncryptLabResultsDataAsync();

                await _context.SaveChangesAsync();
                Console.WriteLine("Data encryption completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during data encryption: {ex.Message}");
                throw;
            }
        }

        private async Task EncryptPatientsDataAsync()
        {
            var patients = await _context.Patients.ToListAsync();
            Console.WriteLine($"Encrypting {patients.Count} patient records...");

            foreach (var patient in patients)
            {
                if (!string.IsNullOrEmpty(patient.FullName))
                    patient.FullName = _encryptionService.Encrypt(patient.FullName);
                
                if (!string.IsNullOrEmpty(patient.Address))
                    patient.Address = _encryptionService.Encrypt(patient.Address);
                
                if (!string.IsNullOrEmpty(patient.ContactNumber))
                    patient.ContactNumber = _encryptionService.Encrypt(patient.ContactNumber);
                
                if (!string.IsNullOrEmpty(patient.EmergencyContact))
                    patient.EmergencyContact = _encryptionService.Encrypt(patient.EmergencyContact);
                
                if (!string.IsNullOrEmpty(patient.EmergencyContactNumber))
                    patient.EmergencyContactNumber = _encryptionService.Encrypt(patient.EmergencyContactNumber);
                
                if (!string.IsNullOrEmpty(patient.Email))
                    patient.Email = _encryptionService.Encrypt(patient.Email);
                
                if (!string.IsNullOrEmpty(patient.Diagnosis))
                    patient.Diagnosis = _encryptionService.Encrypt(patient.Diagnosis);
                
                if (!string.IsNullOrEmpty(patient.Alert))
                    patient.Alert = _encryptionService.Encrypt(patient.Alert);
                
                if (!string.IsNullOrEmpty(patient.Allergies))
                    patient.Allergies = _encryptionService.Encrypt(patient.Allergies);
                
                if (!string.IsNullOrEmpty(patient.MedicalHistory))
                    patient.MedicalHistory = _encryptionService.Encrypt(patient.MedicalHistory);
                
                if (!string.IsNullOrEmpty(patient.CurrentMedications))
                    patient.CurrentMedications = _encryptionService.Encrypt(patient.CurrentMedications);
            }
        }

        private async Task EncryptMedicalRecordsDataAsync()
        {
            var medicalRecords = await _context.MedicalRecords.ToListAsync();
            Console.WriteLine($"Encrypting {medicalRecords.Count} medical record entries...");

            foreach (var record in medicalRecords)
            {
                if (!string.IsNullOrEmpty(record.Diagnosis))
                    record.Diagnosis = _encryptionService.Encrypt(record.Diagnosis);
                
                if (!string.IsNullOrEmpty(record.Treatment))
                    record.Treatment = _encryptionService.Encrypt(record.Treatment);
                
                if (!string.IsNullOrEmpty(record.Notes))
                    record.Notes = _encryptionService.Encrypt(record.Notes);
                
                if (!string.IsNullOrEmpty(record.ChiefComplaint))
                    record.ChiefComplaint = _encryptionService.Encrypt(record.ChiefComplaint);
                
                if (!string.IsNullOrEmpty(record.Duration))
                    record.Duration = _encryptionService.Encrypt(record.Duration);
                
                if (!string.IsNullOrEmpty(record.Medications))
                    record.Medications = _encryptionService.Encrypt(record.Medications);
                
                if (!string.IsNullOrEmpty(record.Prescription))
                    record.Prescription = _encryptionService.Encrypt(record.Prescription);
                
                if (!string.IsNullOrEmpty(record.Instructions))
                    record.Instructions = _encryptionService.Encrypt(record.Instructions);
            }
        }

        private async Task EncryptPrescriptionsDataAsync()
        {
            var prescriptions = await _context.Prescriptions.ToListAsync();
            Console.WriteLine($"Encrypting {prescriptions.Count} prescription records...");

            foreach (var prescription in prescriptions)
            {
                if (!string.IsNullOrEmpty(prescription.Diagnosis))
                    prescription.Diagnosis = _encryptionService.Encrypt(prescription.Diagnosis);
                
                if (!string.IsNullOrEmpty(prescription.Notes))
                    prescription.Notes = _encryptionService.Encrypt(prescription.Notes);
            }
        }

        private async Task EncryptAppointmentsDataAsync()
        {
            var appointments = await _context.Appointments.ToListAsync();
            Console.WriteLine($"Encrypting {appointments.Count} appointment records...");

            foreach (var appointment in appointments)
            {
                if (!string.IsNullOrEmpty(appointment.PatientName))
                    appointment.PatientName = _encryptionService.Encrypt(appointment.PatientName);
                
                if (!string.IsNullOrEmpty(appointment.DependentFullName))
                    appointment.DependentFullName = _encryptionService.Encrypt(appointment.DependentFullName);
                
                if (!string.IsNullOrEmpty(appointment.ContactNumber))
                    appointment.ContactNumber = _encryptionService.Encrypt(appointment.ContactNumber);
                
                if (!string.IsNullOrEmpty(appointment.Address))
                    appointment.Address = _encryptionService.Encrypt(appointment.Address);
                
                if (!string.IsNullOrEmpty(appointment.EmergencyContact))
                    appointment.EmergencyContact = _encryptionService.Encrypt(appointment.EmergencyContact);
                
                if (!string.IsNullOrEmpty(appointment.EmergencyContactNumber))
                    appointment.EmergencyContactNumber = _encryptionService.Encrypt(appointment.EmergencyContactNumber);
                
                if (!string.IsNullOrEmpty(appointment.Allergies))
                    appointment.Allergies = _encryptionService.Encrypt(appointment.Allergies);
                
                if (!string.IsNullOrEmpty(appointment.MedicalHistory))
                    appointment.MedicalHistory = _encryptionService.Encrypt(appointment.MedicalHistory);
                
                if (!string.IsNullOrEmpty(appointment.CurrentMedications))
                    appointment.CurrentMedications = _encryptionService.Encrypt(appointment.CurrentMedications);
                
                if (!string.IsNullOrEmpty(appointment.Description))
                    appointment.Description = _encryptionService.Encrypt(appointment.Description);
                
                if (!string.IsNullOrEmpty(appointment.ReasonForVisit))
                    appointment.ReasonForVisit = _encryptionService.Encrypt(appointment.ReasonForVisit);
                
                if (!string.IsNullOrEmpty(appointment.Prescription))
                    appointment.Prescription = _encryptionService.Encrypt(appointment.Prescription);
                
                if (!string.IsNullOrEmpty(appointment.Instructions))
                    appointment.Instructions = _encryptionService.Encrypt(appointment.Instructions);
            }
        }

        private async Task EncryptApplicationUsersDataAsync()
        {
            var users = await _context.Users.ToListAsync();
            Console.WriteLine($"Encrypting {users.Count} user records...");

            foreach (var user in users)
            {
                if (!string.IsNullOrEmpty(user.Name))
                    user.Name = _encryptionService.Encrypt(user.Name);
                
                if (!string.IsNullOrEmpty(user.Address))
                    user.Address = _encryptionService.Encrypt(user.Address);
                
                if (!string.IsNullOrEmpty(user.PhilHealthId))
                    user.PhilHealthId = _encryptionService.Encrypt(user.PhilHealthId);
                
                if (!string.IsNullOrEmpty(user.FirstName))
                    user.FirstName = _encryptionService.Encrypt(user.FirstName);
                
                if (!string.IsNullOrEmpty(user.MiddleName))
                    user.MiddleName = _encryptionService.Encrypt(user.MiddleName);
                
                if (!string.IsNullOrEmpty(user.LastName))
                    user.LastName = _encryptionService.Encrypt(user.LastName);
            }
        }

        private async Task EncryptVitalSignsDataAsync()
        {
            var vitalSigns = await _context.VitalSigns.ToListAsync();
            Console.WriteLine($"Encrypting {vitalSigns.Count} vital signs records...");

            foreach (var vitalSign in vitalSigns)
            {
                // Encrypt Temperature
                if (!string.IsNullOrEmpty(vitalSign.Temperature))
                {
                    vitalSign.EncryptedTemperature = _encryptionService.Encrypt(vitalSign.Temperature);
                }
                
                // Encrypt BloodPressure
                if (!string.IsNullOrEmpty(vitalSign.BloodPressure))
                    vitalSign.EncryptedBloodPressure = _encryptionService.Encrypt(vitalSign.BloodPressure);
                
                // Encrypt HeartRate
                if (!string.IsNullOrEmpty(vitalSign.HeartRate))
                {
                    vitalSign.EncryptedHeartRate = _encryptionService.Encrypt(vitalSign.HeartRate);
                }
                
                // Encrypt RespiratoryRate
                if (!string.IsNullOrEmpty(vitalSign.RespiratoryRate))
                {
                    vitalSign.EncryptedRespiratoryRate = _encryptionService.Encrypt(vitalSign.RespiratoryRate);
                }
                
                // Encrypt SpO2
                if (!string.IsNullOrEmpty(vitalSign.SpO2))
                {
                    vitalSign.EncryptedSpO2 = _encryptionService.Encrypt(vitalSign.SpO2);
                }
                
                // Encrypt Weight
                if (!string.IsNullOrEmpty(vitalSign.Weight))
                {
                    vitalSign.EncryptedWeight = _encryptionService.Encrypt(vitalSign.Weight);
                }
                
                // Encrypt Height
                if (!string.IsNullOrEmpty(vitalSign.Height))
                {
                    vitalSign.EncryptedHeight = _encryptionService.Encrypt(vitalSign.Height);
                }
                
                // Encrypt Notes
                if (!string.IsNullOrEmpty(vitalSign.Notes))
                    vitalSign.Notes = _encryptionService.Encrypt(vitalSign.Notes);
            }
        }

        private async Task EncryptLabResultsDataAsync()
        {
            var labResults = await _context.LabResults.ToListAsync();
            Console.WriteLine($"Encrypting {labResults.Count} lab result records...");

            foreach (var labResult in labResults)
            {
                if (!string.IsNullOrEmpty(labResult.TestName))
                    labResult.TestName = _encryptionService.Encrypt(labResult.TestName);
                
                if (!string.IsNullOrEmpty(labResult.Result))
                    labResult.Result = _encryptionService.Encrypt(labResult.Result);
                
                if (!string.IsNullOrEmpty(labResult.Notes))
                    labResult.Notes = _encryptionService.Encrypt(labResult.Notes);
            }
        }
    }
}
