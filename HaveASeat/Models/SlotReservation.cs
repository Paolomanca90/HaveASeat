using System.ComponentModel.DataAnnotations;
using HaveASeat.Utilities.Enum;

namespace HaveASeat.Models
{
    /// <summary>
    /// Rappresenta un blocco temporaneo di uno slot per prevenire prenotazioni simultanee.
    /// Il blocco dura 10 minuti e viene automaticamente rilasciato se non confermato.
    /// </summary>
    public class SlotReservation
    {
        public Guid SlotReservationId { get; set; }

        /// <summary>
        /// ID del salone per cui è riservato lo slot
        /// </summary>
        public Guid SaloneId { get; set; }

        /// <summary>
        /// Data dell'appuntamento
        /// </summary>
        public DateTime Data { get; set; }

        /// <summary>
        /// Ora di inizio dello slot
        /// </summary>
        public TimeSpan OraInizio { get; set; }

        /// <summary>
        /// Ora di fine dello slot
        /// </summary>
        public TimeSpan OraFine { get; set; }

        /// <summary>
        /// ID del dipendente (opzionale, per saloni con selezione staff)
        /// </summary>
        public Guid? DipendenteId { get; set; }

        /// <summary>
        /// ID del servizio prenotato
        /// </summary>
        public Guid? ServizioId { get; set; }

        /// <summary>
        /// ID dell'utente autenticato (null per guest)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// ID di sessione per tracciare utenti guest e prevenire furti di reservation
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp di creazione della reservation
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp di scadenza della reservation (CreatedAt + 10 minuti)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Stato corrente della reservation
        /// </summary>
        public SlotReservationStatus Status { get; set; }

        /// <summary>
        /// Token di concorrenza per optimistic locking
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // ==================== Navigation Properties ====================

        public Salone? Salone { get; set; }
        public Dipendente? Dipendente { get; set; }
        public Servizio? Servizio { get; set; }
        public ApplicationUser? User { get; set; }

        // ==================== Computed Properties ====================

        /// <summary>
        /// Indica se la reservation è ancora valida (non scaduta e in stato Pending)
        /// </summary>
        public bool IsActive => Status == SlotReservationStatus.Pending && DateTime.UtcNow < ExpiresAt;

        /// <summary>
        /// Secondi rimanenti prima della scadenza
        /// </summary>
        public int RemainingSeconds => Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds);
    }
}
