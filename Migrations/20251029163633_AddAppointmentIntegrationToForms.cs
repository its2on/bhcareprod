using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentIntegrationToForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxAge",
                table: "FormTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinAge",
                table: "FormTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInAppointmentFlow",
                table: "FormTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "FormSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_AppointmentId",
                table: "FormSubmissions",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormSubmissions_Appointments_AppointmentId",
                table: "FormSubmissions",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormSubmissions_Appointments_AppointmentId",
                table: "FormSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_FormSubmissions_AppointmentId",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "MaxAge",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "MinAge",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "ShowInAppointmentFlow",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "FormSubmissions");
        }
    }
}
