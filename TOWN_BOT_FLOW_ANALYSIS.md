TOWN BOT SPAWN/DESPAWN FLOW ANALYSIS
=====================================

## Comparison: Enemy Bots vs Town Bots

### Enemy Bot Spawn Flow:
1. `SpawnAI()` called
2. `createAI()` called → `AI::spawn()` → **0.1s delay** → `createAIPostSpawn()`
3. `SpawnAI()` schedules **0.5s delay** → `SpawnAIGetClientId()`
4. `SpawnAIGetClientId()`:
   - Brute search **2049-2200** (player IDs start at 2049)
   - Checks `$ClientIdRecentlyFreed` (5 second cooldown)
   - Validates player object exists and is valid
   - Checks if player object is dead
   - Sets team immediately
   - Sets BotInfoAiName immediately

### Town Bot Spawn Flow (BEFORE FIXES):
1. `SpawnZoneBots()` called
2. `AI::spawn()` → **1.0s delay** → `SpawnZoneBotPostSpawn()`
3. `SpawnZoneBotPostSpawn()`:
   - Brute search **2000-2200** (WRONG - starts too early)
   - NO "recently freed" check (MISSING)
   - NO player object validation (MISSING)
   - If fails → **1.0s delay** → `RetryGetAIId()`
4. `RetryGetAIId()`:
   - Brute search **2000-2200** (WRONG - starts too early)
   - NO "recently freed" check (MISSING)
   - NO player object validation (MISSING)
   - Exponential backoff: 0.5s, 1.0s, 1.5s, 2.0s, 2.5s

### Town Bot Spawn Flow (AFTER FIXES):
1. `SpawnZoneBots()` called
2. `AI::spawn()` → **1.0s delay** → `SpawnZoneBotPostSpawn()`
3. `SpawnZoneBotPostSpawn()`:
   - Brute search **2049-2200** (FIXED - matches enemy bots)
   - Checks `$ClientIdRecentlyFreed` (ADDED - 5 second cooldown)
   - Validates player object exists and is valid (ADDED)
   - If fails → **1.0s delay** → `RetryGetAIId()`
4. `RetryGetAIId()`:
   - Brute search **2049-2200** (FIXED - matches enemy bots)
   - Checks `$ClientIdRecentlyFreed` (ADDED - 5 second cooldown)
   - Validates player object exists and is valid (ADDED)
   - Exponential backoff: 0.5s, 1.0s, 1.5s, 2.0s, 2.5s

## Issues Found and Fixed

### 1. **Brute Search Start ID**
**Issue:** Town bots started brute search at 2000, but player IDs start at 2049
- **Impact:** Wasted time checking invalid IDs (2000-2048)
- **Fix:** Changed start ID from 2000 to 2049 in both `SpawnZoneBotPostSpawn()` and `RetryGetAIId()`

### 2. **Missing "Recently Freed" Check**
**Issue:** Town bots didn't check `$ClientIdRecentlyFreed` before reusing client IDs
- **Impact:** Could reuse client IDs that are still being cleaned up, causing conflicts
- **Fix:** Added `$ClientIdRecentlyFreed` check (5 second cooldown) in both functions

### 3. **Missing Player Object Validation**
**Issue:** Town bots didn't validate player object exists and is valid before using client ID
- **Impact:** Could reuse client IDs with invalid/dead player objects (shell bots)
- **Fix:** Added player object validation (exists, isObject, not dead) in both functions

### 4. **Despawn Cleanup**
**Issue:** Town bots didn't mark client IDs as "recently freed" when despawning
- **Impact:** Client IDs could be immediately reused before cleanup completes
- **Fix:** Added `$ClientIdRecentlyFreed[%clientId] = getIntegerTime(true);` in `DespawnZoneBots()`

## Player::onKilled() for Town Bots

### Should Player::onKilled() be used for town bots?

**Answer: YES, but only when they're actually killed (not despawned)**

**Reasoning:**
- **Despawn:** `DespawnZoneBots()` calls `deleteObject()` directly, which does NOT trigger `Player::onKilled()`
- **Killed:** If a player kills a town bot, `Player::Kill()` is called, which DOES trigger `Player::onKilled()`
- **Current Code:** `Player::onKilled()` already handles town bots correctly (lines 1716-1803)
- **Conclusion:** Keep the town bot handling in `Player::onKilled()` - it only runs when bots are actually killed, not when they despawn

## Delay Comparison

### Enemy Bots:
- `createAIPostSpawn()`: **0.1s delay** after `AI::spawn()`
- `SpawnAIGetClientId()`: **0.5s delay** after `createAI()`

### Town Bots:
- `SpawnZoneBotPostSpawn()`: **1.0s delay** after `AI::spawn()`
- `RetryGetAIId()`: **1.0s delay** if first attempt fails, then exponential backoff

### Analysis:
- Town bots use longer delays (1.0s vs 0.1s/0.5s) which is appropriate since:
  - They spawn when players enter zones (less time-critical than enemy bot combat)
  - They need more time to register in client list (zone-based spawning is less predictable)
  - The 1.0s delay matches the comment: "prevents 'Could not get client ID' warnings on fresh server restarts"

## Recommendations

### ✅ Already Fixed:
1. Brute search start ID changed to 2049
2. "Recently freed" check added
3. Player object validation added
4. Despawn cleanup marks client IDs as "recently freed"

### ✅ No Changes Needed:
1. **Delays:** Town bot delays (1.0s) are appropriate for zone-based spawning
2. **Player::onKilled():** Correctly handles town bots when they're killed (not despawned)
3. **Despawn Flow:** `DespawnZoneBots()` correctly deletes objects without triggering `Player::onKilled()`

### 📝 Notes:
- Town bots despawn via `DespawnZoneBots()` → `deleteObject()` (no `Player::Kill()` call)
- Town bots killed by players → `Player::Kill()` → `Player::onKilled()` (handled correctly)
- Both flows now properly clean up `$TownBotSpawned` entries
- Both flows now properly mark client IDs as "recently freed"

## Testing Recommendations

1. **Spawn Test:**
   - Enter a zone with town bots
   - Verify bots spawn correctly
   - Check server logs for "recently freed" checks

2. **Despawn Test:**
   - Enter a zone, wait for bots to spawn
   - Leave the zone
   - Verify bots despawn correctly
   - Check that client IDs are marked as "recently freed"

3. **Kill Test:**
   - Kill a town bot (if possible)
   - Verify `Player::onKilled()` handles it correctly
   - Check that `$TownBotSpawned` is cleared

4. **Client ID Reuse Test:**
   - Despawn a town bot
   - Wait 5+ seconds
   - Spawn a new bot (enemy or town)
   - Verify it can reuse the client ID after cleanup delay


