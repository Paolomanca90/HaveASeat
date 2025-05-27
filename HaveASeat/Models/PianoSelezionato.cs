namespace HaveASeat.Models
{
	public class PianoSelezionato
	{
		public Guid PianoSelezionatoId { get; set; }
		public string ApplicationUserId { get; set; }
		public Guid AbbonamentoId { get; set; }
		public bool Confermato { get; set; }
	}
}
