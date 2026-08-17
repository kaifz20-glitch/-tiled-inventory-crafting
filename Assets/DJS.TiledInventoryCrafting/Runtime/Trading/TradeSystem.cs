using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiledInventory
{
    public enum TradeState
    {
        Pending,
        Accepted,
        Declined,
        Cancelled
    }

    [Serializable]
    public class TradeOffer
    {
        public string id;
        public string fromPlayerId;
        public string toPlayerId;
        public List<ItemStack> offered = new List<ItemStack>();
        public List<ItemStack> requested = new List<ItemStack>();
        public TradeState state = TradeState.Pending;
    }

    /// <summary>
    /// Player-to-player item exchange. In single-player this works against two
    /// <see cref="InventorySystem"/> instances (your inventory + a trading partner).
    /// With a network backend the offers are serialized and routed between players —
    /// see <see cref="NetworkCoordinator"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class TradeSystem : MonoBehaviour
    {
        [SerializeField] private InventorySystem localInventory;
        [SerializeField] private NetworkCoordinator network;

        private readonly List<TradeOffer> offers = new List<TradeOffer>();

        /// <summary>Optional coordinator used to broadcast offers to other players.</summary>
        public void SetNetwork(NetworkCoordinator coordinator) => network = coordinator;

        public IReadOnlyList<TradeOffer> Offers => offers;

        public event Action<TradeOffer> OfferCreated;
        public event Action<TradeOffer> OfferResolved;

        private void Awake()
        {
            if (localInventory == null) localInventory = GetComponent<InventorySystem>();
        }

        /// <summary>Create an offer from the local player. The offered items are reserved
        /// (removed from the bag) so they cannot be spent twice while the offer is pending.</summary>
        public TradeOffer CreateOffer(List<ItemStack> offered, List<ItemStack> requested, string toPlayerId)
        {
            if (localInventory == null || offered == null || offered.Count == 0) return null;

            // reserve offered items
            foreach (var stack in offered)
            {
                if (!localInventory.MainGrid.Contains(stack.item, stack.count)) return null;
            }
            foreach (var stack in offered)
                localInventory.MainGrid.Consume(stack.item, stack.count);

            var offer = new TradeOffer
            {
                id = Guid.NewGuid().ToString("N"),
                fromPlayerId = "local",
                toPlayerId = toPlayerId ?? "any",
                offered = new List<ItemStack>(offered),
                requested = new List<ItemStack>(requested ?? new List<ItemStack>())
            };
            offers.Add(offer);
            Broadcast(NetworkMessageTypes.TradeOffer, JsonUtility.ToJson(offer));
            OfferCreated?.Invoke(offer);
            return offer;
        }

        /// <summary>Accept an offer addressed to the local player. Validates that both sides
        /// still hold their items, swaps them, and closes the offer.</summary>
        public bool AcceptOffer(TradeOffer offer)
        {
            if (offer == null || offer.state != TradeState.Pending) return false;
            if (localInventory == null) return false;

            // local player must still have the requested items
            foreach (var stack in offer.requested)
                if (!localInventory.MainGrid.Contains(stack.item, stack.count))
                {
                    DeclineOffer(offer, "Requested items no longer available.");
                    return false;
                }

            // withdraw the requested items and deposit the offered ones
            foreach (var stack in offer.requested)
                localInventory.MainGrid.Consume(stack.item, stack.count);
            foreach (var stack in offer.offered)
                localInventory.MainGrid.Add(stack);

            offer.state = TradeState.Accepted;
            Broadcast(NetworkMessageTypes.TradeAccept, JsonUtility.ToJson(new TradeResponse { offerId = offer.id }));
            OfferResolved?.Invoke(offer);
            return true;
        }

        public bool DeclineOffer(TradeOffer offer, string reason = null)
        {
            if (offer == null || offer.state != TradeState.Pending) return false;
            // refund the offered items if they were ours
            if (offer.fromPlayerId == "local")
                foreach (var stack in offer.offered)
                    localInventory?.MainGrid.Add(stack);
            offer.state = TradeState.Declined;
            Broadcast(NetworkMessageTypes.TradeDecline, JsonUtility.ToJson(new TradeResponse { offerId = offer.id }));
            OfferResolved?.Invoke(offer);
            return true;
        }

        public bool CancelOffer(TradeOffer offer)
        {
            if (offer == null || offer.state != TradeState.Pending) return false;
            return DeclineOffer(offer);
        }

        /// <summary>Route a trade message received from the network into this system.</summary>
        public void HandleNetworkMessage(NetworkMessage message)
        {
            if (message == null) return;
            switch (message.type)
            {
                case NetworkMessageTypes.TradeOffer:
                {
                    var offer = JsonUtility.FromJson<TradeOffer>(message.payload);
                    if (offer == null) return;
                    offer.fromPlayerId = message.fromPlayerId;
                    offers.Add(offer);
                    OfferCreated?.Invoke(offer);
                    break;
                }
                case NetworkMessageTypes.TradeAccept:
                case NetworkMessageTypes.TradeDecline:
                {
                    var payload = JsonUtility.FromJson<TradeResponse>(message.payload);
                    if (payload == null) return;
                    var offer = offers.Find(o => o.id == payload.offerId);
                    if (offer == null) return;
                    if (message.type == NetworkMessageTypes.TradeAccept)
                        offer.state = TradeState.Accepted;
                    else
                        offer.state = TradeState.Declined;
                    OfferResolved?.Invoke(offer);
                    break;
                }
            }
        }

        private void Broadcast(string type, string payload)
        {
            if (network != null && network.IsConnected)
                network.Backend.Send(type, payload);
        }

        [Serializable]
        private class TradeResponse
        {
            public string offerId;
        }
    }
}
