using HaveASeat.Utilities.Enum;

namespace HaveASeat.Models
{
	public class Appuntamento
	{
		public Guid AppuntamentoId { get; set; }
		public Guid SaloneId { get; set; }
		public string ApplicationUserId { get; set; } = string.Empty;
		public DateTime OraInizio => Data.Date.Add(Slot.OraInizio);
		public DateTime OraFine => Data.Date.Add(Slot.OraFine);
		public string Note { get; set; } = string.Empty;
		public Guid SlotId { get; set; }
		public Guid? DipendenteId { get; set; } // ID del dipendente assegnato all'appuntamento (opzionale)
		public DateTime Data { get; set; } // Data dell'appuntamento
		public StatoAppuntamento Stato { get; set; } // Stato dell'appuntamento (in attesa, confermato, annullato, ecc.)
		public Guid? ServizioId { get; set; }
		public Servizio? Servizio { get; set; }
		public Salone Salone { get; set; } = new Salone();
		public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
		public Slot Slot { get; set; } = new Slot();
		public Dipendente? Dipendente { get; set; } // Dipendente assegnato all'appuntamento (opzionale)
	}
}
