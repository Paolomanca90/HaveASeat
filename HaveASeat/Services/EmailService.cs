using HaveASeat.Data;
using HaveASeat.Utilities.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace HaveASeat.Services
{
	public interface IEmailService
	{
		Task<bool> SendBookingNotificationAsync(BookingNotificationDto notification);
		Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
		Task<bool> SendBookingConfirmationAsync(string toEmail, string customerName, BookingDetailsDto bookingDetails);
		Task<bool> SendBookingCancellationAsync(string toEmail, string customerName, BookingDetailsDto bookingDetails, string? reason = null);
		Task<bool> SendBookingReminderAsync(string toEmail, string customerName, BookingDetailsDto bookingDetails);
		Task<bool> SendPasswordResetAsync(string toEmail, string resetLink);
		Task<bool> SendEmailConfirmationAsync(string toEmail, string confirmationLink);
	}

	public class EmailService : IEmailService
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<EmailService> _logger;
		private readonly EmailSettings _emailSettings;
        private readonly ApplicationDbContext _context;

		public EmailService(IConfiguration configuration, ILogger<EmailService> logger, ApplicationDbContext context)
		{
			_configuration = configuration;
			_logger = logger;
			_emailSettings = _configuration.GetSection("EmailSettings").Get<EmailSettings>()
				?? throw new InvalidOperationException("EmailSettings non configurate correttamente");
            _context = context;
		}

		public async Task<bool> SendBookingNotificationAsync(BookingNotificationDto notification)
		{
			try
			{
				string subject;
				string body;

                var salone = _context.Appuntamento.Include(x => x.Salone).FirstOrDefault(x => x.AppuntamentoId == notification.AppuntamentoId)?.Salone;

				switch (notification.TipoNotifica.ToLower())
				{
					case "conferma":
						subject = $"Conferma prenotazione - {salone?.Nome}";
						body = GenerateBookingConfirmationHtml(notification);
						break;

					case "cancellazione":
						subject = $"Cancellazione prenotazione - {salone?.Nome}";
						body = GenerateBookingCancellationHtml(notification);
						break;

					case "promemoria":
						subject = $"Promemoria appuntamento - {salone?.Nome}";
						body = GenerateBookingReminderHtml(notification);
						break;

					default:
						subject = $"Notifica prenotazione - {salone?.Nome}";
						body = GenerateGenericBookingHtml(notification);
						break;
				}

				return await SendEmailAsync(notification.EmailDestinatario, subject, body);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore invio notifica booking per {AppuntamentoId}", notification.AppuntamentoId);
				return false;
			}
		}

		public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
		{
			try
			{
				using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
				{
					EnableSsl = _emailSettings.EnableSsl,
					UseDefaultCredentials = false,
					Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
				};

				using var mailMessage = new MailMessage
				{
					From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
					Subject = subject,
					Body = body,
					IsBodyHtml = isHtml,
					BodyEncoding = Encoding.UTF8,
					SubjectEncoding = Encoding.UTF8
				};

				mailMessage.To.Add(toEmail);

				await smtpClient.SendMailAsync(mailMessage);
				_logger.LogInformation("Email inviata con successo a {ToEmail}", toEmail);
				return true;
			}
			catch (SmtpException ex)
			{
				_logger.LogError(ex, "Errore SMTP durante l'invio email a {ToEmail}: {ErrorMessage}", toEmail, ex.Message);
				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore generico durante l'invio email a {ToEmail}", toEmail);
				return false;
			}
		}

		public async Task<bool> SendBookingConfirmationAsync(string toEmail, string customerName, BookingDetailsDto bookingDetails)
		{
			try
			{
				var subject = $"Conferma prenotazione - {bookingDetails.NomeSalone}";
				var body = GenerateBookingConfirmationHtml(new BookingNotificationDto
				{
					EmailDestinatario = toEmail,
					AppuntamentoId = bookingDetails.AppuntamentoId,
					TipoNotifica = "Conferma"
				});

				return await SendEmailAsync(toEmail, subject, body);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore invio conferma prenotazione a {ToEmail}", toEmail);
				return false;
			}
		}

		public async Task<bool> SendBookingCancellationAsync(string toEmail, string customerName, BookingDetailsDto bookingDetails, string? reason = null)
		{
			try
			{
				var subject = $"Cancellazione prenotazione - {bookingDetails.NomeSalone}";
				var body = GenerateBookingCancellationHtml(new BookingNotificationDto
				{
					EmailDestinatario = toEmail,
					AppuntamentoId = bookingDetails.AppuntamentoId,
					TipoNotifica = "Cancellazione",
					Messaggio = reason
				});

				return await SendEmailAsync(toEmail, subject, body);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore invio cancellazione prenotazione a {ToEmail}", toEmail);
				return false;
			}
		}

		public async Task<bool> SendBookingReminderAsync(string toEmail, string customerName, BookingDetailsDto bookingDetails)
		{
			try
			{
				var subject = $"Promemoria appuntamento - {bookingDetails.NomeSalone}";
				var body = GenerateBookingReminderHtml(new BookingNotificationDto
				{
					EmailDestinatario = toEmail,
					AppuntamentoId = bookingDetails.AppuntamentoId,
					TipoNotifica = "Promemoria"
				});

				return await SendEmailAsync(toEmail, subject, body);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore invio promemoria a {ToEmail}", toEmail);
				return false;
			}
		}

		public async Task<bool> SendPasswordResetAsync(string toEmail, string resetLink)
		{
			try
			{
				var subject = "Reset Password - HaveASeat";
				var body = GeneratePasswordResetHtml(resetLink);

				return await SendEmailAsync(toEmail, subject, body);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore invio reset password a {ToEmail}", toEmail);
				return false;
			}
		}

		public async Task<bool> SendEmailConfirmationAsync(string toEmail, string confirmationLink)
		{
			try
			{
				var subject = "Conferma il tuo account - HaveASeat";
				var body = GenerateEmailConfirmationHtml(confirmationLink);

				return await SendEmailAsync(toEmail, subject, body);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore invio conferma email a {ToEmail}", toEmail);
				return false;
			}
		}

		#region Template HTML

		private string GenerateBookingConfirmationHtml(BookingNotificationDto notification)
		{
            var salone = _context.Appuntamento.Include(x => x.Salone).FirstOrDefault(x => x.AppuntamentoId == notification.AppuntamentoId)?.Salone;
            var appuntamento = _context.Appuntamento.FirstOrDefault(x => x.AppuntamentoId == notification.AppuntamentoId);
			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Conferma Prenotazione</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .booking-details {{ background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .detail-row {{ margin: 10px 0; padding: 10px 0; border-bottom: 1px solid #eee; }}
        .label {{ font-weight: bold; color: #555; }}
        .value {{ color: #333; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
        .btn {{ display: inline-block; padding: 12px 24px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Prenotazione Confermata!</h1>
        </div>
        <div class='content'>
            <h2>Ciao,</h2>
            <p>La tua prenotazione è stata confermata con successo. Ecco i dettagli:</p>
            
            <div class='booking-details'>
                <div class='detail-row'>
                    <span class='label'>Salone:</span>
                    <span class='value'>{salone?.Nome}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Data:</span>
                    <span class='value'>{appuntamento?.Data:dddd, dd MMMM yyyy}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Orario:</span>
                    <span class='value'>{appuntamento?.OraInizio}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Codice Prenotazione:</span>
                    <span class='value'>{notification.AppuntamentoId.ToString().Substring(0, 8).ToUpper()}</span>
                </div>
            </div>

            <p><strong>Importante:</strong> Ti ricordiamo che puoi cancellare la prenotazione fino a 24 ore prima dell'appuntamento.</p>
            
            <div style='text-align: center;'>
                <a href='#' class='btn'>Gestisci Prenotazione</a>
            </div>
        </div>
        <div class='footer'>
            <p>Questo è un messaggio automatico, non rispondere a questa email.</p>
            <p>&copy; 2025 HaveASeat. Tutti i diritti riservati.</p>
        </div>
    </div>
</body>
</html>";
		}

		private string GenerateBookingCancellationHtml(BookingNotificationDto notification)
		{
			var salone = _context.Appuntamento.Include(x => x.Salone).FirstOrDefault(x => x.AppuntamentoId == notification.AppuntamentoId)?.Salone;
			var appuntamento = _context.Appuntamento.FirstOrDefault(x => x.AppuntamentoId == notification.AppuntamentoId);

			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Prenotazione Cancellata</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .booking-details {{ background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .detail-row {{ margin: 10px 0; padding: 10px 0; border-bottom: 1px solid #eee; }}
        .label {{ font-weight: bold; color: #555; }}
        .value {{ color: #333; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
        .btn {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Prenotazione Cancellata</h1>
        </div>
        <div class='content'>
            <h2>Ciao,</h2>
            <p>La tua prenotazione è stata cancellata. Dettagli della prenotazione cancellata:</p>
            
            <div class='booking-details'>
                <div class='detail-row'>
                    <span class='label'>Salone:</span>
                    <span class='value'>{salone?.Nome}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Data:</span>
                    <span class='value'>{appuntamento?.Data:dddd, dd MMMM yyyy}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Orario:</span>
                    <span class='value'>{appuntamento?.OraInizio}</span>
                </div>
                {(!string.IsNullOrEmpty(notification.Messaggio) ? $@"
                <div class='detail-row'>
                    <span class='label'>Motivo:</span>
                    <span class='value'>{notification.Messaggio}</span>
                </div>" : "")}
            </div>

            <p>Ci dispiace per l'inconveniente. Puoi effettuare una nuova prenotazione quando vuoi.</p>
            
            <div style='text-align: center;'>
                <a href='#' class='btn'>Nuova Prenotazione</a>
            </div>
        </div>
        <div class='footer'>
            <p>Questo è un messaggio automatico, non rispondere a questa email.</p>
            <p>&copy; 2025 HaveASeat. Tutti i diritti riservati.</p>
        </div>
    </div>
</body>
</html>";
		}

		private string GenerateBookingReminderHtml(BookingNotificationDto notification)
		{
			var salone = _context.Appuntamento.Include(x => x.Salone).FirstOrDefault(x => x.AppuntamentoId == notification.AppuntamentoId)?.Salone;
			var appuntamento = _context.Appuntamento.FirstOrDefault(x => x.AppuntamentoId == notification.AppuntamentoId);

			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Promemoria Appuntamento</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #ffc107; color: #212529; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .booking-details {{ background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .detail-row {{ margin: 10px 0; padding: 10px 0; border-bottom: 1px solid #eee; }}
        .label {{ font-weight: bold; color: #555; }}
        .value {{ color: #333; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
        .reminder-box {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔔 Promemoria Appuntamento</h1>
        </div>
        <div class='content'>
            <h2>Ciao,</h2>
            <p>Ti ricordiamo che hai un appuntamento previsto per domani:</p>
            
            <div class='booking-details'>
                <div class='detail-row'>
                    <span class='label'>Salone:</span>
                    <span class='value'>{salone?.Nome}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Data:</span>
                    <span class='value'>{appuntamento?.Data:dddd, dd MMMM yyyy}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Orario:</span>
                    <span class='value'>{appuntamento?.OraInizio}</span>
                </div>
            </div>

            <div class='reminder-box'>
                <strong>💡 Suggerimento:</strong> Ricordati di arrivare qualche minuto prima dell'orario previsto.
            </div>

            <p>Non vediamo l'ora di vederti!</p>
        </div>
        <div class='footer'>
            <p>Questo è un messaggio automatico, non rispondere a questa email.</p>
            <p>&copy; 2025 HaveASeat. Tutti i diritti riservati.</p>
        </div>
    </div>
</body>
</html>";
		}

		private string GeneratePasswordResetHtml(string resetLink)
		{
			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Password</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #6c757d; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .btn {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Reset Password</h1>
        </div>
        <div class='content'>
            <h2>Reset della password richiesto</h2>
            <p>Hai richiesto il reset della tua password. Clicca sul pulsante qui sotto per procedere:</p>
            
            <div style='text-align: center;'>
                <a href='{resetLink}' class='btn'>Reimposta Password</a>
            </div>
            
            <div class='warning'>
                <strong>⚠️ Attenzione:</strong> Questo link scadrà tra 24 ore. Se non hai richiesto tu questo reset, ignora questa email.
            </div>

            <p>Se il pulsante non funziona, copia e incolla questo link nel tuo browser:</p>
            <p style='word-break: break-all; background-color: #e9ecef; padding: 10px; border-radius: 3px;'>{resetLink}</p>
        </div>
        <div class='footer'>
            <p>Questo è un messaggio automatico, non rispondere a questa email.</p>
            <p>&copy; 2025 HaveASeat. Tutti i diritti riservati.</p>
        </div>
    </div>
</body>
</html>";
		}

		private string GenerateEmailConfirmationHtml(string confirmationLink)
		{
			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Conferma Email</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .btn {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📧 Conferma il tuo account</h1>
        </div>
        <div class='content'>
            <h2>Benvenuto in HaveASeat!</h2>
            <p>Grazie per esserti registrato. Per completare la registrazione, conferma il tuo indirizzo email cliccando sul pulsante qui sotto:</p>
            
            <div style='text-align: center;'>
                <a href='{confirmationLink}' class='btn'>Conferma Email</a>
            </div>

            <p>Se il pulsante non funziona, copia e incolla questo link nel tuo browser:</p>
            <p style='word-break: break-all; background-color: #e9ecef; padding: 10px; border-radius: 3px;'>{confirmationLink}</p>
        </div>
        <div class='footer'>
            <p>Questo è un messaggio automatico, non rispondere a questa email.</p>
            <p>&copy; 2025 HaveASeat. Tutti i diritti riservati.</p>
        </div>
    </div>
</body>
</html>";
		}

		private string GenerateGenericBookingHtml(BookingNotificationDto notification)
		{
			var salone = _context.Appuntamento.Include(x => x.Salone).FirstOrDefault(x => x.AppuntamentoId == notification.AppuntamentoId)?.Salone;
			var appuntamento = _context.Appuntamento.FirstOrDefault(x => x.AppuntamentoId == notification.AppuntamentoId);

			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Notifica Prenotazione</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #6c757d; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .booking-details {{ background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .detail-row {{ margin: 10px 0; padding: 10px 0; border-bottom: 1px solid #eee; }}
        .label {{ font-weight: bold; color: #555; }}
        .value {{ color: #333; }}
        .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📅 Notifica Prenotazione</h1>
        </div>
        <div class='content'>
            <h2>Ciao,</h2>
            <p>Ecco i dettagli della tua prenotazione:</p>
            
            <div class='booking-details'>
                <div class='detail-row'>
                    <span class='label'>Salone:</span>
                    <span class='value'>{salone?.Nome}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Data:</span>
                    <span class='value'>{appuntamento?.Data:dddd, dd MMMM yyyy}</span>
                </div>
                <div class='detail-row'>
                    <span class='label'>Orario:</span>
                    <span class='value'>{appuntamento?.OraInizio}</span>
                </div>
            </div>
        </div>
        <div class='footer'>
            <p>Questo è un messaggio automatico, non rispondere a questa email.</p>
            <p>&copy; 2025 HaveASeat. Tutti i diritti riservati.</p>
        </div>
    </div>
</body>
</html>";
		}

		#endregion
	}

	#region Configuration Classes

	public class EmailSettings
	{
		public string SmtpServer { get; set; } = string.Empty;
		public int SmtpPort { get; set; } = 587;
		public bool EnableSsl { get; set; } = true;
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string FromEmail { get; set; } = string.Empty;
		public string FromName { get; set; } = "HaveASeat";
	}

	#endregion
}