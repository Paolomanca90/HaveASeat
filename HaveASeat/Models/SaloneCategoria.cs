namespace HaveASeat.Models
{
	public class SaloneCategoria
	{
		public Guid SaloneCategoriaId { get; set; }
		public Guid SaloneId { get; set; }
		public Guid CategoriaId { get; set; }
		public Salone Salone { get; set; } = new Salone();
		public Categoria Categoria { get; set; } = new Categoria();
	}
}
