using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.ViewModels;
using HaveASeat.Utilities.Dto;
using HaveASeat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HaveASeat.Controllers
{
	public class SearchController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<SearchController> _logger;
		private readonly ISlotService _slotService;
		private readonly ISlotReservationService _slotReservationService;
		private readonly IBookingService _bookingService;

		public SearchController(
			ApplicationDbContext context,
			ILogger<SearchController> logger,
			ISlotService slotService,
			ISlotReservationService slotReservationService,
			IBookingService bookingService)
		{
			_context = context;
			_logger = logger;
			_slotService = slotService;
			_slotReservationService = slotReservationService;
			_bookingService = bookingService;
		}

		[HttpGet]
		public async Task<IActionResult> Index(SearchFilterDto filters, int page = 1, string sort = "rating")
		{
			const int pageSize = 12;

			try
			{
				var query = _context.Salone
					.Include(s => s.SalonePersonalizzazione)
					.Include(s => s.Immagini)
					.Include(s => s.Servizi)
					.Include(s => s.Dipendenti)
						.ThenInclude(d => d.ApplicationUser)
					.Include(s => s.Recensioni)
					.Include(s => s.SaloneCategorie)
						.ThenInclude(sc => sc.Categoria)
					.Include(s => s.SaloneAbbonamenti)
						.ThenInclude(sa => sa.Abbonamento)
					.Where(s => s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo);

				// Applica filtri
				query = ApplyFilters(query, filters);

				// Applica ordinamento
				query = ApplySorting(query, sort);

				var totalItems = await query.CountAsync();
				var saloni = await query
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.ToListAsync();

				var categorie = await _context.Categoria
					.OrderBy(c => c.Nome)
					.ToListAsync();

				var viewModel = new SearchResultsViewModel
				{
					Saloni = saloni.Select(MapToSearchResult).ToList(),
					SearchQuery = filters.Query ?? string.Empty,
					Location = filters.Location ?? string.Empty,
					CategoriaSelezionata = filters.Categoria ?? string.Empty,
					DataSelezionata = filters.Data,
					Categorie = categorie,
					CurrentPage = page,
					TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
					TotalResults = totalItems,
					SortBy = sort
				};

				// Se è una richiesta AJAX, restituisci solo i risultati
				if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
				{
					return PartialView("_SearchResults", viewModel);
				}

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore durante la ricerca");
				return View("Error");
			}
		}

		private IQueryable<Salone> ApplySorting(IQueryable<Salone> query, string sort)
		{
			return sort?.ToLower() switch
			{
				"rating" => query.OrderByDescending(s => s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto) : 0)
								.ThenByDescending(s => s.Recensioni.Count),

				"distance" => query.OrderBy(s => s.Nome),

				"price_low" => query.OrderBy(s => s.Servizi.Any() ? s.Servizi.Min(serv => serv.PrezzoEffettivo) : 999999),

				"price_high" => query.OrderByDescending(s => s.Servizi.Any() ? s.Servizi.Max(serv => serv.PrezzoEffettivo) : 0),

				"newest" => query.OrderByDescending(s => s.DataCreazione),

				"name" => query.OrderBy(s => s.Nome),

				"reviews" => query.OrderByDescending(s => s.Recensioni.Count)
								 .ThenByDescending(s => s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto) : 0),

				_ => query.OrderByDescending(s => s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto) : 0)
						 .ThenByDescending(s => s.Recensioni.Count)
			};
		}

		[HttpGet]
		public async Task<IActionResult> Details(Guid id)
		{
			try
			{
				var salone = await _context.Salone
					.Include(s => s.SalonePersonalizzazione)
					.Include(s => s.Immagini)
					.Include(s => s.Servizi)
					.Include(s => s.Dipendenti)
						.ThenInclude(d => d.ApplicationUser)
					.Include(s => s.Dipendenti)
						.ThenInclude(d => d.ServiziOfferti)
							.ThenInclude(ds => ds.Servizio)
					.Include(s => s.Dipendenti)
						.ThenInclude(d => d.Orari)
					.Include(s => s.Recensioni)
						.ThenInclude(r => r.ApplicationUser)
					.Include(s => s.Orari)
					.Include(s => s.SaloneAbbonamenti)
						.ThenInclude(sa => sa.Abbonamento)
					.FirstOrDefaultAsync(s => s.SaloneId == id && s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo);

				if (salone == null)
				{
					return NotFound("Salone non trovato");
				}

				var viewModel = BuildDetailsViewModel(salone);
				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore nel caricamento dettagli salone {SaloneId}", id);
				return View("Error");
			}
		}

		[HttpGet]
		public async Task<IActionResult> Profilo(Guid id)
		{
			try
			{
				var salone = await _context.Salone
					.Include(s => s.SalonePersonalizzazione)
					.Include(s => s.Immagini)
					.Include(s => s.Servizi)
					.Include(s => s.Dipendenti)
						.ThenInclude(d => d.ApplicationUser)
					.Include(s => s.Dipendenti)
						.ThenInclude(d => d.ServiziOfferti)
							.ThenInclude(ds => ds.Servizio)
					.Include(s => s.Recensioni)
						.ThenInclude(r => r.ApplicationUser)
					.Include(s => s.Orari)
					.FirstOrDefaultAsync(s => s.SaloneId == id && s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo);

				if (salone == null)
				{
					return NotFound("Salone non trovato");
				}

				return View(salone);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore nel caricamento profilo salone {SaloneId}", id);
				return View("Error");
			}
		}

		[HttpGet]
		public async Task<IActionResult> Suggestions(string q, int limit = 5)
		{
			if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
			{
				return Json(new { suggestions = new List<object>() });
			}

			try
			{
				var suggestions = await GetSearchSuggestions(q, limit);
				return Json(new { suggestions });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore nei suggerimenti di ricerca");
				return Json(new { suggestions = new List<object>() });
			}
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> MyBookings()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

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

				var viewModel = appuntamenti.Select(MapToUserBookingViewModel).ToList();
				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore nel caricamento prenotazioni utente {UserId}", userId);
				return View("Error");
			}
		}

		// NUOVO METODO: Ottieni slot disponibili con il nuovo sistema
		[HttpGet]
		public async Task<IActionResult> GetAvailableSlots(Guid saloneId, DateTime data, Guid? dipendenteId = null, Guid? servizioId = null)
		{
			try
			{
				_logger.LogInformation($"Getting available slots for salone {saloneId}, data {data:yyyy-MM-dd}, dipendente {dipendenteId}, servizio {servizioId}");

				var availableSlots = await _slotService.GetAvailableSlotsAsync(saloneId, data, dipendenteId, servizioId);

				return Json(new { success = true, slots = availableSlots });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore nel caricamento slot per salone {SaloneId}", saloneId);
				return Json(new { success = false, message = "Errore nel caricamento degli orari" });
			}
		}

		#region Slot Reservation Endpoints (Sistema di blocco temporaneo)

		/// <summary>
		/// Ottiene gli slot con informazioni sullo stato di reservation
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> GetSlotsWithStatus(Guid saloneId, DateTime data, Guid? dipendenteId = null, Guid? servizioId = null)
		{
			try
			{
				var sessionId = GetOrCreateSessionId();
				var slots = await _slotReservationService.GetSlotsWithStatusAsync(saloneId, data, dipendenteId, servizioId, sessionId);
				return Json(new { success = true, slots, sessionId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore nel caricamento slot con stato per salone {SaloneId}", saloneId);
				return Json(new { success = false, message = "Errore nel caricamento degli orari" });
			}
		}

		/// <summary>
		/// Blocca temporaneamente uno slot (10 minuti)
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> ReserveSlot([FromBody] ReserveSlotRequest request)
		{
			try
			{
				// Imposta session e user ID
				request.SessionId = GetOrCreateSessionId();
				request.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

				var result = await _slotReservationService.ReserveSlotAsync(request);
				return Json(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore durante la reservation dello slot");
				return Json(SlotReservationResult.Fail(SlotReservationErrorCode.DatabaseError, "Errore durante la prenotazione"));
			}
		}

		/// <summary>
		/// Rilascia una reservation attiva
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> ReleaseReservation([FromBody] ReleaseReservationRequest request)
		{
			try
			{
				var sessionId = GetOrCreateSessionId();
				var success = await _slotReservationService.ReleaseReservationAsync(request.ReservationId, sessionId);
				return Json(new { success, message = success ? "Slot rilasciato" : "Impossibile rilasciare lo slot" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore durante il rilascio della reservation");
				return Json(new { success = false, message = "Errore durante il rilascio" });
			}
		}

		/// <summary>
		/// Ottiene lo stato di una reservation
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> GetReservationStatus(Guid reservationId)
		{
			try
			{
				var sessionId = GetOrCreateSessionId();
				var status = await _slotReservationService.GetReservationStatusAsync(reservationId, sessionId);
				return Json(status);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore durante il controllo stato reservation");
				return Json(new ReservationStatusResponse { IsActive = false, Message = "Errore" });
			}
		}

		/// <summary>
		/// Conferma una reservation e crea l'appuntamento
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> ConfirmReservation([FromBody] ConfirmReservationRequest request)
		{
			try
			{
				request.SessionId = GetOrCreateSessionId();
				request.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

				var result = await _slotReservationService.ConfirmReservationAsync(request);
				return Json(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore durante la conferma della reservation");
				return Json(new BookingResponseDto { Success = false, Message = "Errore durante la conferma" });
			}
		}

		/// <summary>
		/// Ottiene o crea un ID sessione per tracciare le reservation
		/// </summary>
		private string GetOrCreateSessionId()
		{
			const string SessionKey = "SlotReservationSessionId";
			var sessionId = HttpContext.Session.GetString(SessionKey);

			if (string.IsNullOrEmpty(sessionId))
			{
				sessionId = Guid.NewGuid().ToString();
				HttpContext.Session.SetString(SessionKey, sessionId);
			}

			return sessionId;
		}

		#endregion

		// METODO AGGIORNATO: CreateBooking con gestione degli slot dinamici
		[HttpPost]
		public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto request)
		{
			if (!ModelState.IsValid)
			{
				return Json(new { success = false, message = "Dati non validi" });
			}

			try
			{
				// Validazione business logic
				var validationResult = await ValidateBookingRequest(request);
				if (!validationResult.IsValid)
				{
					return Json(new { success = false, message = validationResult.ErrorMessage });
				}

				// Gestione utente
				var cliente = await GetOrCreateCustomer(request);
				if (cliente == null)
				{
					return Json(new { success = false, message = "Errore nella gestione dei dati utente" });
				}

				// Ottieni informazioni del servizio per calcolare orari
				var servizio = await _context.Servizio
					.FirstOrDefaultAsync(s => s.ServizioId == request.ServizioId);

				if (servizio == null)
				{
					return Json(new { success = false, message = "Servizio non trovato" });
				}

				// Parse degli orari dalla stringa (formato "HH:mm - HH:mm")
				var timeSlots = request.TimeSlot.Split(" - ");
				if (timeSlots.Length != 2 ||
					!TimeSpan.TryParse(timeSlots[0], out var oraInizio) ||
					!TimeSpan.TryParse(timeSlots[1], out var oraFine))
				{
					return Json(new { success = false, message = "Formato orario non valido" });
				}

				// NUOVA VALIDAZIONE: Verifica disponibilità con il nuovo sistema
				var isSlotAvailable = await _slotService.ValidateSlotBookingAsync(
					request.SaloneId,
					request.Data,
					oraInizio,
					oraFine,
					request.DipendenteId);

				if (!isSlotAvailable)
				{
					return Json(new { success = false, message = "Lo slot selezionato non è più disponibile" });
				}

				// Trova o crea un slot corrispondente nel database
				var slot = await FindOrCreateSlot(request.SaloneId, oraInizio, oraFine);

				// Creazione appuntamento
				var appuntamento = new Appuntamento
				{
					AppuntamentoId = Guid.NewGuid(),
					SaloneId = request.SaloneId,
					Salone = _context.Salone.Find(request.SaloneId),
					ApplicationUserId = cliente.Id,
					ApplicationUser = cliente,
					DipendenteId = request.DipendenteId,
					Dipendente = _context.Dipendente.Find(request.DipendenteId),
					ServizioId = request.ServizioId,
					Servizio = servizio,
					SlotId = slot.SlotId,
					Slot = slot,
					Data = request.Data,
					Note = request.Note ?? "",
					Stato = HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato
				};

				_context.Appuntamento.Add(appuntamento);
				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					message = "Prenotazione confermata con successo!",
					appuntamentoId = appuntamento.AppuntamentoId,
					redirectUrl = Url.Action("BookingConfirmation", new { id = appuntamento.AppuntamentoId })
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore durante la creazione della prenotazione");
				return Json(new { success = false, message = "Errore interno del server" });
			}
		}

		[HttpGet]
		public async Task<IActionResult> BookingConfirmation(Guid id)
		{
			try
			{
				var appuntamento = await _context.Appuntamento
					.Include(a => a.Salone)
					.Include(a => a.Servizio)
					.Include(a => a.Dipendente)
						.ThenInclude(d => d.ApplicationUser)
					.Include(a => a.ApplicationUser)
					.Include(a => a.Slot)
					.FirstOrDefaultAsync(a => a.AppuntamentoId == id);

				if (appuntamento == null)
				{
					return NotFound("Prenotazione non trovata");
				}

				var viewModel = new BookingConfirmationViewModel
				{
					AppuntamentoId = appuntamento.AppuntamentoId,
					NomeSalone = appuntamento.Salone.Nome,
					IndirizzoSalone = $"{appuntamento.Salone.Indirizzo}, {appuntamento.Salone.Citta}",
					TelefonoSalone = appuntamento.Salone.Telefono,
					NomeServizio = appuntamento.Servizio?.Nome ?? "Servizio non specificato",
					DataAppuntamento = appuntamento.Data,
					OrarioAppuntamento = $"{appuntamento.OraInizio:HH\\:mm} - {appuntamento.OraFine:HH\\:mm}",
					NomeDipendente = appuntamento.Dipendente != null ?
						$"{appuntamento.Dipendente.ApplicationUser.Nome} {appuntamento.Dipendente.ApplicationUser.Cognome}" : null,
					PrezzoTotale = appuntamento.Servizio?.PrezzoEffettivo ?? 0,
					Note = appuntamento.Note,
					IsGuestBooking = !User.Identity.IsAuthenticated,
					EmailConferma = appuntamento.ApplicationUser.Email
				};

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore nel caricamento conferma prenotazione {AppuntamentoId}", id);
				return View("Error");
			}
		}

		#region Helper Methods

		private async Task<Slot> FindOrCreateSlot(Guid saloneId, TimeSpan oraInizio, TimeSpan oraFine)
		{
			// Cerca uno slot esistente che corrisponda esattamente
			var existingSlot = await _context.Slot
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId &&
										s.OraInizio == oraInizio &&
										s.OraFine == oraFine);

			if (existingSlot != null)
			{
				return existingSlot;
			}

			// Crea un nuovo slot se non esiste
			var newSlot = new Slot
			{
				SlotId = Guid.NewGuid(),
				SaloneId = saloneId,
				Salone = _context.Salone.Find(saloneId),
				OraInizio = oraInizio,
				OraFine = oraFine,
				GiorniSettimana = "0,1,2,3,4,5,6", // Tutti i giorni per default
				Capacita = 1, // Default capacity
				IsAttivo = true,
				Note = "Auto-generated slot"
			};

			_context.Slot.Add(newSlot);
			await _context.SaveChangesAsync();

			_logger.LogInformation($"Created new slot {oraInizio}-{oraFine} for salone {saloneId}");
			return newSlot;
		}

		private IQueryable<Salone> ApplyFilters(IQueryable<Salone> query, SearchFilterDto filters)
		{
			if (!string.IsNullOrWhiteSpace(filters.Query))
			{
				query = query.Where(s =>
					s.Nome.Contains(filters.Query) ||
					s.Servizi.Any(serv => serv.Nome.Contains(filters.Query) || serv.Descrizione.Contains(filters.Query)));
			}

			if (!string.IsNullOrWhiteSpace(filters.Location))
			{
				query = query.Where(s =>
					s.Citta.Contains(filters.Location) ||
					s.Provincia.Contains(filters.Location) ||
					s.CAP.Contains(filters.Location) ||
					s.Regione.Contains(filters.Location));
			}

			if (!string.IsNullOrWhiteSpace(filters.Categoria))
			{
				query = query.Where(s => s.SaloneCategorie.Any(sc => sc.Categoria.Nome.Contains(filters.Categoria)));
			}

			if (filters.Rating.HasValue)
			{
				query = query.Where(s => s.Recensioni.Any() && s.Recensioni.Average(r => r.Voto) >= filters.Rating.Value);
			}

			if (filters.MaxPrice.HasValue)
			{
				query = query.Where(s => s.Servizi.Any(serv => serv.PrezzoEffettivo <= filters.MaxPrice.Value));
			}

			return query;
		}

		private SaloneSearchResult MapToSearchResult(Salone salone)
		{
			return new SaloneSearchResult
			{
				SaloneId = salone.SaloneId,
				Nome = salone.Nome,
				Citta = salone.Citta,
				Provincia = salone.Provincia,
				Indirizzo = salone.Indirizzo,
				Telefono = salone.Telefono,
				CoverImageUrl = salone.Immagini?.FirstOrDefault(i => i.IsCover)?.Percorso,
				LogoUrl = salone.SalonePersonalizzazione?.LogoUrl,
				Slogan = salone.SalonePersonalizzazione?.Slogan,
				MediaVoti = salone.Recensioni.Any() ? salone.Recensioni.Average(r => r.Voto) : 0,
				NumeroRecensioni = salone.Recensioni.Count,
				NumeroServizi = salone.Servizi.Count,
				PrezzoMinimo = salone.Servizi.Any() ? salone.Servizi.Min(serv => serv.PrezzoEffettivo) : 0,
				PrezzoMassimo = salone.Servizi.Any() ? salone.Servizi.Max(serv => serv.PrezzoEffettivo) : 0,
				ServiziPopolari = salone.Servizi.OrderBy(serv => serv.Nome).Take(3).Select(serv => serv.Nome).ToList(),
				HasPromozioni = salone.Servizi.Any(serv => serv.IsPromotion && serv.DataFinePromozione > DateTime.Now),
				PersonalizzazioneColori = new PersonalizzazioneColori
				{
					ColorePrimario = salone.SalonePersonalizzazione?.ColorePrimario ?? "#7c3aed",
					ColoreSecondario = salone.SalonePersonalizzazione?.ColoreSecondario ?? "#ec4899",
					ColoreAccento = salone.SalonePersonalizzazione?.ColoreAccento ?? "#f59e0b"
				},
				IsPremium = salone.SaloneAbbonamenti.Any(sa =>
					sa.Abbonamento.Nome.Contains("Pro") || sa.Abbonamento.Nome.Contains("Business")),
				HasStaffSelection = salone.SaloneAbbonamenti.Any(sa =>
					sa.Abbonamento.Nome.Contains("Pro") || sa.Abbonamento.Nome.Contains("Business")),
				NumeroDipendenti = salone.Dipendenti.Count,
				VotiDisplay = salone.Recensioni.Any() ? salone.Recensioni.Average(r => r.Voto).ToString("F1") : "Nuovo",
				PrezzoRange = salone.Servizi.Any() ?
					(salone.Servizi.Min(serv => serv.PrezzoEffettivo) == salone.Servizi.Max(serv => serv.PrezzoEffettivo) ?
						$"€{salone.Servizi.Min(serv => serv.PrezzoEffettivo):F0}" :
						$"€{salone.Servizi.Min(serv => serv.PrezzoEffettivo):F0}-{salone.Servizi.Max(serv => serv.PrezzoEffettivo):F0}") :
					"Su richiesta",
				DataCreazione = salone.DataCreazione,
				UltimaAttivita = salone.Recensioni.Any() ?
					salone.Recensioni.Max(r => r.DataCreazione) : salone.DataCreazione
			};
		}

		private SaloneDetailsViewModel BuildDetailsViewModel(Salone salone)
		{
			return new SaloneDetailsViewModel
			{
				Salone = salone,
				MediaVoti = salone.Recensioni.Any() ? salone.Recensioni.Average(r => r.Voto) : 0,
				CoverImage = salone.Immagini?.FirstOrDefault(i => i.IsCover),
				GalleryImages = salone.Immagini?.Where(i => !i.IsCover && !i.IsLogo).ToList() ?? new List<Immagine>(),
				ServiziPerCategoria = salone.Servizi
					.GroupBy(s => string.IsNullOrEmpty(s.Descrizione) ? "Altri Servizi" :
								 s.Descrizione.Length > 50 ? "Servizi Speciali" : "Servizi Base")
					.ToDictionary(g => g.Key, g => g.OrderBy(s => s.Nome).ToList()),
				DipendenteDisponibile = salone.Dipendenti.Where(d => d.ServiziOfferti.Any()).ToList(),
				RecensioniRecenti = salone.Recensioni.OrderByDescending(r => r.DataCreazione).Take(6).ToList(),
				IsPremium = salone.SaloneAbbonamenti.Any(sa =>
					sa.Abbonamento.Nome.Contains("Pro") || sa.Abbonamento.Nome.Contains("Business")),
				MostraSceltaDipendente = salone.SaloneAbbonamenti.Any(sa =>
					sa.Abbonamento.Nome.Contains("Pro") || sa.Abbonamento.Nome.Contains("Business"))
			};
		}

		private async Task<ValidationResult> ValidateBookingRequest(CreateBookingDto request)
		{
			// Verifica salone
			var salone = await _context.Salone.FindAsync(request.SaloneId);
			if (salone == null)
				return ValidationResult.Fail("Salone non trovato");

			// Verifica servizio
			var servizio = await _context.Servizio
				.FirstOrDefaultAsync(s => s.ServizioId == request.ServizioId && s.SaloneId == request.SaloneId);
			if (servizio == null)
				return ValidationResult.Fail("Servizio non disponibile per questo salone");

			// Verifica dipendente se specificato
			if (request.DipendenteId.HasValue)
			{
				var dipendente = await _context.Dipendente
					.Include(d => d.ServiziOfferti)
					.FirstOrDefaultAsync(d => d.DipendenteId == request.DipendenteId.Value && d.SaloneId == request.SaloneId);

				if (dipendente == null)
					return ValidationResult.Fail("Dipendente non trovato");

				// Verifica che il dipendente offra il servizio richiesto
				if (!dipendente.ServiziOfferti.Any(so => so.ServizioId == request.ServizioId))
					return ValidationResult.Fail("Il dipendente selezionato non offre questo servizio");
			}

			return ValidationResult.Success();
		}

		private async Task<ApplicationUser?> GetOrCreateCustomer(CreateBookingDto request)
		{
			if (User.Identity?.IsAuthenticated == true)
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				return await _context.Users.FindAsync(userId);
			}

			// Validazione dati guest
			if (string.IsNullOrWhiteSpace(request.Nome) ||
				string.IsNullOrWhiteSpace(request.Cognome) ||
				string.IsNullOrWhiteSpace(request.Email) ||
				string.IsNullOrWhiteSpace(request.Telefono))
			{
				return null;
			}

			// Cerca utente esistente
			var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
			if (existingUser != null)
				return existingUser;

			// Crea nuovo utente guest
			var newUser = new ApplicationUser
			{
				Id = Guid.NewGuid().ToString(),
				UserName = request.Email,
				Email = request.Email,
				Nome = request.Nome,
				Cognome = request.Cognome,
				PhoneNumber = request.Telefono,
				EmailConfirmed = false
			};

			_context.Users.Add(newUser);
			await _context.SaveChangesAsync();
			return newUser;
		}

		private async Task<List<object>> GetSearchSuggestions(string query, int limit)
		{
			var saloniSuggestions = await _context.Salone
				.Where(s => s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo &&
						   (s.Nome.Contains(query) || s.Citta.Contains(query)))
				.Take(limit)
				.Select(s => new {
					tipo = "salone",
					nome = s.Nome,
					location = s.Citta + ", " + s.Provincia,
					id = s.SaloneId
				})
				.ToListAsync();

			var serviziSuggestions = await _context.Servizio
				.Include(s => s.Salone)
				.Where(s => s.Salone.Stato == HaveASeat.Utilities.Enum.Stato.Attivo &&
						   (s.Nome.Contains(query) || s.Descrizione.Contains(query)))
				.Take(limit)
				.Select(s => new {
					tipo = "servizio",
					nome = s.Nome,
					location = s.Salone.Nome + " - " + s.Salone.Citta,
					id = s.SaloneId
				})
				.ToListAsync();

			return saloniSuggestions.Cast<object>()
				.Concat(serviziSuggestions.Cast<object>())
				.Take(limit)
				.ToList();
		}

		private UserBookingViewModel MapToUserBookingViewModel(Appuntamento appuntamento)
		{
			return new UserBookingViewModel
			{
				AppuntamentoId = appuntamento.AppuntamentoId,
				SaloneId = appuntamento.SaloneId,
				ServizioId = appuntamento.ServizioId ?? Guid.Empty,
				NomeSalone = appuntamento.Salone.Nome,
				IndirizzoSalone = $"{appuntamento.Salone.Indirizzo}, {appuntamento.Salone.Citta}",
				TelefonoSalone = appuntamento.Salone.Telefono,
				NomeServizio = appuntamento.Servizio?.Nome ?? "Servizio non specificato",
				DataAppuntamento = appuntamento.Data,
				OrarioAppuntamento = $"{appuntamento.OraInizio:HH\\:mm} - {appuntamento.OraFine:HH\\:mm}",
				NomeDipendente = appuntamento.Dipendente != null ?
					$"{appuntamento.Dipendente.ApplicationUser.Nome} {appuntamento.Dipendente.ApplicationUser.Cognome}" : null,
				Prezzo = appuntamento.Servizio?.PrezzoEffettivo ?? 0,
				Stato = appuntamento.Stato.ToString(),
				Note = appuntamento.Note,
				CanCancel = appuntamento.Stato == HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato &&
						   appuntamento.Data > DateTime.Now.AddHours(24)
			};
		}

		#endregion

		#region Booking Management Endpoints

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> GetBookingDetails(Guid id)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				var details = await _bookingService.GetBookingDetailsAsync(id, userId);

				if (details == null)
				{
					return NotFound(new { success = false, message = "Prenotazione non trovata" });
				}

				return Json(details);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore recupero dettagli prenotazione {Id}", id);
				return StatusCode(500, new { success = false, message = "Errore durante il recupero dei dettagli" });
			}
		}

		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CancelBooking(Guid id, string? motivo = null)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

				var canCancel = await _bookingService.CanCancelBookingAsync(id, userId);
				if (!canCancel)
				{
					return Json(new { success = false, message = "Non è possibile cancellare questa prenotazione. Verifica che sia stata prenotata da te e che manchino almeno 24 ore." });
				}

				var result = await _bookingService.CancelBookingAsync(id, userId, motivo);

				if (result)
				{
					return Json(new { success = true, message = "Prenotazione cancellata con successo" });
				}

				return Json(new { success = false, message = "Errore durante la cancellazione della prenotazione" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore cancellazione prenotazione {Id}", id);
				return StatusCode(500, new { success = false, message = "Errore durante la cancellazione" });
			}
		}

		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateBooking([FromBody] UpdateBookingDto request)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (string.IsNullOrEmpty(userId))
				{
					return Unauthorized(new { success = false, message = "Utente non autenticato" });
				}

				var result = await _bookingService.UpdateBookingAsync(request, userId);
				return Json(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore modifica prenotazione {Id}", request.AppuntamentoId);
				return StatusCode(500, new BookingResponseDto { Success = false, Message = "Errore durante la modifica" });
			}
		}

		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddReview([FromBody] AddReviewDto request)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (string.IsNullOrEmpty(userId))
				{
					return Unauthorized(new { success = false, message = "Utente non autenticato" });
				}

				var result = await _bookingService.AddReviewAsync(request, userId);

				if (result)
				{
					return Json(new { success = true, message = "Recensione aggiunta con successo!" });
				}

				return Json(new { success = false, message = "Impossibile aggiungere la recensione. Verifica che l'appuntamento sia completato." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore aggiunta recensione per appuntamento {Id}", request.AppuntamentoId);
				return StatusCode(500, new { success = false, message = "Errore durante l'invio della recensione" });
			}
		}

		#endregion

		// Metodo legacy UpdateBooking - mantenuto per compatibilità interna
		private async Task<BookingResponseDto> UpdateBookingInternalAsync(UpdateBookingDto request, string userId)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();

			try
			{
				var appuntamento = await _context.Appuntamento
					.Include(a => a.Salone)
					.Include(a => a.Slot)
					.Include(a => a.Servizio)
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

				// Gestione cambio data
				if (request.NuovaData.HasValue && request.NuovaData.Value.Date != appuntamento.Data.Date)
				{
					appuntamento.Data = request.NuovaData.Value;
					hasChanges = true;
				}

				// Gestione cambio orario (CORRETTO)
				if (!string.IsNullOrEmpty(request.NuovoTimeSlot) && request.NuovoTimeSlot != $"{appuntamento.Slot.OraInizio:HH\\:mm} - {appuntamento.Slot.OraFine:HH\\:mm}")
				{
					// Parse del nuovo slot temporale
					var timeSlots = request.NuovoTimeSlot.Split(" - ");
					if (timeSlots.Length == 2 &&
						TimeSpan.TryParse(timeSlots[0], out var nuovaOraInizio) &&
						TimeSpan.TryParse(timeSlots[1], out var nuovaOraFine))
					{
						// Verifica disponibilità del nuovo slot
						var dataVerifica = request.NuovaData ?? appuntamento.Data;
						var isSlotAvailable = await _slotService.ValidateSlotBookingAsync(
							appuntamento.SaloneId,
							dataVerifica,
							nuovaOraInizio,
							nuovaOraFine,
							request.NuovoDipendenteId ?? appuntamento.DipendenteId);

						if (!isSlotAvailable)
						{
							return new BookingResponseDto
							{
								Success = false,
								Message = "Il nuovo orario selezionato non è disponibile"
							};
						}

						// Trova o crea il nuovo slot
						var nuovoSlot = await FindOrCreateSlot(appuntamento.SaloneId, nuovaOraInizio, nuovaOraFine);
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

				// Gestione cambio dipendente
				if (request.NuovoDipendenteId.HasValue && request.NuovoDipendenteId.Value != appuntamento.DipendenteId)
				{
					// Verifica che il nuovo dipendente esista e offra il servizio
					var dipendente = await _context.Dipendente
						.Include(d => d.ServiziOfferti)
						.FirstOrDefaultAsync(d => d.DipendenteId == request.NuovoDipendenteId.Value &&
												d.SaloneId == appuntamento.SaloneId);

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

				// Gestione cambio servizio
				if (request.NuovoServizioId.HasValue && request.NuovoServizioId.Value != appuntamento.ServizioId)
				{
					var nuovoServizio = await _context.Servizio
						.FirstOrDefaultAsync(s => s.ServizioId == request.NuovoServizioId.Value &&
												s.SaloneId == appuntamento.SaloneId);

					if (nuovoServizio == null)
					{
						return new BookingResponseDto
						{
							Success = false,
							Message = "Servizio non trovato"
						};
					}

					// Verifica che il dipendente (se presente) offra il nuovo servizio
					if (appuntamento.DipendenteId.HasValue)
					{
						var dipendenteOffre = await _context.DipendenteServizio
							.AnyAsync(ds => ds.DipendenteId == appuntamento.DipendenteId.Value &&
										   ds.ServizioId == request.NuovoServizioId.Value);

						if (!dipendenteOffre)
						{
							return new BookingResponseDto
							{
								Success = false,
								Message = "Il dipendente corrente non offre il nuovo servizio selezionato"
							};
						}
					}

					appuntamento.ServizioId = request.NuovoServizioId.Value;
					hasChanges = true;

					// Se cambia servizio, potrebbe essere necessario aggiornare anche lo slot
					// per riflettere la nuova durata
					if (!string.IsNullOrEmpty(request.NuovoTimeSlot))
					{
						// Il nuovo slot è già stato gestito sopra
					}
					else if (nuovoServizio.Durata != appuntamento.Servizio?.Durata)
					{
						// Se la durata è cambiata ma non è stato specificato un nuovo orario,
						// aggiorna automaticamente la fine dello slot
						var nuovaDurata = TimeSpan.FromMinutes((double)nuovoServizio.Durata);
						var nuovaOraFine = appuntamento.Slot.OraInizio.Add(nuovaDurata);

						// Verifica che il nuovo orario sia disponibile
						var isSlotAvailable = await _slotService.ValidateSlotBookingAsync(
							appuntamento.SaloneId,
							appuntamento.Data,
							appuntamento.Slot.OraInizio,
							nuovaOraFine,
							appuntamento.DipendenteId);

						if (!isSlotAvailable)
						{
							return new BookingResponseDto
							{
								Success = false,
								Message = "Il nuovo servizio richiede più tempo di quello disponibile nell'orario corrente"
							};
						}

						// Crea nuovo slot con la durata corretta
						var nuovoSlot = await FindOrCreateSlot(appuntamento.SaloneId, appuntamento.Slot.OraInizio, nuovaOraFine);
						appuntamento.SlotId = nuovoSlot.SlotId;
						hasChanges = true;
					}
				}

				// Gestione note
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
								var notifica = new BookingNotificationDto
								{
									AppuntamentoId = appuntamento.AppuntamentoId,
									TipoNotifica = "modifica",
									EmailDestinatario = appuntamento.ApplicationUser.Email,
									Messaggio = "La tua prenotazione è stata modificata con successo"
								};

								await _bookingService.SendBookingNotificationAsync(notifica);
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
	}

	// Helper classes
	public class ValidationResult
	{
		public bool IsValid { get; set; }
		public string ErrorMessage { get; set; } = string.Empty;

		public static ValidationResult Success() => new() { IsValid = true };
		public static ValidationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
	}
}