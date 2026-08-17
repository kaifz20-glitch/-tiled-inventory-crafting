# Asset Store Submission — Tiled Inventory & Crafting System

Ready-to-paste content for the Unity Asset Store publisher portal.
Every field below is pre-filled from the package. Keep the version in sync with
`Assets/DJS.TiledInventoryCrafting/package.json` and `CHANGELOG.md`.

---

## 1. Release version

```
1.2.0
```

Three-part semantic versioning. Bump `package.json` (`"version"`), `CHANGELOG.md`
(latest entry first) and this file together on every release.

## 2. Changelog

`CHANGELOG.md` (repo root) already lists every version, latest first. Paste the
top entry into the portal's changelog field, or link the file.

## 3. Summary (10–200 characters)

```
Grid inventory, crafting, equipment, trading and persistence for Unity RPGs — drag-and-drop UI, visual recipe editor, zero dependencies. uGUI only.
```

(152 characters — within the 10–200 limit.)

## 4. Description

Paste this into the Description field:

```
A visual-first inventory and crafting system for Unity, built entirely from code at runtime — no prefab wiring, no TextMeshPro, no Input System, no third-party packages.

The whole UI (canvas, panels, toolbar, tooltips, drag-and-drop) is created by a single call to InventoryCraftingUI.BuildUI(), and every color is driven by one RarityPalette asset you can restyle in the Inspector, with an optional font override. It fits any genre that needs item management: RPGs, survival and crafting games, looters, MMOs and management sims.

What you get:
- Grid inventory with configurable size, per-slot restrictions (categories, specific items, locked), stacking, drag to move/merge/swap/equip, and a drop zone to destroy items.
- Equipment grid with head/chest/legs/weapon/accessory slots and swap semantics.
- Crafting queue with sequential crafts, live progress bars, locked-recipe reasons, gold/XP/special material costs, failure chance and cooldowns.
- Visual recipe graph editor (GraphView) with live preview and export to production-ready RecipeDefinition assets.
- Persistence with swappable backends: local JSON slots, autosave, save-on-quit; crafting queue and cooldowns resume after close.
- Trading (player-to-player offers) and multiplayer plumbing (INetworkBackend: SyncVault HTTPS adapter + local simulation).
- Polish: rarity color-coding, search/filter/sort toolbar, hover tooltips, procedural audio, craft particle bursts, achievements, analytics hooks.
- Editor tooling: demo scene builder, content builder, package validator and three headless verification suites for CI.
```

## 5. Technical details (required)

```
- Runtime version: Unity 2022.3 LTS or Unity 6 (6000.x)
- UI: uGUI only (com.unity.ugui) — no TextMeshPro, no Input System, no external dependencies
- Fully code-generated UI — no prefabs to wire
- Customizable: RarityPalette asset drives all colors, optional font override
- ScriptableObject-driven: ItemDefinition, RecipeDefinition, AchievementDefinition
- Editor tools: demo scene builder, recipe graph editor, package validator, 3 headless verification suites
- Verified: 39 logic checks, 14 recipe-graph checks, package validation (demo scene, 9 items, 5 recipes, 2 achievements)
```

## 6. AI/ML declaration

> I have used AI/ML in my package creation process: [x]

Confirmed: **AI-assisted** — the code in this package was produced with
AI-assisted tooling, so this box stays checked at submission.

## 7. Keywords (up to 15)

```
inventory, crafting, grid, equipment, drag-drop, recipe, rpg, loot, trade, persistence, save-system, survival, achievements, item, editor-tool
```

(15 keywords — also set in `package.json` for Package Manager search.)

## 8. Images

All four required sizes are generated from the current UI render (cover-fit crops
of `demo_preview.png`). Files sit next to this document in the repo root —
**not inside the package folder**, so they don't ship with the asset.

| Type | Size | File |
|---|---|---|
| Icon image | 160 × 160 | `icon_160.png` |
| Card image | 420 × 280 | `card_420.png` |
| Cover image | 1950 × 1300 | `cover_1950.png` |
| Social media image | 1200 × 630 | `social_1200.png` |

Notes:
- The social image guidance says **no text or logo overlays** — the generated
  crop contains only in-game UI, which is fine, but consider a text-free variant
  if the store team pushes back.
- `demo_preview.png` (1920×1080) is the full in-app screenshot — use it for the
  portal's screenshots section.
- The crops are automatic placeholders. For a finished store page, commission
  branded marketing art at these exact sizes.

## 9. Suggested category

```
Tools → Gameplay (Inventory & Equipment)
```

(Adjust to the current portal taxonomy when you publish.)

## 10. Before you publish

- [ ] Fill in `documentationUrl`, `changelogUrl`, `licensesUrl` in
      `Assets/DJS.TiledInventoryCrafting/package.json` (your real repo links)
- [ ] Confirm the AI/ML declaration (section 6)
- [ ] Re-run all three verification suites in a clean Unity project
      (Tools → Tiled Inventory: Validate Package, Run Logic Verification,
      Verify Recipe Graph)
- [ ] Regenerate images after any UI change (`Tools → Tiled Inventory →
      Capture Demo Preview`, then re-run the cover-fit crop on the four
      required sizes — ask the assistant to regenerate them)
