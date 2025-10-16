using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Barangay.Migrations
{
    public partial class ConvertVitalSignsToStringTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create backup table
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VitalSigns_Backup_EF')
                BEGIN
                    SELECT * INTO VitalSigns_Backup_EF FROM VitalSigns;
                    PRINT 'Created VitalSigns_Backup_EF table';
                END
            ");

            // Add new nvarchar columns
            migrationBuilder.AddColumn<string>(
                name: "Temperature_New",
                table: "VitalSigns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeartRate_New",
                table: "VitalSigns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RespiratoryRate_New",
                table: "VitalSigns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpO2_New",
                table: "VitalSigns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Weight_New",
                table: "VitalSigns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Height_New",
                table: "VitalSigns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Convert data from old columns to new columns
            migrationBuilder.Sql(@"
                UPDATE VitalSigns 
                SET 
                    Temperature_New = CASE 
                        WHEN Temperature IS NOT NULL THEN CAST(Temperature AS NVARCHAR(50))
                        ELSE NULL 
                    END,
                    HeartRate_New = CASE 
                        WHEN HeartRate IS NOT NULL THEN CAST(HeartRate AS NVARCHAR(50))
                        ELSE NULL 
                    END,
                    RespiratoryRate_New = CASE 
                        WHEN RespiratoryRate IS NOT NULL THEN CAST(RespiratoryRate AS NVARCHAR(50))
                        ELSE NULL 
                    END,
                    SpO2_New = CASE 
                        WHEN SpO2 IS NOT NULL THEN CAST(SpO2 AS NVARCHAR(50))
                        ELSE NULL 
                    END,
                    Weight_New = CASE 
                        WHEN Weight IS NOT NULL THEN CAST(Weight AS NVARCHAR(50))
                        ELSE NULL 
                    END,
                    Height_New = CASE 
                        WHEN Height IS NOT NULL THEN CAST(Height AS NVARCHAR(50))
                        ELSE NULL 
                    END;
            ");

            // Drop old columns
            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "HeartRate",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "RespiratoryRate",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "SpO2",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "VitalSigns");

            // Rename new columns to original names
            migrationBuilder.RenameColumn(
                name: "Temperature_New",
                table: "VitalSigns",
                newName: "Temperature");

            migrationBuilder.RenameColumn(
                name: "HeartRate_New",
                table: "VitalSigns",
                newName: "HeartRate");

            migrationBuilder.RenameColumn(
                name: "RespiratoryRate_New",
                table: "VitalSigns",
                newName: "RespiratoryRate");

            migrationBuilder.RenameColumn(
                name: "SpO2_New",
                table: "VitalSigns",
                newName: "SpO2");

            migrationBuilder.RenameColumn(
                name: "Weight_New",
                table: "VitalSigns",
                newName: "Weight");

            migrationBuilder.RenameColumn(
                name: "Height_New",
                table: "VitalSigns",
                newName: "Height");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the original columns
            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "VitalSigns",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeartRate",
                table: "VitalSigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RespiratoryRate",
                table: "VitalSigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpO2",
                table: "VitalSigns",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "VitalSigns",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Height",
                table: "VitalSigns",
                type: "decimal(5,2)",
                nullable: true);

            // Convert data back (this may cause data loss if conversion fails)
            migrationBuilder.Sql(@"
                UPDATE VitalSigns 
                SET 
                    Temperature = CASE 
                        WHEN ISNUMERIC(Temperature) = 1 THEN CAST(Temperature AS DECIMAL(5,2))
                        ELSE NULL 
                    END,
                    HeartRate = CASE 
                        WHEN ISNUMERIC(HeartRate) = 1 THEN CAST(HeartRate AS INT)
                        ELSE NULL 
                    END,
                    RespiratoryRate = CASE 
                        WHEN ISNUMERIC(RespiratoryRate) = 1 THEN CAST(RespiratoryRate AS INT)
                        ELSE NULL 
                    END,
                    SpO2 = CASE 
                        WHEN ISNUMERIC(SpO2) = 1 THEN CAST(SpO2 AS DECIMAL(5,2))
                        ELSE NULL 
                    END,
                    Weight = CASE 
                        WHEN ISNUMERIC(Weight) = 1 THEN CAST(Weight AS DECIMAL(5,2))
                        ELSE NULL 
                    END,
                    Height = CASE 
                        WHEN ISNUMERIC(Height) = 1 THEN CAST(Height AS DECIMAL(5,2))
                        ELSE NULL 
                    END;
            ");

            // Drop the string columns
            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "HeartRate",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "RespiratoryRate",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "SpO2",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "VitalSigns");

            // Restore original column names and types
            migrationBuilder.AlterColumn<decimal>(
                name: "Temperature",
                table: "VitalSigns",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HeartRate",
                table: "VitalSigns",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RespiratoryRate",
                table: "VitalSigns",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "SpO2",
                table: "VitalSigns",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "VitalSigns",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Height",
                table: "VitalSigns",
                type: "decimal(5,2)",
                nullable: true);
        }
    }
}

