using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Data
{
    [ExcludeFromCodeCoverage]
    public partial class SpirometryDataContext : DbContext
    {
        public virtual DbSet<BillRequestSent> BillRequestSents { get; set; }
        public virtual DbSet<ClarificationFlag> ClarificationFlags { get; set; }
        public virtual DbSet<ExamNotPerformed> ExamNotPerformeds { get; set; }
        public virtual DbSet<ExamStatus> ExamStatuses { get; set; }
        public virtual DbSet<Hold> Holds { get; set; }
        public virtual DbSet<NormalityIndicator> NormalityIndicators { get; set; }
        public virtual DbSet<NotPerformedReason> NotPerformedReasons { get; set; }
        public virtual DbSet<OccurrenceFrequency> OccurrenceFrequencies { get; set; }
        public virtual DbSet<OverreadResult> OverreadResults { get; set; }
        public virtual DbSet<PdfDeliveredToClient> PdfDeliveredToClients { get; set; }
        public virtual DbSet<ProviderPay> ProviderPays { get; set; }
        public virtual DbSet<SessionGrade> SessionGrades { get; set; }
        public virtual DbSet<SpirometryExam> SpirometryExams { get; set; }
        public virtual DbSet<SpirometryExamResult> SpirometryExamResults { get; set; }
        public virtual DbSet<StatusCode> StatusCodes { get; set; }
        public virtual DbSet<TrileanType> TrileanTypes { get; set; }
        public virtual DbSet<CdiEventForPayment> CdiEventForPayments { get; set; }

        public SpirometryDataContext()
        {
        }

        public SpirometryDataContext(DbContextOptions<SpirometryDataContext> opts) : base(opts)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("pg_buffercache")
                .HasPostgresExtension("pg_stat_statements");

            modelBuilder.Entity<BillRequestSent>(entity =>
            {
                entity.ToTable("BillRequestSent");

                entity.HasIndex(e => e.BillId, "BillRequestSent_BillId_key")
                    .IsUnique();

                entity.HasIndex(e => e.BillId, "idx_billrequestsent_billid");

                entity.HasIndex(e => e.SpirometryExamId, "idx_billrequestsent_spirometryexamid");

                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");
                
                entity.Property(e => e.Accepted).HasDefaultValueSql("false");
                
                entity.HasOne(d => d.SpirometryExam)
                    .WithMany(p => p.BillRequestSents)
                    .HasForeignKey(d => d.SpirometryExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("BillRequestSent_SpirometryExamId_fkey");
            });

            modelBuilder.Entity<ClarificationFlag>(entity =>
            {
                entity.ToTable("ClarificationFlag");

                entity.HasIndex(e => e.CdiFlagId, "ClarificationFlag_CdiFlagId_key")
                    .IsUnique();

                entity.HasIndex(e => e.SpirometryExamId, "idx_clarificationflag_spirometryexamid")
                    .IsUnique();

                entity.Property(e => e.CreateDateTime).HasDefaultValueSql("now()");

                entity.HasOne(d => d.SpirometryExam)
                    .WithOne(p => p.ClarificationFlag)
                    .HasForeignKey<ClarificationFlag>(d => d.SpirometryExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("ClarificationFlag_SpirometryExamId_fkey");
            });

            modelBuilder.Entity<ExamNotPerformed>(entity =>
            {
                entity.ToTable("ExamNotPerformed");

                entity.HasIndex(e => e.SpirometryExamId, "ExamNotPerformed_SpirometryExamId_key")
                    .IsUnique();

                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

                entity.Property(e => e.Notes)
                    .HasMaxLength(1024);

                entity.HasOne(d => d.NotPerformedReason)
                    .WithMany(p => p.ExamNotPerformeds)
                    .HasForeignKey(d => d.NotPerformedReasonId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("ExamNotPerformed_NotPerformedReasonId_fkey");

                entity.HasOne(d => d.SpirometryExam)
                    .WithOne(p => p.ExamNotPerformed)
                    .HasForeignKey<ExamNotPerformed>(d => d.SpirometryExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("ExamNotPerformed_SpirometryExamId_fkey");
            });

            modelBuilder.Entity<ExamStatus>(entity =>
            {
                entity.ToTable("ExamStatus");

                entity.HasIndex(e => e.SpirometryExamId, "idx_examstatus_spirometryexamid");

                entity.HasIndex(e => e.StatusCodeId, "idx_examstatus_statuscodeid");

                entity.Property(e => e.CreateDateTime).HasDefaultValueSql("now()");

                entity.HasOne(d => d.SpirometryExam)
                    .WithMany(p => p.ExamStatuses)
                    .HasForeignKey(d => d.SpirometryExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("ExamStatus_SpirometryExamId_fkey");

                entity.HasOne(d => d.StatusCode)
                    .WithMany(p => p.ExamStatuses)
                    .HasForeignKey(d => d.StatusCodeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("ExamStatus_StatusCodeId_fkey");
            });

            modelBuilder.Entity<Hold>(entity =>
            {
                entity.ToTable("Hold");

                entity.HasIndex(e => e.CdiHoldId, "Hold_CdiHoldId_key")
                    .IsUnique();

                entity.HasIndex(e => e.EvaluationId, "Hold_EvaluationId_key")
                    .IsUnique();
            });

            modelBuilder.Entity<NormalityIndicator>(entity =>
            {
                entity.ToTable("NormalityIndicator");

                entity.HasIndex(e => e.Indicator, "NormalityIndicator_Indicator_key")
                    .IsUnique();

                entity.HasIndex(e => e.Normality, "NormalityIndicator_Normality_key")
                    .IsUnique();

                entity.HasIndex(e => e.Indicator, "idx_normalityindicator_indicator");

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

            modelBuilder.Entity<OccurrenceFrequency>(entity =>
            {
                entity.ToTable("OccurrenceFrequency");

                entity.HasIndex(e => e.Frequency, "OccurrenceFrequency_Frequency_key")
                    .IsUnique();

                entity.Property(e => e.Frequency)
                    .IsRequired()
                    .HasMaxLength(128);
            });

            modelBuilder.Entity<OverreadResult>(entity =>
            {
                entity.ToTable("OverreadResult");

                entity.HasIndex(e => e.AppointmentId, "OverreadResult_AppointmentId_key")
                    .IsUnique();

                entity.HasIndex(e => e.NormalityIndicatorId, "idx_overreadresult_normalityindicatorid");

                entity.Property(e => e.BestFev1TestComment)
                    .HasColumnType("character varying");

                entity.Property(e => e.BestFvcTestComment)
                    .HasColumnType("character varying");

                entity.Property(e => e.BestPefTestComment)
                    .HasColumnType("character varying");

                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

                entity.Property(e => e.Fev1FvcRatio).HasPrecision(3, 2);

                entity.Property(e => e.OverreadBy)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.Property(e => e.OverreadComment)
                    .IsRequired()
                    .HasColumnType("character varying");

                entity.HasOne(d => d.NormalityIndicator)
                    .WithMany(p => p.OverreadResults)
                    .HasForeignKey(d => d.NormalityIndicatorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("OverreadResult_NormalityIndicatorId_fkey");
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

                entity.HasIndex(e => e.PaymentId, "ProviderPay_PaymentId_key")
                    .IsUnique();

                entity.HasIndex(e => e.SpirometryExamId, "ProviderPay_SpirometryExamId_key")
                    .IsUnique();

                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.PaymentId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasOne(d => d.SpirometryExam)
                    .WithOne(p => p.ProviderPay)
                    .HasForeignKey<ProviderPay>(d => d.SpirometryExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("ProviderPay_SpirometryExamId_fkey");
            });

            modelBuilder.Entity<SessionGrade>(entity =>
            {
                entity.ToTable("SessionGrade");

                entity.HasIndex(e => e.SessionGradeCode, "SessionGrade_SessionGradeCode_key")
                    .IsUnique();

                entity.Property(e => e.SessionGradeCode)
                    .IsRequired()
                    .HasMaxLength(8);
            });

            modelBuilder.Entity<SpirometryExam>(entity =>
            {
                entity.ToTable("SpirometryExam");

                entity.HasIndex(e => e.AppointmentId, "SpirometryExam_AppointmentId_key")
                    .IsUnique();

                entity.HasIndex(e => e.EvaluationId, "SpirometryExam_EvaluationId_key")
                    .IsUnique();

                entity.HasIndex(e => e.EvaluationId, "idx_spirometryexam_evaluationid");

                entity.Property(e => e.AddressLineOne).HasMaxLength(200);

                entity.Property(e => e.AddressLineTwo).HasMaxLength(200);

                entity.Property(e => e.ApplicationId).HasMaxLength(200);

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

            modelBuilder.Entity<SpirometryExamResult>(entity =>
            {
                entity.HasKey(e => e.SpirometryExamResultsId)
                    .HasName("SpirometryExamResults_pkey");

                entity.HasIndex(e => e.SpirometryExamId, "SpirometryExamResults_SpirometryExamId_key")
                    .IsUnique();

                entity.HasIndex(e => e.CoughMucusOccurrenceFrequencyId, "idx_spirometryexamresults_coughmucusoccurrencefrequencyid");

                entity.HasIndex(e => e.Fev1NormalityIndicatorId, "idx_spirometryexamresults_fev1normalityindicatorid");

                entity.HasIndex(e => e.FvcNormalityIndicatorId, "idx_spirometryexamresults_fvcnormalityindicatorid");

                entity.HasIndex(e => e.GetsShortnessOfBreathAtRestTrileanTypeId, "idx_spirometryexamresults_getsshortnessofbreathatresttrileantyp");

                entity.HasIndex(e => e.GetsShortnessOfBreathWithMildExertionTrileanTypeId, "idx_spirometryexamresults_getsshortnessofbreathwithmildexertion");

                entity.HasIndex(e => e.HadWheezingPast12moTrileanTypeId, "idx_spirometryexamresults_hadwheezingpast12motrileantypeid");

                entity.HasIndex(e => e.NoisyChestOccurrenceFrequencyId, "idx_spirometryexamresults_noisychestoccurrencefrequencyid");

                entity.HasIndex(e => e.NormalityIndicatorId, "idx_spirometryexamresults_normalityindicatorid");

                entity.HasIndex(e => e.ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId,
                    "idx_spirometryexamresults_shortnessofbreathphysicalactivityoccu");

                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("now()");

                entity.Property(e => e.Fev1FvcRatio)
                    .HasPrecision(5, 2);

                entity.Property(e => e.OverreadFev1FvcRatio)
                    .HasPrecision(5, 2);

                entity.HasOne(d => d.CoughMucusOccurrenceFrequency)
                    .WithMany(p => p.SpirometryExamResultCoughMucusOccurrenceFrequencies)
                    .HasForeignKey(d => d.CoughMucusOccurrenceFrequencyId)
                    .HasConstraintName("SpirometryExamResults_CoughMucusOccurrenceFrequencyId_fkey");

                entity.HasOne(d => d.Fev1NormalityIndicator)
                    .WithMany(p => p.SpirometryExamResultFev1NormalityIndicators)
                    .HasForeignKey(d => d.Fev1NormalityIndicatorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("SpirometryExamResults_FEV1NormalityIndicatorId_fkey");

                entity.HasOne(d => d.FvcNormalityIndicator)
                    .WithMany(p => p.SpirometryExamResultFvcNormalityIndicators)
                    .HasForeignKey(d => d.FvcNormalityIndicatorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("SpirometryExamResults_FVCNormalityIndicatorId_fkey");

                entity.HasOne(d => d.GetsShortnessOfBreathAtRestTrileanType)
                    .WithMany(p => p.SpirometryExamResultGetsShortnessOfBreathAtRestTrileanTypes)
                    .HasForeignKey(d => d.GetsShortnessOfBreathAtRestTrileanTypeId)
                    .HasConstraintName("SpirometryExamResults_GetsShortnessOfBreathAtRestTrileanTy_fkey");

                entity.HasOne(d => d.GetsShortnessOfBreathWithMildExertionTrileanType)
                    .WithMany(p => p.SpirometryExamResultGetsShortnessOfBreathWithMildExertionTrileanTypes)
                    .HasForeignKey(d => d.GetsShortnessOfBreathWithMildExertionTrileanTypeId)
                    .HasConstraintName("SpirometryExamResults_GetsShortnessOfBreathWithMildExertio_fkey");

                entity.HasOne(d => d.HadWheezingPast12moTrileanType)
                    .WithMany(p => p.SpirometryExamResultHadWheezingPast12moTrileanTypes)
                    .HasForeignKey(d => d.HadWheezingPast12moTrileanTypeId)
                    .HasConstraintName("SpirometryExamResults_HadWheezingPast12moTrileanTypeId_fkey");

                entity.HasOne(d => d.HasEnvOrExpRiskTrileanType)
                    .WithMany(p => p.SpirometryExamResultHasEnvOrExpRiskTrileanTypes)
                    .HasForeignKey(d => d.HasEnvOrExpRiskTrileanTypeId)
                    .HasConstraintName("SpirometryExamResults_HasEnvOrExpRiskTrileanTypeId_fkey");

                entity.HasOne(d => d.HasHighComorbidityTrileanType)
                    .WithMany(p => p.SpirometryExamResultHasHighComorbidityTrileanTypes)
                    .HasForeignKey(d => d.HasHighComorbidityTrileanTypeId)
                    .HasConstraintName("SpirometryExamResults_HasHighComorbidityTrileanTypeId_fkey");

                entity.HasOne(d => d.HasHighSymptomTrileanType)
                    .WithMany(p => p.SpirometryExamResultHasHighSymptomTrileanTypes)
                    .HasForeignKey(d => d.HasHighSymptomTrileanTypeId)
                    .HasConstraintName("SpirometryExamResults_HasHighSymptomTrileanTypeId_fkey");

                entity.HasOne(d => d.NoisyChestOccurrenceFrequency)
                    .WithMany(p => p.SpirometryExamResultNoisyChestOccurrenceFrequencies)
                    .HasForeignKey(d => d.NoisyChestOccurrenceFrequencyId)
                    .HasConstraintName("SpirometryExamResults_NoisyChestOccurrenceFrequencyId_fkey");

                entity.HasOne(d => d.NormalityIndicator)
                    .WithMany(p => p.SpirometryExamResultNormalityIndicators)
                    .HasForeignKey(d => d.NormalityIndicatorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("SpirometryExamResults_NormalityIndicatorId_fkey");

                entity.HasOne(d => d.SessionGrade)
                    .WithMany(p => p.SpirometryExamResults)
                    .HasForeignKey(d => d.SessionGradeId)
                    .HasConstraintName("SpirometryExamResults_SessionGradeId_fkey");

                entity.HasOne(d => d.ShortnessOfBreathPhysicalActivityOccurrenceFrequency)
                    .WithMany(p => p.SpirometryExamResultShortnessOfBreathPhysicalActivityOccurrenceFrequencies)
                    .HasForeignKey(d => d.ShortnessOfBreathPhysicalActivityOccurrenceFrequencyId)
                    .HasConstraintName("SpirometryExamResults_ShortnessOfBreathPhysicalActivityOcc_fkey");

                entity.HasOne(d => d.SpirometryExam)
                    .WithOne(p => p.SpirometryExamResult)
                    .HasForeignKey<SpirometryExamResult>(d => d.SpirometryExamId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("SpirometryExamResults_SpirometryExamId_fkey");
            });

            modelBuilder.Entity<StatusCode>(entity =>
            {
                entity.ToTable("StatusCode");

                entity.HasIndex(e => e.Name, "StatusCode_Name_key")
                    .IsUnique();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(250);
            });

            modelBuilder.Entity<TrileanType>(entity =>
            {
                entity.ToTable("TrileanType");

                entity.HasIndex(e => e.TrileanValue, "TrileanType_TrileanValue_key")
                    .IsUnique();

                entity.Property(e => e.TrileanValue)
                    .IsRequired()
                    .HasMaxLength(8);
            });

            modelBuilder.Entity<CdiEventForPayment>(entity =>
            {
                entity.ToTable("CdiEventForPayment");

                entity.HasIndex(e => e.RequestId, "CdiEventForPayment_RequestId_key")
                    .IsUnique();

                entity.HasIndex(e => e.EvaluationId, "idx_cdieventforpayment_evaluationid");

                entity.Property(e => e.ApplicationId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedDateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.EventType)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.Reason).HasMaxLength(256);
            });

#pragma warning disable S3251 // SonarQube - Implementations should be provided for "partial" methods - Used by EF
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
#pragma warning restore S3251
    }
}