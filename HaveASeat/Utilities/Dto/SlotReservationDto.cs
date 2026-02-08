using System.ComponentModel.DataAnnotations;

namespace HaveASeat.Utilities.Dto
{
    /// <summary>
    /// Configurazione del sistema di reservation
    /// </summary>
    public static class SlotReservationConfig
    {
        /// <summary>
        /// Durata del blocco in minuti (default: 10 minuti)
        /// </summary>
        public const int DefaultHoldMinutes = 10;

        /// <summary>
        /// Intervallo di cleanup in secondi
        /// </summary>
        public const int CleanupIntervalSeconds = 30;
    }

    /// <summary>
    /// Codici di errore per le operazioni di reservation
    /// </summary>
    public enum SlotReservationErrorCode
    {
        /// <summary>
        /// Lo slot non è disponibile (già prenotato)
        /// </summary>
        SlotNotAvailable,

        /// <summary>
        /// Lo slot è già riservato da un altro utente
        /// </summary>
        SlotAlreadyReserved,

        /// <summary>
        /// Richiesta non valida (dati mancanti o errati)
        /// </summary>
        InvalidRequest,

        /// <summary>
        /// La reservation è scaduta
        /// </summary>
        ReservationExpired,

        /// <summary>
        /// Conflitto di concorrenza (modifica simultanea)
        /// </summary>
        ConcurrencyConflict,

        /// <summary>
        /// Errore generico del database
        /// </summary>
        DatabaseError,

        /// <summary>
        /// Reservation non trovata
        /// </summary>
        ReservationNotFound,

        /// <summary>
        /// Sessione non autorizzata per questa reservation
        /// </summary>
        UnauthorizedSession
    }

    /// <summary>
    /// Richiesta di blocco temporaneo di uno slot
    /// </summary>
    public class ReserveSlotRequest
    {
        [Required]
        public Guid SaloneId { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [Required]
        public TimeSpan OraInizio { get; set; }

        [Required]
        public TimeSpan OraFine { get; set; }

        public Guid? DipendenteId { get; set; }

        public Guid? ServizioId { get; set; }

        /// <summary>
        /// ID utente (se autenticato)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// ID sessione per tracciamento guest
        /// </summary>
        [Required]
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Risultato dell'operazione di reservation
    /// </summary>
    public class SlotReservationResult
    {
        public bool Success { get; set; }

        /// <summary>
        /// ID della reservation creata (se successo)
        /// </summary>
        public Guid? ReservationId { get; set; }

        /// <summary>
        /// Timestamp di scadenza della reservation
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Secondi rimanenti prima della scadenza
        /// </summary>
        public int? RemainingSeconds { get; set; }

        /// <summary>
        /// Messaggio descrittivo
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Codice errore (se fallito)
        /// </summary>
        public SlotReservationErrorCode? ErrorCode { get; set; }

        /// <summary>
        /// Factory per risultato di successo
        /// </summary>
        public static SlotReservationResult Ok(Guid reservationId, DateTime expiresAt)
        {
            var remaining = (int)(expiresAt - DateTime.UtcNow).TotalSeconds;
            return new SlotReservationResult
            {
                Success = true,
                ReservationId = reservationId,
                ExpiresAt = expiresAt,
                RemainingSeconds = Math.Max(0, remaining),
                Message = "Slot riservato con successo"
            };
        }

        /// <summary>
        /// Factory per risultato di errore
        /// </summary>
        public static SlotReservationResult Fail(SlotReservationErrorCode errorCode, string message)
        {
            return new SlotReservationResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message
            };
        }
    }

    /// <summary>
    /// Richiesta di rilascio manuale di una reservation
    /// </summary>
    public class ReleaseReservationRequest
    {
        [Required]
        public Guid ReservationId { get; set; }

        [Required]
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Richiesta di conferma prenotazione (converte reservation in appuntamento)
    /// </summary>
    public class ConfirmReservationRequest
    {
        [Required]
        public Guid ReservationId { get; set; }

        [Required]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// ID utente (se autenticato)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Note aggiuntive per l'appuntamento
        /// </summary>
        [StringLength(500)]
        public string? Note { get; set; }

        // Dati per prenotazione guest
        [StringLength(100)]
        public string? Nome { get; set; }

        [StringLength(100)]
        public string? Cognome { get; set; }

        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string? Telefono { get; set; }
    }

    /// <summary>
    /// Stato dettagliato di uno slot per la visualizzazione UI
    /// </summary>
    public class SlotStatusDto
    {
        public Guid SlotId { get; set; }

        /// <summary>
        /// Orario in formato "HH:mm - HH:mm"
        /// </summary>
        public string TimeSlot { get; set; } = string.Empty;

        public TimeSpan OraInizio { get; set; }
        public TimeSpan OraFine { get; set; }

        /// <summary>
        /// Stato dello slot
        /// </summary>
        public SlotStatusType Status { get; set; }

        /// <summary>
        /// True se la reservation è dell'utente corrente
        /// </summary>
        public bool IsOwnReservation { get; set; }

        /// <summary>
        /// Secondi rimanenti (solo per reservation proprie)
        /// </summary>
        public int? SecondsRemaining { get; set; }

        /// <summary>
        /// ID della reservation (se presente)
        /// </summary>
        public Guid? ReservationId { get; set; }
    }

    /// <summary>
    /// Tipi di stato per uno slot
    /// </summary>
    public enum SlotStatusType
    {
        /// <summary>
        /// Disponibile per la prenotazione
        /// </summary>
        Available,

        /// <summary>
        /// Riservato temporaneamente da un altro utente
        /// </summary>
        Reserved,

        /// <summary>
        /// Riservato dall'utente corrente
        /// </summary>
        OwnReservation,

        /// <summary>
        /// Già prenotato (appuntamento confermato)
        /// </summary>
        Booked
    }

    /// <summary>
    /// Risposta per lo stato di una reservation
    /// </summary>
    public class ReservationStatusResponse
    {
        public bool IsActive { get; set; }
        public Guid? ReservationId { get; set; }
        public int RemainingSeconds { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
