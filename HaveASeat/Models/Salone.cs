using HaveASeat.Utilities.Enum;

namespace HaveASeat.Models
{
	public class Salone
	{
		public Guid SaloneId { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Indirizzo { get; set; } = string.Empty;
		public string CAP { get; set; } = string.Empty;
		public string Citta { get; set; } = string.Empty;
		public string Provincia { get; set; } = string.Empty;
		public string Regione { get; set; } = string.Empty;
		public string Telefono { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string SitoWeb { get; set; } = string.Empty;
		public string? PartitaIVA { get; set; }
		public string? RagioneSociale { get; set; }
		public string? SDI { get; set; }
		public string ApplicationUserId { get; set; } = string.Empty;
		public DateTime DataCreazione { get; set; } = DateTime.Now;
		public Stato Stato { get; set; } = Stato.InAttesaDiApprovazione; // Stato del salone (attivo, in attesa di approvazione, rifiutato, ecc.)
		public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
		public ICollection<Servizio> Servizi { get; set; } = new List<Servizio>();
		public ICollection<Orario> Orari { get; set; } = new List<Orario>();
		public ICollection<Appuntamento> Appuntamenti { get; set; } = new List<Appuntamento>();
		public ICollection<SaloneAbbonamento> SaloneAbbonamenti { get; set; } = new List<SaloneAbbonamento>();
		public ICollection<Dipendente> Dipendenti { get; set; } = new List<Dipendente>();
		public ICollection<Recensione> Recensioni { get; set; } = new List<Recensione>();
		public ICollection<Immagine> Immagini { get; set; } = new List<Immagine>(); 
		public ICollection<Slot> Slots { get; set; } = new List<Slot>();
		public ICollection<SaloneCategoria> SaloneCategorie { get; set; } = new List<SaloneCategoria>();
	}
}
