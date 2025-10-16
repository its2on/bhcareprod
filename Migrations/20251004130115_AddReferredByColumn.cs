using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddReferredByColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ReferredBy column if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'ReferredBy')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments ADD ReferredBy NVARCHAR(MAX) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop ReferredBy column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'HEEADSSSAssessments' AND COLUMN_NAME = 'ReferredBy')
                BEGIN
                    ALTER TABLE HEEADSSSAssessments DROP COLUMN ReferredBy;
                END
            ");
        }
    }
}
