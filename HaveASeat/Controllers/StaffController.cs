using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.Utilities.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HaveASeat.Controllers
{
	[Authorize(Roles = "Partner")]
	public class StaffController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IUserStore<ApplicationUser> _userStore;
		private readonly IUserEmailStore<ApplicationUser> _emailStore;

		public StaffController(
			ApplicationDbContext context,
			UserManager<ApplicationUser> userManager,
			IUserStore<ApplicationUser> userStore)
		{
			_context = context;
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
		}

		// GET: Staff
		public async Task<IActionResult> Index(Guid? saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

			// Se non è specificato un salone, prendi il primo disponibile
			if (!saloneId.HasValue)
			{
				var primoSalone = await _context.Salone
					.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

				if (primoSalone != null)
				{
					saloneId = primoSalone.SaloneId;
				}
				else
				{
					return RedirectToAction("Index", "Partner");
				}
			}

			// Verifica che il salone appartenga all'utente
			var salone = await _context.Salone
				.Include(s => s.Dipendenti)
					.ThenInclude(d => d.ApplicationUser)
				.Include(s => s.Dipendenti)
					.ThenInclude(d => d.ServiziOfferti)
						.ThenInclude(ds => ds.Servizio)
				.Include(s => s.Dipendenti)
					.ThenInclude(d => d.Orari)
				.Include(s => s.Servizi)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return NotFound();
			}

			// Ottieni tutti i saloni per il dropdown
			var saloni = await _context.Salone
				.Where(s => s.ApplicationUserId == userId)
				.OrderBy(s => s.Nome)
				.ToListAsync();

			ViewBag.Saloni = saloni;
			ViewBag.SaloneCorrente = salone;

			return View(salone.Dipendenti.OrderBy(d => d.ApplicationUser.Cognome).ToList());
		}

		// GET: Staff/Create
		public async Task<IActionResult> Create(Guid saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var salone = await _context.Salone
				.Include(s => s.Servizi)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return NotFound();
			}

			ViewBag.Salone = salone;
			ViewBag.Servizi = salone.Servizi.ToList();

			return View();
		}

		// POST: Staff/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Guid saloneId, string nome, string cognome, string email, string telefono, List<Guid> serviziIds)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var salone = await _context.Salone
				.Include(s => s.Servizi)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					// Verifica se l'utente esiste già
					var existingUser = await _userManager.FindByEmailAsync(email);
					ApplicationUser staffUser;

					if (existingUser == null)
					{
						// Crea nuovo utente
						staffUser = new ApplicationUser
						{
							Nome = nome,
							Cognome = cognome,
							Email = email,
							PhoneNumber = telefono,
							UserName = email,
							EmailConfirmed = true
						};

						var tempPassword = GenerateTemporaryPassword();
						var result = await _userManager.CreateAsync(staffUser, tempPassword);

						if (!result.Succeeded)
						{
							foreach (var error in result.Errors)
							{
								ModelState.AddModelError("", error.Description);
							}
							ViewBag.Salone = salone;
							ViewBag.Servizi = salone.Servizi.ToList();
							return View();
						}

						// Assegna il ruolo User (o Staff se esiste)
						await _userManager.AddToRoleAsync(staffUser, "User");

						// TODO: Invia email con credenziali temporanee
					}
					else
					{
						staffUser = existingUser;
					}

					// Crea il record Dipendente
					var dipendente = new Dipendente
					{
						DipendenteId = Guid.NewGuid(),
						SaloneId = saloneId,
						ApplicationUserId = staffUser.Id,
						DataCreazione = DateTime.Now
					};

					_context.Dipendente.Add(dipendente);
					await _context.SaveChangesAsync();

					// Assegna i servizi selezionati
					if (serviziIds != null && serviziIds.Any())
					{
						foreach (var servizioId in serviziIds)
						{
							var dipendenteServizio = new DipendenteServizio
							{
								DipendenteServizioId = Guid.NewGuid(),
								DipendenteId = dipendente.DipendenteId,
								ServizioId = servizioId
							};
							_context.DipendenteServizio.Add(dipendenteServizio);
						}
						await _context.SaveChangesAsync();
					}

					TempData["SuccessMessage"] = $"Staff {nome} {cognome} aggiunto con successo!";
					return RedirectToAction("Index", new { saloneId });
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", "Errore durante la creazione: " + ex.Message);
				}
			}

			ViewBag.Salone = salone;
			ViewBag.Servizi = salone.Servizi.ToList();
			return View();
		}

		// GET: Staff/Details/5
		public async Task<IActionResult> Details(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var dipendente = await _context.Dipendente
				.Include(d => d.ApplicationUser)
				.Include(d => d.Salone)
				.Include(d => d.ServiziOfferti)
					.ThenInclude(ds => ds.Servizio)
				.Include(d => d.Orari)
				.Include(d => d.Appuntamenti)
					.ThenInclude(a => a.ApplicationUser)
				.Include(d => d.Recensioni)
					.ThenInclude(r => r.ApplicationUser)
				.FirstOrDefaultAsync(d => d.DipendenteId == id && d.Salone.ApplicationUserId == userId);

			if (dipendente == null)
			{
				return NotFound();
			}

			return View(dipendente);
		}

		// GET: Staff/Edit/5
		public async Task<IActionResult> Edit(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var dipendente = await _context.Dipendente
				.Include(d => d.ApplicationUser)
				.Include(d => d.Salone)
					.ThenInclude(s => s.Servizi)
				.Include(d => d.ServiziOfferti)
				.FirstOrDefaultAsync(d => d.DipendenteId == id && d.Salone.ApplicationUserId == userId);

			if (dipendente == null)
			{
				return NotFound();
			}

			ViewBag.Servizi = dipendente.Salone.Servizi.ToList();
			ViewBag.ServiziAssegnati = dipendente.ServiziOfferti.Select(ds => ds.ServizioId).ToList();

			return View(dipendente);
		}

		// POST: Staff/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, string nome, string cognome, string telefono, List<Guid> serviziIds)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var dipendente = await _context.Dipendente
				.Include(d => d.ApplicationUser)
				.Include(d => d.Salone)
					.ThenInclude(s => s.Servizi)
				.Include(d => d.ServiziOfferti)
				.FirstOrDefaultAsync(d => d.DipendenteId == id && d.Salone.ApplicationUserId == userId);

			if (dipendente == null)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					// Aggiorna i dati dell'utente
					dipendente.ApplicationUser.Nome = nome;
					dipendente.ApplicationUser.Cognome = cognome;
					dipendente.ApplicationUser.PhoneNumber = telefono;

					// Rimuovi tutti i servizi esistenti
					_context.DipendenteServizio.RemoveRange(dipendente.ServiziOfferti);

					// Aggiungi i nuovi servizi
					if (serviziIds != null && serviziIds.Any())
					{
						foreach (var servizioId in serviziIds)
						{
							var dipendenteServizio = new DipendenteServizio
							{
								DipendenteServizioId = Guid.NewGuid(),
								DipendenteId = dipendente.DipendenteId,
								ServizioId = servizioId
							};
							_context.DipendenteServizio.Add(dipendenteServizio);
						}
					}

					await _context.SaveChangesAsync();
					TempData["SuccessMessage"] = "Dati staff aggiornati con successo!";

					return RedirectToAction("Details", new { id });
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", "Errore durante l'aggiornamento: " + ex.Message);
				}
			}

			ViewBag.Servizi = dipendente.Salone.Servizi.ToList();
			ViewBag.ServiziAssegnati = serviziIds ?? new List<Guid>();
			return View(dipendente);
		}

		// GET: Staff/Delete/5
		public async Task<IActionResult> Delete(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var dipendente = await _context.Dipendente
				.Include(d => d.ApplicationUser)
				.Include(d => d.Salone)
				.Include(d => d.ServiziOfferti)
					.ThenInclude(ds => ds.Servizio)
				.FirstOrDefaultAsync(d => d.DipendenteId == id && d.Salone.ApplicationUserId == userId);

			if (dipendente == null)
			{
				return NotFound();
			}

			return View(dipendente);
		}

		// POST: Staff/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var dipendente = await _context.Dipendente
				.Include(d => d.Salone)
				.Include(d => d.Appuntamenti)
				.FirstOrDefaultAsync(d => d.DipendenteId == id && d.Salone.ApplicationUserId == userId);

			if (dipendente == null)
			{
				return NotFound();
			}

			// Verifica se ci sono appuntamenti futuri
			var appuntamentiFuturi = dipendente.Appuntamenti
				.Any(a => a.Data > DateTime.Now && a.Stato != StatoAppuntamento.Annullato);

			if (appuntamentiFuturi)
			{
				TempData["ErrorMessage"] = "Impossibile rimuovere lo staff: ci sono appuntamenti futuri programmati.";
				return RedirectToAction("Details", new { id });
			}

			var saloneId = dipendente.SaloneId;

			// Rimuovi il dipendente (CASCADE eliminerà anche DipendenteServizio e OrarioDipendente)
			_context.Dipendente.Remove(dipendente);
			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Staff rimosso con successo.";
			return RedirectToAction("Index", new { saloneId });
		}

		// GET: Staff/Schedule/5
		public async Task<IActionResult> Schedule(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var dipendente = await _context.Dipendente
				.Include(d => d.ApplicationUser)
				.Include(d => d.Salone)
				.Include(d => d.Orari)
				.FirstOrDefaultAsync(d => d.DipendenteId == id && d.Salone.ApplicationUserId == userId);

			if (dipendente == null)
			{
				return NotFound();
			}

			return View(dipendente);
		}

		// POST: Staff/Schedule/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Schedule(Guid id, Dictionary<int, string> apertura, Dictionary<int, string> chiusura, List<int> giorniRiposo)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var dipendente = await _context.Dipendente
				.Include(d => d.Salone)
				.Include(d => d.Orari)
				.FirstOrDefaultAsync(d => d.DipendenteId == id && d.Salone.ApplicationUserId == userId);

			if (dipendente == null)
			{
				return NotFound();
			}

			try
			{
				// Rimuovi gli orari esistenti
				_context.OrarioDipendente.RemoveRange(dipendente.Orari);

				// Aggiungi i nuovi orari
				for (int giorno = 0; giorno < 7; giorno++)
				{
					var orarioDipendente = new OrarioDipendente
					{
						OrarioDipendenteId = Guid.NewGuid(),
						DipendenteId = dipendente.DipendenteId,
						Giorno = (DayOfWeek)giorno,
						DataCreazione = DateTime.Now,
						IsDayOff = giorniRiposo?.Contains(giorno) ?? false
					};

					if (!orarioDipendente.IsDayOff && apertura.ContainsKey(giorno) && chiusura.ContainsKey(giorno))
					{
						if (TimeSpan.TryParse(apertura[giorno], out var oraApertura) &&
							TimeSpan.TryParse(chiusura[giorno], out var oraChiusura))
						{
							orarioDipendente.Apertura = oraApertura;
							orarioDipendente.Chiusura = oraChiusura;
						}
					}

					_context.OrarioDipendente.Add(orarioDipendente);
				}

				await _context.SaveChangesAsync();
				TempData["SuccessMessage"] = "Orari del dipendente aggiornati con successo!";

				return RedirectToAction("Details", new { id });
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", "Errore durante l'aggiornamento degli orari: " + ex.Message);
				return View(dipendente);
			}
		}

		// Helper Methods
		private string GenerateTemporaryPassword()
		{
			var random = new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			return new string(Enumerable.Repeat(chars, 8)
				.Select(s => s[random.Next(s.Length)]).ToArray()) + "!";
		}

		private IUserEmailStore<ApplicationUser> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
			{
				throw new NotSupportedException("The default UI requires a user store with email support.");
			}
			return (IUserEmailStore<ApplicationUser>)_userStore;
		}
	}
}