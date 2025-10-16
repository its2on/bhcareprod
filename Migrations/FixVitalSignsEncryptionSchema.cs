using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Barangay.Migrations
{
    public partial class FixVitalSignsEncryptionSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create backup table
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VitalSigns_Backup')
                BEGIN
                    SELECT * INTO VitalSigns_Backup FROM VitalSigns;
                    PRINT 'Created VitalSigns_Backup table';
                END
            ");

            // Drop the existing VitalSigns table
            migrationBuilder.DropTable("VitalSigns");

            // Recreate VitalSigns table with string columns for encrypted numeric values
            migrationBuilder.CreateTable(
                name: "VitalSigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Temperature = table.Column<string>(type: "nvarchar(max)", nullable: true), // Changed from decimal to string
                    BloodPressure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HeartRate = table.Column<string>(type: "nvarchar(max)", nullable: true), // Changed from int to string
                    RespiratoryRate = table.Column<string>(type: "nvarchar(max)", nullable: true), // Changed from int to string
                    SpO2 = table.Column<string>(type: "nvarchar(max)", nullable: true), // Changed from decimal to string
                    Weight = table.Column<string>(type: "nvarchar(max)", nullable: true), // Changed from decimal to string
                    Height = table.Column<string>(type: "nvarchar(max)", nullable: true), // Changed from decimal to string
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSigns", x => x.Id);
                });

            // Copy data from backup with proper conversion
            migrationBuilder.Sql(@"
                INSERT INTO VitalSigns (PatientId, Temperature, BloodPressure, HeartRate, RespiratoryRate, SpO2, Weight, Height, RecordedAt, Notes)
                SELECT 
                    PatientId,
                    CASE WHEN Temperature IS NOT NULL THEN CAST(Temperature AS NVARCHAR(MAX)) ELSE NULL END,
                    BloodPressure,
                    CASE WHEN HeartRate IS NOT NULL THEN CAST(HeartRate AS NVARCHAR(MAX)) ELSE NULL END,
                    CASE WHEN RespiratoryRate IS NOT NULL THEN CAST(RespiratoryRate AS NVARCHAR(MAX)) ELSE NULL END,
                    CASE WHEN SpO2 IS NOT NULL THEN CAST(SpO2 AS NVARCHAR(MAX)) ELSE NULL END,
                    CASE WHEN Weight IS NOT NULL THEN CAST(Weight AS NVARCHAR(MAX)) ELSE NULL END,
                    CASE WHEN Height IS NOT NULL THEN CAST(Height AS NVARCHAR(MAX)) ELSE NULL END,
                    RecordedAt,
                    Notes
                FROM VitalSigns_Backup;
                PRINT 'Data copied from VitalSigns_Backup to VitalSigns with string conversion';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the current VitalSigns table
            migrationBuilder.DropTable("VitalSigns");

            // Recreate with original schema
            migrationBuilder.CreateTable(
                name: "VitalSigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Temperature = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    BloodPressure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HeartRate = table.Column<int>(type: "int", nullable: true),
                    RespiratoryRate = table.Column<int>(type: "int", nullable: true),
                    SpO2 = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSigns", x => x.Id);
                });
        }
    }
}
