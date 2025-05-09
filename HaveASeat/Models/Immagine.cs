namespace HaveASeat.Models
{
	public class Immagine
	{
		public Guid ImmagineId { get; set; }
		public Guid SaloneId { get; set; }
		public string Percorso { get; set; } = string.Empty;
		public DateTime DataCreazione { get; set; } = DateTime.Now;
		public Salone Salone { get; set; } = new Salone();
	}
}
