using HaveASeat.Utilities.Enum;

namespace HaveASeat.Utilities.Dto
{
	public class UpdateAppuntamentoDto
	{
		public Guid? DipendenteId { get; set; }
		public string Note { get; set; }
		public StatoAppuntamento? Stato { get; set; }
	}
}
