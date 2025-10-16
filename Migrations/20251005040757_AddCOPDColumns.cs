using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddCOPDColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "COPDMedication",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "COPDYear",
                table: "NCDRiskAssessments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancerSite",
                table: "NCDRiskAssessments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmokingQuitDuration",
                table: "NCDRiskAssessments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "COPDMedication",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "COPDYear",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "CancerSite",
                table: "NCDRiskAssessments");

            migrationBuilder.DropColumn(
                name: "SmokingQuitDuration",
                table: "NCDRiskAssessments");
        }
    }
}
