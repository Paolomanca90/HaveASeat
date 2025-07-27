using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.Utilities.Constants;
using HaveASeat.Utilities.Dto;
using HaveASeat.Utilities.Enum;
using HaveASeat.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HaveASeat.Controllers
{
	public class CalendarioController : Controller
	{
		private readonly ApplicationDbContext _context;
		public CalendarioController(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index(Guid? saloneId = null, DateTime? data = null)
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrEmpty(userId))
				{
					return RedirectToAction("Login", "Account");
				}

				// Ottieni tutti i saloni dell'utente
				var saloni = await _context.Salone
					.Include(x => x.SaloneAbbonamenti)
					.Where(s => s.ApplicationUserId == userId)
					.ToListAsync();

				if (!saloni.Any())
				{
					// Reindirizza alla creazione del primo salone
					return RedirectToAction("Create", "Salone");
				}

				// Determina il salone corrente
				var saloneCorrente = saloneId.HasValue
					? saloni.FirstOrDefault(s => s.SaloneId == saloneId.Value)
					: saloni.First();

				if (saloneCorrente == null)
				{
					saloneCorrente = saloni.First();
				}

				var abbonamentoStandard = saloneCorrente.SaloneAbbonamenti.Any(x => x.AbbonamentoId == SubscriptionsConstants.Basic);
				if (abbonamentoStandard)
					ViewBag.Basic = true;

				// Data selezionata (default oggi)
				var dataSelezionata = data ?? DateTime.Today;

				// Carica dipendenti con le relazioni necessarie
				var dipendenti = await _context.Dipendente
					.Include(d => d.ApplicationUser)
					.Where(d => d.SaloneId == saloneCorrente.SaloneId)
					.OrderBy(d => d.ApplicationUser.Nome)
					.ToListAsync();

				// Carica slots del salone
				var slots = await _context.Slot
					.Where(s => s.SaloneId == saloneCorrente.SaloneId)
					.OrderBy(s => s.OraInizio)
					.ToListAsync();

				// Carica appuntamenti del giorno con tutte le relazioni necessarie
				var appuntamenti = await _context.Appuntamento
					.Include(a => a.ApplicationUser)
					.Include(a => a.Dipendente)
						.ThenInclude(d => d.ApplicationUser)
					.Include(a => a.Slot)
					.Include(a => a.Servizio) // IMPORTANTE: Include il servizio per durata e prezzo
					.Where(a => a.SaloneId == saloneCorrente.SaloneId &&
							   a.Data.Date == dataSelezionata.Date)
					.ToListAsync();

				// Carica servizi del salone per il dropdown
				var servizi = await _context.Servizio
					.Where(s => s.SaloneId == saloneCorrente.SaloneId)
					.OrderBy(s => s.Nome)
					.ToListAsync();

				// Costruisci il ViewModel
				var viewModel = new CalendarioViewModel
				{
					Saloni = saloni,
					SaloneCorrente = saloneCorrente,
					DataSelezionata = dataSelezionata,
					InizioSettimana = GetInizioSettimana(dataSelezionata),
					FineSettimana = GetFineSettimana(dataSelezionata),
					Appuntamenti = appuntamenti,
					Dipendenti = dipendenti,
					Slots = slots,
					Servizi = servizi, // AGGIUNTO: Per il dropdown servizi
					HasMultipleSedi = saloni.Count > 1
				};

				return View(viewModel);
			}
			catch (Exception ex)
			{
				return View("Error");
			}
		}

		// Metodi helper
		private DateTime GetInizioSettimana(DateTime data)
		{
			var diff = (7 + (data.DayOfWeek - DayOfWeek.Monday)) % 7;
			return data.AddDays(-1 * diff).Date;
		}

		private DateTime GetFineSettimana(DateTime data)
		{
			return GetInizioSettimana(data).AddDays(6);
		}

		[HttpGet]
		public async Task<IActionResult> GetCalendarioData(Guid saloneId, DateTime data)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Verifica autorizzazione
			var salone = await _context.Salone
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Unauthorized();
			}

			var inizioSettimana = data.AddDays(-(int)data.DayOfWeek + (int)DayOfWeek.Monday);
			var fineSettimana = inizioSettimana.AddDays(6);

			var appuntamenti = await _context.Appuntamento
				.Include(a => a.ApplicationUser)
				.Include(a => a.Dipendente)
					.ThenInclude(d => d.ApplicationUser)
				.Include(a => a.Slot)
				.Where(a => a.SaloneId == saloneId &&
						   a.Data >= inizioSettimana &&
						   a.Data <= fineSettimana)
				.Select(a => new {
					Id = a.AppuntamentoId,
					Titolo = $"{a.ApplicationUser.Nome} {a.ApplicationUser.Cognome}",
					Data = a.Data.ToString("yyyy-MM-dd"),
					OraInizio = a.OraInizio.ToString("HH:mm"),
					OraFine = a.OraFine.ToString("HH:mm"),
					DipendenteId = a.DipendenteId,
					Dipendente = a.Dipendente != null ? $"{a.Dipendente.ApplicationUser.Nome} {a.Dipendente.ApplicationUser.Cognome}" : "Non assegnato",
					Cliente = $"{a.ApplicationUser.Nome} {a.ApplicationUser.Cognome}",
					Telefono = a.ApplicationUser.PhoneNumber,
					Stato = a.Stato.ToString(),
					Note = a.Note
				})
				.ToListAsync();

			return Json(new { success = true, appuntamenti });
		}

		[HttpPost]
		public async Task<IActionResult> CreateAppuntamento([FromBody] CreateAppuntamentoDto dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Verifica autorizzazione
			var salone = await _context.Salone
				.FirstOrDefaultAsync(s => s.SaloneId == dto.SaloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Salone non autorizzato" });
			}

			// Verifica che il cliente esista
			var cliente = await _context.Users.FindAsync(dto.ClienteId);
			if (cliente == null)
			{
				return Json(new { success = false, message = "Cliente non trovato" });
			}

			// Verifica che lo slot esista
			var slot = await _context.Slot.FindAsync(dto.SlotId);
			if (slot == null || slot.SaloneId != dto.SaloneId)
			{
				return Json(new { success = false, message = "Slot non valido" });
			}

			// Verifica disponibilità
			var esisteAppuntamento = await _context.Appuntamento
				.AnyAsync(a => a.Data.Date == dto.Data.Date &&
							  a.SlotId == dto.SlotId &&
							  a.Stato != StatoAppuntamento.Annullato);

			if (esisteAppuntamento)
			{
				return Json(new { success = false, message = "Slot già occupato" });
			}

			var appuntamento = new Appuntamento
			{
				AppuntamentoId = Guid.NewGuid(),
				SaloneId = dto.SaloneId,
				Salone = salone,
				ApplicationUserId = dto.ClienteId,
				ApplicationUser = cliente,
				SlotId = dto.SlotId,
				DipendenteId = dto.DipendenteId,
				Dipendente = _context.Dipendente.Find(dto.DipendenteId),
				Data = dto.Data,
				Note = dto.Note ?? "",
				Stato = StatoAppuntamento.Prenotato
			};

			_context.Appuntamento.Add(appuntamento);
			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Appuntamento creato con successo" });
		}

		[HttpPut]
		public async Task<IActionResult> UpdateAppuntamento(Guid id, [FromBody] UpdateAppuntamentoDto dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var appuntamento = await _context.Appuntamento
				.Include(a => a.Salone)
				.FirstOrDefaultAsync(a => a.AppuntamentoId == id && a.Salone.ApplicationUserId == userId);

			if (appuntamento == null)
			{
				return Json(new { success = false, message = "Appuntamento non trovato" });
			}

			// Aggiorna i campi
			if (dto.DipendenteId.HasValue)
				appuntamento.DipendenteId = dto.DipendenteId;

			if (!string.IsNullOrEmpty(dto.Note))
				appuntamento.Note = dto.Note;

			if (dto.Stato.HasValue)
				appuntamento.Stato = dto.Stato.Value;

			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Appuntamento aggiornato" });
		}

		[HttpDelete]
		public async Task<IActionResult> DeleteAppuntamento(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var appuntamento = await _context.Appuntamento
				.Include(a => a.Salone)
				.FirstOrDefaultAsync(a => a.AppuntamentoId == id && a.Salone.ApplicationUserId == userId);

			if (appuntamento == null)
			{
				return Json(new { success = false, message = "Appuntamento non trovato" });
			}

			// Invece di eliminare, segna come annullato
			appuntamento.Stato = StatoAppuntamento.Annullato;
			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Appuntamento annullato" });
		}

		// Metodi aggiuntivi per il PartnerController.cs

		[HttpGet]
		public async Task<IActionResult> GetAppuntamentoDetails(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var appuntamento = await _context.Appuntamento
				.Include(a => a.ApplicationUser)
				.Include(a => a.Dipendente)
					.ThenInclude(d => d.ApplicationUser)
				.Include(a => a.Slot)
				.Include(a => a.Salone)
				.Include(a => a.Servizio)
				.FirstOrDefaultAsync(a => a.AppuntamentoId == id && a.Salone.ApplicationUserId == userId);

			if (appuntamento == null)
			{
				return Json(new { success = false, message = "Appuntamento non trovato" });
			}

			var dettagli = new
			{
				id = appuntamento.AppuntamentoId,
				cliente = $"{appuntamento.ApplicationUser.Nome} {appuntamento.ApplicationUser.Cognome}",
				telefono = appuntamento.ApplicationUser.PhoneNumber,
				email = appuntamento.ApplicationUser.Email,
				data = appuntamento.Data.ToString("dd/MM/yyyy"),
				oraInizio = appuntamento.OraInizio.ToString("HH:mm"),
				oraFine = appuntamento.OraFine.ToString("HH:mm"),
				dipendente = appuntamento.Dipendente != null ?
					$"{appuntamento.Dipendente.ApplicationUser.Nome} {appuntamento.Dipendente.ApplicationUser.Cognome}" : null,
				dipendenteId = appuntamento.DipendenteId,
				stato = appuntamento.Stato.ToString(),
				note = appuntamento.Note,
				servizio = appuntamento.Servizio.Nome
			};

			return Json(new { success = true, appuntamento = dettagli });
		}

		[HttpGet]
		public async Task<IActionResult> SearchClienti(string termine)
		{
			if (string.IsNullOrWhiteSpace(termine) || termine.Length < 2)
			{
				return Json(new { success = true, clienti = new List<object>() });
			}

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Cerca tra gli utenti che hanno già fatto appuntamenti nei saloni dell'utente
			var clienti = await _context.Users
				.Where(u => _context.Appuntamento.Any(a => a.ApplicationUserId == u.Id &&
							_context.Salone.Any(s => s.SaloneId == a.SaloneId && s.ApplicationUserId == userId)) &&
						   (u.Nome.Contains(termine) ||
							u.Cognome.Contains(termine) ||
							u.Email.Contains(termine)))
				.Select(u => new
				{
					id = u.Id,
					nome = $"{u.Nome} {u.Cognome}",
					email = u.Email,
					telefono = u.PhoneNumber ?? "Non specificato"
				})
				.Take(10)
				.ToListAsync();

			return Json(new { success = true, clienti });
		}

		[HttpGet]
		public async Task<IActionResult> GetSlotDisponibili(Guid saloneId, DateTime data)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Verifica autorizzazione
			var salone = await _context.Salone
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Salone non autorizzato" });
			}

			// Recupera tutti gli slot del salone
			var slots = await _context.Slot
				.Where(s => s.SaloneId == saloneId && s.IsAttivo)
				.OrderBy(s => s.OraInizio)
				.ToListAsync();

			// Verifica quali slot sono già occupati per la data specificata
			var slotsOccupati = await _context.Appuntamento
				.Where(a => a.SaloneId == saloneId &&
						   a.Data.Date == data.Date &&
						   a.Stato != StatoAppuntamento.Annullato)
				.Select(a => a.SlotId)
				.ToListAsync();

			var slotsDisponibili = slots.Where(s => !slotsOccupati.Contains(s.SlotId))
				.Select(s => new
				{
					id = s.SlotId,
					oraInizio = s.OraInizio.ToString(@"HH\:mm"),
					oraFine = s.OraFine.ToString(@"HH\:mm"),
					display = $"{s.OraInizio:HH\\:mm} - {s.OraFine:HH\\:mm}",
					capacita = s.Capacita,
					note = s.Note
				})
				.ToList();

			return Json(new { success = true, slots = slotsDisponibili });
		}

		[HttpGet]
		public async Task<IActionResult> GetStatisticheCalendario(Guid saloneId, DateTime dataInizio, DateTime dataFine)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Verifica autorizzazione
			var salone = await _context.Salone
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Salone non autorizzato" });
			}

			var appuntamenti = await _context.Appuntamento
				.Include(a => a.Dipendente)
					.ThenInclude(d => d.ApplicationUser)
				.Where(a => a.SaloneId == saloneId &&
						   a.Data >= dataInizio &&
						   a.Data <= dataFine)
				.ToListAsync();

			var statistiche = new
			{
				totaleAppuntamenti = appuntamenti.Count,
				appuntamentiConfermati = appuntamenti.Count(a => a.Stato == StatoAppuntamento.Prenotato),
				appuntamentiAnnullati = appuntamenti.Count(a => a.Stato == StatoAppuntamento.Annullato),
				tassoAnnullamento = appuntamenti.Count > 0 ?
					Math.Round((double)appuntamenti.Count(a => a.Stato == StatoAppuntamento.Annullato) / appuntamenti.Count * 100, 1) : 0,

				// Statistiche per dipendente
				statisticheDipendenti = appuntamenti
					.Where(a => a.DipendenteId.HasValue)
					.GroupBy(a => new { a.DipendenteId, Nome = a.Dipendente.ApplicationUser.Nome, Cognome = a.Dipendente.ApplicationUser.Cognome })
					.Select(g => new
					{
						dipendenteId = g.Key.DipendenteId,
						nome = $"{g.Key.Nome} {g.Key.Cognome}",
						totaleAppuntamenti = g.Count(),
						appuntamentiConfermati = g.Count(a => a.Stato == StatoAppuntamento.Prenotato),
						appuntamentiAnnullati = g.Count(a => a.Stato == StatoAppuntamento.Annullato)
					})
					.OrderByDescending(x => x.totaleAppuntamenti)
					.ToList(),

				// Distribuzione per giorno della settimana
				distribuzioneGiorni = appuntamenti
					.Where(a => a.Stato == StatoAppuntamento.Prenotato)
					.GroupBy(a => a.Data.DayOfWeek)
					.Select(g => new
					{
						giorno = g.Key.ToString(),
						totale = g.Count()
					})
					.ToList(),

				// Distribuzione per fascia oraria
				distribuzioneFasceOrarie = appuntamenti
					.Where(a => a.Stato == StatoAppuntamento.Prenotato)
					.GroupBy(a => a.OraInizio.Hour)
					.Select(g => new
					{
						ora = g.Key,
						totale = g.Count()
					})
					.OrderBy(x => x.ora)
					.ToList()
			};

			return Json(new { success = true, statistiche });
		}

		[HttpPost]
		public async Task<IActionResult> DuplicaAppuntamento(Guid appuntamentoId, DateTime nuovaData)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var appuntamentoOriginale = await _context.Appuntamento
				.Include(a => a.Salone)
				.FirstOrDefaultAsync(a => a.AppuntamentoId == appuntamentoId && a.Salone.ApplicationUserId == userId);

			if (appuntamentoOriginale == null)
			{
				return Json(new { success = false, message = "Appuntamento non trovato" });
			}

			// Verifica che lo slot sia disponibile nella nuova data
			var slotOccupato = await _context.Appuntamento
				.AnyAsync(a => a.Data.Date == nuovaData.Date &&
							  a.SlotId == appuntamentoOriginale.SlotId &&
							  a.Stato != StatoAppuntamento.Annullato);

			if (slotOccupato)
			{
				return Json(new { success = false, message = "Lo slot è già occupato nella data selezionata" });
			}

			var nuovoAppuntamento = new Appuntamento
			{
				AppuntamentoId = Guid.NewGuid(),
				SaloneId = appuntamentoOriginale.SaloneId,
				ApplicationUserId = appuntamentoOriginale.ApplicationUserId,
				SlotId = appuntamentoOriginale.SlotId,
				DipendenteId = appuntamentoOriginale.DipendenteId,
				Data = nuovaData,
				Note = appuntamentoOriginale.Note + " (Duplicato)",
				Stato = StatoAppuntamento.Prenotato
			};

			_context.Appuntamento.Add(nuovoAppuntamento);
			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Appuntamento duplicato con successo", id = nuovoAppuntamento.AppuntamentoId });
		}

		[HttpPost]
		public async Task<IActionResult> SpostaAppuntamento(Guid appuntamentoId, DateTime nuovaData, Guid nuovoSlotId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var appuntamento = await _context.Appuntamento
				.Include(a => a.Salone)
				.FirstOrDefaultAsync(a => a.AppuntamentoId == appuntamentoId && a.Salone.ApplicationUserId == userId);

			if (appuntamento == null)
			{
				return Json(new { success = false, message = "Appuntamento non trovato" });
			}

			// Verifica che il nuovo slot sia disponibile
			var slotOccupato = await _context.Appuntamento
				.AnyAsync(a => a.Data.Date == nuovaData.Date &&
							  a.SlotId == nuovoSlotId &&
							  a.Stato != StatoAppuntamento.Annullato &&
							  a.AppuntamentoId != appuntamentoId);

			if (slotOccupato)
			{
				return Json(new { success = false, message = "Il nuovo slot è già occupato" });
			}

			// Verifica che il nuovo slot appartenga al salone
			var slot = await _context.Slot
				.FirstOrDefaultAsync(s => s.SlotId == nuovoSlotId && s.SaloneId == appuntamento.SaloneId);

			if (slot == null)
			{
				return Json(new { success = false, message = "Slot non valido" });
			}

			appuntamento.Data = nuovaData;
			appuntamento.SlotId = nuovoSlotId;

			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Appuntamento spostato con successo" });
		}
	}
}
