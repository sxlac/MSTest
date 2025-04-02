using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Signify.PAD.Svc.Core.Data.Entities;

namespace Signify.PAD.Svc.Core.Data;

[ExcludeFromCodeCoverage]
public class PADDataContext : DbContext
{
    public PADDataContext(DbContextOptions<PADDataContext> opts) : base(opts)
    {
    }

    public virtual DbSet<AoeSymptomSupportResult> AoeSymptomSupportResult { get; set; }
    public virtual DbSet<LateralityCode> LateralityCode { get; set; }
    public virtual DbSet<PADStatus> PADStatus { get; set; }
    public virtual DbSet<Entities.PAD> PAD { get; set; }
    public virtual DbSet<PDFToClient> PDFToClient { get; set; }
    public virtual DbSet<PADStatusCode> PADStatusCode { get; set; }
    public virtual DbSet<PADRCMBilling> PADRCMBilling { get; set; }
    public virtual DbSet<PedalPulseCode> PedalPulseCode { get; set; }
    public virtual DbSet<ProviderPay> ProviderPay { get; set; }
    public virtual DbSet<NotPerformed> NotPerformed { get; set; }
    public virtual DbSet<SeverityLookup> SeverityLookup { get; set; }
    public virtual DbSet<WaveformDocument> WaveformDocument { get; set; }
    public virtual DbSet<WaveformDocumentVendor> WaveformDocumentVendor { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PADStatusCode>()
            .Property(s => s.PADStatusCodeId)
            .ValueGeneratedNever();

        modelBuilder.Entity<AoeSymptomSupportResult>(entity =>
        {
            entity.HasOne(d => d.FootPainRestingElevatedLateralityCode)
                .WithMany(p => p.AoeSymptomSupportResultFootPainRestingElevatedLateralityCodes)
                .HasForeignKey(d => d.FootPainRestingElevatedLateralityCodeId);
        });
    }
}