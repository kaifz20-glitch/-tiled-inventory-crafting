using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiledInventory
{
    /// <summary>
    /// Tracks stat counters (craft counts, failures, ...), checks them against every
    /// registered <see cref="AchievementDefinition"/> and raises unlock events.
    /// Counter state is included in saves.
    /// </summary>
    [DisallowMultipleComponent]
    public class AchievementTracker : MonoBehaviour
    {
        [SerializeField] private List<AchievementDefinition> achievements = new List<AchievementDefinition>();

        private readonly Dictionary<string, int> stats = new Dictionary<string, int>();
        private readonly HashSet<string> unlocked = new HashSet<string>();

        /// <summary>Raised when an achievement unlocks (arg = the definition).</summary>
        public event Action<AchievementDefinition> Unlocked;

        public IReadOnlyCollection<string> UnlockedIds => unlocked;

        private void Awake()
        {
            // Auto-wire to crafting events when a CraftingSystem is present.
            var crafting = GetComponent<CraftingSystem>();
            if (crafting != null)
            {
                crafting.JobCompleted += job => AddStat("craft.total", 1, job?.recipe);
                crafting.JobFailed += job => AddStat("craft.failed", 1, job?.recipe);
            }
        }

        /// <summary>Register achievements to check (also happens through inspector list).</summary>
        public void RegisterAchievements(IEnumerable<AchievementDefinition> definitions)
        {
            achievements.Clear();
            achievements.AddRange(definitions);
            CheckAll();
        }

        /// <summary>Increment a stat counter and re-check achievements. <paramref name="context"/>
        /// optionally names the recipe for per-item stats like "craft.<recipeId>".</summary>
        public void AddStat(string key, int amount = 1, RecipeDefinition context = null)
        {
            if (string.IsNullOrEmpty(key)) return;
            stats.TryGetValue(key, out int current);
            stats[key] = current + amount;

            if (context != null)
            {
                string scoped = key + "." + context.Id;
                stats.TryGetValue(scoped, out int scopedCurrent);
                stats[scoped] = scopedCurrent + amount;
                CheckAll();
            }
            CheckAll();
        }

        public int GetStat(string key) => stats.TryGetValue(key, out int v) ? v : 0;

        public bool IsUnlocked(AchievementDefinition achievement) =>
            achievement != null && unlocked.Contains(achievement.Id);

        private void CheckAll()
        {
            bool any = false;
            foreach (var achievement in achievements)
            {
                if (achievement == null || unlocked.Contains(achievement.Id)) continue;
                if (GetStat(achievement.StatKey) >= achievement.TargetValue)
                {
                    unlocked.Add(achievement.Id);
                    Unlocked?.Invoke(achievement);
                    any = true;
                }
            }
            if (any)
                Debug.Log($"[TiledInventory] Achievement unlocked: {string.Join(", ", unlocked)}");
        }

        /// <summary>Snapshot of counters (for save files).</summary>
        public IEnumerable<KeyValuePair<string, int>> GetStats()
        {
            foreach (var kv in stats) yield return kv;
        }

        /// <summary>Restore counter and unlock state from a save.</summary>
        public void Restore(List<string> unlockedIds, List<StatEntry> statEntries)
        {
            unlocked.Clear();
            stats.Clear();
            if (unlockedIds != null)
                foreach (var id in unlockedIds) unlocked.Add(id);
            if (statEntries != null)
                foreach (var entry in statEntries)
                    if (entry != null && !string.IsNullOrEmpty(entry.key))
                        stats[entry.key] = entry.value;
        }
    }
}
