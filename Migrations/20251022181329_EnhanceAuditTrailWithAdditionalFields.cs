using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceAuditTrailWithAdditionalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalContext",
                table: "AuditTrails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceInfo",
                table: "AuditTrails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "AuditTrails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "AuditTrails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestMethod",
                table: "AuditTrails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestUrl",
                table: "AuditTrails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "AuditTrails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalContext",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "DeviceInfo",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "RequestMethod",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "RequestUrl",
                table: "AuditTrails");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "AuditTrails");
        }
    }
}
