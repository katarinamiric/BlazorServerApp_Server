namespace BlazorServerApp_Server.Services
{
    public class ModelTrainingBackgroundService : BackgroundService
    {
        private readonly ILogger<ModelTrainingBackgroundService> _logger;
        private readonly NavigationPredictorService _predictorService;

        // How often to retrain the model (e.g., 1 hour)
        private readonly TimeSpan _trainingInterval = TimeSpan.FromMinutes(1);

        public ModelTrainingBackgroundService(
            ILogger<ModelTrainingBackgroundService> logger,
            NavigationPredictorService predictorService)
        {
            _logger = logger;
            _predictorService = predictorService;
        }

        // This method is called when the host starts.
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Model Training Background Service is starting.");

            // Initial training on startup
            _logger.LogInformation("Triggering initial ML model training at application startup.");
            await _predictorService.TrainModelPeriodicallyAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Model Training Background Service waiting for {_trainingInterval.TotalMinutes} minutes.");
                try
                {
                    // Wait for the specified interval, respecting cancellation requests
                    await Task.Delay(_trainingInterval, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Timer elapsed: Triggering periodic ML model retraining.");
                        await _predictorService.TrainModelPeriodicallyAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    // This is expected when the app is shutting down
                    _logger.LogInformation("Model Training Background Service was cancelled during delay.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while running Model Training Background Service.");
                    // Depending on the error, you might want to stop or retry.
                    // For now, it will just log and continue trying after the delay.
                }
            }

            _logger.LogInformation("Model Training Background Service is stopping.");
        }
    }
}