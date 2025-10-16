using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingHEEADSSSColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivitiesInternetGadgetUse",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DrugsStreetDrugs",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationBullyingExperience",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeRunawayThoughts",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafetyWeaponAccess",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SexualityBisexual",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SexualityGay",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SexualityLesbian",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivitiesInternetGadgetUse",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "DrugsStreetDrugs",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "EducationBullyingExperience",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "HomeRunawayThoughts",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "SafetyWeaponAccess",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "SexualityBisexual",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "SexualityGay",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "SexualityLesbian",
                table: "HEEADSSSAssessments");
        }
    }
}
