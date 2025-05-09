namespace HaveASeat.Models
{
	public class Slot
	{
		public Guid SlotId { get; set; }
		public Guid SaloneId { get; set; }
		public DateTime OraInizio { get; set; } = DateTime.Now;
		public DateTime OraFine { get; set; } = DateTime.Now.AddHours(1);
		public string Note { get; set; } = string.Empty;
		public bool IsAvailable { get; set; } = true;
		public Salone Salone { get; set; } = new Salone();
		public List<Appuntamento> Appuntamenti { get; set; } = new List<Appuntamento>();
	}
}
