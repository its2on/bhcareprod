using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddEatingHabitsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EatingDietPills",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EatingLaxatives",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EatingStarvation",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EatingVomiting",
                table: "HEEADSSSAssessments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EatingDietPills",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "EatingLaxatives",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "EatingStarvation",
                table: "HEEADSSSAssessments");

            migrationBuilder.DropColumn(
                name: "EatingVomiting",
                table: "HEEADSSSAssessments");
        }
    }
}
