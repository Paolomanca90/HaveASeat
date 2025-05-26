namespace HaveASeat.Models
{
	public class PianoSelezionato
	{
		public Guid PianoSelezioantoId { get; set; }
		public string ApplicationUserId { get; set; }
		public Guid AbbonamentoId { get; set; }
		public bool Confermato { get; set; }
	}
}
