using System.ComponentModel.DataAnnotations;

namespace HaveASeat.Utilities.Dto
{
	public class ApplicationUserDto
	{
		[Required(ErrorMessage = "Il nome è obbligatorio.")]
		[StringLength(50, ErrorMessage = "Il nome non può superare i 50 caratteri.")]
		[Display(Name = "Nome")]
		public string Nome { get; set; }

		[Required(ErrorMessage = "Il cognome è obbligatorio.")]
		[StringLength(50, ErrorMessage = "Il cognome non può superare i 50 caratteri.")]
		[Display(Name = "Cognome")]
		public string Cognome { get; set; }

		[Required(ErrorMessage = "L'email è obbligatoria.")]
		[EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido.")]
		[Display(Name = "Email")]
		public string Email { get; set; }

		[Required(ErrorMessage = "Il numero di telefono è obbligatorio.")]
		[Phone(ErrorMessage = "Inserisci un numero di telefono valido.")]
		[StringLength(20, ErrorMessage = "Il numero di telefono non può superare i 20 caratteri.")]
		[Display(Name = "Telefono")]
		public string PhoneNumber { get; set; }

		[StringLength(200)]
		[Display(Name = "Indirizzo")]
		public string Address { get; set; }

		[StringLength(50)]
		[Display(Name = "Città")]
		public string City { get; set; }

		[StringLength(10)]
		[Display(Name = "CAP")]
		public string PostalCode { get; set; }

		[StringLength(50)]
		[Display(Name = "Provincia")]
		public string Province { get; set; }

		[StringLength(50)]
		[Display(Name = "C.F.")]
		public string CodiceFiscale { get; set; }

		[Required(ErrorMessage = "La password è obbligatoria.")]
		[StringLength(100, ErrorMessage = "La password deve essere lunga almeno {2} caratteri e non più di {1}.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Conferma password")]
		[Compare("Password", ErrorMessage = "La password e la conferma password non corrispondono.")]
		public string ConfirmPassword { get; set; }

		public bool AcceptNewsletter { get; set; } = false;
	}
}
