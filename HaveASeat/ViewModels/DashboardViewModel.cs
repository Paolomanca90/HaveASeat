using HaveASeat.Models;

namespace HaveASeat.ViewModels
{
    public class DashboardViewModel
    {
        // Salone selezionato
        public Guid? SelectedSaloneId { get; set; }
        public List<Salone> SaloniUtente { get; set; } = new List<Salone>();
        public Salone? SaloneCorrente { get; set; }
        // Per il form di creazione nuovo salone
        public Salone NuovoSalone { get; set; } = new Salone();

        // Statistiche generali
        public DashboardStats Stats { get; set; } = new DashboardStats();

        // Dati per grafici
        public ChartData ChartData { get; set; } = new ChartData();

        // Top servizi
        public List<TopServizioViewModel> TopServizi { get; set; } = new List<TopServizioViewModel>();

        // Appuntamenti del giorno
        public List<AppuntamentoViewModel> AppuntamentiOggi { get; set; } = new List<AppuntamentoViewModel>();

        // Periodo selezionato per i report
        public string PeriodoSelezionato { get; set; } = "settimana";
        public DateTime DataInizio { get; set; }
        public DateTime DataFine { get; set; }
    }

    public class DashboardStats
    {
        public int PrenotazioniOggi { get; set; }
        public decimal PercentualePrenotazioni { get; set; }
        public bool IsPrenotazioniPositive { get; set; }

        public int NuoviClienti { get; set; }
        public decimal PercentualeNuoviClienti { get; set; }
        public bool IsNuoviClientiPositive { get; set; }

        public decimal IncassoGiornaliero { get; set; }
        public decimal PercentualeIncasso { get; set; }
        public bool IsIncassoPositive { get; set; }

        public int ServiziCompletati { get; set; }
        public decimal PercentualeServizi { get; set; }
        public bool IsServiziPositive { get; set; }
        public int NumeroDipendenti { get; set; }
        public int NumeroServizi { get; set; }
        public int PromozioniAttive { get; set; }
        public decimal PercentualePromozioni { get; set; }
        public bool IsPromozioniPositive { get; set; }
    }

    public class ChartData
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Incassi { get; set; } = new List<decimal>();
        public List<int> Prenotazioni { get; set; } = new List<int>();
    }

    public class TopServizioViewModel
    {
        public Guid ServizioId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int NumeroPrenotazioni { get; set; }
        public decimal IncassoTotale { get; set; }
    }

    public class AppuntamentoViewModel
    {
        public Guid AppuntamentoId { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        public string NomeServizio { get; set; } = string.Empty;
        public string NomeDipendente { get; set; } = string.Empty;
        public string OrarioInizio { get; set; } = string.Empty;
        public string OrarioFine { get; set; } = string.Empty;
        public decimal Prezzo { get; set; }
        public string Stato { get; set; } = string.Empty;
        public string ClasseStato { get; set; } = string.Empty;
    }
}

