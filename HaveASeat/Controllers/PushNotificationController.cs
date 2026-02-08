using HaveASeat.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace HaveASeat.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PushNotificationController : ControllerBase
	{
		private readonly IPushNotificationService _pushService;
		private readonly IConfiguration _configuration;

		public PushNotificationController(
			IPushNotificationService pushService,
			IConfiguration configuration)
		{
			_pushService = pushService;
			_configuration = configuration;
		}

		[HttpGet("vapid-public-key")]
		public IActionResult GetVapidPublicKey()
		{
			var publicKey = _configuration["VapidKeys:PublicKey"];
			return Ok(new { publicKey });
		}

		[HttpPost("subscribe")]
		public async Task<IActionResult> Subscribe([FromBody] JsonElement subscription)
		{
			var endpoint = subscription.GetProperty("endpoint").GetString();
			var keys = subscription.GetProperty("keys");
			var p256dh = keys.GetProperty("p256dh").GetString();
			var auth = keys.GetProperty("auth").GetString();

			if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(p256dh) || string.IsNullOrEmpty(auth))
			{
				return BadRequest("Dati sottoscrizione non validi");
			}

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			await _pushService.SubscribeAsync(endpoint, p256dh, auth, userId);

			return Ok(new { success = true });
		}

		[HttpPost("unsubscribe")]
		public async Task<IActionResult> Unsubscribe([FromBody] JsonElement subscription)
		{
			var endpoint = subscription.GetProperty("endpoint").GetString();
			if (string.IsNullOrEmpty(endpoint))
			{
				return BadRequest("Endpoint non valido");
			}

			await _pushService.UnsubscribeAsync(endpoint);
			return Ok(new { success = true });
		}
	}
}
