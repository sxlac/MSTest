using Microsoft.EntityFrameworkCore;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Diagnostics.CodeAnalysis;

#nullable disable
namespace Signify.DEE.Svc.Core.Data;

[ExcludeFromCodeCoverage]
public partial class DataContext : DbContext
{
    public DataContext()
    {
    }

    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Exam> Exams { get; set; }
    public virtual DbSet<ExamDiagnosis> ExamDiagnoses { get; set; }
    public virtual DbSet<ExamFinding> ExamFindings { get; set; }
    public virtual DbSet<ExamImage> ExamImages { get; set; }
    public virtual DbSet<ExamResult> ExamResults { get; set; }
    public virtual DbSet<ExamStatus> ExamStatuses { get; set; }
    public virtual DbSet<ExamStatusCode> ExamStatusCodes { get; set; }
    public virtual DbSet<LateralityCode> LateralityCodes { get; set; }
    public virtual DbSet<SchemaVersion> SchemaVersions { get; set; }
    public virtual DbSet<Configuration> Configurations { get; set; }
    public virtual DbSet<DEEBilling> DEEBilling { get; set; }
    public virtual DbSet<ProviderPay> ProviderPays { get; set; }
    public virtual DbSet<PDFToClient> PDFToClient { get; set; }
    public virtual DbSet<NotPerformedReason> NotPerformedReason { get; set; }
    public virtual DbSet<DeeNotPerformed> DeeNotPerformed { get; set; }
    public virtual DbSet<ExamLateralityGrade> ExamLateralityGrade { get; set; }
    public virtual DbSet<NonGradableReason> NonGradableReason { get; set; }
    public virtual DbSet<EvaluationObjective> EvaluationObjective { get; set; }
    public virtual DbSet<Hold> Holds { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //Can add additional config settings related to DBContext.
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exam>(entity =>
        {
            entity.ToTable("Exam");

            entity.Property(e => e.CreatedDateTime)
                .HasDefaultValueSql("now()");

            entity.Property(e => e.State).HasMaxLength(5);
        });

        modelBuilder.Entity("Signify.DEE.Svc.Core.Data.Entities.DEEBilling", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .UseIdentityColumn();

            b.Property<string>("BillId")
                .HasMaxLength(50);

            b.Property<int>("ExamId")
                .HasColumnType("int");

            b.HasKey("Id");

            b.Property<bool?>("Accepted").HasDefaultValueSql("false");

            b.ToTable("DEEBilling");
        });

        modelBuilder.Entity<ProviderPay>(entity =>
        {
            entity.ToTable("ProviderPay");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("int")
                .UseIdentityColumn();
            entity.Property(e => e.PaymentId)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.ExamId)
                .HasColumnType("long");
            entity.Property(e => e.CreatedDateTime)
                .HasDefaultValueSql("now()");
            entity.HasOne(d => d.Exam)
                .WithOne(p => p.ProviderPay)
                .HasForeignKey<ProviderPay>(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ExamDiagnosis>(entity =>
        {
            entity.ToTable("ExamDiagnosis");

            entity.Property(e => e.Diagnosis).HasMaxLength(50);

            entity.HasOne(d => d.ExamResult)
                .WithMany(p => p.ExamDiagnoses)
                .HasForeignKey(d => d.ExamResultId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamDiagnosis_ExamResult");
        });

        modelBuilder.Entity<ExamFinding>(entity =>
        {
            entity.ToTable("ExamFinding");

            entity.Property(e => e.Finding).HasMaxLength(500);

            entity.HasOne(d => d.ExamResult)
                .WithMany(p => p.ExamFindings)
                .HasForeignKey(d => d.ExamResultId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamFinding_ExamResult");

            entity.HasOne(d => d.LateralityCode)
                .WithMany(p => p.ExamFindings)
                .HasForeignKey(d => d.LateralityCodeId)
                .HasConstraintName("FK_ExamFinding_LateralityCode");
        });

        modelBuilder.Entity<ExamImage>(entity =>
        {
            entity.ToTable("ExamImage");

            entity.Property(e => e.ImageQuality).HasMaxLength(15);

            entity.Property(e => e.ImageType).HasMaxLength(15);

            entity.HasOne(d => d.Exam)
                .WithMany(p => p.ExamImages)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamImage_Exam");

            entity.HasOne(d => d.LateralityCode)
                .WithMany(p => p.ExamImages)
                .HasForeignKey(d => d.LateralityCodeId)
                .HasConstraintName("FK_ExamImage_LateralityCode");
        });

        modelBuilder.Entity<ExamLateralityGrade>(entity =>
        {
            entity.ToTable("ExamLateralityGrade");

            entity.HasOne(d => d.Exam)
                .WithMany(p => p.ExamLateralityGrades)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamLateralityGrade_Exam");

            entity.HasOne(d => d.LateralityCode)
                .WithMany(p => p.ExamLateralityGrades)
                .HasForeignKey(d => d.LateralityCodeId)
                .HasConstraintName("FK_ExamLateralityGrade_LateralityCode");

        });

        modelBuilder.Entity<NonGradableReason>(entity =>
        {
            entity.ToTable("NonGradableReason");

            entity.HasOne(d => d.ExamLateralityGrade)
                .WithMany(p => p.NonGradableReasons)
                .HasForeignKey(d => d.ExamLateralityGradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NonGradableReason_ExamLateralityGrade");
        });

        modelBuilder.Entity<ExamResult>(entity =>
        {
            entity.ToTable("ExamResult");

            entity.Property(e => e.CarePlan).HasMaxLength(500);

            entity.Property(e => e.GraderFirstName).HasMaxLength(50);

            entity.Property(e => e.GraderLastName).HasMaxLength(50);

            entity.Property(e => e.GraderNpi).HasMaxLength(10);

            entity.Property(e => e.GraderTaxonomy).HasMaxLength(50);

            entity.HasOne(d => d.Exam)
                .WithMany(p => p.ExamResults)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamResult_Exam");
        });

        modelBuilder.Entity<ExamStatus>(entity =>
        {
            entity.ToTable("ExamStatus");

            entity.Property(e => e.ReceivedDateTime)
                .HasDefaultValueSql("now()");

            entity.HasOne(d => d.Exam)
                .WithMany(p => p.ExamStatuses)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamStatus_Exam");

            entity.HasOne(d => d.ExamStatusCode)
                .WithMany(p => p.ExamStatuses)
                .HasForeignKey(d => d.ExamStatusCodeId)
                .HasConstraintName("FK_ExamStatus_ExamStatusCode");
        });

        modelBuilder.Entity<ExamStatusCode>(entity =>
        {
            entity.ToTable("ExamStatusCode");

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<LateralityCode>(entity =>
        {
            entity.ToTable("LateralityCode");

            entity.HasIndex(e => e.Name, "UQ_LateralityCode_Name")
                .IsUnique();

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(256)
                .IsUnicode(false);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(12);
        });

        modelBuilder.Entity<SchemaVersion>(entity =>
        {
            entity.HasKey(e => e.InstalledRank)
                .HasName("schema_version_pk");

            entity.ToTable("schema_version");

            entity.HasIndex(e => e.Success, "schema_version_s_idx");

            entity.Property(e => e.InstalledRank)
                .ValueGeneratedNever()
                .HasColumnName("installed_rank");

            entity.Property(e => e.Checksum).HasColumnName("checksum");

            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");

            entity.Property(e => e.ExecutionTime).HasColumnName("execution_time");

            entity.Property(e => e.InstalledBy)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("installed_by");

            entity.Property(e => e.InstalledOn)
                .HasColumnName("installed_on")
                .HasDefaultValueSql("now()");

            entity.Property(e => e.Script)
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnName("script");

            entity.Property(e => e.Success).HasColumnName("success");

            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("type");

            entity.Property(e => e.Version)
                .HasMaxLength(50)
                .HasColumnName("version");
        });

        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.ToTable("Configuration");
        });

        modelBuilder.Entity<DeeNotPerformed>(entity =>
        {
            entity.ToTable("DeeNotPerformed");

            entity.HasIndex(e => e.ExamId, "DeeNotPerformed_ExamId_key")
                .IsUnique();

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

        });

        modelBuilder.Entity<NotPerformedReason>(entity =>
        {
            entity.ToTable("NotPerformedReason");

            entity.HasIndex(e => e.AnswerId, "NotPerformedReason_AnswerId_key")
                .IsUnique();

            entity.HasIndex(e => e.Reason, "NotPerformedReason_Reason_key")
                .IsUnique();

            entity.Property(e => e.Reason)
                .IsRequired()
                .HasMaxLength(256);
        });

        modelBuilder.Entity<EvaluationObjective>(entity =>
        {
            entity.ToTable("EvaluationObjective");

            entity.HasIndex(e => e.EvaluationObjectiveId, "EvaluationObjective_EvaluationObjective_key")
                .IsUnique();
        });

        modelBuilder.Entity<Hold>(entity =>
        {
            entity.ToTable("Hold");

            entity.HasIndex(e => e.CdiHoldId, "Hold_CdiHoldId_key")
                .IsUnique();

            entity.HasIndex(e => e.EvaluationId, "Hold_EvaluationId_key")
                .IsUnique();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}