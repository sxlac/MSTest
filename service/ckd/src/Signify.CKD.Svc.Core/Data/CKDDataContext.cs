using Microsoft.EntityFrameworkCore;
using Signify.CKD.Svc.Core.Data.Entities;

namespace Signify.CKD.Svc.Core.Data
{
	public class CKDDataContext : DbContext
	{
		public CKDDataContext(DbContextOptions<CKDDataContext> opts) : base(opts)
		{
		}

		public virtual DbSet<Entities.CKD> CKD { get; set; }
		public virtual DbSet<CKDRCMBilling> CKDRCMBilling { get; set; }
		public virtual DbSet<ProviderPay> ProviderPay { get; set; }
		public virtual DbSet<CKDStatus> CKDStatus { get; set; }
		public virtual DbSet<CKDStatusCode> CKDStatusCode { get; set; }
		public virtual DbSet<ExamNotPerformed> ExamNotPerformed { get; set; }
		public virtual DbSet<LookupCKDAnswer> LookupCKDAnswer { get; set; }
		public virtual DbSet<NotPerformedReason> NotPerformedReason { get; set; }
		public virtual DbSet<PDFToClient> PDFToClient { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<CKDStatusCode>()
				.Property(s => s.CKDStatusCodeId)
				.ValueGeneratedNever();
		}
	}
}