using Microsoft.EntityFrameworkCore;
using HealthCareManagement.Web.Models.Entities;

namespace HealthCareManagement.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Module 1: Auth, Roles, Users & FamilyGroups
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<FamilyGroup> FamilyGroups => Set<FamilyGroup>();

    // Module 2: Members, Allergies & Chronic Diseases
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberAllergy> MemberAllergies => Set<MemberAllergy>();
    public DbSet<MemberChronicDisease> MemberChronicDiseases => Set<MemberChronicDisease>();

    // Module 3: Medical Records & Attachments
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<MedicalAttachment> MedicalAttachments => Set<MedicalAttachment>();

    // Module 4: Prescriptions & Medication Schedules
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PrescriptionDose> PrescriptionDoses => Set<PrescriptionDose>();
    public DbSet<MedicationSchedule> MedicationSchedules => Set<MedicationSchedule>();

    // Module 5: Appointments & Immunizations
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ImmunizationRecord> ImmunizationRecords => Set<ImmunizationRecord>();

    // Module 6: Unified Care Reminders
    public DbSet<CareReminder> CareReminders => Set<CareReminder>();

    // Module 7: Health Metrics & Vitals Telemetry
    public DbSet<HealthMetric> HealthMetrics => Set<HealthMetric>();

    // Module 8: Dictionary, Protocols & Audit Logs
    public DbSet<MedicineDictionary> MedicineDictionary => Set<MedicineDictionary>();
    public DbSet<VaccineProtocol> VaccineProtocols => Set<VaccineProtocol>();
    public DbSet<SystemAuditLog> SystemAuditLogs => Set<SystemAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================================================================
        // 1. ROLES & USERS & USERROLES
        // =========================================================================
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.RoleName).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(u => u.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================================================================
        // 2. FAMILY GROUPS & MEMBERS
        // =========================================================================
        modelBuilder.Entity<FamilyGroup>(entity =>
        {
            entity.ToTable("FamilyGroups");
            entity.HasKey(fg => fg.Id);
            entity.Property(fg => fg.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(fg => fg.CreatedByUser)
                  .WithMany(u => u.CreatedFamilyGroups)
                  .HasForeignKey(fg => fg.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("Members");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasIndex(m => m.FamilyGroupId).HasDatabaseName("IX_Members_FamilyGroupId");

            // Filtered Unique Index: 1 User chỉ liên kết với tối đa 1 Member
            entity.HasIndex(m => m.UserId)
                  .IsUnique()
                  .HasFilter("[UserId] IS NOT NULL")
                  .HasDatabaseName("IX_Members_UserId_Unique");

            entity.HasOne(m => m.FamilyGroup)
                  .WithMany(fg => fg.Members)
                  .HasForeignKey(m => m.FamilyGroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.User)
                  .WithOne(u => u.Member)
                  .HasForeignKey<Member>(m => m.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MemberAllergy>(entity =>
        {
            entity.ToTable("MemberAllergies");
            entity.HasKey(ma => ma.Id);

            entity.HasOne(ma => ma.Member)
                  .WithMany(m => m.MemberAllergies)
                  .HasForeignKey(ma => ma.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MemberChronicDisease>(entity =>
        {
            entity.ToTable("MemberChronicDiseases");
            entity.HasKey(mcd => mcd.Id);

            entity.HasOne(mcd => mcd.Member)
                  .WithMany(m => m.MemberChronicDiseases)
                  .HasForeignKey(mcd => mcd.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================================================================
        // 3. MEDICAL RECORDS & ATTACHMENTS
        // =========================================================================
        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.ToTable("MedicalRecords");
            entity.HasKey(mr => mr.Id);
            entity.Property(mr => mr.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasIndex(mr => new { mr.MemberId, mr.ExamDate })
                  .HasDatabaseName("IX_MedicalRecords_MemberId_ExamDate");

            entity.HasOne(mr => mr.Member)
                  .WithMany(m => m.MedicalRecords)
                  .HasForeignKey(mr => mr.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MedicalAttachment>(entity =>
        {
            entity.ToTable("MedicalAttachments");
            entity.HasKey(ma => ma.Id);
            entity.Property(ma => ma.UploadedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(ma => ma.MedicalRecord)
                  .WithMany(mr => mr.MedicalAttachments)
                  .HasForeignKey(ma => ma.MedicalRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================================================================
        // 4. PRESCRIPTIONS, ITEMS, DOSES & MEDICATION SCHEDULES
        // =========================================================================
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.ToTable("Prescriptions");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(p => p.IsActive).HasDefaultValue(true);

            entity.HasIndex(p => new { p.MemberId, p.IsActive })
                  .HasDatabaseName("IX_Prescriptions_MemberId_Active");

            entity.HasOne(p => p.Member)
                  .WithMany(m => m.Prescriptions)
                  .HasForeignKey(p => p.MemberId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.MedicalRecord)
                  .WithMany(mr => mr.Prescriptions)
                  .HasForeignKey(p => p.MedicalRecordId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            entity.ToTable("PrescriptionItems");
            entity.HasKey(pi => pi.Id);

            entity.HasOne(pi => pi.Prescription)
                  .WithMany(p => p.PrescriptionItems)
                  .HasForeignKey(pi => pi.PrescriptionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PrescriptionDose>(entity =>
        {
            entity.ToTable("PrescriptionDoses");
            entity.HasKey(pd => pd.Id);
            entity.Property(pd => pd.Quantity).HasColumnType("decimal(4,1)").HasDefaultValue(1.0m);

            entity.HasOne(pd => pd.PrescriptionItem)
                  .WithMany(pi => pi.PrescriptionDoses)
                  .HasForeignKey(pd => pd.PrescriptionItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MedicationSchedule>(entity =>
        {
            entity.ToTable("MedicationSchedules");
            entity.HasKey(ms => ms.Id);
            entity.Property(ms => ms.IsTaken).HasDefaultValue(false);

            // Non-Clustered Index theo yêu cầu: IX_MedicationsSchedules_Date trên (ScheduleDate, IsTaken)
            entity.HasIndex(ms => new { ms.ScheduleDate, ms.IsTaken })
                  .HasDatabaseName("IX_MedicationsSchedules_Date");

            entity.HasOne(ms => ms.PrescriptionDose)
                  .WithMany(pd => pd.MedicationSchedules)
                  .HasForeignKey(ms => ms.PrescriptionDoseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================================================================
        // 5. APPOINTMENTS & IMMUNIZATIONS
        // =========================================================================
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Status).HasDefaultValue("Chờ khám");
            entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasIndex(a => new { a.MemberId, a.AppointmentDateTime })
                  .HasDatabaseName("IX_Appointments_MemberId_DateTime");

            entity.HasOne(a => a.Member)
                  .WithMany(m => m.Appointments)
                  .HasForeignKey(a => a.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.MedicalRecord)
                  .WithMany(mr => mr.Appointments)
                  .HasForeignKey(a => a.MedicalRecordId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ImmunizationRecord>(entity =>
        {
            entity.ToTable("ImmunizationRecords");
            entity.HasKey(ir => ir.Id);

            entity.HasOne(ir => ir.Member)
                  .WithMany(m => m.ImmunizationRecords)
                  .HasForeignKey(ir => ir.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================================================================
        // 6. CARE REMINDERS
        // =========================================================================
        modelBuilder.Entity<CareReminder>(entity =>
        {
            entity.ToTable("CareReminders");
            entity.HasKey(cr => cr.Id);
            entity.Property(cr => cr.IsCompleted).HasDefaultValue(false);
            entity.Property(cr => cr.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasIndex(cr => new { cr.ScheduledDateTime, cr.IsCompleted })
                  .HasDatabaseName("IX_CareReminders_Schedule_Completed");

            entity.HasOne(cr => cr.Member)
                  .WithMany(m => m.CareReminders)
                  .HasForeignKey(cr => cr.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cr => cr.CompletedByUser)
                  .WithMany(u => u.CompletedCareReminders)
                  .HasForeignKey(cr => cr.CompletedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // =========================================================================
        // 7. HEALTH METRICS (VITALS TELEMETRY)
        // =========================================================================
        modelBuilder.Entity<HealthMetric>(entity =>
        {
            entity.ToTable("HealthMetrics");
            entity.HasKey(hm => hm.Id);
            entity.Property(hm => hm.RecordedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(hm => hm.WeightKg).HasColumnType("decimal(5,2)");
            entity.Property(hm => hm.HeightCm).HasColumnType("decimal(5,2)");
            entity.Property(hm => hm.BloodSugar).HasColumnType("decimal(5,2)");
            entity.Property(hm => hm.BodyTemperature).HasColumnType("decimal(4,1)");
            entity.Property(hm => hm.IsAbnormal).HasDefaultValue(false);

            entity.HasIndex(hm => new { hm.MemberId, hm.RecordedAt })
                  .HasDatabaseName("IX_HealthMetrics_MemberId_RecordedAt");

            entity.HasOne(hm => hm.Member)
                  .WithMany(m => m.HealthMetrics)
                  .HasForeignKey(hm => hm.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================================================================
        // 8. DICTIONARY, PROTOCOLS & AUDIT LOGS
        // =========================================================================
        modelBuilder.Entity<MedicineDictionary>(entity =>
        {
            entity.ToTable("MedicineDictionary");
            entity.HasKey(md => md.Id);
            entity.Property(md => md.IsActive).HasDefaultValue(true);

            entity.HasIndex(md => new { md.BrandName, md.ActiveIngredient })
                  .HasDatabaseName("IX_Medicine_Name");
        });

        modelBuilder.Entity<VaccineProtocol>(entity =>
        {
            entity.ToTable("VaccineProtocols");
            entity.HasKey(vp => vp.Id);
        });

        modelBuilder.Entity<SystemAuditLog>(entity =>
        {
            entity.ToTable("SystemAuditLogs");
            entity.HasKey(al => al.Id);
            entity.Property(al => al.Timestamp).HasDefaultValueSql("GETDATE()");

            entity.HasOne(al => al.User)
                  .WithMany(u => u.SystemAuditLogs)
                  .HasForeignKey(al => al.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
