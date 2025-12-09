# TempSpawn Bot System Analysis

## Overview
`TempSpawn` is a bot spawning system used for temporary bots that are spawned for specific events/quests and then deleted when the event ends. It's used by:
- **remortseal.cs**: Seal Battle bots (SealFighter1, SealMage1, SealGuardian1, etc.)
- **rpgarena.cs**: Colloseum bots (Round1, Round2, Round3, etc.)
- **comchat.cs**: Quest bots spawned from town bots, admin spawns, and pet spawning

## How TempSpawn Works

### Format
```
"TempSpawn <x> <y> <z> <team>"
```
- **Word 0**: `"TempSpawn"` (identifier)
- **Words 1-3**: Position coordinates (x, y, z)
- **Word 4**: Team number

### Flow
1. **AI::helper()** (line 1224-1227):
   - Extracts position from command string: `GetWord(%commandIssuer, 1-3)`
   - **Does NOT increment spawn point counters** (only SpawnPoint does this)

2. **SpawnAI()** (line 2067-2096):
   - Sets `SpawnBotInfo` to the command string
   - Extracts team from Word 4: `GetWord(%commandIssuer, 4)`
   - Sets team immediately: `GameBase::setTeam(%aiId, %team)`
   - Stores `botTeam` for RefreshAll() restoration
   - Sets `LCKconsequence` to "miss"
   - Clears stance (enemy bots shouldn't have stances)
   - Clears various data (RemortStep, belt items, flags, etc.)

3. **Bot Initialization**:
   - Goes through same `createAI()` → `SpawnAIGetClientId()` flow as SpawnPoint bots
   - Gets equipment from `$BotInfo` or `$BotEquipment`
   - Calls `HardcodeAIskills()` and `RefreshAll()`

## Compatibility Analysis

### ✅ What Works Correctly

1. **No Spawn Point Counter**: TempSpawn bots correctly do NOT increment `$numAIperSpawnPoint[]` counters, which is correct since they're temporary and not tied to spawn points.

2. **Team Assignment**: Team is correctly extracted from the command string and set immediately.

3. **Data Clearing**: All stale data is properly cleared (RemortStep, belt items, flags, etc.).

4. **BotInfoAiName**: Set correctly in `SpawnAIGetClientId()` for proper bot tracking.

5. **Cleanup**: When bots die, `Player::onKilled()` properly handles cleanup (including the new `$SpawnAIScheduled` flag clearing).

### ✅ Fixed Issues

1. **✅ RACE Setting** (FIXED):
   - **SpawnPoint bots** (line 2162-2173): Set RACE from armor type before UpdateTeam()
   - **TempSpawn bots** (line 2074-2082): NOW sets RACE from armor type (same logic as SpawnPoint)
   - **Fix Applied**: Added RACE setting logic for TempSpawn bots

2. **✅ Zone Clearing** (FIXED):
   - **SpawnPoint bots** (line 2180-2181): Clear `zone` and `tmpzone` to prevent false zone change detection
   - **TempSpawn bots** (line 2083-2085): NOW clears zone data
   - **Fix Applied**: Added zone clearing for TempSpawn bots

3. **✅ Immediate Client ID Lookup** (FIXED):
   - **SpawnPoint bots** (line 1359-1419): Try to get client ID immediately to set team before UpdateTeam() runs
   - **TempSpawn bots** (line 1362-1420): NOW uses immediate lookup, but correctly extracts team from command string (Word 4)
   - **Fix Applied**: Modified immediate lookup to check for TempSpawn and extract team from command string instead of inferring it

### ⚠️ Remaining Considerations

1. **Missing SpawnTime** (INTENTIONAL):
   - **SpawnPoint bots** (line 2176): Set `SpawnTime` for zone change detection
   - **TempSpawn bots**: Do NOT set SpawnTime
   - **Impact**: Zone change detection won't work for TempSpawn bots (they may be killed if they change zones)
   - **Status**: This is likely **intentional** - TempSpawn bots are temporary and should stay in their spawn zone
   - **Recommendation**: 
     - If TempSpawn bots should be allowed to change zones (e.g., quest bots), add SpawnTime setting
     - If TempSpawn bots should stay in their zone (e.g., Colloseum/Seal Battle), leave as-is

## Recommendations

### ✅ Completed Fixes
1. **✅ Added RACE setting** for TempSpawn bots (prevents warnings and ensures proper team assignment)
2. **✅ Fixed immediate client ID lookup** to correctly extract team from TempSpawn command string
3. **✅ Added zone clearing** for TempSpawn bots (prevents stale zone data issues)

### Remaining Considerations
4. **Consider SpawnTime** - Decide if TempSpawn bots should have zone change detection or not
   - Currently: TempSpawn bots do NOT have SpawnTime (likely intentional)
   - If quest bots need to change zones, add SpawnTime setting
   - If Colloseum/Seal Battle bots should stay in zone, leave as-is

### Low Priority
5. **Documentation**: Add comments explaining that TempSpawn bots are temporary and don't use spawn point counters

## Code Locations

- **TempSpawn Position Extraction**: `Ai.cs` line 1224-1227
- **TempSpawn Bot Initialization**: `Ai.cs` line 2067-2096
- **Usage Examples**:
  - `remortseal.cs` line 330, 335, 339, 580, 583, 585, 620, 623, 625
  - `rpgarena.cs` line 750, 754, 760, 767, 774, 1070, 1079, 1090, 1103-1105
  - `comchat.cs` line 4697, 8967, 9414

## Conclusion

TempSpawn is **fully compatible** with the current bot system. All critical missing features have been fixed:
- ✅ **RACE setting** added (prevents warnings)
- ✅ **Zone clearing** added (prevents stale zone data)
- ✅ **Immediate client ID lookup** fixed (correctly extracts team from command string)

The only remaining difference is **SpawnTime**, which is likely intentional since TempSpawn bots are temporary and should stay in their spawn zone (Colloseum, Seal Battle, etc.). If quest bots need zone change detection, SpawnTime can be added later.

TempSpawn bots now have the same initialization as SpawnPoint bots (except for spawn point counters, which is correct since they're temporary).

