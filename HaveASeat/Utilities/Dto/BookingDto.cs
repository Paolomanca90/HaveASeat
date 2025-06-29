using System.ComponentModel.DataAnnotations;

namespace HaveASeat.Utilities.Dto
{
	// DTO per la ricerca con filtri
	public class SearchFilterDto
	{
		public string? Query { get; set; }
		public string? Location { get; set; }
		public string? Categoria { get; set; }
		public DateTime? Data { get; set; }
		public double? Rating { get; set; }
		public decimal? MaxPrice { get; set; }
		public bool? HasPromozioni { get; set; }
		public bool? SceltaDipendente { get; set; }
		public bool? Premium { get; set; }
		public string? Sort { get; set; }
	}

	// DTO per la creazione delle prenotazioni
	public class CreateBookingDto
	{
		[Required]
		public Guid SaloneId { get; set; }

		[Required]
		public Guid SlotId { get; set; }

		[Required]
		public Guid ServizioId { get; set; }

		public Guid? DipendenteId { get; set; }

		[Required]
		public DateTime Data { get; set; }

		public string TimeSlot { get; set; }

		[StringLength(500)]
		public string? Note { get; set; }

		// Per utenti non registrati
		[StringLength(50)]
		public string? Nome { get; set; }

		[StringLength(50)]
		public string? Cognome { get; set; }

		[EmailAddress]
		public string? Email { get; set; }

		[Phone]
		public string? Telefono { get; set; }
	}

	// DTO per la risposta della prenotazione
	public class BookingResponseDto
	{
		public bool Success { get; set; }
		public string Message { get; set; } = string.Empty;
		public Guid? AppuntamentoId { get; set; }
		public string? RedirectUrl { get; set; }
	}

	// DTO per i dettagli della prenotazione
	public class BookingDetailsDto
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
		public string StatoAppuntamento { get; set; } = string.Empty;
		public bool CanCancel { get; set; }
	}

	// DTO per gli slot disponibili
	public class SlotAvailabilityDto
	{
		public Guid SlotId { get; set; }
		public string OraInizio { get; set; } = string.Empty;
		public string OraFine { get; set; } = string.Empty;
		public string Display { get; set; } = string.Empty;
		public bool Disponibile { get; set; }
		public int PostiLiberi { get; set; }
		public int Capacita { get; set; }
	}

	// DTO per i suggerimenti di ricerca
	public class SearchSuggestionDto
	{
		public string Tipo { get; set; } = string.Empty; // "salone" o "servizio"
		public string Nome { get; set; } = string.Empty;
		public string Location { get; set; } = string.Empty;
		public Guid Id { get; set; }
	}

	// DTO per la cancellazione prenotazioni
	public class CancelBookingDto
	{
		[Required]
		public Guid AppuntamentoId { get; set; }

		[StringLength(200)]
		public string? Motivo { get; set; }
	}

	// DTO per le modifiche alle prenotazioni
	public class UpdateBookingDto
	{
		[Required]
		public Guid AppuntamentoId { get; set; }

		public DateTime? NuovaData { get; set; }
		public Guid? NuovoSlotId { get; set; }
		public Guid? NuovoDipendenteId { get; set; }

		[StringLength(500)]
		public string? Note { get; set; }
	}

	// DTO per le valutazioni
	public class AddReviewDto
	{
		[Required]
		public Guid AppuntamentoId { get; set; }

		[Required]
		[Range(1, 5)]
		public int Rating { get; set; }

		[Required]
		[StringLength(1000)]
		public string Comment { get; set; } = string.Empty;

		public Guid? DipendenteId { get; set; }
	}

	// DTO per le statistiche salone
	public class SaloneStatsDto
	{
		public double MediaVoti { get; set; }
		public int NumeroRecensioni { get; set; }
		public int NumeroServizi { get; set; }
		public decimal PrezzoMinimo { get; set; }
		public decimal PrezzoMassimo { get; set; }
		public bool HasPromozioni { get; set; }
		public List<string> ServiziPopolari { get; set; } = new();
	}

	// DTO per gli orari di apertura
	public class OrariAperturaDto
	{
		public DayOfWeek Giorno { get; set; }
		public TimeSpan Apertura { get; set; }
		public TimeSpan Chiusura { get; set; }
		public bool IsDayOff { get; set; }
		public string DisplayText => IsDayOff ? "Chiuso" : $"{Apertura:hh\\:mm} - {Chiusura:hh\\:mm}";
	}

	// DTO per le prenotazioni rapide (per clienti registrati)
	public class QuickBookingDto
	{
		[Required]
		public Guid SaloneId { get; set; }

		[Required]
		public Guid ServizioId { get; set; }

		public Guid? DipendentePreferito { get; set; }

		[Required]
		public DateTime DataPreferita { get; set; }

		public List<TimeSpan> OrariPreferiti { get; set; } = new();

		public string? NoteSpeciali { get; set; }
	}

	// DTO per il riepilogo prenotazione
	public class BookingSummaryDto
	{
		public string NomeSalone { get; set; } = string.Empty;
		public string NomeServizio { get; set; } = string.Empty;
		public string? NomeDipendente { get; set; }
		public DateTime DataAppuntamento { get; set; }
		public string OrarioAppuntamento { get; set; } = string.Empty;
		public decimal PrezzoTotale { get; set; }
		public int DurataMinuti { get; set; }
		public string? Note { get; set; }
		public bool RichiedeConferma { get; set; }
	}

	// DTO per le notifiche di prenotazione
	public class BookingNotificationDto
	{
		public Guid AppuntamentoId { get; set; }
		public string TipoNotifica { get; set; } = string.Empty; // "conferma", "promemoria", "cancellazione"
		public string EmailDestinatario { get; set; } = string.Empty;
		public string TelefonoDestinatario { get; set; } = string.Empty;
		public DateTime DataInvio { get; set; }
		public string Messaggio { get; set; } = string.Empty;
	}
}