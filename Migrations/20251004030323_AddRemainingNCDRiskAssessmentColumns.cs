using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingNCDRiskAssessmentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlchoholTypeBeer",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlchoholTypeWhisky",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlchoholTypeWine",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlcoholInom",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlcoholOkasyon",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeerConsumption1",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeerConsumption2",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeerConsumption3",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DateAssessment",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EhersisyoDuration",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EhersisyoRegular",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EhersisyoType",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HasEyeDiseaseCondition",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HasLungDiseaseNonInfectious",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthFacilityName",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutrisyonKumakainMamantika",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutrisyonKumakainMatatamis",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutrisyonMadalasGulay",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutrisyonMadalasIsda",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutrisyonMadalasKarne",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutrisyonMadalasPratas",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pananakit21",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pananakit22",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pananakit23",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pananakit24",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pananakit25",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pananakit26",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pananakit27",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pananakit28",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigarilyoKadami",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigarilyoSticks",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigarilyoTumigil",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigarilyoUsok",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StressEpekto",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StressMadalas",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StressSino",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhiskyConsumption1",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhiskyConsumption2",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WineConsumption1",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WineConsumption2",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlchoholTypeBeer",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "AlchoholTypeWhisky",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "AlchoholTypeWine",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "AlcoholInom",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "AlcoholOkasyon",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "BeerConsumption1",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "BeerConsumption2",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "BeerConsumption3",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "DateAssessment",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "EhersisyoDuration",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "EhersisyoRegular",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "EhersisyoType",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "HasEyeDiseaseCondition",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "HasLungDiseaseNonInfectious",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "HealthFacilityName",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "NutrisyonKumakainMamantika",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "NutrisyonKumakainMatatamis",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "NutrisyonMadalasGulay",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "NutrisyonMadalasIsda",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "NutrisyonMadalasKarne",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "NutrisyonMadalasPratas",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "Pananakit21",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "Pananakit22",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "Pananakit23",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "Pananakit24",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "Pananakit25",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "Pananakit26",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "Pananakit27",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "Pananakit28",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "SigarilyoKadami",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "SigarilyoSticks",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "SigarilyoTumigil",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "SigarilyoUsok",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "StressEpekto",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "StressMadalas",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "StressSino",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "WhiskyConsumption1",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "WhiskyConsumption2",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "WineConsumption1",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "WineConsumption2",
                table: "NCDRiskAssessments");
        }
    }
}
