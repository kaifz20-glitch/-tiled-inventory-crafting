using System;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// In-memory stand-in for a real transport. Useful for single-player builds,
    /// offline testing, and verifying the game logic before wiring a real backend.
    /// Messages sent are echoed back locally as if another player sent them —
    /// so shared-station and trade code paths run end to end.
    /// </summary>
    public class LocalSimulationBackend : INetworkBackend
    {
        private string roomId;

        public bool IsConnected { get; private set; }
        public string PlayerId { get; private set; } = "local";

        public event Action<NetworkMessage> MessageReceived;

        public void Connect(string playerId, string roomId)
        {
            PlayerId = playerId ?? "local";
            this.roomId = roomId;
            IsConnected = true;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public void Send(string type, string payload)
        {
            if (!IsConnected) return;
            // Echo as "another player" so message handlers execute.
            MessageReceived?.Invoke(new NetworkMessage
            {
                type = type,
                fromPlayerId = PlayerId,
                payload = payload
            });
        }
    }
}
