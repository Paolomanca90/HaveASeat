using HaveASeat.ViewModels;

namespace HaveASeat.Services
{
    public interface IExportService
    {
        byte[] ExportToCsv(DashboardExportData data);
        byte[] ExportToExcel(DashboardExportData data);
        byte[] ExportToPdf(DashboardExportData data);
    }
    public class DashboardExportData
    {
        public string? NomeSalone { get; set; }
        public string? Periodo { get; set; }
        public DateTime DataExport { get; set; }
        public DashboardStats? Stats { get; set; }
        public List<TopServizioViewModel>? TopServizi { get; set; }
        public List<AppuntamentoExportViewModel>? Appuntamenti { get; set; }
        public int NumeroDipendenti { get; set; }
        public int PromozioniAttive { get; set; }
    }

    public class AppuntamentoExportViewModel
    {
        public DateTime Data { get; set; }
        public string? OraInizio { get; set; }
        public string? OraFine { get; set; }
        public string? NomeCliente { get; set; }
        public string? Servizio { get; set; }
        public string? Dipendente { get; set; }
        public string? Stato { get; set; }
        public decimal Prezzo { get; set; }
    }
}
