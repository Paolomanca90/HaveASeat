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
		public DbSet<Recensione> Recensione { get; set; }
		public DbSet<Slot> Slot { get; set; }
		public DbSet<Immagine> Immagine { get; set; }
		public DbSet<Appuntamento> Appuntamento { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<Salone>()
				.HasOne(o => o.ApplicationUser)
				.WithMany(s => s.Saloni)
				.HasForeignKey(o => o.ApplicationUserId);

			builder.Entity<Salone>()
				.HasMany(o => o.Orari);

			builder.Entity<Orario>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<SaloneServizio>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<SaloneServizio>()
				.HasOne(o => o.Servizio)
				.WithMany()
				.HasForeignKey(o => o.ServizioId);

			builder.Entity<Abbonamento>()
				.Property(p => p.Prezzo)
				.HasColumnType("decimal(18, 2)");

			builder.Entity<Servizio>()
				.Property(p => p.Prezzo)
				.HasColumnType("decimal(18, 2)");

			builder.Entity<Servizio>()
				.Property(p => p.Durata)
				.HasColumnType("decimal(18, 2)");

			builder.Entity<SaloneAbbonamento>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<SaloneAbbonamento>()
				.HasOne(o => o.Abbonamento)
				.WithMany()
				.HasForeignKey(o => o.AbbonamentoId);

			builder.Entity<SaloneResponsabile>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<SaloneResponsabile>()
				.HasOne(o => o.ApplicationUser)
				.WithMany()
				.HasForeignKey(o => o.ApplicationUserId);

			builder.Entity<Recensione>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<Recensione>()
				.HasOne(o => o.ApplicationUser)
				.WithMany()
				.HasForeignKey(o => o.ApplicationUserId);

			builder.Entity<Slot>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<Slot>()
				.HasMany(o => o.Appuntamenti);

			builder.Entity<Immagine>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<Appuntamento>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<Appuntamento>()
				.HasOne(o => o.ApplicationUser)
				.WithMany()
				.HasForeignKey(o => o.ApplicationUserId);

			builder.Entity<Appuntamento>()
				.HasOne(o => o.Slot)
				.WithMany()
				.HasForeignKey(o => o.SlotId);
		}
	}
}
