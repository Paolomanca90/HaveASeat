using HaveASeat.Utilities.Enum;

namespace HaveASeat.Models
{
	public class Categoria
	{
		public Guid CategoriaId { get; set; }
		public string Nome { get; set; } = string.Empty;
		public string Icon { get; set; } = string.Empty; // Percorso dell'icona
	}
}
