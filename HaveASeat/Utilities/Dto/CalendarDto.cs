namespace HaveASeat.Utilities.Dto
{
	public class CalendarDto
	{
		public Guid SaloneId { get; set; }
		public string NomeSalone { get; set; } = string.Empty;
		public DateTime DataSelezionata { get; set; }
		public List<SloDto> Slots { get; set; } = new List<SloDto>();
	}
}
