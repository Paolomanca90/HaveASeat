using HaveASeat.Data;
using HaveASeat.Utilities.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HaveASeat.Controllers
{
	[Authorize(Roles = "Partner")]
	public class PromotionsController : Controller
	{
		private readonly ApplicationDbContext _context;

		public PromotionsController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: Promotions
		public async Task<IActionResult> Index(Guid? saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

			var saloni = await _context.Salone
				.Where(s => s.ApplicationUserId == userId)
				.OrderBy(s => s.Nome)
				.ToListAsync();

			if (!saloni.Any())
			{
				TempData["ErrorMessage"] = "Devi prima creare almeno un salone per gestire le promozioni.";
				return RedirectToAction("Index", "Partner");
			}

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
					.ThenInclude(serv => serv.DipendenteServizi)
						.ThenInclude(ds => ds.Dipendente)
							.ThenInclude(d => d.ApplicationUser)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				TempData["ErrorMessage"] = "Salone non trovato o non autorizzato.";
				return RedirectToAction("Index", "Partner");
			}

			ViewBag.Saloni = saloni;
			ViewBag.SaloneCorrente = salone;
			ViewBag.HasMultipleSedi = saloni.Count > 1;

			// Prepara i dati per la vista
			var promozioniAttive = salone.Servizi
				.Where(s => s.IsPromotion && s.DataFinePromozione > DateTime.Now)
				.ToList();

			var serviziSenzaPromo = salone.Servizi
				.Where(s => !s.IsPromotion)
				.ToList();

			ViewBag.PromozioniAttive = promozioniAttive;
			ViewBag.ServiziSenzaPromo = serviziSenzaPromo;

			return View(salone.Servizi.ToList());
		}

		// POST: Promotions/ActivatePromotion
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ActivatePromotion([FromBody] TogglePromotionDto dto)
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

		// POST: Promotions/ExtendPromotion
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ExtendPromotion(Guid servizioId, DateTime nuovaDataFine)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var servizio = await _context.Servizio
				.Include(s => s.Salone)
				.FirstOrDefaultAsync(s => s.ServizioId == servizioId && s.Salone.ApplicationUserId == userId);

			if (servizio == null)
			{
				return Json(new { success = false, message = "Servizio non trovato" });
			}

			if (!servizio.IsPromotion)
			{
				return Json(new { success = false, message = "Il servizio non è in promozione" });
			}

			if (nuovaDataFine <= DateTime.Now)
			{
				return Json(new { success = false, message = "La nuova data deve essere futura" });
			}

			try
			{
				servizio.DataFinePromozione = nuovaDataFine;
				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					message = "Promozione estesa con successo",
					nuovaDataFine = nuovaDataFine.ToString("dd/MM/yyyy")
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Errore durante l'estensione: " + ex.Message });
			}
		}

		// POST: Promotions/BulkActivate
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> BulkActivate(Guid saloneId, List<Guid> serviziIds, decimal percentualeSconto, DateTime dataFine)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var servizi = await _context.Servizio
				.Include(s => s.Salone)
				.Where(s => serviziIds.Contains(s.ServizioId) && s.Salone.ApplicationUserId == userId && s.SaloneId == saloneId)
				.ToListAsync();

			if (!servizi.Any())
			{
				return Json(new { success = false, message = "Nessun servizio trovato" });
			}

			if (percentualeSconto <= 0 || percentualeSconto >= 100)
			{
				return Json(new { success = false, message = "La percentuale di sconto deve essere tra 1 e 99" });
			}

			if (dataFine <= DateTime.Now)
			{
				return Json(new { success = false, message = "La data di fine deve essere futura" });
			}

			try
			{
				int promozioniAttivate = 0;

				foreach (var servizio in servizi)
				{
					var prezzoPromo = servizio.Prezzo * (1 - percentualeSconto / 100);

					servizio.IsPromotion = true;
					servizio.PrezzoPromozione = prezzoPromo;
					servizio.DataInizioPromozione = DateTime.Now;
					servizio.DataFinePromozione = dataFine;

					promozioniAttivate++;
				}

				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					message = $"Promozione attivata per {promozioniAttivate} servizi",
					promozioniAttivate = promozioniAttivate
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Errore durante l'attivazione multipla: " + ex.Message });
			}
		}

		// GET: Promotions/GetPromotionStats
		[HttpGet]
		public async Task<IActionResult> GetPromotionStats(Guid saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var servizi = await _context.Servizio
				.Include(s => s.Salone)
				.Where(s => s.SaloneId == saloneId && s.Salone.ApplicationUserId == userId)
				.ToListAsync();

			var stats = new
			{
				TotaleServizi = servizi.Count,
				PromozioniAttive = servizi.Count(s => s.IsPromotion && s.DataFinePromozione > DateTime.Now),
				PromozioniScadute = servizi.Count(s => s.IsPromotion && s.DataFinePromozione <= DateTime.Now),
				ServiziNormali = servizi.Count(s => !s.IsPromotion),
				ScontoMedio = servizi.Where(s => s.IsPromotion && s.DataFinePromozione > DateTime.Now)
					.Any() ?
					servizi.Where(s => s.IsPromotion && s.DataFinePromozione > DateTime.Now)
						.Average(s => ((s.Prezzo - s.PrezzoPromozione) / s.Prezzo) * 100) : 0
			};

			return Json(stats);
		}
	}
}