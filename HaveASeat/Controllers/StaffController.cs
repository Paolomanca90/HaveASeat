using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.Utilities.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HaveASeat.Utilities.Dto;

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

			// Ottieni tutti i saloni dell'utente
			var saloni = await _context.Salone
				.Where(s => s.ApplicationUserId == userId)
				.OrderBy(s => s.Nome)
				.ToListAsync();

			// Se non ha nessun salone, reindirizza alla creazione
			if (!saloni.Any())
			{
				TempData["ErrorMessage"] = "Devi prima creare almeno un salone per gestire lo staff.";
				return RedirectToAction("Index", "Partner");
			}

			// Se non è specificato un salone o il salone specificato non esiste/non appartiene all'utente
			if (!saloneId.HasValue || !saloni.Any(s => s.SaloneId == saloneId.Value))
			{
				// Prendi il primo salone disponibile
				saloneId = saloni.First().SaloneId;

				// Se ha più di un salone, reindirizza con il saloneId per evitare ambiguità
				if (saloni.Count > 1)
				{
					TempData["InfoMessage"] = "Hai più sedi. È stato selezionato automaticamente il primo salone.";
					return RedirectToAction("Index", new { saloneId = saloneId.Value });
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
				TempData["ErrorMessage"] = "Salone non trovato o non autorizzato.";
				return RedirectToAction("Index", "Partner");
			}

			ViewBag.Saloni = saloni;
			ViewBag.SaloneCorrente = salone;
			ViewBag.HasMultipleSedi = saloni.Count > 1;

			return View(salone.Dipendenti.OrderBy(d => d.ApplicationUser.Cognome).ToList());
		}

		// GET: Staff/Create
		public async Task<IActionResult> Create(Guid saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Verifica che il salone esista e appartenga all'utente
			var salone = await _context.Salone
				.Include(s => s.Servizi)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				TempData["ErrorMessage"] = "Salone non trovato o non autorizzato.";
				return RedirectToAction("Index");
			}

			ViewBag.Salone = salone;
			ViewBag.Servizi = salone.Servizi.ToList();

			return View(new StaffDto());
		}

		// POST: Staff/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Guid saloneId, StaffDto model)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Verifica che il salone esista e appartenga all'utente
			var salone = await _context.Salone
				.Include(s => s.Servizi)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				TempData["ErrorMessage"] = "Salone non trovato o non autorizzato.";
				return RedirectToAction("Index");
			}

			// Validazioni aggiuntive
			if (string.IsNullOrWhiteSpace(model.Nome) || string.IsNullOrWhiteSpace(model.Cognome) || string.IsNullOrWhiteSpace(model.Email))
			{
				ModelState.AddModelError("", "Nome, cognome ed email sono obbligatori.");
				ViewBag.Salone = salone;
				ViewBag.Servizi = salone.Servizi.ToList();
				return View(model);
			}

			// Verifica che i servizi selezionati appartengano al salone
			if (model.ServiziIds != null && model.ServiziIds.Any())
			{
				var serviziFiltrati = model.ServiziIds.Where(id => salone.Servizi.Any(s => s.ServizioId == id)).ToList();
				if (serviziFiltrati.Count != model.ServiziIds.Count)
				{
					ModelState.AddModelError("", "Alcuni servizi selezionati non sono validi per questo salone.");
					ViewBag.Salone = salone;
					ViewBag.Servizi = salone.Servizi.ToList();
					return View(model);
				}
				model.ServiziIds = serviziFiltrati;
			}

			if (ModelState.IsValid)
			{
				try
				{
					var existingUser = await _userManager.FindByEmailAsync(model.Email);
					ApplicationUser staffUser;

					if (existingUser == null)
					{
						staffUser = new ApplicationUser
						{
							Nome = model.Nome,
							Cognome = model.Cognome,
							Email = model.Email,
							PhoneNumber = model.PhoneNumber,
							UserName = model.Email,
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
							return View(model);
						}

						await _userManager.AddToRoleAsync(staffUser, "User");

						// TODO: Invia email con credenziali temporanee
					}
					else
					{
						// Verifica che l'utente non sia già dipendente di questo salone
						var existingDipendente = await _context.Dipendente
							.FirstOrDefaultAsync(d => d.ApplicationUserId == existingUser.Id && d.SaloneId == saloneId);

						if (existingDipendente != null)
						{
							ModelState.AddModelError("", "Questo utente è già membro dello staff di questo salone.");
							ViewBag.Salone = salone;
							ViewBag.Servizi = salone.Servizi.ToList();
							return View(model);
						}

						staffUser = existingUser;
					}

					var dipendente = new Dipendente
					{
						DipendenteId = Guid.NewGuid(),
						SaloneId = saloneId,
						ApplicationUserId = staffUser.Id,
						DataCreazione = DateTime.Now,
						ApplicationUser = staffUser,
						Salone = salone
					};

					_context.Dipendente.Add(dipendente);
					await _context.SaveChangesAsync();

					if (model.ServiziIds != null && model.ServiziIds.Any())
					{
						var servizi = _context.Servizio.Where(x => model.ServiziIds.Contains(x.ServizioId)).ToList();
						foreach (var servizioId in model.ServiziIds)
						{
							var dipendenteServizio = new DipendenteServizio
							{
								DipendenteServizioId = Guid.NewGuid(),
								DipendenteId = dipendente.DipendenteId,
								ServizioId = servizioId,
								Dipendente = dipendente,
								Servizio = servizi.FirstOrDefault(x => x.ServizioId == servizioId)
							};
							_context.DipendenteServizio.Add(dipendenteServizio);
						}
						await _context.SaveChangesAsync();
					}

					TempData["Success"] = $"Utente {model.Nome} {model.Cognome} aggiunto con successo al {salone.Nome}!";
					return RedirectToAction("Index", new { saloneId });
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", "Errore durante la creazione: " + ex.Message);
				}
			}

			ViewBag.Salone = salone;
			ViewBag.Servizi = salone.Servizi.ToList();
			return View(model);
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