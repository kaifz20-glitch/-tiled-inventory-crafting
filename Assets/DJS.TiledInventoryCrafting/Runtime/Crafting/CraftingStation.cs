using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// A place in the world where crafting happens (workbench, anvil, alchemy table...).
    /// In single-player this is a thin wrapper around a <see cref="CraftingSystem"/>.
    /// In multiplayer, players with the same <see cref="StationId"/> share one queue —
    /// see the network backend and the Multiplayer documentation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CraftingSystem))]
    public class CraftingStation : MonoBehaviour
    {
        [Tooltip("Stable id shared by all clients that use this station (e.g. \"anvil_village_01\").")]
        [SerializeField] private string stationId = "station_default";

        private CraftingSystem system;

        public string StationId
        {
            get => stationId;
            set => stationId = value;
        }

        public CraftingSystem System
        {
            get
            {
                if (system == null) system = GetComponent<CraftingSystem>();
                return system;
            }
        }

        private void Awake()
        {
            system = GetComponent<CraftingSystem>();
        }
    }
}
