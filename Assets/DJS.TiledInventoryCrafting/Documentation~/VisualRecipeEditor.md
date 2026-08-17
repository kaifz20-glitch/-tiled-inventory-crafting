# Visual Recipe Editor (Phase 2)

Open with **Window → Tiled Inventory → Recipe Graph Editor** (or
**Tools → Tiled Inventory → Recipe Graph Editor**).

## Concept

Recipes are defined by dragging nodes instead of editing ScriptableObjects:

- **Item node** — an item + a count. One output port.
- **Recipe node** — craft parameters (name, time, level, costs, cooldown, failure) and a
  list of outputs. One input port that accepts **many** item connections.

Connect item nodes into a recipe node's input port to declare its materials:

```
[ Wood x5 ] ─┐
              ├─▶ [ Forge Sword ]
[ Iron x2 ] ─┘        ├ 3s, lvl 1
                      └ Output: Sword x1
```

## Workflow

1. **New Item Node** — pick an item from the dropdown, set the count.
2. **New Recipe Node** — name it, set craft time / level / costs / cooldown / failure,
   and add outputs with **+ Output**.
3. **Connect** — drag from an item node's port to the recipe node's input port. The port
   label updates live: `5 Wood + 2 Iron`.
4. **Read the preview** — the bottom panel shows every recipe with a live summary and
   warnings (missing inputs/outputs, fail %, cooldown, costs).
5. **Export Recipes…** — pick a folder; each recipe node becomes a production-ready
   `RecipeDefinition` asset (deleting any asset with the same name first).

## Interactions

| Action | How |
|---|---|
| Pan | drag empty space / middle mouse |
| Zoom | scroll wheel |
| Select | click; drag a rectangle to multi-select |
| Move | drag a node |
| Remove | select, then **Remove Selected** in the toolbar |

## Live preview

The bottom panel updates on every change: node moves, field edits, connections, exports.
It flags recipes with no inputs or no outputs, and annotates economy settings.

## Theme

The editor is themed to match the runtime UI (same `RarityPalette`): a dark canvas with
custom-drawn grid lines, rounded node shells with gradient-style headers and accent
underlines, colored port pills and gold connection edges.

- The theme picks up the `RarityPalette` of an `InventoryCraftingUI` in the open scene
  (e.g. the demo scene) and falls back to the default palette otherwise.
- Item nodes underline their header with the **item's rarity color** when an item is
  assigned.
- The preview bar mirrors the UI panels: accent top border, accent header, and warnings
  (missing inputs/outputs) shown in red.

## Verification

`Tools → Tiled Inventory → Verify Recipe Graph` builds the window headless, wires two
item nodes into a recipe, and confirms theming, edge styling, port sync and the export
path (14 checks) — runnable from the command line for CI.

## Notes

- Export requires **at least one input and one output** on each recipe node.
- Recipe asset names come from the node's name field (invalid filename characters are
  sanitized).
- Exported recipes immediately register in the `Registry`, so a UI that auto-discovers
  recipes picks them up without reconfiguring.
- The editor only writes assets when you click **Export** — your graph itself is not
  persisted. Keep exported assets as your source of truth.
