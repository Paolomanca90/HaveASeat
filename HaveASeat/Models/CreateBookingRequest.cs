using System.ComponentModel.DataAnnotations;

namespace HaveASeat.Models
{
	public class CreateBookingRequest
	{
		[Required]
		public Guid SaloneId { get; set; }

		[Required]
		public Guid SlotId { get; set; }

		[Required]
		public Guid ServizioId { get; set; }

		public Guid? DipendenteId { get; set; }

		[Required]
		public DateTime Data { get; set; }

		public string? Note { get; set; }

		// Per utenti non registrati
		public string? Nome { get; set; }

		public string? Cognome { get; set; }

		[EmailAddress]
		public string? Email { get; set; }

		[Phone]
		public string? Telefono { get; set; }
	}
}
