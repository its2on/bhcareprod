USE [Barangay];
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Mark all pending migrations as applied
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
SELECT * FROM (VALUES
    ('20250730144649_AddAppointmentIdToMedicalRecords', '8.0.2'),
    ('20250803133403_AddUnitPropertyToPrescriptionMedications', '8.0.2'),
    ('20250809100607_AddNCDRiskAssessmentColumns', '8.0.2'),
    ('20250810045502_AddMissingNCDRiskAssessmentColumns', '8.0.2'),
    ('20250909160657_AddUserBarangay', '8.0.2'),
    ('20250912233707_AddNotificationSettings', '8.0.2'),
    ('20250913161343_FixMissingColumns', '8.0.2'),
    ('20250915141549_UpdateVitalSignsToStringColumns', '8.0.2'),
    ('20250915143035_AddEncryptedColumnsToVitalSigns', '8.0.2'),
    ('20250927202147_AddAllMissingNCDRiskAssessmentColumns', '8.0.2'),
    ('20251001110232_AddUserSuspensionSystem', '8.0.2'),
    ('20251001115429_FixDatabaseSchema', '8.0.2'),
    ('20251003101955_ConfigureHEEADSSSColumnTypes_Clean', '8.0.2'),
    ('20251004030323_AddRemainingNCDRiskAssessmentColumns', '8.0.2'),
    ('20251004112318_AddHasStrokeSymptomsColumn', '8.0.2'),
    ('20251004125509_AddMissingHEEADSSSColumnsSafely', '8.0.2'),
    ('20251004130115_AddReferredByColumn', '8.0.2'),
    ('20251004132422_AddEatingHabitsColumns', '8.0.2'),
    ('20251005040757_AddCOPDColumns', '8.0.2'),
    ('20251007101911_CreateImmunizationShortcutFormsTable', '8.0.2'),
    ('20251007102351_CreateImmunizationRecordsTable', '8.0.2'),
    ('20251007103131_ForceCreateImmunizationRecordsTable', '8.0.2'),
    ('20251010090747_AddIsFirstLoginField', '8.0.2'),
    ('20251011080238_AddPasswordChangeTracking', '8.0.2')
) AS Migrations(MigrationId, ProductVersion)
WHERE NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory 
    WHERE MigrationId = Migrations.MigrationId
);

PRINT '✓ All migrations marked as applied';

-- Show current migration status
SELECT MigrationId, ProductVersion 
FROM __EFMigrationsHistory 
ORDER BY MigrationId;

GO
