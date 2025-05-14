using System.ComponentModel.DataAnnotations;

namespace HaveASeat.Models
{
	public class Recensione
	{
		public Guid RecensioneId { get; set; }
		public Guid SaloneId { get; set; }
		public string ApplicationUserId { get; set; } = string.Empty;
		public Guid? DipendenteId { get; set; } // ID del dipendente che ha ricevuto la recensione (opzionale)
		[Range(1, 5, ErrorMessage = "Il voto deve essere compreso tra 1 e 5.")]
		public int Voto { get; set; } // Voto da 1 a 5
		public string Commento { get; set; } = string.Empty;
		public DateTime DataCreazione { get; set; } = DateTime.Now;
		public Salone Salone { get; set; } = new Salone();
		public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
		public Dipendente? Dipendente { get; set; } // Dipendente che ha ricevuto la recensione (opzionale)
	}
}
