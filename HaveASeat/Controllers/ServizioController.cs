using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.Utilities.Constants;
using HaveASeat.Utilities.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HaveASeat.Controllers
{
	[Authorize(Roles = "Partner")]
	public class ServizioController : Controller
	{
		private readonly ApplicationDbContext _context;

		public ServizioController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: Servizio
		public async Task<IActionResult> Index(Guid? saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

			// Ottieni tutti i saloni dell'utente
			var saloni = await _context.Salone
				.Include(x => x.SaloneAbbonamenti)
				.Where(s => s.ApplicationUserId == userId)
				.OrderBy(s => s.Nome)
				.ToListAsync();

			if (!saloni.Any())
			{
				TempData["ErrorMessage"] = "Devi prima creare almeno un salone per gestire i servizi.";
				return RedirectToAction("Index", "Partner");
			}

			// Se non è specificato un salone o il salone specificato non esiste/non appartiene all'utente
			if (!saloneId.HasValue || !saloni.Any(s => s.SaloneId == saloneId.Value))
			{
				saloneId = saloni.First().SaloneId;

				if (saloni.Count > 1)
				{
					TempData["InfoMessage"] = "Hai più sedi. È stato selezionato automaticamente il primo salone.";
					return RedirectToAction("Index", new { saloneId = saloneId.Value });
				}
			}

			var salone = await _context.Salone
				.Include(s => s.Servizi)
				.Include(d => d.Dipendenti)
					.ThenInclude(o => o.ServiziOfferti)
				.Include(d => d.Dipendenti)
					.ThenInclude(u => u.ApplicationUser)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				TempData["ErrorMessage"] = "Salone non trovato o non autorizzato.";
				return RedirectToAction("Index", "Partner");
			}

			var abbonamentoStandard = salone.SaloneAbbonamenti.Any(x => x.AbbonamentoId == SubscriptionsConstants.Basic);
			if (abbonamentoStandard)
				ViewBag.Basic = true;

			ViewBag.Saloni = saloni;
			ViewBag.SaloneCorrente = salone;
			ViewBag.HasMultipleSedi = saloni.Count > 1;

			return View(salone.Servizi.OrderBy(s => s.Nome).ToList());
		}

		// GET: Servizio/Create
		public async Task<IActionResult> Create(Guid saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var salone = await _context.Salone
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				TempData["ErrorMessage"] = "Salone non trovato o non autorizzato.";
				return RedirectToAction("Index");
			}

			ViewBag.Salone = salone;
			return View(new Servizio { SaloneId = saloneId });
		}

		// POST: Servizio/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Guid saloneId, Servizio servizio)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var salone = await _context.Salone
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				TempData["ErrorMessage"] = "Salone non trovato o non autorizzato.";
				return RedirectToAction("Index");
			}

			// Verifica che non esista già un servizio con lo stesso nome per questo salone
			var existingService = await _context.Servizio
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.Nome.ToLower() == servizio.Nome.ToLower());

			if (existingService != null)
			{
				ModelState.AddModelError("Nome", "Esiste già un servizio con questo nome per questo salone.");
			}

			// Validazioni aggiuntive
			if (servizio.Prezzo <= 0)
			{
				ModelState.AddModelError("Prezzo", "Il prezzo deve essere maggiore di zero.");
			}

			if (servizio.Durata <= 0)
			{
				ModelState.AddModelError("Durata", "La durata deve essere maggiore di zero.");
			}

			if (ModelState.IsValid)
			{
				servizio.ServizioId = Guid.NewGuid();
				servizio.SaloneId = saloneId;
				servizio.Salone = salone;

				_context.Servizio.Add(servizio);
				await _context.SaveChangesAsync();

				TempData["SuccessMessage"] = $"Servizio '{servizio.Nome}' aggiunto con successo!";
				return RedirectToAction("Index", new { saloneId });
			}

			ViewBag.Salone = salone;
			return View(servizio);
		}

		// GET: Servizio/Edit/5
		public async Task<IActionResult> Edit(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var servizio = await _context.Servizio
				.Include(s => s.Salone)
				.Include(s => s.DipendenteServizi)
				.FirstOrDefaultAsync(s => s.ServizioId == id && s.Salone.ApplicationUserId == userId);

			if (servizio == null)
			{
				return NotFound();
			}

			ViewBag.Salone = servizio.Salone;
			return View(servizio);
		}

		// POST: Servizio/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, Servizio servizio)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var existingServizio = await _context.Servizio
				.Include(s => s.Salone)
				.FirstOrDefaultAsync(s => s.ServizioId == id && s.Salone.ApplicationUserId == userId);

			if (existingServizio == null)
			{
				return NotFound();
			}

			// Verifica che non esista già un servizio con lo stesso nome (escluso quello corrente)
			var duplicateService = await _context.Servizio
				.FirstOrDefaultAsync(s => s.SaloneId == existingServizio.SaloneId &&
										s.Nome.ToLower() == servizio.Nome.ToLower() &&
										s.ServizioId != id);

			if (duplicateService != null)
			{
				ModelState.AddModelError("Nome", "Esiste già un servizio con questo nome per questo salone.");
			}

			if (servizio.Prezzo <= 0)
			{
				ModelState.AddModelError("Prezzo", "Il prezzo deve essere maggiore di zero.");
			}

			if (servizio.Durata <= 0)
			{
				ModelState.AddModelError("Durata", "La durata deve essere maggiore di zero.");
			}

			if (ModelState.IsValid)
			{
				existingServizio.Nome = servizio.Nome;
				existingServizio.Descrizione = servizio.Descrizione;
				existingServizio.Prezzo = servizio.Prezzo;
				existingServizio.Durata = servizio.Durata;
				existingServizio.IsPromotion = servizio.IsPromotion;
				existingServizio.DataInizioPromozione = servizio.DataInizioPromozione;
				existingServizio.DataFinePromozione = servizio.DataFinePromozione;

				await _context.SaveChangesAsync();
				TempData["SuccessMessage"] = "Servizio aggiornato con successo!";

				return RedirectToAction("Index", new { saloneId = existingServizio.SaloneId });
			}

			ViewBag.Salone = existingServizio.Salone;
			return View(servizio);
		}

		// GET: Servizio/Details/5
		public async Task<IActionResult> Details(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var servizio = await _context.Servizio
				.Include(s => s.Salone)
				.Include(s => s.DipendenteServizi)
					.ThenInclude(ds => ds.Dipendente)
						.ThenInclude(d => d.ApplicationUser)
				.FirstOrDefaultAsync(s => s.ServizioId == id && s.Salone.ApplicationUserId == userId);

			if (servizio == null)
			{
				return NotFound();
			}

			return View(servizio);
		}

		// GET: Servizio/Delete/5
		public async Task<IActionResult> Delete(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var servizio = await _context.Servizio
				.Include(s => s.Salone)
				.Include(s => s.DipendenteServizi)
					.ThenInclude(ds => ds.Dipendente)
						.ThenInclude(d => d.ApplicationUser)
				.FirstOrDefaultAsync(s => s.ServizioId == id && s.Salone.ApplicationUserId == userId);

			if (servizio == null)
			{
				return NotFound();
			}

			return View(servizio);
		}

		// POST: Servizio/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var servizio = await _context.Servizio
				.Include(s => s.Salone)
				.Include(s => s.DipendenteServizi)
				.FirstOrDefaultAsync(s => s.ServizioId == id && s.Salone.ApplicationUserId == userId);

			if (servizio == null)
			{
				return NotFound();
			}

			var saloneId = servizio.SaloneId;

			// Rimuovi le associazioni con i dipendenti
			if (servizio.DipendenteServizi.Any())
			{
				_context.DipendenteServizio.RemoveRange(servizio.DipendenteServizi);
			}

			_context.Servizio.Remove(servizio);
			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = $"Servizio '{servizio.Nome}' eliminato con successo.";
			return RedirectToAction("Index", new { saloneId });
		}

		[HttpGet]
		public async Task<IActionResult> GetPromotionModal(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var servizio = await _context.Servizio
				.Include(s => s.Salone)
				.FirstOrDefaultAsync(s => s.ServizioId == id && s.Salone.ApplicationUserId == userId);

			if (servizio == null)
			{
				return NotFound();
			}

			return PartialView("_PromotionModal", servizio);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> TogglePromotion([FromBody] TogglePromotionDto dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var servizio = await _context.Servizio
				.Include(s => s.Salone)
				.FirstOrDefaultAsync(s => s.ServizioId == dto.ServizioId && s.Salone.ApplicationUserId == userId);

			if (servizio == null)
			{
				return Json(new { success = false, message = "Servizio non trovato" });
			}

			try
			{
				if (dto.IsPromotion)
				{
					// Attiva promozione
					if (!dto.PrezzoPromozione.HasValue || !dto.DataFinePromozione.HasValue)
					{
						return Json(new { success = false, message = "Prezzo e data di fine promozione sono obbligatori" });
					}

					if (dto.PrezzoPromozione.Value <= 0)
					{
						return Json(new { success = false, message = "Il prezzo promozionale deve essere maggiore di zero" });
					}

					if (dto.PrezzoPromozione.Value >= servizio.Prezzo)
					{
						return Json(new { success = false, message = "Il prezzo promozionale deve essere inferiore al prezzo standard" });
					}

					if (dto.DataFinePromozione.Value <= DateTime.Now)
					{
						return Json(new { success = false, message = "La data di fine deve essere futura" });
					}

					// Attiva la promozione
					servizio.IsPromotion = true;
					servizio.PrezzoPromozione = dto.PrezzoPromozione.Value;
					servizio.DataInizioPromozione = DateTime.Now;
					servizio.DataFinePromozione = dto.DataFinePromozione.Value;
				}
				else
				{
					// Disattiva promozione
					servizio.IsPromotion = false;
					servizio.PrezzoPromozione = 0;
				}

				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					isPromotion = servizio.IsPromotion,
					prezzoStandard = servizio.Prezzo,
					prezzoPromozione = servizio.PrezzoPromozione,
					message = servizio.IsPromotion ? "Promozione attivata con successo" : "Promozione disattivata"
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Errore durante l'aggiornamento: " + ex.Message });
			}
		}

		// GET: Servizio/Duplicate/5
		public async Task<IActionResult> Duplicate(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var servizio = await _context.Servizio
				.Include(s => s.Salone)
				.FirstOrDefaultAsync(s => s.ServizioId == id && s.Salone.ApplicationUserId == userId);

			if (servizio == null)
			{
				return NotFound();
			}

			var duplicatedServizio = new Servizio
			{
				Nome = $"{servizio.Nome} - Copia",
				Descrizione = servizio.Descrizione,
				Prezzo = servizio.Prezzo,
				Durata = servizio.Durata,
				SaloneId = servizio.SaloneId,
				IsPromotion = false
			};

			ViewBag.Salone = servizio.Salone;
			ViewBag.OriginalService = servizio;
			return View("Create", duplicatedServizio);
		}
	}
}