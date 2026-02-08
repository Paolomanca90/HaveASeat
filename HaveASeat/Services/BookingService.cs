using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.Utilities.Dto;
using HaveASeat.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace HaveASeat.Services
{
	public interface IBookingService
	{
		Task<bool> IsSlotAvailableAsync(Guid saloneId, Guid slotId, DateTime data, Guid? dipendenteId = null);
		Task<List<SlotAvailabilityDto>> GetAvailableSlotsAsync(Guid saloneId, DateTime data, Guid? dipendenteId = null);
		Task<BookingResponseDto> CreateBookingAsync(CreateBookingDto request, string? userId = null);
		Task<bool> CanCancelBookingAsync(Guid appuntamentoId, string? userId = null);
		Task<bool> CancelBookingAsync(Guid appuntamentoId, string? userId = null, string? motivo = null);
		Task<BookingDetailsDto?> GetBookingDetailsAsync(Guid appuntamentoId, string? userId = null);
		Task<List<UserBookingViewModel>> GetUserBookingsAsync(string userId);
		Task<BookingResponseDto> UpdateBookingAsync(UpdateBookingDto request, string userId);
		Task<bool> AddReviewAsync(AddReviewDto request, string userId);
		Task SendBookingNotificationAsync(BookingNotificationDto notification);
	}

	public class BookingService : IBookingService
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<BookingService> _logger;
		private readonly IEmailService _emailService;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IPushNotificationService _pushService;

		public BookingService(
			ApplicationDbContext context,
			ILogger<BookingService> logger,
			IEmailService emailService,
			UserManager<ApplicationUser> userManager,
			IPushNotificationService pushService)
		{
			_context = context;
			_logger = logger;
			_emailService = emailService;
			_userManager = userManager;
			_pushService = pushService;
		}

		public async Task<bool> IsSlotAvailableAsync(Guid saloneId, Guid slotId, DateTime data, Guid? dipendenteId = null)
		{
			try
			{
				var existing = await _context.Appuntamento
					.AnyAsync(a => a.SaloneId == saloneId &&
								  a.SlotId == slotId &&
								  a.Data.Date == data.Date &&
								  a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato &&
								  (!dipendenteId.HasValue || a.DipendenteId == dipendenteId));

				return !existing;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore controllo disponibilità slot {SlotId}", slotId);
				return false;
			}
		}

		public async Task<List<SlotAvailabilityDto>> GetAvailableSlotsAsync(Guid saloneId, DateTime data, Guid? dipendenteId = null)
		{
			try
			{
				var salone = await _context.Salone
					.Include(s => s.Slots)
					.Include(s => s.Orari)
					.FirstOrDefaultAsync(s => s.SaloneId == saloneId);

				if (salone == null)
					return new List<SlotAvailabilityDto>();

				var dayOfWeek = data.DayOfWeek;
				var orarioSalone = salone.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);

				if (orarioSalone == null || orarioSalone.IsDayOff)
					return new List<SlotAvailabilityDto>();

				// Verifica orari dipendente se specificato
				if (dipendenteId.HasValue)
				{
					var dipendente = await _context.Dipendente
						.Include(d => d.Orari)
						.FirstOrDefaultAsync(d => d.DipendenteId == dipendenteId.Value);

					if (dipendente?.Orari.Any() == true)
					{
						var orarioDipendente = dipendente.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);
						if (orarioDipendente != null && orarioDipendente.IsDayOff)
							return new List<SlotAvailabilityDto>();
					}
				}

				var dayNumber = (int)dayOfWeek;
				var slotsDisponibili = salone.Slots
					.Where(s => s.IsAttivo && s.GiorniSettimana.Contains(dayNumber.ToString()))
					.Where(s => s.OraInizio >= orarioSalone.Apertura && s.OraFine <= orarioSalone.Chiusura)
					.OrderBy(s => s.OraInizio)
					.ToList();

				var appuntamentiEsistenti = await _context.Appuntamento
					.Where(a => a.SaloneId == saloneId &&
							   a.Data.Date == data.Date &&
							   a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato)
					.Where(a => !dipendenteId.HasValue || a.DipendenteId == dipendenteId)
					.ToListAsync();

				return slotsDisponibili
					.Where(slot => !appuntamentiEsistenti.Any(a => a.SlotId == slot.SlotId))
					.Select(slot => new SlotAvailabilityDto
					{
						SlotId = slot.SlotId,
						OraInizio = slot.OraInizio.ToString(@"HH\:mm"),
						OraFine = slot.OraFine.ToString(@"HH\:mm"),
						Display = $"{slot.OraInizio:HH\\:mm} - {slot.OraFine:HH\\:mm}",
						Disponibile = true,
						PostiLiberi = Math.Max(0, slot.Capacita - appuntamentiEsistenti.Count(a => a.SlotId == slot.SlotId)),
						Capacita = slot.Capacita
					})
					.ToList();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore recupero slot disponibili per salone {SaloneId}", saloneId);
				return new List<SlotAvailabilityDto>();
			}
		}

		public async Task<BookingResponseDto> CreateBookingAsync(CreateBookingDto request, string? userId = null)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();

			try
			{
				// Validazioni
				var validationResult = await ValidateBookingRequest(request);
				if (!validationResult.IsValid)
				{
					return new BookingResponseDto
					{
						Success = false,
						Message = validationResult.ErrorMessage
					};
				}

				// Gestione utente
				var cliente = await GetOrCreateCustomer(request, userId);
				if (cliente == null)
				{
					return new BookingResponseDto
					{
						Success = false,
						Message = "Errore nella gestione dei dati utente"
					};
				}

				// Verifica disponibilità finale (double-check per race conditions)
				if (!await IsSlotAvailableAsync(request.SaloneId, request.SlotId.Value, request.Data, request.DipendenteId))
				{
					return new BookingResponseDto
					{
						Success = false,
						Message = "Lo slot selezionato non è più disponibile"
					};
				}

				// Creazione appuntamento
				var appuntamento = new Appuntamento
				{
					AppuntamentoId = Guid.NewGuid(),
					SaloneId = request.SaloneId,
					ApplicationUserId = cliente.Id,
					SlotId = request.SlotId.Value,
					DipendenteId = request.DipendenteId,
					ServizioId = request.ServizioId,
					Data = request.Data,
					Note = request.Note ?? "",
					Stato = HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato
				};

				_context.Appuntamento.Add(appuntamento);
				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				// Invio notifica asincrona
				_ = Task.Run(async () =>
				{
					try
					{
						await SendBookingConfirmation(appuntamento);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Errore invio notifica prenotazione {AppuntamentoId}", appuntamento.AppuntamentoId);
					}

					// Notifica push al proprietario del salone
					try
					{
						var salone = await _context.Salone
							.Include(s => s.Servizi)
							.FirstOrDefaultAsync(s => s.SaloneId == appuntamento.SaloneId);
						if (salone != null && !string.IsNullOrEmpty(salone.ApplicationUserId))
						{
							var servizioNome = salone.Servizi?.FirstOrDefault(s => s.ServizioId == appuntamento.ServizioId)?.Nome ?? "Servizio";
							await _pushService.SendNotificationAsync(
								salone.ApplicationUserId,
								"Nuova Prenotazione",
								$"Nuovo appuntamento per {servizioNome} il {appuntamento.Data:dd/MM/yyyy}",
								"/Partner/Appuntamenti",
								$"booking-{appuntamento.AppuntamentoId}");
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Errore invio push al salone per appuntamento {AppuntamentoId}", appuntamento.AppuntamentoId);
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
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError(ex, "Errore durante la creazione della prenotazione");

				return new BookingResponseDto
				{
					Success = false,
					Message = "Si è verificato un errore durante la prenotazione"
				};
			}
		}

		public async Task<bool> CanCancelBookingAsync(Guid appuntamentoId, string? userId = null)
		{
			try
			{
				var appuntamento = await _context.Appuntamento
					.FirstOrDefaultAsync(a => a.AppuntamentoId == appuntamentoId);

				if (appuntamento == null) return false;

				// Verifica autorizzazione
				if (userId != null && appuntamento.ApplicationUserId != userId) return false;

				// Verifica che sia cancellabile (almeno 24 ore prima)
				var limiteCancellazione = DateTime.Now.AddHours(24);
				return appuntamento.Data > limiteCancellazione &&
					   appuntamento.Stato == HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore controllo cancellabilità prenotazione {AppuntamentoId}", appuntamentoId);
				return false;
			}
		}

		public async Task<bool> CancelBookingAsync(Guid appuntamentoId, string? userId = null, string? motivo = null)
		{
			try
			{
				var appuntamento = await _context.Appuntamento
					.Include(a => a.Salone)
					.Include(a => a.ApplicationUser)
					.Include(a => a.Servizio)
					.FirstOrDefaultAsync(a => a.AppuntamentoId == appuntamentoId);

				if (appuntamento == null) return false;

				if (!await CanCancelBookingAsync(appuntamentoId, userId)) return false;

				appuntamento.Stato = HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato;
				if (!string.IsNullOrEmpty(motivo))
				{
					appuntamento.Note += $"\nCancellato: {motivo}";
				}

				await _context.SaveChangesAsync();

				// Invio notifica cancellazione
				_ = Task.Run(async () =>
				{
					try
					{
						await SendCancellationNotification(appuntamento, motivo);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Errore invio notifica cancellazione {AppuntamentoId}", appuntamentoId);
					}

					// Notifica push al proprietario del salone
					try
					{
						if (!string.IsNullOrEmpty(appuntamento.Salone?.ApplicationUserId))
						{
							var servizioNome = appuntamento.Servizio?.Nome ?? "Servizio";
							await _pushService.SendNotificationAsync(
								appuntamento.Salone.ApplicationUserId,
								"Prenotazione Cancellata",
								$"L'appuntamento per {servizioNome} del {appuntamento.Data:dd/MM/yyyy} è stato cancellato",
								"/Partner/Appuntamenti",
								$"cancel-{appuntamento.AppuntamentoId}");
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Errore invio push cancellazione al salone per {AppuntamentoId}", appuntamentoId);
					}
				});

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore cancellazione prenotazione {AppuntamentoId}", appuntamentoId);
				return false;
			}
		}

		public async Task<BookingDetailsDto?> GetBookingDetailsAsync(Guid appuntamentoId, string? userId = null)
		{
			try
			{
				var appuntamento = await _context.Appuntamento
					.Include(a => a.Salone)
					.Include(a => a.ApplicationUser)
					.Include(a => a.Servizio)
					.Include(a => a.Dipendente)
						.ThenInclude(d => d.ApplicationUser)
					.Include(a => a.Slot)
					.FirstOrDefaultAsync(a => a.AppuntamentoId == appuntamentoId);

				if (appuntamento == null) return null;

				// Verifica autorizzazione
				if (userId != null && appuntamento.ApplicationUserId != userId) return null;

				return new BookingDetailsDto
				{
					AppuntamentoId = appuntamento.AppuntamentoId,
					NomeSalone = appuntamento.Salone.Nome,
					IndirizzoSalone = $"{appuntamento.Salone.Indirizzo}, {appuntamento.Salone.Citta}",
					TelefonoSalone = appuntamento.Salone.Telefono,
					NomeServizio = appuntamento.Servizio?.Nome ?? "Servizio non specificato",
					DataAppuntamento = appuntamento.Data,
					OrarioAppuntamento = $"{appuntamento.Slot.OraInizio:HH\\:mm} - {appuntamento.Slot.OraFine:HH\\:mm}",
					NomeDipendente = appuntamento.Dipendente != null ?
						$"{appuntamento.Dipendente.ApplicationUser.Nome} {appuntamento.Dipendente.ApplicationUser.Cognome}" : null,
					PrezzoTotale = appuntamento.Servizio?.PrezzoEffettivo ?? 0,
					Note = appuntamento.Note,
					StatoAppuntamento = appuntamento.Stato.ToString(),
					CanCancel = await CanCancelBookingAsync(appuntamentoId, userId)
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore recupero dettagli prenotazione {AppuntamentoId}", appuntamentoId);
				return null;
			}
		}

		public async Task<List<UserBookingViewModel>> GetUserBookingsAsync(string userId)
		{
			try
			{
				var appuntamenti = await _context.Appuntamento
					.Include(a => a.Salone)
					.Include(a => a.Servizio)
					.Include(a => a.Dipendente)
						.ThenInclude(d => d.ApplicationUser)
					.Include(a => a.Slot)
					.Where(a => a.ApplicationUserId == userId)
					.OrderByDescending(a => a.Data)
					.ToListAsync();

				return appuntamenti.Select(a => new UserBookingViewModel
				{
					AppuntamentoId = a.AppuntamentoId,
					SaloneId = a.SaloneId,
					ServizioId = a.ServizioId ?? Guid.Empty,
					NomeSalone = a.Salone.Nome,
					IndirizzoSalone = $"{a.Salone.Indirizzo}, {a.Salone.Citta}",
					TelefonoSalone = a.Salone.Telefono,
					NomeServizio = a.Servizio?.Nome ?? "Servizio non specificato",
					DataAppuntamento = a.Data,
					OrarioAppuntamento = $"{a.Slot.OraInizio:HH\\:mm} - {a.Slot.OraFine:HH\\:mm}",
					NomeDipendente = a.Dipendente != null ?
						$"{a.Dipendente.ApplicationUser.Nome} {a.Dipendente.ApplicationUser.Cognome}" : null,
					Prezzo = a.Servizio?.PrezzoEffettivo ?? 0,
					Stato = a.Stato.ToString(),
					Note = a.Note,
					CanCancel = a.Stato == HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato &&
							   a.Data > DateTime.Now.AddHours(24)
				}).ToList();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore recupero prenotazioni utente {UserId}", userId);
				return new List<UserBookingViewModel>();
			}
		}

		// Correzione per il metodo UpdateBookingAsync nel BookingService
		// Sostituisci il metodo esistente con questa versione

		public async Task<BookingResponseDto> UpdateBookingAsync(UpdateBookingDto request, string userId)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();

			try
			{
				var appuntamento = await _context.Appuntamento
					.Include(a => a.Salone)
					.Include(a => a.Slot)
					.Include(a => a.Servizio)
					.Include(a => a.ApplicationUser)
					.FirstOrDefaultAsync(a => a.AppuntamentoId == request.AppuntamentoId &&
											a.ApplicationUserId == userId);

				if (appuntamento == null)
				{
					return new BookingResponseDto
					{
						Success = false,
						Message = "Prenotazione non trovata"
					};
				}

				// Verifica se è modificabile
				if (appuntamento.Data <= DateTime.Now.AddHours(24))
				{
					return new BookingResponseDto
					{
						Success = false,
						Message = "Non è possibile modificare la prenotazione con meno di 24 ore di anticipo"
					};
				}

				bool hasChanges = false;

				// Applica modifiche data
				if (request.NuovaData.HasValue && request.NuovaData.Value.Date != appuntamento.Data.Date)
				{
					appuntamento.Data = request.NuovaData.Value;
					hasChanges = true;
				}

				// Applica modifiche orario (CORRETTO - usa NuovoTimeSlot)
				if (!string.IsNullOrEmpty(request.NuovoTimeSlot))
				{
					var timeSlots = request.NuovoTimeSlot.Split(" - ");
					if (timeSlots.Length == 2 &&
						TimeSpan.TryParse(timeSlots[0], out var nuovaOraInizio) &&
						TimeSpan.TryParse(timeSlots[1], out var nuovaOraFine))
					{
						var dataVerifica = request.NuovaData ?? appuntamento.Data;

						// Verifica disponibilità nuovo slot usando ISlotService se disponibile
						// Oppure logica di verifica manuale
						var isAvailable = await IsSlotAvailableForUpdate(
							appuntamento.SaloneId,
							dataVerifica,
							nuovaOraInizio,
							nuovaOraFine,
							request.NuovoDipendenteId ?? appuntamento.DipendenteId,
							appuntamento.AppuntamentoId);

						if (!isAvailable)
						{
							return new BookingResponseDto
							{
								Success = false,
								Message = "Il nuovo orario selezionato non è disponibile"
							};
						}

						// Trova o crea nuovo slot
						var nuovoSlot = await FindOrCreateSlotForBooking(appuntamento.SaloneId, nuovaOraInizio, nuovaOraFine);
						appuntamento.SlotId = nuovoSlot.SlotId;
						hasChanges = true;
					}
					else
					{
						return new BookingResponseDto
						{
							Success = false,
							Message = "Formato orario non valido"
						};
					}
				}

				// Applica modifiche dipendente
				if (request.NuovoDipendenteId.HasValue && request.NuovoDipendenteId.Value != appuntamento.DipendenteId)
				{
					// Verifica che il dipendente esista e offra il servizio
					var dipendente = await _context.Dipendente
						.Include(d => d.ServiziOfferti)
						.FirstOrDefaultAsync(d => d.DipendenteId == request.NuovoDipendenteId.Value);

					if (dipendente == null)
					{
						return new BookingResponseDto
						{
							Success = false,
							Message = "Dipendente non trovato"
						};
					}

					if (appuntamento.ServizioId.HasValue &&
						!dipendente.ServiziOfferti.Any(so => so.ServizioId == appuntamento.ServizioId.Value))
					{
						return new BookingResponseDto
						{
							Success = false,
							Message = "Il dipendente selezionato non offre questo servizio"
						};
					}

					appuntamento.DipendenteId = request.NuovoDipendenteId.Value;
					hasChanges = true;
				}

				// Applica modifiche servizio
				if (request.NuovoServizioId.HasValue && request.NuovoServizioId.Value != appuntamento.ServizioId)
				{
					var nuovoServizio = await _context.Servizio
						.FirstOrDefaultAsync(s => s.ServizioId == request.NuovoServizioId.Value);

					if (nuovoServizio == null)
					{
						return new BookingResponseDto
						{
							Success = false,
							Message = "Servizio non trovato"
						};
					}

					appuntamento.ServizioId = request.NuovoServizioId.Value;
					hasChanges = true;
				}

				// Applica modifiche note
				if (!string.IsNullOrEmpty(request.Note) && request.Note != appuntamento.Note)
				{
					appuntamento.Note = request.Note;
					hasChanges = true;
				}

				if (hasChanges)
				{
					await _context.SaveChangesAsync();
					await transaction.CommitAsync();

					// Invia notifica se richiesto
					if (request.InviaNotifica)
					{
						_ = Task.Run(async () =>
						{
							try
							{
								await SendUpdateNotification(appuntamento);
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, "Errore invio notifica modifica prenotazione {AppuntamentoId}", appuntamento.AppuntamentoId);
							}
						});
					}

					return new BookingResponseDto
					{
						Success = true,
						Message = "Prenotazione modificata con successo",
						AppuntamentoId = appuntamento.AppuntamentoId
					};
				}

				return new BookingResponseDto
				{
					Success = false,
					Message = "Nessuna modifica da applicare"
				};
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError(ex, "Errore modifica prenotazione {AppuntamentoId}", request.AppuntamentoId);

				return new BookingResponseDto
				{
					Success = false,
					Message = "Errore durante la modifica della prenotazione"
				};
			}
		}

		// Helper methods per il BookingService
		private async Task<bool> IsSlotAvailableForUpdate(Guid saloneId, DateTime data, TimeSpan oraInizio, TimeSpan oraFine, Guid? dipendenteId, Guid excludeAppuntamentoId)
		{
			var conflictingAppointments = await _context.Appuntamento
				.Include(a => a.Slot)
				.Where(a => a.SaloneId == saloneId &&
						   a.Data.Date == data.Date &&
						   a.AppuntamentoId != excludeAppuntamentoId &&
						   a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato)
				.Where(a => !dipendenteId.HasValue || a.DipendenteId == dipendenteId)
				.ToListAsync();

			foreach (var appointment in conflictingAppointments)
			{
				var existingStart = appointment.OraInizio.TimeOfDay;
				var existingEnd = appointment.OraFine.TimeOfDay;

				// Controlla sovrapposizione
				if (oraInizio < existingEnd && oraFine > existingStart)
				{
					return false;
				}
			}

			return true;
		}

		private async Task<Slot> FindOrCreateSlotForBooking(Guid saloneId, TimeSpan oraInizio, TimeSpan oraFine)
		{
			// Cerca slot esistente
			var existingSlot = await _context.Slot
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId &&
										s.OraInizio == oraInizio &&
										s.OraFine == oraFine);

			if (existingSlot != null)
			{
				return existingSlot;
			}

			// Crea nuovo slot
			var newSlot = new Slot
			{
				SlotId = Guid.NewGuid(),
				SaloneId = saloneId,
				OraInizio = oraInizio,
				OraFine = oraFine,
				GiorniSettimana = "0,1,2,3,4,5,6",
				Capacita = 1,
				IsAttivo = true,
				Note = "Auto-generated for booking update"
			};

			_context.Slot.Add(newSlot);
			await _context.SaveChangesAsync();

			return newSlot;
		}

		private async Task SendUpdateNotification(Appuntamento appuntamento)
		{
			var notification = new BookingNotificationDto
			{
				AppuntamentoId = appuntamento.AppuntamentoId,
				EmailDestinatario = appuntamento.ApplicationUser.Email,
				TipoNotifica = "modifica",
				Messaggio = "La tua prenotazione è stata modificata con successo",
				InviaEmail = true,
				ParametriTemplate = new Dictionary<string, string>
				{
					["NomeSalone"] = appuntamento.Salone.Nome,
					["DataAppuntamento"] = appuntamento.Data.ToString("dd/MM/yyyy"),
					["OrarioAppuntamento"] = $"{appuntamento.OraInizio:HH:mm} - {appuntamento.OraFine:HH:mm}",
					["ServizioNome"] = appuntamento.Servizio?.Nome ?? "Servizio"
				}
			};

			await SendBookingNotificationAsync(notification);
		}

		public async Task<bool> AddReviewAsync(AddReviewDto request, string userId)
		{
			try
			{
				var appuntamento = await _context.Appuntamento
					.Include(a => a.Salone)
					.FirstOrDefaultAsync(a => a.AppuntamentoId == request.AppuntamentoId &&
											a.ApplicationUserId == userId &&
											a.Data < DateTime.Now &&
											a.Stato == HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato);

				if (appuntamento == null) return false;

				// Crea nuova recensione
				var recensione = new Recensione
				{
					RecensioneId = Guid.NewGuid(),
					ApplicationUserId = userId,
					SaloneId = appuntamento.SaloneId,
					ApplicationUser = await _userManager.FindByIdAsync(userId),
					DipendenteId = request.DipendenteId,
					Dipendente = request.DipendenteId.HasValue
						? await _context.Dipendente.FindAsync(request.DipendenteId.Value)
						: null,
					Salone = appuntamento.Salone,
					Voto = request.Rating,
					Commento = request.Comment,
					DataCreazione = DateTime.Now
				};

				_context.Recensione.Add(recensione);
				await _context.SaveChangesAsync();

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore aggiunta recensione per appuntamento {AppuntamentoId}", request.AppuntamentoId);
				return false;
			}
		}

		public async Task SendBookingNotificationAsync(BookingNotificationDto notification)
		{
			try
			{
				await _emailService.SendBookingNotificationAsync(notification);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore invio notifica prenotazione");
			}
		}

		#region Metodi privati di supporto

		private async Task<(bool IsValid, string ErrorMessage)> ValidateBookingRequest(CreateBookingDto request)
		{
			try
			{
				// Verifica che la data sia futura
				if (request.Data <= DateTime.Now)
				{
					return (false, "La data selezionata deve essere futura");
				}

				// Verifica che il salone esista
				var salone = await _context.Salone
					.FirstOrDefaultAsync(s => s.SaloneId == request.SaloneId);

				if (salone == null)
				{
					return (false, "Salone non trovato");
				}

				// Verifica che lo slot esista
				var slot = await _context.Slot
					.FirstOrDefaultAsync(s => s.SlotId == request.SlotId);

				if (slot == null)
				{
					return (false, "Slot non trovato");
				}

				// Verifica dipendente se specificato
				if (request.DipendenteId.HasValue)
				{
					var dipendente = await _context.Dipendente
						.FirstOrDefaultAsync(d => d.DipendenteId == request.DipendenteId.Value);

					if (dipendente == null)
					{
						return (false, "Dipendente non trovato");
					}
				}

				// Verifica servizio se specificato
				if (request.ServizioId != Guid.Empty)
				{
					var servizio = await _context.Servizio
						.FirstOrDefaultAsync(s => s.ServizioId == request.ServizioId);

					if (servizio == null)
					{
						return (false, "Servizio non trovato");
					}
				}

				return (true, string.Empty);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore validazione richiesta prenotazione");
				return (false, "Errore durante la validazione");
			}
		}

		private async Task<ApplicationUser?> GetOrCreateCustomer(CreateBookingDto request, string? userId)
		{
			try
			{
				// Se l'utente è autenticato, usa quello
				if (!string.IsNullOrEmpty(userId))
				{
					return await _userManager.FindByIdAsync(userId);
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

		private async Task SendBookingConfirmation(Appuntamento appuntamento)
		{
			try
			{
				var bookingDetails = await GetBookingDetailsAsync(appuntamento.AppuntamentoId);
				if (bookingDetails != null)
				{
					var notification = new BookingNotificationDto
					{
						AppuntamentoId = appuntamento.AppuntamentoId,
						EmailDestinatario = appuntamento.ApplicationUser.Email,
						Messaggio = $"La tua prenotazione per {bookingDetails.NomeServizio} presso {bookingDetails.NomeSalone} " +
								   $"è stata confermata per il {bookingDetails.DataAppuntamento:dd/MM/yyyy} " +
								   $"dalle {bookingDetails.OrarioAppuntamento}.",
						TipoNotifica = "Conferma"
					};

					await SendBookingNotificationAsync(notification);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore invio conferma prenotazione {AppuntamentoId}", appuntamento.AppuntamentoId);
			}
		}

		private async Task SendCancellationNotification(Appuntamento appuntamento, string? motivo)
		{
			try
			{
				var notification = new BookingNotificationDto
				{
					AppuntamentoId = appuntamento.AppuntamentoId,
					EmailDestinatario = appuntamento.ApplicationUser.Email,
					TipoNotifica = "Cancellazione",
					Messaggio = motivo
				};

				await SendBookingNotificationAsync(notification);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore invio notifica cancellazione {AppuntamentoId}", appuntamento.AppuntamentoId);
			}
		}

		#endregion
	}
}