using HaveASeat.Utilities.Enum;

namespace HaveASeat.Models
{
	public class Appuntamento
	{
		public Guid AppuntamentoId { get; set; }
		public Guid SaloneId { get; set; }
		public string ApplicationUserId { get; set; } = string.Empty;
		public DateTime OraInizio { get; set; } = DateTime.Now;
		public DateTime OraFine { get; set; } = DateTime.Now.AddHours(1);
		public string Note { get; set; } = string.Empty;
		public Guid SlotId { get; set; }
		public DateTime Data { get; set; } // Data dell'appuntamento
		public StatoAppuntamento Stato { get; set; } // Stato dell'appuntamento (in attesa, confermato, annullato, ecc.)
		public Salone Salone { get; set; } = new Salone();
		public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
		public Slot Slot { get; set; } = new Slot();
	}
}
