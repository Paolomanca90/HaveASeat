namespace HaveASeat.Models
{
	public class Orario
	{
		public Guid OrarioId { get; set; }
		public Guid SaloneId { get; set; }
		public string Giorno { get; set; } = string.Empty;
		public TimeSpan Apertura { get; set; }
		public TimeSpan Chiusura { get; set; }
		public Salone Salone { get; set; } = new Salone();
	}
}
