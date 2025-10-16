using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSuspensionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EmailSuspensions' AND xtype='U')
                BEGIN
                    CREATE TABLE [EmailSuspensions] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [Email] nvarchar(255) NOT NULL,
                        [FailureCount] int NOT NULL,
                        [LastFailureDate] datetime2 NOT NULL,
                        [SuspensionStartDate] datetime2 NULL,
                        [SuspensionEndDate] datetime2 NULL,
                        [SuspensionReason] nvarchar(50) NULL,
                        [SuspensionLevel] nvarchar(50) NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_EmailSuspensions] PRIMARY KEY ([Id])
                    );
                    
                    CREATE INDEX [IX_EmailSuspensions_Email] ON [EmailSuspensions] ([Email]);
                    CREATE INDEX [IX_EmailSuspensions_IsActive] ON [EmailSuspensions] ([IsActive]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sysobjects WHERE name='EmailSuspensions' AND xtype='U')
                BEGIN
                    DROP TABLE [EmailSuspensions];
                END
            ");
        }
    }
}
