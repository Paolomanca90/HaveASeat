namespace HaveASeat.Models
{
	public class SaloneServizio
	{
		public Guid SaloneServizioId { get; set; }
		public Guid SaloneId { get; set; }
		public Guid ServizioId { get; set; }
		public DateTime DataCreazione { get; set; } = DateTime.Now;
		public Salone Salone { get; set; } = new Salone();
		public Servizio Servizio { get; set; } = new Servizio();
	}
}
