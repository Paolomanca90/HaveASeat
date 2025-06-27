namespace HaveASeat.Utilities.Dto
{
	public class SaveCustomizationDto
	{
		public Guid SaloneId { get; set; }
		public string? TemaColore { get; set; }
		public string? ColorePrimario { get; set; }
		public string? ColoreSecondario { get; set; }
		public string? ColoreAccento { get; set; }
		public string? LayoutTipo { get; set; }
		public string? Slogan { get; set; }
		public string? InstagramUrl { get; set; }
		public string? FacebookUrl { get; set; }
		public string? TiktokUrl { get; set; }
		public bool? MostraGalleria { get; set; }
		public bool? MostraTeam { get; set; }
		public bool? MostraServizi { get; set; }
		public bool? MostraRecensioni { get; set; }
		public bool? MostraContatti { get; set; }
	}
}