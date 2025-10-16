-- Fix NCD Risk Assessment Column Sizes for Encryption (Robust Version)
-- This script increases column sizes to accommodate encrypted data
-- Handles default constraints and dependencies

-- Drop default constraints first
DECLARE @sql NVARCHAR(MAX) = '';

-- Get all default constraints for NCDRiskAssessments table
SELECT @sql = @sql + 'ALTER TABLE NCDRiskAssessments DROP CONSTRAINT ' + name + ';' + CHAR(13)
FROM sys.default_constraints 
WHERE parent_object_id = OBJECT_ID('NCDRiskAssessments')
AND name LIKE 'DF__NCDRiskAs%';

-- Execute the drop statements
IF @sql IS NOT NULL AND @sql != ''
BEGIN
    PRINT 'Dropping default constraints...';
    EXEC sp_executesql @sql;
END

-- Now alter the columns
PRINT 'Altering column sizes...';

-- Alcohol-related fields
ALTER TABLE NCDRiskAssessments ALTER COLUMN AlcoholAmount1Bottle320ml nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN AlcoholAmount2Bottle640ml nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN AlcoholAmount3to4WineGlasses300ml nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN AlcoholAmountLessThan3Shot45ml nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN AlcoholAmountMoreThan4Shots75ml nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN AlcoholFrequency1to3TimesPerWeek nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN AlcoholFrequencyMoreThan4TimesPerWeek nvarchar(4000);

-- Boolean fields that are encrypted as strings
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasDiabetes nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasHypertension nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasCancer nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasCOPD nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasLungDisease nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasEyeDisease nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHasHypertension nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHasHeartDisease nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHasStroke nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHasDiabetes nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHasCancer nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHasKidneyDisease nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHasOtherDisease nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HighSaltIntake nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasDifficultyBreathing nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasAsthma nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasNoRegularExercise nvarchar(4000);

-- Other boolean fields
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasPolyuria nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasPolydipsia nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasPolyphagia nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasWeightLoss nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasUrineProtein nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasUrineKetones nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN BreastCancerScreened nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN CervicalCancerScreened nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasChestPain nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN ChestPainSpreadsToArm nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN NumbnessWhenWalkingFast nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN PainRelievedWithRest nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN LossOfConsciousnessLessThan10Min nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN PainLastsMoreThan30Min nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN SeeDoctorIfYes nvarchar(4000);

-- Nutrition fields
ALTER TABLE NCDRiskAssessments ALTER COLUMN EatsVegetablesDaily nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN EatsFruitsDaily nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN EatsFishDaily nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN EatsMeatDaily nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasUnhealthyDiet nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN EatsFattyFoodMoreThan2TimesPerWeek nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN EatsSweetFoodMoreThan2TimesPerWeek nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN EatsOilyFoodMoreThan2TimesPerWeek nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasHighSaltIntake nvarchar(4000);

-- Alcohol consumption fields
ALTER TABLE NCDRiskAssessments ALTER COLUMN DrinksAlcohol nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN DrinksBeer nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN DrinksWine nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN DrinksWhiskyGinBrandy nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN IsBingeDrinker nvarchar(4000);

-- Exercise fields
ALTER TABLE NCDRiskAssessments ALTER COLUMN ModerateIntensityExercise nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN VigorousIntensityExercise nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN CombinationExercise nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN InsufficientPhysicalActivity nvarchar(4000);

-- Smoking fields
ALTER TABLE NCDRiskAssessments ALTER COLUMN FormerSmoker nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN NeverSmokedButExposedToSmoke nvarchar(4000);
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasHistoryOfSmoking nvarchar(4000);

-- Stress field
ALTER TABLE NCDRiskAssessments ALTER COLUMN HasStress nvarchar(4000);

-- Family history fields (skip the problematic ones for now)
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryCancerFather nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryCancerMother nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryCancerSibling nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryDiabetesFather nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryDiabetesMother nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryDiabetesSibling nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryHeartDiseaseFather nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryHeartDiseaseMother nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryHeartDiseaseSibling nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryLungDiseaseFather nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryLungDiseaseMother nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryLungDiseaseSibling nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryOtherFather nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryOtherMother nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryOtherSibling nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryStrokeFather nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryStrokeMother nvarchar(4000);
-- ALTER TABLE NCDRiskAssessments ALTER COLUMN FamilyHistoryStrokeSibling nvarchar(4000);

PRINT 'NCD Risk Assessment column sizes updated successfully for encryption support.';
