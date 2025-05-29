using HaveASeat.Data;
using HaveASeat.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HaveASeat.Controllers
{
	public class SaloneController : Controller
	{
		private readonly ApplicationDbContext _context;
		public SaloneController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpPost]
		public IActionResult Create(Salone model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return RedirectToAction("Error", "Home");
			}
			var user = _context.Users.Find(userId);
			if (user == null)
			{
				return RedirectToAction("Error", "Home");
			}

			var salone = new Salone
			{
				Nome = model.Nome,
				Indirizzo = model.Indirizzo,
				Citta = model.Citta,
				Provincia = model.Provincia,
				CAP = model.CAP,
				Regione = model.Regione,
				Telefono = model.Telefono,
				Email = model.Email,
				SitoWeb = model.SitoWeb,
				PartitaIVA = model.PartitaIVA,
				RagioneSociale = model.RagioneSociale,
				SDI = model.SDI,
				ApplicationUserId = userId,
				ApplicationUser = user,
			};

			_context.Salone.Add(salone);
			_context.SaveChanges();

			return RedirectToAction("CreateCheckoutSession", "Partner", new { id = userId });
		}
	}
}
