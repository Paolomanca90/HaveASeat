using System.ComponentModel.DataAnnotations;

namespace HaveASeat.Utilities.Dto
{
	// DTO per la ricerca con filtri (invariato)
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

	// DTO AGGIORNATO per la creazione delle prenotazioni
	public class CreateBookingDto
	{
		[Required]
		public Guid SaloneId { get; set; }

		[Required]
		public Guid ServizioId { get; set; }

		/// <summary>
		/// ID del dipendente selezionato (opzionale, solo per saloni con scelta dipendente)
		/// </summary>
		public Guid? DipendenteId { get; set; }

		[Required]
		public DateTime Data { get; set; }

		/// <summary>
		/// Slot temporale nel formato "HH:mm - HH:mm" (es. "10:00 - 10:30")
		/// </summary>
		[Required]
		public string TimeSlot { get; set; } = string.Empty;

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

		/// <summary>
		/// ID dello slot generato dinamicamente (per uso interno)
		/// </summary>
		public Guid? SlotId { get; set; }
	}

	// DTO per la risposta della prenotazione (invariato)
	public class BookingResponseDto
	{
		public bool Success { get; set; }
		public string Message { get; set; } = string.Empty;
		public Guid? AppuntamentoId { get; set; }
		public string? RedirectUrl { get; set; }
	}

	// DTO AGGIORNATO per i dettagli della prenotazione
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
		public int DurataMinuti { get; set; }
		public bool IsFuturo { get; set; }
		public bool IsOggi { get; set; }
		public string? TempoRimanenteDescrizione { get; set; }
	}

	// DTO AGGIORNATO per gli slot disponibili
	public class SlotAvailabilityDto
	{
		/// <summary>
		/// ID temporaneo dello slot (per identificazione unica)
		/// </summary>
		public Guid SlotId { get; set; }

		/// <summary>
		/// Ora di inizio nel formato "HH:mm"
		/// </summary>
		public string OraInizio { get; set; } = string.Empty;

		/// <summary>
		/// Ora di fine nel formato "HH:mm"
		/// </summary>
		public string OraFine { get; set; } = string.Empty;

		/// <summary>
		/// Formato display "HH:mm - HH:mm"
		/// </summary>
		public string Display { get; set; } = string.Empty;

		/// <summary>
		/// Se lo slot è disponibile per la prenotazione
		/// </summary>
		public bool Disponibile { get; set; }

		/// <summary>
		/// Posti liberi (utilizzato solo quando non c'è scelta dipendente)
		/// </summary>
		public int PostiLiberi { get; set; }

		/// <summary>
		/// Capacità totale dello slot
		/// </summary>
		public int Capacita { get; set; }

		/// <summary>
		/// Durata del servizio in minuti per questo slot
		/// </summary>
		public int ServiceDurationMinutes { get; set; } = 30;

		/// <summary>
		/// ID del dipendente associato a questo slot (se applicabile)
		/// </summary>
		public Guid? DipendenteId { get; set; }

		/// <summary>
		/// Se questo slot richiede conferma dal salone
		/// </summary>
		public bool RichiedeConferma { get; set; } = false;

		/// <summary>
		/// Prezzo specifico per questo slot (se diverso dal prezzo base del servizio)
		/// </summary>
		public decimal? PrezzoSpecifico { get; set; }
	}

	// DTO per i suggerimenti di ricerca (invariato)
	public class SearchSuggestionDto
	{
		public string Tipo { get; set; } = string.Empty; // "salone" o "servizio"
		public string Nome { get; set; } = string.Empty;
		public string Location { get; set; } = string.Empty;
		public Guid Id { get; set; }
	}

	// DTO AGGIORNATO per la cancellazione prenotazioni
	public class CancelBookingDto
	{
		[Required]
		public Guid AppuntamentoId { get; set; }

		[StringLength(200)]
		public string? Motivo { get; set; }

		/// <summary>
		/// Se la cancellazione deve inviare notifica al salone
		/// </summary>
		public bool NotificaSalone { get; set; } = true;

		/// <summary>
		/// Se la cancellazione deve inviare notifica al cliente
		/// </summary>
		public bool NotificaCliente { get; set; } = true;
	}

	// DTO AGGIORNATO per le modifiche alle prenotazioni
	public class UpdateBookingDto
	{
		[Required]
		public Guid AppuntamentoId { get; set; }

		public DateTime? NuovaData { get; set; }

		/// <summary>
		/// Nuovo slot temporale nel formato "HH:mm - HH:mm"
		/// </summary>
		public string? NuovoTimeSlot { get; set; }

		public Guid? NuovoDipendenteId { get; set; }

		public Guid? NuovoServizioId { get; set; }

		[StringLength(500)]
		public string? Note { get; set; }

		/// <summary>
		/// Se l'aggiornamento deve inviare notifica
		/// </summary>
		public bool InviaNotifica { get; set; } = true;
	}

	// DTO per le valutazioni (invariato)
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

	// DTO AGGIORNATO per le statistiche salone
	public class SaloneStatsDto
	{
		public double MediaVoti { get; set; }
		public int NumeroRecensioni { get; set; }
		public int NumeroServizi { get; set; }
		public decimal PrezzoMinimo { get; set; }
		public decimal PrezzoMassimo { get; set; }
		public bool HasPromozioni { get; set; }
		public List<string> ServiziPopolari { get; set; } = new();
		public bool HasStaffSelection { get; set; }
		public int NumeroSlotDisponibili { get; set; }
		public TimeSpan? ProssimoSlotDisponibile { get; set; }
	}

	// DTO AGGIORNATO per gli orari di apertura
	public class OrariAperturaDto
	{
		public DayOfWeek Giorno { get; set; }
		public TimeSpan Apertura { get; set; }
		public TimeSpan Chiusura { get; set; }
		public bool IsDayOff { get; set; }
		public string DisplayText => IsDayOff ? "Chiuso" : $"{Apertura:hh\\:mm} - {Chiusura:hh\\:mm}";

		/// <summary>
		/// Numero di slot disponibili per questo giorno
		/// </summary>
		public int SlotsDisponibili { get; set; }

		/// <summary>
		/// Orari con maggiore disponibilità
		/// </summary>
		public List<string> OrariConsigliati { get; set; } = new();
	}

	// DTO NUOVO per la validazione degli slot
	public class SlotValidationDto
	{
		public Guid SaloneId { get; set; }
		public DateTime Data { get; set; }
		public TimeSpan OraInizio { get; set; }
		public TimeSpan OraFine { get; set; }
		public Guid? DipendenteId { get; set; }
		public Guid? ServizioId { get; set; }
		public bool IsDisponibile { get; set; }
		public string? MotivoIndisponibilita { get; set; }
		public List<Guid> AppuntamentiConflittuali { get; set; } = new();
	}

	// DTO per le prenotazioni rapide (aggiornato)
	public class QuickBookingDto
	{
		[Required]
		public Guid SaloneId { get; set; }

		[Required]
		public Guid ServizioId { get; set; }

		public Guid? DipendentePreferito { get; set; }

		[Required]
		public DateTime DataPreferita { get; set; }

		/// <summary>
		/// Orari preferiti nel formato "HH:mm"
		/// </summary>
		public List<string> OrariPreferiti { get; set; } = new();

		public string? NoteSpeciali { get; set; }

		/// <summary>
		/// Se accettare slot alternativi vicini agli orari preferiti
		/// </summary>
		public bool AccettaAlternative { get; set; } = true;

		/// <summary>
		/// Numero massimo di giorni in avanti per cercare slot alternativi
		/// </summary>
		public int GiorniMassimiRicerca { get; set; } = 7;
	}

	// DTO AGGIORNATO per il riepilogo prenotazione
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
		public bool HasStaffSelection { get; set; }
		public string IndirizzoSalone { get; set; } = string.Empty;
		public string TelefonoSalone { get; set; } = string.Empty;

		/// <summary>
		/// Politiche di cancellazione
		/// </summary>
		public string PoliticheCancellazione { get; set; } = "Gratuita fino a 24 ore prima";

		/// <summary>
		/// Istruzioni speciali per il cliente
		/// </summary>
		public List<string> IstruzioniSpeciali { get; set; } = new();
	}

	// DTO AGGIORNATO per le notifiche di prenotazione
	public class BookingNotificationDto
	{
		public Guid AppuntamentoId { get; set; }
		public string TipoNotifica { get; set; } = string.Empty; // "conferma", "promemoria", "cancellazione", "modifica"
		public string EmailDestinatario { get; set; } = string.Empty;
		public string TelefonoDestinatario { get; set; } = string.Empty;
		public DateTime DataInvio { get; set; }
		public string Messaggio { get; set; } = string.Empty;
		public bool InviaEmail { get; set; } = true;
		public bool InviaSMS { get; set; } = false;
		public Dictionary<string, string> ParametriTemplate { get; set; } = new();
	}

	// DTO NUOVO per la disponibilità di un dipendente
	public class DipendenteAvailabilityDto
	{
		public Guid DipendenteId { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Cognome { get; set; } = string.Empty;
		public DateTime Data { get; set; }
		public List<SlotAvailabilityDto> SlotsDisponibili { get; set; } = new();
		public bool IsDisponibile { get; set; }
		public string? MotivoIndisponibilita { get; set; }
		public List<Guid> ServiziOfferti { get; set; } = new();
	}

	// DTO NUOVO per il report di disponibilità del salone
	public class SaloneAvailabilityReportDto
	{
		public Guid SaloneId { get; set; }
		public string NomeSalone { get; set; } = string.Empty;
		public DateTime DataInizio { get; set; }
		public DateTime DataFine { get; set; }
		public List<DipendenteAvailabilityDto> DisponibilitaDipendenti { get; set; } = new();
		public List<SlotAvailabilityDto> SlotsGenerali { get; set; } = new();
		public bool HasStaffSelection { get; set; }
		public Dictionary<DateTime, int> SlotsPerGiorno { get; set; } = new();
		public string[] GiorniChiusura { get; set; } = Array.Empty<string>();
	}
}