using HaveASeat.Data;
using HaveASeat.Models;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace HaveASeat.Services
{
	public interface IPushNotificationService
	{
		Task SubscribeAsync(string endpoint, string p256dh, string auth, string? userId = null);
		Task UnsubscribeAsync(string endpoint);
		Task SendNotificationAsync(string userId, string title, string body, string? url = null, string? tag = null);
		Task SendNotificationToAllAsync(string title, string body, string? url = null);
	}

	public class PushNotificationService : IPushNotificationService
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<PushNotificationService> _logger;
		private readonly VapidDetails _vapidDetails;

		public PushNotificationService(
			ApplicationDbContext context,
			ILogger<PushNotificationService> logger,
			IConfiguration configuration)
		{
			_context = context;
			_logger = logger;

			var vapidSection = configuration.GetSection("VapidKeys");
			_vapidDetails = new VapidDetails(
				vapidSection["Subject"] ?? "mailto:noreply@haveaseat.it",
				vapidSection["PublicKey"] ?? string.Empty,
				vapidSection["PrivateKey"] ?? string.Empty);
		}

		public async Task SubscribeAsync(string endpoint, string p256dh, string auth, string? userId = null)
		{
			// Verifica se esiste già una sottoscrizione con lo stesso endpoint
			var existing = await _context.PushSubscription
				.FirstOrDefaultAsync(s => s.Endpoint == endpoint);

			if (existing != null)
			{
				existing.P256dh = p256dh;
				existing.Auth = auth;
				existing.UserId = userId ?? existing.UserId;
				existing.IsActive = true;
			}
			else
			{
				var subscription = new PushSubscriptionEntity
				{
					PushSubscriptionId = Guid.NewGuid(),
					Endpoint = endpoint,
					P256dh = p256dh,
					Auth = auth,
					UserId = userId,
					CreatedAt = DateTime.UtcNow,
					IsActive = true
				};
				_context.PushSubscription.Add(subscription);
			}

			await _context.SaveChangesAsync();
		}

		public async Task UnsubscribeAsync(string endpoint)
		{
			var subscription = await _context.PushSubscription
				.FirstOrDefaultAsync(s => s.Endpoint == endpoint);

			if (subscription != null)
			{
				subscription.IsActive = false;
				await _context.SaveChangesAsync();
			}
		}

		public async Task SendNotificationAsync(string userId, string title, string body, string? url = null, string? tag = null)
		{
			var subscriptions = await _context.PushSubscription
				.Where(s => s.UserId == userId && s.IsActive)
				.ToListAsync();

			foreach (var sub in subscriptions)
			{
				await SendPushAsync(sub, title, body, url, tag);
			}
		}

		public async Task SendNotificationToAllAsync(string title, string body, string? url = null)
		{
			var subscriptions = await _context.PushSubscription
				.Where(s => s.IsActive)
				.ToListAsync();

			foreach (var sub in subscriptions)
			{
				await SendPushAsync(sub, title, body, url);
			}
		}

		private async Task SendPushAsync(PushSubscriptionEntity sub, string title, string body, string? url = null, string? tag = null)
		{
			try
			{
				var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
				var webPushClient = new WebPushClient();

				var payload = System.Text.Json.JsonSerializer.Serialize(new
				{
					title,
					body,
					url = url ?? "/",
					tag = tag ?? "haveaseat"
				});

				await webPushClient.SendNotificationAsync(pushSubscription, payload, _vapidDetails);
			}
			catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone ||
											  ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				// Endpoint non più valido, disattiva la sottoscrizione
				_logger.LogInformation("Push subscription scaduta, disattivazione: {Endpoint}", sub.Endpoint);
				sub.IsActive = false;
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Errore invio push notification a {Endpoint}", sub.Endpoint);
			}
		}
	}
}
