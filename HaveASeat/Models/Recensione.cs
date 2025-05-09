namespace HaveASeat.Models
{
	public class Recensione
	{
		public Guid RecensioneId { get; set; }
		public Guid SaloneId { get; set; }
		public string ApplicationUserId { get; set; } = string.Empty;
		public int Voto { get; set; } // Voto da 1 a 5
		public string Commento { get; set; } = string.Empty;
		public DateTime DataCreazione { get; set; } = DateTime.Now;
		public Salone Salone { get; set; } = new Salone();
		public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
	}
}
