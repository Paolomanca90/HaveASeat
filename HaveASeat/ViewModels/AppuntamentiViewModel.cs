using HaveASeat.Models;

namespace HaveASeat.ViewModels
{
    public class AppuntamentiViewModel
    {
        public List<Salone> SaloniUtente { get; set; } = new List<Salone>();
        public Guid SelectedSaloneId { get; set; }
    }

    //public class AppuntamentiExportsData
    //{
    //    public string NomeSalone { get; set; } = string.Empty;
    //    public DateTime DataInizio { get; set; }
    //    public DateTime DataFine { get; set; }
    //    public DateTime DataExport { get; set; }

    //    // Statistiche
    //    public int TotaleAppuntamenti { get; set; }
    //    public int AppuntamentiConfermati { get; set; }
    //    public int AppuntamentiCancellati { get; set; }
    //    public decimal IncassoTotale { get; set; }

    //    // Lista appuntamenti
    //    public List<AppuntamentoExportViewModel> Appuntamenti { get; set; } = new List<AppuntamentoExportViewModel>();
    //}

    //public class AppuntamentoExportViewModel
    //{
    //    public DateTime Data { get; set; }
    //    public string OraInizio { get; set; } = string.Empty;
    //    public string OraFine { get; set; } = string.Empty;
    //    public string NomeCliente { get; set; } = string.Empty;
    //    public string TelefonoCliente { get; set; } = string.Empty;
    //    public string EmailCliente { get; set; } = string.Empty;
    //    public string Servizio { get; set; } = string.Empty;
    //    public string Dipendente { get; set; } = string.Empty;
    //    public string Stato { get; set; } = string.Empty;
    //    public decimal Prezzo { get; set; }
    //    public string? Note { get; set; }
    //}
}
