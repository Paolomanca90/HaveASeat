using Microsoft.AspNetCore.Identity;

namespace HaveASeat.Models
{
	public class ApplicationUser : IdentityUser
	{
		public string? Nome { get; set; }
		public string? Cognome { get; set; }
		public string? Indirizzo { get; set; }
		public string? CAP { get; set; }
		public string? Città { get; set; }
		public string? Provincia { get; set; }
		public string? CodiceFiscale { get; set; }
		public string? ImmagineUser { get; set; }
		public DateTime DataRegistrazione { get; set; } = DateTime.Now;
		public bool AcceptNewsletter { get; set; } = false;
		public ICollection<Salone> Saloni { get; set; } = new List<Salone>();
		public ICollection<Appuntamento> Appuntamenti { get; set; } = new List<Appuntamento>();
		public ICollection<Recensione> Recensioni { get; set; } = new List<Recensione>();
	}
}
