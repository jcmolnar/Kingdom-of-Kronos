# Kingdom of Kronos - Codebase Context Prompt

Use this prompt at the start of a new chat session to provide context.

> [!WARNING]
> Last updated: **December 2025**. Line numbers in referenced documents are approximate.

---

## Overview

**Kingdom of Kronos** is a TorqueScript-based RPG mod for Starsiege: Tribes 1.

## Key Reference Documents

| Document | Purpose |
|----------|---------|
| [SCRIPT_OVERVIEW.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/scripts/SCRIPT_OVERVIEW.md) | High-level codebase architecture |
| [TOWN_BOT_CODE_LOCATIONS.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/scripts/TOWN_BOT_CODE_LOCATIONS.md) | Town bot functions and code flow |
| [SPAWN_DESPAWN_FUNCTION_LIST.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/scripts/SPAWN_DESPAWN_FUNCTION_LIST.md) | Complete spawn/despawn function list |
| [REAL_PLAYER_SAFEGUARDS.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/scripts/REAL_PLAYER_SAFEGUARDS.md) | Player protection mechanisms |
| [/.agent/workflows/torquescript-rules.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/.agent/workflows/torquescript-rules.md) | TorqueScript syntax rules and gotchas |

---

## Core Architecture (December 2025)

### 1. Bot Type System

**Town Bots** (NPCs, Merchants, Bankers):
- Spawned via `SpawnZoneBots()` → `SpawnSingleZoneBot()` → `AI::spawn()` directly
- `BotInfoAiName` starts with **"TownBot_"** prefix (e.g., "TownBot_merchant1")
- Do NOT have `SpawnBotInfo` (explicitly cleared)
- Always team **0** (Citizen)
- Dynamic loading: spawn when players enter zone, despawn when empty

**Enemy Bots** (Monsters):
- Spawned via `SpawnLoop()` → `AI::helper()` → `SpawnAI()` → `createAI()`
- Have BOTH `BotInfoAiName` AND `SpawnBotInfo`
- Team determined by race/zone
- Use spawn transaction system (ReserveSpawnSlot/CommitSpawnSlot/RollbackSpawnSlot)

### 2. Data Storage Arrays

| Array | Purpose |
|-------|---------|
| `$TownBotData[%id, "field"]` | Town bot data (BotInfoAiName, etc.) |
| `$EnemyBotData[%id, "field"]` | Enemy bot data (SpawnBotInfo, etc.) |
| `$ClientData[%id, "field"]` | Player + backwards-compatible bot data |
| `$BotType[%id]` | Fast O(1) bot type lookup ("town" or "enemy") |
| `storeData()/fetchData()` | Routes to correct array automatically |

### 3. Critical Safeguards

**Player Protection Priority Order:**
1. **Save file check** - `isFile("temp\\" @ %name @ ".cs")` (most reliable)
2. **AI-controlled check** - `Player::isAiControlled(%id)`
3. **Bot data markers** - `BotInfoAiName`, `SpawnBotInfo`
4. **Client ID range** - Players ≤2048, Bots 2049+

**Key Functions:**
- `IsRealPlayer(%clientId)` - Master check for real player detection
- `IsSafeToModify(%clientId, %context)` - Use before any bot cleanup
- `isTownBot(%clientId)` / `isEnemyBot(%clientId)` - Bot type detection

### 4. Spawn Timing (Current Values)

| Event | Delay |
|-------|-------|
| `SpawnAIGetClientId()` after spawn | **0.5s** |
| `InitTownBotPostSpawn()` | 0.1s |
| `VerifyTownBotTeam()` / `VerifyEnemyBotTeam()` | 0.2s |
| `AI::setWeapons()` after spawn | 0.15s (relative) |
| `InitTownBotItemsForBot()` | 1.5s |
| Despawn schedule after zone empty | 30s |

### 5. Watchdog System (Freeze Detection)

- Heartbeat exports state to `temp\watchdog_state.cs` every 5s
- Use `Watchdog_Enter("FunctionName")` / `Watchdog_Exit()` to track
- Use `Watchdog_LoopCheck(%counter, %limit)` for loop guards
- If server freezes, check `temp\watchdog_state.cs` for last function

### 6. Town Bot Orphan Respawn

When a town bot's client ID is hijacked by an enemy bot:
1. `CleanupOrphanedClientId()` detects `BotInfoAiName` starts with "TownBot_"
2. Clears `$TownBotSpawned[%botName]` and related data
3. Schedules `SpawnSingleZoneBot()` after 2s if zone has players

### 7. PlayerManager Plugin

C++ plugin providing fast client ID lookup:
- `PlayerManager::getFreeId()` - O(1) next free ID
- `PlayerManager::isIdFree(%id)` - Check if ID available
- Located in `Plugins/PlayerManager.dll`

---

## Key Files

| File | Purpose |
|------|---------|
| `Ai.cs` | Core bot spawning, despawning, safeguards, town bots |
| `playerdamage.cs` | Death handling, damage, loot |
| `spawn.cs` | SpawnLoop, spawn point management |
| `zone.cs` | Zone system, player tracking |
| `rpgfunk.cs` | Utilities, data storage, RefreshAll |
| `Server.cs` | Server initialization |
| `connectivity.cs` | Player connection handling |

---

## Common Patterns

**Bot Identification:**
```cpp
// Town bot check
if(String::findSubStr(%botInfoAiName, "TownBot_") == 0)
    // It's a town bot

// Enemy bot check  
if(%spawnBotInfo != "" && %spawnBotInfo != -1)
    // It's an enemy bot
```

**Safe Cleanup Pattern:**
```cpp
if(!IsRealPlayer(%clientId))
{
    // Safe to clean up bot data
    deleteObject(%playerObj);
}
```

---

## Working with AllScripts.txt

`AllScripts.txt` is a concatenated file containing all script files for easy reference.

**To extract a specific file:**
- Search for `=== FILE START: filename.cs ===` and `=== FILE END: filename.cs ===`

**To regenerate AllScripts.txt after editing files:**
```powershell
cd "C:\Users\Joe\Desktop\Kingdom of Kronos V0.8.1 Development\rpg\scripts"
$output = @()
Get-ChildItem -Filter "*.cs" | ForEach-Object {
    $output += "=== FILE START: $($_.Name) ==="
    $output += ""
    $output += Get-Content $_.FullName
    $output += ""
    $output += "=== FILE END: $($_.Name) ==="
    $output += ""
}
$output | Set-Content "..\AllScripts.txt" -Encoding UTF8
Write-Host "AllScripts.txt regenerated with $((Get-ChildItem -Filter '*.cs').Count) files"
```

**To update a single file in AllScripts.txt:**
```python
python -c "import re; content = open('AllScripts.txt', 'r', encoding='utf-8', errors='ignore').read(); file_content = open('filename.cs', 'r', encoding='utf-8', errors='ignore').read(); content = re.sub(r'=== FILE START: filename\.cs ===.*?=== FILE END: filename\.cs ===', '=== FILE START: filename.cs ===\n\n' + file_content + '\n\n=== FILE END: filename.cs ===', content, flags=re.DOTALL); open('AllScripts.txt', 'w', encoding='utf-8').write(content); print('AllScripts.txt updated')"
```

---

## Important Notes

1. **Always use function names** for code search (line numbers shift frequently)
2. **Check SCRIPT_OVERVIEW.md** first for architecture questions
3. **Use IsRealPlayer()/IsSafeToModify()** before any bot cleanup
4. **Town bots have no SpawnBotInfo** - this distinguishes them from enemies
5. **Spawn timing is 0.5s**, not 3.0s (updated December 2025)

---

**When working on this codebase:**
1. Read `/torquescript-rules` workflow for syntax rules
2. Check relevant reference documents above
3. Follow established safeguard patterns
4. Test with watchdog active for freeze detection

