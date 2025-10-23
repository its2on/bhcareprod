using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Barangay.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Barangay.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionMedication> PrescriptionMedications { get; set; }
        public DbSet<FamilyMember> FamilyMembers { get; set; }
        public DbSet<VitalSign> VitalSigns { get; set; }
        public DbSet<MedicalHistory> MedicalHistories { get; set; }
        public DbSet<LabResult> LabResults { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<StaffMember> StaffMembers { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<ImmunizationRecord> ImmunizationRecords { get; set; }
        public DbSet<ImmunizationShortcutForm> ImmunizationShortcutForms { get; set; }
        public DbSet<AppointmentAttachment> AppointmentAttachments { get; set; }
        public DbSet<AppointmentFile> AppointmentFiles { get; set; }
        public DbSet<UserDocument> UserDocuments { get; set; } = null!;
        
        // New models for appointment booking flow
        public DbSet<FamilyRecord> FamilyRecords { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<ConsultationTimeSlot> ConsultationTimeSlots { get; set; }
        
        // Assessment models
        public DbSet<HEEADSSSAssessment> HEEADSSSAssessments { get; set; }
        public DbSet<NCDRiskAssessment> NCDRiskAssessments { get; set; }
        public DbSet<AdolescentHealthInfo> AdolescentHealthInfo { get; set; }
        public DbSet<IntegratedAssessment> IntegratedAssessments { get; set; }
        public DbSet<FeedbackRating> FeedbackRatings { get; set; }
        public DbSet<PatientHistory> PatientHistories { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }
        public DbSet<EmailSuspension> EmailSuspensions { get; set; }
        public DbSet<PasswordResetOTP> PasswordResetOTPs { get; set; }
        
        // RBAC models
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<StaffPosition> StaffPositions { get; set; }
        public DbSet<StaffPermission> StaffPermissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        public DbSet<GuardianInformation> GuardianInformation { get; set; }

        public DbSet<HealthReport> HealthReports { get; set; }

        public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }

        public DbSet<UrlToken> UrlTokens { get; set; }
        public DbSet<UserSuspension> UserSuspensions { get; set; }
        public DbSet<FamilyNumberCounter> FamilyNumberCounters { get; set; }
        public DbSet<AuditTrail> AuditTrails { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure FullName as a computed column to prevent EF from trying to insert into it.
            // This is a common cause for the "QUOTED_IDENTIFIER" error when a computed column is indexed.
            builder.Entity<ApplicationUser>()
                .Property(u => u.FullName)
                .HasComputedColumnSql("TRIM(ISNULL(FirstName + ' ', '') + ISNULL(MiddleName + ' ', '') + ISNULL(LastName, ''))");

            // Inform EF Core that the AspNetUsers table has triggers.
            // This changes the save mechanism to be compatible with triggers by avoiding the OUTPUT clause.
            builder.Entity<ApplicationUser>().ToTable(tb => tb.HasTrigger("trg_AspNetUsers"));
            
            // Check if UserNumber column exists in the database with a try-catch block
            bool userNumberExists = false;
            try
            {
                // Use safer approach for checking column existence using Database.GetConnectionString()
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(Database.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'UserNumber') SELECT 1 ELSE SELECT 0";
                        var result = command.ExecuteScalar();
                        userNumberExists = (result != null && Convert.ToBoolean(result));
                    }
                }
            }
            catch (Exception)
            {
                // If there's an error checking, default to not including the column in queries
                userNumberExists = false;
            }
            
            // Only include UserNumber in queries if the column exists
            if (!userNumberExists)
            {
                // Exclude the non-existing column from queries using shadow properties
                builder.Entity<ApplicationUser>().Ignore(u => u.UserNumber);
                
                // Log that we're ignoring the UserNumber property
                Console.WriteLine("UserNumber column not found in database. Using fallback mechanism.");
            }
            else 
            {
                Console.WriteLine("UserNumber column exists in database.");
            }
            
            // Configure User entity
            builder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.FirstName).IsRequired();
                entity.Property(e => e.LastName).IsRequired();
                entity.Property(e => e.Password).IsRequired();
                entity.Property(e => e.Status).IsRequired();
            });
            
            // Configure relationship between Prescription and PrescriptionMedication
            // Configure Message entity relationships
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
                
            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
                
            // Configure UserPermission relationships
            builder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
                
            builder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
                
            // Configure StaffPosition relationships
            builder.Entity<StaffPosition>()
                .HasMany(sp => sp.Permissions)
                .WithMany(p => p.StaffPositions)
                .UsingEntity<Dictionary<string, object>>(
                    "StaffPositionPermission",
                    j => j
                        .HasOne<Permission>()
                        .WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.NoAction),
                    j => j
                        .HasOne<StaffPosition>()
                        .WithMany()
                        .HasForeignKey("StaffPositionId")
                        .OnDelete(DeleteBehavior.NoAction)
                );
                
            // Configure Patient relationships
            builder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<Patient>(p => p.UserId);
                
            // Configure Doctor relationships
            builder.Entity<Doctor>()
                .HasOne(d => d.User)
                .WithOne()
                .HasForeignKey<Doctor>(d => d.UserId);
                
            // Configure FamilyMember relationships
            builder.Entity<FamilyMember>()
                .HasOne(fm => fm.Patient)
                .WithMany(p => p.FamilyMembers)
                .HasForeignKey(fm => fm.PatientId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
                
            // Configure MedicalRecord relationships
            builder.Entity<MedicalRecord>()
                .HasOne(mr => mr.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(mr => mr.PatientId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
                
            builder.Entity<MedicalRecord>()
                .HasOne(mr => mr.Doctor)
                .WithMany()
                .HasForeignKey(mr => mr.DoctorId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
                
            // Configure Prescription relationships
            builder.Entity<Prescription>()
                .HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.Entity<Prescription>()
                .HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Configure VitalSign relationships
            builder.Entity<VitalSign>()
                .HasOne(vs => vs.Patient)
                .WithMany(p => p.VitalSigns)
                .HasForeignKey(vs => vs.PatientId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
                
            // Configure Appointment relationships
            builder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
                
            builder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId);
                
            // Configure AppointmentAttachment relationships
            builder.Entity<AppointmentAttachment>()
                .HasOne(aa => aa.Appointment)
                .WithMany(a => a.Attachments)
                .HasForeignKey(aa => aa.AppointmentId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
                
            // Configure AppointmentFile relationships
            builder.Entity<AppointmentFile>()
                .HasOne(af => af.Appointment)
                .WithMany(a => a.Files)
                .HasForeignKey(af => af.AppointmentId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
            
            // Configure Identity relationships
            builder.Entity<IdentityUserClaim<string>>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();

            // Configure Feedback relationships
            builder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();

            // Configure Notification relationships
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();

            // Configure relationship between Prescription and PrescriptionMedication
            builder.Entity<Prescription>()
                .HasMany(p => p.PrescriptionMedicines)
                .WithOne(pm => pm.Prescription)
                .HasForeignKey(pm => pm.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PrescriptionMedication>()
                .HasOne(pm => pm.MedicalRecord)
                .WithMany()
                .HasForeignKey(pm => pm.MedicalRecordId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<IdentityUserLogin<string>>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ul => ul.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            
            builder.Entity<IdentityUserToken<string>>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            
            builder.Entity<IdentityUserRole<string>>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<IdentityRoleClaim<string>>()
                .HasOne<IdentityRole>()
                .WithMany()
                .HasForeignKey(rc => rc.RoleId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure StaffPermission relationships
            builder.Entity<StaffPermission>()
                .HasOne(sp => sp.StaffMember)
                .WithMany(s => s.StaffPermissions)
                .HasForeignKey(sp => sp.StaffMemberId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();

            builder.Entity<StaffPermission>()
                .HasOne(sp => sp.Permission)
                .WithMany()
                .HasForeignKey(sp => sp.PermissionId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();

            // Configure GuardianInformation relationships
            builder.Entity<GuardianInformation>()
                .HasKey(g => g.GuardianId);
                
            builder.Entity<GuardianInformation>()
                .HasOne(g => g.User)
                .WithMany(u => u.GuardianConsents)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();

            // Configure UserDocument relationships
            builder.Entity<UserDocument>(entity =>
            {
                // Configure relationship with User
                entity.HasOne(d => d.User)
                      .WithMany(u => u.UserDocuments)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Configure relationship with Approver
                entity.HasOne(d => d.Approver)
                      .WithMany(u => u.ApprovedDocuments)
                      .HasForeignKey(d => d.ApprovedBy)
                      .OnDelete(DeleteBehavior.NoAction);

                // Configure indexes
                entity.HasIndex(d => d.UserId);
                entity.HasIndex(d => d.Status);
                entity.HasIndex(d => d.UploadDate);
            });

            // Configure HealthReport relationships
            builder.Entity<HealthReport>()
                .HasOne(hr => hr.User)
                .WithMany()
                .HasForeignKey(hr => hr.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();

            builder.Entity<HealthReport>()
                .HasOne(hr => hr.Doctor)
                .WithMany()
                .HasForeignKey(hr => hr.DoctorId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();

            builder.Entity<NCDRiskAssessment>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<NCDRiskAssessment>()
                .HasOne(n => n.Appointment)
                .WithMany()
                .HasForeignKey(n => n.AppointmentId)
                .IsRequired(false)  // Make AppointmentId nullable
                .OnDelete(DeleteBehavior.SetNull);

            // Configure UrlToken relationships
            builder.Entity<UrlToken>()
                .HasOne(ut => ut.User)
                .WithMany()
                .HasForeignKey(ut => ut.ResourceId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // Configure indexes for UrlToken
            builder.Entity<UrlToken>()
                .HasIndex(ut => ut.Token)
                .IsUnique();

            builder.Entity<UrlToken>()
                .HasIndex(ut => ut.ResourceType);

            builder.Entity<UrlToken>()
                .HasIndex(ut => ut.ResourceId);

            builder.Entity<UrlToken>()
                .HasIndex(ut => ut.ExpiresAt);

            builder.Entity<UrlToken>()
                .HasIndex(ut => ut.IsUsed);

            // Configure HEEADSSSAssessment encrypted columns to use NVARCHAR(MAX)
            builder.Entity<HEEADSSSAssessment>(entity =>
            {
                entity.Property(e => e.Is4Ps)
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.IsNHPTS)
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.IsPhilHealthBeneficiaryOnly)
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.IsOwnPhilHealth)
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.PhilHealthPIN)
                    .HasColumnType("nvarchar(max)");

                // Configure other encrypted fields that might have size issues
                entity.Property(e => e.FullName)
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.AppointmentId)
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.HealthFacility)
                    .HasColumnType("nvarchar(max)");
                    
                entity.Property(e => e.FamilyNo)
                    .HasColumnType("nvarchar(max)");
            });

            // Configure AuditTrail entity
            builder.Entity<AuditTrail>()
                .HasOne(at => at.User)
                .WithMany()
                .HasForeignKey(at => at.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // Add indexes for AuditTrail performance
            builder.Entity<AuditTrail>()
                .HasIndex(at => at.Timestamp);
            
            builder.Entity<AuditTrail>()
                .HasIndex(at => at.EntityName);
            
            builder.Entity<AuditTrail>()
                .HasIndex(at => at.UserId);
            
            builder.Entity<AuditTrail>()
                .HasIndex(at => at.ActionType);
        }
    }
}
