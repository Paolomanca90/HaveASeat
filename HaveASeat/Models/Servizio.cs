namespace HaveASeat.Models
{
	public class Servizio
	{
		public Guid ServizioId { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Descrizione { get; set; } = string.Empty;
		public decimal Prezzo { get; set; }
		public decimal Durata { get; set; }
	}
}
