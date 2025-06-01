using System.ComponentModel.DataAnnotations;

namespace HaveASeat.Utilities.Dto
{
	public class StaffDto
	{
		[Required(ErrorMessage = "Il nome è obbligatorio.")]
		public string Nome { get; set; }

		[Required(ErrorMessage = "Il cognome è obbligatorio.")]
		public string Cognome { get; set; }

		[Required(ErrorMessage = "L'email è obbligatoria.")]
		[EmailAddress]
		public string Email { get; set; }

		[Required(ErrorMessage = "Il telefono è obbligatoria.")]
		[Phone]
		public string PhoneNumber { get; set; }

		public List<Guid> ServiziIds { get; set; } = new List<Guid>();
	}
}