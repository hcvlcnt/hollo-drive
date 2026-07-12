using Microsoft.Extensions.Options;

namespace backend.Services
{
    public class TrashCleanupHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<TrashCleanupOptions> _options;
        private readonly ILogger<TrashCleanupHostedService> _logger;

        public TrashCleanupHostedService(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<TrashCleanupOptions> options,
            ILogger<TrashCleanupHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_options.CurrentValue.RunOnStartup)
            {
                await RunCleanupAsync(stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(GetInterval(), stoppingToken);
                    await RunCleanupAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private async Task RunCleanupAsync(CancellationToken cancellationToken)
        {
            if (!_options.CurrentValue.Enabled)
            {
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<ITrashCleanupService>();
                var result = await cleanupService.CleanupExpiredItemsAsync(cancellationToken);

                if (result.DeletedFiles > 0 || result.DeletedFolders > 0)
                {
                    _logger.LogInformation(
                        "Trash cleanup removed {DeletedFiles} files and {DeletedFolders} folders deleted before {CutoffUtc}.",
                        result.DeletedFiles,
                        result.DeletedFolders,
                        result.CutoffUtc);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Trash cleanup failed.");
            }
        }

        private TimeSpan GetInterval()
        {
            var intervalHours = Math.Max(_options.CurrentValue.IntervalHours, 1);
            return TimeSpan.FromHours(intervalHours);
        }
    }
}
