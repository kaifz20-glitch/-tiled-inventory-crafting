# Persistence

## What gets saved

`SaveManager.Save()` snapshots everything into one JSON blob:

- **Profile** — level, XP, gold, crafting skill.
- **All grids** — name, dimensions, every slot: stack (item id + count) and restrictions
  (locked, equipment slot, allowed categories/items).
- **Crafting queue** — recipe id, remaining/total seconds (in-progress crafts resume).
- **Cooldowns** — recipe id → when it ends (unscaled time).
- **Achievements** — unlocked ids and stat counters.

Items and recipes are stored by **stable id** (`ItemDefinition.Id`), resolved through the
`Registry` at load time. Add/remove assets freely — saves resolve by id, not by asset
reference.

## Slots

`SaveManager.Slot` names the slot; `Save()`/`Load()`/`DeleteSave()` operate on it.
Autosave writes to the same slot on an interval (`autoSaveInterval`, default 60 s) and on
quit (`OnApplicationQuit`).

## Local backend (default)

`LocalFileSaveBackend` writes to:

```
Application.persistentDataPath/TiledInventory/saves/{slot}.json
```

This is correct on every platform (Windows, macOS, Linux, Android, iOS, WebGL).

## Cloud backend (SyncVault)

`SyncVaultSaveBackend` implements the same `ISaveBackend` contract over HTTPS, so
switching is one line:

```csharp
saveManager.Backend = new SyncVaultSaveBackend("https://<your-syncvault-endpoint>/v1", "<player-token>");
```

`SyncVaultSaveBackend` also reads `SYNCVAULT_BASE_URL` and `SYNCVAULT_TOKEN` environment
variables. The save is a **per-player key/value blob** — use a player-scoped token so
each player reads/writes their own data.

> The sync `Save()`/`Load()` busy-waits on the request, which is fine for tooling but not
> for production. Use the async helpers instead:
>
> ```csharp
> StartCoroutine(syncBackend.SaveAsync(slot, json, ok => ...));
> StartCoroutine(syncBackend.LoadAsync(slot, (ok, json) => ...));
> ```

### Writing your own backend

Implement `ISaveBackend` (three methods) and assign it to `SaveManager.Backend`.
The contract is deliberately tiny: named slots in, JSON out.

## JSON format (excerpt)

```json
{
  "version": "1.0.0",
  "savedAtUtc": "2026-08-15T12:00:00Z",
  "profile": { "level": 1, "xp": 40, "gold": 250, "craftingSkill": 1 },
  "grids": [
    {
      "gridName": "Main",
      "width": 5,
      "height": 5,
      "slots": [
        { "itemId": "demo_wood", "count": 5, "locked": false,
          "equipmentSlotType": "None",
          "allowedCategories": [], "allowedItems": [] }
      ]
    }
  ],
  "craftQueue": [ { "recipeId": "demo_sword", "remainingSeconds": 1.2, "totalSeconds": 3.0 } ],
  "cooldowns": [],
  "unlockedAchievements": [],
  "achievementStats": []
}
```

Serialization is plain `JsonUtility` (field-based DTOs, no third-party packages).

## Load-time behaviour

- Grids are recreated by name with saved dimensions (a 7×5 grid saves and restores as 7×5).
- The in-progress craft resumes from `remainingSeconds`.
- Expired cooldowns are ignored.
- Items that no longer exist (deleted assets) are skipped gracefully.
