namespace etymo.Web.Components.Services
{
    public class HeartbeatService(ILogger<HeartbeatService> logger) : IHostedService
    {
        private readonly ILogger<HeartbeatService> _logger = logger;
        private Timer? _timer;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Heartbeat service starting...");

            // Create a timer that fires every 5 minutes (300,000 ms)
            _timer = new Timer(DoHeartbeat, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Heartbeat service stopping...");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoHeartbeat(object? state)
        {
            _logger.LogDebug("Heartbeat tick at: {time}", DateTime.UtcNow);
        }
    }
}