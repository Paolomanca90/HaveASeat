using HaveASeat.Utilities.Enum;

namespace HaveASeat.Models
{
	public class Appuntamento
	{
		public Guid AppuntamentoId { get; set; }
		public Guid SaloneId { get; set; }
		public string ApplicationUserId { get; set; } = string.Empty;

		// PROPRIETÀ AGGIORNATE: Calcolo dinamico degli orari basato su Slot
		public DateTime OraInizio => Data.Date.Add(Slot?.OraInizio ?? TimeSpan.Zero);
		public DateTime OraFine => Data.Date.Add(Slot?.OraFine ?? TimeSpan.Zero);

		public string Note { get; set; } = string.Empty;
		public Guid SlotId { get; set; }
		public Guid? DipendenteId { get; set; } // ID del dipendente assegnato all'appuntamento (opzionale)
		public DateTime Data { get; set; } // Data dell'appuntamento
		public StatoAppuntamento Stato { get; set; } // Stato dell'appuntamento (in attesa, confermato, annullato, ecc.)
		public Guid? ServizioId { get; set; }

		// Relazioni di navigazione
		public Servizio? Servizio { get; set; }
		public Salone Salone { get; set; } = new Salone();
		public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();
		public Slot Slot { get; set; } = new Slot();
		public Dipendente? Dipendente { get; set; } // Dipendente assegnato all'appuntamento (opzionale)

		// NUOVE PROPRIETÀ HELPER per il calcolo della durata
		/// <summary>
		/// Durata calcolata dell'appuntamento in minuti
		/// </summary>
		public int DurataMinuti => (int)(OraFine - OraInizio).TotalMinutes;

		/// <summary>
		/// Verifica se l'appuntamento è nel futuro
		/// </summary>
		public bool IsFuturo => Data.Date > DateTime.Now.Date ||
								(Data.Date == DateTime.Now.Date && OraInizio.TimeOfDay > DateTime.Now.TimeOfDay);

		/// <summary>
		/// Verifica se l'appuntamento è oggi
		/// </summary>
		public bool IsOggi => Data.Date == DateTime.Now.Date;

		/// <summary>
		/// Verifica se l'appuntamento può essere cancellato (24 ore prima)
		/// </summary>
		public bool PuoEssereCancellato => IsFuturo &&
										  DateTime.Now.AddHours(24) < OraInizio &&
										  Stato == StatoAppuntamento.Prenotato;

		/// <summary>
		/// Verifica se l'appuntamento si sovrappone con un altro periodo
		/// </summary>
		/// <param name="altroInizio">Ora inizio dell'altro appuntamento</param>
		/// <param name="altroFine">Ora fine dell'altro appuntamento</param>
		/// <returns>True se c'è sovrapposizione</returns>
		public bool SiSovrappone(TimeSpan altroInizio, TimeSpan altroFine)
		{
			var mioInizio = OraInizio.TimeOfDay;
			var mioFine = OraFine.TimeOfDay;

			return mioInizio < altroFine && mioFine > altroInizio;
		}

		/// <summary>
		/// Verifica se l'appuntamento si sovrappone con un altro appuntamento
		/// </summary>
		/// <param name="altroAppuntamento">L'altro appuntamento da confrontare</param>
		/// <returns>True se c'è sovrapposizione nella stessa data</returns>
		public bool SiSovrappone(Appuntamento altroAppuntamento)
		{
			if (Data.Date != altroAppuntamento.Data.Date)
				return false;

			return SiSovrappone(altroAppuntamento.OraInizio.TimeOfDay, altroAppuntamento.OraFine.TimeOfDay);
		}

		/// <summary>
		/// Ottiene una descrizione testuale dello stato dell'appuntamento
		/// </summary>
		public string StatoDescrizione => Stato switch
		{
			StatoAppuntamento.Prenotato when IsFuturo => "Confermato",
			StatoAppuntamento.Prenotato when !IsFuturo => "Completato",
			StatoAppuntamento.Annullato => "Cancellato",
			_ => Stato.ToString()
		};

		/// <summary>
		/// Ottiene la classe CSS appropriata per lo stato dell'appuntamento
		/// </summary>
		public string StatoCssClass => Stato switch
		{
			StatoAppuntamento.Prenotato when IsFuturo => "text-green-600 bg-green-100",
			StatoAppuntamento.Prenotato when !IsFuturo => "text-blue-600 bg-blue-100",
			StatoAppuntamento.Annullato => "text-red-600 bg-red-100",
			_ => "text-gray-600 bg-gray-100"
		};

		/// <summary>
		/// Ottiene un sommario testuale dell'appuntamento
		/// </summary>
		public string Sommario
		{
			get
			{
				var servizioNome = Servizio?.Nome ?? "Servizio non specificato";
				var dipendenteNome = Dipendente != null ?
					$" con {Dipendente.ApplicationUser.Nome} {Dipendente.ApplicationUser.Cognome}" : "";
				var orario = $"{OraInizio:HH:mm}-{OraFine:HH:mm}";

				return $"{servizioNome}{dipendenteNome} - {orario}";
			}
		}

		/// <summary>
		/// Calcola il tempo rimanente fino all'appuntamento
		/// </summary>
		public TimeSpan? TempoRimanente
		{
			get
			{
				if (!IsFuturo) return null;
				return OraInizio - DateTime.Now;
			}
		}

		/// <summary>
		/// Ottiene una descrizione user-friendly del tempo rimanente
		/// </summary>
		public string TempoRimanenteDescrizione
		{
			get
			{
				var tempo = TempoRimanente;
				if (!tempo.HasValue) return "";

				var timeSpan = tempo.Value;

				if (timeSpan.TotalDays >= 1)
					return $"tra {(int)timeSpan.TotalDays} giorni";
				else if (timeSpan.TotalHours >= 1)
					return $"tra {(int)timeSpan.TotalHours} ore";
				else if (timeSpan.TotalMinutes >= 1)
					return $"tra {(int)timeSpan.TotalMinutes} minuti";
				else
					return "a breve";
			}
		}
	}
}