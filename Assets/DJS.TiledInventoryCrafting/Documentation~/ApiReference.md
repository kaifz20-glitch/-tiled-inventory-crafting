# API Reference

All runtime code lives in the `DJS.TiledInventoryCrafting` namespace. Editor tools live in
`DJS.TiledInventoryCrafting.EditorTools`.

---

## Core types

### `ItemStack` (struct)
A reference to an item plus a count. Used everywhere: slots, recipe inputs/outputs,
trades, save data.

- `ItemDefinition item` — the item (null = empty).
- `int count` — stack size.
- `bool IsEmpty` — true when item is null or count ≤ 0.
- `static ItemStack Empty` — an empty stack.

### `ItemRarity` (enum)
`Common, Uncommon, Rare, Epic, Legendary`.

### `RarityColors` (static)
- `Color Get(ItemRarity)` — the canonical rarity color. UI tinting actually uses
  `RarityPalette` (tweakable); `RarityColors` is the fallback/quick reference.

### `ItemCategory` (enum)
`Material, Weapon, Armor, Helmet, Legs, Consumable, Tool, Currency, Quest, Trinket`.

### `EquipmentSlotType` (enum)
`None, Head, Chest, Legs, Weapon, Accessory`. `None` = not equippable.

### `StatType` (enum) / `StatModifier` (struct)
`Damage, Armor, Durability, Speed, Luck`; `StatModifier { StatType stat; int value }`.
Extend the enum for game-specific stats.

### `Registry` (static)
- `ItemDefinition FindItem(string id)` — resolve an item by its stable id.
- `RecipeDefinition FindRecipe(string id)` — resolve a recipe by id.
- `IEnumerable<ItemDefinition> AllItems` / `IEnumerable<RecipeDefinition> AllRecipes`.
- Items/recipes self-register in `OnEnable`; you normally never call Register/Unregister.

---

## Items

### `ItemDefinition : ScriptableObject`
Create via **Assets → Create → Tiled Inventory → Item Definition**.

| Member | Type | Notes |
|---|---|---|
| `Id` | string | Stable unique id (auto-generated). Save files reference this. |
| `DisplayName` | string | |
| `Description` | string | Shown in tooltips. |
| `Icon` | Sprite | Optional — UI draws a rarity placeholder if missing. |
| `Rarity` | ItemRarity | Drives color-coding. |
| `Category` | ItemCategory | Used by restrictions and filters. |
| `MaxStack` | int | Stack cap in a single slot. |
| `EquippableSlot` | EquipmentSlotType | `None` = not equippable. |
| `CanEquip` | bool | `EquippableSlot != None`. |
| `Stats` | IReadOnlyList\<StatModifier\> | Damage, armor, ... |
| `SellValue` | int | |
| `int GetStat(StatType)` | | Sum of a stat across modifiers. |

---

## Inventory

### `InventoryGrid` (plain class, serializable)
A W×H grid of slots. Pure data + rules; no rendering.

- `string GridName`
- `Vector2Int Size` / `int Count`
- `event Action<int> SlotChanged` — argument is the slot index.
- `event Action Changed` — any structural change.
- `void Resize(Vector2Int)` — preserve contents where possible.
- `bool IsValidIndex(int)` / `InventorySlot GetSlot(int)`
- `int IndexOf(Vector2Int)` / `Vector2Int PositionOf(int)`
- `bool CanPlace(int index, ItemDefinition, int count)` — bounds + restriction + stack rules.
- `bool CanPlaceAnywhere(ItemDefinition, int count)`
- `int CountItem(ItemDefinition)` / `bool Contains(ItemDefinition, int count)`
- `int Add(ItemStack)` — merge-then-fill; returns leftover.
- `int Remove(int index, int count)` — returns amount removed.
- `bool Consume(ItemDefinition, int count)` — across slots; returns success.
- `bool Move(int from, int to, int count)` — merge, swap, restrictions enforced.
- `void Clear()` — keep restrictions, drop contents.

### `SlotRestriction` (class, serializable)
Per-slot placement rules.
- `bool locked` — rejects everything, drawn dimmed.
- `EquipmentSlotType equipmentSlotType` — `None` = unrestricted (used by equipment grid).
- `List<ItemCategory> allowedCategories`
- `List<ItemDefinition> allowedItems`
- `bool Allows(ItemDefinition)` — full rule evaluation.

### `InventorySlot` (class, serializable)
`ItemStack stack` + `SlotRestriction restriction`.

### `InventorySystem : MonoBehaviour`
Owns all grids; grids are addressable by name.
- `InventoryGrid MainGrid` — the "Main" grid (created on demand).
- `Vector2Int DefaultMainGridSize` — serialized size used when "Main" is first created
  (default 5×5; the demo scene sets it to 9×8).
- `InventoryGrid EquipmentGrid` — the "Equipment" grid (1×5).
- `IReadOnlyList<InventoryGrid> Grids`
- `event Action Changed`
- `InventoryGrid GetGrid(string)` / `GetOrCreateGrid(string, Vector2Int)`
- `int AddItem(ItemStack)` / `int AddItem(string gridName, ItemStack)` — leftover returned.
- `int CountItem(ItemDefinition)` — across all grids.
- `bool Consume(ItemDefinition, int count)` — across all grids.

### `EquipmentSystem : MonoBehaviour`
Typed equipment over the equipment grid.
- `InventoryGrid Grid`
- `event Action<int> EquipmentChanged` — slot index, -1 = structural.
- `int GetSlotIndex(EquipmentSlotType)` / `EquipmentSlotType GetSlotType(int)`
- `ItemDefinition GetEquipped(EquipmentSlotType)`
- `bool Equip(ItemDefinition)` — from the main bag; swaps current gear back. `false` if impossible.
- `bool Unequip(EquipmentSlotType)` — `false` if the bag is full.
- `int GetTotalStat(StatType)` — sum across equipped items.

---

## Crafting

### `RecipeDefinition : ScriptableObject`
Create via **Assets → Create → Tiled Inventory → Recipe Definition** (or the visual editor).

| Member | Type | Notes |
|---|---|---|
| `Id` | string | Stable unique id. |
| `DisplayName` | string | |
| `Inputs` | IReadOnlyList\<ItemStack\> | Materials consumed at queue time. |
| `Outputs` | IReadOnlyList\<ItemStack\> | Granted on success. |
| `CraftTime` | float | Seconds (unscaled time). |
| `LevelRequirement` | int | Player level needed to queue. |
| `GoldCost` / `XpCost` | int | Currency costs consumed at queue time. |
| `SpecialCosts` | IReadOnlyList\<ItemStack\> | Extra materials on top of inputs. |
| `FailureChance` | float | Base 0..1 chance to fail (materials lost, no output). |
| `FailureChanceReductionPerSkill` | float | Failure reduced by craftingSkill × this. |
| `CooldownSeconds` | float | Wait after finishing before re-queueing. |
| `float GetFailureChance(int craftingSkill)` | | Effective failure chance. |
| `bool HasMaterials(InventoryGrid)` | | Inputs + special costs present? |
| `string GetSummary()` | | "5 Wood + 2 Iron → 1 Sword". |

### `CraftJob` (class, serializable)
`RecipeDefinition recipe`, `float remainingSeconds`, `float totalSeconds`,
`CraftJobState state`, `float Progress` (0..1).

### `CraftJobState` (enum)
`Queued, Crafting, Completed, Failed, Cancelled`.

### `CraftRejection` (enum)
`None, MissingMaterials, LevelTooLow, NotEnoughGold, NotEnoughXp, OnCooldown`.

### `CraftingSystem : MonoBehaviour`
- `IReadOnlyList<CraftJob> Queue`
- `event Action<CraftJob> JobStarted / JobCompleted / JobFailed / JobCancelled`
- `event Action QueueChanged`
- `CraftRejection CanQueue(RecipeDefinition)` — validation only, no side effects.
- `CraftRejection TryQueue(RecipeDefinition)` — validate, consume costs, enqueue.
- `bool Cancel(int queueIndex)` — queued (not started) jobs only; no refund.
- `void CancelAll()`
- `float GetCooldownRemaining(RecipeDefinition)`
- `bool IsCrafting` / `int QueuedCount`
- Save support: `RestoreQueue(List<CraftJobSaveData>, Func<string, RecipeDefinition>)`,
  `RestoreCooldowns(List<CooldownSaveData>)`, `GetCooldowns()`.

Timers use `Time.unscaledTime`, so the queue keeps running when the game is paused.

### `CraftingStation : MonoBehaviour`
- `string StationId` — stable id shared by clients (multiplayer).
- `CraftingSystem System` — the attached system.

---

## Profile

### `PlayerProfile : MonoBehaviour`
- `int Level / Xp / Gold / CraftingSkill / XpPerLevel / XpIntoLevel`
- `event Action Changed`
- `void AddGold(int)` / `bool SpendGold(int)` / `bool SpendXp(int)`
- `void AddXp(int)` — auto level-up.
- `void AddCraftingSkill(int)`
- `void SetAll(int level, int xp, int gold, int craftingSkill)`

Swap this for your own RPG stats by adapting `CraftingSystem` references.

---

## Persistence

### `ISaveBackend` (interface)
`bool Save(string slot, string json)`, `bool Load(string slot, out string json)`,
`bool Delete(string slot)`.

### `LocalFileSaveBackend : ISaveBackend`
Writes to `Application.persistentDataPath/DJS.TiledInventoryCrafting/saves/{slot}.json`.

### `SyncVaultSaveBackend : ISaveBackend`
Cloud backend adapter (see [Persistence](Persistence.md)). Async helpers:
`SaveAsync(slot, json, onDone)`, `LoadAsync(slot, onDone)`.

### `SaveManager : MonoBehaviour`
- `ISaveBackend Backend` — swap storage; default local file.
- `string Slot` — save slot name.
- `event Action<bool> Saved / Loaded`
- `bool Save()` / `bool Load()` / `bool DeleteSave()`
- Autosave (`autoSave`, `autoSaveInterval`) and save-on-quit.

### Save data (DTOs, JsonUtility-friendly)
`GameSaveData` contains `ProfileSaveData`, `List<GridSaveData>` (with `SlotSaveData`),
`List<CraftJobSaveData>`, `List<CooldownSaveData>`, unlocked achievements and stat
counters. No dictionaries — plain lists.

---

## Multiplayer (Phase 2)

### `INetworkBackend` (interface)
`bool IsConnected`, `string PlayerId`, `Connect(playerId, roomId)`, `Disconnect()`,
`Send(type, payload)`, `event Action<NetworkMessage> MessageReceived`.

### `NetworkMessage`
`string type` (see `NetworkMessageTypes`), `string fromPlayerId`, `string payload` (JSON).

### `NetworkCoordinator : MonoBehaviour`
- `INetworkBackend Backend` — assign the transport.
- `void Connect(INetworkBackend, string playerId, string roomId)` / `Disconnect()`
- `void SyncGrid(InventoryGrid)` — push a snapshot to the room.
- `void SyncQueue(string stationId)` — push the shared station queue.
- `void RequestCraft(string stationId, RecipeDefinition)`
- `event Action<string> RemoteInventoryApplied` / `event Action RemoteQueueApplied`

### `LocalSimulationBackend : INetworkBackend`
In-memory transport that echoes messages back — exercises multiplayer code paths offline.

### `SyncVaultBackend : INetworkBackend`
HTTPS transport adapter for SyncVault channels. Call `UpdatePump()` from an Update loop
(polling; websockets can replace it). See [Multiplayer](Multiplayer.md).

---

## Trading (Phase 2)

### `TradeSystem : MonoBehaviour`
- `IReadOnlyList<TradeOffer> Offers`
- `event Action<TradeOffer> OfferCreated / OfferResolved`
- `TradeOffer CreateOffer(List<ItemStack> offered, List<ItemStack> requested, string toPlayerId)`
  — offered items are reserved (removed from the bag) while pending.
- `bool AcceptOffer(TradeOffer)` — validates both sides, swaps, closes.
- `bool DeclineOffer(TradeOffer, string reason = null)` / `bool CancelOffer(TradeOffer)`
- `void HandleNetworkMessage(NetworkMessage)` — routes remote trade messages.
- `void SetNetwork(NetworkCoordinator)` — enables broadcasting.

### `TradeOffer`
`id`, `fromPlayerId`, `toPlayerId`, `List<ItemStack> offered`, `List<ItemStack> requested`,
`TradeState state` (`Pending/Accepted/Declined/Cancelled`).

---

## Achievements (Phase 3)

### `AchievementDefinition : ScriptableObject`
`Id`, `Title`, `Description`, `Icon`, `StatKey` (e.g. `"craft.total"`), `TargetValue`.

### `AchievementTracker : MonoBehaviour`
- `event Action<AchievementDefinition> Unlocked`
- `IReadOnlyCollection<string> UnlockedIds`
- `void RegisterAchievements(IEnumerable<AchievementDefinition>)`
- `void AddStat(string key, int amount = 1, RecipeDefinition context = null)` — scoped
  stats are keyed `key.<recipeId>` (so "craft total sword" = `craft.total.<swordId>`).
- `int GetStat(string key)` / `bool IsUnlocked(AchievementDefinition)`
- `void Restore(List<string> unlockedIds, List<StatEntry>)`

Auto-wires to `CraftingSystem.JobCompleted` / `JobFailed` when on the same GameObject.

---

## Analytics (Phase 3)

### `IAnalyticsSink` (interface)
`void Track(string eventName, IReadOnlyDictionary<string, object> properties)`.

### `ConsoleAnalyticsSink : IAnalyticsSink`
Logs to the console — the shipped default.

### `CraftAnalytics : MonoBehaviour`
- `IAnalyticsSink Sink` — assign your provider's sink.
- `void Track(string eventName, IReadOnlyDictionary<string, object> properties = null)`
- Auto-tracks `craft.started`, `craft.completed`, `craft.failed`, `trade.offer_created`,
  `trade.accepted/declined/cancelled` when systems are on the same GameObject.

---

## UI

### `InventoryCraftingUI : MonoBehaviour` (root)
- `RarityPalette Palette` — every color, inspector-editable.
- `Canvas Canvas`, `Tooltip Tooltip`, `DragDropService Drag`
- `InventoryGridView InventoryView`, `EquipmentPanelUI EquipmentView`,
  `CraftingPanelUI CraftingView`, `TradePanelUI TradeView`
- `void BuildUI()` — build the whole screen from code (also runs on Awake).
- `void ToggleTradePanel(bool? open = null)`
- Serialized inputs: the systems, `recipes`, `tradeItems`, `palette`, `buildOnAwake`,
  `canvasSortingOrder`.

### `InventoryGridView : MonoBehaviour`
- `void Bind(InventoryGrid, RarityPalette, RectTransform slotContainer)`
- `void Rebuild()` / `void RefreshAll()`
- `void BuildToolbar(RectTransform toolbar)` — search, category, sort.

### `EquipmentPanelUI : MonoBehaviour`
`Bind(EquipmentSystem, RarityPalette, RectTransform slotColumn, Text statsText)`.

### `CraftingPanelUI : MonoBehaviour`
`Bind(CraftingSystem, InventorySystem, PlayerProfile, List<RecipeDefinition>, RarityPalette,
RectTransform recipeListRoot, RectTransform queueRoot, Text statusText)`.
- `void RebuildRecipes()` / `void RefreshQueue()`
- `event Action<RecipeDefinition> CraftRequested`
- Dropping an item on a recipe row attempts to craft it ("drag to craft").

### `TradePanelUI : MonoBehaviour`
`Bind(TradeSystem, InventorySystem, RarityPalette, List<ItemDefinition>, RectTransform
offerSection, RectTransform offersSection)`.

### `SlotView : MonoBehaviour`
One tiled cell; handles tooltips and drag/drop. `Bind(InventoryGrid, int, RarityPalette,
string emptyHint = null)`, `Refresh()`.

### `DragDropService : MonoBehaviour` (singleton)
`static Instance`, `IsDragging`, `BeginDrag(...)`, `DropOn(InventoryGrid, int)`,
`DropItem()`, `EndDrag()`. `Ctrl`-drag moves a single item.

### `DropZone : MonoBehaviour`
The "drop items" strip under the inventory grid. Dragging an item onto it removes it
from its source grid entirely (calls `DragDropService.DropItem()`); highlights while a
drag hovers over it. `static DropZone Create(RectTransform parent, RarityPalette)`.
Built automatically by `InventoryCraftingUI.BuildUI()`.

### `Tooltip : MonoBehaviour` (singleton)
`Show(ItemDefinition, string extra = null)`, `Show(string, string, Color)`, `Hide()`.

### `UIFactory` (static)
Code-built uGUI helpers: `EnsureEventSystem()`, `CreateCanvas(...)`, `CreatePanel(...)`,
`CreateText(...)`, `CreateButton(...)`, `CreateInputField(...)`, `CreateScrollView(...)`,
`CreateProgressBar(...)`, `SetProgress(...)`, `CreateSelect(...)`, `GetSolidSprite(...)`,
`CreateIconSprite(...)`.

Rounded/gradient 9-sliced sprites (generated at runtime, tintable):
`GetRoundedFillSprite(radius)`, `GetRoundedTopSprite(radius)`,
`GetRoundedBottomSprite(radius)`, `GetRoundedFrameSprite(radius, thickness)`,
`GetGradientSprite(top, bottom)`, `GetRoundedGradientSprite(top, bottom, radius, ...)`.

### `RarityPalette` (serializable)
All UI colors: rarity colors, surfaces (incl. `panelHeaderTop/Bottom` gradient), buttons,
text, and an `accent` color. The single re-skin point.

### `Fonts` (static)
`Font Default` — built-in font (LegacyRuntime.ttf on Unity 6, Arial on 2022 LTS).
Override to use your own font.

---

## Demo

### `DemoController : MonoBehaviour`
HUD for the demo scene: gather buttons, gold/XP, save/load/reset, trade toggle,
achievement toasts, audio + particles on craft complete. `PopulateShowcase()` pre-fills
starter materials and equips starter gear while the bag is empty.

### `AudioFeedback : MonoBehaviour`
Procedural click/craft/equip/fail sounds (no assets needed). Assign your own clips via
`ClickClip`, `CraftClip`, `EquipClip`, `FailClip`.

### `ParticleBurst` (static)
`Play(Vector3 position, Color color, int count = 26, float lifetime = 1.1f)` — one-shot
runtime particle burst for craft-complete feedback.

---

## Editor tools (`DJS.TiledInventoryCrafting.EditorTools`)

| Menu item | Purpose |
|---|---|
| `Tools → Tiled Inventory → Build Demo Scene` | Generates the demo scene. |
| `Tools → Tiled Inventory → Create Demo Content` | Generates items/recipes/achievements. |
| `Tools → Tiled Inventory → Recipe Graph Editor` | Visual recipe editor. |
| `Tools → Tiled Inventory → Validate Package` | Success-criteria checks. |
| `Tools → Tiled Inventory → Run Logic Verification` | Edit-mode test suite: grid rules, crafting queue, save/load round-trip, UI build, drop item (39 checks). |
| `Tools → Tiled Inventory → Verify Recipe Graph` | Edit-mode test suite for the graph editor: theming, edge styling, port sync, export (14 checks). |
| `Tools → Tiled Inventory → Capture Demo Preview` | Renders the demo UI to 1920×1080 store-page screenshots (`demo_preview.png`, `screenshot_trade.png`, `screenshot_crafting.png`). |
| `Assets → Create → Tiled Inventory → …` | Item / Recipe / Achievement definitions. |
