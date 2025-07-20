using System.ComponentModel.DataAnnotations;

namespace HaveASeat.Models
{
	public class GiftCard
	{
		public Guid GiftCardId { get; set; }

		[Required]
		[StringLength(20)]
		public string Code { get; set; } = string.Empty;

		[Required]
		[Range(10, 1000)]
		public decimal Amount { get; set; }

		public Guid? SaloneId { get; set; } // Null se è una gift card generica

		[Required]
		[StringLength(100)]
		public string RecipientName { get; set; } = string.Empty;

		[Required]
		[EmailAddress]
		[StringLength(200)]
		public string RecipientEmail { get; set; } = string.Empty;

		[Required]
		[StringLength(100)]
		public string SenderName { get; set; } = string.Empty;

		[Required]
		[EmailAddress]
		[StringLength(200)]
		public string SenderEmail { get; set; } = string.Empty;

		[StringLength(500)]
		public string? Message { get; set; }

		public DateTime ExpiryDate { get; set; }
		public bool IsUsed { get; set; }
		public DateTime? UsedAt { get; set; }
		public string? UsedByUserId { get; set; }
		public DateTime CreatedAt { get; set; }

		// Navigation properties
		public Salone? Salone { get; set; }
		public ApplicationUser? UsedByUser { get; set; }

		// Computed properties
		public bool IsExpired => DateTime.Now > ExpiryDate;
		public bool IsValid => !IsUsed && !IsExpired;
		public string FormattedCode => Code.Insert(4, "-").Insert(9, "-");
		public string DisplaySalonName => Salone?.Nome ?? "Qualsiasi salone partner";
	}
}