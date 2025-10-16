using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBarangay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ValidUntil",
                table: "Prescriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicationName",
                table: "PrescriptionMedications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Removed problematic AlterColumn for EatsProcessedFood as column doesn't exist

            migrationBuilder.AddColumn<string>(
                name: "Barangay",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Guarded create to avoid duplicate table error on environments where it already exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmailVerifications')
                BEGIN
                    CREATE TABLE [EmailVerifications] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Email] NVARCHAR(255) NOT NULL,
                        [VerificationCode] NVARCHAR(10) NOT NULL,
                        [ExpiryTime] DATETIME2 NOT NULL,
                        [IsVerified] BIT NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL,
                        [VerifiedAt] DATETIME2 NULL
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FeedbackRatings')
                BEGIN
                    CREATE TABLE [FeedbackRatings] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] NVARCHAR(MAX) NOT NULL,
                        [ServiceType] NVARCHAR(MAX) NOT NULL,
                        [AppointmentId] INT NULL,
                        [Rating] INT NOT NULL,
                        [Comments] NVARCHAR(1000) NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PatientHistories')
                BEGIN
                    CREATE TABLE [PatientHistories] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [PatientId] NVARCHAR(450) NOT NULL,
                        [AppointmentId] INT NULL,
                        [DoctorId] NVARCHAR(MAX) NOT NULL,
                        [Diagnosis] NVARCHAR(500) NOT NULL,
                        [Symptoms] NTEXT NOT NULL,
                        [Treatment] NTEXT NOT NULL,
                        [Notes] NTEXT NOT NULL,
                        [Medications] NVARCHAR(500) NOT NULL,
                        [RecordDate] DATETIME2 NOT NULL,
                        [UpdatedAt] DATETIME2 NULL
                    );
                    CREATE INDEX [IX_PatientHistories_AppointmentId] ON [PatientHistories] ([AppointmentId]);
                    CREATE INDEX [IX_PatientHistories_PatientId] ON [PatientHistories] ([PatientId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailVerifications");

            migrationBuilder.DropTable(
                name: "FeedbackRatings");

            migrationBuilder.DropTable(
                name: "PatientHistories");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "MedicationName",
                table: "PrescriptionMedications");

            migrationBuilder.DropColumn(
                name: "Barangay",
                table: "AspNetUsers");

            // Removed problematic AlterColumn for EatsProcessedFood as column doesn't exist
        }
    }
}
