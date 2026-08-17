# Multiplayer (Phase 2)

## Architecture

Game code never talks to a specific transport. It talks to `INetworkBackend`; the
`NetworkCoordinator` translates network messages into system calls:

```
CraftingSystem ──┐
InventorySystem ─┼── NetworkCoordinator ── INetworkBackend ── SyncVault / LocalSim
TradeSystem ─────┘
```

Two transports ship:

| Backend | Use |
|---|---|
| `LocalSimulationBackend` | Single-player, offline dev, automated tests. Echoes messages back so every multiplayer code path runs end-to-end locally. |
| `SyncVaultBackend` | Real multiplayer over SyncVault channels. |

## What SyncVault does for you

SyncVault is a game backend service: room/channel messaging, per-player data, auth
tokens. This package uses two of its capabilities:

1. **Channels** (room messaging) — for shared crafting queues, inventory sync, trades.
2. **Player key/value storage** — for cloud saves (`SyncVaultSaveBackend`).

The adapter ships with the documented REST contract. Because SyncVault project URLs and
token flows differ per account, plug in your endpoint and token:

```csharp
var backend = new SyncVaultBackend("https://<your-project>.syncvault.example/v1", "<auth-token>");
coordinator.Connect(backend, playerId, roomId);
```

> **SyncVault setup steps (per your SyncVault account):**
> 1. Create a project and a room/channel definition for your game.
> 2. Create a player-scoped auth token (or configure your login flow to mint one).
> 3. Set `BaseUrl` + `AuthToken` on the backend (or env vars `SYNCVAULT_BASE_URL` /
>    `SYNCVAULT_TOKEN`).
> 4. If your project's endpoints differ from the adapter's defaults, adjust
>    `SyncVaultBackend` (message POST/GET paths) to match — the message shapes are
>    documented in `INetworkBackend.cs` (`NetworkMessageTypes`).

## Wiring

```csharp
// one client:
var coordinator = GetComponent<NetworkCoordinator>();
var backend = new SyncVaultBackend(url, token);   // or LocalSimulationBackend for offline
coordinator.Connect(backend, playerId, roomId);
```

`SyncVaultBackend` is HTTPS-poll based; call `UpdatePump()` every frame (the coordinator
does this automatically). For production, swap the poll for a websocket — keep the
`INetworkBackend` contract and nothing else changes.

## Shared crafting stations

A `CraftingStation` has a `StationId`. Players with the same station id share one queue:

- Push the queue: `coordinator.SyncQueue(stationId)` — call it whenever the local queue
  changes (or on an interval).
- Ask the station to craft: `coordinator.RequestCraft(stationId, recipe)` — the host
  validates and queues, then broadcasts the new queue state.
- Remote queue snapshots arrive via `coordinator.RemoteQueueApplied` and are applied with
  `CraftingSystem.RestoreQueue`.

> **Host-authoritative recommended flow:** one client acts as the station host. Remote
> players send `RequestCraft`; the host runs `TryQueue` and broadcasts `QueueSync`. This
> prevents two players double-spending the same materials.

## Inventory sync

Push a grid snapshot: `coordinator.SyncGrid(inventory.MainGrid)`.
Remote snapshots replace matching grids (by name) via `RemoteInventoryApplied`.

## Trading

`TradeSystem` broadcasts offers when a `NetworkCoordinator` is assigned:
`tradeSystem.SetNetwork(coordinator)`.

- `CreateOffer` → `trade.offer` broadcast (offered items are reserved locally).
- `AcceptOffer` / `DeclineOffer` → response broadcast.
- The receiving side reconstructs the offer from the message (`fromPlayerId` set from the
  transport) and raises `OfferCreated`.

The `TradePanelUI` handles the whole flow; it also works offline via
`LocalSimulationBackend` (offers echo back to yourself).

## Message types

| Type | Payload | Direction |
|---|---|---|
| `inventory.sync` | `GridSaveData` JSON | room broadcast |
| `crafting.queue.sync` | station id + jobs | room broadcast |
| `crafting.request` | station id + recipe id | room broadcast (host acts) |
| `trade.offer` / `trade.accept` / `trade.decline` | `TradeOffer` / `{offerId}` | room broadcast |
| `chat` | string | room broadcast (unhandled by default) |

## Offline fallback

If the transport is missing or disconnected, all systems keep working locally —
`NetworkCoordinator.IsConnected` is false and `SyncGrid`/`SyncQueue` no-op. This is the
default in the demo scene.
