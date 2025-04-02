using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Data;

[ExcludeFromCodeCoverage]
public class FOBTDataContext : DbContext
{
    static FOBTDataContext()
    {

    }
    public FOBTDataContext(DbContextOptions<FOBTDataContext> opts) : base(opts)
    { }

    public virtual DbSet<FOBTStatus> FOBTStatus { get; set; }

    public virtual DbSet<Entities.FOBT> FOBT { get; set; }
    public virtual DbSet<Entities.PDFToClient> PDFToClient { get; set; }
    public virtual DbSet<Entities.FOBTBilling> FOBTBilling { get; set; }
    public virtual DbSet<Entities.LabResults> LabResults { get; set; }
    public virtual DbSet<FOBTStatusCode> FOBTStatusCode { get; set; }
    public virtual DbSet<FOBTBarcodeHistory> FOBTBarcodeHistory { get; set; }
    public virtual DbSet<FOBTNotPerformed> FOBTNotPerformed { get; set; }
    public virtual DbSet<NotPerformedReason> NotPerformedReason { get; set; }
    public virtual DbSet<ProviderPay> ProviderPay { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FOBTNotPerformed>(entity =>
        {
            entity.ToTable("FOBTNotPerformed");

            entity.HasIndex(e => e.FOBTId, "FOBTNotPerformed_FOBTId_key")
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

        modelBuilder.Entity<FOBTBilling>(entity =>
        {
            entity.ToTable("FOBTBilling");

            entity.Property(e => e.Accepted).HasDefaultValueSql("false");

            entity.Property(e => e.AcceptedAt).HasColumnType("timestamp with time zone");

            entity.Property(e => e.BillId).HasMaxLength(50);

            entity.Property(e => e.BillingProductCode).HasMaxLength(50);

            entity.Property(e => e.CreatedDateTime)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.FOBTId).HasColumnName("FOBTId");
        });

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FOBTStatusCode>()
            .Property(s => s.FOBTStatusCodeId)
            .ValueGeneratedNever();
    }
}