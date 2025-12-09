TOWN BOT CLEANUP INVESTIGATION
================================

## Issues Found

### 1. **Bot Name Extraction Failure in AI::onDroneKilled()**
**Location:** `Ai.cs` line 2846-2851

**Problem:**
- `AI::onDroneKilled()` extracts bot name from `BotInfoAiName` to clear `$TownBotSpawned[%botName]`
- If `BotInfoAiName` is already cleared or empty, `%botName` becomes empty
- When `%botName` is empty, `$TownBotSpawned[%botName] = "";` doesn't clear the correct entry
- This leaves stale entries in `$TownBotSpawned`, preventing client IDs from being reused

**Root Cause:**
- `Player::onKilled()` may clear `BotInfoAiName` before `AI::onDroneKilled()` reads it
- No fallback mechanism to find bot name if `BotInfoAiName` is missing

**Fix Applied:**
- Added fallback to extract bot name from `%aiName` parameter (full AI name like "TownBot_merchant1")
- Added fallback to search `$TownBotSpawned` by clientId to find the bot name
- Added logging to track when fallbacks are used

### 2. **Bot Name Extraction Failure in Player::onKilled()**
**Location:** `playerdamage.cs` line 1728-1740

**Problem:**
- `Player::onKilled()` extracts bot name from `BotInfoAiName` to clear `$TownBotSpawned[%botName]`
- If `BotInfoAiName` is empty, `%botName` becomes empty
- When `%botName` is empty, `$TownBotSpawned[%botName] = "";` doesn't clear the correct entry

**Root Cause:**
- Same as issue #1 - no fallback if `BotInfoAiName` is missing

**Fix Applied:**
- Added fallback to search `$TownBotSpawned` by clientId to find the bot name
- Added logging to track when fallbacks are used
- Ensures `$TownBotSpawned` is cleared even if bot name is unknown

### 3. **Missing $TownBotData in Fallback Search**
**Location:** `Ai.cs` line 2619-2630

**Problem:**
- `AI::onDroneKilled()` checks `$BotInfoAiName[%aiId]`, `$EnemyBotData`, and `$ClientData` for `BotInfoAiName`
- Does NOT check `$TownBotData[%aiId, "BotInfoAiName"]`
- `Player::onKilled()` clears `$ClientData[%aiId, "BotInfoAiName"]` but NOT `$TownBotData`
- This means `AI::onDroneKilled()` can't find the bot name even though it exists in `$TownBotData`

**Root Cause:**
- Incomplete fallback search - missing `$TownBotData` array

**Fix Applied:**
- Added `$TownBotData[%aiId, "BotInfoAiName"]` to the fallback search chain
- Checks `$TownBotData` BEFORE `$EnemyBotData` and `$ClientData` (since town bots use `$TownBotData`)

## Summary of Fixes

### Ai.cs - AI::onDroneKilled() (Town Bot Section)
1. **Enhanced bot name extraction:**
   - Primary: Extract from `BotInfoAiName` (if available)
   - Fallback 1: Extract from `%aiName` parameter (full AI name)
   - Fallback 2: Search `$TownBotSpawned` by clientId

2. **Enhanced $TownBotSpawned cleanup:**
   - If bot name is found: Clear `$TownBotSpawned[%botName]`
   - If bot name is unknown: Search `$TownBotSpawned` by clientId and clear the matching entry
   - Added logging for all cleanup operations

3. **Enhanced BotInfoAiName fallback search:**
   - Added `$TownBotData[%aiId, "BotInfoAiName"]` to fallback chain
   - Checks `$TownBotData` before `$EnemyBotData` and `$ClientData`

### playerdamage.cs - Player::onKilled() (Town Bot Section)
1. **Enhanced bot name extraction:**
   - Primary: Extract from `BotInfoAiName` (if available)
   - Fallback: Search `$TownBotSpawned` by clientId

2. **Enhanced $TownBotSpawned cleanup:**
   - If bot name is found: Clear `$TownBotSpawned[%botName]`
   - If bot name is unknown: Search `$TownBotSpawned` by clientId and clear the matching entry
   - Added logging for all cleanup operations

## Why This Happens

1. **Race Condition:** `Player::onKilled()` runs before `AI::onDroneKilled()`, and may clear data before `AI::onDroneKilled()` can read it

2. **Data Clearing Order:** `Player::onKilled()` clears `$ClientData` but not `$TownBotData`, causing `AI::onDroneKilled()` to miss the data if it only checks `$ClientData`

3. **Missing Fallbacks:** No mechanism to find bot name if `BotInfoAiName` is already cleared

## Expected Behavior After Fixes

1. **Town bots dying:**
   - `Player::onKilled()` extracts bot name (with fallback if needed)
   - `Player::onKilled()` clears `$TownBotSpawned[%botName]` (with fallback if needed)
   - `AI::onDroneKilled()` extracts bot name (with multiple fallbacks)
   - `AI::onDroneKilled()` clears `$TownBotSpawned[%botName]` again (idempotent - safe to call twice)
   - Client ID is marked as "recently freed" to prevent immediate reuse

2. **Client ID reuse:**
   - Client IDs are properly freed from `$TownBotSpawned`
   - Client IDs can be reused after cleanup delay (5 seconds)
   - No stale entries blocking new bot spawns

3. **Logging:**
   - All cleanup operations are logged
   - Warnings are logged when fallbacks are used
   - Easier to debug future issues

## Testing Recommendations

1. **Kill a town bot manually** and verify:
   - `$TownBotSpawned[botName]` is cleared
   - Client ID is marked as "recently freed"
   - No stale entries remain

2. **Spawn a new bot** using the same client ID after cleanup delay:
   - Should succeed without conflicts
   - Should not see "client ID in use by town bot" errors

3. **Check #listtownbots** after killing bots:
   - Should show 0 stale entries
   - All alive bots should be in zones with players

4. **Test zone despawn** (#despawnemptyzones):
   - Should properly clear `$TownBotSpawned` entries
   - Should free client IDs for reuse


