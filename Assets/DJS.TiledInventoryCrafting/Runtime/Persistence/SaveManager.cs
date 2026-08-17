using System;
using System.Collections.Generic;
using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// Snapshots the whole game state (profile, all grids, crafting queue, cooldowns,
    /// achievement progress) into a JSON blob and restores it. The storage backend is
    /// swappable — local file by default, SyncVault (or any cloud store) via
    /// <see cref="ISaveBackend"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private CraftingSystem crafting;
        [SerializeField] private PlayerProfile profile;
        [SerializeField] private AchievementTracker achievements;

        [Tooltip("Save slot name. \"autosave\" is used when AutoSave is on.")]
        [SerializeField] private string slot = "save1";

        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 60f;

        private ISaveBackend backend;
        private float nextAutoSave;

        /// <summary>Storage backend. Defaults to a local file. Assign a cloud backend to persist remotely.</summary>
        public ISaveBackend Backend
        {
            get => backend ?? (backend = new LocalFileSaveBackend());
            set => backend = value;
        }

        public string Slot { get => slot; set => slot = value; }

        public event Action<bool> Saved;
        public event Action<bool> Loaded;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<InventorySystem>();
            if (crafting == null) crafting = GetComponent<CraftingSystem>();
            if (profile == null) profile = GetComponent<PlayerProfile>();
            if (achievements == null) achievements = GetComponent<AchievementTracker>();
            backend = new LocalFileSaveBackend();
        }

        private void Update()
        {
            if (!autoSave) return;
            if (Time.unscaledTime >= nextAutoSave)
            {
                nextAutoSave = Time.unscaledTime + autoSaveInterval;
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            if (autoSave) Save();
        }

        // ------------------------------------------------------------------ save

        /// <summary>Serialize the current state and hand it to the backend.</summary>
        public bool Save()
        {
            var data = Capture();
            data.savedAtUtc = DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            bool ok = Backend.Save(slot, json);
            Saved?.Invoke(ok);
            return ok;
        }

        private GameSaveData Capture()
        {
            var data = new GameSaveData { version = "1.0.0" };
            if (profile != null)
            {
                data.profile.level = profile.Level;
                data.profile.xp = profile.Xp;
                data.profile.gold = profile.Gold;
                data.profile.craftingSkill = profile.CraftingSkill;
            }

            if (inventory != null)
                foreach (var grid in inventory.Grids)
                    data.grids.Add(CaptureGrid(grid));

            if (crafting != null)
            {
                foreach (var job in crafting.Queue)
                {
                    if (job.recipe == null) continue;
                    data.craftQueue.Add(new CraftJobSaveData
                    {
                        recipeId = job.recipe.Id,
                        remainingSeconds = job.remainingSeconds,
                        totalSeconds = job.totalSeconds
                    });
                }
                foreach (var kv in crafting.GetCooldowns())
                    data.cooldowns.Add(new CooldownSaveData
                    {
                        recipeId = kv.Key,
                        endsAtUnscaledTime = kv.Value
                    });
            }

            if (achievements != null)
            {
                data.unlockedAchievements.AddRange(achievements.UnlockedIds);
                foreach (var kv in achievements.GetStats())
                    data.achievementStats.Add(new StatEntry { key = kv.Key, value = kv.Value });
            }

            return data;
        }

        private static GridSaveData CaptureGrid(InventoryGrid grid)
        {
            var data = new GridSaveData
            {
                gridName = grid.GridName,
                width = grid.Size.x,
                height = grid.Size.y
            };
            for (int i = 0; i < grid.Count; i++)
            {
                var slot = grid.GetSlot(i);
                var s = new SlotSaveData
                {
                    locked = slot.restriction.locked,
                    equipmentSlotType = slot.restriction.equipmentSlotType.ToString()
                };
                foreach (var cat in slot.restriction.allowedCategories)
                    s.allowedCategories.Add(cat.ToString());
                foreach (var item in slot.restriction.allowedItems)
                    if (item != null) s.allowedItems.Add(item.Id);
                if (!slot.IsEmpty)
                {
                    s.itemId = slot.stack.item.Id;
                    s.count = slot.stack.count;
                }
                data.slots.Add(s);
            }
            return data;
        }

        // ------------------------------------------------------------------ load

        /// <summary>Load from the backend and restore every system. Returns success.</summary>
        public bool Load()
        {
            if (!Backend.Load(slot, out string json)) { Loaded?.Invoke(false); return false; }
            try
            {
                var data = JsonUtility.FromJson<GameSaveData>(json);
                bool ok = Restore(data);
                Loaded?.Invoke(ok);
                return ok;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DJS.TiledInventoryCrafting] Failed to parse save '{slot}': {e.Message}");
                Loaded?.Invoke(false);
                return false;
            }
        }

        private bool Restore(GameSaveData data)
        {
            if (data == null) return false;

            if (profile != null)
                profile.SetAll(data.profile.level, data.profile.xp, data.profile.gold, data.profile.craftingSkill);

            if (inventory != null)
            {
                foreach (var gridData in data.grids)
                {
                    var grid = inventory.GetOrCreateGrid(gridData.gridName, new Vector2Int(gridData.width, gridData.height));
                    if (grid.Size != new Vector2Int(gridData.width, gridData.height))
                        grid.Resize(new Vector2Int(gridData.width, gridData.height));
                    for (int i = 0; i < grid.Count && i < gridData.slots.Count; i++)
                    {
                        var slot = grid.GetSlot(i);
                        var s = gridData.slots[i];
                        slot.restriction.locked = s.locked;
                        if (Enum.TryParse(s.equipmentSlotType, out EquipmentSlotType eqType))
                            slot.restriction.equipmentSlotType = eqType;
                        slot.restriction.allowedCategories.Clear();
                        foreach (var cat in s.allowedCategories)
                            if (Enum.TryParse(cat, out ItemCategory c)) slot.restriction.allowedCategories.Add(c);
                        slot.restriction.allowedItems.Clear();
                        foreach (var itemId in s.allowedItems)
                        {
                            var restrictedItem = Registry.FindItem(itemId);
                            if (restrictedItem != null) slot.restriction.allowedItems.Add(restrictedItem);
                        }
                        var stackItem = string.IsNullOrEmpty(s.itemId) ? null : Registry.FindItem(s.itemId);
                        slot.stack = stackItem != null && s.count > 0 ? new ItemStack(stackItem, s.count) : ItemStack.Empty;
                    }
                    grid.EmitChanged();
                }
            }

            if (crafting != null)
            {
                crafting.RestoreQueue(data.craftQueue, Registry.FindRecipe);
                crafting.RestoreCooldowns(data.cooldowns);
            }

            if (achievements != null)
                achievements.Restore(data.unlockedAchievements, data.achievementStats);

            return true;
        }

        /// <summary>Delete the current slot via the backend.</summary>
        public bool DeleteSave()
        {
            bool ok = Backend.Delete(slot);
            if (ok) Debug.Log($"[DJS.TiledInventoryCrafting] Deleted save slot '{slot}'.");
            return ok;
        }
    }
}
