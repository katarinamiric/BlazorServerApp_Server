using BlazorServerApp_Server.Data;
using Microsoft.ML;
using System.Text.Json;
using Microsoft.ML.Data;
using BlazorServerApp_Server.Redis;

namespace BlazorServerApp_Server.Services
{
    public class NavigationPredictorService
    {
        private readonly ILogger<NavigationPredictorService> _logger;
        private readonly MLContext _mlContext;
        private ITransformer? _trainedModel;
        private PredictionEngine<AdvancedNavigationLogEntry, NavigationPredictionOutput>? _predictionEngine;
        // Store known page URLs from the training data for prediction input validation/suggestion
        private HashSet<string> _allPossiblePageUrls = new HashSet<string>();
        private readonly RedisPageHistoryService _redisPageHistoryService;

        private const string MODEL_FILE_NAME = "navigation_prediction_model.zip";
        private string ModelPath => Path.Combine(AppContext.BaseDirectory, MODEL_FILE_NAME);

        private const string ROUTES_FILE_NAME = "prerenderable-routes.json";
        private string RoutesFilePath => Path.Combine(AppContext.BaseDirectory, ROUTES_FILE_NAME);

        public NavigationPredictorService(
            ILogger<NavigationPredictorService> logger,
            RedisPageHistoryService redisPageHistoryService) // Inject the service that gets DB data
        {
            _mlContext = new MLContext(seed: 0); // Seed for reproducibility
            _logger = logger;
            _redisPageHistoryService = redisPageHistoryService; // Assign
        }


        // This method will be called periodically by the timer
        public async Task TrainModelPeriodicallyAsync()
        {
            _logger.LogInformation("NavigationPredictorService: Starting periodic model training...");

            // Fetch training data from the database via RedisPageHistoryService
            List<AdvancedNavigationLogEntry> trainingData = await _redisPageHistoryService.GetAllAdvancedNavigationLogEntriesAsync();

            if (trainingData == null || !trainingData.Any())
            {
                _logger.LogWarning("NavigationPredictorService: No training data available from database. Skipping model training.");
                // IMPORTANT: If no data is available, _trainedModel and _predictionEngine will remain null
                // or use the last successfully trained model. Consider a fallback or initial dummy data.
                return;
            }

            // Populate _allPossiblePageUrls from the training data
            _allPossiblePageUrls.Clear();
            foreach (var entry in trainingData)
            {
                _allPossiblePageUrls.Add(entry.PreviousPage1Url);
                _allPossiblePageUrls.Add(entry.PreviousPage2Url);
                _allPossiblePageUrls.Add(entry.PreviousPage3Url);
                _allPossiblePageUrls.Add(entry.NextPageUrl);
            }
            _allPossiblePageUrls.Remove(""); // Remove empty string if present

            _logger.LogInformation(
                $"Loaded {trainingData.Count} advanced training entries from DB. Known pages for prediction: {string.Join(", ", _allPossiblePageUrls)}");

            TrainModel(trainingData); // Call the actual training logic
        }

        private void LoadAllPossiblePageUrls()
        {
            if (File.Exists(RoutesFilePath))
            {
                try
                {
                    var jsonString = File.ReadAllText(RoutesFilePath);

                    var config = JsonSerializer.Deserialize<PrerenderableRoutesConfig>(jsonString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (config?.PrerendableRoutes != null)
                    {
                        _allPossiblePageUrls = config.PrerendableRoutes
                            .Distinct()
                            .OrderBy(url => url)
                            .ToHashSet();

                        _logger.LogInformation(
                            $"Loaded {_allPossiblePageUrls.Count} pre-renderable routes from {ROUTES_FILE_NAME}.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading pre-renderable routes from {ROUTES_FILE_NAME}.");
                }
            }
            else
            {
                _logger.LogWarning(
                    $"Pre-renderable routes file not found at {RoutesFilePath}. Falling back to dynamic detection from test data (less ideal).");
            }


            // Fallback: If JSON not found or failed, derive from test data as before.

            //var tempTestData = LoadTestData(); // Pass true to skip setting _allPossiblePageUrls again
            //_allPossiblePageUrls = tempTestData.Select(e => e.NextPageUrl)
            //    .Distinct()
            //    .OrderBy(url => url)
            //    .ToList();
            //_logger.LogInformation($"Falling back to {_allPossiblePageUrls.Count} routes derived from test data.");
        }

        //private List<AdvancedNavigationLogEntry> LoadTestData()
        //{
        //    var rawData = new List<AdvancedNavigationLogEntry>
        //    {
        //        // User1 (Desktop) - Home -> Weather -> Heavy Report (often afternoon/evening)
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/weather", PreviousPage2Url = "/", PreviousPage3Url = "",
        //            TimeOfDayInHours = 15.0f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/heavy-report"
        //        },
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/weather", PreviousPage2Url = "/", PreviousPage3Url = "",
        //            TimeOfDayInHours = 16.5f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/heavy-report"
        //        },
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/heavy-report", PreviousPage2Url = "/weather", PreviousPage3Url = "/",
        //            TimeOfDayInHours = 17.0f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/product/{id}"
        //        },
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/product/{id}", PreviousPage2Url = "/heavy-report",
        //            PreviousPage3Url = "/weather",
        //            TimeOfDayInHours = 17.1f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/"
        //        },
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/", PreviousPage2Url = "/product/{id}", PreviousPage3Url = "/heavy-report",
        //            TimeOfDayInHours = 17.2f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/weather"
        //        },
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/", PreviousPage2Url = "/product/{id}", PreviousPage3Url = "/heavy-report",
        //            TimeOfDayInHours = 17.2f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/product"
        //        },

        //        // User2 (Mobile) - Home -> Counter -> Home (often morning/lunch)
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/counter", PreviousPage2Url = "/", PreviousPage3Url = "",
        //            TimeOfDayInHours = 9.0f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/"
        //        },
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/", PreviousPage2Url = "/counter", PreviousPage3Url = "",
        //            TimeOfDayInHours = 9.1f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/weather"
        //        },
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/weather", PreviousPage2Url = "/", PreviousPage3Url = "/counter",
        //            TimeOfDayInHours = 12.0f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/weather"
        //        },

        //        // User3 (Tablet) - Quick check of Weather2 (morning)
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/weather2", PreviousPage2Url = "/", PreviousPage3Url = "",
        //            TimeOfDayInHours = 8.3f, UserId = "user3", DeviceType = "Tablet", NextPageUrl = "/weather"
        //        },
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/", PreviousPage2Url = "/weather2", PreviousPage3Url = "",
        //            TimeOfDayInHours = 8.4f, UserId = "user3", DeviceType = "Tablet", NextPageUrl = "/heavy-report"
        //        },

        //        // More data for variety
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/weather-prerendered", PreviousPage2Url = "/", PreviousPage3Url = "/weather",
        //            TimeOfDayInHours = 10.0f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/weather2"
        //        },
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/weather", PreviousPage2Url = "/heavy-report", PreviousPage3Url = "/",
        //            TimeOfDayInHours = 20.0f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/weather2"
        //        },
        //        new AdvancedNavigationLogEntry
        //        {
        //            PreviousPage1Url = "/heavy-report", PreviousPage2Url = "/weather", PreviousPage3Url = "/weather",
        //            TimeOfDayInHours = 21.0f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/"
        //        },
        //    };


        //    _logger.LogInformation(
        //        $"Loaded {rawData.Count} advanced test entries. Known pages for prediction: {string.Join(", ", _allPossiblePageUrls)}");
        //    return rawData;
        //}

        private void TrainModel(List<AdvancedNavigationLogEntry> trainingData)
        {
            IDataView dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = _mlContext.Transforms.Conversion
                .MapValueToKey("Label", "Label")

                // 1. Map string values to numeric keys
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("PreviousPage1UrlKey", "PreviousPage1Url"))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("PreviousPage2UrlKey", "PreviousPage2Url"))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("PreviousPage3UrlKey", "PreviousPage3Url"))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("UserIdKey", "UserId"))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("DeviceTypeKey", "DeviceType"))

                // 2. One-hot encode these numeric keys into sparse vectors
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("PreviousPage1UrlEncoded",
                    "PreviousPage1UrlKey"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("PreviousPage2UrlEncoded",
                    "PreviousPage2UrlKey"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("PreviousPage3UrlEncoded",
                    "PreviousPage3UrlKey"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("UserIdEncoded", "UserIdKey"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("DeviceTypeEncoded", "DeviceTypeKey"))

                // Concatenate all features into a single 'Features' vector required by the trainer
                // Include numerical features directly (TimeOfDayInHours)
                .Append(_mlContext.Transforms.Concatenate("Features",
                    "PreviousPage1UrlEncoded",
                    "PreviousPage2UrlEncoded",
                    "PreviousPage3UrlEncoded",
                    "UserIdEncoded",
                    "DeviceTypeEncoded",
                    "TimeOfDayInHours")) // Numerical feature directly included
                .AppendCacheCheckpoint(_mlContext) // Cache data for faster training

                // Choose your multi-class classification trainer
                // LightGBM is generally fast and accurate
                .Append(_mlContext.MulticlassClassification.Trainers.LightGbm("Label", "Features"))
                // Or FastTree if preferred:
                // .Append(_mlContext.MulticlassClassification.Trainers.FastTreeOva("Label", "Features")) // OVA (One-vs-All) for multi-class
                .Append(_mlContext.Transforms.Conversion
                    .MapKeyToValue("PredictedLabel")); // Map numeric prediction back to original string label

            _logger.LogInformation("NavigationPredictorService: Training advanced ML.NET model...");
            _trainedModel = pipeline.Fit(dataView); // Train the model

            _logger.LogInformation("NavigationPredictorService: Model training complete.");

            // Create a prediction engine for making predictions
            _predictionEngine =
                _mlContext.Model.CreatePredictionEngine<AdvancedNavigationLogEntry, NavigationPredictionOutput>(
                    _trainedModel);
        }


        //private void SaveModel()
        //{
        //    if (_trainedModel == null) return;

        //    _logger.LogInformation($"NavigationPredictorService: Saving model to {ModelPath}");

        //    var trainingData = LoadTestData(); // cuvamo set koji smo koristili za treniranje
        //    IDataView dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        //    // Save the model along with the input schema
        //    _mlContext.Model.Save(_trainedModel, dataView.Schema, ModelPath);
        //}

        //private void LoadModel()
        //{
        //    if (!File.Exists(ModelPath)) return;
        //    _logger.LogInformation($"NavigationPredictorService: Loading model from {ModelPath}");
        //    ITransformer loadedModel = _mlContext.Model.Load(ModelPath, out DataViewSchema modelSchema);

        //    LoadTestData();

        //    _trainedModel = loadedModel;
        //    _predictionEngine =
        //        _mlContext.Model.CreatePredictionEngine<AdvancedNavigationLogEntry, NavigationPredictionOutput>(
        //            _trainedModel);
        //    _logger.LogInformation("NavigationPredictorService: Model loaded successfully.");
        //}

        /// <summary>
        /// Predicts the most likely next pages based on the trained ML.NET model,
        /// considering last 3 pages, time of day, user ID, and device type.
        /// </summary>
        public List<string> PredictNextPage(string previousPage1, string previousPage2, string previousPage3, string userId, string deviceType, int maxPredictions = 3)
        {
            if (_predictionEngine == null)
            {
                _logger.LogWarning("NavigationPredictorService: Model not trained yet. Cannot make prediction.");
                return new List<string>(); // Return empty list
            }

            // Time of day at the moment of prediction
            float timeOfDay = (float)DateTime.UtcNow.TimeOfDay.TotalHours;

            var input = new AdvancedNavigationLogEntry
            {
                PreviousPage1Url = previousPage1,
                PreviousPage2Url = previousPage2,
                PreviousPage3Url = previousPage3,
                UserId = userId,
                DeviceType = deviceType,
                TimeOfDayInHours = timeOfDay
            };

            var prediction = _predictionEngine.Predict(input);

            // Get the slot names (original labels/URLs) corresponding to the scores
            VBuffer<ReadOnlyMemory<char>> slotNames = default;
            _predictionEngine.OutputSchema["Score"].GetSlotNames(ref slotNames);
            var pageUrls = slotNames.GetValues().ToArray() // Use .GetValues() for ReadOnlyMemory<char>
                .Select(charMem => charMem.ToString())
                .ToArray();

            // Combine scores with their corresponding page URLs
            var scoresWithUrls = new List<(string Url, float Score)>();
            for (int i = 0; i < prediction.Scores.Length; i++)
            {
                if (i < pageUrls.Length)
                {
                    scoresWithUrls.Add((pageUrls[i], prediction.Scores[i]));
                }
                else
                {
                    _logger.LogWarning($"Score index {i} out of bounds for known page URLs. This might indicate a mismatch in label mapping.");
                }
            }

            // Order by score descending and take the top N
            var topPredictions = scoresWithUrls
                .OrderByDescending(x => x.Score)
                .Take(maxPredictions)
                .Select(x => x.Url)
                .ToList();

            _logger.LogInformation($"Prediction for User: {userId}, Device: {deviceType}, History: {previousPage3} -> {previousPage2} -> {previousPage1} -> Top {maxPredictions} Predicted: {string.Join(", ", topPredictions)}");

            return topPredictions;
        }
        //Summary
        //To retrain the model in the future
        public void RecordNavigation(
            string previousPage1Url,
            string previousPage2Url,
            string previousPage3Url,
            float timeOfDayInHours,
            string userId,
            string deviceType,
            string nextPageUrl)
        {
            _logger.LogInformation(
                $"Advanced navigation recorded for future ML training: P1:{previousPage1Url}, P2:{previousPage2Url}, P3:{previousPage3Url}, Time:{timeOfDayInHours}, User:{userId}, Device:{deviceType} -> {nextPageUrl}");
            // In a real system, you'd add this to a persistent store (database, file, etc.).
            // You would then periodically re-train the model on this accumulated data.
            // For this example, we're not dynamically updating the in-memory model after initial load.
        }
    }
}