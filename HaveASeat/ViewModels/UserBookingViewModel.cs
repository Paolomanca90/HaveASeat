namespace HaveASeat.ViewModels
{
	public class UserBookingViewModel
	{
		public Guid AppuntamentoId { get; set; }
		public Guid SaloneId { get; set; }
		public Guid ServizioId { get; set; }
		public string NomeSalone { get; set; } = string.Empty;
		public string IndirizzoSalone { get; set; } = string.Empty;
		public string TelefonoSalone { get; set; } = string.Empty;
		public string NomeServizio { get; set; } = string.Empty;
		public DateTime DataAppuntamento { get; set; }
		public string OrarioAppuntamento { get; set; } = string.Empty;
		public string? NomeDipendente { get; set; }
		public decimal Prezzo { get; set; }
		public string Stato { get; set; } = string.Empty;
		public string? Note { get; set; }
		public bool CanCancel { get; set; }
		public string StatusClass => Stato switch
		{
			"Prenotato" when DataAppuntamento > DateTime.Now => "status-confirmed",
			"Annullato" => "status-cancelled",
			_ => "status-completed"
		};
		public string StatusText => Stato switch
		{
			"Prenotato" when DataAppuntamento > DateTime.Now => "Confermato",
			"Annullato" => "Cancellato",
			_ => "Completato"
		};
	}
}
