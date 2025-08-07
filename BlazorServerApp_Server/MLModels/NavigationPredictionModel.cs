using Microsoft.ML.Data;
using System;

namespace BlazorServerApp_Server.Services // Or BlazorServerApp_Server.MLModels if you prefer
{
    /// <summary>
    /// Represents a single historical navigation log entry for training the ML model,
    /// including previous pages, time of day, user ID, and device type.
    /// </summary>
    public class AdvancedNavigationLogEntry
    {
        public int Id { get; set; }
        // Features (Inputs to the model)
        [LoadColumn(0)] public string PreviousPage1Url { get; set; } = string.Empty;
        [LoadColumn(1)] public string PreviousPage2Url { get; set; } = string.Empty;
        [LoadColumn(2)] public string PreviousPage3Url { get; set; } = string.Empty;

        [LoadColumn(3)] public float TimeOfDayInHours { get; set; }
        [LoadColumn(4)] public string UserId { get; set; } = string.Empty;
        [LoadColumn(5)] public string DeviceType { get; set; } = string.Empty;
        [LoadColumn(6)] public string UserGender { get; set; } = string.Empty;


        // Label (Output for the model to predict)
        [LoadColumn(6)]
        [ColumnName("Label")] // ML.NET expects the target column to be named "Label"
        public string NextPageUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the input features for making a single prediction with the trained model.
    /// </summary>
    public class AdvancedNavigationPredictionInput
    {
        [ColumnName("PreviousPage1Url")] public string PreviousPage1Url { get; set; } = string.Empty;
        [ColumnName("PreviousPage2Url")] public string PreviousPage2Url { get; set; } = string.Empty;
        [ColumnName("PreviousPage3Url")] public string PreviousPage3Url { get; set; } = string.Empty;
        [ColumnName("TimeOfDayInHours")] public float TimeOfDayInHours { get; set; }
        [ColumnName("UserId")] public string UserId { get; set; } = string.Empty;
        [ColumnName("DeviceType")] public string DeviceType { get; set; } = string.Empty;
        [ColumnName("UserGender")] public string UserGender { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the output of the ML.NET prediction.
    /// </summary>
    public class NavigationPredictionOutput
    {
        // PredictedLabel will hold the most likely next page URL (string).
        [ColumnName("PredictedLabel")]
        public string PredictedNextPageUrl { get; set; } = string.Empty;

        // Scores will hold the probability or confidence score for each possible next page.
        // The index of the array corresponds to a mapped key of the page URL.
        [ColumnName("Score")]
        public float[] Scores { get; set; } = Array.Empty<float>();
    }
}