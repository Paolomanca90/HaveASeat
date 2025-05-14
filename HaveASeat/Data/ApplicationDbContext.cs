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
		public DbSet<DipendenteServizio> SaloneServizio { get; set; }
		public DbSet<Abbonamento> Abbonamento { get; set; }
		public DbSet<SaloneAbbonamento> SaloneAbbonamento { get; set; }
		public DbSet<Dipendente> SaloneResponsabile { get; set; }
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
			
			builder.Entity<Salone>()
				.HasMany(o => o.Recensioni);

			builder.Entity<Orario>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<DipendenteServizio>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<DipendenteServizio>()
				.HasOne(o => o.Servizio)
				.WithMany()
				.HasForeignKey(o => o.ServizioId)
				.OnDelete(DeleteBehavior.NoAction);

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
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<SaloneAbbonamento>()
				.HasOne(o => o.Abbonamento)
				.WithMany()
				.HasForeignKey(o => o.AbbonamentoId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Dipendente>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Dipendente>()
				.HasOne(o => o.ApplicationUser)
				.WithMany()
				.HasForeignKey(o => o.ApplicationUserId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Recensione>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Recensione>()
				.HasOne(o => o.ApplicationUser)
				.WithMany()
				.HasForeignKey(o => o.ApplicationUserId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Slot>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Slot>()
				.HasMany(o => o.Appuntamenti);

			builder.Entity<Immagine>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Appuntamento>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Appuntamento>()
				.HasOne(o => o.ApplicationUser)
				.WithMany()
				.HasForeignKey(o => o.ApplicationUserId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Appuntamento>()
				.HasOne(o => o.Slot)
				.WithMany()
				.HasForeignKey(o => o.SlotId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}
}
