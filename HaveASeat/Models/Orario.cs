namespace HaveASeat.Models
{
	public class Orario
	{
		public Guid OrarioId { get; set; }
		public Guid SaloneId { get; set; }
		public DayOfWeek Giorno { get; set; }
		public TimeSpan Apertura { get; set; }
		public TimeSpan Chiusura { get; set; }
		public bool IsDayOff { get; set; }
		public Salone Salone { get; set; } = new Salone();
	}
}
