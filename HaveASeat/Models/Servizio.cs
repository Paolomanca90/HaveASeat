namespace HaveASeat.Models
{
	public class Servizio
	{
		public Guid ServizioId { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Descrizione { get; set; } = string.Empty;
		public decimal Prezzo { get; set; } // Prezzo standard
		public decimal PrezzoPromozione { get; set; } // Prezzo durante la promozione
		public decimal Durata { get; set; }
		public Guid SaloneId { get; set; }
		public bool IsPromotion { get; set; } = false;
		public DateTime DataInizioPromozione { get; set; } = DateTime.Now;
		public DateTime DataFinePromozione { get; set; } = DateTime.Now.AddDays(7);

		public Salone Salone { get; set; } = new Salone();
		public ICollection<DipendenteServizio> DipendenteServizi { get; set; } = new List<DipendenteServizio>();

		// Proprietà calcolata per ottenere il prezzo effettivo
		public decimal PrezzoEffettivo => IsPromotion && DataFinePromozione > DateTime.Now ? PrezzoPromozione : Prezzo;

		// Metodo per attivare la promozione
		public void AttivaPromozione(decimal prezzoPromo, DateTime dataFine)
		{
			IsPromotion = true;
			PrezzoPromozione = prezzoPromo;
			DataInizioPromozione = DateTime.Now;
			DataFinePromozione = dataFine;
		}

		// Metodo per disattivare la promozione
		public void DisattivaPromozione()
		{
			IsPromotion = false;
			PrezzoPromozione = 0;
		}
	}
}
