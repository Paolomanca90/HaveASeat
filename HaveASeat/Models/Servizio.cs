namespace HaveASeat.Models
{
	public class Servizio
	{
		public Guid ServizioId { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Descrizione { get; set; } = string.Empty;
		public decimal Prezzo { get; set; }
		public decimal Durata { get; set; }
		public Guid SaloneId { get; set; }
		public bool IsPromotion { get; set; } = false; // Indica se il servizio è in promozione
		public DateTime DataInizioPromozione { get; set; } = DateTime.Now; // Data di inizio della promozione
		public DateTime DataFinePromozione { get; set; } = DateTime.Now.AddDays(7); // Data di fine della promozione
		public Salone Salone { get; set; } = new Salone();
	}
}
