namespace HaveASeat.Models
{
	public class SaloneResponsabile
	{
		public Guid SaloneResponsabileId { get; set; }
		public Guid SaloneId { get; set; }
		public string ApplicationUserId { get; set; } = string.Empty;
		public DateTime DataCreazione { get; set; } = DateTime.Now;
		public Salone Salone { get; set; } = new Salone();
		public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
	}
}
