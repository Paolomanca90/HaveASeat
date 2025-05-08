namespace HaveASeat.Models
{
	public class Abbonamento
	{
		public Guid AbbonamentoId { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Descrizione { get; set; } = string.Empty;
		public decimal Prezzo { get; set; }
		public int Durata { get; set; } // Durata in giorni
	}
}
