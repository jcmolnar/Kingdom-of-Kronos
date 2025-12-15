# Complete Function List: Spawn and Despawn Flows

This document lists all functions currently used in the spawn and despawn flows for enemy bots and town bots.

---

## ENEMY BOT SPAWN FLOW

### Initialization & Entry Points
1. **`InitSpawnPoints()`** (`Spawn.cs:124`)
   - Initializes all spawn points on server startup
   - Calls `SpawnLoop()` for each spawn point

2. **`SpawnLoop(%this)`** (`Spawn.cs:164`)
   - Main spawn loop for each spawn point
   - Checks zone flags, cooldowns, and spawn limits
   - Calls `ReserveSpawnSlot()` to reserve a slot
   - Calls `AI::helper()` to spawn bot
   - Calls `RollbackSpawnSlot()` on failure

### Spawn Transaction System
3. **`ReserveSpawnSlot(%spawnPoint)`** (`Spawn.cs:8`)
   - Atomically reserves a spawn slot (increments counter)
   - Returns true if slot was successfully reserved

4. **`CommitSpawnSlot(%spawnPoint)`** (`Spawn.cs:56`)
   - Confirms spawn was successful (keeps the reserved slot)
   - Called after bot is fully spawned and registered

5. **`RollbackSpawnSlot(%spawnPoint)`** (`Spawn.cs:79`)
   - Reverts a reserved slot (decrements counter)
   - Called on ANY failure: engine error, script crash, duplicate ID, etc.

### Core Spawn Functions
6. **`AI::helper(%aiName, %displayName, %commandIssuer, %loadout, %spawnPointId)`** (`Ai.cs:3232`)
   - Gets AI number from `getAInumberFromName()`
   - Constructs new bot name with number
   - Calls `SpawnAI()` to spawn the bot

7. **`SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout, %spawnPointId)`** (`Ai.cs:3323`)
   - Checks if bot already exists via `AI::getClientIdFromName()`
   - Checks scheduled spawn flag `$SpawnAIScheduled[%newName]`
   - Calls `createAI()` to create the bot
   - Schedules `SpawnAIGetClientId()` after 3.0s delay

8. **`createAI(%aiName, %markerGroup, %name, %skipPostSpawn, %bypassRaceCheck)`** (`Ai.cs:1189`)
   - Calls `AI::spawn()` to create the bot object
   - Schedules `createAIPostSpawn()` for TempSpawn/MarkerSpawn bots (skipped for SpawnPoint bots)
   - Returns AI name on success, -1 on failure

9. **`createAIPostSpawn(%aiName, %armor, %group)`** (`Ai.cs:1568`)
   - Only called for TempSpawn/MarkerSpawn bots (NOT SpawnPoint bots)
   - Validates Player object exists
   - Checks for ghost client IDs
   - Skips if `SpawnBotInfo` is set (enemy bots)

10. **`SpawnAIGetClientId(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout, %spawnPointId, %predictedId)`** (`Ai.cs:4987-7101`, **2,114 lines**)
    - **WARNING**: This function is massive and scheduled for refactoring
    - **Priority 1**: Checks predicted ID from `PlayerManager::getFreeId()` (O(1) lookup)
    - **Priority 2**: Gets client ID via `AI::getId()` (engine lookup)
    - **Priority 3**: Falls back to `NEWgetClientByName()` (display name search)
    - **Priority 4**: Brute-force range loop (2049-2200)
    - Validates player object exists via `Bot_GetValidatedPlayerObject()`
    - Handles zone becoming empty during 3.0s spawn delay
    - Cleans up stale bot data from reused client IDs
    - Detects and handles ghost bots (shell bots)
    - Multiple real player safeguards (save file checks)
    - Sets `BotInfoAiName` immediately
    - Sets team via `DetermineBotTeam()` + `GameBase::setTeam()`
    - Calls `ScheduleTeamEnforcement()` for aggressive team enforcement
    - Schedules `VerifyEnemyBotTeam()` for verification
    - Registers bot via `RegisterBot()`
    - Calls `CommitSpawnSlot()` to commit the reserved slot
    - Sets `HasLoadedAndSpawned` flag
    - Schedules `AI::setWeapons()` after 0.2s delay


### Team Management
11. **`DetermineBotTeam(%botName, %displayName, %commandIssuer, %clientId)`** (`Ai.cs:3141`)
    - Determines the correct team for a bot based on type and spawn source
    - Returns team number

12. **`ScheduleTeamEnforcement(%clientId, %expectedTeam)`** (`Ai.cs:11775`)
    - Schedules aggressive team enforcement with multiple retries
    - Ensures team is set correctly even if `UpdateTeam()` tries to override it

13. **`VerifyEnemyBotTeam(%clientId, %botName, %expectedTeam)`** (`Ai.cs:11792`)
    - Verifies team was set correctly
    - Restores team if it was changed

### Bot Initialization
14. **`AI::setWeapons(%aiName, %loadout)`** (`Ai.cs:1795`)
    - Sets weapons and equipment for the bot
    - Calls `GiveThisStuff()` to give items
    - Calls `HardcodeAIskills()` to initialize skills
    - Calls `RefreshAllEnemyBot()` to refresh stats

15. **`GiveThisStuff(%clientId, %list, %echo, %multiplier)`** (`rpgfunk.cs:5281`)
    - Gives items to the bot
    - Handles belt, inventory, and equipment

16. **`HardcodeAIskills(%aiId)`** (`Ai.cs:6760`)
    - Initializes AI skills and stats
    - Sets team before calling `RefreshAllEnemyBot()`
    - Calls `VerifyEnemyBotTeam()` to verify team after refresh

17. **`RefreshAllEnemyBot(%clientId)`** (`rpgfunk.cs:4836`)
    - Refreshes all stats for enemy bots
    - Updates HP, MANA, regen rates, etc.

### Registry & Tracking
18. **`RegisterBot(%clientId, %spawnPointId, %team, %aiName)`** (`Ai.cs:336`)
    - Registers bot in centralized registry
    - Used for reliable spawn counter management

19. **`IncrementSpawnCounter(%spawnPointId)`** (`Ai.cs:545`)
    - Increments spawn counter for a spawn point
    - Used by registry system

### Client ID Lookup
20. **`AI::getClientIdFromName(%aiName)`** (`Ai.cs:7304`)
    - Gets client ID from bot name
    - Uses `GetEveryoneIdList()` first
    - Falls back to brute-force range loop (2049-2200)

21. **`GetEveryoneIdList()`** (`rpgfunk.cs:4073`)
    - Gets list of all client IDs
    - Uses `Client::getFirst()` and `Client::getNext()` for iteration

### PlayerManager (C++ Plugin Integration)
22. **`PlayerManager::getFreeId()`** (`Plugins/PlayerManager.dll`)
    - Returns next available client ID (O(1) lookup)
    - Called from `SpawnAI()` to predict client ID before `AI::spawn()`
    - Passed to `SpawnAIGetClientId()` as `%predictedId`

23. **`PlayerManager::isIdFree(%id)`** (`Plugins/PlayerManager.dll`)
    - Checks if specific client ID is available
    - Used for validation before spawn

### Helper Functions (Bot Detection & Cleanup)
24. **`HasEnemyBotNamePrefix(%name)`** (`Ai.cs:2053`)
    - Checks if name starts with enemy bot prefix
    - Covers: Alien, Admin, Angel, Demon, God, Minotaur, Ogre, Orc, Pigman, Undead, Zombie, Seal, Enemy, Void
    - Consolidates 14+ inline pattern checks

25. **`IsSafeToModify(%clientId, %context)`** (`Ai.cs:~1200`)
    - Unified safeguard for bot vs real player detection
    - Checks: save file, AI-controlled flag, bot markers
    - Returns true if safe to modify (is a bot)

26. **`PreSpawnCleanup(%clientId)`** (`Ai.cs:1891`)
    - Clears all stale bot data from client ID
    - Called before reusing a client ID for new bot
    - Handles ghost bot cleanup

27. **`Bot_GetValidatedPlayerObject(%clientId)`** (`Ai.cs:~4800`)
    - Validates player object exists and is valid
    - Returns player object ID or empty string


---

## TOWN BOT SPAWN FLOW

### Initialization & Entry Points
1. **`InitTownBots()`** (`Ai.cs:8037`)
   - Initializes all town bots on server startup
   - Called from `Server::onServerCreated()`
   - Calls `SpawnZoneBots()` for each zone

2. **`SpawnZoneBots(%zoneIndex)`** (`Ai.cs:9245`)
   - Spawns all town bots for a specific zone
   - Iterates through `$TownBotRegistry`
   - Calls `SpawnSingleZoneBot()` for each bot in the zone

3. **`SpawnSingleZoneBot(%botName, %zoneIndex)`** (`Ai.cs:8985`)
   - Spawns a single town bot
   - Constructs AI name with "TownBot_" prefix
   - Gets race and armor from `$BotInfo`
   - Calls `AI::spawn()` directly (NOT through `createAI()`)
   - Calls `SpawnZoneBotPostSpawn()` immediately

4. **`SpawnZoneBotPostSpawn(%aiName, %botName, %displayName, %zoneIndex)`** (`Ai.cs:8405`)
   - Gets client ID via `NEWgetClientByName()` immediately
   - If fails, schedules `RetryGetAIId()` after 0.1s
   - Sets `BotInfoAiName` with "TownBot_" prefix
   - Clears `SpawnBotInfo` (town bots should NOT have it)
   - Sets team to 0 (Citizen) immediately
   - Sets `HasLoadedAndSpawned` flag
   - Schedules `VerifyTownBotTeam()` after 0.2s
   - Schedules `InitTownBotPostSpawn()` after 0.1s
   - Schedules `InitTownBotItemsForBot()` after 1.5s

5. **`RetryGetAIId(%aiName, %botName, %displayName, %zoneIndex)`** (`Ai.cs:8631`)
   - Retry function for getting client ID when timing issues occur
   - Handles cases where `AI::spawn()` succeeds but client ID can't be found immediately
   - Same flow as `SpawnZoneBotPostSpawn()` but with retry logic

### Post-Spawn Initialization
6. **`InitTownBotPostSpawn(%aiName, %name)`** (`Ai.cs:10395`)
   - Gets client ID from `$TownBotSpawned[%name]`
   - Falls back to `NEWgetClientByName()` if needed
   - Validates and sets race/armor
   - Sets `BotInfoAiName` with "TownBot_" prefix
   - Clears `SpawnBotInfo` (town bots should NOT have it)
   - Sets team to 0 (Citizen)
   - Schedules `VerifyTownBotTeam()` after 0.2s
   - Sets AI variables to disable AI behavior
   - Schedules `addToSetMissionCleanup()` after 0.1s

7. **`addToSetMissionCleanup(%clientId)`** (`Ai.cs:10561`)
   - Adds town bot to `MissionCleanup` SimSet
   - Ensures proper cleanup on server shutdown

8. **`InitTownBotItemsForBot(%clientId, %botName)`** (`Ai.cs:10600`)
   - Mounts items on town bot
   - Gets items from `$BotInfo[%botName, ITEMS]`
   - Mounts items via `Player::mountItem()`

### Team Management
9. **`VerifyTownBotTeam(%clientId, %botName, %expectedTeam)`** (`Ai.cs:11681`)
   - Verifies team was set correctly for town bots
   - Restores team if it was changed
   - Town bots should always be team 0 (Citizen)

### Client ID Lookup
10. **`NEWgetClientByName(%name)`** (`rpgfunk.cs:3323`)
    - Gets client ID from display name
    - Uses `Client::getFirst()` and `Client::getNext()` for iteration

---

## ENEMY BOT DESPAWN FLOW

### Zone-Based Despawn
1. **`ScheduleZoneBotDespawn(%zoneIndex)`** (`Ai.cs:10191`)
   - Schedules bot despawn for a zone (30 seconds after all players leave)
   - Calls `DespawnZoneBots()` after delay

2. **`CheckAndDespawnZoneBots(%zoneIndex)`** (`Ai.cs:10201`)
   - Checks if despawn is still needed before executing
   - Prevents duplicate despawns

3. **`DespawnZoneBots(%zoneIndex)`** (`Ai.cs:9675`)
   - Despawns all bots in a zone when it becomes empty
   - First despawns town bots (calls `AI::delete()`)
   - Then despawns enemy bots (calls `Player::Kill()`)
   - For enemy bots:
     - Sets `noDropLootbagFlag` and `noExperienceFlag`
     - Calls `Player::Kill()` which triggers `Player::onKilled()`

### Death-Based Despawn (Natural Death)
4. **`Player::onKilled(%this)`** (`playerdamage.cs:493`)
   - Called when a bot dies (killed by player or despawn)
   - For enemy bots:
     - Gets `BotInfoAiName` BEFORE clearing it
     - Calls `DecrementSpawnCounter()` to decrement spawn counter
     - Decrements `$ActiveEnemyBots`, `$TotalActiveBots`, and `$numAI`
     - Frees AI number from `$aiNumTable`
     - Marks client ID as recently freed
     - Sets spawn point cooldown (5 seconds)
     - Clears `$SpawnAIScheduled` flag
     - Schedules `AI::delete()` after 1.0s delay
     - Clears various data fields (but keeps `BotInfoAiName` and `SpawnBotInfo` for `AI::onDroneKilled()`)

5. **`AI::onDroneKilled(%aiName)`** (`Ai.cs:5772`)
   - Called after `Player::onKilled()` completes
   - Gets `BotInfoAiName` and `SpawnBotInfo` from `fetchData()`
   - Calls `CleanupBot()` to clean up bot data
   - Calls `AddToGraveyard()` to mark bot as dead

6. **`CleanupBot(%clientId, %aiName)`** (`Ai.cs:1074`)
   - Comprehensive bot cleanup function
   - Calls `DecrementSpawnCounter()` (redundant but safe)
   - Decrements tracking counters
   - Frees AI number
   - Calls `PreSpawnCleanup()` to clear all data storage
   - Marks client ID as recently freed with validation token

7. **`PreSpawnCleanup(%clientId)`** (`Ai.cs:774`)
   - Clears all bot data storage
   - Clears `$EnemyBotData`, `$ClientData`, `$BotRegistry`, etc.
   - Clears `storeData()` fields
   - Clears directive tables
   - Clears belt cached lists

8. **`DecrementSpawnCounter(%clientId)`** (`Ai.cs:438`)
   - Decrements spawn counter for a spawn point
   - Uses bot registry to find spawn point
   - Falls back to multiple methods if registry lookup fails

9. **`AddToGraveyard(%clientId, %aiName)`** (`Ai.cs:182`)
   - Marks bot as dead in graveyard
   - Used to prevent client ID reuse for a short period

### Admin Commands (Manual Despawn)
10. **`#despawnbots`** (in `comchat.cs:3491`)
    - Admin command to despawn all bots
    - For enemy bots: calls `Player::Kill()` which triggers `Player::onKilled()`

11. **`#resetspawns`** (in `comchat.cs:7244`)
    - Admin command to reset spawns
    - For enemy bots: calls `Player::Kill()` which triggers `Player::onKilled()`

---

## TOWN BOT DESPAWN FLOW

### Zone-Based Despawn
1. **`ScheduleZoneBotDespawn(%zoneIndex)`** (`Ai.cs:10191`)
   - Schedules bot despawn for a zone (30 seconds after all players leave)
   - Calls `DespawnZoneBots()` after delay

2. **`CheckAndDespawnZoneBots(%zoneIndex)`** (`Ai.cs:10201`)
   - Checks if despawn is still needed before executing
   - Prevents duplicate despawns

3. **`DespawnZoneBots(%zoneIndex)`** (`Ai.cs:9675`)
   - Despawns all bots in a zone when it becomes empty
   - First despawns town bots:
     - Extracts bot name from `BotInfoAiName` (removes "TownBot_" prefix)
     - Calls `AI::delete()` to delete the bot
     - Clears `$TownBotSpawned[%botName]`
     - Decrements `$ActiveTownBots` and `$TotalActiveBots`
     - Removes from `$TownBotList`

### Death-Based Despawn (Natural Death)
4. **`Player::onKilled(%this)`** (`playerdamage.cs:493`)
   - Called when a town bot dies (should be rare)
   - For town bots:
     - Gets `BotInfoAiName` BEFORE clearing it
     - Frees AI number from `$aiNumTable`
     - Decrements `$ActiveTownBots` and `$TotalActiveBots`
     - Schedules `AI::delete()` after 1.0s delay

5. **`AI::onDroneKilled(%aiName)`** (`Ai.cs:5772`)
   - Called after `Player::onKilled()` completes
   - For town bots: calls `CleanupBot()` to clean up bot data

6. **`CleanupBot(%clientId, %aiName)`** (`Ai.cs:1074`)
   - Comprehensive bot cleanup function
   - For town bots:
     - Decrements `$ActiveTownBots` and `$TotalActiveBots`
     - Frees AI number
     - Calls `PreSpawnCleanup()` to clear all data storage
     - Removes from `$TownBotList`

7. **`PreSpawnCleanup(%clientId)`** (`Ai.cs:774`)
   - Clears all bot data storage
   - Clears `$TownBotData`, `$ClientData`, etc.
   - Clears `storeData()` fields

### Admin Commands (Manual Despawn)
8. **`#despawnbots`** (in `comchat.cs:3491`)
   - Admin command to despawn all bots
   - For town bots: calls `AI::delete()` directly

9. **`#resetspawns`** (in `comchat.cs:7244`)
   - Admin command to reset spawns
   - For town bots: calls `Player::Kill()` which triggers `Player::onKilled()`

---

## SHARED/UTILITY FUNCTIONS

### Client ID Lookup
- **`GetClientIdFromPlayerObject(%playerObj)`** (`playerdamage.cs:5`)
  - Gets client ID from player object
  - Uses reverse lookup via `Client::getFirst()`/`getNext()` (primary)
  - Falls back to range loop (2049-2200) if primary fails

### Data Management
- **`storeData(%clientId, %key, %value)`** (various files)
  - Stores data for a client ID
  - Used throughout spawn/despawn flows

- **`fetchData(%clientId, %key)`** (various files)
  - Retrieves data for a client ID
  - Used throughout spawn/despawn flows

- **`ClearVariables(%clientId)`** (`rpgfunk.cs:3857`)
  - Clears all variables for a client ID
  - Used in cleanup flows

### Registry & Tracking
- **`GetBotSpawnPointFromRegistry(%clientId)`** (`Ai.cs:401`)
  - Gets spawn point from bot registry
  - Used for spawn counter management

- **`GetRegisteredBotCount(%spawnPointId)`** (`Ai.cs:419`)
  - Gets count of registered bots for a spawn point
  - Used for spawn counter validation

- **`ReconcileSpawnCounters()`** (`Ai.cs:563`)
  - Reconciles spawn counters with actual bot counts
  - Called periodically to fix counter drift

- **`StartSpawnCounterReconciliation()`** (`Ai.cs:763`)
  - Starts periodic spawn counter reconciliation
  - Called on server startup

### Graveyard System
- **`AddToGraveyard(%clientId, %aiName)`** (`Ai.cs:182`)
  - Marks bot as dead in graveyard
  - Prevents client ID reuse for a short period

- **`RemoveFromGraveyard(%clientId, %aiName)`** (`Ai.cs:190`)
  - Removes bot from graveyard
  - Called when client ID is reused

- **`IsInGraveyard(%clientId, %aiName)`** (`Ai.cs:198`)
  - Checks if bot is in graveyard
  - Used to prevent client ID reuse

- **`CleanupOldGraveyardEntries()`** (`Ai.cs:182`)
  - Removes old graveyard entries (older than 10 seconds)
  - Called periodically to prevent accumulation

---

## NOTES

### Key Differences: Enemy vs Town Bots

**Enemy Bots:**
- Spawned via `SpawnLoop()` → `AI::helper()` → `SpawnAI()` → `createAI()`
- Use `SpawnBotInfo` to identify as enemy bots
- Use transaction system (`ReserveSpawnSlot`, `CommitSpawnSlot`, `RollbackSpawnSlot`)
- Despawned via `Player::Kill()` which triggers `Player::onKilled()`
- Have spawn counters and cooldowns

**Town Bots:**
- Spawned via `SpawnZoneBots()` → `SpawnSingleZoneBot()` → `AI::spawn()` directly
- Do NOT use `SpawnBotInfo` (cleared to prevent confusion)
- Use "TownBot_" prefix in `BotInfoAiName`
- Always team 0 (Citizen)
- Despawned via `AI::delete()` directly (zone-based) or `Player::Kill()` (death-based)
- No spawn counters or cooldowns

### Timing Considerations

- Enemy bots: `SpawnAIGetClientId()` scheduled 3.0s after `createAI()` to allow Player object to register
- Town bots: `SpawnZoneBotPostSpawn()` called immediately, with `RetryGetAIId()` scheduled 0.1s if lookup fails
- Team verification: Scheduled 0.2s after spawn to catch `UpdateTeam()` overrides
- AI number freeing: Scheduled 1.0s after death to allow engine to finish death callback chain


