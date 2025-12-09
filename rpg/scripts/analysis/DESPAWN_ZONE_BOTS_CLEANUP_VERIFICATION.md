# DespawnZoneBots Cleanup Verification

## Overview
This document verifies that `DespawnZoneBots()` properly cleans up all data and releases client IDs when despawning enemy bots from empty zones.

## Flow Analysis

### 1. DespawnZoneBots() Function (Ai.cs, lines 5605-5841)

**For Enemy Bots:**
- Calls `Player::Kill(%botId)` (line 5837)
- This triggers `Player::onKilled()` callback
- Sets `noDropLootbagFlag` before killing (line 5836)

**For Town Bots:**
- Directly deletes player object (line 5724)
- Cleans up all data before deletion (lines 5641-5716)
- Marks client ID as recently freed (line 5712)

### 2. Player::onKilled() Cleanup (playerdamage.cs, lines 1500-1887)

**For Enemy Bots (has SpawnBotInfo):**
- ✅ Decrements spawn counter (lines 1501-1513)
- ✅ Decrements bot tracking counters (lines 1515-1522)
- ✅ Frees AI number from $aiNumTable (lines 1524-1537)
- ✅ Clears $SpawnAIScheduled flag (line 1553)
- ✅ Marks client ID as recently freed (line 1558)
- ✅ Schedules AI::delete() (line 1564)
- ✅ Clears storeData fields (lines 1572-1587)
- ✅ Clears directive table (lines 1593-1605)
- ✅ Clears belt cached lists (lines 1607-1613)
- ✅ Clears AI movement data (lines 1616-1620)
- ✅ Cleans up pet/bot group data (lines 1622-1635)
- ✅ Clears BotInfoAiName for non-spawn-point bots (lines 1637-1652)

**Missing Cleanup:**
- ❌ Does NOT clear $EnemyBotData array entries
- ❌ Does NOT clear $BotInfoAiName direct array
- ❌ Does NOT clear $ClientData array entries (for enemy bots)

### 3. AI::onDroneKilled() Cleanup (Ai.cs, lines 2627-2962)

**For Enemy Bots:**
- ✅ Clears $SpawnAIScheduled flag (line 2737)
- ✅ Clears all storeData fields (lines 2845-2874)
- ✅ Clears $EnemyBotData array (lines 2876-2894)
- ✅ Clears $ClientData array (lines 2896-2899)
- ✅ Clears $BotInfoAiName direct array (line 2902)
- ✅ Clears directive table (lines 2904-2909)
- ✅ Clears belt cached lists (lines 2911-2917)
- ✅ Cleans up pet/bot group data (lines 2919-2929)
- ✅ Schedules AI::delete() (line 2949)
- ✅ Schedules player object deletion (line 2959)
- ✅ Schedules clearing of $ClientIdRecentlyFreed (line 2869)

**Issue:** AI::onDroneKilled() is designed for Drones, but enemy bots are Player objects. The function does try to handle Player objects via getClientIdFromName(), but it may not always be called for Player objects killed via Player::Kill().

## Problem Identified

**When DespawnZoneBots() kills enemy bots:**
1. ✅ Calls `Player::Kill()` which triggers `Player::onKilled()`
2. ❓ `AI::onDroneKilled()` may NOT be called for Player objects
3. ❌ `$EnemyBotData` array entries are NOT cleared
4. ❌ `$BotInfoAiName` direct array is NOT cleared (for spawn-point bots)
5. ❌ `$ClientData` array entries are NOT cleared

## Required Fixes

### Fix 1: Add Enemy Bot Data Cleanup to DespawnZoneBots()

After calling `Player::Kill()`, we should also clear the data arrays that `Player::onKilled()` doesn't clear:

```torquescript
// After Player::Kill() call (line 5837)
// CRITICAL: Clear enemy bot data arrays that Player::onKilled() doesn't clear
$EnemyBotData[%botId, "SpawnBotInfo"] = "";
$EnemyBotData[%botId, "SpawnTime"] = "";
$EnemyBotData[%botId, "BotInfoAiName"] = "";
$ClientData[%botId, "SpawnBotInfo"] = "";
$ClientData[%botId, "BotInfoAiName"] = "";
$BotInfoAiName[%botId] = "";  // Clear direct array
```

### Fix 2: Ensure Client ID is Marked as Recently Freed

`Player::onKilled()` already marks the client ID as recently freed (line 1558), so this is already handled.

## Verification Checklist

### Town Bots (DespawnZoneBots handles directly):
- ✅ BotInfoAiName cleared (line 5644)
- ✅ $TownBotData cleared (lines 5648-5655)
- ✅ $ClientData cleared (lines 5642-5643)
- ✅ $BotInfoAiName direct array cleared (line 5644)
- ✅ storeData fields cleared (lines 5666-5677)
- ✅ Directive table cleared (lines 5679-5684)
- ✅ Belt cached lists cleared (lines 5686-5692)
- ✅ AI number freed (lines 5699-5708)
- ✅ Client ID marked as recently freed (line 5712)
- ✅ Retry counters cleared (lines 5714-5716)
- ✅ $TownBotSpawned cleared (line 5751)
- ✅ $TownBotList updated (lines 5755-5776)

### Enemy Bots (DespawnZoneBots calls Player::Kill()):
- ✅ Spawn counter decremented (Player::onKilled, line 1506)
- ✅ Bot tracking counters decremented (Player::onKilled, lines 1516-1521)
- ✅ AI number freed (Player::onKilled, lines 1524-1537)
- ✅ $SpawnAIScheduled cleared (Player::onKilled, line 1553)
- ✅ Client ID marked as recently freed (Player::onKilled, line 1558)
- ✅ storeData fields cleared (Player::onKilled, lines 1572-1587)
- ✅ Directive table cleared (Player::onKilled, lines 1593-1605)
- ✅ Belt cached lists cleared (Player::onKilled, lines 1607-1613)
- ✅ AI movement data cleared (Player::onKilled, lines 1616-1620)
- ✅ Pet/bot group data cleared (Player::onKilled, lines 1622-1635)
- ❌ **$EnemyBotData array NOT cleared** (missing)
- ❌ **$BotInfoAiName direct array NOT cleared for spawn-point bots** (missing)
- ❌ **$ClientData array NOT cleared for enemy bots** (missing)

## Recommendation

Add cleanup of `$EnemyBotData`, `$BotInfoAiName`, and `$ClientData` arrays in `DespawnZoneBots()` after calling `Player::Kill()` for enemy bots, since `AI::onDroneKilled()` may not be called for Player objects.


