namespace BlazorServerApp_Server.Services
{
    public class ModelTrainingBackgroundService : BackgroundService
    {
        private readonly ILogger<ModelTrainingBackgroundService> _logger;
        private readonly NavigationPredictorService _predictorService;

        private readonly TimeSpan _trainingInterval = TimeSpan.FromMinutes(1);

        public ModelTrainingBackgroundService(
            ILogger<ModelTrainingBackgroundService> logger,
            NavigationPredictorService predictorService)
        {
            _logger = logger;
            _predictorService = predictorService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Model Training Background Service is starting.");

            _logger.LogInformation("Triggering initial ML model training at application startup.");
            await _predictorService.TrainModelPeriodicallyAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Model Training Background Service waiting for {_trainingInterval.TotalMinutes} minutes.");
                try
                {
                    await Task.Delay(_trainingInterval, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Timer elapsed: Triggering periodic ML model retraining.");
                        await _predictorService.TrainModelPeriodicallyAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Model Training Background Service was cancelled during delay.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while running Model Training Background Service.");
                }
            }

            _logger.LogInformation("Model Training Background Service is stopping.");
        }
    }
}