using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.Utilities.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace HaveASeat.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[AllowAnonymous]
	public class StripeWebhookController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<StripeWebhookController> _logger;
		private readonly string _webhookSecret;

		public StripeWebhookController(
			ApplicationDbContext context,
			ILogger<StripeWebhookController> logger,
			IConfiguration configuration)
		{
			_context = context;
			_logger = logger;
			_webhookSecret = configuration["Stripe:WebhookKey"] ?? string.Empty;
		}

		[HttpPost]
		public async Task<IActionResult> HandleWebhook()
		{
			var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

			try
			{
				var stripeEvent = EventUtility.ConstructEvent(
					json,
					Request.Headers["Stripe-Signature"],
					_webhookSecret);

				_logger.LogInformation("Stripe webhook ricevuto: {EventType}", stripeEvent.Type);

				switch (stripeEvent.Type)
				{
					case EventTypes.CheckoutSessionCompleted:
						await HandleCheckoutSessionCompleted(stripeEvent);
						break;

					case EventTypes.PaymentIntentSucceeded:
						await HandlePaymentIntentSucceeded(stripeEvent);
						break;

					case EventTypes.PaymentIntentPaymentFailed:
						await HandlePaymentIntentFailed(stripeEvent);
						break;

					case EventTypes.CustomerSubscriptionCreated:
					case EventTypes.CustomerSubscriptionUpdated:
						await HandleSubscriptionUpdated(stripeEvent);
						break;

					case EventTypes.CustomerSubscriptionDeleted:
						await HandleSubscriptionDeleted(stripeEvent);
						break;

					default:
						_logger.LogInformation("Evento Stripe non gestito: {EventType}", stripeEvent.Type);
						break;
				}

				return Ok();
			}
			catch (StripeException ex)
			{
				_logger.LogError(ex, "Errore validazione webhook Stripe");
				return BadRequest("Firma webhook non valida");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore elaborazione webhook Stripe");
				return StatusCode(500);
			}
		}

		private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
		{
			var session = stripeEvent.Data.Object as Session;
			if (session == null) return;

			_logger.LogInformation("Checkout session completata: {SessionId}, PaymentStatus: {Status}, Mode: {Mode}",
				session.Id, session.PaymentStatus, session.Mode);

			if (session.PaymentStatus != "paid" && session.Status != "complete") return;

			// Per subscription mode, i metadata sono nella subscription
			string? saloneIdStr = null;
			string? userIdStr = null;
			string? abbonamentoIdStr = null;
			var stripeSubscriptionId = session.SubscriptionId;
			var stripeCustomerId = session.CustomerId;

			// Prova a recuperare metadata dalla subscription
			if (!string.IsNullOrEmpty(stripeSubscriptionId))
			{
				var subscriptionService = new SubscriptionService();
				var subscription = await subscriptionService.GetAsync(stripeSubscriptionId);
				subscription?.Metadata?.TryGetValue("SaloneId", out saloneIdStr);
				subscription?.Metadata?.TryGetValue("UserId", out userIdStr);
				subscription?.Metadata?.TryGetValue("AbbonamentoId", out abbonamentoIdStr);
			}

			// Fallback: prova dal PaymentIntent (per sessioni in mode payment)
			if (string.IsNullOrEmpty(saloneIdStr) && !string.IsNullOrEmpty(session.PaymentIntentId))
			{
				var paymentIntentService = new PaymentIntentService();
				var paymentIntent = await paymentIntentService.GetAsync(session.PaymentIntentId);
				paymentIntent?.Metadata?.TryGetValue("SaloneId", out saloneIdStr);
				stripeSubscriptionId ??= paymentIntent?.Id;
			}

			if (string.IsNullOrEmpty(saloneIdStr) || !Guid.TryParse(saloneIdStr, out var saloneId))
			{
				_logger.LogWarning("SaloneId non trovato nei metadata della sessione {SessionId}", session.Id);
				return;
			}

			// Verifica se già elaborato (idempotenza)
			var idToCheck = stripeSubscriptionId ?? session.Id;
			var existingAbbonamento = await _context.SaloneAbbonamento
				.AnyAsync(sa => sa.StripeSubscriptionId == idToCheck);

			if (existingAbbonamento)
			{
				_logger.LogInformation("Pagamento già elaborato per {Id}", idToCheck);
				return;
			}

			var salone = await _context.Salone.FindAsync(saloneId);
			if (salone == null)
			{
				_logger.LogWarning("Salone {SaloneId} non trovato per il pagamento", saloneId);
				return;
			}

			salone.Stato = Stato.Attivo;
			_context.Salone.Update(salone);

			var pianoSelezionato = await _context.PianoSelezionato
				.FirstOrDefaultAsync(x => x.ApplicationUserId == salone.ApplicationUserId && !x.Confermato);

			if (pianoSelezionato != null)
			{
				pianoSelezionato.Confermato = true;
				_context.PianoSelezionato.Update(pianoSelezionato);

				var abbonamento = await _context.Abbonamento.FindAsync(pianoSelezionato.AbbonamentoId);
				var durataGiorni = abbonamento?.Durata ?? 30;

				var saloneAbbonamento = new SaloneAbbonamento
				{
					SaloneAbbonamentoId = Guid.NewGuid(),
					SaloneId = salone.SaloneId,
					AbbonamentoId = pianoSelezionato.AbbonamentoId,
					DataInizio = DateTime.Now,
					DataFine = DateTime.Now.AddDays(durataGiorni),
					Stato = Stato.Attivo,
					StripeCustomerId = stripeCustomerId,
					StripeSubscriptionId = idToCheck,
					IsTrial = false
				};
				_context.SaloneAbbonamento.Add(saloneAbbonamento);

				// Log transazione
				_context.PaymentTransaction.Add(new PaymentTransaction
				{
					PaymentTransactionId = Guid.NewGuid(),
					SaloneId = salone.SaloneId,
					UserId = salone.ApplicationUserId,
					StripeSessionId = session.Id,
					StripeSubscriptionId = stripeSubscriptionId,
					StripeCustomerId = stripeCustomerId,
					Importo = abbonamento?.Prezzo ?? 0,
					Valuta = "EUR",
					Tipo = "subscription_created",
					Stato = "paid",
					Descrizione = $"Abbonamento {abbonamento?.Nome} - Webhook",
					DataCreazione = DateTime.UtcNow
				});
			}

			await _context.SaveChangesAsync();
			_logger.LogInformation("Salone {SaloneId} attivato tramite webhook", saloneId);
		}

		private async Task HandlePaymentIntentSucceeded(Event stripeEvent)
		{
			var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
			if (paymentIntent == null) return;

			_logger.LogInformation("PaymentIntent riuscito: {PaymentIntentId}, Amount: {Amount}",
				paymentIntent.Id, paymentIntent.Amount);

			// Log transazione
			Guid.TryParse(paymentIntent.Metadata?.GetValueOrDefault("SaloneId"), out var saloneId);
			if (saloneId != Guid.Empty)
			{
				var salone = await _context.Salone.FindAsync(saloneId);
				_context.PaymentTransaction.Add(new PaymentTransaction
				{
					PaymentTransactionId = Guid.NewGuid(),
					SaloneId = saloneId,
					UserId = salone?.ApplicationUserId ?? "",
					StripePaymentIntentId = paymentIntent.Id,
					StripeCustomerId = paymentIntent.CustomerId,
					Importo = paymentIntent.Amount / 100m,
					Valuta = paymentIntent.Currency?.ToUpper() ?? "EUR",
					Tipo = "payment_succeeded",
					Stato = "paid",
					Descrizione = $"Pagamento riuscito - {paymentIntent.Id}",
					DataCreazione = DateTime.UtcNow
				});
				await _context.SaveChangesAsync();
			}
		}

		private async Task HandlePaymentIntentFailed(Event stripeEvent)
		{
			var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
			if (paymentIntent == null) return;

			_logger.LogWarning("PaymentIntent fallito: {PaymentIntentId}, Error: {Error}",
				paymentIntent.Id,
				paymentIntent.LastPaymentError?.Message ?? "Sconosciuto");

			Guid.TryParse(paymentIntent.Metadata?.GetValueOrDefault("SaloneId"), out var saloneId);
			if (saloneId != Guid.Empty)
			{
				var salone = await _context.Salone.FindAsync(saloneId);

				_context.PaymentTransaction.Add(new PaymentTransaction
				{
					PaymentTransactionId = Guid.NewGuid(),
					SaloneId = saloneId,
					UserId = salone?.ApplicationUserId ?? "",
					StripePaymentIntentId = paymentIntent.Id,
					StripeCustomerId = paymentIntent.CustomerId,
					Importo = paymentIntent.Amount / 100m,
					Valuta = paymentIntent.Currency?.ToUpper() ?? "EUR",
					Tipo = "payment_failed",
					Stato = "failed",
					Descrizione = $"Pagamento fallito: {paymentIntent.LastPaymentError?.Message ?? "Sconosciuto"}",
					DataCreazione = DateTime.UtcNow
				});
				await _context.SaveChangesAsync();
			}
		}

		private async Task HandleSubscriptionUpdated(Event stripeEvent)
		{
			var subscription = stripeEvent.Data.Object as Subscription;
			if (subscription == null) return;

			_logger.LogInformation("Subscription aggiornata: {SubscriptionId}, Status: {Status}",
				subscription.Id, subscription.Status);

			var abbonamento = await _context.SaloneAbbonamento
				.FirstOrDefaultAsync(sa => sa.StripeSubscriptionId == subscription.Id);

			if (abbonamento != null)
			{
				abbonamento.Stato = subscription.Status switch
				{
					"active" => Stato.Attivo,
					"past_due" => Stato.InAttesaDiApprovazione,
					"canceled" => Stato.Rifiutato,
					_ => abbonamento.Stato
				};

				// Aggiorna data fine se presente nei dati della subscription
				if (subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd != null)
				{
					abbonamento.DataFine = subscription.Items.Data.First().CurrentPeriodEnd;
				}

				await _context.SaveChangesAsync();
			}
		}

		private async Task HandleSubscriptionDeleted(Event stripeEvent)
		{
			var subscription = stripeEvent.Data.Object as Subscription;
			if (subscription == null) return;

			_logger.LogInformation("Subscription cancellata: {SubscriptionId}", subscription.Id);

			var abbonamento = await _context.SaloneAbbonamento
				.Include(sa => sa.Salone)
				.FirstOrDefaultAsync(sa => sa.StripeSubscriptionId == subscription.Id);

			if (abbonamento != null)
			{
				abbonamento.Stato = Stato.Rifiutato;

				// Disattiva il salone se non ha altri abbonamenti attivi
				var hasOtherActive = await _context.SaloneAbbonamento
					.AnyAsync(sa => sa.SaloneId == abbonamento.SaloneId &&
								   sa.SaloneAbbonamentoId != abbonamento.SaloneAbbonamentoId &&
								   sa.Stato == Stato.Attivo &&
								   sa.DataFine > DateTime.Now);

				if (!hasOtherActive)
				{
					abbonamento.Salone.Stato = Stato.Rifiutato;
				}

				await _context.SaveChangesAsync();
			}
		}
	}
}
