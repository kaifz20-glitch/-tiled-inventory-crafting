using System;

namespace TiledInventory
{
    /// <summary>Well-known message kinds carried over the network.</summary>
    public static class NetworkMessageTypes
    {
        public const string InventorySync = "inventory.sync";       // full grid snapshot
        public const string QueueSync = "crafting.queue.sync";      // shared station queue
        public const string CraftRequest = "crafting.request";      // ask a station to queue a recipe
        public const string TradeOffer = "trade.offer";
        public const string TradeAccept = "trade.accept";
        public const string TradeDecline = "trade.decline";
        public const string Chat = "chat";
    }

    /// <summary>A message delivered from another player (or from the local sim).</summary>
    public class NetworkMessage
    {
        public string type;
        public string fromPlayerId;
        public string payload;   // JSON
    }

    /// <summary>
    /// Transport abstraction for multiplayer. The game code only talks to this interface:
    /// pushing inventory snapshots, sharing crafting queues, trading. Two implementations
    /// ship: <see cref="LocalSimulationBackend"/> (single-player / offline testing) and
    /// <see cref="SyncVaultBackend"/> (real multiplayer via SyncVault).
    /// </summary>
    public interface INetworkBackend
    {
        bool IsConnected { get; }
        string PlayerId { get; }

        /// <summary>Join a room/channel. Messages for that room arrive via <see cref="MessageReceived"/>.</summary>
        void Connect(string playerId, string roomId);

        void Disconnect();

        /// <summary>Broadcast a message to everyone in the room (including other players).</summary>
        void Send(string type, string payload);

        event Action<NetworkMessage> MessageReceived;
    }
}
