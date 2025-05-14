namespace HaveASeat.Models
{
	public class OrarioDipendente
	{
		public Guid OrarioDipendenteId { get; set; }
		public Guid DipendenteId { get; set; }
		public DayOfWeek Giorno { get; set; }
		public TimeSpan Apertura { get; set; }
		public TimeSpan Chiusura { get; set; }
		public DateTime DataCreazione { get; set; } = DateTime.Now;
		public bool IsDayOff { get; set; } = false; // Indica se il giorno è un giorno di riposo
		public DateTime InizioFerie { get; set; } = DateTime.Now; // Data di inizio ferie
		public DateTime FineFerie { get; set; } = DateTime.Now.AddDays(7); // Data di fine ferie
		public Dipendente Dipendente { get; set; } = new Dipendente();
	}
}
