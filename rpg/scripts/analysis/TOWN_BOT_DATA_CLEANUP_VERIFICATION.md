# Town Bot Data Cleanup Verification

This document verifies that all data set during town bot spawn is properly cleared during despawn/kill.

## Data Set During Spawn

### 1. SpawnZoneBotPostSpawn() / RetryGetAIId() / SpawnSingleZoneBot()

#### Arrays:
- `$TownBotData[%clientId, "BotInfoAiName"]` = "TownBot_" @ %botName
- `$TownBotSpawned[%botName]` = %clientId
- `$TownBotList` = $TownBotList @ %clientId @ " "
- `$TownBotData[%clientId, "SpawnBotInfo"]` = "" (explicitly cleared)
- `$TownBotData[%clientId, "SpawnTime"]` = "" (explicitly cleared)
- `$ClientData[%clientId, "SpawnBotInfo"]` = "" (explicitly cleared)
- `$ClientData[%clientId, "SpawnTime"]` = "" (explicitly cleared)
- `$BotInfoAiName[%clientId]` = (not set directly, but should be cleared)

#### storeData():
- `storeData(%clientId, "BotInfoAiName", "TownBot_" @ %botName)`
- `storeData(%clientId, "SpawnBotInfo", "")` (explicitly cleared)
- `storeData(%clientId, "SpawnTime", "")` (explicitly cleared)

#### Retry Counters:
- `$TownBotRetryGetAIIdCount[%botName]` = (set during retries, cleared on success)
- `$TownBotSpawnRetry[%botName]` = (set during retries, cleared on success)

#### AI Number Table:
- `$aiNumTable[%aiNumber]` = (set by AI::helper, not in spawn functions)
- `$tmpbotn[%aiName]` = (set by AI::helper, not in spawn functions)

#### Client ID Recently Freed:
- `$ClientIdRecentlyFreed[%clientId]` = (cleared if old enough, not set during spawn)

### 2. InitTownBotPostSpawn()

#### Player Object:
- `%playerObj.name` = %name

#### storeData():
- `storeData(%clientId, "RACE", %botRace)`
- `storeData(%clientId, "NoDropLoot", "true")`
- `storeData(%clientId, "BotInfoAiName", "TownBot_" @ %name)`
- `storeData(%clientId, "SpawnBotInfo", "")` (explicitly cleared)
- `storeData(%clientId, "SpawnTime", "")` (explicitly cleared)
- `storeData(%clientId, "MountWeaponOnSpawn", "false")` (if empty)
- `storeData(%clientId, "MountWeaponOnTalk", "true")` (if empty)
- `storeData(%clientId, "ShowIdleMessage", "true"/"false")` (if empty)
- `storeData(%clientId, "LastInteractionTime", getSimTime())`
- `storeData(%clientId, "dumbAIflag", "true")`

#### Arrays:
- `$TownBotData[%clientId, "BotInfoAiName"]` = "TownBot_" @ %name
- `$TownBotList` = $TownBotList @ %clientId @ " " (if not already in list)

#### AI Variables (via AI::setVar):
- `AI::setVar(%aiName, "pathType", "none")`
- `AI::setVar(%aiName, "spotDist", 0)`
- `AI::setVar(%aiName, "attackMode", 0)`
- `AI::setVar(%aiName, "iq", 0)`

### 3. InitTownBotItemsForBot()

#### Items:
- Items added to inventory via `Player::incItemCount()`
- Items mounted via `Player::mountItem()`
- Belt items set via `Belt::GiveThisStuff()` -> `$TownBotData[%clientId, "QuestItems"]`, etc.

## Data Cleared During Despawn (DespawnZoneBots())

### Arrays:
- ✅ `$TownBotData[%clientId, "BotInfoAiName"]` = ""
- ✅ `$ClientData[%clientId, "BotInfoAiName"]` = ""
- ✅ `$BotInfoAiName[%clientId]` = ""
- ✅ `$TownBotData[%clientId, "SpawnBotInfo"]` = ""
- ✅ `$TownBotData[%clientId, "SpawnTime"]` = ""
- ✅ `$TownBotData[%clientId, "QuestItems"]` = ""
- ✅ `$TownBotData[%clientId, "KeyItems"]` = ""
- ✅ `$TownBotData[%clientId, "Consumables"]` = ""
- ✅ `$TownBotData[%clientId, "Armor"]` = ""
- ✅ `$TownBotData[%clientId, "Accessories"]` = ""
- ✅ `$TownBotData[%clientId, "Other"]` = ""
- ✅ `$TownBotSpawned[%botName]` = ""
- ✅ `$TownBotList` = (removed from list)
- ✅ `$aiNumTable[%townBotNumber]` = ""
- ✅ `$tmpbotn[%aiName]` = ""
- ✅ `$ClientIdRecentlyFreed[%clientId]` = getIntegerTime(true)

### storeData():
- ✅ `storeData(%clientId, "BotInfoAiName", "")`
- ✅ `storeData(%clientId, "noDropLootbagFlag", True)` (set, not cleared - but this is for cleanup)
- ✅ `storeData(%clientId, "zone", "")`
- ✅ `storeData(%clientId, "tmpzone", "")`

### Missing Cleanup:
- ❌ `storeData(%clientId, "RACE", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "NoDropLoot", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "MountWeaponOnSpawn", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "MountWeaponOnTalk", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "ShowIdleMessage", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "LastInteractionTime", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "dumbAIflag", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "SpawnBotInfo", "")` - NOT EXPLICITLY CLEARED (but was already "")
- ❌ `storeData(%clientId, "SpawnTime", "")` - NOT EXPLICITLY CLEARED (but was already "")
- ❌ `$Belt::CachedList[%clientId, ...]` - NOT CLEARED
- ❌ `$aidirectiveTable[%clientId, ...]` - NOT CLEARED
- ❌ `$TownBotRetryGetAIIdCount[%botName]` - NOT CLEARED (but only used during spawn)
- ❌ `$TownBotSpawnRetry[%botName]` - NOT CLEARED (but only used during spawn)

## Data Cleared During Kill (Player::onKilled())

### Arrays:
- ✅ `$TownBotSpawned[%botName]` = ""
- ✅ `$TownBotList` = (removed from list)
- ✅ `$TownBotData[%clientId, "QuestItems"]` = ""
- ✅ `$TownBotData[%clientId, "KeyItems"]` = ""
- ✅ `$TownBotData[%clientId, "Consumables"]` = ""
- ✅ `$TownBotData[%clientId, "Armor"]` = ""
- ✅ `$TownBotData[%clientId, "Accessories"]` = ""
- ✅ `$TownBotData[%clientId, "Other"]` = ""
- ✅ `$ClientData[%clientId, "BotInfoAiName"]` = ""
- ✅ `$ClientData[%clientId, "SpawnBotInfo"]` = ""
- ✅ `$ClientData[%clientId, "SpawnTime"]` = ""
- ✅ `$Belt::CachedList[%clientId, "QuestItems"]` = ""
- ✅ `$Belt::CachedList[%clientId, "KeyItems"]` = ""
- ✅ `$Belt::CachedList[%clientId, "Consumables"]` = ""
- ✅ `$Belt::CachedList[%clientId, "Armor"]` = ""
- ✅ `$Belt::CachedList[%clientId, "Accessories"]` = ""
- ✅ `$Belt::CachedList[%clientId, "Other"]` = ""
- ✅ `$aiNumTable[%townBotNumber]` = ""
- ✅ `$tmpbotn[%aiName]` = ""
- ✅ `$ClientIdRecentlyFreed[%clientId]` = getIntegerTime(true)

### storeData():
- ✅ `storeData(%clientId, "QuestItems", "")`
- ✅ `storeData(%clientId, "KeyItems", "")`
- ✅ `storeData(%clientId, "Consumables", "")`
- ✅ `storeData(%clientId, "Armor", "")`
- ✅ `storeData(%clientId, "Accessories", "")`
- ✅ `storeData(%clientId, "Other", "")`
- ✅ `storeData(%clientId, "zone", "")`
- ✅ `storeData(%clientId, "tmpzone", "")`
- ✅ `storeData(%clientId, "botTeam", "")`
- ✅ `storeData(%clientId, "AITarget", "")`
- ✅ `storeData(%clientId, "AILastDestination", "")`
- ✅ `storeData(%clientId, "AILastLoggedDist", "")`
- ✅ `storeData(%clientId, "AIMovementLoopRunning", "")`

### Missing Cleanup:
- ❌ `storeData(%clientId, "BotInfoAiName", "")` - NOT CLEARED (intentionally, for AI::onDroneKilled)
- ❌ `storeData(%clientId, "SpawnBotInfo", "")` - NOT CLEARED (intentionally, for AI::onDroneKilled)
- ❌ `storeData(%clientId, "SpawnTime", "")` - NOT CLEARED (intentionally, for AI::onDroneKilled)
- ❌ `$TownBotData[%clientId, "BotInfoAiName"]` - NOT CLEARED (intentionally, for AI::onDroneKilled)
- ❌ `$TownBotData[%clientId, "SpawnBotInfo"]` - NOT CLEARED (intentionally, for AI::onDroneKilled)
- ❌ `$TownBotData[%clientId, "SpawnTime"]` - NOT CLEARED (intentionally, for AI::onDroneKilled)
- ❌ `$BotInfoAiName[%clientId]` - NOT CLEARED (intentionally, for AI::onDroneKilled)
- ❌ `storeData(%clientId, "RACE", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "NoDropLoot", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "MountWeaponOnSpawn", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "MountWeaponOnTalk", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "ShowIdleMessage", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "LastInteractionTime", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "dumbAIflag", "")` - NOT CLEARED
- ❌ `storeData(%clientId, "DeathProcessed", "")` - NOT CLEARED (set to "town" but not cleared)

## Data Cleared During Kill (AI::onDroneKilled())

### Arrays:
- ✅ `$TownBotData[%aiId, "BotInfoAiName"]` = ""
- ✅ `$TownBotData[%aiId, "SpawnBotInfo"]` = ""
- ✅ `$TownBotData[%aiId, "SpawnTime"]` = ""
- ✅ `$TownBotData[%aiId, "QuestItems"]` = ""
- ✅ `$TownBotData[%aiId, "KeyItems"]` = ""
- ✅ `$TownBotData[%aiId, "Consumables"]` = ""
- ✅ `$TownBotData[%aiId, "Armor"]` = ""
- ✅ `$TownBotData[%aiId, "Accessories"]` = ""
- ✅ `$TownBotData[%aiId, "Other"]` = ""
- ✅ `$ClientData[%aiId, "BotInfoAiName"]` = ""
- ✅ `$ClientData[%aiId, "SpawnBotInfo"]` = ""
- ✅ `$ClientData[%aiId, "SpawnTime"]` = ""
- ✅ `$BotInfoAiName[%aiId]` = ""
- ✅ `$aidirectiveTable[%aiId, 0-99]` = "" (all cleared)
- ✅ `$Belt::CachedList[%aiId, "QuestItems"]` = ""
- ✅ `$Belt::CachedList[%aiId, "KeyItems"]` = ""
- ✅ `$Belt::CachedList[%aiId, "Consumables"]` = ""
- ✅ `$Belt::CachedList[%aiId, "Armor"]` = ""
- ✅ `$Belt::CachedList[%aiId, "Accessories"]` = ""
- ✅ `$Belt::CachedList[%aiId, "Other"]` = ""
- ✅ `$TownBotSpawned[%botName]` = ""
- ✅ `$TownBotList` = (removed from list)
- ✅ `$aiNumTable[%aiNumber]` = ""
- ✅ `$tmpbotn[%aiName]` = ""

### storeData():
- ✅ `storeData(%aiId, "BotInfoAiName", "")`
- ✅ `storeData(%aiId, "SpawnBotInfo", "")`
- ✅ `storeData(%aiId, "SpawnTime", "")`
- ✅ `storeData(%aiId, "QuestItems", "")`
- ✅ `storeData(%aiId, "KeyItems", "")`
- ✅ `storeData(%aiId, "Consumables", "")`
- ✅ `storeData(%aiId, "Armor", "")`
- ✅ `storeData(%aiId, "Accessories", "")`
- ✅ `storeData(%aiId, "Other", "")`
- ✅ `storeData(%aiId, "zone", "")`
- ✅ `storeData(%aiId, "tmpzone", "")`
- ✅ `storeData(%aiId, "botTeam", "")`
- ✅ `storeData(%aiId, "AITarget", "")`
- ✅ `storeData(%aiId, "AILastDestination", "")`
- ✅ `storeData(%aiId, "AILastLoggedDist", "")`
- ✅ `storeData(%aiId, "AIMovementLoopRunning", "")`
- ✅ `storeData(%aiId, "DeathProcessed", "")` - CLEARED after 5 seconds (line 3062)

### Missing Cleanup:
- ❌ `storeData(%aiId, "RACE", "")` - NOT CLEARED
- ❌ `storeData(%aiId, "NoDropLoot", "")` - NOT CLEARED
- ❌ `storeData(%aiId, "MountWeaponOnSpawn", "")` - NOT CLEARED
- ❌ `storeData(%aiId, "MountWeaponOnTalk", "")` - NOT CLEARED
- ❌ `storeData(%aiId, "ShowIdleMessage", "")` - NOT CLEARED
- ❌ `storeData(%aiId, "LastInteractionTime", "")` - NOT CLEARED
- ❌ `storeData(%aiId, "dumbAIflag", "")` - NOT CLEARED
- ❌ `$ClientIdRecentlyFreed[%aiId]` - NOT SET (but should be set like in despawn)

## Summary of Missing Cleanup

### Critical (should be cleared):
1. `storeData(%clientId, "RACE", "")` - Set in InitTownBotPostSpawn, never cleared
2. `storeData(%clientId, "NoDropLoot", "")` - Set in InitTownBotPostSpawn, never cleared
3. `storeData(%clientId, "dumbAIflag", "")` - Set in InitTownBotPostSpawn, never cleared
4. `storeData(%clientId, "MountWeaponOnSpawn", "")` - Set in InitTownBotPostSpawn, never cleared
5. `storeData(%clientId, "MountWeaponOnTalk", "")` - Set in InitTownBotPostSpawn, never cleared
6. `storeData(%clientId, "ShowIdleMessage", "")` - Set in InitTownBotPostSpawn, never cleared
7. `storeData(%clientId, "LastInteractionTime", "")` - Set in InitTownBotPostSpawn, never cleared
8. `$aidirectiveTable[%clientId, 0-99]` - NOT CLEARED in DespawnZoneBots (but cleared in AI::onDroneKilled)
9. `$Belt::CachedList[%clientId, ...]` - NOT CLEARED in DespawnZoneBots (but cleared in Player::onKilled and AI::onDroneKilled)
10. `$ClientIdRecentlyFreed[%aiId]` - NOT SET in AI::onDroneKilled (but set in DespawnZoneBots and Player::onKilled)

### Non-Critical (retry counters, only used during spawn):
- `$TownBotRetryGetAIIdCount[%botName]` - Only used during spawn retries
- `$TownBotSpawnRetry[%botName]` - Only used during spawn retries

## Recommendations

1. Add cleanup for all `storeData()` fields set in `InitTownBotPostSpawn()` to both `DespawnZoneBots()` and `AI::onDroneKilled()`
2. Add `$aidirectiveTable` cleanup to `DespawnZoneBots()`
3. Add `$Belt::CachedList` cleanup to `DespawnZoneBots()`
4. Add `$ClientIdRecentlyFreed` marking to `AI::onDroneKilled()`


