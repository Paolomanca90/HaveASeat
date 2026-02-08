using HaveASeat.Data;
using HaveASeat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HaveASeat.Controllers
{
	[Authorize]
	public class PreferitiController : Controller
	{
		private readonly ApplicationDbContext _context;

		public PreferitiController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: Preferiti
		public async Task<IActionResult> Index()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var preferiti = await _context.Preferito
				.Include(p => p.Salone)
					.ThenInclude(s => s.SalonePersonalizzazione)
				.Include(p => p.Salone)
					.ThenInclude(s => s.Immagini)
				.Include(p => p.Salone)
					.ThenInclude(s => s.Recensioni)
				.Include(p => p.Salone)
					.ThenInclude(s => s.SaloneCategorie)
						.ThenInclude(sc => sc.Categoria)
				.Where(p => p.ApplicationUserId == userId)
				.OrderByDescending(p => p.DataAggiunta)
				.ToListAsync();

			return View(preferiti);
		}

		// POST: Toggle preferito (aggiunge o rimuove)
		[HttpPost]
		public async Task<IActionResult> Toggle([FromBody] TogglePreferitoRequest request)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return Unauthorized(new { success = false, message = "Utente non autenticato" });
			}

			var existing = await _context.Preferito
				.FirstOrDefaultAsync(p => p.ApplicationUserId == userId && p.SaloneId == request.SaloneId);

			if (existing != null)
			{
				_context.Preferito.Remove(existing);
				await _context.SaveChangesAsync();
				return Json(new { success = true, isFavorite = false, message = "Rimosso dai preferiti" });
			}

			var preferito = new Preferito
			{
				PreferitoId = Guid.NewGuid(),
				ApplicationUserId = userId,
				SaloneId = request.SaloneId,
				DataAggiunta = DateTime.UtcNow
			};

			_context.Preferito.Add(preferito);
			await _context.SaveChangesAsync();
			return Json(new { success = true, isFavorite = true, message = "Aggiunto ai preferiti" });
		}

		// GET: Verifica se un salone è nei preferiti
		[HttpGet]
		public async Task<IActionResult> Check(Guid saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return Json(new { isFavorite = false });
			}

			var exists = await _context.Preferito
				.AnyAsync(p => p.ApplicationUserId == userId && p.SaloneId == saloneId);

			return Json(new { isFavorite = exists });
		}
	}

	public class TogglePreferitoRequest
	{
		public Guid SaloneId { get; set; }
	}
}
