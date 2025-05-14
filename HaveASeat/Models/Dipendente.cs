namespace HaveASeat.Models
{
	public class Dipendente
	{
		public Guid DipendenteId { get; set; }
		public Guid SaloneId { get; set; }
		public string ApplicationUserId { get; set; } = string.Empty;
		public DateTime DataCreazione { get; set; } = DateTime.Now;
		public Salone Salone { get; set; } = new Salone();
		public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
		public ICollection<OrarioDipendente> Orari { get; set; } = new List<OrarioDipendente>();
		public ICollection<Appuntamento> Appuntamenti { get; set; } = new List<Appuntamento>();
		public ICollection<Recensione> Recensioni { get; set; } = new List<Recensione>();
	}
}
