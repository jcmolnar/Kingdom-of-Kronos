# Complete Town Bot Code Locations

This document lists all code related to town bots and where it's located in the codebase.

---

## PRIMARY FILE: `rpg/scripts/Ai.cs`

### Initialization & Registration

#### 1. `InitTownBots()`
**Location:** `Ai.cs:7956-8225`

**Purpose:** Initializes all town bots on server startup
- Scans `MissionGroup/TownBots` for bot definitions
- Calls `GatherBotInfo()` to parse bot info from SimGroups
- Determines zone for each bot using `GetBotZone()`
- Registers bots in `$TownBotRegistry`, `$TownBotZone[]`, and `$TownBotSpawned[]`
- Stores spawn positions and rotations in `$BotInfo[%name, SPAWN_POS]` and `$BotInfo[%name, SPAWN_ROT]`

**Called from:** `Server.cs:310` (`Server::onServerCreated()`)

---

### Spawn Functions

#### 2. `SpawnZoneBots(%zoneIndex)`
**Location:** `Ai.cs:9166-9580`

**Purpose:** Spawns all town bots for a specific zone
- Iterates through `$TownBotRegistry`
- Checks if bot belongs to target zone (`$TownBotZone[%botName] == %zoneIndex`)
- Checks if bot is already spawned (`$TownBotSpawned[%botName] == ""`)
- Calls `SpawnSingleZoneBot()` for each bot in the zone

**Called from:** 
- `InitTownBots()` (for each zone)
- Zone system when players enter a zone

---

#### 3. `SpawnSingleZoneBot(%botName, %zoneIndex)`
**Location:** `Ai.cs:8906-9165`

**Purpose:** Spawns a single town bot
- Constructs AI name: `"TownBot_" @ %botName`
- Gets race and armor from `$BotInfo[%botName, RACE]` and `$RaceToArmorType[]`
- Gets spawn position/rotation from `$BotInfo[%botName, SPAWN_POS]` and `$BotInfo[%botName, SPAWN_ROT]`
- Calls `AI::spawn()` directly (NOT through `createAI()`)
- Clears stale enemy bot data if client ID was previously used
- Handles retry logic for spawn conflicts (up to 3 attempts)
- Calls `SpawnZoneBotPostSpawn()` immediately after spawn

---

#### 4. `SpawnZoneBotPostSpawn(%aiName, %botName, %displayName, %zoneIndex)`
**Location:** `Ai.cs:8326-8551`

**Purpose:** Post-spawn initialization for town bots
- Gets client ID via `NEWgetClientByName(%displayName)` immediately
- If lookup fails, schedules `RetryGetAIId()` after 0.1s
- Sets `BotInfoAiName` with "TownBot_" prefix in `$TownBotData[]` and `storeData()`
- Clears `SpawnBotInfo` (town bots should NOT have it)
- Sets team to 0 (Citizen) immediately via `GameBase::setTeam()`
- Sets `HasLoadedAndSpawned` flag
- Stores client ID in `$TownBotSpawned[%botName]`
- Adds to `$TownBotList`
- Schedules `VerifyTownBotTeam()` after 0.2s
- Schedules `InitTownBotPostSpawn()` after 0.1s
- Schedules `InitTownBotItemsForBot()` after 1.5s

---

#### 5. `RetryGetAIId(%aiName, %botName, %displayName, %zoneIndex)`
**Location:** `Ai.cs:8552-8905`

**Purpose:** Retry function for getting client ID when timing issues occur
- Handles cases where `AI::spawn()` succeeds but client ID can't be found immediately
- Uses exponential backoff (up to 10 retries, delay up to 5s)
- Same flow as `SpawnZoneBotPostSpawn()` but with retry logic
- Falls back to multiple lookup methods:
  1. `NEWgetClientByName(%displayName)`
  2. Brute-force search through `GetEveryoneIdList()`
  3. Brute-force search with substring matching

---

### Post-Spawn Initialization

#### 6. `InitTownBotPostSpawn(%aiName, %name)`
**Location:** `Ai.cs:10316-10579`

**Purpose:** Finalizes town bot initialization after spawn
- Gets client ID from `$TownBotSpawned[%name]`
- Falls back to `NEWgetClientByName()` if needed
- Validates and sets race/armor (fixes wrong race after enemy bot deaths)
- Sets `BotInfoAiName` with "TownBot_" prefix
- Clears `SpawnBotInfo` (town bots should NOT have it)
- Sets team to 0 (Citizen)
- Schedules `VerifyTownBotTeam()` after 0.2s
- Sets AI variables to disable AI behavior:
  - `AI::setVar(%aiName, "pathType", "none")`
  - `AI::setVar(%aiName, "spotDist", 0)`
  - `AI::setVar(%aiName, "attackMode", 0)`
  - `AI::setVar(%aiName, "iq", 0)`
- Sets `dumbAIflag` to prevent AI behavior
- Stores items from `$BotInfo[%name, ITEMS]` in `storeData(%clientId, "TownBotItems")`
- Schedules `addToSetMissionCleanup()` after 0.1s
- Adds to `$TownBotList`

**Scheduled from:** `SpawnZoneBotPostSpawn()` at T+0.1s

---

#### 7. `addToSetMissionCleanup(%clientId)`
**Location:** `Ai.cs:10561-10616`

**Purpose:** Adds town bot to `MissionCleanup` SimSet
- Validates client ID and player object
- Adds player object to `MissionCleanup` for proper cleanup on server shutdown
- Ensures bots are cleaned up when server shuts down

**Scheduled from:** `InitTownBotPostSpawn()` at T+0.1s

---

#### 8. `InitTownBotItemsForBot(%clientId, %botName)`
**Location:** `Ai.cs:10519-10726`

**Purpose:** Mounts items on a single town bot
- Gets items from `fetchData(%clientId, "TownBotItems")` or `$BotInfo[%botName, ITEMS]`
- First pass: Adds all items to inventory (including equipped versions for armor/accessories)
- Second pass: Mounts weapons, armor, and shields
- Handles item mounting logic (weapons mount on talk by default, armor/shields mount immediately)

**Scheduled from:** `SpawnZoneBotPostSpawn()` at T+1.5s

---

#### 9. `InitTownBotItems()`
**Location:** `Ai.cs:10727-11060`

**Purpose:** Mounts items on all town bots (batch operation)
- Iterates through `$TownBotList`
- Gets items from `fetchData(%clientId, "TownBotItems")`
- Same two-pass logic as `InitTownBotItemsForBot()`
- Used for batch initialization or after rotation

**Called from:** 
- `RotateTownBotPostSpawn()` after rotation
- Manual admin commands

---

### Team Management

#### 10. `VerifyTownBotTeam(%clientId, %botName, %expectedTeam)`
**Location:** `Ai.cs:11602-11680`

**Purpose:** Verifies team was set correctly for town bots
- Checks if team is 0 (Citizen)
- Restores team if it was changed
- Retries after 0.1s if verification fails
- Town bots should ALWAYS be team 0 (Citizen)

**Scheduled from:**
- `SpawnZoneBotPostSpawn()` at T+0.2s
- `InitTownBotPostSpawn()` at T+0.2s
- `RetryGetAIId()` at T+0.2s

---

### Rotation Functions

#### 11. `RotateTownBot(%clientId, %rot)`
**Location:** `Ai.cs:11219-11270`

**Purpose:** Rotates a town bot to a new rotation
- Gets bot name from `fetchData(%clientId, "BotInfoAiName")` or `Client::getName()`
- Sets rotation directly via `GameBase::setRotation()` (no delete/respawn needed)
- Also updates player object rotation if needed

**Used by:** Guard rotation system (when guards rotate)

---

#### 12. `RotateTownBotPostSpawn(%aiName, %name)`
**Location:** `Ai.cs:11273-11356`

**Purpose:** Post-spawn initialization for rotated town bots
- Gets client ID from `$TownBotSpawned[%name]` or `NEWgetClientByName()`
- Sets bot name property
- Sets `NoDropLoot` flag
- Sets `BotInfoAiName`
- Stores items in `TownBotItems`
- Schedules `InitTownBotItems()` after 0.2s

---

### Despawn Functions

#### 13. `DespawnZoneBots(%zoneIndex)`
**Location:** `Ai.cs:9596-10121`

**Purpose:** Despawns all bots in a zone when it becomes empty
- First despawns town bots, then enemy bots
- For town bots:
  - Iterates through `$TownBotRegistry`
  - Finds bots in target zone (`$TownBotZone[%botName] == %zoneIndex`)
  - Validates bot is actually a bot (not a player)
  - Clears all bot data from `$TownBotData[]`, `$ClientData[]`, and `storeData()`
  - Clears conversation state for all players
  - Clears directive tables
  - Clears belt cached lists
  - Frees AI number from `$aiNumTable`
  - Marks client ID as recently freed
  - Calls `AddToGraveyard()` to register bot death
  - Calls `AI::delete()` to delete the bot
  - Clears `$TownBotSpawned[%botName]`
  - Decrements `$ActiveTownBots` and `$TotalActiveBots`
  - Removes from `$TownBotList`

**Called from:** `ScheduleZoneBotDespawn()` after 30s delay

---

#### 14. `ScheduleZoneBotDespawn(%zoneIndex)`
**Location:** `Ai.cs:10191-10198`

**Purpose:** Schedules bot despawn for a zone (30 seconds after all players leave)
- Schedules `DespawnZoneBots()` after 30s delay
- Prevents crash from too many operations happening at once

**Called from:** Zone system when zone becomes empty

---

#### 15. `CheckAndDespawnZoneBots(%zoneIndex)`
**Location:** `Ai.cs:10201-10203` (approximately)

**Purpose:** Checks if despawn is still needed before executing
- Prevents duplicate despawns
- Only despawns if flag is still set to "pending"

---

### Helper Functions

#### 16. `GetBotZone(%position)`
**Location:** `Ai.cs:8237-8317`

**Purpose:** Determines which zone a position belongs to
- Iterates through all zones
- Checks if position is within zone bounds (length/width)
- Returns zone index or 0 if not found
- Used by `InitTownBots()` to assign bots to zones

**Used by:** `InitTownBots()` to determine bot zones

---

#### 17. `GatherBotInfo(%group)`
**Location:** `Ai.cs:11358-11476`

**Purpose:** Parses bot info from SimGroup structure
- Extracts bot information (NAME, LVL, RACE, ITEMS, SHOP, etc.)
- Stores in `$BotInfo[%aiName, FIELD]` arrays
- Returns marker object for spawn position
- Used by both town bots and enemy bots

**Used by:** `InitTownBots()` to parse bot definitions

---

### Bot Type Detection

#### 18. `isTownBot(%clientId)`
**Location:** `Ai.cs:966-1014`

**Purpose:** Determines if a client ID belongs to a town bot
- **CRITICAL SAFEGUARD:** Checks for character save file first (real players have save files)
- Checks `$TownBotSpawned` registry (most reliable)
- Checks `BotInfoAiName` for "TownBot_" prefix
- Checks if `BotInfoAiName` exists but `SpawnBotInfo` doesn't
- Returns true if town bot, false otherwise

**Used by:** Many functions to distinguish town bots from enemy bots and players

---

#### 19. `IsTownBot(%clientId)`
**Location:** `Ai.cs:3160-3176`

**Purpose:** Alternative town bot detection function
- Checks `BotInfoAiName` and `SpawnBotInfo` via `fetchData()`
- Town bots have `BotInfoAiName` but no `SpawnBotInfo`
- Returns true if town bot, false otherwise

**Note:** Similar to `isTownBot()` but uses different logic

---

#### 20. `GetClientDataType(%clientId)`
**Location:** `rpgstats.cs:5-55`

**Purpose:** Determines which data array to use for a client
- Returns: "player", "townbot", or "enemybot"
- Checks `$TownBotData[]` array first for town bots
- Checks `$EnemyBotData[]` array for enemy bots
- Falls back to `$ClientData[]` for backwards compatibility

---

### Cleanup Functions

#### 21. `CleanupBot(%clientId, %aiName)`
**Location:** `Ai.cs:1074-1143`

**Purpose:** Comprehensive bot cleanup function
- Handles both town bots and enemy bots
- For town bots:
  - Decrements `$ActiveTownBots` and `$TotalActiveBots`
  - Frees AI number
  - Calls `PreSpawnCleanup()` to clear all data storage
  - Removes from `$TownBotList`

**Called from:** `AI::onDroneKilled()` after bot death

---

#### 22. `PreSpawnCleanup(%clientId)`
**Location:** `Ai.cs:774-957`

**Purpose:** Clears all bot data storage
- Clears `$TownBotData[]`, `$ClientData[]`, `$EnemyBotData[]`
- Clears `storeData()` fields
- Clears directive tables
- Clears belt cached lists
- Used for both town bots and enemy bots

---

## SECONDARY FILES

### `rpg/scripts/Server.cs`

#### 23. `Server::onServerCreated()`
**Location:** `Server.cs:310`

**Code:**
```torquescript
echo("===== Calling InitTownBots() =====");
InitTownBots();
echo("===== InitTownBots() completed, TownBotList: " @ $TownBotList @ " =====");
```

**Purpose:** Initializes town bots on server startup

---

### `rpg/scripts/zone.cs`

#### 24. `Zone::getNumPlayers(%zoneIndex)`
**Location:** `zone.cs:1539-1557`

**Purpose:** Gets number of players in a zone
- **TOWN BOT RELATED:** Added shell bot detection
- Skips bots with bot data but no player object (shell bots)
- Prevents shell bots from being counted in zone player counts

---

### `rpg/scripts/playerdamage.cs`

#### 25. `Player::onKilled(%this)`
**Location:** `playerdamage.cs:493-2557`

**Purpose:** Handles bot death
- **TOWN BOT RELATED:** For town bots:
  - Gets `BotInfoAiName` BEFORE clearing it
  - Frees AI number from `$aiNumTable`
  - Decrements `$ActiveTownBots` and `$TotalActiveBots`
  - Schedules `AI::delete()` after 1.0s delay

---

### `rpg/scripts/comchat.cs`

#### 26. `#despawnbots` Command
**Location:** `comchat.cs:3491-3550`

**Purpose:** Admin command to despawn all bots
- **TOWN BOT RELATED:** For town bots:
  - Extracts bot name from `BotInfoAiName` (removes "TownBot_" prefix)
  - Calls `AI::delete()` directly
  - Clears `$TownBotSpawned[%botName]`
  - Decrements `$ActiveTownBots` and `$TotalActiveBots`

---

#### 27. `#resetspawns` Command
**Location:** `comchat.cs:7244-7295`

**Purpose:** Admin command to reset spawns
- **TOWN BOT RELATED:** For town bots:
  - Calls `Player::Kill()` which triggers `Player::onKilled()`

---

#### 28. `#listbots` Command
**Location:** `comchat.cs` (approximately)

**Purpose:** Lists all bots
- **TOWN BOT RELATED:** Should list town bots (may need updating per user request)

---

### `rpg/scripts/shopping.cs`

#### 29. `SetupShop(%clientId, %id)`
**Location:** `shopping.cs:1-48`

**Purpose:** Sets up shop interface for a town bot
- **TOWN BOT RELATED:**
  - Gets bot name from `fetchData(%id, "BotInfoAiName")`
  - Removes "TownBot_" prefix if present
  - Falls back to `Client::getName()` or `%playerObj.name`
  - Gets shop info from `$BotInfo[%botName, SHOP]`

**Used by:** Player interaction system when talking to merchants/bankers

---

### `rpg/scripts/rpgfunk.cs`

#### 30. `NEWgetClientByName(%name)`
**Location:** `rpgfunk.cs:3323-3336`

**Purpose:** Gets client ID from display name
- **TOWN BOT RELATED:** Used by town bot spawn functions to find client ID
- Uses `GetEveryoneIdList()` and `Client::getName()` for lookup
- Case-insensitive comparison

**Used by:**
- `SpawnZoneBotPostSpawn()`
- `RetryGetAIId()`
- `InitTownBotPostSpawn()`
- `RotateTownBotPostSpawn()`

---

## GLOBAL VARIABLES

### Town Bot Registry & Tracking

**Location:** `Ai.cs:7950-7952` (initialization)

- **`$TownBotRegistry`**: List of registered bot names (space-separated string)
- **`$TownBotZone[%botName]`**: Maps bot name to zone index
- **`$TownBotSpawned[%botName]`**: Maps bot name to clientId if spawned, "" if not spawned
- **`$TownBotList`**: List of active town bot client IDs (space-separated string)
- **`$TownBotData[%clientId, "FIELD"]`**: Data array for town bots (BotInfoAiName, SpawnBotInfo, etc.)
- **`$TownBotRetryGetAIIdCount[%botName]`**: Retry counter for client ID lookup
- **`$TownBotSpawnRetry[%botName]`**: Retry counter for spawn conflicts

### Bot Tracking Counters

**Location:** `Ai.cs:44-46` (initialization)

- **`$ActiveTownBots`**: Count of active town bots
- **`$TotalActiveBots`**: Total count of all active bots (enemy + town)

---

## DATA STRUCTURES

### Bot Info Arrays

**Location:** `Ai.cs:11358-11476` (`GatherBotInfo()` function)

- **`$BotInfo[%botName, NAME]`**: Display name
- **`$BotInfo[%botName, RACE]`**: Bot race
- **`$BotInfo[%botName, LVL]`**: Bot level
- **`$BotInfo[%botName, ITEMS]`**: Items string
- **`$BotInfo[%botName, SHOP]`**: Shop items
- **`$BotInfo[%botName, SPAWN_POS]`**: Spawn position (set by `InitTownBots()`)
- **`$BotInfo[%botName, SPAWN_ROT]`**: Spawn rotation (set by `InitTownBots()`)
- **`$BotInfo[%botName, SPAWN_MARKER]`**: Spawn marker object

### StoreData Fields (Town Bot Specific)

- **`BotInfoAiName`**: Full AI name with "TownBot_" prefix (e.g., "TownBot_merchant1")
- **`TownBotItems`**: Items string for later mounting
- **`RACE`**: Bot race
- **`NoDropLoot`**: Flag to prevent loot drops
- **`MountWeaponOnSpawn`**: Flag to mount weapon on spawn (default: false)
- **`MountWeaponOnTalk`**: Flag to mount weapon on talk (default: false)
- **`ShowIdleMessage`**: Flag to show idle message after 30 minutes (default: false)
- **`LastInteractionTime`**: Timestamp of last player interaction
- **`dumbAIflag`**: Flag to disable AI behavior
- **`HasLoadedAndSpawned`**: Flag indicating bot is fully initialized
- **`SpawnInvuln`**: Spawn invulnerability flag
- **`botTeam`**: Bot team (always 0 for town bots)

---

## CODE FLOW SUMMARY

### Spawn Flow
1. **Server Startup** → `Server::onServerCreated()` → `InitTownBots()`
2. **Zone Entry** → `SpawnZoneBots(%zoneIndex)`
3. **Bot Spawn** → `SpawnSingleZoneBot(%botName, %zoneIndex)`
4. **Post-Spawn** → `SpawnZoneBotPostSpawn()` (T+0.0s)
5. **Initialization** → `InitTownBotPostSpawn()` (T+0.1s)
6. **Item Mounting** → `InitTownBotItemsForBot()` (T+1.5s)
7. **Team Verification** → `VerifyTownBotTeam()` (T+0.2s)

### Despawn Flow
1. **Zone Empty** → `ScheduleZoneBotDespawn(%zoneIndex)`
2. **Despawn Check** → `CheckAndDespawnZoneBots(%zoneIndex)`
3. **Despawn Execution** → `DespawnZoneBots(%zoneIndex)`
4. **Cleanup** → Clear all data, free AI number, delete bot

### Death Flow
1. **Bot Death** → `Player::onKilled()` (engine callback)
2. **Cleanup** → `AI::onDroneKilled()` → `CleanupBot()`
3. **Data Clearing** → Clear `$TownBotData[]`, `$TownBotSpawned[]`, etc.

---

## KEY DIFFERENCES: Town Bots vs Enemy Bots

### Town Bots
- Use "TownBot_" prefix in `BotInfoAiName`
- Do NOT have `SpawnBotInfo` (cleared to prevent confusion)
- Always team 0 (Citizen)
- Spawned via `SpawnZoneBots()` → `SpawnSingleZoneBot()` → `AI::spawn()` directly
- Despawned via `AI::delete()` (zone-based) or `Player::Kill()` (death-based)
- No spawn counters or cooldowns
- Stored in `$TownBotData[]` array
- Tracked in `$TownBotRegistry`, `$TownBotZone[]`, `$TownBotSpawned[]`

### Enemy Bots
- No prefix in `BotInfoAiName` (e.g., "Incarnate8")
- Have `SpawnBotInfo` set (e.g., "SpawnPoint 8678")
- Team determined by race/spawn source
- Spawned via `SpawnLoop()` → `AI::helper()` → `SpawnAI()` → `createAI()`
- Despawned via `Player::Kill()` (triggers `Player::onKilled()`)
- Use spawn counters and cooldowns
- Stored in `$EnemyBotData[]` array
- Tracked in `$BotRegistry[]` array

---

## SUMMARY

**Primary File:** `rpg/scripts/Ai.cs` (lines 7950-11842)
- All main town bot functions are in this file

**Secondary Files:**
- `rpg/scripts/Server.cs` - Initialization
- `rpg/scripts/zone.cs` - Zone detection
- `rpg/scripts/playerdamage.cs` - Death handling
- `rpg/scripts/comchat.cs` - Admin commands
- `rpg/scripts/shopping.cs` - Shop system
- `rpg/scripts/rpgfunk.cs` - Client ID lookup
- `rpg/scripts/rpgstats.cs` - Data type detection

**Total Functions:** 30+ functions related to town bots


