using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingHEEADSSSColumnsSafely : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns only if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'ActivitiesInternetGadgetUse')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments ADD ActivitiesInternetGadgetUse NVARCHAR(MAX) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'EducationBullyingExperience')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments ADD EducationBullyingExperience NVARCHAR(MAX) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'HomeRunawayThoughts')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments ADD HomeRunawayThoughts NVARCHAR(MAX) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'SafetyWeaponAccess')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments ADD SafetyWeaponAccess NVARCHAR(MAX) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'SexualityGay')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments ADD SexualityGay NVARCHAR(MAX) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'SexualityLesbian')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments ADD SexualityLesbian NVARCHAR(MAX) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'SexualityBisexual')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments ADD SexualityBisexual NVARCHAR(MAX) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'DrugsStreetDrugs')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments ADD DrugsStreetDrugs NVARCHAR(MAX) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop columns if they exist
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'ActivitiesInternetGadgetUse')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments DROP COLUMN ActivitiesInternetGadgetUse;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'EducationBullyingExperience')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments DROP COLUMN EducationBullyingExperience;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'HomeRunawayThoughts')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments DROP COLUMN HomeRunawayThoughts;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'SafetyWeaponAccess')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments DROP COLUMN SafetyWeaponAccess;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'SexualityGay')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments DROP COLUMN SexualityGay;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'SexualityLesbian')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments DROP COLUMN SexualityLesbian;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'SexualityBisexual')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments DROP COLUMN SexualityBisexual;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'DrugsStreetDrugs')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments DROP COLUMN DrugsStreetDrugs;
                END
            ");
        }
    }
}
