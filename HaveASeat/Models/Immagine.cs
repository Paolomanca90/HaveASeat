namespace HaveASeat.Models
{
	public class Immagine
	{
		public Guid ImmagineId { get; set; }
		public Guid SaloneId { get; set; }
		public string Percorso { get; set; } = string.Empty;
		public bool IsLogo { get; set; } = false;
		public bool IsCover { get; set; } = false;
		public DateTime DataCreazione { get; set; } = DateTime.Now;
		public Salone Salone { get; set; } = new Salone();
	}
}
