using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.Services;
using HaveASeat.Utilities.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HaveASeat.Controllers
{
	public class GiftCardController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IGiftCardPdfService _pdfService;
		//private readonly IEmailService _emailService;

		public GiftCardController(ApplicationDbContext context, IGiftCardPdfService pdfService /*,IEmailService emailService*/)
		{
			_context = context;
			_pdfService = pdfService;
			//_emailService = emailService;
		}

		// GET: GiftCard - Solo landing page informativa
		public async Task<IActionResult> Index()
		{
			var popularSalons = await _context.Salone
				.Include(s => s.Servizi)
				.Include(s => s.SaloneAbbonamenti)
				.Include(s => s.Recensioni)
				.Include(s => s.Immagini)
				.Where(s => s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo)
				.OrderByDescending(s => s.Recensioni.Count)
				.Take(8)
				.ToListAsync();

			var saloni = popularSalons.Select(s => new {
				SaloneId = s.SaloneId,
				Nome = s.Nome,
				Citta = s.Citta,
				CoverImageUrl = s.Immagini?.FirstOrDefault(i => i.IsCover)?.Percorso,
				IsPremium = s.SaloneAbbonamenti.Any(sa =>
						sa.Abbonamento.Nome.Contains("Pro") || sa.Abbonamento.Nome.Contains("Business")),
			});

			ViewBag.PopularSalons = saloni;
			return View();
		}

		// GET: GiftCard/Create - Form completo in una pagina
		public async Task<IActionResult> Create(Guid? saloneId = null, decimal? amount = null)
		{
			var viewModel = new CreateGiftCardViewModel
			{
				SaloneId = saloneId,
				Amount = amount ?? 50
			};

			// Se arriva un saloneId specifico, caricalo
			if (saloneId.HasValue)
			{
				var salone = await _context.Salone
					.Include(s => s.Servizi)
					.Include(s => s.Immagini)
					.FirstOrDefaultAsync(s => s.SaloneId == saloneId.Value);

				if (salone != null)
				{
					viewModel.SelectedSalone = salone;
				}
			}

			return View(viewModel);
		}

		// API: Ricerca saloni per AJAX
		[HttpGet]
		public async Task<IActionResult> SearchSalons(string query, int limit = 10)
		{
			if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
			{
				return Json(new { salons = new List<object>() });
			}

			var salons = await _context.Salone
				.Where(s => s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo)
				.Where(s => s.Nome.Contains(query) ||
						   s.Citta.Contains(query) ||
						   s.Provincia.Contains(query))
				.Select(s => new
				{
					id = s.SaloneId,
					nome = s.Nome,
					citta = s.Citta,
					provincia = s.Provincia,
					indirizzo = s.Indirizzo,
					logo = s.Immagini.FirstOrDefault(i => i.IsLogo) != null ?
						   s.Immagini.FirstOrDefault(i => i.IsLogo).Percorso : null
				})
				.Take(limit)
				.ToListAsync();

			return Json(new { salons });
		}

		// POST: GiftCard/Create - Creazione gift card
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CreateGiftCardDto model)
		{
			if (!ModelState.IsValid)
			{
				return Json(new { success = false, message = "Dati non validi" });
			}

			try
			{
				var giftCard = new GiftCard
				{
					GiftCardId = Guid.NewGuid(),
					Code = GenerateGiftCardCode(),
					Amount = model.Amount,
					SaloneId = model.SaloneId,
					RecipientName = model.RecipientName,
					RecipientEmail = model.RecipientEmail,
					SenderName = model.SenderName,
					SenderEmail = model.SenderEmail,
					Message = model.Message,
					ExpiryDate = DateTime.Now.AddYears(1),
					IsUsed = false,
					CreatedAt = DateTime.Now
				};

				_context.GiftCard.Add(giftCard);
				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					message = "Gift card creata con successo!",
					redirectUrl = Url.Action("Confirmation", new { id = giftCard.GiftCardId })
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Errore durante la creazione della gift card" });
			}
		}


		// GET: GiftCard/Confirmation
		public async Task<IActionResult> Confirmation(Guid id)
		{
			var giftCard = await _context.GiftCard
				.Include(g => g.Salone)
				.FirstOrDefaultAsync(g => g.GiftCardId == id);

			if (giftCard == null)
			{
				return NotFound();
			}

			return View(giftCard);
		}

		// GET: GiftCard/Redeem
		public IActionResult Redeem()
		{
			return View();
		}

		// POST: GiftCard/Redeem
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Redeem(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				return Json(new { success = false, message = "Inserisci un codice valido" });
			}

			var giftCard = await _context.GiftCard
				.Include(g => g.Salone)
				.FirstOrDefaultAsync(g => g.Code == code.ToUpper());

			if (giftCard == null)
			{
				return Json(new { success = false, message = "Codice gift card non valido" });
			}

			if (giftCard.IsUsed)
			{
				return Json(new { success = false, message = "Questa gift card è già stata utilizzata" });
			}

			if (giftCard.ExpiryDate < DateTime.Now)
			{
				return Json(new { success = false, message = "Questa gift card è scaduta" });
			}

			return Json(new
			{
				success = true,
				giftCard = new
				{
					amount = giftCard.Amount,
					saloneName = giftCard.Salone?.Nome ?? "Qualsiasi salone",
					expiryDate = giftCard.ExpiryDate.ToString("dd/MM/yyyy"),
					recipientName = giftCard.RecipientName
				},
				redirectUrl = giftCard.SaloneId.HasValue
					? Url.Action("Details", "Search", new { id = giftCard.SaloneId })
					: Url.Action("Index", "Search")
			});
		}

		// Helper method per generare codice gift card
		private string GenerateGiftCardCode()
		{
			var random = new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			var code = new string(Enumerable.Repeat(chars, 12)
				.Select(s => s[random.Next(s.Length)]).ToArray());

			// Formato: XXXX-XXXX-XXXX
			return $"{code.Substring(0, 4)}-{code.Substring(4, 4)}-{code.Substring(8, 4)}";
		}

		// GET: GiftCard/DownloadPDF/{id}
		[HttpGet]
		public async Task<IActionResult> DownloadPDF(Guid id)
		{
			try
			{
				var giftCard = await _context.GiftCard
					.Include(g => g.Salone)
					.FirstOrDefaultAsync(g => g.GiftCardId == id);

				if (giftCard == null)
				{
					return NotFound("Buono regalo non trovato");
				}

				// Genera il PDF
				var pdfBytes = await _pdfService.GenerateGiftCardPdfAsync(giftCard);

				// Nome file
				var fileName = $"buono-regalo-{giftCard.FormattedCode.Replace("-", "")}.pdf";

				// Restituisci il file PDF
				return File(pdfBytes, "application/pdf", fileName);
			}
			catch (Exception ex)
			{
				// Log dell'errore
				return BadRequest("Errore durante la generazione del PDF");
			}
		}

		// GET: GiftCard/PreviewPDF/{id} - Per vedere l'anteprima nel browser
		[HttpGet]
		public async Task<IActionResult> PreviewPDF(Guid id)
		{
			try
			{
				var giftCard = await _context.GiftCard
					.Include(g => g.Salone)
					.FirstOrDefaultAsync(g => g.GiftCardId == id);

				if (giftCard == null)
				{
					return NotFound("Buono regalo non trovato");
				}

				var pdfBytes = await _pdfService.GenerateGiftCardPdfAsync(giftCard);

				// Mostra nel browser invece di scaricare
				return File(pdfBytes, "application/pdf");
			}
			catch (Exception ex)
			{
				return BadRequest("Errore durante la generazione del PDF");
			}
		}

		// POST: GiftCard/ResendEmail - Per reinviare l'email
		[HttpPost]
		public async Task<IActionResult> ResendEmail(Guid giftCardId)
		{
			try
			{
				var giftCard = await _context.GiftCard
					.Include(g => g.Salone)
					.FirstOrDefaultAsync(g => g.GiftCardId == giftCardId);

				if (giftCard == null)
				{
					return Json(new { success = false, message = "Buono regalo non trovato" });
				}

				//await _emailService.SendGiftCardEmailAsync(giftCard);

				return Json(new { success = true, message = "Email inviata con successo" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Errore durante l'invio dell'email" });
			}
		}
	}

	// DTO per la creazione gift card
	public class CreateGiftCardDto
	{
		public decimal Amount { get; set; }
		public Guid? SaloneId { get; set; }
		public string RecipientName { get; set; } = string.Empty;
		public string RecipientEmail { get; set; } = string.Empty;
		public string SenderName { get; set; } = string.Empty;
		public string SenderEmail { get; set; } = string.Empty;
		public string? Message { get; set; }
	}

	// ViewModel per la pagina di creazione
	public class CreateGiftCardViewModel
	{
		public Guid? SaloneId { get; set; }
		public decimal Amount { get; set; } = 50;
		public Salone? SelectedSalone { get; set; }
	}
}