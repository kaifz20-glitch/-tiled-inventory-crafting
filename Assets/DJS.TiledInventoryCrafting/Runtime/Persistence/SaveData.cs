using System;
using System.Collections.Generic;

namespace TiledInventory
{
    // Plain field-based DTOs so JsonUtility can round-trip them without any
    // third-party serializers. Dictionaries are avoided on purpose.

    [Serializable]
    public class ProfileSaveData
    {
        public int level = 1;
        public int xp;
        public int gold;
        public int craftingSkill = 1;
    }

    [Serializable]
    public class SlotSaveData
    {
        public string itemId;
        public int count;
        public bool locked;
        public string equipmentSlotType = "None";
        public List<string> allowedCategories = new List<string>();
        public List<string> allowedItems = new List<string>();
    }

    [Serializable]
    public class GridSaveData
    {
        public string gridName;
        public int width;
        public int height;
        public List<SlotSaveData> slots = new List<SlotSaveData>();
    }

    [Serializable]
    public class StatEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class GameSaveData
    {
        public string version = "1.0.0";
        public string savedAtUtc;
        public ProfileSaveData profile = new ProfileSaveData();
        public List<GridSaveData> grids = new List<GridSaveData>();
        public List<CraftJobSaveData> craftQueue = new List<CraftJobSaveData>();
        public List<CooldownSaveData> cooldowns = new List<CooldownSaveData>();
        public List<string> unlockedAchievements = new List<string>();
        public List<StatEntry> achievementStats = new List<StatEntry>();
    }

    [Serializable]
    public class CooldownSaveData
    {
        public string recipeId;
        public float endsAtUnscaledTime;
    }
}
