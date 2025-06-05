using HaveASeat.Models;
using HaveASeat.Utilities.Enum;

namespace HaveASeat.ViewModels
{
	public class CalendarioViewModel
	{
		public List<Salone> Saloni { get; set; } = new List<Salone>();
		public Salone SaloneCorrente { get; set; } = new Salone();
		public DateTime DataSelezionata { get; set; }
		public DateTime InizioSettimana { get; set; }
		public DateTime FineSettimana { get; set; }
		public List<Appuntamento> Appuntamenti { get; set; } = new List<Appuntamento>();
		public List<Dipendente> Dipendenti { get; set; } = new List<Dipendente>();
		public List<Slot> Slots { get; set; } = new List<Slot>();

		public List<Servizio> Servizi { get; set; } = new List<Servizio>();

		public bool HasMultipleSedi { get; set; }

		// Metodi helper esistenti
		public List<Appuntamento> GetAppuntamentiPerGiorno(DateTime data)
		{
			return Appuntamenti.Where(a => a.Data.Date == data.Date).ToList();
		}

		public List<Appuntamento> GetAppuntamentiPerDipendente(Guid dipendenteId, DateTime data)
		{
			return Appuntamenti.Where(a => a.DipendenteId == dipendenteId && a.Data.Date == data.Date).ToList();
		}

		public bool HasAppuntamentiInSlot(DateTime data, Guid slotId)
		{
			return Appuntamenti.Any(a => a.Data.Date == data.Date && a.SlotId == slotId && a.Stato != StatoAppuntamento.Annullato);
		}

		// NUOVI METODI per il calendario dipendenti
		public List<Appuntamento> GetAppuntamentiPerDipendenteESlot(Guid dipendenteId, Guid slotId, DateTime data)
		{
			return Appuntamenti.Where(a =>
				a.DipendenteId == dipendenteId &&
				a.SlotId == slotId &&
				a.Data.Date == data.Date &&
				a.Stato != StatoAppuntamento.Annullato).ToList();
		}

		public Appuntamento GetAppuntamentoInSlot(Guid dipendenteId, Guid slotId, DateTime data)
		{
			return Appuntamenti.FirstOrDefault(a =>
				a.DipendenteId == dipendenteId &&
				a.SlotId == slotId &&
				a.Data.Date == data.Date &&
				a.Stato != StatoAppuntamento.Annullato);
		}

		// Calcola quanti slot occupa un appuntamento in base alla durata del servizio
		public int GetSlotsOccupatiDaAppuntamento(Appuntamento appuntamento)
		{
			if (appuntamento.Servizio == null) return 1;

			// Assumendo che ogni slot sia di 30 minuti
			var minutiPerSlot = 30;
			var slotsNecessari = (int)Math.Ceiling((double)appuntamento.Servizio.Durata / minutiPerSlot);
			return Math.Max(1, slotsNecessari);
		}

		// Verifica se un dipendente è disponibile in un determinato slot
		public bool IsDipendenteDisponibile(Guid dipendenteId, Guid slotId, DateTime data)
		{
			return !Appuntamenti.Any(a =>
				a.DipendenteId == dipendenteId &&
				a.SlotId == slotId &&
				a.Data.Date == data.Date &&
				a.Stato != StatoAppuntamento.Annullato);
		}

		// Ottieni il colore per un tipo di servizio (per la visualizzazione)
		public string GetColoreServizio(string nomeServizio)
		{
			// Colori predefiniti per diversi tipi di servizio
			var coloriServizi = new Dictionary<string, string>
			{
				["taglio"] = "bg-blue-100 border-blue-500 text-blue-800",
				["colorazione"] = "bg-purple-100 border-purple-500 text-purple-800",
				["piega"] = "bg-green-100 border-green-500 text-green-800",
				["barba"] = "bg-orange-100 border-orange-500 text-orange-800",
				["massaggio"] = "bg-teal-100 border-teal-500 text-teal-800",
				["default"] = "bg-gray-100 border-gray-500 text-gray-800"
			};

			if (string.IsNullOrEmpty(nomeServizio))
				return coloriServizi["default"];

			var servizioLower = nomeServizio.ToLower();
			foreach (var kvp in coloriServizi)
			{
				if (servizioLower.Contains(kvp.Key))
					return kvp.Value;
			}

			return coloriServizi["default"];
		}
	}
}