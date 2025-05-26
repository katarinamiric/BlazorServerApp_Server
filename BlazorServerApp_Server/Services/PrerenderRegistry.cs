using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace BlazorServerApp_Server.Services
{
    namespace BlazorServerApp_Server.Services
    {
        public class PrerenderRegistry
        {
            private readonly ConcurrentDictionary<Type, byte> _activePrerenderedPages =
                new ConcurrentDictionary<Type, byte>();

            private readonly Dictionary<string, Type> _tableToComponentMapping =
                new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Weather", typeof(Components.Pages.Weather) },
                };


            public void RegisterPageForPrerendering(Type componentType)
            {
                if (!typeof(IComponent).IsAssignableFrom(componentType))
                {
                    throw new ArgumentException("Provided type must be a Blazor component.", nameof(componentType));
                }

                _activePrerenderedPages.TryAdd(componentType, 0);
                Console.WriteLine($"[PrerenderRegistry] Registered: {componentType.Name}");
            }

            public void UnregisterPageForPrerendering(Type componentType)
            {
                _activePrerenderedPages.TryRemove(componentType, out _);
                Console.WriteLine($"[PrerenderRegistry] Unregistered: {componentType.Name}");
            }

            public bool IsPageActivelyPrerendered(Type componentType)
            {
                return _activePrerenderedPages.ContainsKey(componentType);
            }

            public Type? GetComponentTypeForTable(string tableName)
            {
                _tableToComponentMapping.TryGetValue(tableName, out var componentType);
                return componentType;
            }

            public IEnumerable<Type> GetActivePrerenderedPages()
            {
                return _activePrerenderedPages.Keys;
            }
        }
    }
}