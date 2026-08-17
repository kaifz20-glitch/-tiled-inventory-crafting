using System.Collections.Generic;
using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// Facade that turns system events into analytics events. Attach it to the same
    /// GameObject as <see cref="CraftingSystem"/> and it tracks crafts, failures and
    /// trades automatically. Assign <see cref="Sink"/> to route events anywhere.
    /// </summary>
    [DisallowMultipleComponent]
    public class CraftAnalytics : MonoBehaviour
    {
        private IAnalyticsSink sink = new ConsoleAnalyticsSink();

        /// <summary>Where events go. Assign your provider sink here.</summary>
        public IAnalyticsSink Sink
        {
            get => sink;
            set => sink = value ?? new ConsoleAnalyticsSink();
        }

        private void Awake()
        {
            var crafting = GetComponent<CraftingSystem>();
            if (crafting != null)
            {
                crafting.JobStarted += job => Track("craft.started", job);
                crafting.JobCompleted += job => Track("craft.completed", job);
                crafting.JobFailed += job => Track("craft.failed", job);
            }

            var trading = GetComponent<TradeSystem>();
            if (trading != null)
            {
                trading.OfferCreated += offer => Track("trade.offer_created", new Dictionary<string, object>
                {
                    ["offerId"] = offer.id,
                    ["itemCount"] = offer.offered.Count
                });
                trading.OfferResolved += offer => Track("trade." + offer.state.ToString().ToLowerInvariant(), new Dictionary<string, object>
                {
                    ["offerId"] = offer.id
                });
            }
        }

        private void Track(string eventName, CraftJob job)
        {
            var props = new Dictionary<string, object>
            {
                ["recipe"] = job?.recipe != null ? job.recipe.Id : "unknown"
            };
            if (job?.recipe != null)
            {
                props["recipeName"] = job.recipe.DisplayName;
                props["craftTime"] = job.recipe.CraftTime;
                props["failureChance"] = job.recipe.GetFailureChance(GetComponent<PlayerProfile>()?.CraftingSkill ?? 0);
            }
            Track(eventName, props);
        }

        /// <summary>Track an event manually (e.g. from your own systems).</summary>
        public void Track(string eventName, IReadOnlyDictionary<string, object> properties = null)
        {
            sink.Track(eventName, properties);
        }
    }
}
