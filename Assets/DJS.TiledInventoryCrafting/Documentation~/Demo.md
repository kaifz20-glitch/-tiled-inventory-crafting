# Demo Guide

## The RPG scenario

Gather materials → smelt → craft a sword → equip it.

> The demo scene starts **pre-populated** for presentation: wood, ore, iron, leather,
> coins and potions are already in the 9×8 bag, and a sword, helmet and chestplate are
> equipped. (This only happens while the bag is empty, so saved games are untouched.
> Use **Reset** in the HUD to start from scratch and follow the full flow below.)

1. **Gather Wood +5** (HUD) — wood lands in the 9×8 grid, top-left first, auto-stacking.
2. **Gather Iron Ore +3** — twice, for six ore.
3. Smelt: find **Smelt Iron Bar** in the crafting panel → **Craft**. Watch the queue
   progress bar. (3 ore → 1 bar; repeat for 2 bars.)
4. **Forge Sword** needs **5 Wood + 2 Iron** → craft it. A particle burst + jingle plays.
5. Drag the sword from the grid onto the **Weapon** slot to equip it. The stats line
   updates ("Total: 8 dmg · 0 armor"). Drag it back out to unequip.

Also try:

- **Ctrl + drag** — move a single item instead of a stack.
- **Drag onto the red drop zone** under the grid — destroy an item (removes it from the
  inventory entirely).
- **Search / category / sort** toolbar — filter the grid.
- **Potion recipe** — costs 10 gold, has a 25% failure chance (reduced by crafting
  skill) and a 6 s cooldown, plus a special cost (1 gold coin). The economy knobs.
- **Save / Load / Reset** — persistence (saves to `persistentDataPath`).
- **Trade** — build an offer (offered items are reserved), send it; the local simulation
  echoes it back so you can accept it against yourself and watch the swap.
- **Achievements** — craft 3 items → "Blacksmith" toast; forge a sword → "Sword Master".

## How the demo is built

Nothing in the demo scene is hand-authored. `Editor/DemoSceneBuilder.cs` runs
**Tools → Tiled Inventory → Build Demo Scene** and:

1. Generates content assets (`Editor/DemoContentBuilder.cs`): 9 items with procedural
   128 px icons (one bespoke shape per item — log, ore, bar, hide, sword, helmet,
   chestplate, coin, potion), 5 recipes, 2 achievements.
2. Sets the demo bag to 9×8 (`InventorySystem.defaultMainGridSize`).
3. Creates a camera.
4. Creates one "GameSystems" GameObject with every component, wiring serialized
   references via `SerializedObject`.
5. Saves `Demo.unity` and pings it.

`InventoryCraftingUI` then builds the whole UI from code at runtime — the scene file
contains no prefab references to wire.

## Store-page screenshot

**Tools → Tiled Inventory → Capture Demo Preview** renders the fully-built UI
(equipment + populated inventory + crafting) to a 1920×1080 PNG saved next to the
project as `demo_preview.png` — no play mode required.

## Fresh-import check

To verify the "fresh import, no config" promise on another machine:

1. Copy the project folder to the target machine.
2. Open in Unity 2022.3 LTS or Unity 6. Let it import (first import takes a while).
3. **Tools → Tiled Inventory → Build Demo Scene** (regenerates demo assets + scene).
4. Open `Demo.unity`, press Play.
5. Gather wood (×1) + ore (×2), smelt, craft the sword. Timer stops under 2 minutes.

## Success criteria checklist

**Phase 1 done when:**

- [ ] Demo works on Unity 2022 LTS **and** 6.0.
- [ ] You can craft a sword from 5 wood + 2 iron in < 2 minutes (fresh import, no config).
- [ ] Inventory supports 5×5 grid, drag-and-drop, equip slots.
- [ ] All 3 video tutorials recorded and uploaded (see below).
- [ ] Documentation reviewed by someone outside your head.
- [ ] Tested on 2 other machines (not yours).

**Phase 2 done when:**

- [ ] Recipe editor is intuitive (test with one non-developer friend; redesign if they get lost).
- [ ] Multiplayer demo: 2 players craft from the same station with zero desyncs in 5 test runs.
- [ ] All existing features still work (no regressions).

**Phase 3 done when:**

- [ ] Phase 2 has been live 2+ weeks with < 10% refund rate.
- [ ] You have energy left and customers are asking for these features.

## Suggested tutorial videos

1. **"Craft a Sword in 2 Minutes"** — fresh import → demo scene → craft the sword.
2. **"Your First Item & Recipe"** — create content in the inspector, wire a custom scene.
3. **"Visual Recipe Editing"** — node graph → export → use in game.

## Verifying multiplayer locally

Run two editor instances (or one editor + one build) with `LocalSimulationBackend`, or
wire `SyncVaultBackend` with a real room id. Two players on the same `stationId` share
the queue; trades travel as messages. The 5-run zero-desync test is the Phase 2 gate.

## Automated checks (no play mode needed)

- **Tools → Tiled Inventory → Validate Package** — checks demo scene, content, recipe
  validity, unique ids, and the 5 wood + 2 iron → 1 sword scenario.
- **Tools → Tiled Inventory → Run Logic Verification** — an edit-mode test suite (39
  checks) that exercises grid stacking/restrictions/moves, crafting rejection paths and
  completion, a save→mutate→load round-trip, the full code-built UI construction, and
  dropping items via the drop zone.
- **Tools → Tiled Inventory → Verify Recipe Graph** — edit-mode test suite (14 checks)
  for the visual recipe editor: theming, edge styling, port sync, and the export path.

All run headless too (batch mode) — they are what verified this package on Unity
2022.3 LTS during development.
