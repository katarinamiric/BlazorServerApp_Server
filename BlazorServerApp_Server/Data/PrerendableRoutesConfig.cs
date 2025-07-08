namespace BlazorServerApp_Server.Data
{
    // In a new file, or at the top of your NavigationPredictorService.cs
    public class PrerenderableRoutesConfig
    {
        // The property name must match the JSON key "prerendable-routes"
        // Case-insensitivity is usually handled by JsonSerializerOptions,
        // but exact match is safest.
        [System.Text.Json.Serialization.JsonPropertyName("prerendable-routes")]
        public List<string>? PrerendableRoutes { get; set; }
    }
}
