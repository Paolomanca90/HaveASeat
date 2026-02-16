using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.Utilities.Dto;
using HaveASeat.Utilities.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HaveASeat.Services
{
    public interface ISlotReservationService
    {
        /// <summary>
        /// Blocca temporaneamente uno slot per 10 minuti
        /// </summary>
        Task<SlotReservationResult> ReserveSlotAsync(ReserveSlotRequest request);

        /// <summary>
        /// Rilascia manualmente una reservation
        /// </summary>
        Task<bool> ReleaseReservationAsync(Guid reservationId, string sessionId);

        /// <summary>
        /// Ottiene lo stato di una reservation
        /// </summary>
        Task<ReservationStatusResponse> GetReservationStatusAsync(Guid reservationId, string sessionId);

        /// <summary>
        /// Conferma una reservation convertendola in Appuntamento
        /// </summary>
        Task<BookingResponseDto> ConfirmReservationAsync(ConfirmReservationRequest request);

        /// <summary>
        /// Verifica se uno slot è disponibile (considerando anche le reservation attive)
        /// </summary>
        Task<bool> IsSlotAvailableAsync(Guid saloneId, DateTime data, TimeSpan oraInizio, TimeSpan oraFine, Guid? dipendenteId, string? excludeSessionId = null);

        /// <summary>
        /// Ottiene gli slot disponibili con informazioni sulle reservation
        /// </summary>
        Task<List<SlotStatusDto>> GetSlotsWithStatusAsync(Guid saloneId, DateTime data, Guid? dipendenteId, Guid? servizioId, string sessionId);

        /// <summary>
        /// Pulisce le reservation scadute
        /// </summary>
        Task<int> CleanupExpiredReservationsAsync();

        /// <summary>
        /// Ottiene la reservation attiva per una sessione (se presente)
        /// </summary>
        Task<SlotReservation?> GetActiveReservationForSessionAsync(string sessionId);
    }

    public class SlotReservationService : ISlotReservationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SlotReservationService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public SlotReservationService(
            ApplicationDbContext context,
            ILogger<SlotReservationService> logger,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<SlotReservationResult> ReserveSlotAsync(ReserveSlotRequest request)
        {
            try
            {
                // Validazione base
                if (request.SaloneId == Guid.Empty || string.IsNullOrEmpty(request.SessionId))
                {
                    return SlotReservationResult.Fail(SlotReservationErrorCode.InvalidRequest, "Richiesta non valida");
                }

                // Verifica che la data sia futura
                if (request.Data.Date < DateTime.Now.Date)
                {
                    return SlotReservationResult.Fail(SlotReservationErrorCode.InvalidRequest, "La data deve essere futura");
                }

                // Rilascia eventuali reservation precedenti della stessa sessione
                await ReleaseAllReservationsForSessionAsync(request.SessionId);

                // Verifica disponibilità (escludendo la propria sessione)
                if (!await IsSlotAvailableAsync(request.SaloneId, request.Data, request.OraInizio, request.OraFine, request.DipendenteId, request.SessionId))
                {
                    return SlotReservationResult.Fail(SlotReservationErrorCode.SlotNotAvailable, "Lo slot non è disponibile");
                }

                // Crea la reservation
                var now = DateTime.UtcNow;
                var reservation = new SlotReservation
                {
                    SlotReservationId = Guid.NewGuid(),
                    SaloneId = request.SaloneId,
                    Data = request.Data.Date,
                    OraInizio = request.OraInizio,
                    OraFine = request.OraFine,
                    DipendenteId = request.DipendenteId,
                    ServizioId = request.ServizioId,
                    UserId = request.UserId,
                    SessionId = request.SessionId,
                    CreatedAt = now,
                    ExpiresAt = now.AddMinutes(SlotReservationConfig.DefaultHoldMinutes),
                    Status = SlotReservationStatus.Pending
                };

                _context.SlotReservation.Add(reservation);

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Slot riservato: {ReservationId} per sessione {SessionId}", reservation.SlotReservationId, request.SessionId);
                    return SlotReservationResult.Ok(reservation.SlotReservationId, reservation.ExpiresAt);
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    // Un'altra sessione ha già riservato questo slot
                    _logger.LogWarning("Conflitto reservation per slot {SaloneId} {Data} {OraInizio}", request.SaloneId, request.Data, request.OraInizio);
                    return SlotReservationResult.Fail(SlotReservationErrorCode.SlotAlreadyReserved, "Questo slot è già stato riservato da un altro utente");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la reservation dello slot");
                return SlotReservationResult.Fail(SlotReservationErrorCode.DatabaseError, "Errore durante la prenotazione");
            }
        }

        public async Task<bool> ReleaseReservationAsync(Guid reservationId, string sessionId)
        {
            try
            {
                var reservation = await _context.SlotReservation
                    .FirstOrDefaultAsync(r => r.SlotReservationId == reservationId &&
                                             r.SessionId == sessionId &&
                                             r.Status == SlotReservationStatus.Pending);

                if (reservation == null)
                {
                    return false;
                }

                reservation.Status = SlotReservationStatus.Released;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reservation rilasciata: {ReservationId}", reservationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il rilascio della reservation {ReservationId}", reservationId);
                return false;
            }
        }

        public async Task<ReservationStatusResponse> GetReservationStatusAsync(Guid reservationId, string sessionId)
        {
            try
            {
                var reservation = await _context.SlotReservation
                    .FirstOrDefaultAsync(r => r.SlotReservationId == reservationId && r.SessionId == sessionId);

                if (reservation == null)
                {
                    return new ReservationStatusResponse
                    {
                        IsActive = false,
                        Message = "Reservation non trovata"
                    };
                }

                var isActive = reservation.Status == SlotReservationStatus.Pending && DateTime.UtcNow < reservation.ExpiresAt;
                var remaining = Math.Max(0, (int)(reservation.ExpiresAt - DateTime.UtcNow).TotalSeconds);

                return new ReservationStatusResponse
                {
                    IsActive = isActive,
                    ReservationId = reservation.SlotReservationId,
                    RemainingSeconds = remaining,
                    ExpiresAt = reservation.ExpiresAt,
                    Message = isActive ? "Reservation attiva" : "Reservation scaduta o non valida"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il controllo stato reservation {ReservationId}", reservationId);
                return new ReservationStatusResponse
                {
                    IsActive = false,
                    Message = "Errore durante il controllo"
                };
            }
        }

        public async Task<BookingResponseDto> ConfirmReservationAsync(ConfirmReservationRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Trova la reservation
                var reservation = await _context.SlotReservation
                    .FirstOrDefaultAsync(r => r.SlotReservationId == request.ReservationId &&
                                             r.SessionId == request.SessionId);

                if (reservation == null)
                {
                    return new BookingResponseDto
                    {
                        Success = false,
                        Message = "Reservation non trovata"
                    };
                }

                // Verifica che sia ancora valida
                if (reservation.Status != SlotReservationStatus.Pending)
                {
                    return new BookingResponseDto
                    {
                        Success = false,
                        Message = "La reservation non è più valida"
                    };
                }

                if (DateTime.UtcNow >= reservation.ExpiresAt)
                {
                    reservation.Status = SlotReservationStatus.Expired;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new BookingResponseDto
                    {
                        Success = false,
                        Message = "Il tempo per completare la prenotazione è scaduto. Seleziona nuovamente lo slot."
                    };
                }

                // Verifica disponibilità finale (double-check)
                var hasConflict = await _context.Appuntamento
                    .AnyAsync(a => a.SaloneId == reservation.SaloneId &&
                                  a.Data.Date == reservation.Data.Date &&
                                  a.Stato != StatoAppuntamento.Annullato &&
                                  (!reservation.DipendenteId.HasValue || a.DipendenteId == reservation.DipendenteId) &&
                                  a.Slot.OraInizio < reservation.OraFine &&
                                  a.Slot.OraFine > reservation.OraInizio);

                if (hasConflict)
                {
                    reservation.Status = SlotReservationStatus.Released;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new BookingResponseDto
                    {
                        Success = false,
                        Message = "Lo slot non è più disponibile"
                    };
                }

                // Gestione utente
                var cliente = await GetOrCreateCustomer(request);
                if (cliente == null)
                {
                    return new BookingResponseDto
                    {
                        Success = false,
                        Message = "Errore nella gestione dei dati utente"
                    };
                }

                // Trova o crea lo slot nel database
                var slot = await FindOrCreateSlotAsync(reservation.SaloneId, reservation.OraInizio, reservation.OraFine);

                // Carica le entità correlate per evitare che EF Core aggiunga phantom entities dai default del modello
                var salone = await _context.Salone.FindAsync(reservation.SaloneId);
                var servizio = reservation.ServizioId.HasValue
                    ? await _context.Servizio.FindAsync(reservation.ServizioId.Value)
                    : null;
                var dipendente = reservation.DipendenteId.HasValue
                    ? await _context.Dipendente.FindAsync(reservation.DipendenteId.Value)
                    : null;

                // Crea l'appuntamento con tutte le navigation properties per evitare phantom entities
                var appuntamento = new Appuntamento
                {
                    AppuntamentoId = Guid.NewGuid(),
                    SaloneId = reservation.SaloneId,
                    Salone = salone!,
                    ApplicationUserId = cliente.Id,
                    ApplicationUser = cliente,
                    SlotId = slot.SlotId,
                    Slot = slot,
                    DipendenteId = reservation.DipendenteId,
                    Dipendente = dipendente,
                    ServizioId = reservation.ServizioId,
                    Servizio = servizio,
                    Data = reservation.Data,
                    Note = request.Note ?? "",
                    Stato = StatoAppuntamento.Prenotato
                };

                _context.Appuntamento.Add(appuntamento);

                // Marca la reservation come confermata
                reservation.Status = SlotReservationStatus.Confirmed;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Reservation {ReservationId} confermata come Appuntamento {AppuntamentoId}",
                    reservation.SlotReservationId, appuntamento.AppuntamentoId);

                // Invio notifica asincrona
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendBookingConfirmationAsync(appuntamento, cliente);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore invio notifica prenotazione {AppuntamentoId}", appuntamento.AppuntamentoId);
                    }
                });

                return new BookingResponseDto
                {
                    Success = true,
                    Message = "Prenotazione confermata con successo!",
                    AppuntamentoId = appuntamento.AppuntamentoId,
                    RedirectUrl = $"/Search/BookingConfirmation/{appuntamento.AppuntamentoId}"
                };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Conflitto di concorrenza durante la conferma della reservation {ReservationId}", request.ReservationId);

                return new BookingResponseDto
                {
                    Success = false,
                    Message = "Si è verificato un conflitto. Riprova."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Errore durante la conferma della reservation {ReservationId}", request.ReservationId);

                return new BookingResponseDto
                {
                    Success = false,
                    Message = "Si è verificato un errore durante la prenotazione"
                };
            }
        }

        public async Task<bool> IsSlotAvailableAsync(Guid saloneId, DateTime data, TimeSpan oraInizio, TimeSpan oraFine, Guid? dipendenteId, string? excludeSessionId = null)
        {
            try
            {
                // Verifica appuntamenti esistenti
                var hasAppointmentConflict = await _context.Appuntamento
                    .Include(a => a.Slot)
                    .AnyAsync(a => a.SaloneId == saloneId &&
                                  a.Data.Date == data.Date &&
                                  a.Stato != StatoAppuntamento.Annullato &&
                                  (!dipendenteId.HasValue || a.DipendenteId == dipendenteId) &&
                                  a.Slot.OraInizio < oraFine &&
                                  a.Slot.OraFine > oraInizio);

                if (hasAppointmentConflict)
                {
                    return false;
                }

                // Verifica reservation attive (escludendo la sessione corrente se specificata)
                var now = DateTime.UtcNow;
                var hasReservationConflict = await _context.SlotReservation
                    .AnyAsync(r => r.SaloneId == saloneId &&
                                  r.Data.Date == data.Date &&
                                  r.Status == SlotReservationStatus.Pending &&
                                  r.ExpiresAt > now &&
                                  (!dipendenteId.HasValue || r.DipendenteId == dipendenteId) &&
                                  r.OraInizio < oraFine &&
                                  r.OraFine > oraInizio &&
                                  (excludeSessionId == null || r.SessionId != excludeSessionId));

                return !hasReservationConflict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore verifica disponibilità slot");
                return false;
            }
        }

        public async Task<List<SlotStatusDto>> GetSlotsWithStatusAsync(Guid saloneId, DateTime data, Guid? dipendenteId, Guid? servizioId, string sessionId)
        {
            try
            {
                var salone = await _context.Salone
                    .Include(s => s.Orari)
                    .Include(s => s.Servizi)
                    .Include(s => s.Dipendenti)
                        .ThenInclude(d => d.Orari)
                    .FirstOrDefaultAsync(s => s.SaloneId == saloneId);

                if (salone == null)
                {
                    return new List<SlotStatusDto>();
                }

                // Ottieni orario per il giorno: prima controlla il dipendente, poi fallback al salone
                var dayOfWeek = data.DayOfWeek;
                TimeSpan apertura;
                TimeSpan chiusura;

                if (dipendenteId.HasValue)
                {
                    var dipendente = salone.Dipendenti.FirstOrDefault(d => d.DipendenteId == dipendenteId.Value);
                    var orarioDipendente = dipendente?.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);

                    if (orarioDipendente != null)
                    {
                        if (orarioDipendente.IsDayOff)
                            return new List<SlotStatusDto>();

                        apertura = orarioDipendente.Apertura;
                        chiusura = orarioDipendente.Chiusura;
                    }
                    else
                    {
                        // Fallback agli orari del salone
                        var orarioSalone = salone.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);
                        if (orarioSalone == null || orarioSalone.IsDayOff)
                            return new List<SlotStatusDto>();

                        apertura = orarioSalone.Apertura;
                        chiusura = orarioSalone.Chiusura;
                    }
                }
                else
                {
                    var orarioSalone = salone.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);
                    if (orarioSalone == null || orarioSalone.IsDayOff)
                        return new List<SlotStatusDto>();

                    apertura = orarioSalone.Apertura;
                    chiusura = orarioSalone.Chiusura;
                }

                // Ottieni durata del servizio (default 30 min)
                var durataServizio = 30;
                if (servizioId.HasValue)
                {
                    var servizio = salone.Servizi.FirstOrDefault(s => s.ServizioId == servizioId.Value);
                    if (servizio != null)
                    {
                        durataServizio = (int)servizio.Durata;
                    }
                }

                // Genera slot dinamici
                var slots = new List<SlotStatusDto>();
                var currentTime = apertura;

                while (currentTime.Add(TimeSpan.FromMinutes(durataServizio)) <= chiusura)
                {
                    var oraFine = currentTime.Add(TimeSpan.FromMinutes(durataServizio));

                    slots.Add(new SlotStatusDto
                    {
                        SlotId = Guid.NewGuid(), // ID temporaneo per UI
                        OraInizio = currentTime,
                        OraFine = oraFine,
                        TimeSlot = $"{currentTime:hh\\:mm} - {oraFine:hh\\:mm}",
                        Status = SlotStatusType.Available
                    });

                    currentTime = currentTime.Add(TimeSpan.FromMinutes(30)); // Incremento fisso di 30 min
                }

                // Ottieni appuntamenti esistenti
                var appuntamenti = await _context.Appuntamento
                    .Include(a => a.Slot)
                    .Where(a => a.SaloneId == saloneId &&
                               a.Data.Date == data.Date &&
                               a.Stato != StatoAppuntamento.Annullato &&
                               (!dipendenteId.HasValue || a.DipendenteId == dipendenteId))
                    .ToListAsync();

                // Ottieni reservation attive
                var now = DateTime.UtcNow;
                var reservations = await _context.SlotReservation
                    .Where(r => r.SaloneId == saloneId &&
                               r.Data.Date == data.Date &&
                               r.Status == SlotReservationStatus.Pending &&
                               r.ExpiresAt > now &&
                               (!dipendenteId.HasValue || r.DipendenteId == dipendenteId))
                    .ToListAsync();

                // Aggiorna lo stato di ogni slot
                foreach (var slot in slots)
                {
                    // Verifica conflitto con appuntamenti
                    var hasAppointmentConflict = appuntamenti.Any(a =>
                        a.Slot.OraInizio < slot.OraFine && a.Slot.OraFine > slot.OraInizio);

                    if (hasAppointmentConflict)
                    {
                        slot.Status = SlotStatusType.Booked;
                        continue;
                    }

                    // Verifica conflitto con reservation
                    var conflictingReservation = reservations.FirstOrDefault(r =>
                        r.OraInizio < slot.OraFine && r.OraFine > slot.OraInizio);

                    if (conflictingReservation != null)
                    {
                        if (conflictingReservation.SessionId == sessionId)
                        {
                            slot.Status = SlotStatusType.OwnReservation;
                            slot.ReservationId = conflictingReservation.SlotReservationId;
                            slot.SecondsRemaining = conflictingReservation.RemainingSeconds;
                            slot.IsOwnReservation = true;
                        }
                        else
                        {
                            slot.Status = SlotStatusType.Reserved;
                        }
                    }
                }

                // Filtra slot passati se è oggi
                if (data.Date == DateTime.Now.Date)
                {
                    var currentTimeOfDay = DateTime.Now.TimeOfDay;
                    slots = slots.Where(s => s.OraInizio > currentTimeOfDay).ToList();
                }

                return slots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore recupero slot con stato per salone {SaloneId}", saloneId);
                return new List<SlotStatusDto>();
            }
        }

        public async Task<int> CleanupExpiredReservationsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredReservations = await _context.SlotReservation
                    .Where(r => r.Status == SlotReservationStatus.Pending && r.ExpiresAt <= now)
                    .ToListAsync();

                if (expiredReservations.Count == 0)
                {
                    return 0;
                }

                foreach (var reservation in expiredReservations)
                {
                    reservation.Status = SlotReservationStatus.Expired;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Pulite {Count} reservation scadute", expiredReservations.Count);
                return expiredReservations.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la pulizia delle reservation scadute");
                return 0;
            }
        }

        public async Task<SlotReservation?> GetActiveReservationForSessionAsync(string sessionId)
        {
            var now = DateTime.UtcNow;
            return await _context.SlotReservation
                .FirstOrDefaultAsync(r => r.SessionId == sessionId &&
                                         r.Status == SlotReservationStatus.Pending &&
                                         r.ExpiresAt > now);
        }

        #region Metodi privati

        private async Task ReleaseAllReservationsForSessionAsync(string sessionId)
        {
            var activeReservations = await _context.SlotReservation
                .Where(r => r.SessionId == sessionId && r.Status == SlotReservationStatus.Pending)
                .ToListAsync();

            foreach (var reservation in activeReservations)
            {
                reservation.Status = SlotReservationStatus.Released;
            }

            if (activeReservations.Count > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Rilasciate {Count} reservation precedenti per sessione {SessionId}", activeReservations.Count, sessionId);
            }
        }

        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException?.Message.Contains("IX_SlotReservation_UniqueActive") == true ||
                   ex.InnerException?.Message.Contains("UNIQUE") == true ||
                   ex.InnerException?.Message.Contains("duplicate") == true;
        }

        private async Task<ApplicationUser?> GetOrCreateCustomer(ConfirmReservationRequest request)
        {
            try
            {
                // Se l'utente è autenticato, usa quello
                if (!string.IsNullOrEmpty(request.UserId))
                {
                    return await _userManager.FindByIdAsync(request.UserId);
                }

                // Se no, cerca per email o crea nuovo utente guest
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var existingUser = await _userManager.FindByEmailAsync(request.Email);
                    if (existingUser != null)
                    {
                        return existingUser;
                    }

                    // Crea nuovo utente guest
                    var guestUser = new ApplicationUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        Email = request.Email,
                        UserName = request.Email,
                        Nome = request.Nome ?? "",
                        Cognome = request.Cognome ?? "",
                        PhoneNumber = request.Telefono ?? "",
                        EmailConfirmed = false
                    };

                    var result = await _userManager.CreateAsync(guestUser);
                    return result.Succeeded ? guestUser : null;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore gestione utente");
                return null;
            }
        }

        private async Task<Slot> FindOrCreateSlotAsync(Guid saloneId, TimeSpan oraInizio, TimeSpan oraFine)
        {
            var existingSlot = await _context.Slot
                .FirstOrDefaultAsync(s => s.SaloneId == saloneId &&
                                         s.OraInizio == oraInizio &&
                                         s.OraFine == oraFine);

            if (existingSlot != null)
            {
                return existingSlot;
            }

            var newSlot = new Slot
            {
                SlotId = Guid.NewGuid(),
                SaloneId = saloneId,
                OraInizio = oraInizio,
                OraFine = oraFine,
                GiorniSettimana = "0,1,2,3,4,5,6",
                Capacita = 1,
                IsAttivo = true,
                Note = "Auto-generated"
            };

            _context.Slot.Add(newSlot);
            await _context.SaveChangesAsync();

            return newSlot;
        }

        private async Task SendBookingConfirmationAsync(Appuntamento appuntamento, ApplicationUser cliente)
        {
            try
            {
                // Carica i dati necessari
                var salone = await _context.Salone.FindAsync(appuntamento.SaloneId);
                var servizio = appuntamento.ServizioId.HasValue
                    ? await _context.Servizio.FindAsync(appuntamento.ServizioId.Value)
                    : null;
                var slot = await _context.Slot.FindAsync(appuntamento.SlotId);

                if (salone != null && slot != null && !string.IsNullOrEmpty(cliente.Email))
                {
                    var notification = new BookingNotificationDto
                    {
                        AppuntamentoId = appuntamento.AppuntamentoId,
                        EmailDestinatario = cliente.Email,
                        TipoNotifica = "Conferma",
                        Messaggio = $"La tua prenotazione presso {salone.Nome} è stata confermata per il {appuntamento.Data:dd/MM/yyyy} alle {slot.OraInizio:hh\\:mm}.",
                        InviaEmail = true,
                        ParametriTemplate = new Dictionary<string, string>
                        {
                            ["NomeSalone"] = salone.Nome,
                            ["DataAppuntamento"] = appuntamento.Data.ToString("dd/MM/yyyy"),
                            ["OrarioAppuntamento"] = $"{slot.OraInizio:hh\\:mm} - {slot.OraFine:hh\\:mm}",
                            ["ServizioNome"] = servizio?.Nome ?? "Servizio",
                            ["IndirizzoSalone"] = $"{salone.Indirizzo}, {salone.Citta}"
                        }
                    };

                    await _emailService.SendBookingNotificationAsync(notification);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore invio conferma prenotazione {AppuntamentoId}", appuntamento.AppuntamentoId);
            }
        }

        #endregion
    }
}
