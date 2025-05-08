using HaveASeat.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HaveASeat.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

		public DbSet<Servizio> Servizio { get; set; }
		public DbSet<Salone> Salone { get; set; }
		public DbSet<Orario> Orario { get; set; }
		public DbSet<SaloneServizio> SaloneServizio { get; set; }
		public DbSet<Abbonamento> Abbonamento { get; set; }
		public DbSet<SaloneAbbonamento> SaloneAbbonamento { get; set; }
		public DbSet<SaloneResponsabile> SaloneResponsabile { get; set; }

		//protected override void OnModelCreating(ModelBuilder builder)
		//{
		//	base.OnModelCreating(builder);

		//	builder.Entity<Ordine>()
		//		.HasOne(o => o.ApplicationUser)
		//		.WithMany()
		//		.HasForeignKey(o => o.ApplicationUserId);

		//	builder.Entity<Ordine>()
		//		.Property(o => o.Totale)
		//		.HasColumnType("decimal(18, 2)");

		//	builder.Entity<Ordine>()
		//		.HasMany(o => o.Dettaglio)
		//		.WithOne(d => d.Ordine)
		//		.HasForeignKey(d => d.OrdineId);

		//	builder.Entity<Prodotto>()
		//		.Property(p => p.Prezzo)
		//		.HasColumnType("decimal(18, 2)");

		//	builder.Entity<Dettaglio>()
		//		.Property(p => p.Prezzo)
		//		.HasColumnType("decimal(18, 2)");

		//	builder.Entity<Dettaglio>()
		//		.HasOne(o => o.Prodotto)
		//		.WithMany()
		//		.HasForeignKey(o => o.ProdottoId);

		//	builder.Entity<Dettaglio>()
		//		.HasMany(d => d.DateAbbonamento)
		//		.WithOne(da => da.Dettaglio)
		//		.HasForeignKey(da => da.DettaglioId);
		//}
	}
}
