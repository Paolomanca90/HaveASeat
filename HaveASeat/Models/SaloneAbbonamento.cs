using HaveASeat.Utilities.Enum;

namespace HaveASeat.Models
{
	public class SaloneAbbonamento
	{
		public Guid SaloneAbbonamentoId { get; set; }
		public Guid SaloneId { get; set; }
		public Guid AbbonamentoId { get; set; }
		public DateTime DataInizio { get; set; } = DateTime.Now;
		public DateTime DataFine { get; set; } = DateTime.Now.AddMonths(1); // Data di scadenza dell'abbonamento
		public Stato Stato { get; set; } // Stato dell'abbonamento (attivo, in attesa di approvazione, rifiutato, ecc.)
		public string Note { get; set; } = string.Empty; // Note aggiuntive sull'abbonamento
		public Salone Salone { get; set; } = new Salone();
		public Abbonamento Abbonamento { get; set; } = new Abbonamento();
	}
}
