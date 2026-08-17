# Customization Guide

Everything below is achievable **without writing code** unless noted.

## 1. Re-skin the UI (colors, no code)

Select the object with `InventoryCraftingUI` and edit the **Palette** field:

- **Rarity colors** — tint of slot glows, tooltip titles, slot frames, recipe accents.
- **Surfaces** — panel background, header gradient (`panelHeaderTop/Bottom`), slot
  backgrounds, locked overlay, row colors.
- **Buttons** — normal/highlight/pressed/disabled.
- **Text** — primary, secondary, disabled, success, warning, danger.
- **Accent** — the gold accent used for header underlines, the HUD and emphasis.

Panels, slots, buttons and tooltips use rounded-corner 9-sliced sprites generated at
runtime, so the theme stays crisp at any panel size.

The palette is a plain serialized field — you can save presets as assets or copy values
between projects.

## 2. Use your own font (one line of code)

`Fonts.Default` returns the built-in font. To use your own:

```csharp
// somewhere before UI creation (e.g. in your bootstrap):
TiledInventory.Fonts.Default = Resources.Load<Font>("MyFont");
```

The UI creates all text through `UIFactory.CreateText`, so one change re-fonts everything.

## 3. Add a new item

1. **Assets → Create → Tiled Inventory → Item Definition**.
2. Set name, description, icon (optional — a placeholder is drawn otherwise), rarity,
   category, max stack, equip slot, stats.
3. Reference it from a recipe or add it via `inventory.AddItem(new ItemStack(item, n))`.

No registry to update — the item registers itself on load.

## 4. Add a new recipe

Option A — inspector:
1. **Assets → Create → Tiled Inventory → Recipe Definition**.
2. Add inputs (item + count), outputs, craft time, level requirement, and optionally
   gold/XP/special costs, failure chance, cooldown.
3. Add it to `InventoryCraftingUI.Recipes` (or leave the list empty to auto-discover).

Option B — visual editor (Phase 2):
1. **Window → Tiled Inventory → Recipe Graph Editor**.
2. Add an item node and a recipe node, connect them, fill in outputs.
3. **Export Recipes…** → pick a folder → production-ready assets are created.

## 5. Tune the economy

| Knob | Where | Effect |
|---|---|---|
| `failureChance` | Recipe | Base chance the craft fails (materials consumed, no output). |
| `failureChanceReductionPerSkill` | Recipe | Each point of `PlayerProfile.CraftingSkill` reduces failure by this. |
| `cooldownSeconds` | Recipe | Seconds after finishing before the recipe can be queued again. |
| `goldCost` / `xpCost` | Recipe | Currency consumed at queue time (shown in the row tooltip). |
| `specialCosts` | Recipe | Extra materials on top of inputs (e.g. a catalyst). |
| `craftTime` | Recipe | Seconds per craft (unscaled time — runs while paused). |
| `levelRequirement` | Recipe | Locks the row in the UI until the player levels. |
| `xpPerLevel` | PlayerProfile | XP curve for leveling. |

> **Design note:** costs are consumed at queue time, not completion — so players can't
> double-spend materials by queueing. Cancelling a queued job does **not** refund.

## 6. Resize the inventory

`InventorySystem` owns grids by name. Two ways to size the default bag:

**Inspector (no code):** set `InventorySystem.defaultMainGridSize` — e.g. 9×8 for the
demo. This is the size used when the "Main" grid is first created.

**At runtime:**

```csharp
var grid = inventory.GetGrid("Main");
grid.Resize(new Vector2Int(7, 5));
```

The UI (and save files) adapt automatically. Save files store grid dimensions, so a
resized grid restores correctly.

## 7. Slot restrictions

Each slot has a `SlotRestriction`:

```csharp
var slot = inventory.MainGrid.GetSlot(0);
slot.restriction.locked = true;                                  // nothing enters
slot.restriction.allowedCategories.Add(ItemCategory.Weapon);     // weapons only
slot.restriction.allowedItems.Add(mySpecialItem);                // specific items only
```

The equipment grid uses `equipmentSlotType` per slot; you can build your own restricted
grids (e.g. a potion-only belt) the same way.

## 8. Swap the player profile

`CraftingSystem` talks to `PlayerProfile` through a serialized reference. Replace the
component with your own class that exposes the same members (`Level`, `Gold`, `Xp`,
`CraftingSkill`) and re-point the reference.

## 9. Swap the sounds

`AudioFeedback` uses procedurally generated clips by default. Assign your own via the
`ClickClip`, `CraftClip`, `EquipClip`, `FailClip` fields — the demo and UI use them
automatically. Remove the component entirely if you have your own audio system.

## 10. Replace the particle burst

`DemoController` calls `ParticleBurst.Play(...)` on `JobCompleted`. Replace that call
(or the `ParticleBurst` implementation) with your VFX system. In a shipped game you'd
hook `CraftingSystem.JobCompleted` yourself.

## 11. Analytics to your provider

Implement `IAnalyticsSink` and assign it:

```csharp
public class MySink : IAnalyticsSink
{
    public void Track(string eventName, IReadOnlyDictionary<string, object> props)
    {
        // push to GameAnalytics / Amplitude / your endpoint
    }
}

craftAnalytics.Sink = new MySink();
```

Events fired: `craft.started`, `craft.completed`, `craft.failed`, `trade.offer_created`,
`trade.accepted/declined/cancelled`.

## 12. Move the panels

The panels are laid out in `InventoryCraftingUI.BuildUI()` with explicit anchors
(1920×1080 reference space). To rearrange, adjust the anchor rects:

- Equipment: `(144,60)-(260,800)` left.
- Inventory: `(420,60)-(780,800)` center.
- Crafting: `(1216,60)-(560,800)` right.
- Trade: bottom drawer, toggled via `ToggleTradePanel()`.

## 13. Per-game stats

Add stats to the `StatType` enum (e.g. `Mana, CritChance`), assign them on items, read
them with `item.GetStat(StatType.Mana)` / `equipment.GetTotalStat(StatType.Mana)`.
