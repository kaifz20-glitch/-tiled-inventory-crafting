# Getting Started

## 1. Install

Copy the `DJS.TiledInventoryCrafting` folder anywhere under `Assets/` (or import the
`.unitypackage`). There are no package dependencies beyond uGUI, which ships with Unity.

Everything compiles into two assemblies:

- `TiledInventory.Runtime` — the systems and UI (works in builds).
- `TiledInventory.Editor` — tools (recipe graph editor, scene builder, validation).

## 2. Try the demo

1. **Tools → Tiled Inventory → Build Demo Scene** (menu bar).
2. Open `Assets/DJS.TiledInventoryCrafting/Demo/Demo.unity`.
3. Press **Play**.

The scene is generated entirely from code — camera, systems GameObject, and the whole
UI. You can rebuild it any time; it never overwrites your own scenes.

## 3. Build it into your own scene

The demo wiring is a good reference (`Editor/DemoSceneBuilder.cs`). To wire manually:

### 3a. One GameObject for the systems

Create an empty GameObject (e.g. "Player Systems") and add, in this order:

| Component | Purpose | Auto-resolves |
|---|---|---|
| `InventorySystem` | Owns the grids | — |
| `EquipmentSystem` | Equipment semantics | needs InventorySystem (same GO) |
| `PlayerProfile` | Level, gold, XP, crafting skill | — |
| `CraftingSystem` | Queue + economy | needs InventorySystem + PlayerProfile |
| `CraftingStation` | Station identity (multiplayer) | needs CraftingSystem |
| `SaveManager` | Persistence | needs the above |
| `AchievementTracker` | Achievements (Phase 3) | optional |
| `TradeSystem` | Trading (Phase 2) | needs InventorySystem |
| `NetworkCoordinator` | Multiplayer glue (Phase 2) | optional |
| `CraftAnalytics` | Analytics events (Phase 3) | optional |
| `AudioFeedback` | Demo sounds | optional |
| `InventoryCraftingUI` | Builds the whole UI | needs the systems |

Every `[SerializeField]` left empty auto-resolves from the same GameObject, so for the
common "everything on one object" setup you only need to fill in **content**:

### 3b. Content (items & recipes)

Create items: **Assets → Create → Tiled Inventory → Item Definition**.
Create recipes: **Assets → Create → Tiled Inventory → Recipe Definition**.

Assign to `InventoryCraftingUI.Recipes` (shown in the crafting panel) and
`InventoryCraftingUI.TradeItems` (available for trade offers). Leave the lists empty to
auto-discover from the Registry (all loaded assets).

> The Registry is how save files and network messages resolve content by id. Any loaded
> `ItemDefinition`/`RecipeDefinition` registers itself automatically in `OnEnable`.

### 3c. UI

`InventoryCraftingUI` builds its own canvas at runtime — nothing to prefab. It creates:

- an `EventSystem` (if none exists),
- a `Canvas` + `CanvasScaler` (1920×1080 reference),
- the equipment, inventory, crafting and trade panels.

## 4. Configuration overview

| Setting | Where | Default |
|---|---|---|
| Grid size | `InventorySystem` → grids (name "Main") | 5×5 |
| Equipment slots | `EquipmentSystem` (fixed) | Head/Chest/Legs/Weapon/Accessory |
| Rarity & surface colors | `InventoryCraftingUI.Palette` | dark theme |
| Recipe list | `InventoryCraftingUI.Recipes` | auto-discover |
| Save slot & autosave | `SaveManager` | "save1", 60 s |
| Failure / cooldown / costs | per `RecipeDefinition` | 0 |
| Crafting skill effect | per recipe `failureChanceReductionPerSkill` | 0.005/level |

## 5. Next steps

- Read the [API Reference](ApiReference.md).
- Re-skin in [Customization](Customization.md).
- Wire multiplayer in [Multiplayer](Multiplayer.md).
