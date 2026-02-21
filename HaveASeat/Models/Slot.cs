namespace HaveASeat.Models
{
	public class Slot
	{
		public Guid SlotId { get; set; }
		public Guid SaloneId { get; set; }
		public TimeSpan OraInizio { get; set; }
		public TimeSpan OraFine { get; set; }
		public string GiorniSettimana { get; set; } = "0,1,2,3,4,5,6";
		public int Capacita { get; set; } = 1;
		public string Note { get; set; } = string.Empty;
		public bool IsAttivo { get; set; } = true;
		public Salone? Salone { get; set; }
		public List<Appuntamento> Appuntamenti { get; set; } = new List<Appuntamento>();
	}
}
