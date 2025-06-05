using HaveASeat.Utilities.Enum;

namespace HaveASeat.ViewModels
{
	public class CalendarioAppuntamentoViewModel
	{
		public Guid AppuntamentoId { get; set; }
		public string ClienteNome { get; set; }
		public string ClienteTelefono { get; set; }
		public string DipendenteNome { get; set; }
		public DateTime Data { get; set; }
		public TimeSpan OraInizio { get; set; }
		public TimeSpan OraFine { get; set; }
		public string Note { get; set; }
		public StatoAppuntamento Stato { get; set; }
		public string ServizioNome { get; set; }
		public decimal? Prezzo { get; set; }

		// Proprietà per il calendario
		public string CssClass => Stato switch
		{
			StatoAppuntamento.Prenotato => "bg-green-100 border-green-500 text-green-800",
			StatoAppuntamento.Annullato => "bg-red-100 border-red-500 text-red-800",
			_ => "bg-gray-100 border-gray-500 text-gray-800"
		};
	}
}
