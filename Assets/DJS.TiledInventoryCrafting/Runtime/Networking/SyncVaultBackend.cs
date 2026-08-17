using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TiledInventory
{
    /// <summary>
    /// Multiplayer transport backed by SyncVault channels.
    ///
    /// SyncVault exposes room/channel messaging (typically websockets with an HTTPS
    /// fallback). This adapter implements the <see cref="INetworkBackend"/> contract
    /// against that API. The exact endpoint shapes below are the documented defaults;
    /// adjust <see cref="MessageUrl"/> / <see cref="ChannelUrl"/> to match your
    /// SyncVault project. See Documentation~/Multiplayer.md for the full setup guide.
    ///
    /// Run one instance per client; hook <see cref="MessageReceived"/> into
    /// <see cref="NetworkCoordinator"/>.
    /// </summary>
    public class SyncVaultBackend : INetworkBackend
    {
        public string BaseUrl = "https://api.syncvault.example/v1";
        public string AuthToken = "";
        public bool VerboseLogging = true;

        private string roomId;
        private readonly Queue<NetworkMessage> inbox = new Queue<NetworkMessage>();
        private UnityWebRequest pollRequest;

        public bool IsConnected { get; private set; }
        public string PlayerId { get; private set; } = "";

        public event Action<NetworkMessage> MessageReceived;

        public SyncVaultBackend(string baseUrl = null, string authToken = null)
        {
            if (!string.IsNullOrEmpty(baseUrl)) BaseUrl = baseUrl;
            if (!string.IsNullOrEmpty(authToken)) AuthToken = authToken;
        }

        public void Connect(string playerId, string roomId)
        {
            PlayerId = playerId ?? Guid.NewGuid().ToString("N");
            this.roomId = roomId;
            IsConnected = true;
            Debug.Log($"[TiledInventory] SyncVault connected as '{PlayerId}' in room '{roomId}'.");
            // Start polling the channel for messages (see UpdatePump below).
        }

        public void Disconnect()
        {
            IsConnected = false;
            if (pollRequest != null) { pollRequest.Dispose(); pollRequest = null; }
        }

        /// <summary>Publish a message to the room channel.</summary>
        public void Send(string type, string payload)
        {
            if (!IsConnected) return;
            var message = new NetworkMessage { type = type, fromPlayerId = PlayerId, payload = payload };
            var json = JsonUtility.ToJson(message);
            var body = new UTF8Encoding().GetBytes(json);
            var request = new UnityWebRequest($"{BaseUrl.TrimEnd('/')}/channels/{Uri.EscapeDataString(roomId)}/messages", "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(AuthToken))
                request.SetRequestHeader("Authorization", $"Bearer {AuthToken}");
            var op = request.SendWebRequest();
            op.completed += _ =>
            {
                if (VerboseLogging && request.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning($"[TiledInventory] SyncVault send failed: {request.error}");
                request.Dispose();
            };
        }

        /// <summary>
        /// Call from a MonoBehaviour Update loop. Polls the channel for new messages
        /// and raises <see cref="MessageReceived"/> for each. (In production prefer a
        /// websocket; this HTTPS poll keeps the adapter dependency-free.)
        /// </summary>
        public void UpdatePump()
        {
            if (!IsConnected) return;
            while (inbox.Count > 0)
                MessageReceived?.Invoke(inbox.Dequeue());

            if (pollRequest != null && !pollRequest.isDone) return;

            pollRequest?.Dispose();
            pollRequest = new UnityWebRequest($"{BaseUrl.TrimEnd('/')}/channels/{Uri.EscapeDataString(roomId)}/messages?after=" + LastSeen, "GET");
            pollRequest.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(AuthToken))
                pollRequest.SetRequestHeader("Authorization", $"Bearer {AuthToken}");
            var op = pollRequest.SendWebRequest();
            op.completed += _ =>
            {
                if (pollRequest == null) return;
                if (pollRequest.result == UnityWebRequest.Result.Success)
                {
                    string body = pollRequest.downloadHandler.text;
                    var list = JsonUtility.FromJson<MessageList>(WrapArray(body));
                    if (list != null && list.messages != null)
                    {
                        foreach (var msg in list.messages)
                        {
                            if (string.IsNullOrEmpty(msg.id)) continue;
                            LastSeen = msg.id;
                            if (msg.fromPlayerId == PlayerId) continue; // skip own echoes
                            inbox.Enqueue(new NetworkMessage
                            {
                                type = msg.type,
                                fromPlayerId = msg.fromPlayerId,
                                payload = msg.payload
                            });
                        }
                    }
                }
                else if (VerboseLogging)
                {
                    Debug.LogWarning($"[TiledInventory] SyncVault poll failed: {pollRequest.error}");
                }
            };
        }

        private string LastSeen = "";

        private static string WrapArray(string body)
        {
            // Server may return a bare JSON array; JsonUtility needs an object wrapper.
            var trimmed = body.Trim();
            if (trimmed.StartsWith("["))
                return "{\"messages\":" + trimmed + "}";
            return trimmed;
        }

        [Serializable]
        private class MessageList
        {
            public List<MessageEntry> messages = new List<MessageEntry>();
        }

        [Serializable]
        private class MessageEntry
        {
            public string id;
            public string type;
            public string fromPlayerId;
            public string payload;
        }
    }
}
