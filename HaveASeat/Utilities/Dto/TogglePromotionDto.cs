namespace HaveASeat.Utilities.Dto
{
	public class TogglePromotionDto
	{
		public Guid ServizioId { get; set; }
		public bool IsPromotion { get; set; }
		public decimal? PrezzoPromozione { get; set; }
		public DateTime? DataFinePromozione { get; set; }
	}
}
