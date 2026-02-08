namespace HaveASeat.Models
{
	public class Preferito
	{
		public Guid PreferitoId { get; set; }
		public string ApplicationUserId { get; set; } = string.Empty;
		public Guid SaloneId { get; set; }
		public DateTime DataAggiunta { get; set; } = DateTime.UtcNow;

		public ApplicationUser ApplicationUser { get; set; } = null!;
		public Salone Salone { get; set; } = null!;
	}
}
