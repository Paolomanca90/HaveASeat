namespace HaveASeat.Utilities.Enum
{
    /// <summary>
    /// Stati possibili per una reservation temporanea di slot
    /// </summary>
    public enum SlotReservationStatus
    {
        /// <summary>
        /// Slot bloccato temporaneamente, in attesa di conferma prenotazione
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Reservation convertita in Appuntamento confermato
        /// </summary>
        Confirmed = 1,

        /// <summary>
        /// Reservation scaduta senza conferma (timeout)
        /// </summary>
        Expired = 2,

        /// <summary>
        /// Reservation rilasciata manualmente dall'utente
        /// </summary>
        Released = 3
    }
}
