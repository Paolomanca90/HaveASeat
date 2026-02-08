namespace HaveASeat.Models
{
	public class PaymentTransaction
	{
		public Guid PaymentTransactionId { get; set; }
		public Guid SaloneId { get; set; }
		public string UserId { get; set; } = string.Empty;
		public string? StripeSessionId { get; set; }
		public string? StripeSubscriptionId { get; set; }
		public string? StripeCustomerId { get; set; }
		public string? StripePaymentIntentId { get; set; }
		public decimal Importo { get; set; }
		public string Valuta { get; set; } = "EUR";
		public string Tipo { get; set; } = string.Empty; // subscription_created, payment_succeeded, payment_failed, subscription_cancelled
		public string Stato { get; set; } = string.Empty; // paid, failed, refunded, pending
		public string Descrizione { get; set; } = string.Empty;
		public DateTime DataCreazione { get; set; } = DateTime.UtcNow;

		public Salone? Salone { get; set; }
		public ApplicationUser? ApplicationUser { get; set; }
	}
}
