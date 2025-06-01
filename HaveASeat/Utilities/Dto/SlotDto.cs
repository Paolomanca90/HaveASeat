namespace HaveASeat.Utilities.Dto
{
	public class SlotDto
	{
		public Guid SlotId { get; set; }
		public DateTime OraInizio { get; set; }
		public DateTime OraFine { get; set; }
		public bool IsDisponibile { get; set; }
		public int PostiDisponibili { get; set; }
		public int PostiTotali { get; set; }
		public string Orario => $"{OraInizio:HH:mm} - {OraFine:HH:mm}";
		public string StatusClass => IsDisponibile ? "disponibile" : "non-disponibile";
		public string DisponibilitaTesto => IsDisponibile ? $"{PostiDisponibili} posti liberi" : "Completo";
	}
}
