using System;
using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// Lightweight player meta-data used by the crafting economy: level, XP, gold and
    /// crafting skill (which reduces failure chance). Swap this for your own RPG stats
    /// by implementing the same members or adapting the <see cref="CraftingSystem"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerProfile : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private int xp;
        [SerializeField] private int gold;
        [SerializeField] private int craftingSkill = 1;

        [Tooltip("XP required to reach level N+1 from level N.")]
        [SerializeField] private int xpPerLevel = 100;

        public event Action Changed;

        public int Level => level;
        public int Xp => xp;
        public int Gold => gold;
        public int CraftingSkill => craftingSkill;
        public int XpPerLevel => xpPerLevel;
        public int XpIntoLevel => xp % xpPerLevel;

        public void AddGold(int amount)
        {
            gold = Mathf.Max(0, gold + amount);
            Changed?.Invoke();
        }

        public bool SpendGold(int amount)
        {
            if (amount < 0 || gold < amount) return false;
            gold -= amount;
            Changed?.Invoke();
            return true;
        }

        public bool SpendXp(int amount)
        {
            if (amount < 0 || xp < amount) return false;
            xp -= amount;
            Changed?.Invoke();
            return true;
        }

        public void AddXp(int amount)
        {
            xp += Mathf.Max(0, amount);
            while (xp >= xpPerLevel * level)
            {
                xp -= xpPerLevel * level;
                level++;
            }
            Changed?.Invoke();
        }

        public void AddCraftingSkill(int amount)
        {
            craftingSkill = Mathf.Max(0, craftingSkill + amount);
            Changed?.Invoke();
        }

        public void SetAll(int level, int xp, int gold, int craftingSkill)
        {
            this.level = Mathf.Max(1, level);
            this.xp = Mathf.Max(0, xp);
            this.gold = Mathf.Max(0, gold);
            this.craftingSkill = Mathf.Max(0, craftingSkill);
            Changed?.Invoke();
        }
    }
}
