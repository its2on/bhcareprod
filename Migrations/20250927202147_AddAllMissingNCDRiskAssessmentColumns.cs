using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddAllMissingNCDRiskAssessmentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add all missing columns to NCDRiskAssessments table (idempotent for Azure SQL)
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.NCDRiskAssessments','AlcoholAmount1Bottle320ml') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [AlcoholAmount1Bottle320ml] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','AlcoholAmount2Bottle640ml') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [AlcoholAmount2Bottle640ml] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','AlcoholAmount3to4WineGlasses300ml') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [AlcoholAmount3to4WineGlasses300ml] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','AlcoholAmountLessThan3Shot45ml') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [AlcoholAmountLessThan3Shot45ml] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','AlcoholAmountMoreThan4Shots75ml') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [AlcoholAmountMoreThan4Shots75ml] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','AlcoholFrequency1to3TimesPerWeek') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [AlcoholFrequency1to3TimesPerWeek] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','AlcoholFrequencyMoreThan4TimesPerWeek') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [AlcoholFrequencyMoreThan4TimesPerWeek] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','AssessmentDate') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [AssessmentDate] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','BMI') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [BMI] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','BMIStatus') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [BMIStatus] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','BPStatus') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [BPStatus] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','BaselineBP') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [BaselineBP] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','BloodSugarStatus') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [BloodSugarStatus] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','BreastCancerScreened') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [BreastCancerScreened] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','CancerScreeningStatus') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [CancerScreeningStatus] NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','CervicalCancerScreened') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [CervicalCancerScreened] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','ChestPainSpreadsToArm') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [ChestPainSpreadsToArm] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','CholesterolResult') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [CholesterolResult] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','CholesterolStatus') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [CholesterolStatus] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','CombinationExercise') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [CombinationExercise] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','DateOfAssessment') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [DateOfAssessment] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','Designation') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [Designation] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','DoctorName') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [DoctorName] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','DrinksAlcohol') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [DrinksAlcohol] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','DrinksBeer') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [DrinksBeer] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','DrinksWhiskyGinBrandy') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [DrinksWhiskyGinBrandy] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','DrinksWine') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [DrinksWine] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','EatsFattyFoodMoreThan2TimesPerWeek') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [EatsFattyFoodMoreThan2TimesPerWeek] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','EatsFishDaily') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [EatsFishDaily] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','EatsFruitsDaily') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [EatsFruitsDaily] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','EatsMeatDaily') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [EatsMeatDaily] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','EatsOilyFoodMoreThan2TimesPerWeek') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [EatsOilyFoodMoreThan2TimesPerWeek] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','EatsSweetFoodMoreThan2TimesPerWeek') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [EatsSweetFoodMoreThan2TimesPerWeek] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','EatsVegetablesDaily') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [EatsVegetablesDaily] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','FastingBloodSugar') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [FastingBloodSugar] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','FormerSmoker') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [FormerSmoker] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasChestPain') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasChestPain] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasHighSaltIntake') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasHighSaltIntake] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasHistoryOfSmoking') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasHistoryOfSmoking] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasPolydipsia') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasPolydipsia] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasPolyphagia') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasPolyphagia] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasPolyuria') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasPolyuria] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasStress') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasStress] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasUnhealthyDiet') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasUnhealthyDiet] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasUrineKetones') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasUrineKetones] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasUrineProtein') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasUrineProtein] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','HasWeightLoss') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [HasWeightLoss] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','Height') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [Height] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','Hip') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [Hip] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','IDNumber') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [IDNumber] NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','InsufficientPhysicalActivity') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [InsufficientPhysicalActivity] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','InterviewedBy') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [InterviewedBy] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','IsBingeDrinker') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [IsBingeDrinker] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','LeftArmMeanBP') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [LeftArmMeanBP] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','LossOfConsciousnessLessThan10Min') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [LossOfConsciousnessLessThan10Min] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','ModerateIntensityExercise') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [ModerateIntensityExercise] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','NeverSmokedButExposedToSmoke') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [NeverSmokedButExposedToSmoke] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','NumbnessWhenWalkingFast') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [NumbnessWhenWalkingFast] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','PainLastsMoreThan30Min') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [PainLastsMoreThan30Min] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','PainRelievedWithRest') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [PainRelievedWithRest] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','PatientSignature') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [PatientSignature] NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','RandomBloodSugar') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [RandomBloodSugar] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','RightArmMeanBP') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [RightArmMeanBP] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','RiskPercentage') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [RiskPercentage] NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','SeeDoctorIfYes') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [SeeDoctorIfYes] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','UrineKetones') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [UrineKetones] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','UrineProtein') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [UrineProtein] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','VigorousIntensityExercise') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [VigorousIntensityExercise] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','WHRatio') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [WHRatio] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','WHStatus') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [WHStatus] NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','Waist') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [Waist] NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.NCDRiskAssessments','Weight') IS NULL ALTER TABLE [dbo].[NCDRiskAssessments] ADD [Weight] NVARCHAR(10) NULL;

");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all the added columns
            migrationBuilder.DropColumn(name: "AlcoholAmount1Bottle320ml", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "AlcoholAmount2Bottle640ml", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "AlcoholAmount3to4WineGlasses300ml", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "AlcoholAmountLessThan3Shot45ml", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "AlcoholAmountMoreThan4Shots75ml", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "AlcoholFrequency1to3TimesPerWeek", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "AlcoholFrequencyMoreThan4TimesPerWeek", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "AssessmentDate", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "BMI", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "BMIStatus", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "BPStatus", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "BaselineBP", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "BloodSugarStatus", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "BreastCancerScreened", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "CancerScreeningStatus", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "CervicalCancerScreened", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "ChestPainSpreadsToArm", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "CholesterolResult", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "CholesterolStatus", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "CombinationExercise", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "DateOfAssessment", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "Designation", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "DoctorName", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "DrinksAlcohol", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "DrinksBeer", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "DrinksWhiskyGinBrandy", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "DrinksWine", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "EatsFattyFoodMoreThan2TimesPerWeek", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "EatsFishDaily", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "EatsFruitsDaily", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "EatsMeatDaily", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "EatsOilyFoodMoreThan2TimesPerWeek", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "EatsSweetFoodMoreThan2TimesPerWeek", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "EatsVegetablesDaily", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "FastingBloodSugar", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "FormerSmoker", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasChestPain", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasHighSaltIntake", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasHistoryOfSmoking", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasPolydipsia", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasPolyphagia", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasPolyuria", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasStress", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasUnhealthyDiet", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasUrineKetones", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasUrineProtein", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "HasWeightLoss", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "Height", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "Hip", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "IDNumber", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "InsufficientPhysicalActivity", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "InterviewedBy", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "IsBingeDrinker", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "LeftArmMeanBP", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "LossOfConsciousnessLessThan10Min", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "ModerateIntensityExercise", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "NeverSmokedButExposedToSmoke", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "NumbnessWhenWalkingFast", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "PainLastsMoreThan30Min", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "PainRelievedWithRest", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "PatientSignature", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "RandomBloodSugar", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "RightArmMeanBP", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "RiskPercentage", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "SeeDoctorIfYes", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "UrineKetones", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "UrineProtein", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "VigorousIntensityExercise", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "WHRatio", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "WHStatus", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "Waist", table: "NCDRiskAssessments");
            migrationBuilder.DropColumn(name: "Weight", table: "NCDRiskAssessments");
        }
    }
}





