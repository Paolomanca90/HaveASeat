using HaveASeat.Data;
using HaveASeat.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace HaveASeat.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
		private readonly ApplicationDbContext _context;

		public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
		{
			_logger = logger;
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			try
			{
				// Recupera i saloni più popolari (ordinati per valutazione e numero di recensioni)
				var saloniPopolari = await _context.Salone
					.Include(s => s.SalonePersonalizzazione)
					.Include(s => s.Immagini)
					.Include(s => s.Servizi)
					.Include(s => s.Recensioni)
					.Include(s => s.Dipendenti)
						.ThenInclude(d => d.ApplicationUser)
					.Include(s => s.SaloneAbbonamenti)
						.ThenInclude(sa => sa.Abbonamento)
					.Where(s => s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo)
					.OrderByDescending(s => s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto) : 0)
					.ThenByDescending(s => s.Recensioni.Count)
					.ThenByDescending(s => s.DataCreazione)
					.Take(6)
					.ToListAsync();

				// Mappa i saloni nel formato per la view
				var saloniViewModel = saloniPopolari.Select(s => new
				{
					SaloneId = s.SaloneId,
					Nome = s.Nome,
					Citta = s.Citta,
					Provincia = s.Provincia,
					Indirizzo = s.Indirizzo,
					Telefono = s.Telefono,
					CoverImageUrl = s.Immagini?.FirstOrDefault(i => i.IsCover)?.Percorso,
					LogoUrl = s.SalonePersonalizzazione?.LogoUrl,
					Slogan = s.SalonePersonalizzazione?.Slogan,
					MediaVoti = s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto) : 0,
					NumeroRecensioni = s.Recensioni.Count,
					NumeroServizi = s.Servizi.Count,
					PrezzoMinimo = s.Servizi.Any() ? s.Servizi.Min(serv => serv.PrezzoEffettivo) : 0,
					PrezzoMassimo = s.Servizi.Any() ? s.Servizi.Max(serv => serv.PrezzoEffettivo) : 0,
					ServiziPopolari = s.Servizi.OrderBy(serv => serv.Nome).Take(3).Select(serv => serv.Nome).ToList(),
					HasPromozioni = s.Servizi.Any(serv => serv.IsPromotion && serv.DataFinePromozione > DateTime.Now),
					PersonalizzazioneColori = new
					{
						ColorePrimario = s.SalonePersonalizzazione?.ColorePrimario ?? "#7c3aed",
						ColoreSecondario = s.SalonePersonalizzazione?.ColoreSecondario ?? "#ec4899",
						ColoreAccento = s.SalonePersonalizzazione?.ColoreAccento ?? "#f59e0b"
					},
					IsPremium = s.SaloneAbbonamenti.Any(sa =>
						sa.Abbonamento.Nome.Contains("Pro") || sa.Abbonamento.Nome.Contains("Business")),
					NumeroDipendenti = s.Dipendenti.Count,
					VotiDisplay = s.Recensioni.Any() ? s.Recensioni.Average(r => r.Voto).ToString("F1") : "Nuovo",
					PrezzoRange = s.Servizi.Any() ?
						(s.Servizi.Min(serv => serv.PrezzoEffettivo) == s.Servizi.Max(serv => serv.PrezzoEffettivo) ?
							$"€{s.Servizi.Min(serv => serv.PrezzoEffettivo):F0}" :
							$"€{s.Servizi.Min(serv => serv.PrezzoEffettivo):F0}-{s.Servizi.Max(serv => serv.PrezzoEffettivo):F0}") :
						"Su richiesta"
				}).ToList();

				ViewBag.SaloniPopolari = saloniViewModel;

				// Statistiche per la home
				var totaleSaloni = await _context.Salone
					.Where(s => s.Stato == HaveASeat.Utilities.Enum.Stato.Attivo)
					.CountAsync();

				var totalePrenotazioni = await _context.Appuntamento
					.Where(a => a.Stato == HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato)
					.CountAsync();

				var totaleRecensioni = await _context.Recensione.CountAsync();

				ViewBag.TotaleSaloni = totaleSaloni;
				ViewBag.TotalePrenotazioni = totalePrenotazioni;
				ViewBag.TotaleRecensioni = totaleRecensioni;

				// Servizi più richiesti
				var serviziPopolari = await _context.Servizio
					.Include(s => s.Salone)
					.Where(s => s.Salone.Stato == HaveASeat.Utilities.Enum.Stato.Attivo)
					.GroupBy(s => s.Nome.ToLower())
					.Select(g => new { Nome = g.Key, Conteggio = g.Count() })
					.OrderByDescending(x => x.Conteggio)
					.Take(8)
					.ToListAsync();

				ViewBag.ServiziPopolari = serviziPopolari.Select(s => new {
					Nome = s.Nome,
					Conteggio = s.Conteggio
				}).ToList();

				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore nel caricamento della homepage");

				// Fallback: mostra view vuota
				ViewBag.SaloniPopolari = new List<object>();
				ViewBag.TotaleSaloni = 0;
				ViewBag.TotalePrenotazioni = 0;
				ViewBag.TotaleRecensioni = 0;
				ViewBag.ServiziPopolari = new List<object>();

				return View();
			}
		}

		public IActionResult ForPartner()
        {
            return View();
        }

        [HttpPost]
		public IActionResult SelectPlan(string id)
		{
			TempData["SelectedPianoId"] = id;
            var piano = _context.Abbonamento.Find(Guid.Parse(id));
			return Json(new { success = true, redirectUrl = "/Auth/NewPartner", selectedPlan = piano }); 
		}

		public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
