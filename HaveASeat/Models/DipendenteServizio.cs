namespace HaveASeat.Models
{
	public class DipendenteServizio
	{
		public Guid DipendenteServizioId { get; set; }
		public Guid DipendenteId { get; set; }
		public Guid ServizioId { get; set; }
		public Dipendente Dipendente { get; set; } = new Dipendente();
		public Servizio Servizio { get; set; } = new Servizio();
	}
}
