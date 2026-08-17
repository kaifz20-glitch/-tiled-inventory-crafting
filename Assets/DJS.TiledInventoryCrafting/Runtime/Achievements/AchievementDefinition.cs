using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// One achievement: a counter (e.g. "craft.sword") that must reach a target value.
    /// Counters are fed by <see cref="AchievementTracker"/> listening to crafting events,
    /// or manually via <see cref="AchievementTracker.AddStat"/>.
    ///
    /// Create from the menu: <c>Assets &gt; Create &gt; Tiled Inventory &gt; Achievement Definition</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Tiled Inventory/Achievement Definition", fileName = "Achievement", order = 2)]
    public class AchievementDefinition : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string title = "New Achievement";
        [TextArea(1, 3)]
        [SerializeField] private string description = "";
        [SerializeField] private Sprite icon;

        [Header("Condition")]
        [Tooltip("Stat key to count, e.g. \"craft.sword\" or \"craft.total\".")]
        [SerializeField] private string statKey = "craft.total";
        [SerializeField] private int targetValue = 1;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public Sprite Icon => icon;
        public string StatKey => statKey;
        public int TargetValue => targetValue;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = System.Guid.NewGuid().ToString("N").Substring(0, 12);
            if (string.IsNullOrEmpty(title))
                title = name;
            if (targetValue < 1) targetValue = 1;
        }
    }
}
