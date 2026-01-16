namespace HaveASeat.ViewModels
{
    public class UpdateProfileModel
    {
        public string? Nome { get; set; }
        public string? Cognome { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CodiceFiscale { get; set; }
        public string? Indirizzo { get; set; }
        public string? Citta { get; set; }
        public string? Provincia { get; set; }
        public string? CAP { get; set; }
        public bool AcceptNewsletter { get; set; }
    }
}
