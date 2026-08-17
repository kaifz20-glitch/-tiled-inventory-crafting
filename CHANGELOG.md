# Changelog

All notable changes to the Tiled Inventory & Crafting System.

## [1.3.0] — DJS namespaces

### Changed
- All namespaces renamed to carry the `DJS.` prefix: `TiledInventory` →
  `DJS.TiledInventoryCrafting` and `TiledInventory.EditorTools` →
  `DJS.TiledInventoryCrafting.EditorTools`. Assemblies renamed to
  `DJS.TiledInventoryCrafting.Runtime` / `.Editor` (asmdefs + metas renamed
  together, so GUID references stay intact).

### New-user documentation
- **Welcome window** — opens once on first import (Tools → Tiled Inventory →
  Welcome) with quick-start buttons: Build Demo Scene, Open Documentation,
  Open Demo Scene.
- **Tools → Tiled Inventory → Open Documentation** — reveals the hidden
  `Documentation~` folder in the OS file browser (Unity hides `~` folders from
  the Project window by design, so docs were invisible in-editor).
- Visible `README.md` inside the package folder pointing to the full docs,
  plus a first-import note in GettingStarted.
- Docs refreshed for the namespace rename (assembly names, code samples,
  save path now `persistentDataPath/DJS.TiledInventoryCrafting/saves`).

### Store media
- **Capture Demo Preview** now renders a three-shot gallery in one run
  (all 1920×1080, ≥1200px min width for the store): `demo_preview.png`
  (main view), `screenshot_trade.png` (trade drawer open),
  `screenshot_crafting.png` (crafting queue populated). Derived store
  images regenerated from the current UI.

## [1.2.1] — layout overlap fix

### Fixed
- **Overlapping rows in the crafting panel**: the scroll-view content's
  `VerticalLayoutGroup` had `childControlHeight = false`, so recipe and queue rows
  (built with stretch anchors) all filled the entire scroll content and rendered on
  top of each other after crafting. The layout group now controls child heights, so
  rows stack with proper spacing. Same latent fix applied to the dropdown popup and
  the equipment slot list.
- Regenerated `demo_preview.png` (and the derived store images) against the fixed UI.

## [1.2.0] — asset store release

### Packaging
- Added `package.json` (UPM metadata) at the package root — the asset installs via
  Package Manager or `.unitypackage`.
- Fixed stale `Assets/TiledInventoryCrafting/...` paths throughout the editor tools and
  docs after the folder was renamed to `DJS.TiledInventoryCrafting` (Build Demo Scene,
  Validate Package, Run Logic Verification, Verify Recipe Graph and Capture Demo Preview
  all pointed at a non-existent folder).
- Removed internal pre-release planning notes from the project root.
- Regenerated the store-page screenshot against the current UI.

## [1.1.2] — drop items

### New
- **Drop items from inventory**: a red drop zone under the inventory grid. Drag an item
  (or stack) onto it to remove it from its source grid entirely — bag or equipment.
  `DragDropService.DropItem()` (Ctrl-drag drops a single item), `DropZone` component.

### Verification
- `LogicVerify.RunHeadless()` — batch/CI entry point (no dialog); exits 0 on pass,
  1 on fail. Suite now covers the drop-item path (39 checks).

## [1.1.0] — store presentation polish

### Visuals
- **Procedural item icons**: 128 px, one bespoke anti-aliased shape per demo item
  (wood log, iron ore, iron bar, leather hide, sword, helmet, chestplate, gold coin,
  potion) plus achievement medallions — rendered with SDF shapes, directional
  lighting and edge bevels; bilinear filtered.
- **Refined UI theme**: richer rarity colors, header gradients with a gold accent,
  rounded 9-sliced panels/slots/buttons/tooltips, rarity-tinted slot frames.
- **Layout overhaul**: fixed panel overlap bug, three evenly spaced columns, gradient
  backdrop, styled headers, 9×8 demo bag, larger equipment rows.
- Demo starts pre-populated (starter materials + equipped sword/helmet/chestplate)
  when the bag is empty, so the scene is presentable immediately.

### Tooling
- `Tools → Tiled Inventory → Capture Demo Preview` renders the demo UI to a
  1920×1080 `demo_preview.png` without play mode (store-page screenshots).
- `InventorySystem.defaultMainGridSize` — inspector-configurable default bag size.
- Icons are re-rendered on every content build pass (byte-compare keeps it a no-op).

## [1.1.1] — recipe graph theme

### Recipe Graph Editor visuals
- The whole editor now follows the runtime UI theme: dark canvas, palette-driven
  custom grid (replaces the default grid), rounded node shells with gradient-style
  headers and accent underlines, colored port pills, and gold connection edges.
- The theme reads the `RarityPalette` of an `InventoryCraftingUI` in the open scene
  and falls back to the defaults otherwise; item nodes underline with their item's
  rarity color.
- Themed live-preview bar with an accent header and color-coded warnings.

### Fixes & tooling
- **Fixed recipe export**: `GetOutputStacks` matched nothing (children of the
  outputs container are plain `VisualElement`s), so exported recipes always lost
  their outputs. Output rows are now tracked explicitly.
- `Tools → Tiled Inventory → Verify Recipe Graph` — edit-mode verification of the
  graph editor: builds the window, wires item nodes into a recipe, and confirms
  theming, edge styling, port sync, and the export path (14 checks).

## [1.0.0] — initial release

### Phase 1 — MVP
- Grid-based inventory manager: configurable grid size, per-slot restrictions
  (locked, allowed categories, allowed items, equipment slot type).
- ItemDefinition ScriptableObject: id, name, description, icon, rarity, category,
  max stack, equip slot, stats, sell value.
- Drag-and-drop inventory UI: move, merge, swap, equip; Ctrl-drag moves single items.
- RecipeDefinition ScriptableObject: inputs → outputs, craft time, level requirement.
- Crafting queue: sequential crafts, cancel queued jobs, live progress bars.
- Recipe UI: locked recipes grayed out with reasons, per-recipe craft buttons.
- Persistence: `SaveManager` with swappable backends; local JSON slots; autosave;
  cloud-save interface (`ISaveBackend`).
- Polish: procedural audio, craft particle burst, hover tooltips.
- Editor tools: demo scene builder, demo content builder, package validator,
  edit-mode logic verification suite (grid, crafting, save/load, UI build).

### Phase 2 — Premium
- Visual recipe editor (GraphView): item nodes, recipe nodes, connections, live
  preview, export to production-ready assets.
- Equipment slots: head, chest, legs, weapon, accessory with swap semantics.
- Rarity color-coding, sorting (name/rarity/type/count), search, category filter.
- Crafting economy: failure chance reduced by crafting skill, gold/XP/special
  material costs, cooldowns.
- Multiplayer: `INetworkBackend` abstraction, `NetworkCoordinator`,
  `LocalSimulationBackend`, `SyncVaultBackend` (HTTPS transport + cloud saves),
  shared crafting stations, inventory sync, player-to-player trading.

### Phase 3 — Stretch
- Visual customization: inspector `RarityPalette` for all UI colors; font override.
- Analytics: `IAnalyticsSink`, `CraftAnalytics` auto-tracking craft/trade events.
- Achievements: `AchievementDefinition` + `AchievementTracker` with stat counters,
  unlock events, save/restore, demo toast UI.
