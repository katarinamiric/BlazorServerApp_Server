using BlazorServerApp_Server.Data;
using Microsoft.ML;
using System.Text.Json;

namespace BlazorServerApp_Server.Services
{
    // Make sure NavigationLogEntry, NavigationLogEntry, NavigationPredictionOutput are defined or included here
    // as per the previous step.

    public class NavigationPredictorService
    {
        private readonly ILogger<NavigationPredictorService> _logger;
        private readonly MLContext _mlContext;
        private ITransformer? _trainedModel;
        private PredictionEngine<AdvancedNavigationLogEntry, NavigationPredictionOutput>? _predictionEngine;
        private List<string> _allPossiblePageUrls = new List<string>(); // To map scores back to URLs

        // --- Model File Path (Optional for persistence) ---
        private const string MODEL_FILE_NAME = "navigation_prediction_model.zip";
        private string ModelPath => Path.Combine(AppContext.BaseDirectory, MODEL_FILE_NAME);

        private const string ROUTES_FILE_NAME = "prerenderable-routes.json";
        private string RoutesFilePath => Path.Combine(AppContext.BaseDirectory, ROUTES_FILE_NAME);

        public NavigationPredictorService(ILogger<NavigationPredictorService> logger)
        {
            uint nextId = 0;
            var pageToId = new Dictionary<string, uint>();

            _logger = logger;
            LoadAllPossiblePageUrls();


            uint GetPageId(string page)
            {
                if (!pageToId.ContainsKey(page))
                    pageToId[page] = nextId++;
                return pageToId[page];
            }

            var idToPage = pageToId.ToDictionary(kv => kv.Value, kv => kv.Key);

            _mlContext = new MLContext(); // Initialize ML.NET context

            // Attempt to load existing model, otherwise train a new one
            if (File.Exists(ModelPath))
            {
                _logger.LogInformation("NavigationPredictorService: Loading existing ML.NET model...");
                LoadModel();
            }
            else
            {
                _logger.LogInformation(
                    "NavigationPredictorService: No existing model found. Training a new model with test data...");
                var testData = LoadTestData();
                TrainModel(testData);
                SaveModel(); // Save the newly trained model
            }
        }
        private void LoadAllPossiblePageUrls()
        {
            if (File.Exists(RoutesFilePath))
            {
                try
                {
                    var jsonString = File.ReadAllText(RoutesFilePath);

                    // NEW: Deserialize into the PrerenderableRoutesConfig class
                    var config = JsonSerializer.Deserialize<PrerenderableRoutesConfig>(jsonString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // Optional: for flexibility in JSON key casing

                    if (config?.PrerendableRoutes != null) // Check if config and its list are not null
                    {
                        // Extract the list from the deserialized object
                        _allPossiblePageUrls = config.PrerendableRoutes
                            .Distinct()
                            .OrderBy(url => url)
                            .ToList();

                        _logger.LogInformation($"Loaded {_allPossiblePageUrls.Count} pre-renderable routes from {ROUTES_FILE_NAME}.");
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
                _logger.LogWarning($"Pre-renderable routes file not found at {RoutesFilePath}. Falling back to dynamic detection from test data (less ideal).");
            }


            // Fallback: If JSON not found or failed, derive from test data as before.

            //var tempTestData = LoadTestData(); // Pass true to skip setting _allPossiblePageUrls again
            //_allPossiblePageUrls = tempTestData.Select(e => e.NextPageUrl)
            //    .Distinct()
            //    .OrderBy(url => url)
            //    .ToList();
            //_logger.LogInformation($"Falling back to {_allPossiblePageUrls.Count} routes derived from test data.");
        }

        // --- Data Collection (Simulation) ---
        private List<AdvancedNavigationLogEntry> LoadTestData()
        {
            var rawData = new List<AdvancedNavigationLogEntry>
            {
                // User1 (Desktop) - Home -> Weather -> Heavy Report (often afternoon/evening)
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/weather", PreviousPage2Url = "/", PreviousPage3Url = "",
                    TimeOfDayInHours = 15.0f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/heavy-report"
                },
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/weather", PreviousPage2Url = "/", PreviousPage3Url = "",
                    TimeOfDayInHours = 16.5f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/heavy-report"
                },
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/heavy-report", PreviousPage2Url = "/weather", PreviousPage3Url = "/",
                    TimeOfDayInHours = 17.0f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/product/{id}"
                },
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/product/{id}", PreviousPage2Url = "/heavy-report", PreviousPage3Url = "/weather",
                    TimeOfDayInHours = 17.1f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/"
                },
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/", PreviousPage2Url = "/product/{id}", PreviousPage3Url = "/heavy-report",
                    TimeOfDayInHours = 17.2f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/weather"
                },
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/", PreviousPage2Url = "/product/{id}", PreviousPage3Url = "/heavy-report",
                    TimeOfDayInHours = 17.2f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/product"
                },

                // User2 (Mobile) - Home -> Counter -> Home (often morning/lunch)
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/counter", PreviousPage2Url = "/", PreviousPage3Url = "",
                    TimeOfDayInHours = 9.0f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/"
                },
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/", PreviousPage2Url = "/counter", PreviousPage3Url = "",
                    TimeOfDayInHours = 9.1f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/weather"
                },
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/weather", PreviousPage2Url = "/", PreviousPage3Url = "/counter",
                    TimeOfDayInHours = 12.0f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/weather"
                },

                // User3 (Tablet) - Quick check of Weather2 (morning)
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/weather2", PreviousPage2Url = "/", PreviousPage3Url = "",
                    TimeOfDayInHours = 8.3f, UserId = "user3", DeviceType = "Tablet", NextPageUrl = "/weather"
                },
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/", PreviousPage2Url = "/weather2", PreviousPage3Url = "",
                    TimeOfDayInHours = 8.4f, UserId = "user3", DeviceType = "Tablet", NextPageUrl = "/heavy-report"
                },

                // More data for variety
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/weather-prerendered", PreviousPage2Url = "/", PreviousPage3Url = "/weather",
                    TimeOfDayInHours = 10.0f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/weather2"
                },
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/weather", PreviousPage2Url = "/heavy-report", PreviousPage3Url = "/",
                    TimeOfDayInHours = 20.0f, UserId = "user2", DeviceType = "Mobile", NextPageUrl = "/weather2"
                },
                new AdvancedNavigationLogEntry
                {
                    PreviousPage1Url = "/heavy-report", PreviousPage2Url = "/weather", PreviousPage3Url = "/weather",
                    TimeOfDayInHours = 21.0f, UserId = "user1", DeviceType = "Desktop", NextPageUrl = "/"
                },
            };

     
        _logger.LogInformation(
                $"Loaded {rawData.Count} advanced test entries. Known pages for prediction: {string.Join(", ", _allPossiblePageUrls)}");
            return rawData;
        }

        // --- ML.NET Model Training ---

        private void TrainModel(List<AdvancedNavigationLogEntry> trainingData)
        {
            // Convert list to IDataView (ML.NET's data representation)
            IDataView dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Define the ML.NET training pipeline
            var pipeline = _mlContext.Transforms.Conversion
                .MapValueToKey("Label", "Label") // Map NextPageUrl (Label) to numeric keys

                // Feature Engineering for Categorical Features (URLs, UserId, DeviceType)
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


        private void SaveModel()
        {
            if (_trainedModel == null) return;

            _logger.LogInformation($"NavigationPredictorService: Saving model to {ModelPath}");

            // Create schema from the training data (or re-create if needed)
            var trainingData = LoadTestData(); // Ensure it matches what was used during training
            IDataView dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Save the model along with the input schema
            _mlContext.Model.Save(_trainedModel, dataView.Schema, ModelPath);
        }



        private void LoadModel()
        {
            if (!File.Exists(ModelPath)) return;
            _logger.LogInformation($"NavigationPredictorService: Loading model from {ModelPath}");
            ITransformer loadedModel = _mlContext.Model.Load(ModelPath, out DataViewSchema modelSchema);

            // Re-load test data to get _allPossiblePageUrls (simplistic for this example)
            // In production, this list would be derived from your known routes or saved with the model.
            LoadTestData();

            _trainedModel = loadedModel;
            _predictionEngine =
                _mlContext.Model.CreatePredictionEngine<AdvancedNavigationLogEntry, NavigationPredictionOutput>(
                    _trainedModel);
            _logger.LogInformation("NavigationPredictorService: Model loaded successfully.");
        }

        // --- Prediction Logic ---
        /// <summary>
        /// Predicts the most likely next pages based on the trained ML.NET model,
        /// considering last 3 pages, time of day, user ID, and device type.
        /// </summary>
        public IEnumerable<string> PredictNextPages(
            string currentPageUrl, // This is PreviousPage1Url in the model
            string previousPage2Url,
            string previousPage3Url,
            float timeOfDayInHours,
            string userId,
            string deviceType,
            int maxPredictions = 3)
        {
            if (_predictionEngine == null || _allPossiblePageUrls == null || !_allPossiblePageUrls.Any())
            {
                _logger.LogWarning("Prediction engine or label mapping not initialized. Cannot make predictions.");
                return Enumerable.Empty<string>();
            }

            // Create input for prediction
            var input = new AdvancedNavigationLogEntry
            {
                PreviousPage1Url = currentPageUrl, // Current page is the most recent previous page
                PreviousPage2Url = previousPage2Url,
                PreviousPage3Url = previousPage3Url,
                TimeOfDayInHours = timeOfDayInHours,
                UserId = userId,
                DeviceType = deviceType
            };

            // Predict. This will give scores for ALL possible next pages.
            NavigationPredictionOutput prediction = _predictionEngine.Predict(input);

            // Map scores back to Page URLs
            var scoresWithUrls = new List<(string Url, float Score)>();
            for (int i = 0; i < prediction.Scores.Length; i++)
            {
                if (i < _allPossiblePageUrls.Count)
                {
                    scoresWithUrls.Add((_allPossiblePageUrls[i], prediction.Scores[i]));
                }
                else
                {
                    _logger.LogWarning(
                        $"Score index {i} out of bounds for known URLs. This might indicate a mismatch between training labels and prediction output.");
                }
            }

            // Order by score descending and take the top N
            var topPredictions = scoresWithUrls
                .OrderByDescending(x => x.Score)
                .Take(maxPredictions)
                .Select(x => x.Url)
                .ToList();

            _logger.LogInformation(
                $"ML.NET Prediction for P1:'{currentPageUrl}', P2:'{previousPage2Url}', P3:'{previousPage3Url}', Time:{timeOfDayInHours}, User:{userId}, Device:{deviceType}: {string.Join(", ", topPredictions)}");
            return topPredictions;
        }

        // --- Live Data Tracking (for future model re-training) ---
        // This method would need to capture all the new features for future training
        public void RecordNavigation(
            string previousPage1Url, // Current page when navigation occurred
            string previousPage2Url,
            string previousPage3Url,
            float timeOfDayInHours,
            string userId,
            string deviceType,
            string nextPageUrl) // The page the user actually navigated to
        {
            _logger.LogInformation(
                $"Advanced navigation recorded for future ML training: P1:{previousPage1Url}, P2:{previousPage2Url}, P3:{previousPage3Url}, Time:{timeOfDayInHours}, User:{userId}, Device:{deviceType} -> {nextPageUrl}");
            // In a real system, you'd add this to a persistent store (database, file, etc.).
            // You would then periodically re-train the model on this accumulated data.
            // For this example, we're not dynamically updating the in-memory model after initial load.
        }
    }
}