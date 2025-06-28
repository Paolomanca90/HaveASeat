using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.ViewModels;
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

		[HttpGet]
		public async Task<IActionResult> GetAvailableSlots(Guid saloneId, DateTime data, Guid? dipendenteId = null, Guid? servizioId = null)
		{
			var salone = await _context.Salone
				.Include(s => s.Slots)
				.Include(s => s.Orari)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Salone non trovato" });
			}

			// Ottieni il giorno della settimana
			var dayOfWeek = data.DayOfWeek;

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
					if (orarioDipendente == null || orarioDipendente.IsDayOff)
					{
						return Json(new { success = false, message = "Il dipendente selezionato non è disponibile in questo giorno" });
					}
				}
			}

			// Ottieni gli slots del salone per quel giorno
			var slotsDisponibili = salone.Slots
				.Where(s => s.IsAttivo && s.GiorniSettimana.Contains(((int)dayOfWeek).ToString()))
				.Where(s => s.OraInizio >= orarioSalone.Apertura && s.OraFine <= orarioSalone.Chiusura)
				.OrderBy(s => s.OraInizio)
				.ToList();

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
				disponibile = !appuntamentiEsistenti.Any(a => a.SlotId == slot.SlotId),
				postiLiberi = slot.Capacita - appuntamentiEsistenti.Count(a => a.SlotId == slot.SlotId)
			}).ToList();

			return Json(new { success = true, slots });
		}

		[HttpPost]
		public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
		{
			try
			{
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

				// Verifica disponibilità
				var slotOccupato = await _context.Appuntamento
					.AnyAsync(a => a.Data.Date == request.Data.Date &&
								  a.SlotId == request.SlotId &&
								  a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato &&
								  (!request.DipendenteId.HasValue || a.DipendenteId == request.DipendenteId));

				if (slotOccupato)
				{
					return Json(new { success = false, message = "Slot non più disponibile" });
				}

				// Gestisci utente non registrato
				ApplicationUser cliente;
				if (User.Identity?.IsAuthenticated == true)
				{
					var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
					cliente = await _context.Users.FindAsync(userId);
				}
				else
				{
					// Cerca se esiste già un utente con questa email
					cliente = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

					if (cliente == null)
					{
						// Crea un nuovo utente temporaneo
						cliente = new ApplicationUser
						{
							Id = Guid.NewGuid().ToString(),
							UserName = request.Email,
							Email = request.Email,
							Nome = request.Nome,
							Cognome = request.Cognome,
							PhoneNumber = request.Telefono,
							EmailConfirmed = false // Richiederà conferma successiva
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

				// TODO: Invia email di conferma se necessario

				return Json(new
				{
					success = true,
					message = "Prenotazione confermata con successo!",
					appuntamentoId = appuntamento.AppuntamentoId,
					dettagli = new
					{
						salone = salone.Nome,
						servizio = servizio.Nome,
						data = request.Data.ToString("dd/MM/yyyy"),
						ora = $"{slot.OraInizio:HH\\:mm} - {slot.OraFine:HH\\:mm}",
						prezzo = servizio.PrezzoEffettivo
					}
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Errore durante la prenotazione: " + ex.Message });
			}
		}

		[HttpGet]
		public async Task<IActionResult> Suggestions(string q, int limit = 5)
		{
			if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
			{
				return Json(new { suggestions = new List<object>() });
			}

			var saloniSuggestions = await _context.Salone
				.Where(s => s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo && s.Nome.Contains(q))
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