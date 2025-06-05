namespace HaveASeat.Utilities.Dto
{
	public class BulkActivateDto
	{
		public Guid SaloneId { get; set; }
		public List<Guid> ServiziIds { get; set; } = new List<Guid>();
		public decimal PercentualeSconto { get; set; }
		public DateTime DataFine { get; set; }
	}
}
