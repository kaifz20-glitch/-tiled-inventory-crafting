# Tiled Inventory & Crafting System

A visual-first inventory and crafting system for Unity — **drag-and-drop crafting recipes,
a grid inventory that actually looks good, and a crafting UI you can ship as-is.**

No code required to get started. No JSON recipes. No framework to skin. Build a
collect-materials → craft-sword RPG loop in under two minutes on a fresh import.

---

## Quick start (under 2 minutes)

1. **Open the project** in Unity 2022 LTS (or Unity 6 — the project upgrades cleanly).
2. Run **Tools → Tiled Inventory → Build Demo Scene**.
3. Open `Assets/DJS.TiledInventoryCrafting/Demo/Demo.unity` and press **Play**.
4. Click **Gather Wood +5** and **Gather Iron Ore +3** (twice), smelt the ore, then
   click **Craft** on the *Forge Sword* recipe.

That's the whole loop: collect materials → craft sword.

### What you just saw

| Feature | Where |
|---|---|
| Grid inventory (5×5), drag to move, merge, swap, drop | Center panel |
| Rarity color-coding | Slot glows, tooltips |
| Search, category filter, sorting | Toolbar above the grid |
| Equipment (head/chest/legs/weapon) with drag-to-equip | Left panel |
| Crafting queue with progress bars, locked recipes grayed out | Right panel |
| Crafting failure chance, gold costs, cooldowns | Potion recipe |
| Save / load / reset | HUD buttons |
| Trade (offers, accept/decline) | Trade button |
| Achievements + analytics + audio + particles | Automatic |

---

## Feature map (Phase 1 · Phase 2 · Phase 3)

### Phase 1 — MVP (shipped)
- Grid-based inventory: configurable size, per-slot restrictions (categories, specific items, locked).
- `ItemDefinition` ScriptableObject: name, icon, rarity, category, max stack, equip slot, stats.
- Drag-and-drop UI: move, stack, swap, equip, drop (drag onto the drop zone to
  destroy an item). `Ctrl`-drag moves a single item.
- `RecipeDefinition` ScriptableObject: inputs → outputs, craft time, level requirement.
- Crafting queue: multiple crafts in sequence, cancel queued items, live progress.
- Recipe UI: available recipes, locked recipes grayed out with the reason, craft buttons.
- Persistence: local JSON save/load (slots), autosave, cloud backend interface.
- Polish: procedural audio, craft particle burst, hover tooltips.

### Phase 2 — Premium (shipped)
- **Visual recipe editor**: node graph (Window → Tiled Inventory → Recipe Graph Editor),
  live preview, export to production-ready recipe assets.
- Equipment slots (head, chest, legs, weapon, accessory).
- Item rarity color-coding, sorting, search & filter.
- Crafting economy: failure chance (reduced by crafting skill), gold/XP/special-material
  costs, cooldowns between crafts.
- Multiplayer plumbing: transport abstraction (`INetworkBackend`), shared crafting
  stations, inventory sync, player-to-player trading.

### Phase 3 — Stretch (shipped)
- Visual customization: swap colors, fonts, icons without code (inspector palette).
- Analytics hooks: craft started/completed/failed, trade events.
- Achievement system: stat counters, unlock events, toast UI, saved progress.

---

## Documentation index

- [Getting Started](GettingStarted.md) — install, first scene, manual wiring, configuration.
- [API Reference](ApiReference.md) — every public type, field, method and event.
- [Customization](Customization.md) — re-skin, new items/recipes, tuning economy.
- [Visual Recipe Editor](VisualRecipeEditor.md) — the node-graph editor.
- [Persistence](Persistence.md) — save formats, slots, cloud backends.
- [Multiplayer](Multiplayer.md) — SyncVault wiring, shared stations, trading.
- [Demo Guide](Demo.md) — the demo scene, how it's built, success criteria.
- [Troubleshooting](Troubleshooting.md) — FAQ and known issues.

## Requirements

- Unity **2022.3 LTS** or **Unity 6 (6000.x)**.
- Built with uGUI (`com.unity.ugui`) — no TextMeshPro, no Input System, no external
  packages required.

## Scope (what this package is NOT)

- ❌ UI theme customizer (the palette covers re-skinning; a full theme editor is out of scope)
- ❌ Full RPG progression system (bring your own; `PlayerProfile` is swappable)
- ❌ AI vendor NPC system
- ❌ Mod support
