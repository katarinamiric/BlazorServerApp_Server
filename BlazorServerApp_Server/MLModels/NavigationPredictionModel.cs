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
        // Features (Inputs to the model)
        [LoadColumn(0)] public string PreviousPage1Url { get; set; } = string.Empty; // Most recent previous page
        [LoadColumn(1)] public string PreviousPage2Url { get; set; } = string.Empty; // Second most recent
        [LoadColumn(2)] public string PreviousPage3Url { get; set; } = string.Empty; // Third most recent

        [LoadColumn(3)] public float TimeOfDayInHours { get; set; } // e.g., 14.5 for 2:30 PM
        [LoadColumn(4)] public string UserId { get; set; } = string.Empty;
        [LoadColumn(5)] public string DeviceType { get; set; } = string.Empty; // e.g., "Desktop", "Mobile", "Tablet"

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