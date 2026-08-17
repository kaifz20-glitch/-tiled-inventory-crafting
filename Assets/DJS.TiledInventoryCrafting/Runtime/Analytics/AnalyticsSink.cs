using System.Collections.Generic;
using UnityEngine;

namespace TiledInventory
{
    /// <summary>
    /// Receives gameplay events (crafts, failures, trades, ...). Implement this to push
    /// events to your analytics provider (GameAnalytics, Unity Analytics, Amplitude, a
    /// custom HTTP endpoint, ...). <see cref="ConsoleAnalyticsSink"/> is the shipped default.
    /// </summary>
    public interface IAnalyticsSink
    {
        void Track(string eventName, IReadOnlyDictionary<string, object> properties);
    }

    /// <summary>Prints events to the console. Swap for a real sink in production.</summary>
    public class ConsoleAnalyticsSink : IAnalyticsSink
    {
        public void Track(string eventName, IReadOnlyDictionary<string, object> properties)
        {
            if (properties == null || properties.Count == 0)
            {
                Debug.Log($"[Analytics] {eventName}");
                return;
            }
            var parts = new List<string>(properties.Count);
            foreach (var kv in properties) parts.Add($"{kv.Key}={kv.Value}");
            Debug.Log($"[Analytics] {eventName} ({string.Join(", ", parts)})");
        }
    }
}
