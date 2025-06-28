using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HaveASeat.Controllers
{
	public class SearchController : Controller
	{
		private readonly ApplicationDbContext _context;

		public SearchController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> Index(string q = "", string location = "", string categoria = "", DateTime? data = null, int page = 1)
		{
			const int pageSize = 12;

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

			// Filtro per termine di ricerca
			if (!string.IsNullOrWhiteSpace(q))
			{
				query = query.Where(s =>
					s.Nome.Contains(q) ||
					s.Servizi.Any(serv => serv.Nome.Contains(q) || serv.Descrizione.Contains(q)));
			}

			// Filtro per località
			if (!string.IsNullOrWhiteSpace(location))
			{
				query = query.Where(s =>
					s.Citta.Contains(location) ||
					s.Provincia.Contains(location) ||
					s.CAP.Contains(location) ||
					s.Regione.Contains(location));
			}

			// Filtro per categoria
			if (!string.IsNullOrWhiteSpace(categoria))
			{
				query = query.Where(s => s.SaloneCategorie.Any(sc => sc.Categoria.Nome.Contains(categoria)));
			}

			var totalItems = await query.CountAsync();
			var saloni = await query
				.OrderByDescending(s => s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto) : 0)
				.ThenByDescending(s => s.DataCreazione)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// Carica categorie per filtri
			var categorie = await _context.Categoria.OrderBy(c => c.Nome).ToListAsync();

			var viewModel = new SearchResultsViewModel
			{
				Saloni = saloni.Select(s => new SaloneSearchResult
				{
					SaloneId = s.SaloneId,
					Nome = s.Nome,
					Citta = s.Citta,
					Provincia = s.Provincia,
					Indirizzo = s.Indirizzo,
					Telefono = s.Telefono,
					CoverImageUrl = s.Immagini?.FirstOrDefault(i => i.IsCover)?.Percorso,
					LogoUrl = s.SalonePersonalizzazione?.LogoUrl,
					Slogan = s.SalonePersonalizzazione?.Slogan,
					MediaVoti = s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto) : 0,
					NumeroRecensioni = s.Recensioni.Count,
					NumeroServizi = s.Servizi.Count,
					PrezzoMinimo = s.Servizi.Any() ? s.Servizi.Min(serv => serv.PrezzoEffettivo) : 0,
					PrezzoMassimo = s.Servizi.Any() ? s.Servizi.Max(serv => serv.PrezzoEffettivo) : 0,
					ServiziPopolari = s.Servizi.OrderBy(serv => serv.Nome).Take(3).Select(serv => serv.Nome).ToList(),
					HasPromozioni = s.Servizi.Any(serv => serv.IsPromotion && serv.DataFinePromozione > DateTime.Now),
					PersonalizzazioneColori = new PersonalizzazioneColori
					{
						ColorePrimario = s.SalonePersonalizzazione?.ColorePrimario ?? "#7c3aed",
						ColoreSecondario = s.SalonePersonalizzazione?.ColoreSecondario ?? "#ec4899",
						ColoreAccento = s.SalonePersonalizzazione?.ColoreAccento ?? "#f59e0b"
					}
				}).ToList(),
				SearchQuery = q,
				Location = location,
				CategoriaSelezionata = categoria,
				DataSelezionata = data,
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

		[HttpGet]
		public async Task<IActionResult> Details(Guid id)
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
				return NotFound();
			}

			var viewModel = new SaloneDetailsViewModel
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

			return View(viewModel);
		}

		[HttpPost]
		public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
		{
			try
			{
				// Validazione input
				if (request == null || request.SaloneId == Guid.Empty || request.ServizioId == Guid.Empty)
				{
					return Json(new { success = false, message = "Dati di prenotazione mancanti" });
				}

				var salone = await _context.Salone.FindAsync(request.SaloneId);
				if (salone == null)
				{
					return Json(new { success = false, message = "Salone non trovato" });
				}

				var slot = await _context.Slot.FindAsync(request.SlotId);
				if (slot == null)
				{
					return Json(new { success = false, message = "Slot non valido" });
				}

				var servizio = await _context.Servizio.FindAsync(request.ServizioId);
				if (servizio == null)
				{
					return Json(new { success = false, message = "Servizio non trovato" });
				}

				// Verifica che il servizio appartenga al salone
				if (servizio.SaloneId != request.SaloneId)
				{
					return Json(new { success = false, message = "Servizio non disponibile per questo salone" });
				}

				// Verifica che il dipendente (se specificato) appartenga al salone
				if (request.DipendenteId.HasValue)
				{
					var dipendente = await _context.Dipendente
						.FirstOrDefaultAsync(d => d.DipendenteId == request.DipendenteId.Value && d.SaloneId == request.SaloneId);

					if (dipendente == null)
					{
						return Json(new { success = false, message = "Dipendente non valido per questo salone" });
					}
				}

				// Verifica disponibilità dello slot
				var slotOccupato = await _context.Appuntamento
					.AnyAsync(a => a.Data.Date == request.Data.Date &&
								  a.SlotId == request.SlotId &&
								  a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato &&
								  (!request.DipendenteId.HasValue || a.DipendenteId == request.DipendenteId));

				if (slotOccupato)
				{
					return Json(new { success = false, message = "Lo slot selezionato non è più disponibile" });
				}

				// Gestisci utente (autenticato o guest)
				ApplicationUser cliente;
				if (User.Identity?.IsAuthenticated == true)
				{
					var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
					cliente = await _context.Users.FindAsync(userId);

					if (cliente == null)
					{
						return Json(new { success = false, message = "Utente non trovato" });
					}
				}
				else
				{
					// Validazione dati guest
					if (string.IsNullOrWhiteSpace(request.Nome) ||
						string.IsNullOrWhiteSpace(request.Cognome) ||
						string.IsNullOrWhiteSpace(request.Email) ||
						string.IsNullOrWhiteSpace(request.Telefono))
					{
						return Json(new { success = false, message = "Tutti i campi sono obbligatori per la prenotazione" });
					}

					// Cerca se esiste già un utente con questa email
					cliente = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

					if (cliente == null)
					{
						// Crea un nuovo utente guest
						cliente = new ApplicationUser
						{
							Id = Guid.NewGuid().ToString(),
							UserName = request.Email,
							Email = request.Email,
							Nome = request.Nome,
							Cognome = request.Cognome,
							PhoneNumber = request.Telefono,
							EmailConfirmed = false // Utente guest non confermato
						};

						_context.Users.Add(cliente);
						await _context.SaveChangesAsync();
					}
				}

				// Crea l'appuntamento
				var appuntamento = new Appuntamento
				{
					AppuntamentoId = Guid.NewGuid(),
					SaloneId = request.SaloneId,
					ApplicationUserId = cliente.Id,
					SlotId = request.SlotId,
					DipendenteId = request.DipendenteId,
					ServizioId = request.ServizioId,
					Data = request.Data,
					Note = request.Note ?? "",
					Stato = HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato
				};

				_context.Appuntamento.Add(appuntamento);
				await _context.SaveChangesAsync();

				// Prepara dati per la conferma
				var bookingConfirmation = new BookingConfirmationViewModel
				{
					AppuntamentoId = appuntamento.AppuntamentoId,
					NomeSalone = salone.Nome,
					IndirizzoSalone = $"{salone.Indirizzo}, {salone.Citta}",
					TelefonoSalone = salone.Telefono,
					NomeServizio = servizio.Nome,
					DataAppuntamento = request.Data,
					OrarioAppuntamento = $"{slot.OraInizio:HH\\:mm} - {slot.OraFine:HH\\:mm}",
					NomeDipendente = request.DipendenteId.HasValue ?
						(await _context.Dipendente
							.Include(d => d.ApplicationUser)
							.FirstOrDefaultAsync(d => d.DipendenteId == request.DipendenteId))
							?.ApplicationUser?.Nome + " " +
						(await _context.Dipendente
							.Include(d => d.ApplicationUser)
							.FirstOrDefaultAsync(d => d.DipendenteId == request.DipendenteId))
							?.ApplicationUser?.Cognome : null,
					PrezzoTotale = servizio.PrezzoEffettivo,
					Note = request.Note,
					IsGuestBooking = !User.Identity.IsAuthenticated,
					EmailConferma = cliente.Email
				};

				// TODO: Invia email di conferma
				// await _emailService.SendBookingConfirmationAsync(bookingConfirmation);

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
				// Log dell'errore
				Console.WriteLine($"Errore durante la prenotazione: {ex.Message}");
				return Json(new { success = false, message = "Si è verificato un errore durante la prenotazione. Riprova." });
			}
		}

		[HttpGet]
		public async Task<IActionResult> BookingConfirmation(Guid id)
		{
			var appuntamento = await _context.Appuntamento
				.Include(a => a.Salone)
				.Include(a => a.ApplicationUser)
				.Include(a => a.Servizio)
				.Include(a => a.Dipendente)
					.ThenInclude(d => d.ApplicationUser)
				.Include(a => a.Slot)
				.FirstOrDefaultAsync(a => a.AppuntamentoId == id);

			if (appuntamento == null)
			{
				return NotFound("Prenotazione non trovata");
			}

			// Verifica che l'utente possa vedere questa prenotazione
			var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			if (User.Identity.IsAuthenticated && appuntamento.ApplicationUserId != currentUserId)
			{
				return Forbid("Non autorizzato a visualizzare questa prenotazione");
			}

			var viewModel = new BookingConfirmationViewModel
			{
				AppuntamentoId = appuntamento.AppuntamentoId,
				NomeSalone = appuntamento.Salone.Nome,
				IndirizzoSalone = $"{appuntamento.Salone.Indirizzo}, {appuntamento.Salone.Citta}",
				TelefonoSalone = appuntamento.Salone.Telefono,
				NomeServizio = appuntamento.Servizio.Nome,
				DataAppuntamento = appuntamento.Data,
				OrarioAppuntamento = $"{appuntamento.Slot.OraInizio:HH\\:mm} - {appuntamento.Slot.OraFine:HH\\:mm}",
				NomeDipendente = appuntamento.Dipendente != null ?
					$"{appuntamento.Dipendente.ApplicationUser.Nome} {appuntamento.Dipendente.ApplicationUser.Cognome}" : null,
				PrezzoTotale = appuntamento.Servizio.PrezzoEffettivo,
				Note = appuntamento.Note,
				IsGuestBooking = !appuntamento.ApplicationUser.EmailConfirmed,
				EmailConferma = appuntamento.ApplicationUser.Email
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> GetAvailableSlots(Guid saloneId, DateTime data, Guid? dipendenteId = null, Guid? servizioId = null)
		{
			try
			{
				var salone = await _context.Salone
					.Include(s => s.Slots)
					.Include(s => s.Orari)
					.FirstOrDefaultAsync(s => s.SaloneId == saloneId);

				if (salone == null)
				{
					return Json(new { success = false, message = "Salone non trovato" });
				}

				// Ottieni il giorno della settimana (0 = Domenica, 1 = Lunedì, ecc.)
				var dayOfWeek = data.DayOfWeek;
				int dayNumber = (int)dayOfWeek;

				// Verifica se il salone è aperto in quel giorno
				var orarioSalone = salone.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);
				if (orarioSalone == null || orarioSalone.IsDayOff)
				{
					return Json(new { success = false, message = "Il salone è chiuso in questo giorno" });
				}

				// Se è specificato un dipendente, verifica i suoi orari
				if (dipendenteId.HasValue)
				{
					var dipendente = await _context.Dipendente
						.Include(d => d.Orari)
						.FirstOrDefaultAsync(d => d.DipendenteId == dipendenteId.Value);

					if (dipendente != null)
					{
						var orarioDipendente = dipendente.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);
						if (orarioDipendente != null && orarioDipendente.IsDayOff)
						{
							return Json(new { success = false, message = "Il dipendente selezionato non è disponibile in questo giorno" });
						}
					}
				}

				// Ottieni gli slots del salone per quel giorno
				var slotsDisponibili = salone.Slots
					.Where(s => s.IsAttivo && s.GiorniSettimana.Contains(dayNumber.ToString()))
					.Where(s => s.OraInizio >= orarioSalone.Apertura && s.OraFine <= orarioSalone.Chiusura)
					.OrderBy(s => s.OraInizio)
					.ToList();

				// Se c'è un dipendente selezionato, filtra anche per i suoi orari
				if (dipendenteId.HasValue)
				{
					var dipendente = await _context.Dipendente
						.Include(d => d.Orari)
						.FirstOrDefaultAsync(d => d.DipendenteId == dipendenteId.Value);

					if (dipendente?.Orari.Any() == true)
					{
						var orarioDipendente = dipendente.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);
						if (orarioDipendente != null && !orarioDipendente.IsDayOff)
						{
							slotsDisponibili = slotsDisponibili
								.Where(s => s.OraInizio >= orarioDipendente.Apertura && s.OraFine <= orarioDipendente.Chiusura)
								.ToList();
						}
					}
				}

				// Verifica disponibilità considerando gli appuntamenti esistenti
				var appuntamentiEsistenti = await _context.Appuntamento
					.Where(a => a.SaloneId == saloneId &&
							   a.Data.Date == data.Date &&
							   a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato)
					.Where(a => !dipendenteId.HasValue || a.DipendenteId == dipendenteId.Value)
					.ToListAsync();

				var slots = slotsDisponibili.Select(slot => new
				{
					slotId = slot.SlotId,
					oraInizio = slot.OraInizio.ToString(@"HH\:mm"),
					oraFine = slot.OraFine.ToString(@"HH\:mm"),
					display = $"{slot.OraInizio:HH\\:mm} - {slot.OraFine:HH\\:mm}",
					disponibile = !appuntamentiEsistenti.Any(a => a.SlotId == slot.SlotId),
					postiLiberi = Math.Max(0, slot.Capacita - appuntamentiEsistenti.Count(a => a.SlotId == slot.SlotId)),
					capacita = slot.Capacita
				}).Where(s => s.disponibile).ToList();

				return Json(new { success = true, slots });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Errore nel caricamento degli slot: {ex.Message}");
				return Json(new { success = false, message = "Errore nel caricamento degli orari disponibili" });
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
				var saloniSuggestions = await _context.Salone
					.Where(s => s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo &&
							   (s.Nome.Contains(q) || s.Citta.Contains(q)))
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
							   (s.Nome.Contains(q) || s.Descrizione.Contains(q)))
					.Take(limit)
					.Select(s => new {
						tipo = "servizio",
						nome = s.Nome,
						location = s.Salone.Nome + " - " + s.Salone.Citta,
						id = s.SaloneId
					})
					.ToListAsync();

				var allSuggestions = saloniSuggestions.Cast<object>()
					.Concat(serviziSuggestions.Cast<object>())
					.Take(limit)
					.ToList();

				return Json(new { suggestions = allSuggestions });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Errore nei suggerimenti: {ex.Message}");
				return Json(new { suggestions = new List<object>() });
			}
		}

		[HttpGet]
		public async Task<IActionResult> Index(string q = "", string location = "", string categoria = "",
			DateTime? data = null, int page = 1, string sort = "rating", int? rating = null, decimal? prezzo = null)
		{
			const int pageSize = 12;

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

			// Filtro per termine di ricerca
			if (!string.IsNullOrWhiteSpace(q))
			{
				query = query.Where(s =>
					s.Nome.Contains(q) ||
					s.Servizi.Any(serv => serv.Nome.Contains(q) || serv.Descrizione.Contains(q)));
			}

			// Filtro per località
			if (!string.IsNullOrWhiteSpace(location))
			{
				query = query.Where(s =>
					s.Citta.Contains(location) ||
					s.Provincia.Contains(location) ||
					s.CAP.Contains(location) ||
					s.Regione.Contains(location));
			}

			// Filtro per categoria
			if (!string.IsNullOrWhiteSpace(categoria))
			{
				query = query.Where(s => s.SaloneCategorie.Any(sc => sc.Categoria.Nome.Contains(categoria)));
			}

			// Filtro per valutazione minima
			if (rating.HasValue)
			{
				query = query.Where(s => s.Recensioni.Any() && s.Recensioni.Average(r => r.Voto) >= rating.Value);
			}

			// Filtro per prezzo massimo
			if (prezzo.HasValue)
			{
				query = query.Where(s => s.Servizi.Any(serv => serv.PrezzoEffettivo <= prezzo.Value));
			}

			// Ordinamento
			switch (sort.ToLower())
			{
				case "distance":
					// TODO: Implementare ordinamento per distanza se necessario
					query = query.OrderBy(s => s.Nome);
					break;
				case "price_low":
					query = query.OrderBy(s => s.Servizi.Min(serv => serv.PrezzoEffettivo));
					break;
				case "price_high":
					query = query.OrderByDescending(s => s.Servizi.Max(serv => serv.PrezzoEffettivo));
					break;
				case "newest":
					query = query.OrderByDescending(s => s.DataCreazione);
					break;
				case "rating":
				default:
					query = query.OrderByDescending(s => s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto) : 0)
								 .ThenByDescending(s => s.DataCreazione);
					break;
			}

			var totalItems = await query.CountAsync();
			var saloni = await query
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// Carica categorie per filtri
			var categorie = await _context.Categoria.OrderBy(c => c.Nome).ToListAsync();

			var viewModel = new SearchResultsViewModel
			{
				Saloni = saloni.Select(s => new SaloneSearchResult
				{
					SaloneId = s.SaloneId,
					Nome = s.Nome,
					Citta = s.Citta,
					Provincia = s.Provincia,
					Indirizzo = s.Indirizzo,
					Telefono = s.Telefono,
					CoverImageUrl = s.Immagini?.FirstOrDefault(i => i.IsCover)?.Percorso,
					LogoUrl = s.SalonePersonalizzazione?.LogoUrl,
					Slogan = s.SalonePersonalizzazione?.Slogan,
					MediaVoti = s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto) : 0,
					NumeroRecensioni = s.Recensioni.Count,
					NumeroServizi = s.Servizi.Count,
					PrezzoMinimo = s.Servizi.Any() ? s.Servizi.Min(serv => serv.PrezzoEffettivo) : 0,
					PrezzoMassimo = s.Servizi.Any() ? s.Servizi.Max(serv => serv.PrezzoEffettivo) : 0,
					ServiziPopolari = s.Servizi.OrderBy(serv => serv.Nome).Take(3).Select(serv => serv.Nome).ToList(),
					HasPromozioni = s.Servizi.Any(serv => serv.IsPromotion && serv.DataFinePromozione > DateTime.Now),
					PersonalizzazioneColori = new PersonalizzazioneColori
					{
						ColorePrimario = s.SalonePersonalizzazione?.ColorePrimario ?? "#7c3aed",
						ColoreSecondario = s.SalonePersonalizzazione?.ColoreSecondario ?? "#ec4899",
						ColoreAccento = s.SalonePersonalizzazione?.ColoreAccento ?? "#f59e0b"
					}
				}).ToList(),
				SearchQuery = q,
				Location = location,
				CategoriaSelezionata = categoria,
				DataSelezionata = data,
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

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> MyBookings()
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

			var appuntamenti = await _context.Appuntamento
				.Include(a => a.Salone)
				.Include(a => a.Servizio)
				.Include(a => a.Dipendente)
					.ThenInclude(d => d.ApplicationUser)
				.Include(a => a.Slot)
				.Where(a => a.ApplicationUserId == userId)
				.OrderByDescending(a => a.Data)
				.ToListAsync();

			var viewModel = appuntamenti.Select(a => new UserBookingViewModel
			{
				AppuntamentoId = a.AppuntamentoId,
				SaloneId = a.SaloneId,
				ServizioId = a.ServizioId.Value,
				NomeSalone = a.Salone.Nome,
				IndirizzoSalone = $"{a.Salone.Indirizzo}, {a.Salone.Citta}",
				TelefonoSalone = a.Salone.Telefono,
				NomeServizio = a.Servizio.Nome,
				DataAppuntamento = a.Data,
				OrarioAppuntamento = $"{a.Slot.OraInizio:HH\\:mm} - {a.Slot.OraFine:HH\\:mm}",
				NomeDipendente = a.Dipendente != null ?
					$"{a.Dipendente.ApplicationUser.Nome} {a.Dipendente.ApplicationUser.Cognome}" : null,
				Prezzo = a.Servizio.PrezzoEffettivo,
				Stato = a.Stato.ToString(),
				Note = a.Note,
				CanCancel = a.Stato == HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato &&
						   a.Data > DateTime.Now.AddHours(24)
			}).ToList();

			return View(viewModel);
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> GetBookingDetails(Guid id)
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

			var appuntamento = await _context.Appuntamento
				.Include(a => a.Salone)
				.Include(a => a.Servizio)
				.Include(a => a.Dipendente)
					.ThenInclude(d => d.ApplicationUser)
				.Include(a => a.Slot)
				.FirstOrDefaultAsync(a => a.AppuntamentoId == id && a.ApplicationUserId == userId);

			if (appuntamento == null)
			{
				return Json(new { success = false, message = "Appuntamento non trovato" });
			}

			var booking = new
			{
				appuntamentoId = appuntamento.AppuntamentoId,
				saloneId = appuntamento.SaloneId,
				servizioId = appuntamento.ServizioId,
				nomeSalone = appuntamento.Salone.Nome,
				indirizzoSalone = $"{appuntamento.Salone.Indirizzo}, {appuntamento.Salone.Citta}",
				telefonoSalone = appuntamento.Salone.Telefono,
				nomeServizio = appuntamento.Servizio.Nome,
				dataAppuntamento = appuntamento.Data.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("it-IT")),
				orarioAppuntamento = $"{appuntamento.Slot.OraInizio:HH\\:mm} - {appuntamento.Slot.OraFine:HH\\:mm}",
				nomeDipendente = appuntamento.Dipendente != null ?
					$"{appuntamento.Dipendente.ApplicationUser.Nome} {appuntamento.Dipendente.ApplicationUser.Cognome}" : null,
				prezzo = appuntamento.Servizio.PrezzoEffettivo,
				stato = appuntamento.Stato.ToString(),
				note = appuntamento.Note,
				canCancel = appuntamento.Stato == HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato &&
						   appuntamento.Data > DateTime.Now.AddHours(24)
			};

			return Json(new { success = true, booking });
		}

		[Authorize]
		[HttpPost]
		public async Task<IActionResult> CancelBooking(Guid id)
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

			var appuntamento = await _context.Appuntamento
				.FirstOrDefaultAsync(a => a.AppuntamentoId == id && a.ApplicationUserId == userId);

			if (appuntamento == null)
			{
				return Json(new { success = false, message = "Appuntamento non trovato" });
			}

			// Verifica che sia cancellabile
			if (appuntamento.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato)
			{
				return Json(new { success = false, message = "L'appuntamento non può essere cancellato" });
			}

			if (appuntamento.Data <= DateTime.Now.AddHours(24))
			{
				return Json(new { success = false, message = "Non è possibile cancellare l'appuntamento con meno di 24 ore di anticipo" });
			}

			appuntamento.Stato = HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato;
			await _context.SaveChangesAsync();

			// TODO: Invia email di cancellazione
			// await _emailService.SendCancellationNotificationAsync(appuntamento);

			return Json(new { success = true, message = "Appuntamento cancellato con successo" });
		}

		// Aggiungi anche questo metodo di supporto per la navigazione
		[HttpGet]
		public async Task<IActionResult> Details(Guid id, Guid? servizio = null)
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

			var viewModel = new SaloneDetailsViewModel
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

			// Se è stato specificato un servizio, passa l'ID nella ViewBag per il pre-selezione
			if (servizio.HasValue)
			{
				ViewBag.PreselectedService = servizio.Value;
			}

			return View(viewModel);
		}

		// Metodo per gestire le richieste di feedback/recensioni
		[Authorize]
		[HttpPost]
		public async Task<IActionResult> AddReview(Guid appuntamentoId, int rating, string comment)
		{
			var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

			var appuntamento = await _context.Appuntamento
				.Include(a => a.Salone)
				.FirstOrDefaultAsync(a => a.AppuntamentoId == appuntamentoId &&
										 a.ApplicationUserId == userId &&
										 a.Data < DateTime.Now);

			if (appuntamento == null)
			{
				return Json(new { success = false, message = "Appuntamento non trovato o non completato" });
			}

			// Verifica se esiste già una recensione
			var existingReview = await _context.Recensione
				.FirstOrDefaultAsync(r => r.ApplicationUserId == userId && r.SaloneId == appuntamento.SaloneId);

			if (existingReview != null)
			{
				return Json(new { success = false, message = "Hai già lasciato una recensione per questo salone" });
			}

			var recensione = new Recensione
			{
				RecensioneId = Guid.NewGuid(),
				SaloneId = appuntamento.SaloneId,
				ApplicationUserId = userId,
				DipendenteId = appuntamento.DipendenteId,
				Voto = rating,
				Commento = comment,
				DataCreazione = DateTime.Now
			};

			_context.Recensione.Add(recensione);
			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Recensione aggiunta con successo" });
		}
	}

	public class CreateBookingRequest
	{
		public Guid SaloneId { get; set; }
		public Guid SlotId { get; set; }
		public Guid ServizioId { get; set; }
		public Guid? DipendenteId { get; set; }
		public DateTime Data { get; set; }
		public string? Note { get; set; }

		// Per utenti non registrati
		public string? Nome { get; set; }
		public string? Cognome { get; set; }
		public string? Email { get; set; }
		public string? Telefono { get; set; }
	}
}