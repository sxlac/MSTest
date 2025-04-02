using Microsoft.EntityFrameworkCore;
using Signify.eGFR.Core.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Signify.eGFR.Core.Data;

[ExcludeFromCodeCoverage]
public partial class DataContext : DbContext
{
    public virtual DbSet<BillRequestSent> BillRequestSents { get; set; }
    public virtual DbSet<ExamStatus> ExamStatuses { get; set; }
    public virtual DbSet<Exam> Exams { get; set; }
    public virtual DbSet<ExamStatusCode> ExamStatusCodes { get; set; }
    public virtual DbSet<BarcodeHistory> BarcodeHistories { get; set; }
    public virtual DbSet<ExamNotPerformed> ExamNotPerformeds { get; set; }
    public virtual DbSet<NotPerformedReason> NotPerformedReasons { get; set; }
    public virtual DbSet<QuestLabResult> QuestLabResults { get; set; }
    public virtual DbSet<LabResult> LabResults { get; set; }
    public virtual DbSet<PdfDeliveredToClient> PdfDeliveredToClients { get; set; }

    public virtual DbSet<ProviderPay> ProviderPay { get; set; }
    
    public DataContext() { }

    public DataContext(DbContextOptions<DataContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_buffercache")
            .HasPostgresExtension("pg_stat_statements");
            
                   modelBuilder.Entity<BarcodeHistory>(entity =>
            {
                entity.ToTable("BarcodeHistory");

                entity.HasIndex(e => e.Barcode, "BarcodeHistory_Barcode_key")
                    .IsUnique();

                entity.HasIndex(e => e.ExamId, "idx_barcodehistory_examid");

                entity.Property(e => e.Barcode)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(d => d.Exam)
                    .WithMany(p => p.BarcodeHistories)
                    .HasForeignKey(d => d.ExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("BarcodeHistory_ExamId_fkey");
            });

            modelBuilder.Entity<BillRequestSent>(entity =>
            {
                entity.ToTable("BillRequestSent");

                entity.HasIndex(e => e.BillId, "BillRequestSent_BillId_key")
                    .IsUnique();

                entity.HasIndex(e => e.BillId, "idx_billrequestsent_billid");

                entity.HasIndex(e => e.ExamId, "idx_billrequestsent_examid");

                entity.Property(e => e.BillRequestSentId).HasDefaultValueSql("nextval('\"BillRequest_BillRequestId_seq\"'::regclass)");

                entity.Property(e => e.Accepted).HasDefaultValueSql("false");

                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

                entity.HasOne(d => d.Exam)
                    .WithMany(p => p.BillRequestSents)
                    .HasForeignKey(d => d.ExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("BillRequestSent_ExamId_fkey");
                
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

                entity.Property(e => e.DateOfBirth).HasColumnType("timestamp without time zone");

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
                    .WithMany(p => p.ExamsNotPerformed)
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
            

            modelBuilder.Entity<LabResult>(entity =>
            {
                entity.ToTable("LabResult");

                entity.HasIndex(e => e.ExamId, "LabResult_ExamId_key")
                    .IsUnique();

                entity.HasIndex(e => e.ExamId, "idx_labresult_examid");

                entity.HasIndex(e => e.NormalityIndicatorId, "idx_labresult_normalityindicatorid");

                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

                entity.Property(e => e.EgfrResult).HasPrecision(10, 3);

                entity.HasOne(d => d.Exam)
                    .WithOne(p => p.LabResult)
                    .HasForeignKey<LabResult>(d => d.ExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("LabResult_ExamId_fkey");

                entity.HasOne(d => d.NormalityIndicator)
                    .WithMany(p => p.LabResults)
                    .HasForeignKey(d => d.NormalityIndicatorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("LabResult_NormalityIndicatorId_fkey");
            });

            modelBuilder.Entity<NormalityIndicator>(entity =>
            {
                entity.ToTable("NormalityIndicator");

                entity.HasIndex(e => e.Indicator, "NormalityIndicator_Indicator_key")
                    .IsUnique();

                entity.HasIndex(e => e.Normality, "NormalityIndicator_Normality_key")
                    .IsUnique();

                entity.Property(e => e.Indicator).HasMaxLength(1);

                entity.Property(e => e.Normality)
                    .IsRequired()
                    .HasMaxLength(128);
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

                entity.Property(e => e.PdfDeliveredToClientId).HasDefaultValueSql("nextval('pdfdeliveredtoclient_pdfdeliveredtoclientid_seq'::regclass)");

                entity.Property(e => e.BatchName).HasMaxLength(256);
            });

            modelBuilder.Entity<ProviderPay>(entity =>
            {
                entity.ToTable("ProviderPay");

                entity.HasIndex(e => e.ExamId, "ProviderPay_ExamId_key")
                    .IsUnique();

                entity.HasIndex(e => e.PaymentId, "ProviderPay_PaymentId_key")
                    .IsUnique();

                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.PaymentId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasOne(d => d.Exam)
                    .WithOne(p => p.ProviderPay)
                    .HasForeignKey<ProviderPay>(d => d.ExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("ProviderPay_ExamId_fkey");
            });

            modelBuilder.Entity<QuestLabResult>(entity =>
            {
                entity.HasKey(e => e.LabResultId)
                    .HasName("QuestLabResult_pkey");

                entity.ToTable("QuestLabResult");

                entity.HasIndex(e => e.CenseoId, "idx_questlabresult_censeoid");

                entity.Property(e => e.CenseoId).HasMaxLength(8);

                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

                entity.Property(e => e.CreatinineResult).HasPrecision(3, 2);

                entity.Property(e => e.eGFRResult).HasColumnName("eGFRResult");

                entity.Property(e => e.Normality).HasMaxLength(25);

                entity.Property(e => e.NormalityCode).HasMaxLength(1);

                entity.Property(e => e.VendorLabTestNumber).HasMaxLength(25);
            });

            modelBuilder.HasSequence("pdfdeliveredtoclient_pdfdeliveredtoclientid_seq");
            
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}