using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Signify.uACR.Core.Data.Entities;

namespace Signify.uACR.Core.Data;

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

    public virtual DbSet<BarcodeExam> BarcodeExams { get; set; }
    public virtual DbSet<BillRequest> BillRequests { get; set; }
    public virtual DbSet<Exam> Exams { get; set; }
    public virtual DbSet<ExamNotPerformed> ExamNotPerformeds { get; set; }
    public virtual DbSet<ExamStatus> ExamStatuses { get; set; }
    public virtual DbSet<ExamStatusCode> ExamStatusCodes { get; set; }
    public virtual DbSet<FlywaySchemaHistory> FlywaySchemaHistories { get; set; }
    public virtual DbSet<LabResult> LabResults { get; set; }
    public virtual DbSet<NotPerformedReason> NotPerformedReasons { get; set; }
    public virtual DbSet<PdfDeliveredToClient> PdfDeliveredToClients { get; set; }
    public virtual DbSet<ProviderPay> ProviderPays { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BarcodeExam>(entity =>
        {
            entity.ToTable("BarcodeExam");

            entity.HasIndex(e => e.Barcode, "BarcodeHistory_Barcode_key")
                .IsUnique();

            entity.HasIndex(e => e.ExamId, "idx_barcodeexam_examid");

            entity.HasIndex(e => e.ExamId, "idx_barcodehistory_examid");

            entity.Property(e => e.BarcodeExamId).HasDefaultValueSql("nextval('\"BarcodeHistory_BarcodeHistoryId_seq\"'::regclass)");

            entity.Property(e => e.Barcode)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasOne(d => d.Exam)
                .WithMany(p => p.BarcodeExams)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BarcodeHistory_ExamId_fkey");
        });

        modelBuilder.Entity<BillRequest>(entity =>
        {
            entity.ToTable("BillRequest");

            entity.HasIndex(e => e.BillId, "BillRequest_BillId_key")
                .IsUnique();

            entity.HasIndex(e => e.ExamId, "idx_billrequest_examid");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

            entity.Property(e => e.Accepted).HasDefaultValueSql("false");
            
            entity.HasOne(d => d.Exam)
                .WithMany(p => p.BillRequests)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("BillRequest_ExamId_fkey");
            
            entity.Property(e => e.BillingProductCode).HasMaxLength(50);
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.ToTable("Exam");

            entity.HasIndex(e => e.EvaluationId, "Exam_EvaluationId_key")
                .IsUnique();

            entity.HasIndex(e => e.EvaluationId, "idx_exam_evaluationid");

            entity.Property(e => e.AddressLineOne).HasMaxLength(200);

            entity.Property(e => e.AddressLineTwo).HasMaxLength(200);

            entity.Property(e => e.ApplicationId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CenseoId).HasMaxLength(8);

            entity.Property(e => e.City).HasMaxLength(100);

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

            entity.Property(e => e.DateOfBirth).HasColumnType("date");

            entity.Property(e => e.FirstName).HasMaxLength(100);

            entity.Property(e => e.LastName).HasMaxLength(100);

            entity.Property(e => e.MiddleName).HasMaxLength(100);

            entity.Property(e => e.NationalProviderIdentifier).HasMaxLength(10);

            entity.Property(e => e.State).HasMaxLength(2);

            entity.Property(e => e.ZipCode).HasMaxLength(5);
        });

        modelBuilder.Entity<ExamNotPerformed>(entity =>
        {
            entity.ToTable("ExamNotPerformed");

            entity.HasIndex(e => e.ExamId, "ExamNotPerformed_ExamId_key")
                .IsUnique();

            entity.HasIndex(e => e.NotPerformedReasonId, "idx_examnotperformed_notperformedreasonid");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

            entity.Property(e => e.Notes)
                .HasMaxLength(1024);
                
            entity.HasOne(d => d.Exam)
                .WithOne(p => p.ExamNotPerformed)
                .HasForeignKey<ExamNotPerformed>(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ExamNotPerformed_ExamId_fkey");

            entity.HasOne(d => d.NotPerformedReason)
                .WithMany(p => p.ExamNotPerformeds)
                .HasForeignKey(d => d.NotPerformedReasonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ExamNotPerformed_NotPerformedReasonId_fkey");
        });

        modelBuilder.Entity<ExamStatus>(entity =>
        {
            entity.ToTable("ExamStatus");

            entity.HasIndex(e => e.ExamId, "idx_examstatus_examid");

            entity.HasIndex(e => e.ExamStatusCodeId, "idx_examstatus_examstatuscodeid");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Exam)
                .WithMany(p => p.ExamStatuses)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ExamStatus_ExamId_fkey");

            entity.HasOne(d => d.ExamStatusCode)
                .WithMany(p => p.ExamStatuses)
                .HasForeignKey(d => d.ExamStatusCodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ExamStatus_ExamStatusCodeId_fkey");
        });

        modelBuilder.Entity<ExamStatusCode>(entity =>
        {
            entity.ToTable("ExamStatusCode");

            entity.HasIndex(e => e.StatusName, "ExamStatusCode_StatusName_key")
                .IsUnique();

            entity.Property(e => e.StatusName)
                .IsRequired()
                .HasMaxLength(250);
        });

        modelBuilder.Entity<FlywaySchemaHistory>(entity =>
        {
            entity.HasKey(e => e.InstalledRank)
                .HasName("flyway_schema_history_pk");

            entity.ToTable("flyway_schema_history");

            entity.HasIndex(e => e.Success, "flyway_schema_history_s_idx");

            entity.Property(e => e.InstalledRank)
                .ValueGeneratedNever()
                .HasColumnName("installed_rank");

            entity.Property(e => e.Checksum).HasColumnName("checksum");

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("description");

            entity.Property(e => e.ExecutionTime).HasColumnName("execution_time");

            entity.Property(e => e.InstalledBy)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("installed_by");

            entity.Property(e => e.InstalledOn)
                .HasColumnType("timestamp without time zone")
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

        modelBuilder.Entity<LabResult>(entity =>
        {
            entity.ToTable("LabResult");
            
            entity.HasIndex(e => e.EvaluationId, "idx_labresult_evaluationid");
            entity.Property(e => e.Normality).HasMaxLength(25);
            entity.Property(e => e.NormalityCode).HasMaxLength(1);
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

        modelBuilder.Entity<PdfDeliveredToClient>(entity =>
        {
            entity.ToTable("PdfDeliveredToClient");

            entity.HasIndex(e => e.EvaluationId, "idx_pdfdeliveredtoclient_evaluationid");

            entity.Property(e => e.BatchName).HasMaxLength(256);
        });

        modelBuilder.Entity<ProviderPay>(entity =>
        {
            entity.ToTable("ProviderPay");

            entity.HasIndex(e => e.ExamId, "ProviderPay_ExamId_key")
                .IsUnique();

            entity.HasIndex(e => e.PaymentId, "ProviderPay_PaymentId_key")
                .IsUnique();

            entity.HasIndex(e => e.ExamId, "idx_providerpay_examid");

            entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

            entity.Property(e => e.PaymentId)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(d => d.Exam)
                .WithOne(p => p.ProviderPay)
                .HasForeignKey<ProviderPay>(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ProviderPay_ExamId_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}