using System;
using System.Collections.Generic;
using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// Glue between a transport (<see cref="INetworkBackend"/>) and the game systems.
    /// Converts remote inventory snapshots, shared-station queue updates and trade
    /// messages into local state changes. Works identically with the local simulation
    /// backend, which is how the demo exercises multiplayer code paths offline.
    /// </summary>
    [DisallowMultipleComponent]
    public class NetworkCoordinator : MonoBehaviour
    {
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private CraftingSystem crafting;
        [SerializeField] private TradeSystem trading;

        private INetworkBackend backend;

        public INetworkBackend Backend
        {
            get => backend;
            set => backend = value;
        }

        public bool IsConnected => backend != null && backend.IsConnected;
        public string PlayerId => backend?.PlayerId;

        /// <summary>Raised when a remote inventory snapshot is applied.</summary>
        public event Action<string> RemoteInventoryApplied;
        /// <summary>Raised when a remote crafting queue snapshot is applied.</summary>
        public event Action RemoteQueueApplied;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<InventorySystem>();
            if (crafting == null) crafting = GetComponent<CraftingSystem>();
            if (trading == null) trading = GetComponent<TradeSystem>();
        }

        /// <summary>Connect the coordinator to a transport and start listening.</summary>
        public void Connect(INetworkBackend transport, string playerId, string roomId)
        {
            if (backend != null) backend.MessageReceived -= OnMessage;
            backend = transport;
            if (backend == null) return;
            backend.MessageReceived += OnMessage;
            backend.Connect(playerId, roomId);
        }

        public void Disconnect()
        {
            if (backend != null)
            {
                backend.MessageReceived -= OnMessage;
                backend.Disconnect();
            }
            backend = null;
        }

        private void Update()
        {
            // Pumps HTTPS-poll transports (no-op for websocket/local backends).
            (backend as SyncVaultBackend)?.UpdatePump();
        }

        private void OnMessage(NetworkMessage message)
        {
            if (message == null) return;
            switch (message.type)
            {
                case NetworkMessageTypes.InventorySync:
                    ApplyRemoteInventory(message.payload);
                    break;
                case NetworkMessageTypes.QueueSync:
                    ApplyRemoteQueue(message.payload);
                    break;
                case NetworkMessageTypes.TradeOffer:
                case NetworkMessageTypes.TradeAccept:
                case NetworkMessageTypes.TradeDecline:
                    trading?.HandleNetworkMessage(message);
                    break;
                default:
                    Debug.Log($"[DJS.TiledInventoryCrafting] Unhandled network message '{message.type}' from {message.fromPlayerId}.");
                    break;
            }
        }

        // ------------------------------------------------------------------ sending

        /// <summary>Push a full grid snapshot to the room.</summary>
        public void SyncGrid(InventoryGrid grid)
        {
            if (!IsConnected || grid == null) return;
            var data = CaptureGrid(grid);
            backend.Send(NetworkMessageTypes.InventorySync, JsonUtility.ToJson(data));
        }

        /// <summary>Push the current crafting queue (shared station) to the room.</summary>
        public void SyncQueue(string stationId)
        {
            if (!IsConnected || crafting == null) return;
            var data = new StationQueueData
            {
                stationId = stationId,
                jobs = new List<CraftJobSaveData>()
            };
            foreach (var job in crafting.Queue)
            {
                if (job.recipe == null) continue;
                data.jobs.Add(new CraftJobSaveData
                {
                    recipeId = job.recipe.Id,
                    remainingSeconds = job.remainingSeconds,
                    totalSeconds = job.totalSeconds
                });
            }
            backend.Send(NetworkMessageTypes.QueueSync, JsonUtility.ToJson(data));
        }

        /// <summary>Ask the station (any connected client) to queue a recipe for the shared queue.</summary>
        public void RequestCraft(string stationId, RecipeDefinition recipe)
        {
            if (!IsConnected || recipe == null) return;
            var data = new CraftRequestData { stationId = stationId, recipeId = recipe.Id };
            backend.Send(NetworkMessageTypes.CraftRequest, JsonUtility.ToJson(data));
        }

        // ------------------------------------------------------------------ receiving

        private void ApplyRemoteInventory(string payload)
        {
            if (inventory == null) return;
            try
            {
                var data = JsonUtility.FromJson<GridSaveData>(payload);
                if (data == null) return;
                var grid = inventory.GetOrCreateGrid(data.gridName, new Vector2Int(data.width, data.height));
                if (grid.Size != new Vector2Int(data.width, data.height))
                    grid.Resize(new Vector2Int(data.width, data.height));
                for (int i = 0; i < grid.Count && i < data.slots.Count; i++)
                {
                    var slot = grid.GetSlot(i);
                    var s = data.slots[i];
                    var item = string.IsNullOrEmpty(s.itemId) ? null : Registry.FindItem(s.itemId);
                    slot.stack = item != null && s.count > 0 ? new ItemStack(item, s.count) : ItemStack.Empty;
                }
                grid.EmitChanged();
                RemoteInventoryApplied?.Invoke(data.gridName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DJS.TiledInventoryCrafting] Failed to apply remote inventory: {e.Message}");
            }
        }

        private void ApplyRemoteQueue(string payload)
        {
            if (crafting == null) return;
            try
            {
                var data = JsonUtility.FromJson<StationQueueData>(payload);
                if (data == null) return;
                crafting.RestoreQueue(data.jobs, Registry.FindRecipe);
                RemoteQueueApplied?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[DJS.TiledInventoryCrafting] Failed to apply remote queue: {e.Message}");
            }
        }

        // ------------------------------------------------------------------ helpers

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
                if (!slot.IsEmpty)
                {
                    s.itemId = slot.stack.item.Id;
                    s.count = slot.stack.count;
                }
                data.slots.Add(s);
            }
            return data;
        }

        [Serializable]
        private class StationQueueData
        {
            public string stationId;
            public List<CraftJobSaveData> jobs = new List<CraftJobSaveData>();
        }

        [Serializable]
        private class CraftRequestData
        {
            public string stationId;
            public string recipeId;
        }
    }
}
