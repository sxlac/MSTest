using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;

namespace Signify.HBA1CPOC.Svc.Core.Data;

[ExcludeFromCodeCoverage]
public class Hba1CpocDataContext : DbContext
{
    static Hba1CpocDataContext()
    {

    }
    public Hba1CpocDataContext(DbContextOptions<Hba1CpocDataContext> opts) : base(opts)
    {}
        
    public virtual DbSet<HBA1CPOCStatus> HBA1CPOCStatus { get; set; }

    public virtual DbSet<Entities.HBA1CPOC> HBA1CPOC { get; set; }

    public  virtual  DbSet<HBA1CPOCStatusCode> HBA1CPOCStatusCode { get; set;  }
    public virtual DbSet<PDFToClient> PDFToClient { get; set; }
    public virtual DbSet<HBA1CPOCRCMBilling> HBA1CPOCRCMBilling { get; set; }

    public virtual DbSet<Hba1CpocNotPerformed> HBA1CPOCNotPerformed { get; set; }
    public virtual DbSet<NotPerformedReason> NotPerformedReason { get; set; }

    public virtual DbSet<ProviderPay> ProviderPay { get; set; }
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hba1CpocNotPerformed>(entity =>
        {
            entity.ToTable("HBA1CPOCNotPerformed");

            entity.HasIndex(e => e.HBA1CPOCId, "HBA1CPOCNotPerformed_HBA1CPOCId_key")
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



        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<HBA1CPOCStatusCode>()
            .Property(s => s.HBA1CPOCStatusCodeId)
            .ValueGeneratedNever();
    }
}