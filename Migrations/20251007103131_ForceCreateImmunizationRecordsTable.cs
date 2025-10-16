using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class ForceCreateImmunizationRecordsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if table exists before creating
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ImmunizationRecords' AND xtype='U')
                BEGIN
                    CREATE TABLE [ImmunizationRecords] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [ChildName] nvarchar(4000) NOT NULL,
                        [DateOfBirth] nvarchar(4000) NOT NULL,
                        [PlaceOfBirth] nvarchar(4000) NULL,
                        [Address] nvarchar(4000) NULL,
                        [MotherName] nvarchar(4000) NOT NULL,
                        [FatherName] nvarchar(4000) NULL,
                        [Sex] nvarchar(4000) NULL,
                        [BirthHeight] nvarchar(4000) NULL,
                        [BirthWeight] nvarchar(4000) NULL,
                        [HealthCenter] nvarchar(4000) NULL,
                        [Barangay] nvarchar(4000) NULL,
                        [FamilyNumber] nvarchar(4000) NULL,
                        [Email] nvarchar(4000) NULL,
                        [ContactNumber] nvarchar(4000) NULL,
                        [BCGVaccineDate] nvarchar(4000) NULL,
                        [BCGVaccineRemarks] nvarchar(4000) NULL,
                        [HepatitisBVaccineDate] nvarchar(4000) NULL,
                        [HepatitisBVaccineRemarks] nvarchar(4000) NULL,
                        [Pentavalent1Date] nvarchar(4000) NULL,
                        [Pentavalent1Remarks] nvarchar(4000) NULL,
                        [Pentavalent2Date] nvarchar(4000) NULL,
                        [Pentavalent2Remarks] nvarchar(4000) NULL,
                        [Pentavalent3Date] nvarchar(4000) NULL,
                        [Pentavalent3Remarks] nvarchar(4000) NULL,
                        [OPV1Date] nvarchar(4000) NULL,
                        [OPV1Remarks] nvarchar(4000) NULL,
                        [OPV2Date] nvarchar(4000) NULL,
                        [OPV2Remarks] nvarchar(4000) NULL,
                        [OPV3Date] nvarchar(4000) NULL,
                        [OPV3Remarks] nvarchar(4000) NULL,
                        [IPV1Date] nvarchar(4000) NULL,
                        [IPV1Remarks] nvarchar(4000) NULL,
                        [IPV2Date] nvarchar(4000) NULL,
                        [IPV2Remarks] nvarchar(4000) NULL,
                        [PCV1Date] nvarchar(4000) NULL,
                        [PCV1Remarks] nvarchar(4000) NULL,
                        [PCV2Date] nvarchar(4000) NULL,
                        [PCV2Remarks] nvarchar(4000) NULL,
                        [PCV3Date] nvarchar(4000) NULL,
                        [PCV3Remarks] nvarchar(4000) NULL,
                        [MMR1Date] nvarchar(4000) NULL,
                        [MMR1Remarks] nvarchar(4000) NULL,
                        [MMR2Date] nvarchar(4000) NULL,
                        [MMR2Remarks] nvarchar(4000) NULL,
                        [CreatedAt] nvarchar(4000) NOT NULL,
                        [UpdatedAt] nvarchar(4000) NOT NULL,
                        [CreatedBy] nvarchar(4000) NOT NULL,
                        [UpdatedBy] nvarchar(4000) NOT NULL,
                        [Status] nvarchar(4000) NOT NULL,
                        CONSTRAINT [PK_ImmunizationRecords] PRIMARY KEY ([Id])
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
