# Troubleshooting & FAQ

## Import / compile

**Q: "The type or namespace name 'DJS.TiledInventoryCrafting' could not be found."**
Your scripts must reference the assemblies. If you use asmdefs, add
`DJS.TiledInventoryCrafting.Runtime` to your asmdef's references. If not, ensure the
`DJS.TiledInventoryCrafting` folder is under `Assets/`.

**Q: Compile error about `StandaloneInputModule` being deprecated.**
You're on Unity 6 with the new Input System as the active input handler. The demo uses
`StandaloneInputModule` (old input), which still compiles (deprecation warning) and works
when Active Input Handling is *Input Manager* or *Both*. For a pure Input System project,
replace it with `InputSystemUIInputModule` on the EventSystem.

**Q: The UI text shows as squares/boxes.**
Every text element assigns the built-in font explicitly, so this should not happen. If
you changed `Fonts.Default` to a missing font, restore it.

## The demo

**Q: Nothing happens when I press Play.**
Run **Tools → Tiled Inventory → Build Demo Scene** first, then open `Demo.unity`. If the
scene was deleted, rebuild it.

**Q: Where did my demo content go?**
`Tools → Tiled Inventory → Create Demo Content` regenerates items/recipes/achievements
(idempotent — existing assets are reused). `Build Demo Scene` calls it automatically.

**Q: The craft says "missing materials" but I gathered everything.**
The recipe consumes materials from the **Main** grid. Check the grid isn't full (full
grids leave leftovers — the gather buttons toast "Inventory full!"). Smelted **Iron
Bars** are a separate item from **Iron Ore** — check you crafted bars.

**Q: The potion keeps failing.**
That's the failure-chance showcase (25% base, reduced by crafting skill). Raise
`PlayerProfile.CraftingSkill` or lower `failureChance` on the recipe.

## Behaviour questions

**Q: Can I cancel a craft and get materials back?**
No — costs are consumed at queue time and cancelling a queued job does not refund. This
is documented, deliberate behaviour (prevents material duping).

**Q: Why did my items disappear after Load?**
Saves resolve items by `Id`. If you deleted the item asset (or it was regenerated with a
new id), the slot loads empty. Keep ids stable; delete your save if content changed
fundamentally (the Reset button clears the current state).

**Q: Dragging doesn't work / drag ghost is stuck.**
An `EventSystem` must exist (the UI creates one automatically). If you add your own
EventSystem with a different input module, ensure it's the active one. A stuck ghost
usually means `DragDropService.EndDrag` didn't run — release the mouse button.

**Q: The queue doesn't advance while the game is paused.**
Craft timers use unscaled time by design, so they **do** advance during pause. If you
want real-time-only crafting, change `Time.unscaledDeltaTime` to `Time.deltaTime` in
`CraftingSystem.Update()`.

## Multiplayer

**Q: The SyncVault backend never connects.**
Check: `BaseUrl`/`AuthToken` set (or env vars), room id shared across clients, and your
SyncVault project's endpoints match the adapter's expectations (see Multiplayer.md).
Without a SyncVault account, use `LocalSimulationBackend` — everything else is identical.

**Q: Two players craft from the same station and both see double results.**
Use the host-authoritative flow: only the host runs `TryQueue` on `crafting.request` and
broadcasts `QueueSync`. See Multiplayer.md.

## Scope

**Q: Where's the theme customizer / mod support / AI vendors?**
Deliberately out of scope (see the product plan). The palette covers re-skinning;
anything bigger is a separate product.
