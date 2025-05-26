// BlazorServerApp_Server/Services/PrerenderRegistry.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components; // For IComponent

namespace BlazorServerApp_Server.Services
{

    namespace BlazorServerApp_Server.Services
    {
        public class PrerenderRegistry
        {
            // Stores the Type of components that are currently registered for active prerendering.
            // Using ConcurrentDictionary as a thread-safe hash set for fast lookups and concurrent access.
            private readonly ConcurrentDictionary<Type, byte> _activePrerenderedPages = new ConcurrentDictionary<Type, byte>();

            // This internal mapping defines which database tables affect which Blazor component types.
            // This is crucial for the SqlServiceBrokerListener to know what to re-prerender.
            private readonly Dictionary<string, Type> _tableToComponentMapping = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            {"Weather", typeof(Components.Pages.Weather)}, // If WeatherForecast table changes, it affects Weather page
            // {"Products", typeof(Components.Pages.ProductList)}, // Example: Products table affects ProductList page
            // {"BlogPosts", typeof(Components.Pages.BlogArticle)}, // Example: BlogPosts table affects BlogArticle page
            // Add more mappings as your application grows
        };

            /// <summary>
            /// Registers a Blazor component type as actively being prerendered.
            /// When this page's data changes, it should be considered for re-prerendering.
            /// </summary>
            public void RegisterPageForPrerendering(Type componentType)
            {
                if (!typeof(IComponent).IsAssignableFrom(componentType))
                {
                    throw new ArgumentException("Provided type must be a Blazor component.", nameof(componentType));
                }
                _activePrerenderedPages.TryAdd(componentType, 0); // Value doesn't matter, just using it as a set
                Console.WriteLine($"[PrerenderRegistry] Registered: {componentType.Name}");
            }

            /// <summary>
            /// Unregisters a Blazor component type from active prerendering.
            /// If this page's data changes, it will no longer trigger re-prerendering unless re-registered.
            /// </summary>
            public void UnregisterPageForPrerendering(Type componentType)
            {
                _activePrerenderedPages.TryRemove(componentType, out _);
                Console.WriteLine($"[PrerenderRegistry] Unregistered: {componentType.Name}");
            }

            /// <summary>
            /// Checks if a Blazor component type is currently registered for active prerendering.
            /// </summary>
            public bool IsPageActivelyPrerendered(Type componentType)
            {
                return _activePrerenderedPages.ContainsKey(componentType);
            }

            /// <summary>
            /// Gets the Blazor component Type associated with a given database table name.
            /// </summary>
            public Type? GetComponentTypeForTable(string tableName)
            {
                _tableToComponentMapping.TryGetValue(tableName, out var componentType);
                return componentType;
            }

            // Optional: Get all currently registered pages (for debugging/monitoring)
            public IEnumerable<Type> GetActivePrerenderedPages()
            {
                return _activePrerenderedPages.Keys;
            }
        }
    }
}
