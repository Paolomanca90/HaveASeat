using HaveASeat.Models;

namespace HaveASeat.ViewModels
{
	public class SearchResultsViewModel
	{
		public List<SaloneSearchResult> Saloni { get; set; } = new List<SaloneSearchResult>();
		public string SearchQuery { get; set; } = string.Empty;
		public string Location { get; set; } = string.Empty;
		public string CategoriaSelezionata { get; set; } = string.Empty;
		public DateTime? DataSelezionata { get; set; }
		public List<Categoria> Categorie { get; set; } = new List<Categoria>();
		public int CurrentPage { get; set; }
		public int TotalPages { get; set; }
		public int TotalResults { get; set; }
		public bool HasResults => Saloni.Any();
		public string ResultsText => TotalResults == 1 ? "1 risultato" : $"{TotalResults} risultati";
	}

	public class SaloneSearchResult
	{
		public Guid SaloneId { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Citta { get; set; } = string.Empty;
		public string Provincia { get; set; } = string.Empty;
		public string Indirizzo { get; set; } = string.Empty;
		public string Telefono { get; set; } = string.Empty;
		public string? CoverImageUrl { get; set; }
		public string? LogoUrl { get; set; }
		public string? Slogan { get; set; }
		public double MediaVoti { get; set; }
		public int NumeroRecensioni { get; set; }
		public int NumeroServizi { get; set; }
		public decimal PrezzoMinimo { get; set; }
		public decimal PrezzoMassimo { get; set; }
		public List<string> ServiziPopolari { get; set; } = new List<string>();
		public bool HasPromozioni { get; set; }
		public PersonalizzazioneColori PersonalizzazioneColori { get; set; } = new PersonalizzazioneColori();

		public string PrezzoRange => PrezzoMinimo == PrezzoMassimo ?
			$"€{PrezzoMinimo:F0}" :
			$"€{PrezzoMinimo:F0} - €{PrezzoMassimo:F0}";

		public string VotiDisplay => MediaVoti > 0 ? MediaVoti.ToString("F1") : "Nuovo";

		public string RecensioniText => NumeroRecensioni switch
		{
			0 => "Nessuna recensione",
			1 => "1 recensione",
			_ => $"{NumeroRecensioni} recensioni"
		};
	}

	public class SaloneDetailsViewModel
	{
		public Salone Salone { get; set; } = new Salone();
		public double MediaVoti { get; set; }
		public Immagine? CoverImage { get; set; }
		public List<Immagine> GalleryImages { get; set; } = new List<Immagine>();
		public Dictionary<string, List<Servizio>> ServiziPerCategoria { get; set; } = new Dictionary<string, List<Servizio>>();
		public List<Dipendente> DipendenteDisponibile { get; set; } = new List<Dipendente>();
		public List<Recensione> RecensioniRecenti { get; set; } = new List<Recensione>();
		public bool IsPremium { get; set; }
		public bool MostraSceltaDipendente { get; set; }

		public string VotiDisplay => MediaVoti > 0 ? MediaVoti.ToString("F1") : "Nuovo";
		public int StellePiene => (int)Math.Floor(MediaVoti);
		public bool HaMezzaStella => MediaVoti % 1 >= 0.5;
		public int StelleVuote => 5 - StellePiene - (HaMezzaStella ? 1 : 0);

		public PersonalizzazioneColori PersonalizzazioneColori => new PersonalizzazioneColori
		{
			ColorePrimario = Salone.SalonePersonalizzazione?.ColorePrimario ?? "#7c3aed",
			ColoreSecondario = Salone.SalonePersonalizzazione?.ColoreSecondario ?? "#ec4899",
			ColoreAccento = Salone.SalonePersonalizzazione?.ColoreAccento ?? "#f59e0b"
		};

		public LayoutPersonalizzazione LayoutPersonalizzazione => new LayoutPersonalizzazione
		{
			TemaColore = Salone.SalonePersonalizzazione?.TemaColore ?? "elegante",
			LayoutTipo = Salone.SalonePersonalizzazione?.LayoutTipo ?? "moderno",
			MostraGalleria = Salone.SalonePersonalizzazione?.MostraGalleria ?? true,
			MostraTeam = Salone.SalonePersonalizzazione?.MostraTeam ?? true,
			MostraServizi = Salone.SalonePersonalizzazione?.MostraServizi ?? true,
			MostraRecensioni = Salone.SalonePersonalizzazione?.MostraRecensioni ?? true,
			MostraContatti = Salone.SalonePersonalizzazione?.MostraContatti ?? true
		};
	}

	public class PersonalizzazioneColori
	{
		public string ColorePrimario { get; set; } = "#7c3aed";
		public string ColoreSecondario { get; set; } = "#ec4899";
		public string ColoreAccento { get; set; } = "#f59e0b";
	}

	public class LayoutPersonalizzazione
	{
		public string TemaColore { get; set; } = "elegante";
		public string LayoutTipo { get; set; } = "moderno";
		public bool MostraGalleria { get; set; } = true;
		public bool MostraTeam { get; set; } = true;
		public bool MostraServizi { get; set; } = true;
		public bool MostraRecensioni { get; set; } = true;
		public bool MostraContatti { get; set; } = true;
	}

	public class BookingConfirmationViewModel
	{
		public Guid AppuntamentoId { get; set; }
		public string NomeSalone { get; set; } = string.Empty;
		public string IndirizzoSalone { get; set; } = string.Empty;
		public string TelefonoSalone { get; set; } = string.Empty;
		public string NomeServizio { get; set; } = string.Empty;
		public DateTime DataAppuntamento { get; set; }
		public string OrarioAppuntamento { get; set; } = string.Empty;
		public string? NomeDipendente { get; set; }
		public decimal PrezzoTotale { get; set; }
		public string? Note { get; set; }
		public bool IsGuestBooking { get; set; }
		public string? EmailConferma { get; set; }
	}

	public class BookingViewModel
	{
		public Guid SaloneId { get; set; }
		public string NomeSalone { get; set; } = string.Empty;
		public Guid ServizioId { get; set; }
		public string NomeServizio { get; set; } = string.Empty;
		public decimal PrezzoServizio { get; set; }
		public decimal DurataServizio { get; set; }
		public Guid? DipendenteId { get; set; }
		public string? NomeDipendente { get; set; }
		public DateTime DataSelezionata { get; set; }
		public Guid SlotId { get; set; }
		public string OrarioSlot { get; set; } = string.Empty;
		public bool RichiedeRegistrazione { get; set; }
		public PersonalizzazioneColori PersonalizzazioneColori { get; set; } = new PersonalizzazioneColori();
	}

	public class SlotAvailabilityViewModel
	{
		public Guid SlotId { get; set; }
		public string OraInizio { get; set; } = string.Empty;
		public string OraFine { get; set; } = string.Empty;
		public string Display { get; set; } = string.Empty;
		public bool Disponibile { get; set; }
		public int PostiLiberi { get; set; }
		public int Capacita { get; set; }
	}

	public class BookingStepViewModel
	{
		public int CurrentStep { get; set; }
		public int TotalSteps { get; set; }
		public bool CanProceed { get; set; }
		public bool ShowStaffSelection { get; set; }
		public string StepTitle { get; set; } = string.Empty;
		public string StepDescription { get; set; } = string.Empty;
	}
}