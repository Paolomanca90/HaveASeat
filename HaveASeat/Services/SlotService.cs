using HaveASeat.Data;
using HaveASeat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HaveASeat.Services
{
	public interface ISlotService
	{
		Task<List<SlotAvailabilityDto>> GetAvailableSlotsAsync(Guid saloneId, DateTime data, Guid? dipendenteId = null, Guid? servizioId = null);
		Task<bool> IsSlotAvailableAsync(Guid saloneId, DateTime data, TimeSpan oraInizio, TimeSpan oraFine, Guid? dipendenteId = null);
		Task<List<SlotAvailabilityDto>> GenerateDynamicSlotsAsync(Guid saloneId, DateTime data, Guid? dipendenteId = null, int? serviceDurationMinutes = null);
		Task<bool> ValidateSlotBookingAsync(Guid saloneId, DateTime data, TimeSpan oraInizio, TimeSpan oraFine, Guid? dipendenteId = null);
	}

	public class SlotService : ISlotService
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<SlotService> _logger;
		private const int SLOT_DURATION_MINUTES = 30;

		public SlotService(ApplicationDbContext context, ILogger<SlotService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<List<SlotAvailabilityDto>> GetAvailableSlotsAsync(Guid saloneId, DateTime data, Guid? dipendenteId = null, Guid? servizioId = null)
		{
			try
			{
				_logger.LogInformation($"Getting available slots for salone {saloneId}, data {data:yyyy-MM-dd}, dipendente {dipendenteId}, servizio {servizioId}");

				var salone = await _context.Salone
					.Include(s => s.Orari)
					.Include(s => s.SaloneAbbonamenti)
						.ThenInclude(sa => sa.Abbonamento)
					.Include(s => s.Dipendenti)
						.ThenInclude(d => d.Orari)
					.Include(s => s.Dipendenti)
						.ThenInclude(d => d.ServiziOfferti)
							.ThenInclude(so => so.Servizio)
					.FirstOrDefaultAsync(s => s.SaloneId == saloneId);

				if (salone == null)
				{
					_logger.LogWarning($"Salone {saloneId} not found");
					return new List<SlotAvailabilityDto>();
				}

				// Verifica se il salone ha la funzionalità di scelta dipendente
				var hasStaffSelection = salone.SaloneAbbonamenti.Any(sa =>
					sa.Abbonamento.Nome.Contains("Pro") || sa.Abbonamento.Nome.Contains("Business"));

				// Ottieni la durata del servizio se specificato
				int? serviceDurationMinutes = null;
				if (servizioId.HasValue)
				{
					var servizio = await _context.Servizio
						.FirstOrDefaultAsync(s => s.ServizioId == servizioId.Value);
					serviceDurationMinutes = servizio != null ? (int)servizio.Durata : null;
				}

				List<SlotAvailabilityDto> availableSlots;

				if (hasStaffSelection && dipendenteId.HasValue)
				{
					// Modalità con scelta dipendente
					availableSlots = await GenerateSlotsForDipendente(salone, data, dipendenteId.Value, serviceDurationMinutes, servizioId);
				}
				else
				{
					// Modalità senza scelta dipendente (usa orari salone)
					availableSlots = await GenerateSlotsForSalone(salone, data, serviceDurationMinutes);
				}

				_logger.LogInformation($"Generated {availableSlots.Count} available slots");
				return availableSlots;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting available slots for salone {saloneId}");
				return new List<SlotAvailabilityDto>();
			}
		}

		public async Task<List<SlotAvailabilityDto>> GenerateDynamicSlotsAsync(Guid saloneId, DateTime data, Guid? dipendenteId = null, int? serviceDurationMinutes = null)
		{
			return await GetAvailableSlotsAsync(saloneId, data, dipendenteId, null);
		}

		public async Task<bool> IsSlotAvailableAsync(Guid saloneId, DateTime data, TimeSpan oraInizio, TimeSpan oraFine, Guid? dipendenteId = null)
		{
			try
			{
				// Controlla se ci sono appuntamenti che si sovrappongono
				var existingAppointments = await _context.Appuntamento
					.Include(a => a.Slot)
					.Where(a => a.SaloneId == saloneId &&
							   a.Data.Date == data.Date &&
							   a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato)
					.Where(a => !dipendenteId.HasValue || a.DipendenteId == dipendenteId)
					.ToListAsync();

				foreach (var appointment in existingAppointments)
				{
					var existingStart = appointment.OraInizio.TimeOfDay;
					var existingEnd = appointment.OraFine.TimeOfDay;

					// Controlla sovrapposizione: se l'inizio o la fine del nuovo slot si sovrappone con un appuntamento esistente
					if ((oraInizio < existingEnd && oraFine > existingStart))
					{
						_logger.LogDebug($"Slot conflict found: new slot {oraInizio}-{oraFine} overlaps with existing {existingStart}-{existingEnd}");
						return false;
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking slot availability");
				return false;
			}
		}

		public async Task<bool> ValidateSlotBookingAsync(Guid saloneId, DateTime data, TimeSpan oraInizio, TimeSpan oraFine, Guid? dipendenteId = null)
		{
			// Verifica che la data sia futura
			if (data.Date <= DateTime.Now.Date)
				return false;

			// Verifica che l'orario sia nel futuro se è oggi
			if (data.Date == DateTime.Now.Date && oraInizio <= DateTime.Now.TimeOfDay)
				return false;

			// Verifica disponibilità slot
			return await IsSlotAvailableAsync(saloneId, data, oraInizio, oraFine, dipendenteId);
		}

		private async Task<List<SlotAvailabilityDto>> GenerateSlotsForDipendente(Salone salone, DateTime data, Guid dipendenteId, int? serviceDurationMinutes, Guid? servizioId)
		{
			var dipendente = salone.Dipendenti.FirstOrDefault(d => d.DipendenteId == dipendenteId);
			if (dipendente == null)
			{
				_logger.LogWarning($"Dipendente {dipendenteId} not found in salone {salone.SaloneId}");
				return new List<SlotAvailabilityDto>();
			}

			// Verifica che il dipendente offra il servizio richiesto
			if (servizioId.HasValue && !dipendente.ServiziOfferti.Any(so => so.ServizioId == servizioId.Value))
			{
				_logger.LogWarning($"Dipendente {dipendenteId} does not offer service {servizioId}");
				return new List<SlotAvailabilityDto>();
			}

			var dayOfWeek = data.DayOfWeek;

			// Cerca l'orario specifico del dipendente per quel giorno
			var orarioDipendente = dipendente.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);

			// Se il dipendente non ha orari specifici, usa gli orari del salone come fallback
			TimeSpan apertura, chiusura;
			bool isDayOff;

			if (orarioDipendente != null)
			{
				apertura = orarioDipendente.Apertura;
				chiusura = orarioDipendente.Chiusura;
				isDayOff = orarioDipendente.IsDayOff;
			}
			else
			{
				var orarioSalone = salone.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);
				if (orarioSalone == null)
				{
					_logger.LogDebug($"No working hours found for day {dayOfWeek}");
					return new List<SlotAvailabilityDto>();
				}
				apertura = orarioSalone.Apertura;
				chiusura = orarioSalone.Chiusura;
				isDayOff = orarioSalone.IsDayOff;
			}

			if (isDayOff)
			{
				_logger.LogDebug($"Day off for dipendente {dipendenteId} on {dayOfWeek}");
				return new List<SlotAvailabilityDto>();
			}

			return await GenerateSlotList(salone.SaloneId, data, apertura, chiusura, serviceDurationMinutes, dipendenteId);
		}

		private async Task<List<SlotAvailabilityDto>> GenerateSlotsForSalone(Salone salone, DateTime data, int? serviceDurationMinutes)
		{
			var dayOfWeek = data.DayOfWeek;
			var orarioSalone = salone.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);

			if (orarioSalone == null || orarioSalone.IsDayOff)
			{
				_logger.LogDebug($"No working hours or day off for salone {salone.SaloneId} on {dayOfWeek}");
				return new List<SlotAvailabilityDto>();
			}

			return await GenerateSlotList(salone.SaloneId, data, orarioSalone.Apertura, orarioSalone.Chiusura, serviceDurationMinutes);
		}

		private async Task<List<SlotAvailabilityDto>> GenerateSlotList(Guid saloneId, DateTime data, TimeSpan apertura, TimeSpan chiusura, int? serviceDurationMinutes, Guid? dipendenteId = null)
		{
			var slots = new List<SlotAvailabilityDto>();
			var slotDuration = TimeSpan.FromMinutes(SLOT_DURATION_MINUTES);
			var serviceDuration = TimeSpan.FromMinutes(serviceDurationMinutes ?? SLOT_DURATION_MINUTES);

			var currentTime = apertura;

			// Ottieni tutti gli appuntamenti esistenti per ottimizzare le query
			var existingAppointments = await _context.Appuntamento
				.Include(a => a.Slot)
				.Where(a => a.SaloneId == saloneId &&
						   a.Data.Date == data.Date &&
						   a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato)
				.Where(a => !dipendenteId.HasValue || a.DipendenteId == dipendenteId)
				.ToListAsync();

			_logger.LogDebug($"Found {existingAppointments.Count} existing appointments for {data:yyyy-MM-dd}");

			while (currentTime.Add(serviceDuration) <= chiusura)
			{
				var slotStart = currentTime;
				var slotEnd = currentTime.Add(serviceDuration);

				// Verifica se questo slot si sovrappone con appuntamenti esistenti
				bool isOccupied = false;

				foreach (var appointment in existingAppointments)
				{
					var existingStart = appointment.OraInizio.TimeOfDay;
					var existingEnd = appointment.OraFine.TimeOfDay;

					// Controlla sovrapposizione
					if (slotStart < existingEnd && slotEnd > existingStart)
					{
						isOccupied = true;
						_logger.LogDebug($"Slot {slotStart}-{slotEnd} overlaps with appointment {existingStart}-{existingEnd}");
						break;
					}
				}

				if (!isOccupied)
				{
					// Crea uno slot ID temporaneo basato su orario per identificazione univoca
					var tempSlotId = Guid.NewGuid();

					slots.Add(new SlotAvailabilityDto
					{
						SlotId = tempSlotId,
						OraInizio = slotStart.ToString(@"HH\:mm"),
						OraFine = slotEnd.ToString(@"HH\:mm"),
						Display = $"{slotStart:HH\\:mm} - {slotEnd:HH\\:mm}",
						Disponibile = true,
						PostiLiberi = 1, // Sempre 1 quando c'è scelta dipendente
						Capacita = 1,
						ServiceDurationMinutes = serviceDurationMinutes ?? SLOT_DURATION_MINUTES
					});

					_logger.LogDebug($"Added available slot: {slotStart:HH\\:mm} - {slotEnd:HH\\:mm}");
				}

				currentTime = currentTime.Add(slotDuration);
			}

			return slots;
		}
	}

	// DTO per gli slot disponibili (aggiornato)
	public class SlotAvailabilityDto
	{
		public Guid SlotId { get; set; }
		public string OraInizio { get; set; } = string.Empty;
		public string OraFine { get; set; } = string.Empty;
		public string Display { get; set; } = string.Empty;
		public bool Disponibile { get; set; }
		public int PostiLiberi { get; set; }
		public int Capacita { get; set; }
		public int ServiceDurationMinutes { get; set; } = 30;
	}
}