using HaveASeat.Utilities.Dto;

namespace HaveASeat.Services
{
    /// <summary>
    /// Background service che pulisce periodicamente le reservation scadute
    /// </summary>
    public class SlotReservationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SlotReservationCleanupService> _logger;

        public SlotReservationCleanupService(
            IServiceProvider serviceProvider,
            ILogger<SlotReservationCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SlotReservationCleanupService avviato");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var reservationService = scope.ServiceProvider.GetRequiredService<ISlotReservationService>();

                    var cleanedCount = await reservationService.CleanupExpiredReservationsAsync();

                    if (cleanedCount > 0)
                    {
                        _logger.LogInformation("Pulite {Count} reservation scadute", cleanedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante la pulizia delle reservation scadute");
                }

                await Task.Delay(TimeSpan.FromSeconds(SlotReservationConfig.CleanupIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("SlotReservationCleanupService terminato");
        }
    }
}
