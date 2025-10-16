using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddNCDRiskAssessmentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancerMedication",
                table: "NCDRiskAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancerYear",
                table: "NCDRiskAssessments",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CivilStatus",
                table: "NCDRiskAssessments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiabetesMedication",
                table: "NCDRiskAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiabetesYear",
                table: "NCDRiskAssessments",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryCancerFather",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryCancerMother",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryCancerSibling",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryDiabetesFather",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryDiabetesMother",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryDiabetesSibling",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryHeartDiseaseFather",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryHeartDiseaseMother",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryHeartDiseaseSibling",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryLungDiseaseFather",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryLungDiseaseMother",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryLungDiseaseSibling",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FamilyHistoryOther",
                table: "NCDRiskAssessments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryOtherFather",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryOtherMother",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryOtherSibling",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryStrokeFather",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryStrokeMother",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FamilyHistoryStrokeSibling",
                table: "NCDRiskAssessments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "NCDRiskAssessments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HypertensionMedication",
                table: "NCDRiskAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HypertensionYear",
                table: "NCDRiskAssessments",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "NCDRiskAssessments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LungDiseaseMedication",
                table: "NCDRiskAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LungDiseaseYear",
                table: "NCDRiskAssessments",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "NCDRiskAssessments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "NCDRiskAssessments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CivilStatus",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Religion",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserNumber",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancerMedication",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "CancerYear",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "CivilStatus",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "DiabetesMedication",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "DiabetesYear",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryCancerFather",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryCancerMother",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryCancerSibling",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryDiabetesFather",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryDiabetesMother",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryDiabetesSibling",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryHeartDiseaseFather",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryHeartDiseaseMother",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryHeartDiseaseSibling",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryLungDiseaseFather",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryLungDiseaseMother",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryLungDiseaseSibling",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryOther",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryOtherFather",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryOtherMother",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryOtherSibling",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryStrokeFather",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryStrokeMother",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FamilyHistoryStrokeSibling",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "HypertensionMedication",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "HypertensionYear",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "LungDiseaseMedication",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "LungDiseaseYear",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "CivilStatus",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Religion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserNumber",
                table: "AspNetUsers");
        }
    }
}
