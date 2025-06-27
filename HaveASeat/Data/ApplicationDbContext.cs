using HaveASeat.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace HaveASeat.Data
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<Servizio> Servizio { get; set; }
		public DbSet<Salone> Salone { get; set; }
		public DbSet<Orario> Orario { get; set; }
		public DbSet<DipendenteServizio> DipendenteServizio { get; set; } 
		public DbSet<Abbonamento> Abbonamento { get; set; }
		public DbSet<SaloneAbbonamento> SaloneAbbonamento { get; set; }
		public DbSet<Dipendente> Dipendente { get; set; } 
		public DbSet<Recensione> Recensione { get; set; }
		public DbSet<Slot> Slot { get; set; }
		public DbSet<Immagine> Immagine { get; set; }
		public DbSet<Appuntamento> Appuntamento { get; set; }
		public DbSet<OrarioDipendente> OrarioDipendente { get; set; } 
		public DbSet<Categoria> Categoria { get; set; } 
		public DbSet<SaloneCategoria> SaloneCategoria { get; set; } 
		public DbSet<PianoSelezionato> PianoSelezionato { get; set; }
		public DbSet<SalonePersonalizzazione> SalonePersonalizzazione { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.Entity<Salone>()
				.HasOne(o => o.ApplicationUser)
				.WithMany(s => s.Saloni)
				.HasForeignKey(o => o.ApplicationUserId);

			builder.Entity<Salone>()
				.HasMany(o => o.Orari)
				.WithOne(o => o.Salone)
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<Salone>()
				.HasMany(o => o.Recensioni)
				.WithOne(o => o.Salone)
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<Salone>()
				.HasMany(o => o.Servizi)
				.WithOne(o => o.Salone)
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<Salone>()
				.HasMany(o => o.Dipendenti)
				.WithOne(o => o.Salone)
				.HasForeignKey(o => o.SaloneId);

			builder.Entity<Orario>()
				.HasOne(o => o.Salone)
				.WithMany()
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<DipendenteServizio>()
				.HasOne(o => o.Dipendente)
				.WithMany(d => d.ServiziOfferti)
				.HasForeignKey(o => o.DipendenteId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<DipendenteServizio>()
				.HasOne(o => o.Servizio)
				.WithMany(s => s.DipendenteServizi)
				.HasForeignKey(o => o.ServizioId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<OrarioDipendente>()
				.HasOne(o => o.Dipendente)
				.WithMany(d => d.Orari)
				.HasForeignKey(o => o.DipendenteId)
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
				.WithMany(s => s.SaloneAbbonamenti)
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<SaloneAbbonamento>()
				.HasOne(o => o.Abbonamento)
				.WithMany()
				.HasForeignKey(o => o.AbbonamentoId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Dipendente>()
				.HasOne(o => o.Salone)
				.WithMany(s => s.Dipendenti)
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Dipendente>()
				.HasOne(o => o.ApplicationUser)
				.WithMany()
				.HasForeignKey(o => o.ApplicationUserId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Dipendente>()
				.HasMany(d => d.Recensioni)
				.WithOne(r => r.Dipendente)
				.HasForeignKey(r => r.DipendenteId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Dipendente>()
				.HasMany(d => d.Appuntamenti)
				.WithOne(a => a.Dipendente)
				.HasForeignKey(a => a.DipendenteId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Recensione>()
				.HasOne(o => o.Salone)
				.WithMany(s => s.Recensioni)
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Recensione>()
				.HasOne(o => o.ApplicationUser)
				.WithMany(u => u.Recensioni)
				.HasForeignKey(o => o.ApplicationUserId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Slot>()
				.HasOne(o => o.Salone)
				.WithMany(s => s.Slots)
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Slot>()
				.HasMany(o => o.Appuntamenti)
				.WithOne(a => a.Slot)
				.HasForeignKey(a => a.SlotId);

			builder.Entity<Immagine>()
				.HasOne(o => o.Salone)
				.WithMany(s => s.Immagini)
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Immagine>()
				.Property(e => e.IsCover)
				.HasDefaultValue(false);

			builder.Entity<Appuntamento>()
				.HasOne(o => o.Salone)
				.WithMany(s => s.Appuntamenti)
				.HasForeignKey(o => o.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Appuntamento>()
				.HasOne(o => o.ApplicationUser)
				.WithMany(u => u.Appuntamenti)
				.HasForeignKey(o => o.ApplicationUserId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Appuntamento>()
				.HasOne(o => o.Slot)
				.WithMany(s => s.Appuntamenti)
				.HasForeignKey(o => o.SlotId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<SaloneCategoria>()
				.HasOne(sc => sc.Salone)
				.WithMany(s => s.SaloneCategorie)
				.HasForeignKey(sc => sc.SaloneId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.Entity<SaloneCategoria>()
				.HasOne(sc => sc.Categoria)
				.WithMany()
				.HasForeignKey(sc => sc.CategoriaId)
				.OnDelete(DeleteBehavior.NoAction);

            builder.Entity<PianoSelezionato>()
				.HasKey(p => p.PianoSelezionatoId);

			// CONFIGURAZIONI per SalonePersonalizzazione
			builder.Entity<SalonePersonalizzazione>(entity =>
			{
				entity.HasKey(e => e.SalonePersonalizzazioneId);

				entity.HasOne(e => e.Salone)
					  .WithOne(s => s.SalonePersonalizzazione)
					  .HasForeignKey<SalonePersonalizzazione>(e => e.SaloneId)
					  .OnDelete(DeleteBehavior.Cascade);

				entity.Property(e => e.TemaColore)
					  .HasMaxLength(50)
					  .IsRequired();

				entity.Property(e => e.ColorePrimario)
					  .HasMaxLength(7)
					  .IsRequired();

				entity.Property(e => e.ColoreSecondario)
					  .HasMaxLength(7)
					  .IsRequired();

				entity.Property(e => e.ColoreAccento)
					  .HasMaxLength(7)
					  .IsRequired();

				entity.Property(e => e.LayoutTipo)
					  .HasMaxLength(50)
					  .IsRequired();

				entity.Property(e => e.LogoUrl)
					  .HasMaxLength(500);

				entity.Property(e => e.Slogan)
					  .HasMaxLength(200);

				entity.Property(e => e.InstagramUrl)
					  .HasMaxLength(500);

				entity.Property(e => e.FacebookUrl)
					  .HasMaxLength(500);

				entity.Property(e => e.TiktokUrl)
					  .HasMaxLength(500);
			});
		}
    }
}