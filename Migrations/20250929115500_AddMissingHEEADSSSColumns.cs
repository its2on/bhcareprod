using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingHEEADSSSColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing columns to HEEADSSSAssessments table

            // BMI related columns
            migrationBuilder.AddColumn<string>(
                name: "BMI",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BMINormal",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BMIObese",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BMIOverweight",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BMIUnderweight",
                table: "HEEADSSSAssessments",
                type: "bit",
                nullable: true);

            // Height and Weight
            migrationBuilder.AddColumn<string>(
                name: "Height",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Weight",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            // Immunization columns
            migrationBuilder.AddColumn<string>(
                name: "ImmunizationHPV",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImmunizationMR",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImmunizationTd",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            // Female specific columns
            migrationBuilder.AddColumn<string>(
                name: "AgeOfFirstPregnancy",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DateOfMenarche",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OBScore",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            // Vital signs
            migrationBuilder.AddColumn<string>(
                name: "VitalBP",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VitalPR",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VitalRR",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VitalTemp",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            // Medical information
            migrationBuilder.AddColumn<string>(
                name: "ChiefComplaint",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyHistory",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpDate",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HistoryOfPresentIllness",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Management",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PastMedicalHistory",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhysicalExaminationFindings",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonForReferral",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferredTo",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingDiagnosis",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            // Additional columns that might be missing
            migrationBuilder.AddColumn<string>(
                name: "EducationCurrentlyStudying",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeEnvironment",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeFamilyProblems",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeParentalListening",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolPerformance",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttendanceIssues",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CareerPlans",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hobbies",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhysicalActivity",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreenTime",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivitiesRegularExercise",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            // ContactNumber if missing
            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            // Address if missing
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            // Gender if missing
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            // Age if missing (as string)
            migrationBuilder.AddColumn<string>(
                name: "Age",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            // FullName if missing
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the added columns
            migrationBuilder.DropColumn(name: "BMI", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "BMINormal", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "BMIObese", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "BMIOverweight", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "BMIUnderweight", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "Height", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "Weight", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "ImmunizationHPV", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "ImmunizationMR", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "ImmunizationTd", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "AgeOfFirstPregnancy", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "DateOfMenarche", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "OBScore", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "VitalBP", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "VitalPR", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "VitalRR", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "VitalTemp", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "ChiefComplaint", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "FamilyHistory", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "FollowUpDate", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "HistoryOfPresentIllness", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "Management", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "PastMedicalHistory", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "PhysicalExaminationFindings", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "ReasonForReferral", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "ReferredTo", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "WorkingDiagnosis", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "EducationCurrentlyStudying", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "HomeEnvironment", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "HomeFamilyProblems", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "HomeParentalListening", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "SchoolPerformance", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "AttendanceIssues", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "CareerPlans", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "Hobbies", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "PhysicalActivity", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "ScreenTime", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "ActivitiesRegularExercise", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "ContactNumber", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "Address", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "Gender", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "Age", table: "HEEADSSSAssessments");
            migrationBuilder.DropColumn(name: "FullName", table: "HEEADSSSAssessments");
        }
    }
}
