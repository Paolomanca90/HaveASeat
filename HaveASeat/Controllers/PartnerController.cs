using HaveASeat.Data;
using HaveASeat.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using System.Security.Claims;
using HaveASeat.Utilities.Enum;
using Microsoft.EntityFrameworkCore;
using HaveASeat.ViewModels;
using System.Text;
using HaveASeat.Utilities.Constants;
using HaveASeat.Services;

namespace HaveASeat.Controllers
{
	[Authorize(Roles = "Partner")]
	public class PartnerController : Controller
	{
		private readonly ApplicationDbContext _context;
        private readonly ILogger<PartnerController> _logger;

        public PartnerController(ApplicationDbContext context, ILogger<PartnerController> logger)
		{
			_context = context;
            _logger = logger;
        }
        #region Index
        public async Task<IActionResult> Index(Guid? saloneId = null, string periodo = "settimana")		
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Auth");
			}
			var viewModel = new DashboardViewModel
			{
				PeriodoSelezionato = periodo,
				SaloniUtente = new List<Salone>(),
				Stats = new DashboardStats(),
				ChartData = new ChartData(),
				TopServizi = new List<TopServizioViewModel>(),
				AppuntamentiOggi = new List<AppuntamentoViewModel>()
			};
			// Recupera tutti i saloni dell'utente
			viewModel.SaloniUtente = await _context.Salone
				.Include(x => x.SaloneAbbonamenti)
				.Where(s => s.ApplicationUserId == userId && s.Stato == Stato.Attivo)
				.ToListAsync();
			// Se non ci sono saloni attivi, mostra la view per creare il primo salone
			if (!viewModel.SaloniUtente.Any())
			{
				var checkPiano = _context.PianoSelezionato.FirstOrDefault(x => x.ApplicationUserId == userId && x.Confermato == false);
				if (checkPiano != null)
					TempData["SelectedPianoId"] = checkPiano.ApplicationUserId;

				ViewBag.NomeUtente = _context.Users.Find(userId)?.Nome;
				return View(viewModel);
			}
			// Determina quale salone visualizzare
			if (saloneId.HasValue && viewModel.SaloniUtente.Any(s => s.SaloneId == saloneId.Value))
			{
				viewModel.SelectedSaloneId = saloneId.Value;
			}
			else
			{
				viewModel.SelectedSaloneId = viewModel.SaloniUtente.First().SaloneId;
			}

			viewModel.SaloneCorrente = viewModel.SaloniUtente.First(s => s.SaloneId == viewModel.SelectedSaloneId);
            ViewBag.HasMultipleSedi = viewModel.SaloniUtente.Count > 1;
			ViewBag.Saloni = viewModel.SaloniUtente;
            var abbonamentoStandard = viewModel.SaloneCorrente.SaloneAbbonamenti.Any(x => x.AbbonamentoId == SubscriptionsConstants.Basic);
			if (abbonamentoStandard)
				ViewBag.Basic = true;
			// Calcola le date del periodo
			var oggi = DateTime.Today;
			switch (periodo.ToLower())
			{
				case "giorno":
					viewModel.DataInizio = oggi;
					viewModel.DataFine = oggi.AddDays(1).AddSeconds(-1);
					break;
				case "settimana":
					var inizioSettimana = oggi.AddDays(-(int)oggi.DayOfWeek + (int)DayOfWeek.Monday);
					viewModel.DataInizio = inizioSettimana;
					viewModel.DataFine = inizioSettimana.AddDays(7).AddSeconds(-1);
					break;
				case "mese":
					viewModel.DataInizio = new DateTime(oggi.Year, oggi.Month, 1);
					viewModel.DataFine = viewModel.DataInizio.AddMonths(1).AddSeconds(-1);
					break;
				default:
					viewModel.DataInizio = oggi.AddDays(-7);
					viewModel.DataFine = oggi.AddDays(1).AddSeconds(-1);
					break;
			}
			// Carica le statistiche
			await LoadDashboardStats(viewModel);
			await LoadChartData(viewModel);
			await LoadTopServizi(viewModel);
			await LoadAppuntamentiOggi(viewModel);

			ViewBag.NomeUtente = _context.Users.Find(userId)?.Nome;
			return View(viewModel);
		}
        #endregion

        #region LoadDashboardStats
        private async Task LoadDashboardStats(DashboardViewModel viewModel)
		{
			var oggi = DateTime.Today;
			var ieri = oggi.AddDays(-1);
			var inizioSettimana = oggi.AddDays(-(int)oggi.DayOfWeek + (int)DayOfWeek.Monday);

			// Query base per gli appuntamenti del salone selezionato
			var appuntamentiQuery = _context.Appuntamento
				.Include(a => a.Slot)
				.Include(a => a.ApplicationUser)
				.Where(a => a.SaloneId == viewModel.SelectedSaloneId && a.Stato == StatoAppuntamento.Prenotato);

			// Prenotazioni oggi
			var prenotazioniOggi = await appuntamentiQuery
				.Where(a => a.Data.Date == oggi)
				.CountAsync();

			var prenotazioniIeri = await appuntamentiQuery
				.Where(a => a.Data.Date == ieri)
				.CountAsync();

			viewModel.Stats.PrenotazioniOggi = prenotazioniOggi;
			viewModel.Stats.PercentualePrenotazioni = prenotazioniIeri > 0
				? Math.Round(((decimal)(prenotazioniOggi - prenotazioniIeri) / prenotazioniIeri) * 100, 1)
				: 0;
			viewModel.Stats.IsPrenotazioniPositive = viewModel.Stats.PercentualePrenotazioni >= 0;

			// Nuovi clienti questa settimana
			var clientiSettimana = await appuntamentiQuery
				.Where(a => a.Data >= inizioSettimana)
				.Select(a => a.ApplicationUserId)
				.Distinct()
				.CountAsync();

			var clientiSettimanaScorsa = await appuntamentiQuery
				.Where(a => a.Data >= inizioSettimana.AddDays(-7) && a.Data < inizioSettimana)
				.Select(a => a.ApplicationUserId)
				.Distinct()
				.CountAsync();

			viewModel.Stats.NuoviClienti = clientiSettimana;
			viewModel.Stats.PercentualeNuoviClienti = clientiSettimanaScorsa > 0
				? Math.Round(((decimal)(clientiSettimana - clientiSettimanaScorsa) / clientiSettimanaScorsa) * 100, 1)
				: 0;
			viewModel.Stats.IsNuoviClientiPositive = viewModel.Stats.PercentualeNuoviClienti >= 0;

			// Incasso giornaliero
			var serviziOggi = await _context.Appuntamento
				.Include(a => a.Salone)
				.ThenInclude(s => s.Servizi)
				.Where(a => a.SaloneId == viewModel.SelectedSaloneId &&
						   a.Data.Date == oggi &&
						   a.Stato == StatoAppuntamento.Prenotato)
				.ToListAsync();

			var incassoOggi = 0m;
			foreach (var app in serviziOggi)
			{
				// Assumendo che ci sia una relazione tra appuntamento e servizio
				// Per ora uso un prezzo fisso, ma dovrebbe essere preso dal servizio
				incassoOggi += 50; // Placeholder - sostituire con il prezzo reale del servizio
			}

			var incassoIeri = 0m; // Calcolare allo stesso modo per ieri

			viewModel.Stats.IncassoGiornaliero = incassoOggi;
			viewModel.Stats.PercentualeIncasso = incassoIeri > 0
				? Math.Round(((incassoOggi - incassoIeri) / incassoIeri) * 100, 1)
				: 0;
			viewModel.Stats.IsIncassoPositive = viewModel.Stats.PercentualeIncasso >= 0;

			// Servizi completati
			var serviziCompletatiOggi = await appuntamentiQuery
				.Where(a => a.Data.Date == oggi && a.Stato == StatoAppuntamento.Prenotato)
				.CountAsync();

			var serviziCompletatiIeri = await appuntamentiQuery
				.Where(a => a.Data.Date == ieri && a.Stato == StatoAppuntamento.Prenotato)
				.CountAsync();

			viewModel.Stats.ServiziCompletati = serviziCompletatiOggi;
			viewModel.Stats.PercentualeServizi = serviziCompletatiIeri > 0
				? Math.Round(((decimal)(serviziCompletatiOggi - serviziCompletatiIeri) / serviziCompletatiIeri) * 100, 1)
				: 0;
			viewModel.Stats.IsServiziPositive = viewModel.Stats.PercentualeServizi >= 0;
            
			var promozioniAttive = await _context.Servizio
				.Where(s => s.SaloneId == viewModel.SelectedSaloneId &&
                s.IsPromotion &&
                s.DataFinePromozione > DateTime.Now)
				.CountAsync();

            // Promozioni attive ieri (per il confronto)
            var promozioniIeri = await _context.Servizio
                .Where(s => s.SaloneId == viewModel.SelectedSaloneId &&
                            s.IsPromotion &&
                            s.DataInizioPromozione <= ieri &&
                            s.DataFinePromozione > ieri)
                .CountAsync();

            viewModel.Stats.PromozioniAttive = promozioniAttive;
            viewModel.Stats.PercentualePromozioni = promozioniIeri > 0
                ? Math.Round(((decimal)(promozioniAttive - promozioniIeri) / promozioniIeri) * 100, 1)
                : (promozioniAttive > 0 ? 100 : 0);
            viewModel.Stats.IsPromozioniPositive = viewModel.Stats.PercentualePromozioni >= 0;
            // Numero di servizi attivi
            var numeroServizi = await _context.Servizio
                .Where(s => s.SaloneId == viewModel.SelectedSaloneId)
                .CountAsync();

            viewModel.Stats.NumeroServizi = numeroServizi;

            // Numero di dipendenti
            var numeroDipendenti = await _context.Dipendente
                .Where(d => d.SaloneId == viewModel.SelectedSaloneId)
                .CountAsync();

            viewModel.Stats.NumeroDipendenti = numeroDipendenti;
        }
        #endregion

        #region LoadChartData
        private async Task LoadChartData(DashboardViewModel viewModel)
		{
			var labels = new List<string>();
			var incassi = new List<decimal>();
			var prenotazioni = new List<int>();

			// Genera i dati in base al periodo selezionato
			if (viewModel.PeriodoSelezionato == "giorno")
			{
				// Dati orari per oggi
				for (int ora = 8; ora <= 20; ora++)
				{
					labels.Add($"{ora}:00");

					var inizioOra = DateTime.Today.AddHours(ora);
					var fineOra = inizioOra.AddHours(1);

					var appuntamentiOra = await _context.Appuntamento
						.Where(a => a.SaloneId == viewModel.SelectedSaloneId &&
								   a.Data >= inizioOra && a.Data < fineOra &&
								   a.Stato != StatoAppuntamento.Annullato)
						.CountAsync();

					prenotazioni.Add(appuntamentiOra);
					incassi.Add(appuntamentiOra * 50); // Placeholder per incasso
				}
			}
			else if (viewModel.PeriodoSelezionato == "settimana")
			{
				// Dati giornalieri per la settimana
				var inizioSettimana = viewModel.DataInizio;
				for (int giorno = 0; giorno < 7; giorno++)
				{
					var data = inizioSettimana.AddDays(giorno);
					labels.Add(data.ToString("ddd"));

					var appuntamentiGiorno = await _context.Appuntamento
						.Where(a => a.SaloneId == viewModel.SelectedSaloneId &&
								   a.Data.Date == data.Date &&
								   a.Stato != StatoAppuntamento.Annullato)
						.CountAsync();

					prenotazioni.Add(appuntamentiGiorno);
					incassi.Add(appuntamentiGiorno * 50); // Placeholder per incasso
				}
			}
			else // mese
			{
				// Dati settimanali per il mese
				var inizioMese = viewModel.DataInizio;
				var settimane = 4;
				for (int settimana = 0; settimana < settimane; settimana++)
				{
					var inizioSettimana = inizioMese.AddDays(settimana * 7);
					var fineSettimana = inizioSettimana.AddDays(7);
					labels.Add($"Sett. {settimana + 1}");

					var appuntamentiSettimana = await _context.Appuntamento
						.Where(a => a.SaloneId == viewModel.SelectedSaloneId &&
								   a.Data >= inizioSettimana && a.Data < fineSettimana &&
								   a.Stato != StatoAppuntamento.Annullato)
						.CountAsync();

					prenotazioni.Add(appuntamentiSettimana);
					incassi.Add(appuntamentiSettimana * 50); // Placeholder per incasso
				}
			}

			viewModel.ChartData.Labels = labels;
			viewModel.ChartData.Incassi = incassi;
			viewModel.ChartData.Prenotazioni = prenotazioni;
		}
        #endregion

        #region LoadTopServizi
        private async Task LoadTopServizi(DashboardViewModel viewModel)
		{
			// Simulazione dati - in produzione questi dovrebbero venire dal database
			viewModel.TopServizi = new List<TopServizioViewModel>
			{
				new TopServizioViewModel { Nome = "Taglio e Piega", NumeroPrenotazioni = 34, IncassoTotale = 1360 },
				new TopServizioViewModel { Nome = "Colorazione Capelli", NumeroPrenotazioni = 28, IncassoTotale = 2240 },
				new TopServizioViewModel { Nome = "Manicure", NumeroPrenotazioni = 21, IncassoTotale = 840 },
				new TopServizioViewModel { Nome = "Trattamento Viso", NumeroPrenotazioni = 15, IncassoTotale = 1050 },
				new TopServizioViewModel { Nome = "Massaggio Rilassante", NumeroPrenotazioni = 12, IncassoTotale = 960 }
			};
		}
        #endregion

        #region LoadAppuntamentiOggi
        private async Task LoadAppuntamentiOggi(DashboardViewModel viewModel)
		{
			var oggi = DateTime.Today;

			var appuntamenti = await _context.Appuntamento
				.Include(a => a.ApplicationUser)
				.Include(a => a.Dipendente)
					.ThenInclude(d => d.ApplicationUser)
				.Include(a => a.Slot)
				.Include(a => a.Servizio)
				.Where(a => a.SaloneId == viewModel.SelectedSaloneId && a.Data.Date == oggi)
                .OrderBy(a => a.Slot.OraInizio)
                .ToListAsync();

			viewModel.AppuntamentiOggi = appuntamenti.Select(a => new AppuntamentoViewModel
			{
                AppuntamentoId = a.AppuntamentoId,
                NomeCliente = $"{a.ApplicationUser.Nome} {a.ApplicationUser.Cognome}",
                TelefonoCliente = a.ApplicationUser.PhoneNumber ?? "",
                NomeServizio = a.Servizio?.Nome ?? "Servizio non specificato",
                NomeDipendente = a.Dipendente != null
            ? $"{a.Dipendente.ApplicationUser.Nome} {a.Dipendente.ApplicationUser.Cognome}"
            : "Non assegnato",
                OrarioInizio = a.OraInizio.ToString("HH:mm"),
                OrarioFine = a.OraFine.ToString("HH:mm"),
                Prezzo = a.Servizio?.PrezzoEffettivo ?? 0,
                Stato = a.Stato.ToString(),
                ClasseStato = a.Stato switch
                {
                    StatoAppuntamento.Prenotato => "status-completed",
                    StatoAppuntamento.Annullato => "status-cancelled",
                    _ => "status-pending"
                }
            }).ToList();
        }
        #endregion

        #region GetDashboardData
        [HttpGet]
		public async Task<IActionResult> GetDashboardData(Guid saloneId, string periodo)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return Unauthorized();
			}

			// Verifica che il salone appartenga all'utente
			var salone = await _context.Salone
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return NotFound();
			}

			var viewModel = new DashboardViewModel
			{
				SelectedSaloneId = saloneId,
				PeriodoSelezionato = periodo
			};
			// Calcola le date del periodo
			var oggi = DateTime.Today;
			switch (periodo.ToLower())
			{
				case "giorno":
					viewModel.DataInizio = oggi;
					viewModel.DataFine = oggi.AddDays(1).AddSeconds(-1);
					break;
				case "settimana":
					var inizioSettimana = oggi.AddDays(-(int)oggi.DayOfWeek + (int)DayOfWeek.Monday);
					viewModel.DataInizio = inizioSettimana;
					viewModel.DataFine = inizioSettimana.AddDays(7).AddSeconds(-1);
					break;
				case "mese":
					viewModel.DataInizio = new DateTime(oggi.Year, oggi.Month, 1);
					viewModel.DataFine = viewModel.DataInizio.AddMonths(1).AddSeconds(-1);
					break;
				default:
					viewModel.DataInizio = oggi.AddDays(-7);
					viewModel.DataFine = oggi.AddDays(1).AddSeconds(-1);
					break;
			}
			// Carica le statistiche
			await LoadDashboardStats(viewModel);
			await LoadChartData(viewModel);
			await LoadTopServizi(viewModel);
			await LoadAppuntamentiOggi(viewModel);
			return Json(new
			{
				success = true,
				stats = viewModel.Stats,
				chartData = viewModel.ChartData,
				topServizi = viewModel.TopServizi,
				appuntamenti = viewModel.AppuntamentiOggi
			});
			//         var checkPiano = _context.PianoSelezionato.FirstOrDefault(x => x.ApplicationUserId == userId && x.Confermato == false);
			//         if (checkPiano != null)
			//             TempData["SelectedPianoId"] = checkPiano.ApplicationUserId;
			//ViewBag.NomeUtente = _context.Users.Find(userId)?.Nome;
			//         return View();
		}
        #endregion

        #region ExportDashboard
        [HttpGet]
        public async Task<IActionResult> ExportDashboard(Guid saloneId, string periodo, string formato = "csv")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verifica che il salone appartenga all'utente
            var salone = await _context.Salone
                .Include(s => s.Dipendenti)
                .Include(s => s.Servizi)
                .FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

            if (salone == null)
            {
                return NotFound();
            }

            // Prepara i dati per l'export
            var viewModel = new DashboardViewModel
            {
                SelectedSaloneId = saloneId,
                PeriodoSelezionato = periodo
            };

            // Calcola date base sul periodo
            var oggi = DateTime.Today;
            switch (periodo.ToLower())
            {
                case "giorno":
                    viewModel.DataInizio = oggi;
                    viewModel.DataFine = oggi.AddDays(1).AddSeconds(-1);
                    break;
                case "settimana":
                    var inizioSettimana = oggi.AddDays(-(int)oggi.DayOfWeek + (int)DayOfWeek.Monday);
                    viewModel.DataInizio = inizioSettimana;
                    viewModel.DataFine = inizioSettimana.AddDays(7).AddSeconds(-1);
                    break;
                case "mese":
                    viewModel.DataInizio = new DateTime(oggi.Year, oggi.Month, 1);
                    viewModel.DataFine = viewModel.DataInizio.AddMonths(1).AddSeconds(-1);
                    break;
            }

            await LoadDashboardStats(viewModel);
            await LoadTopServizi(viewModel);

            // Recupera gli appuntamenti per l'export
            var appuntamenti = await _context.Appuntamento
                .Include(a => a.ApplicationUser)
                .Include(a => a.Dipendente)
                    .ThenInclude(d => d.ApplicationUser)
                .Include(a => a.Servizio)
                .Where(a => a.SaloneId == saloneId &&
                           a.Data >= viewModel.DataInizio &&
                           a.Data <= viewModel.DataFine)
                .OrderBy(a => a.Data)
                .ToListAsync();

            // Prepara i dati per l'export service
            var exportData = new DashboardExportData
            {
                NomeSalone = salone.Nome,
                Periodo = periodo,
                DataExport = DateTime.Now,
                Stats = viewModel.Stats,
                TopServizi = viewModel.TopServizi,
                NumeroDipendenti = salone.Dipendenti.Count,
                NumeroServizi = salone.Servizi.Count,
                PromozioniAttive = salone.Servizi.Count(s => s.IsPromotion && s.DataFinePromozione > DateTime.Now),
                Appuntamenti = appuntamenti.Select(a => new AppuntamentoExportViewModel
                {
                    Data = a.Data,
                    OraInizio = a.OraInizio.ToString("HH:mm"),
                    OraFine = a.OraFine.ToString("HH:mm"),
                    NomeCliente = $"{a.ApplicationUser.Nome} {a.ApplicationUser.Cognome}",
                    Servizio = a.Servizio?.Nome ?? "N/A",
                    Dipendente = a.Dipendente != null ?
                        $"{a.Dipendente.ApplicationUser.Nome} {a.Dipendente.ApplicationUser.Cognome}" : "N/A",
                    Stato = a.Stato.ToString(),
                    Prezzo = a.Servizio?.PrezzoEffettivo ?? 0
                }).ToList()
            };

            // Usa il servizio di export per generare il file nel formato richiesto
            var exportService = new ExportService();
            byte[] fileBytes;
            string fileName;
            string contentType;

            switch (formato.ToLower())
            {
                case "excel":
                    fileBytes = exportService.ExportToExcel(exportData);
                    fileName = $"report_{salone.Nome.Replace(" ", "_")}_{periodo}_{DateTime.Now:yyyyMMdd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;
                case "pdf":
                    fileBytes = exportService.ExportToPdf(exportData);
                    fileName = $"report_{salone.Nome.Replace(" ", "_")}_{periodo}_{DateTime.Now:yyyyMMdd}.pdf";
                    contentType = "application/pdf";
                    break;
                case "csv":
                default:
                    fileBytes = exportService.ExportToCsv(exportData);
                    fileName = $"report_{salone.Nome.Replace(" ", "_")}_{periodo}_{DateTime.Now:yyyyMMdd}.csv";
                    contentType = "text/csv";
                    break;
            }

            return File(fileBytes, contentType, fileName);
        }
        #endregion

        #region Appuntamenti
        [HttpGet]
        public async Task<IActionResult> Appuntamenti(Guid? saloneId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var viewModel = new AppuntamentiViewModel
            {
                SaloniUtente = new List<Salone>()
            };

            // Recupera tutti i saloni dell'utente
            viewModel.SaloniUtente = await _context.Salone
                .Where(s => s.ApplicationUserId == userId && s.Stato == Stato.Attivo)
                .OrderBy(s => s.Nome)
                .ToListAsync();

            if (!viewModel.SaloniUtente.Any())
            {
                return RedirectToAction("Sedi");
            }

            // Determina quale salone visualizzare
            if (saloneId.HasValue && viewModel.SaloniUtente.Any(s => s.SaloneId == saloneId.Value))
            {
                viewModel.SelectedSaloneId = saloneId.Value;
            }
            else
            {
                viewModel.SelectedSaloneId = viewModel.SaloniUtente.First().SaloneId;
            }
            ViewBag.SaloneCorrente = viewModel.SaloniUtente;
            ViewBag.Saloni = viewModel.SaloniUtente;
            ViewBag.HasMultipleSedi = viewModel.SaloniUtente.Count > 1;
            ViewBag.NomeUtente = _context.Users.Find(userId)?.Nome;

            return View(viewModel);
        }
        #endregion

        #region GetAppuntamenti
        //[HttpGet]
        //public async Task<IActionResult> GetAppuntamenti(Guid saloneId)
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Json(new { success = false, message = "Non autorizzato" });
        //    }

        //    // Verifica che il salone appartenga all'utente
        //    var salone = await _context.Salone
        //        .FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

        //    if (salone == null)
        //    {
        //        return Json(new { success = false, message = "Salone non trovato" });
        //    }

        //    try
        //    {
        //        // Recupera tutti gli appuntamenti del salone
        //        var appuntamentiQuery = await _context.Appuntamento
        //            .Include(a => a.ApplicationUser)
        //            .Include(a => a.Servizio)
        //            .Include(a => a.Dipendente)
        //                .ThenInclude(d => d.ApplicationUser)
        //            .Include(a => a.Slot)
        //            .Where(a => a.SaloneId == saloneId)
        //            .OrderBy(a => a.Data)
        //            .ThenBy(a => a.Slot.OraInizio)
        //            .ToListAsync();

        //        var appuntamenti = appuntamentiQuery.Select(a => new
        //        {
        //            id = a.AppuntamentoId.ToString(),
        //            data = a.Data.ToString("yyyy-MM-dd"),
        //            orarioInizio = a.OraInizio.ToString("HH:mm"),
        //            orarioFine = a.OraFine.ToString("HH:mm"),
        //            nomeCliente = $"{a.ApplicationUser.Nome} {a.ApplicationUser.Cognome}",
        //            telefonoCliente = a.ApplicationUser.PhoneNumber ?? "",
        //            emailCliente = a.ApplicationUser.Email,
        //            servizioId = a.ServizioId.HasValue ? a.ServizioId.Value.ToString() : "",
        //            nomeServizio = a.Servizio != null ? a.Servizio.Nome : "N/D",
        //            durata = a.Servizio != null ? (int)a.Servizio.Durata : 0,
        //            prezzo = a.Servizio != null ? a.Servizio.PrezzoEffettivo : 0,
        //            dipendenteId = a.DipendenteId.HasValue ? a.DipendenteId.Value.ToString() : "",
        //            nomeDipendente = a.Dipendente != null ?
        //                $"{a.Dipendente.ApplicationUser.Nome} {a.Dipendente.ApplicationUser.Cognome}" : null,
        //            stato = a.Stato.ToString(),
        //            note = a.Note ?? ""
        //        }).ToList();

        //        // Recupera servizi - semplificato
        //        var servizi = await _context.Servizio
        //            .Where(s => s.SaloneId == saloneId)
        //            .OrderBy(s => s.Nome)
        //            .Select(s => new { id = s.ServizioId.ToString(), nome = s.Nome })
        //            .ToListAsync();

        //        // Recupera dipendenti - materializza prima poi trasforma
        //        var dipendentiQuery = await _context.Dipendente
        //            .Include(d => d.ApplicationUser)
        //            .Where(d => d.SaloneId == saloneId)
        //            .ToListAsync();

        //        var dipendenti = dipendentiQuery
        //            .Select(d => new
        //            {
        //                id = d.DipendenteId.ToString(),
        //                nome = $"{d.ApplicationUser.Nome} {d.ApplicationUser.Cognome}"
        //            })
        //            .OrderBy(d => d.nome)
        //            .ToList();

        //        return Json(new
        //        {
        //            success = true,
        //            appuntamenti,
        //            servizi,
        //            dipendenti
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Errore nel recupero degli appuntamenti: " + ex.Message });
        //    }
        //}
        [HttpGet]
        public async Task<IActionResult> GetAppuntamenti(Guid saloneId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Non autorizzato" });
            }

            // Log per debug
            _logger?.LogInformation($"GetAppuntamenti chiamato per salone: {saloneId}, user: {userId}");

            // Verifica che il salone appartenga all'utente
            var salone = await _context.Salone
                .FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

            if (salone == null)
            {
                _logger?.LogWarning($"Salone non trovato: {saloneId} per user: {userId}");
                return Json(new { success = false, message = "Salone non trovato" });
            }

            try
            {
                // Prima verifichiamo quanti appuntamenti ci sono nel DB
                var totalAppuntamenti = await _context.Appuntamento
                    .Where(a => a.SaloneId == saloneId)
                    .CountAsync();

                _logger?.LogInformation($"Totale appuntamenti nel DB per salone {saloneId}: {totalAppuntamenti}");

                // Recupera tutti gli appuntamenti del salone
                var appuntamentiQuery = await _context.Appuntamento
                    .Include(a => a.ApplicationUser)
                    .Include(a => a.Servizio)
                    .Include(a => a.Dipendente)
                        .ThenInclude(d => d.ApplicationUser)
                    .Include(a => a.Slot)
                    .Where(a => a.SaloneId == saloneId)
                    .ToListAsync();

                _logger?.LogInformation($"Appuntamenti recuperati con Include: {appuntamentiQuery.Count}");

                // Debug: stampa i primi 3 appuntamenti
                foreach (var app in appuntamentiQuery.Take(3))
                {
                    _logger?.LogInformation($"Appuntamento: ID={app.AppuntamentoId}, Data={app.Data}, Cliente={app.ApplicationUser?.Nome}, Slot={app.Slot?.OraInizio}");
                }

                var appuntamenti = appuntamentiQuery.Select(a => new
                {
                    id = a.AppuntamentoId.ToString(),
                    data = a.Data.ToString("yyyy-MM-dd"),
                    orarioInizio = a.OraInizio.ToString("HH:mm"),
                    orarioFine = a.OraFine.ToString("HH:mm"),
                    nomeCliente = a.ApplicationUser != null ?
                        $"{a.ApplicationUser.Nome ?? ""} {a.ApplicationUser.Cognome ?? ""}".Trim() : "Cliente N/D",
                    telefonoCliente = a.ApplicationUser?.PhoneNumber ?? "",
                    emailCliente = a.ApplicationUser?.Email ?? "",
                    servizioId = a.ServizioId?.ToString() ?? "",
                    nomeServizio = a.Servizio?.Nome ?? "N/D",
                    durata = a.Servizio != null ? (int)a.Servizio.Durata : 30,
                    prezzo = a.Servizio?.PrezzoEffettivo ?? 0,
                    dipendenteId = a.DipendenteId?.ToString() ?? "",
                    nomeDipendente = a.Dipendente?.ApplicationUser != null ?
                        $"{a.Dipendente.ApplicationUser.Nome ?? ""} {a.Dipendente.ApplicationUser.Cognome ?? ""}".Trim() : "",
                    stato = a.Stato.ToString(),
                    note = a.Note ?? ""
                }).ToList();

                _logger?.LogInformation($"Appuntamenti trasformati: {appuntamenti.Count}");

                // Recupera servizi
                var servizi = await _context.Servizio
                    .Where(s => s.SaloneId == saloneId)
                    .OrderBy(s => s.Nome)
                    .Select(s => new { id = s.ServizioId.ToString(), nome = s.Nome })
                    .ToListAsync();

                _logger?.LogInformation($"Servizi trovati: {servizi.Count}");

                // Recupera dipendenti
                var dipendentiQuery = await _context.Dipendente
                    .Include(d => d.ApplicationUser)
                    .Where(d => d.SaloneId == saloneId)
                    .ToListAsync();

                var dipendenti = dipendentiQuery
                    .Select(d => new
                    {
                        id = d.DipendenteId.ToString(),
                        nome = d.ApplicationUser != null ?
                            $"{d.ApplicationUser.Nome ?? ""} {d.ApplicationUser.Cognome ?? ""}".Trim() : "N/D"
                    })
                    .OrderBy(d => d.nome)
                    .ToList();

                _logger?.LogInformation($"Dipendenti trovati: {dipendenti.Count}");

                var result = new
                {
                    success = true,
                    appuntamenti,
                    servizi,
                    dipendenti,
                    debug = new
                    {
                        totalAppuntamenti = totalAppuntamenti,
                        appuntamentiCaricati = appuntamenti.Count,
                        saloneId = saloneId.ToString(),
                        userId = userId
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Errore in GetAppuntamenti per salone {saloneId}");
                return Json(new
                {
                    success = false,
                    message = "Errore nel recupero degli appuntamenti: " + ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        #endregion

        #region UpdateAppuntamentoStatus
        [HttpPost]
        public async Task<IActionResult> UpdateAppuntamentoStatus(Guid appointmentId, string newStatus)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Non autorizzato" });
            }

            var appuntamento = await _context.Appuntamento
                .Include(a => a.Salone)
                .FirstOrDefaultAsync(a => a.AppuntamentoId == appointmentId &&
                                          a.Salone.ApplicationUserId == userId);

            if (appuntamento == null)
            {
                return Json(new { success = false, message = "Appuntamento non trovato" });
            }

            try
            {
                // Converti lo stato
                if (Enum.TryParse<StatoAppuntamento>(newStatus, out var nuovoStato))
                {
                    appuntamento.Stato = nuovoStato;
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Stato aggiornato con successo"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Stato non valido" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Errore durante l'aggiornamento: " + ex.Message });
            }
        }
        #endregion
      
        #region GetPromozioniAttive
        [HttpGet]
        public async Task<IActionResult> GetPromozioniAttive(Guid saloneId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Non autorizzato" });
            }

            var salone = await _context.Salone
                .Include(s => s.Servizi)
                .FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

            if (salone == null)
            {
                return Json(new { success = false, message = "Salone non trovato" });
            }

            var promozioniAttive = salone.Servizi
                .Where(s => s.IsPromotion && s.DataFinePromozione > DateTime.Now)
                .OrderBy(s => s.DataFinePromozione)
                .Select(s => new
                {
                    servizioId = s.ServizioId,
                    nome = s.Nome,
                    descrizione = s.Descrizione,
                    prezzo = s.Prezzo,
                    prezzoPromozione = s.PrezzoPromozione,
                    risparmio = s.Prezzo - s.PrezzoPromozione,
                    percentualeSconto = Math.Round(((s.Prezzo - s.PrezzoPromozione) / s.Prezzo) * 100, 0),
                    dataInizioPromozione = s.DataInizioPromozione,
                    dataFinePromozione = s.DataFinePromozione,
                    giorniRimanenti = (s.DataFinePromozione - DateTime.Now).Days,
                    inScadenza = (s.DataFinePromozione - DateTime.Now).Days <= 3
                })
                .ToList();

            var totalePromozioni = promozioniAttive.Count;
            var promozioniInScadenza = promozioniAttive.Count(p => p.inScadenza);

            return Json(new
            {
                success = true,
                totalePromozioni,
                promozioniInScadenza,
                promozioni = promozioniAttive
            });
        }
        #endregion

        #region GetServiziAttivi
        [HttpGet]
        public async Task<IActionResult> GetServiziAttivi(Guid saloneId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Non autorizzato" });
            }

            var salone = await _context.Salone
                .Include(s => s.Servizi)
                    .ThenInclude(s => s.DipendenteServizi)
                        .ThenInclude(ds => ds.Dipendente)
                            .ThenInclude(d => d.ApplicationUser)
                .FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

            if (salone == null)
            {
                return Json(new { success = false, message = "Salone non trovato" });
            }

            var serviziAttivi = salone.Servizi
                .OrderBy(s => s.Nome)
                .Select(s => new
                {
                    servizioId = s.ServizioId,
                    nome = s.Nome,
                    descrizione = s.Descrizione,
                    prezzo = s.Prezzo,
                    prezzoEffettivo = s.PrezzoEffettivo,
                    durata = s.Durata,
                    isPromotion = s.IsPromotion && s.DataFinePromozione > DateTime.Now,
                    prezzoPromozione = s.PrezzoPromozione,
                    percentualeSconto = s.IsPromotion && s.DataFinePromozione > DateTime.Now ?
                        Math.Round(((s.Prezzo - s.PrezzoPromozione) / s.Prezzo) * 100, 0) : 0,
                    dipendentiAssegnati = s.DipendenteServizi.Select(ds => new
                    {
                        nome = ds.Dipendente.ApplicationUser.Nome,
                        cognome = ds.Dipendente.ApplicationUser.Cognome
                    }).ToList(),
                    numeroDipendenti = s.DipendenteServizi.Count
                })
                .ToList();

            var totaleServizi = serviziAttivi.Count;
            var serviziInPromozione = serviziAttivi.Count(s => s.isPromotion);
            var prezzoMedio = serviziAttivi.Any() ? serviziAttivi.Average(s => s.prezzoEffettivo) : 0;

            return Json(new
            {
                success = true,
                totaleServizi,
                serviziInPromozione,
                prezzoMedio = Math.Round(prezzoMedio, 2),
                servizi = serviziAttivi
            });
        }
        #endregion

        #region GetDipendentiAttivi
        [HttpGet]
        public async Task<IActionResult> GetDipendentiAttivi(Guid saloneId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Non autorizzato" });
            }

            var salone = await _context.Salone
                .Include(s => s.Dipendenti)
                    .ThenInclude(d => d.ApplicationUser)
                .Include(s => s.Dipendenti)
                    .ThenInclude(d => d.ServiziOfferti)
                        .ThenInclude(ds => ds.Servizio)
                .Include(s => s.Dipendenti)
                    .ThenInclude(d => d.Appuntamenti)
                .FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

            if (salone == null)
            {
                return Json(new { success = false, message = "Salone non trovato" });
            }

            var oggi = DateTime.Today;
            var dipendentiAttivi = salone.Dipendenti
                .OrderBy(d => d.ApplicationUser.Cognome)
                .ThenBy(d => d.ApplicationUser.Nome)
                .Select(d => new
                {
                    dipendenteId = d.DipendenteId,
                    nome = d.ApplicationUser.Nome,
                    cognome = d.ApplicationUser.Cognome,
                    email = d.ApplicationUser.Email,
                    telefono = d.ApplicationUser.PhoneNumber,
                    dataAssunzione = d.DataCreazione,
                    serviziOfferti = d.ServiziOfferti.Select(so => new
                    {
                        servizioId = so.ServizioId,
                        nomeServizio = so.Servizio.Nome,
                        prezzo = so.Servizio.PrezzoEffettivo
                    }).ToList(),
                    numeroServizi = d.ServiziOfferti.Count,
                    appuntamentiOggi = d.Appuntamenti.Count(a => a.Data.Date == oggi && a.Stato == StatoAppuntamento.Prenotato),
                    appuntamentiTotali = d.Appuntamenti.Count(a => a.Stato == StatoAppuntamento.Prenotato),
                    prossimoAppuntamento = d.Appuntamenti
                        .Where(a => a.Data >= DateTime.Now && a.Stato == StatoAppuntamento.Prenotato)
                        .OrderBy(a => a.Data)
                        .Select(a => new
                        {
                            data = a.Data,
                            ora = a.OraInizio.ToString("HH:mm"),
                            servizio = a.Servizio.Nome,
                            cliente = $"{a.ApplicationUser.Nome} {a.ApplicationUser.Cognome}"
                        })
                        .FirstOrDefault()
                })
                .ToList();

            var totaleDipendenti = dipendentiAttivi.Count;
            var dipendentiConAppuntamentiOggi = dipendentiAttivi.Count(d => d.appuntamentiOggi > 0);
            var dipendentiSenzaServizi = dipendentiAttivi.Count(d => d.numeroServizi == 0);

            return Json(new
            {
                success = true,
                totaleDipendenti,
                dipendentiConAppuntamentiOggi,
                dipendentiSenzaServizi,
                dipendenti = dipendentiAttivi
            });
        }
        #endregion

        #region GetPrenotazioniGiornaliere
        [HttpGet]
        public async Task<IActionResult> GetPrenotazioniGiornaliere(Guid saloneId, DateTime? data = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Non autorizzato" });
            }

            var salone = await _context.Salone
                .FirstOrDefaultAsync(s => s.SaloneId == saloneId && s.ApplicationUserId == userId);

            if (salone == null)
            {
                return Json(new { success = false, message = "Salone non trovato" });
            }

            var dataRiferimento = data ?? DateTime.Today;

            // Prima recupera i dati grezzi
            var prenotazioniQuery = await _context.Appuntamento
                .Include(a => a.ApplicationUser)
                .Include(a => a.Dipendente)
                    .ThenInclude(d => d.ApplicationUser)
                .Include(a => a.Servizio)
                .Include(a => a.Slot)
                .Where(a => a.SaloneId == saloneId &&
                           a.Data.Date == dataRiferimento.Date &&
                           a.Stato == StatoAppuntamento.Prenotato)
                .ToListAsync();

            // Poi ordina e proietta in memoria
            var prenotazioni = prenotazioniQuery
                .OrderBy(a => a.OraInizio)
                .Select(a => new
                {
                    appuntamentoId = a.AppuntamentoId,
                    data = a.Data,
                    oraInizio = a.OraInizio.ToString("HH:mm"),
                    oraFine = a.OraFine.ToString("HH:mm"),
                    cliente = new
                    {
                        nome = $"{a.ApplicationUser.Nome} {a.ApplicationUser.Cognome}",
                        telefono = a.ApplicationUser.PhoneNumber,
                        email = a.ApplicationUser.Email
                    },
                    servizio = new
                    {
                        nome = a.Servizio?.Nome ?? "Servizio non specificato",
                        durata = a.Servizio?.Durata ?? 0,
                        prezzo = a.Servizio?.PrezzoEffettivo ?? 0
                    },
                    dipendente = a.Dipendente != null ? new
                    {
                        nome = $"{a.Dipendente.ApplicationUser.Nome} {a.Dipendente.ApplicationUser.Cognome}"
                    } : null,
                    note = a.Note,
                    stato = a.Stato.ToString()
                })
                .ToList();

            // Calcola statistiche orarie
            var prenotazioniPerOra = prenotazioni
                .GroupBy(p => p.oraInizio.Substring(0, 2))
                .Select(g => new
                {
                    ora = g.Key,
                    numero = g.Count()
                })
                .OrderBy(x => x.ora)
                .ToList();

            // Calcola incasso totale
            var incassoTotale = prenotazioni.Sum(p => p.servizio.prezzo);

            return Json(new
            {
                success = true,
                data = dataRiferimento.ToString("dd/MM/yyyy"),
                totalePrenotazioni = prenotazioni.Count,
                incassoTotale,
                prenotazioniPerOra,
                prenotazioni
            });
        }
        #endregion



        #region Stripe
        public IActionResult CreateCheckoutSession(string id)
		{
			if (id == null)
				return BadRequest("Utente non loggato");
			return View();
		}

		[HttpPost]
		public IActionResult CreateCheckoutSession()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return BadRequest("L'utente non esiste.");
			}

			var checkPiano = _context.PianoSelezionato.FirstOrDefault(x => x.ApplicationUserId == userId && x.Confermato == false);
			if (checkPiano == null)
			{
				if (TempData["SelectedPianoId"] != null)
				{
					var selectedPianoId = TempData["SelectedPianoId"].ToString();
					checkPiano = new PianoSelezionato
					{
						ApplicationUserId = userId,
						PianoSelezionatoId = Guid.NewGuid(),
						AbbonamentoId = Guid.Parse(selectedPianoId)
					};
				}
				else
					return BadRequest("Nessun piano selezionato.");
			}

			var piano = _context.Abbonamento.FirstOrDefault(x => x.AbbonamentoId == checkPiano.AbbonamentoId);
			if (piano == null)
			{
				return BadRequest("Piano non trovato.");
			}

			var total = piano.Prezzo;
			var saloneId = _context.Salone.FirstOrDefault(x => x.ApplicationUserId == userId && x.Stato == Stato.InAttesaDiApprovazione)?.SaloneId;
			if (saloneId == null)
			{
				return BadRequest("Salone non trovato.");
			}

			var customerId = CreateOrGetStripeCustomer();

			var options = new SessionCreateOptions
			{
				Customer = customerId,
				PaymentMethodTypes = new List<string> { "card", "klarna", "paypal", "samsung_pay", "sepa_debit" },
				LineItems = new List<SessionLineItemOptions>
				{
					new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = Convert.ToInt64(total * 100),
							Currency = "eur",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = "Registrazione Have A Seat",
								Metadata = new Dictionary<string, string>
								{
									{"P_IVA", GetCustomerMetadata("P_IVA")},
									{"SaloneId", saloneId.ToString()}
								}
							},
						},
						Quantity = 1,
					},
				},
				Mode = "payment",
				SuccessUrl = $"{Request.Scheme}://{Request.Host}/Partner/PaymentSuccess?sessionId={{CHECKOUT_SESSION_ID}}",
				CancelUrl = Url.Action("PaymentCancel", "Partner", null, Request.Scheme),
				PaymentIntentData = new SessionPaymentIntentDataOptions
				{
					Metadata = new Dictionary<string, string>
					{
						{"P_IVA", GetCustomerMetadata("P_IVA")},
						{"SaloneId", saloneId.ToString()}
					}
				}
			};

			var service = new SessionService();
			var session = service.Create(options);

			return Json(new { url = session.Url });
		}

		private string CreateOrGetStripeCustomer()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var salone = _context.Salone.FirstOrDefault(x => x.ApplicationUserId == userId && x.Stato == Stato.InAttesaDiApprovazione);

			var customerService = new CustomerService();

			var searchOptions = new CustomerSearchOptions
			{
				Query = $"email:'{salone?.Email}' AND metadata['P_IVA']:'{salone?.PartitaIVA}'"
			};

			var existingCustomers = customerService.Search(searchOptions);
			if (existingCustomers.Any())
				return existingCustomers.First().Id;

			var customerOptions = new CustomerCreateOptions
			{
				Email = salone?.Email,
				Name = salone?.Nome,
				Address = new AddressOptions
				{
					City = salone?.Citta,
					Country = "IT",
					Line1 = salone?.Indirizzo,
					PostalCode = salone?.CAP,
					State = salone?.Provincia
				},
				Phone = salone?.Telefono,
				Metadata = new Dictionary<string, string>
				{
					{"P_IVA", salone?.PartitaIVA}
				}
			};

			var newCustomer = customerService.Create(customerOptions);
			return newCustomer.Id;
		}

		private string GetCustomerMetadata(string key)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var salone = _context.Salone.FirstOrDefault(x => x.ApplicationUserId == userId && x.Stato == Stato.InAttesaDiApprovazione);
			return key switch
			{
				"P_IVA" => salone?.PartitaIVA,
				_ => string.Empty
			};
		}

		public IActionResult PaymentSuccess(string sessionId)
		{
			var service = new SessionService();
			Session session = service.Get(sessionId);

			if (session.PaymentStatus == "paid")
			{
				var paymentIntentService = new PaymentIntentService();
				PaymentIntent paymentIntent = paymentIntentService.Get(session.PaymentIntentId);
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				var salone = _context.Salone.FirstOrDefault(x => x.ApplicationUserId == userId && x.Stato == Stato.InAttesaDiApprovazione);
				if (salone != null)
				{
					salone.Stato = Stato.Attivo;
					_context.Salone.Update(salone);
					var pianoSelezionato = _context.PianoSelezionato.FirstOrDefault(x => x.ApplicationUserId == userId && x.Confermato == false);
					if (pianoSelezionato == null)
					{
						ViewBag.Errore = "Errore: Piano selezionato non trovato.";
						return RedirectToAction("Index");
					}

					pianoSelezionato.Confermato = true;
					_context.PianoSelezionato.Update(pianoSelezionato);

					var saloneAbbonamento = new SaloneAbbonamento
					{
						SaloneAbbonamentoId = Guid.NewGuid(),
						SaloneId = salone.SaloneId,
						Salone = salone,
						AbbonamentoId = pianoSelezionato.AbbonamentoId,
						Abbonamento = _context.Abbonamento.Find(pianoSelezionato.AbbonamentoId),
						DataInizio = DateTime.Now,
						DataFine = DateTime.Now.AddMonths(1),
						Stato = Stato.Attivo,
						StripeCustomerId = paymentIntent.CustomerId,
						StripeSubscriptionId = paymentIntent.Id,
						IsTrial = false
					};
					_context.SaloneAbbonamento.Add(saloneAbbonamento);
					_context.SaveChanges();
					ViewBag.Messaggio = "Registrazione effettuata con successo!";
					return RedirectToAction("Index");
				}
				else
				{
					ViewBag.Errore = "Errore: Salone non trovato.";
				}
			}
			else
			{
				ViewBag.Errore = "Errore: Il pagamento non è stato completato.";
			}

			return View("Index");
		}

		public IActionResult PaymentCancel()
		{
			ViewBag.Errore = "Pagamento annullato.";
			return View("Index");
		}
        #endregion

        #region Sedi
        public IActionResult Sedi()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			// Recupera tutti i saloni dell'utente con le relative informazioni
			var saloni = _context.Salone
				.Where(s => s.ApplicationUserId == userId)
				.Include(s => s.Appuntamenti)
				.Include(s => s.Dipendenti)
				.Include(s => s.Servizi)
				.Include(s => s.SaloneAbbonamenti)
				.OrderByDescending(s => s.DataCreazione)
				.ToList();

			return View(saloni);
		}

		[HttpGet]
		public IActionResult SedeDetails(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			var salone = _context.Salone
				.Where(s => s.SaloneId == id && s.ApplicationUserId == userId)
				.Include(s => s.Appuntamenti)
					.ThenInclude(a => a.ApplicationUser)
				.Include(s => s.Dipendenti)
					.ThenInclude(d => d.ApplicationUser)
				.Include(s => s.Dipendenti)
					.ThenInclude(o => o.ServiziOfferti)
				.Include(s => s.Servizi)
				.Include(s => s.SaloneAbbonamenti)
					.ThenInclude(sa => sa.Abbonamento)
				.Include(s => s.Orari)
				.Include(s => s.Recensioni)
					.ThenInclude(r => r.ApplicationUser)
				.FirstOrDefault();

			if (salone == null)
			{
				return NotFound();
			}

			return View(salone);
		}

		public IActionResult CreateSede()
		{
			return View();
		}

		[HttpPost]
		public IActionResult CreateSedePrefix([Bind(Prefix = "NuovoSalone")] Salone model)
		{
			return CreateSedeInternal(model);
		}

		[HttpPost]
		public IActionResult CreateSede(Salone model)
		{
			return CreateSedeInternal(model);
		}

		private IActionResult CreateSedeInternal(Salone model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
				return RedirectToAction("Error", "Home");

			var user = _context.Users.Find(userId);
			if (user == null)
				return RedirectToAction("Error", "Home");

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
				ApplicationUser = user
			};

			_context.Salone.Add(salone);
			_context.SaveChanges();
			return RedirectToAction("CreateCheckoutSession", "Partner", new { id = userId });
		}

		[HttpGet]
		public IActionResult EditSede(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			var salone = _context.Salone
				.FirstOrDefault(s => s.SaloneId == id && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return NotFound();
			}

			return View(salone);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditSede(Salone model)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			var existingSalone = await _context.Salone
				.FirstOrDefaultAsync(s => s.SaloneId == model.SaloneId && s.ApplicationUserId == userId);

			if (existingSalone == null)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				existingSalone.Nome = model.Nome;
				existingSalone.Indirizzo = model.Indirizzo;
				existingSalone.Citta = model.Citta;
				existingSalone.Provincia = model.Provincia;
				existingSalone.CAP = model.CAP;
				existingSalone.Regione = model.Regione;
				existingSalone.Telefono = model.Telefono;
				existingSalone.Email = model.Email;
				existingSalone.SitoWeb = model.SitoWeb;
				existingSalone.RagioneSociale = model.RagioneSociale;
				existingSalone.PartitaIVA = model.PartitaIVA;
				existingSalone.SDI = model.SDI;

				try
				{
					await _context.SaveChangesAsync();
					TempData["Success"] = "Sede aggiornata con successo!";
					return RedirectToAction("SedeDetails", new { id = existingSalone.SaloneId });
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", "Errore durante l'aggiornamento: " + ex.Message);
				}
			}

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> UpdateSedeStatus(Guid id, Stato nuovoStato)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return Json(new { success = false, message = "Utente non autorizzato" });
			}

			var salone = await _context.Salone
				.FirstOrDefaultAsync(s => s.SaloneId == id && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Sede non trovata" });
			}

			try
			{
				salone.Stato = nuovoStato;
				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					message = $"Stato sede aggiornato a {nuovoStato}",
					newStatus = nuovoStato.ToString()
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Errore durante l'aggiornamento: " + ex.Message });
			}
		}

		[HttpPost]
		public async Task<IActionResult> DeleteSede(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return Json(new { success = false, message = "Utente non autorizzato" });
			}

			var salone = await _context.Salone
				.Include(s => s.Appuntamenti)
				.Include(s => s.Dipendenti)
				.Include(s => s.Servizi)
				.FirstOrDefaultAsync(s => s.SaloneId == id && s.ApplicationUserId == userId);

			if (salone == null)
			{
				return Json(new { success = false, message = "Sede non trovata" });
			}

			// Verifica se ci sono appuntamenti futuri
			var appuntamentiFuturi = salone.Appuntamenti
				.Any(a => a.Data > DateTime.Now && a.Stato != StatoAppuntamento.Annullato);

			if (appuntamentiFuturi)
			{
				return Json(new
				{
					success = false,
					message = "Impossibile eliminare la sede: ci sono appuntamenti futuri programmati"
				});
			}

			try
			{
				_context.Salone.Remove(salone);
				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					message = "Sede eliminata con successo"
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Errore durante l'eliminazione: " + ex.Message });
			}
		}

		[HttpGet]
		public IActionResult ManageSede(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			var salone = _context.Salone
				.Where(s => s.SaloneId == id && s.ApplicationUserId == userId)
				.Include(s => s.Servizi)
				.Include(s => s.Dipendenti)
					.ThenInclude(d => d.ApplicationUser)
				.Include(s => s.Dipendenti)
					.ThenInclude(d => d.ServiziOfferti)
				.Include(s => s.Orari)
				.FirstOrDefault();

			if (salone == null)
			{
				return NotFound();
			}

			// Imposta il salone come corrente nella sessione per le altre operazioni
			HttpContext.Session.SetString("CurrentSaloneId", id.ToString());

			return View(salone);
		}
        #endregion

        #region Profilo Partner
        [HttpGet]
        public IActionResult ProfiloPartner()
		{
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var pianoSelected = _context.PianoSelezionato.FirstOrDefault(x => x.ApplicationUserId == userId);
            if (pianoSelected != null)
            {
                var abbonamento = _context.Abbonamento
                    .FirstOrDefault(a => a.AbbonamentoId == pianoSelected.AbbonamentoId);

                if (abbonamento != null)
                {
                    ViewBag.AbbonamentoNome = abbonamento.Nome;
                }
            }

            // Genera le iniziali per il placeholder
            ViewBag.UserInitials = GetUserInitials(user.Nome, user.Cognome);

            return View(user);
        }
        private string GetUserInitials(string nome, string cognome)
        {
            var initials = "";
            if (!string.IsNullOrEmpty(nome))
                initials += nome[0];
            if (!string.IsNullOrEmpty(cognome))
                initials += cognome[0];

            return initials.ToUpper();
        }
        [HttpPost]
        public async Task<IActionResult> UploadProfileImage(IFormFile profileImage)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Utente non autorizzato" });
            }

            if (profileImage == null || profileImage.Length == 0)
            {
                return Json(new { success = false, message = "Nessun file selezionato" });
            }

            // Verifica il tipo di file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(profileImage.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return Json(new { success = false, message = "Formato file non supportato. Usa JPG, JPEG, PNG o GIF." });
            }

            // Verifica la dimensione del file (max 5MB)
            if (profileImage.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "Il file è troppo grande. Dimensione massima: 5MB" });
            }

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Utente non trovato" });
                }

                // Elimina l'immagine precedente se esiste
                if (!string.IsNullOrEmpty(user.ImmagineUser))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ImmagineUser.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                // Crea la directory se non esiste
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                // Genera un nome univoco per il file
                var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Salva il file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                // Aggiorna il database con il percorso dell'immagine
                user.ImmagineUser = $"/uploads/profiles/{uniqueFileName}";
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = "Immagine caricata con successo",
                    imagePath = user.ImmagineUser
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Errore durante il caricamento: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Utente non autorizzato" });
            }

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Utente non trovato" });
                }

                // Elimina il file fisico se esiste
                if (!string.IsNullOrEmpty(user.ImmagineUser))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ImmagineUser.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // Rimuovi il percorso dal database
                user.ImmagineUser = null;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Immagine del profilo eliminata con successo"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Errore durante l'eliminazione: " + ex.Message });
            }
        }
        #endregion

        #region Personalizzazione salone
        public async Task<IActionResult> Personalizza(Guid? id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			// Recupera tutti i saloni dell'utente
			var saloniUtente = await _context.Salone
				.Include(x => x.SaloneAbbonamenti)
				.Include(x => x.SalonePersonalizzazione)
				.Where(s => s.ApplicationUserId == userId && s.Stato == Stato.Attivo)
				.OrderBy(s => s.Nome)
				.ToListAsync();

			// Se non ci sono saloni attivi, reindirizza alle sedi
			if (!saloniUtente.Any())
			{
				return RedirectToAction("Sedi");
			}

			// Determina quale salone personalizzare
			Salone selectedSalone;
			if (id.HasValue && saloniUtente.Any(s => s.SaloneId == id.Value))
			{
				selectedSalone = saloniUtente.FirstOrDefault(s => s.SaloneId == id.Value);
			}
			else
			{
				selectedSalone = saloniUtente.First();
				// Se ha più saloni e non è specificato l'ID, reindirizza con il primo salone
				if (saloniUtente.Count > 1)
				{
					return RedirectToAction("Personalizza", new { id = selectedSalone.SaloneId });
				}
			}

			// Verifica se ha abbonamento Standard
			var abbonamentoStandard = selectedSalone.SaloneAbbonamenti.Any(x => x.AbbonamentoId == SubscriptionsConstants.Basic);
			if (abbonamentoStandard)
				ViewBag.Basic = true;

			// Passa i dati alla view
			ViewBag.Saloni = saloniUtente;
			ViewBag.SaloneCorrente = selectedSalone;
			ViewBag.HasMultipleSedi = saloniUtente.Count > 1;

			return View(selectedSalone);
		}
        #endregion
    }
}
