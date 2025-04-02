using Microsoft.EntityFrameworkCore;
using Signify.A1C.Svc.Core.Data.Entities;

namespace Signify.A1C.Svc.Core.Data
{
    public class A1CDataContext : DbContext
    {
	    static A1CDataContext()
	    {

	    }
        public A1CDataContext(DbContextOptions<A1CDataContext> opts) : base(opts)
        {}
        
        public virtual DbSet<A1CStatus> A1CStatus { get; set; }
		
		public virtual DbSet<LabResults> LabResults { get; set; }

        public virtual DbSet<Entities.A1C> A1C { get; set; }

        public  virtual  DbSet<A1CStatusCode> A1CStatusCode { get; set;  }
        public virtual DbSet<A1CBarcodeHistory> A1CBarcodeHistory { get; set; }
         
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
	        base.OnModelCreating(modelBuilder);

	        modelBuilder.Entity<A1CStatusCode>()
		        .Property(s => s.A1CStatusCodeId)
		        .ValueGeneratedNever();
        }
    }
}