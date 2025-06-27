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
	public class PersonalizzazioneController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _environment;

		public PersonalizzazioneController(ApplicationDbContext context, IWebHostEnvironment environment)
		{
			_context = context;
			_environment = environment;
		}

		// GET: Personalizzazione/GetCustomization
		[HttpGet]
		public async Task<IActionResult> GetCustomization(Guid saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var salone = await _context.Salone
				.Include(s => s.SalonePersonalizzazione)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Salone non trovato" });
			}

			var personalizzazione = salone.SalonePersonalizzazione ?? new SalonePersonalizzazione
			{
				SaloneId = saloneId,
				TemaColore = "elegante",
				ColorePrimario = "#7c3aed",
				ColoreSecondario = "#ec4899",
				ColoreAccento = "#f59e0b",
				LayoutTipo = "moderno",
				MostraGalleria = true,
				MostraTeam = true,
				MostraServizi = true,
				MostraRecensioni = true,
				MostraContatti = true
			};

			return Json(new
			{
				success = true,
				personalizzazione = new
				{
					temaColore = personalizzazione.TemaColore,
					colorePrimario = personalizzazione.ColorePrimario,
					coloreSecondario = personalizzazione.ColoreSecondario,
					coloreAccento = personalizzazione.ColoreAccento,
					layoutTipo = personalizzazione.LayoutTipo,
					logoUrl = personalizzazione.LogoUrl,
					slogan = personalizzazione.Slogan,
					instagramUrl = personalizzazione.InstagramUrl,
					facebookUrl = personalizzazione.FacebookUrl,
					tiktokUrl = personalizzazione.TiktokUrl,
					mostraGalleria = personalizzazione.MostraGalleria,
					mostraTeam = personalizzazione.MostraTeam,
					mostraServizi = personalizzazione.MostraServizi,
					mostraRecensioni = personalizzazione.MostraRecensioni,
					mostraContatti = personalizzazione.MostraContatti
				}
			});
		}

		// POST: Personalizzazione/SaveCustomization
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SaveCustomization([FromBody] SaveCustomizationDto dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var salone = await _context.Salone
				.Include(s => s.SalonePersonalizzazione)
				.FirstOrDefaultAsync(s => s.SaloneId == dto.SaloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Salone non trovato" });
			}

			try
			{
				var personalizzazione = salone.SalonePersonalizzazione;
				if (personalizzazione == null)
				{
					personalizzazione = new SalonePersonalizzazione
					{
						SalonePersonalizzazioneId = Guid.NewGuid(),
						SaloneId = dto.SaloneId,
						DataCreazione = DateTime.Now,
						Salone = salone
					};
					_context.SalonePersonalizzazione.Add(personalizzazione);
				}

				// Aggiorna i valori
				personalizzazione.TemaColore = dto.TemaColore ?? personalizzazione.TemaColore;
				personalizzazione.ColorePrimario = dto.ColorePrimario ?? personalizzazione.ColorePrimario;
				personalizzazione.ColoreSecondario = dto.ColoreSecondario ?? personalizzazione.ColoreSecondario;
				personalizzazione.ColoreAccento = dto.ColoreAccento ?? personalizzazione.ColoreAccento;
				personalizzazione.LayoutTipo = dto.LayoutTipo ?? personalizzazione.LayoutTipo;
				personalizzazione.Slogan = dto.Slogan ?? personalizzazione.Slogan;
				personalizzazione.InstagramUrl = dto.InstagramUrl ?? personalizzazione.InstagramUrl;
				personalizzazione.FacebookUrl = dto.FacebookUrl ?? personalizzazione.FacebookUrl;
				personalizzazione.TiktokUrl = dto.TiktokUrl ?? personalizzazione.TiktokUrl;

				if (dto.MostraGalleria.HasValue) personalizzazione.MostraGalleria = dto.MostraGalleria.Value;
				if (dto.MostraTeam.HasValue) personalizzazione.MostraTeam = dto.MostraTeam.Value;
				if (dto.MostraServizi.HasValue) personalizzazione.MostraServizi = dto.MostraServizi.Value;
				if (dto.MostraRecensioni.HasValue) personalizzazione.MostraRecensioni = dto.MostraRecensioni.Value;
				if (dto.MostraContatti.HasValue) personalizzazione.MostraContatti = dto.MostraContatti.Value;

				personalizzazione.DataUltimaModifica = DateTime.Now;

				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Personalizzazione salvata con successo" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Errore durante il salvataggio: " + ex.Message });
			}
		}

		// POST: Personalizzazione/UploadGalleryImage
		[HttpPost]
		public async Task<IActionResult> UploadGalleryImage(Guid saloneId, IFormFile imageFile, bool isCover = false)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var salone = await _context.Salone
				.Include(s => s.Immagini)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Salone non trovato" });
			}

			if (imageFile == null || imageFile.Length == 0)
			{
				return Json(new { success = false, message = "Nessun file selezionato" });
			}

			// Verifica se ha raggiunto il limite di immagini (max 10 per galleria)
			var currentImages = salone.Immagini.Count(i => !i.IsLogo && !i.IsCover);
			if (!isCover && currentImages >= 10)
			{
				return Json(new { success = false, message = "Hai raggiunto il limite massimo di 10 immagini nella galleria" });
			}

			// Verifica tipo file
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
			var extension = Path.GetExtension(imageFile.FileName).ToLower();

			if (!allowedExtensions.Contains(extension))
			{
				return Json(new { success = false, message = "Formato file non supportato. Usa JPG o PNG." });
			}

			// Verifica dimensione (max 5MB)
			if (imageFile.Length > 5 * 1024 * 1024)
			{
				return Json(new { success = false, message = "File troppo grande. Massimo 5MB." });
			}

			try
			{
				// Crea directory se non esiste
				var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "gallery");
				Directory.CreateDirectory(uploadsFolder);

				// Se è cover, elimina la cover precedente
				if (isCover)
				{
					var oldCover = salone.Immagini.FirstOrDefault(i => i.IsCover);
					if (oldCover != null)
					{
						var oldPath = Path.Combine(_environment.WebRootPath, oldCover.Percorso.TrimStart('/'));
						if (System.IO.File.Exists(oldPath))
						{
							System.IO.File.Delete(oldPath);
						}
						_context.Immagine.Remove(oldCover);
					}
				}

				// Salva nuova immagine
				var uniqueFileName = $"{saloneId}_{Guid.NewGuid()}{extension}";
				var filePath = Path.Combine(uploadsFolder, uniqueFileName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await imageFile.CopyToAsync(stream);
				}

				// Aggiungi al database
				var immagine = new Immagine
				{
					ImmagineId = Guid.NewGuid(),
					SaloneId = saloneId,
					Salone = salone,
					Percorso = $"/uploads/gallery/{uniqueFileName}",
					IsCover = isCover,
					IsLogo = false, // IMPORTANTE: Assicurati che non sia logo
					DataCreazione = DateTime.Now
				};

				_context.Immagine.Add(immagine);
				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					message = isCover ? "Foto di copertina aggiornata" : "Immagine aggiunta alla galleria",
					imageUrl = immagine.Percorso,
					imageId = immagine.ImmagineId,
					isCover = isCover
				});
			}
			catch (Exception ex)
			{
				// Log dell'errore per debugging
				Console.WriteLine($"Errore upload immagine: {ex.Message}");
				return Json(new { success = false, message = "Errore durante il caricamento: " + ex.Message });
			}
		}

		[HttpPost]
		public async Task<IActionResult> UploadLogo(Guid saloneId, IFormFile logoFile)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var salone = await _context.Salone
				.Include(s => s.SalonePersonalizzazione)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Salone non trovato" });
			}

			if (logoFile == null || logoFile.Length == 0)
			{
				return Json(new { success = false, message = "Nessun file selezionato" });
			}

			// Verifica tipo file
			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".svg" };
			var extension = Path.GetExtension(logoFile.FileName).ToLower();

			if (!allowedExtensions.Contains(extension))
			{
				return Json(new { success = false, message = "Formato file non supportato. Usa JPG, PNG o SVG." });
			}

			// Verifica dimensione (max 2MB)
			if (logoFile.Length > 2 * 1024 * 1024)
			{
				return Json(new { success = false, message = "File troppo grande. Massimo 2MB." });
			}

			try
			{
				// Crea directory se non esiste
				var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "logos");
				Directory.CreateDirectory(uploadsFolder);

				// Elimina logo precedente se esiste
				var personalizzazione = salone.SalonePersonalizzazione;
				if (personalizzazione?.LogoUrl != null)
				{
					var oldLogoPath = Path.Combine(_environment.WebRootPath, personalizzazione.LogoUrl.TrimStart('/'));
					if (System.IO.File.Exists(oldLogoPath))
					{
						System.IO.File.Delete(oldLogoPath);
					}
				}

				// Salva nuovo logo
				var uniqueFileName = $"{saloneId}_{Guid.NewGuid()}{extension}";
				var filePath = Path.Combine(uploadsFolder, uniqueFileName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await logoFile.CopyToAsync(stream);
				}

				// Aggiorna o crea personalizzazione
				if (personalizzazione == null)
				{
					personalizzazione = new SalonePersonalizzazione
					{
						SalonePersonalizzazioneId = Guid.NewGuid(),
						SaloneId = saloneId,
						Salone = salone,
						DataCreazione = DateTime.Now
					};
					_context.SalonePersonalizzazione.Add(personalizzazione);
				}

				personalizzazione.LogoUrl = $"/uploads/logos/{uniqueFileName}";
				personalizzazione.DataUltimaModifica = DateTime.Now;

				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					message = "Logo caricato con successo",
					logoUrl = personalizzazione.LogoUrl
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Errore upload logo: {ex.Message}");
				return Json(new { success = false, message = "Errore durante il caricamento: " + ex.Message });
			}
		}

		// DELETE: Personalizzazione/DeleteImage
		[HttpDelete]
		public async Task<IActionResult> DeleteImage(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var immagine = await _context.Immagine
				.Include(i => i.Salone)
				.FirstOrDefaultAsync(i => i.ImmagineId == id && i.Salone.ApplicationUserId == userId);

			if (immagine == null)
			{
				return Json(new { success = false, message = "Immagine non trovata" });
			}

			try
			{
				// Elimina file fisico
				var filePath = Path.Combine(_environment.WebRootPath, immagine.Percorso.TrimStart('/'));
				if (System.IO.File.Exists(filePath))
				{
					System.IO.File.Delete(filePath);
				}

				// Elimina dal database
				_context.Immagine.Remove(immagine);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Immagine eliminata con successo" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Errore durante l'eliminazione: " + ex.Message });
			}
		}

		// GET: Personalizzazione/GetGalleryImages
		[HttpGet]
		public async Task<IActionResult> GetGalleryImages(Guid saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var salone = await _context.Salone
				.Include(s => s.Immagini)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Salone non trovato" });
			}

			var immagini = salone.Immagini
				.Where(i => !i.IsLogo)
				.OrderByDescending(i => i.IsCover)
				.ThenByDescending(i => i.DataCreazione)
				.Select(i => new
				{
					id = i.ImmagineId,
					url = i.Percorso,
					isCover = i.IsCover,
					isLogo = i.IsLogo,
					dataCreazione = i.DataCreazione
				})
				.ToList();

			return Json(new { success = true, immagini });
		}

		// Delete: Personalizzazione/DeleteLogo
		[HttpDelete]
		public async Task<IActionResult> DeleteLogo(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var salone = await _context.Salone
				.Include(s => s.SalonePersonalizzazione)
				.FirstOrDefaultAsync(s => s.SaloneId == id && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Salone non trovato" });
			}

			var logoUrl = salone.SalonePersonalizzazione?.LogoUrl;

			if (logoUrl != null)
			{
				var oldLogoPath = Path.Combine(_environment.WebRootPath, salone.SalonePersonalizzazione.LogoUrl.TrimStart('/'));
				if (System.IO.File.Exists(oldLogoPath))
				{
					System.IO.File.Delete(oldLogoPath);
				}
			}

			salone.SalonePersonalizzazione.LogoUrl = null;
			salone.SalonePersonalizzazione.DataUltimaModifica = DateTime.Now;

			await _context.SaveChangesAsync();

			return Json(new { success = true });
		}

		// GET: Personalizzazione/Preview
		public async Task<IActionResult> Preview(Guid saloneId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var salone = await _context.Salone
				.Include(s => s.SalonePersonalizzazione)
				.Include(s => s.Immagini)
				.Include(s => s.Servizi)
				.Include(s => s.Dipendenti)
					.ThenInclude(d => d.ApplicationUser)
				.Include(s => s.Recensioni)
					.ThenInclude(r => r.ApplicationUser)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return NotFound();
			}

			return View("Preview", salone);
		}
	}
}