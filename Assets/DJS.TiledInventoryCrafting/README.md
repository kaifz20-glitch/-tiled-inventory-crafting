# DJS Tiled Inventory & Crafting System

Grid inventory, crafting, equipment, trading and persistence for Unity —
everything builds from code, no prefab wiring. uGUI only, zero dependencies.

## Quick start (under 2 minutes)

1. **Tools → Tiled Inventory → Build Demo Scene**
2. Open **`Assets/DJS.TiledInventoryCrafting/Demo/Demo.unity`**
3. Press **Play** — gather materials, craft a sword, equip it

A **Welcome window** opens automatically on first import with these buttons.
Reopen it anytime: **Tools → Tiled Inventory → Welcome**.

## Documentation

The full documentation ships in the hidden `Documentation~` folder
(Unity hides `~` folders from the Project window by design). Open it from:

**Tools → Tiled Inventory → Open Documentation**

| Doc | What it covers |
|---|---|
| `GettingStarted.md` | Install, manual wiring, configuration |
| `ApiReference.md` | Every public type, field, method, event |
| `Customization.md` | Re-skin, new items/recipes, economy tuning |
| `VisualRecipeEditor.md` | The node-graph recipe editor |
| `Persistence.md` | Save formats, slots, cloud backends |
| `Multiplayer.md` | SyncVault wiring, shared stations, trading |
| `Demo.md` | The demo scene and how it's built |
| `Troubleshooting.md` | FAQ and known issues |

## Verification

This package ships with three self-check suites (menu: **Tools → Tiled Inventory**):

- **Validate Package** — checks demo scene, items, recipes, achievements
- **Run Logic Verification** — 39 edit-mode checks (grid, crafting, save/load, UI)
- **Verify Recipe Graph** — 14 checks on the graph editor
