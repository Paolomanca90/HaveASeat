using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.ViewModels;
using HaveASeat.Utilities.Dto;
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

		public SearchController(ApplicationDbContext context, ILogger<SearchController> logger)
		{
			_context = context;
			_logger = logger;
		}

		[HttpGet]
		public async Task<IActionResult> Index(SearchFilterDto filters, int page = 1)
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
					.Where(s => s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo);

				// Applica filtri
				query = ApplyFilters(query, filters);

				var totalItems = await query.CountAsync();
				var saloni = await query
					.OrderByDescending(s => s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto) : 0)
					.ThenByDescending(s => s.DataCreazione)
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
					TotalResults = totalItems
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

				// Creazione appuntamento
				var appuntamento = await CreateAppointment(request, cliente.Id);

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
		public async Task<IActionResult> GetAvailableSlots(Guid saloneId, DateTime data, Guid? dipendenteId = null, Guid? servizioId = null)
		{
			try
			{
				var slots = await GetAvailableSlotsForDate(saloneId, data, dipendenteId, servizioId);
				return Json(new { success = true, slots });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore nel caricamento slot per salone {SaloneId}", saloneId);
				return Json(new { success = false, message = "Errore nel caricamento degli orari" });
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

		// Metodi privati helper
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
				}
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

			// Verifica slot
			var slot = await _context.Slot.FindAsync(request.SlotId);
			if (slot == null || slot.SaloneId != request.SaloneId)
				return ValidationResult.Fail("Slot non valido");

			// Verifica disponibilità
			var slotOccupato = await _context.Appuntamento
				.AnyAsync(a => a.Data.Date == request.Data.Date &&
							  a.SlotId == request.SlotId &&
							  a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato &&
							  (!request.DipendenteId.HasValue || a.DipendenteId == request.DipendenteId));

			if (slotOccupato)
				return ValidationResult.Fail("Lo slot selezionato non è più disponibile");

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

		private async Task<Appuntamento> CreateAppointment(CreateBookingDto request, string userId)
		{
			var appuntamento = new Appuntamento
			{
				AppuntamentoId = Guid.NewGuid(),
				SaloneId = request.SaloneId,
				ApplicationUserId = userId,
				SlotId = request.SlotId,
				DipendenteId = request.DipendenteId,
				ServizioId = request.ServizioId,
				Data = request.Data,
				Note = request.Note ?? "",
				Stato = HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato
			};

			_context.Appuntamento.Add(appuntamento);
			await _context.SaveChangesAsync();
			return appuntamento;
		}

		private async Task<List<object>> GetAvailableSlotsForDate(Guid saloneId, DateTime data, Guid? dipendenteId, Guid? servizioId)
		{
			var salone = await _context.Salone
				.Include(s => s.Slots)
				.Include(s => s.Orari)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId);

			if (salone == null) return new List<object>();

			var dayOfWeek = data.DayOfWeek;
			var orarioSalone = salone.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);

			if (orarioSalone == null || orarioSalone.IsDayOff)
				return new List<object>();

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
				.Select(slot => new {
					slotId = slot.SlotId,
					oraInizio = slot.OraInizio.ToString(@"HH\:mm"),
					oraFine = slot.OraFine.ToString(@"HH\:mm"),
					display = $"{slot.OraInizio:HH\\:mm} - {slot.OraFine:HH\\:mm}",
					disponibile = true,
					postiLiberi = Math.Max(0, slot.Capacita - appuntamentiEsistenti.Count(a => a.SlotId == slot.SlotId)),
					capacita = slot.Capacita
				})
				.Cast<object>()
				.ToList();
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
				OrarioAppuntamento = $"{appuntamento.Slot.OraInizio:HH\\:mm} - {appuntamento.Slot.OraFine:HH\\:mm}",
				NomeDipendente = appuntamento.Dipendente != null ?
					$"{appuntamento.Dipendente.ApplicationUser.Nome} {appuntamento.Dipendente.ApplicationUser.Cognome}" : null,
				Prezzo = appuntamento.Servizio?.PrezzoEffettivo ?? 0,
				Stato = appuntamento.Stato.ToString(),
				Note = appuntamento.Note,
				CanCancel = appuntamento.Stato == HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato &&
						   appuntamento.Data > DateTime.Now.AddHours(24)
			};
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