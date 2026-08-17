using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiledInventory
{
    public enum CraftJobState
    {
        Queued,
        Crafting,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>One entry in the crafting queue.</summary>
    [Serializable]
    public class CraftJob
    {
        public RecipeDefinition recipe;
        public float remainingSeconds;
        public float totalSeconds;
        public CraftJobState state;

        public float Progress => totalSeconds <= 0f ? 1f : Mathf.Clamp01(1f - remainingSeconds / totalSeconds);
    }

    /// <summary>Serializable snapshot of one queue entry, used by the save system.</summary>
    [Serializable]
    public class CraftJobSaveData
    {
        public string recipeId;
        public float remainingSeconds;
        public float totalSeconds;
    }

    /// <summary>Why a craft could not be queued — surfaced to the UI for tooltips.</summary>
    public enum CraftRejection
    {
        None,
        MissingMaterials,
        LevelTooLow,
        NotEnoughGold,
        NotEnoughXp,
        OnCooldown
    }

    /// <summary>
    /// Executes recipes against an inventory and profile: validates requirements,
    /// consumes costs at queue time, runs a sequential queue with craft timers,
    /// rolls failure, grants outputs and fires events. All timers use unscaled time
    /// so the queue keeps running while the game is paused.
    /// </summary>
    [DisallowMultipleComponent]
    public class CraftingSystem : MonoBehaviour
    {
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private PlayerProfile profile;

        private readonly List<CraftJob> queue = new List<CraftJob>();
        private readonly Dictionary<string, float> cooldownUntil = new Dictionary<string, float>();

        /// <summary>The queue in order, including the in-progress craft at index 0.</summary>
        public IReadOnlyList<CraftJob> Queue => queue;

        public event Action<CraftJob> JobStarted;
        public event Action<CraftJob> JobCompleted;
        public event Action<CraftJob> JobFailed;
        public event Action<CraftJob> JobCancelled;
        public event Action QueueChanged;

        public InventorySystem Inventory => inventory;
        public PlayerProfile Profile => profile;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<InventorySystem>();
            if (profile == null) profile = GetComponent<PlayerProfile>();
        }

        private void Update()
        {
            if (queue.Count == 0) return;
            var job = queue[0];
            if (job.state == CraftJobState.Queued)
            {
                job.state = CraftJobState.Crafting;
                JobStarted?.Invoke(job);
                QueueChanged?.Invoke();
            }
            if (job.state != CraftJobState.Crafting) return;

            job.remainingSeconds -= Time.unscaledDeltaTime;
            if (job.remainingSeconds > 0f) return;

            FinishJob(job);
        }

        private void FinishJob(CraftJob job)
        {
            queue.RemoveAt(0);

            bool failed = job.recipe != null && UnityEngine.Random.value < job.recipe.GetFailureChance(profile != null ? profile.CraftingSkill : 0);
            if (job.recipe != null && job.recipe.CooldownSeconds > 0f)
                cooldownUntil[job.recipe.Id] = Time.unscaledTime + job.recipe.CooldownSeconds;

            if (failed)
            {
                job.state = CraftJobState.Failed;
                job.remainingSeconds = 0f;
                JobFailed?.Invoke(job);
            }
            else
            {
                job.state = CraftJobState.Completed;
                job.remainingSeconds = 0f;
                if (job.recipe != null && inventory != null)
                    foreach (var output in job.recipe.Outputs)
                        inventory.AddItem(output);
                JobCompleted?.Invoke(job);
            }
            QueueChanged?.Invoke();
        }

        /// <summary>Validate a recipe without any side effects — safe to call from UI checks.</summary>
        public CraftRejection CanQueue(RecipeDefinition recipe)
        {
            if (recipe == null) return CraftRejection.MissingMaterials;
            if (inventory == null || profile == null) return CraftRejection.MissingMaterials;

            if (profile.Level < recipe.LevelRequirement) return CraftRejection.LevelTooLow;
            if (profile.Gold < recipe.GoldCost) return CraftRejection.NotEnoughGold;
            if (profile.Xp < recipe.XpCost) return CraftRejection.NotEnoughXp;
            if (GetCooldownRemaining(recipe) > 0f) return CraftRejection.OnCooldown;
            if (!recipe.HasMaterials(inventory.MainGrid)) return CraftRejection.MissingMaterials;
            return CraftRejection.None;
        }

        /// <summary>
        /// Validate and queue a recipe. Costs (materials, gold, XP, special costs) are
        /// consumed immediately; the craft itself starts when its turn in the queue arrives.
        /// </summary>
        public CraftRejection TryQueue(RecipeDefinition recipe)
        {
            var rejection = CanQueue(recipe);
            if (rejection != CraftRejection.None) return rejection;

            // consume everything up front
            foreach (var input in recipe.Inputs)
                inventory.MainGrid.Consume(input.item, input.count);
            foreach (var cost in recipe.SpecialCosts)
                inventory.MainGrid.Consume(cost.item, cost.count);
            if (recipe.GoldCost > 0) profile.SpendGold(recipe.GoldCost);
            if (recipe.XpCost > 0) profile.SpendXp(recipe.XpCost);

            var job = new CraftJob
            {
                recipe = recipe,
                totalSeconds = recipe.CraftTime,
                remainingSeconds = recipe.CraftTime,
                state = CraftJobState.Queued
            };
            queue.Add(job);
            QueueChanged?.Invoke();
            return CraftRejection.None;
        }

        /// <summary>Cancel a queued (not yet started) job. Materials are NOT refunded — documented behaviour.</summary>
        public bool Cancel(int queueIndex)
        {
            if (queueIndex < 0 || queueIndex >= queue.Count) return false;
            var job = queue[queueIndex];
            if (job.state != CraftJobState.Queued) return false;
            job.state = CraftJobState.Cancelled;
            queue.RemoveAt(queueIndex);
            JobCancelled?.Invoke(job);
            QueueChanged?.Invoke();
            return true;
        }

        public void CancelAll()
        {
            for (int i = queue.Count - 1; i >= 0; i--)
                if (queue[i].state == CraftJobState.Queued)
                {
                    queue[i].state = CraftJobState.Cancelled;
                    JobCancelled?.Invoke(queue[i]);
                }
            queue.Clear();
            QueueChanged?.Invoke();
        }

        /// <summary>Seconds left before a recipe can be queued again (0 = ready).</summary>
        public float GetCooldownRemaining(RecipeDefinition recipe)
        {
            if (recipe == null) return 0f;
            if (!cooldownUntil.TryGetValue(recipe.Id, out float end)) return 0f;
            float remaining = end - Time.unscaledTime;
            if (remaining <= 0f) { cooldownUntil.Remove(recipe.Id); return 0f; }
            return remaining;
        }

        public bool IsCrafting => queue.Count > 0 && queue[0].state == CraftJobState.Crafting;
        public int QueuedCount => queue.Count;

        // --- save/load support --------------------------------------------------

        public IReadOnlyDictionary<string, float> GetCooldowns() => cooldownUntil;

        /// <summary>Restore cooldown state from save data.</summary>
        public void RestoreCooldowns(List<CooldownSaveData> cooldowns)
        {
            cooldownUntil.Clear();
            if (cooldowns == null) return;
            foreach (var cd in cooldowns)
                if (cd != null && !string.IsNullOrEmpty(cd.recipeId))
                    cooldownUntil[cd.recipeId] = cd.endsAtUnscaledTime;
        }

        /// <summary>Rebuild the queue from save data. Recipes are resolved by id.</summary>
        public void RestoreQueue(List<CraftJobSaveData> savedJobs, Func<string, RecipeDefinition> resolver)
        {
            queue.Clear();
            foreach (var saved in savedJobs)
            {
                if (saved == null) continue;
                var recipe = resolver != null ? resolver(saved.recipeId) : null;
                if (recipe == null) continue;
                queue.Add(new CraftJob
                {
                    recipe = recipe,
                    totalSeconds = saved.totalSeconds > 0f ? saved.totalSeconds : recipe.CraftTime,
                    remainingSeconds = saved.remainingSeconds,
                    state = CraftJobState.Queued
                });
            }
            if (queue.Count > 0)
                queue[0].state = CraftJobState.Crafting;
            QueueChanged?.Invoke();
        }
    }
}
