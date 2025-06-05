namespace HaveASeat.Utilities.Dto
{
	public class CreateAppuntamentoDto
	{
		public Guid SaloneId { get; set; }
		public string ClienteId { get; set; }
		public Guid SlotId { get; set; }
		public Guid? DipendenteId { get; set; }
		public DateTime Data { get; set; }
		public string Note { get; set; }
	}
}
