# Spawn/Despawn Timing Analysis

> [!WARNING]
> **Line numbers are approximate** and may have shifted due to ongoing development. Last verified: December 2025.

## TOWN BOT SPAWN FLOW

### SpawnSingleZoneBot() / SpawnZoneBotPostSpawn()
- **T+0.0s**: `AI::spawn()` called directly (NOT through `createAI()`)
- **T+0.0s**: Client ID lookup via `NEWgetClientByName()`
  - If fails: `RetryGetAIId()` scheduled for **0.1s**
- **T+0.0s**: `BotInfoAiName` set immediately
- **T+0.0s**: Team set immediately (team 0)
- **T+0.0s**: `$TownBotSpawned[%botName] = %clientId` set
- **T+0.0s**: `InitTownBotPostSpawn()` scheduled for **0.1s** (line 4619, 4921, 5146)
- **T+0.0s**: `VerifyTownBotTeam()` scheduled for **0.2s** (line 4615, 4919, 6072)
- **T+0.0s**: `InitTownBotItemsForBot()` scheduled for **1.5s** (line 4621, 4922, 5147)

### InitTownBotPostSpawn() (runs at T+0.1s)
- Gets client ID from `$TownBotSpawned[%botName]` (already set at T+0.0s) ✓
- Sets team again (redundant but safe)
- Schedules `VerifyTownBotTeam()` for **0.2s** (line 6072) - **OVERLAPS with T+0.2s from spawn!**

### VerifyTownBotTeam() (runs at T+0.2s)
- Checks team, restores if needed
- If fails, retries after **0.1s** (line 7159)

### InitTownBotItemsForBot() (runs at T+1.5s)
- Mounts items on bot
- No dependencies on earlier functions

### createAIPostSpawn() (NOT called for town bots)
- Only called for TempSpawn/MarkerSpawn bots
- Town bots spawned via `SpawnZoneBots()` skip this entirely ✓

---

## ENEMY BOT SPAWN FLOW

### SpawnAI() → createAI()
- **T+0.0s**: `AI::spawn()` called
- **T+0.0s**: `createAIPostSpawn()` scheduled for **0.5s** (line ~146)
- **T+0.0s**: `SpawnAIGetClientId()` scheduled for **0.5s** (line ~1461)

### createAIPostSpawn() (runs at T+0.5s)
- Validates Player object exists
- Checks for ghost client IDs
- Skips if `SpawnBotInfo` is set (enemy bots) ✓
- Only processes TempSpawn/MarkerSpawn bots

### SpawnAIGetClientId() (runs at T+0.5s)
- Gets client ID via brute-force search
- Sets `BotInfoAiName` immediately
- Sets team immediately
- **T+0.5s**: `VerifyEnemyBotTeam()` scheduled for **0.5s** (line 2361) → **runs at T+1.0s**
- **T+0.5s**: `AI::setWeapons()` scheduled for **0.15s** (line 2388) → **runs at T+0.65s**

### Immediate Client ID Lookup (if found during SpawnAI)
- **T+0.0s**: Team set immediately
- **T+0.0s**: `VerifyEnemyBotTeam()` scheduled for **0.5s** (line 1452) → **runs at T+0.5s**
- **T+0.0s**: `SpawnAIGetClientId()` still scheduled for **0.5s** (line 1461) → **runs at T+0.5s**

### VerifyEnemyBotTeam() (runs at T+0.5s or T+1.0s)
- Checks team using Player object
- If Player object not ready, retries after **1.0s** (line 7212)

### AI::setWeapons() (runs at T+0.65s)
- Called from within `SpawnAIGetClientId()` (which runs at T+0.5s)
- Schedules for **0.15s** relative to when `SpawnAIGetClientId()` runs
- **TIMING**: Runs 0.15s after `SpawnAIGetClientId()` completes ✓

---

## DESPAWN FLOW

### DespawnZoneBots()
- Called via `ScheduleZoneBotDespawn()` with **1.5s delay** (line 5910)
- Checks `HasLoadedAndSpawned` flag to skip players ✓
- Checks `SpawnBotInfo` to identify enemy bots ✓
- Checks `BotInfoAiName` to identify town bots ✓
- Clears data arrays BEFORE killing (line 5876-5883)
- Marks client ID as recently freed (line 5886)
- Calls `Player::Kill()` which triggers `Player::onKilled()`

### Player::onKilled()
- Decrements spawn counters
- Clears `$SpawnAIScheduled` flag
- Marks client ID as recently freed (5s timer)
- Schedules `AI::delete()` for cleanup

---

## POTENTIAL TIMING ISSUES

### ✅ NO ISSUES FOUND:

1. **Town Bot Spawn**: All functions run in correct order
   - `InitTownBotPostSpawn()` at T+0.1s depends on `$TownBotSpawned` set at T+0.0s ✓
   - `VerifyTownBotTeam()` at T+0.2s is safe (team already set at T+0.0s) ✓
   - `InitTownBotItemsForBot()` at T+1.5s has no dependencies ✓

2. **Enemy Bot Spawn**: All functions run in correct order
   - `SpawnAIGetClientId()` at T+0.5s sets data before `VerifyEnemyBotTeam()` at T+1.0s ✓
   - `AI::setWeapons()` at T+0.65s runs after `SpawnAIGetClientId()` completes ✓
   - `createAIPostSpawn()` at T+0.5s skips enemy bots (has `SpawnBotInfo`) ✓

3. **Despawn**: All checks happen before killing ✓
   - Data cleared before `Player::Kill()` ✓
   - Player safeguards in place ✓

### ⚠️ MINOR OVERLAP (NOT A PROBLEM):

1. **Town Bot Team Verification**: 
   - `VerifyTownBotTeam()` scheduled twice:
     - Once at T+0.2s from spawn (line 4615, 4919)
     - Once at T+0.2s from `InitTownBotPostSpawn()` (line 6072)
   - **Impact**: Both run at T+0.2s, but second one is redundant (team already verified)
   - **Fix**: Not needed - redundant check is safe

2. **Enemy Bot Team Verification**:
   - If immediate client ID found: `VerifyEnemyBotTeam()` at T+0.5s (line 1452)
   - Normal flow: `VerifyEnemyBotTeam()` at T+1.0s (line 2361)
   - **Impact**: If immediate lookup succeeds, verification happens earlier (good!)
   - **Fix**: Not needed - earlier verification is better

---

## RECOMMENDATIONS

### ✅ CURRENT TIMING IS CORRECT:

1. **0.5s delays** for `createAIPostSpawn()` and `SpawnAIGetClientId()` are appropriate
   - Gives engine time to initialize Player objects
   - Prevents "team -1" errors

2. **0.1s delay** for `InitTownBotPostSpawn()` is appropriate
   - `$TownBotSpawned` is set immediately at T+0.0s
   - No dependency on `createAIPostSpawn()` (not called for town bots)

3. **0.15s delay** for `AI::setWeapons()` is appropriate
   - Runs after `SpawnAIGetClientId()` completes
   - Gives time for bot to be fully initialized

4. **0.2s delay** for `VerifyTownBotTeam()` is appropriate
   - Team is set immediately, but verification catches `UpdateTeam()` overrides

5. **1.5s delay** for `InitTownBotItemsForBot()` is appropriate
   - No dependencies, can run anytime after spawn

6. **1.5s delay** for `DespawnZoneBots()` is appropriate
   - Prevents crashes from too many operations at once
   - Gives time for zone change to complete

### 🔧 OPTIONAL IMPROVEMENTS:

1. **Remove redundant `VerifyTownBotTeam()` call** from `InitTownBotPostSpawn()`:
   - Team is already verified at T+0.2s from spawn
   - Second verification at T+0.2s from `InitTownBotPostSpawn()` is redundant
   - **Impact**: Low - redundant check is safe but unnecessary

---

## SUMMARY

**✅ NO CRITICAL TIMING ISSUES FOUND**

All delays are properly sequenced:
- Town bots: T+0.0s (spawn) → T+0.1s (init) → T+0.2s (verify) → T+1.5s (items)
- Enemy bots: T+0.0s (spawn) → T+0.5s (get ID) → T+0.65s (weapons) → T+1.0s (verify)
- Despawn: T+0.0s (trigger) → T+1.5s (despawn)
- **Town bot orphan respawn**: When a town bot's client ID is hijacked by an enemy bot, `CleanupOrphanedClientId()` detects this and schedules `SpawnSingleZoneBot()` after 2s if players are still in the zone.

No overlapping critical operations, no premature checks, all dependencies satisfied.


