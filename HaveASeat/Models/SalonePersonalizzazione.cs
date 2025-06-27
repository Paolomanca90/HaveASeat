namespace HaveASeat.Models
{
	public class SalonePersonalizzazione
	{
		public Guid SalonePersonalizzazioneId { get; set; }
		public Guid SaloneId { get; set; }

		// Colori e Tema
		public string TemaColore { get; set; } = "elegante"; // elegante, professionale, naturale, energico
		public string ColorePrimario { get; set; } = "#7c3aed";
		public string ColoreSecondario { get; set; } = "#ec4899";
		public string ColoreAccento { get; set; } = "#f59e0b";

		// Layout
		public string LayoutTipo { get; set; } = "moderno"; // moderno, classico, magazine

		// Branding
		public string? LogoUrl { get; set; }
		public string? Slogan { get; set; }

		// Social Media
		public string? InstagramUrl { get; set; }
		public string? FacebookUrl { get; set; }
		public string? TiktokUrl { get; set; }

		// Sezioni Visibili
		public bool MostraGalleria { get; set; } = true;
		public bool MostraTeam { get; set; } = true;
		public bool MostraServizi { get; set; } = true;
		public bool MostraRecensioni { get; set; } = true;
		public bool MostraContatti { get; set; } = true;

		// Date
		public DateTime DataCreazione { get; set; } = DateTime.Now;
		public DateTime DataUltimaModifica { get; set; } = DateTime.Now;

		// Navigation
		public Salone Salone { get; set; } = new Salone();
	}
}