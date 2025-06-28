using HaveASeat.Controllers;
using HaveASeat.Data;
using HaveASeat.Models;
using HaveASeat.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HaveASeat.Services
{
	public interface IBookingService
	{
		Task<bool> IsSlotAvailableAsync(Guid saloneId, Guid slotId, DateTime data, Guid? dipendenteId = null);
		Task<List<SlotAvailabilityViewModel>> GetAvailableSlotsAsync(Guid saloneId, DateTime data, Guid? dipendenteId = null);
		Task<BookingConfirmationViewModel> CreateBookingAsync(CreateBookingRequest request, string? userId = null);
		Task<bool> CanCancelBookingAsync(Guid appuntamentoId, string? userId = null);
		Task<bool> CancelBookingAsync(Guid appuntamentoId, string? userId = null);
	}

	public class BookingService : IBookingService
	{
		private readonly ApplicationDbContext _context;

		public BookingService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<bool> IsSlotAvailableAsync(Guid saloneId, Guid slotId, DateTime data, Guid? dipendenteId = null)
		{
			var existing = await _context.Appuntamento
				.AnyAsync(a => a.SaloneId == saloneId &&
							  a.SlotId == slotId &&
							  a.Data.Date == data.Date &&
							  a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato &&
							  (!dipendenteId.HasValue || a.DipendenteId == dipendenteId));

			return !existing;
		}

		public async Task<List<SlotAvailabilityViewModel>> GetAvailableSlotsAsync(Guid saloneId, DateTime data, Guid? dipendenteId = null)
		{
			var salone = await _context.Salone
				.Include(s => s.Slots)
				.Include(s => s.Orari)
				.FirstOrDefaultAsync(s => s.SaloneId == saloneId);

			if (salone == null) return new List<SlotAvailabilityViewModel>();

			var dayOfWeek = data.DayOfWeek;
			var orarioSalone = salone.Orari.FirstOrDefault(o => o.Giorno == dayOfWeek);

			if (orarioSalone == null || orarioSalone.IsDayOff)
				return new List<SlotAvailabilityViewModel>();

			var slots = salone.Slots
				.Where(s => s.IsAttivo && s.GiorniSettimana.Contains(((int)dayOfWeek).ToString()))
				.Where(s => s.OraInizio >= orarioSalone.Apertura && s.OraFine <= orarioSalone.Chiusura)
				.OrderBy(s => s.OraInizio)
				.ToList();

			var appuntamentiEsistenti = await _context.Appuntamento
				.Where(a => a.SaloneId == saloneId &&
						   a.Data.Date == data.Date &&
						   a.Stato != HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato)
				.Where(a => !dipendenteId.HasValue || a.DipendenteId == dipendenteId)
				.ToListAsync();

			return slots.Select(slot => new SlotAvailabilityViewModel
			{
				SlotId = slot.SlotId,
				OraInizio = slot.OraInizio.ToString(@"HH\:mm"),
				OraFine = slot.OraFine.ToString(@"HH\:mm"),
				Display = $"{slot.OraInizio:HH\\:mm} - {slot.OraFine:HH\\:mm}",
				Disponibile = !appuntamentiEsistenti.Any(a => a.SlotId == slot.SlotId),
				PostiLiberi = Math.Max(0, slot.Capacita - appuntamentiEsistenti.Count(a => a.SlotId == slot.SlotId)),
				Capacita = slot.Capacita
			}).Where(s => s.Disponibile).ToList();
		}

		public async Task<BookingConfirmationViewModel> CreateBookingAsync(CreateBookingRequest request, string? userId = null)
		{
			// Implementazione della creazione della prenotazione
			// Questo metodo dovrebbe essere chiamato dal controller
			throw new NotImplementedException("Da implementare nel controller");
		}

		public async Task<bool> CanCancelBookingAsync(Guid appuntamentoId, string? userId = null)
		{
			var appuntamento = await _context.Appuntamento
				.FirstOrDefaultAsync(a => a.AppuntamentoId == appuntamentoId);

			if (appuntamento == null) return false;

			// Verifica autorizzazione
			if (userId != null && appuntamento.ApplicationUserId != userId) return false;

			// Verifica che sia cancellabile (almeno 24 ore prima)
			var limiteCancellazione = DateTime.Now.AddHours(24);
			return appuntamento.Data > limiteCancellazione &&
				   appuntamento.Stato == HaveASeat.Utilities.Enum.StatoAppuntamento.Prenotato;
		}

		public async Task<bool> CancelBookingAsync(Guid appuntamentoId, string? userId = null)
		{
			var appuntamento = await _context.Appuntamento
				.FirstOrDefaultAsync(a => a.AppuntamentoId == appuntamentoId);

			if (appuntamento == null) return false;

			if (!await CanCancelBookingAsync(appuntamentoId, userId)) return false;

			appuntamento.Stato = HaveASeat.Utilities.Enum.StatoAppuntamento.Annullato;
			await _context.SaveChangesAsync();

			return true;
		}
	}
}