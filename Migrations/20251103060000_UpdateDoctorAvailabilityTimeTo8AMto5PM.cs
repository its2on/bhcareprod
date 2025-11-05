using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDoctorAvailabilityTimeTo8AMto5PM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update all existing DoctorAvailability records to use 8:00 AM - 5:00 PM schedule
            migrationBuilder.Sql(@"
                UPDATE DoctorAvailabilities 
                SET StartTime = '08:00:00', 
                    EndTime = '17:00:00',
                    MaxAppointmentsPerDay = 100,
                    SlotDurationMinutes = 5,
                    LastUpdated = GETDATE()
                WHERE StartTime != '08:00:00' OR EndTime != '17:00:00'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback to 7:30 AM - 5:00 PM if needed
            migrationBuilder.Sql(@"
                UPDATE DoctorAvailabilities 
                SET StartTime = '07:30:00', 
                    EndTime = '17:00:00',
                    MaxAppointmentsPerDay = 30,
                    SlotDurationMinutes = 18,
                    LastUpdated = GETDATE()
            ");
        }
    }
}
