using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingNCDColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the remaining missing columns to NCDRiskAssessments table
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.NCDRiskAssessments','AlcoholStoppedDuration') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [AlcoholStoppedDuration] NVARCHAR(4000) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','EyeDiseaseMedication') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [EyeDiseaseMedication] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','EyeDiseaseYear') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [EyeDiseaseYear] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','FamilyHistoryEyeDiseaseFather') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [FamilyHistoryEyeDiseaseFather] NVARCHAR(4000) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','FamilyHistoryEyeDiseaseMother') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [FamilyHistoryEyeDiseaseMother] NVARCHAR(4000) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','FamilyHistoryEyeDiseaseSibling') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [FamilyHistoryEyeDiseaseSibling] NVARCHAR(4000) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','FamilyHistoryKidneyDiseaseFather') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [FamilyHistoryKidneyDiseaseFather] NVARCHAR(4000) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','FamilyHistoryKidneyDiseaseMother') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [FamilyHistoryKidneyDiseaseMother] NVARCHAR(4000) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','FamilyHistoryKidneyDiseaseSibling') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [FamilyHistoryKidneyDiseaseSibling] NVARCHAR(4000) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasEnoughExercise') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasEnoughExercise] NVARCHAR(4000) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','IDNo') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [IDNo] NVARCHAR(4000) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','Smoked100Sticks') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [Smoked100Sticks] NVARCHAR(4000) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the added columns
            migrationBuilder.DropColumn(name: "AlcoholStoppedDuration", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "EyeDiseaseMedication", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "EyeDiseaseYear", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "FamilyHistoryEyeDiseaseFather", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "FamilyHistoryEyeDiseaseMother", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "FamilyHistoryEyeDiseaseSibling", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "FamilyHistoryKidneyDiseaseFather", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "FamilyHistoryKidneyDiseaseMother", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "FamilyHistoryKidneyDiseaseSibling", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasEnoughExercise", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "IDNo", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "Smoked100Sticks", table: "NCDRiskAssessments");
        }
    }
}
