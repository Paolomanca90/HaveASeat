using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.Utilities.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HaveASeat.Controllers
{
	public class GiftCardController : Controller
	{
		private readonly ApplicationDbContext _context;

		public GiftCardController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: GiftCard
		public async Task<IActionResult> Index()
		{
			var popularSalons = await _context.Salone
				.Include(s => s.Servizi)
				.Include(s => s.Recensioni)
				.Include(s => s.Immagini)
				.Where(s => s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo)
				.OrderByDescending(s => s.Recensioni.Count)
				.Take(8)
				.ToListAsync();

			ViewBag.PopularSalons = popularSalons;

			return View();
		}

		// GET: GiftCard/Create
		public async Task<IActionResult> Create(Guid? saloneId = null, decimal? amount = null)
		{
			var viewModel = new CreateGiftCardViewModel
			{
				SaloneId = saloneId,
				Amount = amount ?? 50
			};

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

		// POST: GiftCard/Create
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

				// Qui potresti aggiungere l'invio dell'email

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