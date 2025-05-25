using HaveASeat.Data;
using HaveASeat.Utilities.Constants;

namespace HaveASeat.Utilities.Subscriptions
{
	public static class SubscriptionsInitializer
	{
		public static async Task InitializeSubscriptions(ApplicationDbContext _context)
		{
			if (!_context.Abbonamento.Any(x => x.AbbonamentoId == SubscriptionsConstants.Basic))
			{
				var abbonamentoBasic = new Models.Abbonamento
				{
					AbbonamentoId = SubscriptionsConstants.Basic,
					Nome = "Abbonamento Standard",
					Descrizione = "Accesso base alle funzionalità.",
					Prezzo = 9.99m,
					Durata = 30
				};
				_context.Abbonamento.Add(abbonamentoBasic);
			}

			if (!_context.Abbonamento.Any(x => x.AbbonamentoId == SubscriptionsConstants.Pro))
			{
				var abbonamentoPremium = new Models.Abbonamento
				{
					AbbonamentoId = SubscriptionsConstants.Pro,
					Nome = "Abbonamento Pro",
					Descrizione = "Accesso completo a tutte le funzionalità.",
					Prezzo = 29.99m,
					Durata = 30
				};
				_context.Abbonamento.Add(abbonamentoPremium);
			}

			if (!_context.Abbonamento.Any(x => x.AbbonamentoId == SubscriptionsConstants.Business))
			{
				var abbonamentoVIP = new Models.Abbonamento
				{
					AbbonamentoId = SubscriptionsConstants.Business,
					Nome = "Abbonamento Business",
					Descrizione = "Accesso esclusivo con vantaggi premium.",
					Prezzo = 59.99m,
					Durata = 30
				};
				_context.Abbonamento.Add(abbonamentoVIP);
			}

			await _context.SaveChangesAsync();
		}
	}
}
