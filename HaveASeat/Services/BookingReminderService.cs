using HaveASeat.Data;
using HaveASeat.Utilities.Dto;
using HaveASeat.Utilities.Enum;
using Microsoft.EntityFrameworkCore;

namespace HaveASeat.Services
{
	public class BookingReminderService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<BookingReminderService> _logger;
		private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

		public BookingReminderService(
			IServiceProvider serviceProvider,
			ILogger<BookingReminderService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("BookingReminderService avviato");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await SendRemindersAsync(stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Errore durante l'invio dei promemoria");
				}

				await Task.Delay(_checkInterval, stoppingToken);
			}
		}

		private async Task SendRemindersAsync(CancellationToken stoppingToken)
		{
			using var scope = _serviceProvider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
			var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

			// Trova appuntamenti nelle prossime 24 ore che non hanno ancora ricevuto il promemoria
			var now = DateTime.Now;
			var reminderWindow = now.AddHours(24);

			var appuntamenti = await context.Appuntamento
				.Include(a => a.ApplicationUser)
				.Include(a => a.Salone)
				.Include(a => a.Servizio)
				.Include(a => a.Dipendente)
					.ThenInclude(d => d!.ApplicationUser)
				.Include(a => a.Slot)
				.Where(a => a.Stato == StatoAppuntamento.Prenotato &&
							!a.ReminderInviato &&
							a.Data >= now.Date &&
							a.Data <= reminderWindow.Date)
				.ToListAsync(stoppingToken);

			// Filtra per ora effettiva (Data contiene solo la data, OraInizio contiene l'ora)
			var daNotificare = appuntamenti
				.Where(a =>
				{
					var dataOra = a.Data.Date.Add(a.Slot?.OraInizio ?? TimeSpan.Zero);
					return dataOra >= now && dataOra <= reminderWindow;
				})
				.ToList();

			if (!daNotificare.Any())
			{
				return;
			}

			_logger.LogInformation("Invio promemoria per {Count} appuntamenti", daNotificare.Count);

			foreach (var appuntamento in daNotificare)
			{
				if (stoppingToken.IsCancellationRequested) break;

				try
				{
					var nomeSalone = appuntamento.Salone?.Nome ?? "il salone";
					var orario = appuntamento.Slot != null
						? $"{appuntamento.Slot.OraInizio:hh\\:mm}"
						: "orario non specificato";
					var data = appuntamento.Data.ToString("dd/MM/yyyy");
					var servizio = appuntamento.Servizio?.Nome ?? "Servizio";

					// Invia email promemoria
					if (!string.IsNullOrEmpty(appuntamento.ApplicationUser?.Email))
					{
						try
						{
							var bookingDetails = new BookingDetailsDto
							{
								AppuntamentoId = appuntamento.AppuntamentoId,
								NomeSalone = nomeSalone,
								IndirizzoSalone = appuntamento.Salone != null
									? $"{appuntamento.Salone.Indirizzo}, {appuntamento.Salone.Citta}" : "",
								TelefonoSalone = appuntamento.Salone?.Telefono ?? "",
								NomeServizio = servizio,
								DataAppuntamento = appuntamento.Data,
								OrarioAppuntamento = orario,
								NomeDipendente = appuntamento.Dipendente != null
									? $"{appuntamento.Dipendente.ApplicationUser.Nome} {appuntamento.Dipendente.ApplicationUser.Cognome}" : null,
								PrezzoTotale = appuntamento.Servizio?.PrezzoEffettivo ?? 0
							};

							await emailService.SendBookingReminderAsync(
								appuntamento.ApplicationUser.Email,
								appuntamento.ApplicationUser.Nome ?? "Cliente",
								bookingDetails);
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Errore invio email promemoria per appuntamento {Id}",
								appuntamento.AppuntamentoId);
						}
					}

					// Invia push notification
					if (!string.IsNullOrEmpty(appuntamento.ApplicationUserId))
					{
						try
						{
							await pushService.SendNotificationAsync(
								appuntamento.ApplicationUserId,
								"Promemoria Appuntamento",
								$"Hai un appuntamento domani alle {orario} presso {nomeSalone} per {servizio}",
								$"/Search/MyBookings",
								$"reminder-{appuntamento.AppuntamentoId}");
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Errore invio push promemoria per appuntamento {Id}",
								appuntamento.AppuntamentoId);
						}
					}

					// Marca come inviato
					appuntamento.ReminderInviato = true;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Errore elaborazione promemoria per appuntamento {Id}",
						appuntamento.AppuntamentoId);
				}
			}

			await context.SaveChangesAsync(stoppingToken);
			_logger.LogInformation("Promemoria inviati con successo per {Count} appuntamenti", daNotificare.Count);
		}
	}
}
