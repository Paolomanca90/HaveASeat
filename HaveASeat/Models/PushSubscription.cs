namespace HaveASeat.Models
{
	public class PushSubscriptionEntity
	{
		public Guid PushSubscriptionId { get; set; }
		public string? UserId { get; set; }
		public string Endpoint { get; set; } = string.Empty;
		public string P256dh { get; set; } = string.Empty;
		public string Auth { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public bool IsActive { get; set; } = true;

		public ApplicationUser? ApplicationUser { get; set; }
	}
}
