using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureHEEADSSSColumnTypes_Clean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UpdatedAt",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlcoholPerOccasion",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            // Normalize boolean-like string columns to '1'/'0' before altering to BIT
            migrationBuilder.Sql(@"
UPDATE HEEADSSSAssessments
SET
    WeightConcerns = CASE 
        WHEN WeightConcerns IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(WeightConcerns))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(WeightConcerns))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END,
    SuicidalThoughts = CASE 
        WHEN SuicidalThoughts IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(SuicidalThoughts))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(SuicidalThoughts))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END,
    SubstanceUse = CASE 
        WHEN SubstanceUse IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(SubstanceUse))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(SubstanceUse))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END,
    SexualActivity = CASE 
        WHEN SexualActivity IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(SexualActivity))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(SexualActivity))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END,
    SelfHarmBehavior = CASE 
        WHEN SelfHarmBehavior IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(SelfHarmBehavior))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(SelfHarmBehavior))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END,
    MoodChanges = CASE 
        WHEN MoodChanges IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(MoodChanges))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(MoodChanges))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END,
    FeelsSafeAtSchool = CASE 
        WHEN FeelsSafeAtSchool IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(FeelsSafeAtSchool))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(FeelsSafeAtSchool))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END,
    FeelsSafeAtHome = CASE 
        WHEN FeelsSafeAtHome IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(FeelsSafeAtHome))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(FeelsSafeAtHome))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END,
    ExperiencedBullying = CASE 
        WHEN ExperiencedBullying IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(ExperiencedBullying))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(ExperiencedBullying))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END,
    EatingDisorderSymptoms = CASE 
        WHEN EatingDisorderSymptoms IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(EatingDisorderSymptoms))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(EatingDisorderSymptoms))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END,
    AttendanceIssues = CASE 
        WHEN AttendanceIssues IS NULL THEN NULL
        WHEN UPPER(LTRIM(RTRIM(AttendanceIssues))) IN ('1','TRUE','YES','Y','T','ON') THEN '1'
        WHEN UPPER(LTRIM(RTRIM(AttendanceIssues))) IN ('0','FALSE','NO','N','F','OFF') THEN '0'
        ELSE NULL END;
");

            migrationBuilder.AlterColumn<bool>(
                name: "WeightConcerns",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "SuicidalThoughts",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "SubstanceUse",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "SexualActivity",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "SelfHarmBehavior",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "MoodChanges",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "FeelsSafeAtSchool",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "FeelsSafeAtHome",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "ExperiencedBullying",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "EatingDisorderSymptoms",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "AttendanceIssues",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // Add missing columns to HEEADSSSAssessments only if they don't already exist
            migrationBuilder.Sql(@"
IF COL_LENGTH('HEEADSSSAssessments','Is4Ps') IS NULL ALTER TABLE HEEADSSSAssessments ADD Is4Ps nvarchar(max) NULL;
IF COL_LENGTH('HEEADSSSAssessments','IsNHPTS') IS NULL ALTER TABLE HEEADSSSAssessments ADD IsNHPTS nvarchar(max) NULL;
IF COL_LENGTH('HEEADSSSAssessments','IsOwnPhilHealth') IS NULL ALTER TABLE HEEADSSSAssessments ADD IsOwnPhilHealth nvarchar(max) NULL;
IF COL_LENGTH('HEEADSSSAssessments','IsPhilHealthBeneficiaryOnly') IS NULL ALTER TABLE HEEADSSSAssessments ADD IsPhilHealthBeneficiaryOnly nvarchar(max) NULL;
IF COL_LENGTH('HEEADSSSAssessments','PhilHealthPIN') IS NULL ALTER TABLE HEEADSSSAssessments ADD PhilHealthPIN nvarchar(max) NULL;
");
            // Sanitize BirthDate values to ensure successful conversion from NVARCHAR to DATETIME2
            migrationBuilder.Sql(@"UPDATE u SET BirthDate = NULL FROM AspNetUsers u WHERE TRY_CONVERT(datetime2, u.BirthDate) IS NULL AND u.BirthDate IS NOT NULL;");
            migrationBuilder.AlterColumn<DateTime>(
                name: "BirthDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
            // Create EmailSuspensions table only if it doesn't already exist (idempotent)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EmailSuspensions' AND xtype='U')
BEGIN
    CREATE TABLE [EmailSuspensions] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [FailureCount] int NOT NULL,
        [LastFailureDate] datetime2 NOT NULL,
        [SuspensionStartDate] datetime2 NULL,
        [SuspensionEndDate] datetime2 NULL,
        [SuspensionReason] nvarchar(50) NOT NULL,
        [SuspensionLevel] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_EmailSuspensions] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_EmailSuspensions_Email] ON [EmailSuspensions] ([Email]);
    CREATE INDEX [IX_EmailSuspensions_IsActive] ON [EmailSuspensions] ([IsActive]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSuspensions");

            migrationBuilder.DropColumn(
                name: "AlcoholPerOccasion",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "Is4Ps",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "IsNHPTS",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "IsOwnPhilHealth",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "IsPhilHealthBeneficiaryOnly",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "PhilHealthPIN",
                table: "HEEADSSSAssessments");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedAt",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "WeightConcerns",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SuicidalThoughts",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SubstanceUse",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SexualActivity",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SelfHarmBehavior",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MoodChanges",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FeelsSafeAtSchool",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FeelsSafeAtHome",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExperiencedBullying",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EatingDisorderSymptoms",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AttendanceIssues",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BirthDate",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
