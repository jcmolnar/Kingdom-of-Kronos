# Real Player Safeguards - Complete List

This document lists all safeguards implemented to prevent bot spawning and cleanup from affecting real players.

> [!WARNING]
> **Line numbers are approximate** and may have shifted due to ongoing development. Use function names for searching. Last verified: December 2025.

---

## 1. Character Save File Checks

**Purpose:** The most reliable indicator of a real player is the presence of a character save file (`temp\\PlayerName.cs`).

### Locations:
- **`PreSpawnCleanup()` (Ai.cs:794-805)**: Hard guard - never touch real players with save files
- **`isTownBot()` (Ai.cs:975-981)**: Returns false if player has save file
- **`isEnemyBot()` (Ai.cs:1026-1032)**: Returns false if player has save file
- **`ReconcileSpawnCounters()` (Ai.cs:692-701)**: Checks save file before deletion
- **`createAI()` (Ai.cs:1339-1351, 1442-1454)**: Verifies bot is not a real player with save file
- **`SpawnAIGetClientId()` (Ai.cs:4294-4304, 4317-4327, 4337-4344, 3810-3829)**: Multiple save file checks before deletion
- **`SpawnAIGetClientId()` Ghost Bot Cleanup (Ai.cs:4698-4707)**: Checks save file as first check
- **`AI::onDroneKilled()` (Ai.cs:6000-6017)**: Checks save file before bot cleanup
- **`DespawnZoneBots()` (Ai.cs:9711-9714, 9971-9974)**: Verifies bot is not a player with save file
- **`GetClientIdFromPlayerObject()` (playerdamage.cs:221-231)**: Checks save file for player detection

---

## 2. AI-Controlled Detection

**Purpose:** Real players are NOT AI-controlled. Bots are AI-controlled.

### Locations:
- **`PreSpawnCleanup()` (Ai.cs:797-805)**: Checks `!Player::isAiControlled()` before cleanup
- **`PreSpawnCleanup()` (Ai.cs:830-846)**: Verifies connected client is NOT AI-controlled
- **`createAI()` (Ai.cs:1386-1401, 1489-1504)**: Checks if client is NOT AI-controlled before deletion
- **`SpawnAIGetClientId()` (Ai.cs:4372-4386)**: Verifies connected client is NOT AI-controlled
- **`SpawnAIGetClientId()` Old Player Object Deletion (Ai.cs:3815-3824)**: Verifies old player object is AI-controlled
- **`AI::RemoveBotFromBotGroup()` (Ai.cs:7406-7410)**: Prevents removing non-AI players
- **`AI::AddBotToBotGroup()` (Ai.cs:7428-7432)**: Prevents adding non-AI players
- **`GetClientIdFromPlayerObject()` (playerdamage.cs:215-231)**: Uses `Player::isAiControlled()` for bot detection

---

## 3. HasLoadedAndSpawned Flag Check

**Purpose:** Real players have `HasLoadedAndSpawned` flag set. Bots should not have this flag (or it's cleared during bot cleanup).

### Locations:
- **`SpawnAIGetClientId()` Ghost Bot Cleanup (Ai.cs:4710-4718)**: Checks `HasLoadedAndSpawned` flag
- **`SpawnAIGetClientId()` Ghost Bot Player Object Deletion (Ai.cs:4808)**: Final check before deletion
- **`SpawnAI()` (Ai.cs:5208-5218, 5269-5279)**: Safety check before deletion
- **`AI::RemoveBotFromBotGroup()` (Ai.cs:7413-7417)**: Only removes bots that have loaded and spawned
- **`AI::AddBotToBotGroup()` (Ai.cs:7434-7438)**: Only adds bots that have loaded and spawned
- **`DespawnZoneBots()` (Ai.cs:9738-9742)**: Only despawns bots that have loaded and spawned

---

## 4. Bot Data Indicators

**Purpose:** Bots have specific data flags (`BotInfoAiName`, `SpawnBotInfo`) that real players should never have.

### Locations:
- **`SpawnAIGetClientId()` Ghost Bot Cleanup (Ai.cs:4721-4730)**: Checks for `SpawnBotInfo` or `BotInfoAiName`
- **`SpawnAIGetClientId()` Old Player Object Deletion (Ai.cs:3806-3809)**: Verifies old bot data exists
- **`SpawnAIGetClientId()` Real Player Detection (Ai.cs:4959-4967)**: Checks `SpawnBotInfo` as priority 1
- **`SpawnAIGetClientId()` Real Player Detection (Ai.cs:4973-4978)**: Checks `BotInfoAiName` match as priority 2
- **`GetClientIdFromPlayerObject()` (playerdamage.cs:211-215)**: Checks bot data indicators first
- **`Zone::getNumPlayers()` (zone.cs:1547-1557)**: Detects shell bots (has bot data but no player object)
- **`FindPlayerInBotGroup()` (Ai.cs:3793-3833)**: Early shell bot detection

---

## 5. Client ID Range Checks

**Purpose:** Real players typically use client IDs 2048 and below. Bots use 2049-2200. However, this is NOT a reliable safeguard alone - it's used in combination with other checks.

### Locations:
- **`SpawnAIGetClientId()` Old Player Object Deletion (Ai.cs:3797-3801)**: Only proceeds if client ID is in bot range (2049+)
- **`GetClientIdFromPlayerObject()` (playerdamage.cs:71-79)**: Range loop fallback (2049-2200)

---

## 6. Recently Freed Client ID Protection

**Purpose:** When a bot dies, its client ID is marked as "recently freed" to prevent immediate reuse. This prevents collisions when a real player connects and gets a recently freed bot ID.

### Locations:
- **`Server::onClientConnect()` (connectivity.cs:260-280)**: Checks if client ID was recently freed, delays player spawn if needed
- **`GetClientIdFromPlayerObject()` (playerdamage.cs:19-28, 275-296)**: Checks recently freed flag, allows if reverse verification passes
- **`SpawnAIGetClientId()` (Ai.cs:3740-3783, 4103-4117, 4442-4449)**: Checks recently freed flag before reusing client ID
- **`SpawnAIGetClientId()` (Ai.cs:4074-4077, 4430-4433)**: Clears recently freed flag when bot is found
- **`AI::onDroneKilled()` (Ai.cs:1171-1178)**: Marks client ID as recently freed with validation token
- **`Player::onKilled()` (playerdamage.cs:10156-10157)**: Marks bot ID as recently freed

---

## 7. Validation Tokens

**Purpose:** Prevents scheduled cleanup operations from affecting real players who get the same client ID after a bot dies. Each scheduled operation stores a unique validation token that must match when the operation executes.

### Locations:
- **`AI::onDroneKilled()` (Ai.cs:1172-1178)**: Validation token for `$ClientIdRecentlyFreed` scheduled clear
- **`SpawnAIGetClientId()` Ghost Bot Cleanup (Ai.cs:4815-4819)**: Validation token for ghost bot cleanup
- **`SpawnAI()` (Ai.cs:5208-5212, 5269-5273, 5361-5365, 5383-5387)**: Validation tokens for scheduled deletions
- **`AI::onDroneKilled()` Seal Battle Cleanup (Ai.cs:6233-6236)**: Validation token for seal battle cleanup
- **`DespawnZoneBots()` (Ai.cs:9864-9871)**: Validation token for scheduled player object deletion

---

## 8. Reverse Verification

**Purpose:** Verifies that `Client::getOwnedObject(%clientId)` returns the same player object that `Player::getClient(%playerObj)` returned. This prevents client ID collisions.

### Locations:
- **`GetClientIdFromPlayerObject()` (playerdamage.cs:151-208, 273-362)**: Two-tier reverse verification (Client::getFirst()/getNext() primary, range loop fallback)
- **`GetClientIdFromPlayerObject()` (playerdamage.cs:49-148)**: Fallback reverse verification when primary fails

---

## 9. Final Verification Before Deletion

**Purpose:** Re-checks all safeguards immediately before deleting a player object to prevent race conditions where a real player connects between initial check and deletion.

### Locations:
- **`PreSpawnCleanup()` (Ai.cs:817-827)**: Final save file check immediately before deletion
- **`createAI()` (Ai.cs:1381-1401, 1484-1504)**: Final verification before deleting stale player objects
- **`SpawnAIGetClientId()` (Ai.cs:4337-4365)**: Final verification before deleting old player object
- **`SpawnAIGetClientId()` Ghost Bot Cleanup (Ai.cs:4791-4808)**: Final safety check before deleting ghost player object

---

## 10. Name Pattern Matching

**Purpose:** Real players don't have bot name patterns (e.g., "Alien", "Admin", "Demon", etc.). This is used as a secondary check.

### Locations:
- **`SpawnAIGetClientId()` Real Player Detection (Ai.cs:4981-5083)**: Checks internal name patterns and display name patterns
- **`SpawnAIGetClientId()` (Ai.cs:4360-4365)**: Checks if name doesn't match bot patterns before deletion
- **`SpawnAIGetClientId()` (Ai.cs:5086-5090)**: Error if real player name detected

---

## 11. Shell Bot Detection

**Purpose:** Shell bots are bots with bot data but no valid player object. They should be skipped to prevent false positives.

### Locations:
- **`Zone::getNumPlayers()` (zone.cs:1544-1557)**: Skips shell bots from zone player counts
- **`FindPlayerInBotGroup()` (Ai.cs:3793-3833)**: Early shell bot detection, returns -1 immediately

---

## 12. Connection Status Checks

**Purpose:** Verifies if a client is currently connected before performing cleanup operations.

### Locations:
- **`PreSpawnCleanup()` (Ai.cs:830-846)**: Checks if client is connected and NOT AI-controlled
- **`createAI()` (Ai.cs:1397-1401, 1500-1504)**: Checks if connected client is NOT AI-controlled
- **`SpawnAIGetClientId()` (Ai.cs:4372-4386)**: Verifies connected client is NOT AI-controlled

---

## 13. Bot Group Protection

**Purpose:** Prevents real players from being added to or removed from bot groups.

### Locations:
- **`AI::RemoveBotFromBotGroup()` (Ai.cs:7405-7410)**: Prevents removing players from bot groups
- **`AI::AddBotToBotGroup()` (Ai.cs:7427-7432)**: Prevents adding players to bot groups

---

## 14. Entity Type Validation

**Purpose:** Verifies that the client ID matches the expected entity type (bot vs player) before processing.

### Locations:
- **`GetClientIdFromPlayerObject()` (playerdamage.cs:209-231, 299-321)**: Entity type validation after reverse verification
- **`GetClientIdFromPlayerObject()` (playerdamage.cs:562-580)**: Bot detection using multiple indicators

---

## 15. Stale Data Clearing

**Purpose:** When a real player is detected with stale bot data, the bot data is cleared to prevent false positives.

### Locations:
- **`AI::onDroneKilled()` (Ai.cs:6008-6014)**: Clears stale bot data when real player detected
- **`SpawnAIGetClientId()` (Ai.cs:4069-4085)**: Clears old bot data when reusing recently freed client ID

---

## 16. Hard Rules

**Purpose:** Absolute rules that are never violated, regardless of other conditions.

### Locations:
- **`PreSpawnCleanup()` (Ai.cs:794)**: "Hard guard: never touch real players (non-AI or players with a save file)"
- **`SpawnAIGetClientId()` (Ai.cs:3927)**: "Hard Rule: Never touch a ClientID unless Player::isAiControlled(%id) returns true"
- **`ReconcileSpawnCounters()` (Ai.cs:794)**: "Hard guard: never clean up real players"

---

## 17. Scheduled Operation Safety

**Purpose:** All scheduled cleanup operations verify conditions before executing to prevent affecting real players.

### Locations:
- **All `$ClientIdRecentlyFreed` scheduled clears**: Check validation token before clearing
- **All scheduled player object deletions**: Check validation token and save file before deletion
- **`DespawnZoneBots()` scheduled deletion (Ai.cs:9871)**: Checks validation token AND save file before deletion

---

## 18. Lootbag Cleanup Protection

**Purpose:** Prevents player-owned lootbags from being aggregated, merged, or deleted during bot cleanup operations.

### Locations:
- **`IsLootOwnerBot()` (rpgfunk.cs:2772-2806)**: 
  - Checks if lootbag owner is a bot by verifying character save file exists
  - If save file exists (`temp\\OwnerName.cs`), treats owner as real player (returns false)
  - Checks town bot registry and enemy bot registry to identify bot owners
  - **Primary safeguard**: Save file check prevents player-owned lootbags from being treated as bot lootbags

- **`AggregateLootbags()` (rpgfunk.cs:2848-2868, 2929-2948)**: 
  - Uses `IsLootOwnerBot()` to filter out player-owned lootbags
  - Only aggregates bot-owned or neutral lootbags (owner="*" or bot name)
  - Skips lootbags where owner name doesn't match bot patterns and has no save file
  - **Protection**: Player-owned lootbags are explicitly skipped during aggregation

- **`SafeDeleteLootbag()` (rpgfunk.cs:7950-7975)**: 
  - **CRITICAL SAFEGUARD**: Checks if object type is "Player" and aborts if so
  - Validates object is an Item type before deletion
  - Validates mapName is "Backpack" or "Lootbag" before deletion
  - **Protection**: Prevents accidental deletion of Player objects when deleting lootbags

- **`SaveWorldDeployables()` (rpgfunk.cs:2492-2521)**: 
  - Skips lootbags owned by bots when saving world state
  - Checks bot name patterns (Alien, Admin, Angel, Demon, etc.)
  - Verifies owner is currently a bot using `isRPGAI()`
  - **Protection**: Only saves player-owned lootbags to world save (bot lootbags are temporary)

---

## 19. Object Type Validation

**Purpose:** Ensures cleanup operations only affect the correct object types, preventing accidental deletion of player objects or other critical objects.

### Locations:
- **`SafeDeleteLootbag()` (rpgfunk.cs:7955-7972)**: 
  - Validates object type is "Item" (not "Player")
  - Validates mapName is "Backpack" or "Lootbag"
  - Aborts if object type is "Player" (critical safeguard)
  - Aborts if object type is not "Item" and mapName doesn't match lootbag patterns

- **`AggregateLootbags()` (rpgfunk.cs:2906-2913)**: 
  - Validates object type is "Item" before processing
  - Validates mapName is "Backpack" or loot data exists
  - Skips non-lootbag objects during MissionCleanup scan

---

## 20. Owner Name Validation

**Purpose:** Validates lootbag ownership to distinguish between player-owned and bot-owned lootbags.

### Locations:
- **`AggregateLootbags()` (rpgfunk.cs:2848-2862, 2929-2943)**: 
  - Parses owner name from loot string (first word)
  - Checks if owner is a bot using `IsLootOwnerBot()`
  - Determines if lootbag is player-owned based on owner name and namelist
  - Skips player-owned lootbags (owner != "*" and owner != bot name)

- **`SaveWorldDeployables()` (rpgfunk.cs:2494-2521)**: 
  - Extracts owner name from loot string
  - Checks bot name patterns to identify bot-owned lootbags
  - Verifies owner is currently a bot using `isRPGAI()`
  - Skips bot-owned lootbags from world save

---

## Summary

The system uses **multiple layers of protection** to prevent bot operations from affecting real players:

1. **Primary Detection**: Character save file checks (most reliable)
2. **Secondary Detection**: AI-controlled status, bot data indicators, name patterns
3. **Collision Prevention**: Recently freed flags, validation tokens, reverse verification
4. **Race Condition Protection**: Final verification before deletion, connection status checks
5. **Hard Rules**: Absolute rules that are never violated
6. **Object Cleanup Protection**: Lootbag ownership validation, object type checks, owner name verification

**Total Safeguard Count**: 20 major categories with 60+ individual check locations across the codebase.

---

## Key Files

- **`rpg/scripts/Ai.cs`**: Primary bot spawning/cleanup logic with most safeguards
- **`rpg/scripts/playerdamage.cs`**: Player object to client ID conversion with reverse verification
- **`rpg/scripts/connectivity.cs`**: Player connection handling with collision prevention
- **`rpg/scripts/zone.cs`**: Zone player counting with shell bot detection
- **`rpg/scripts/rpgfunk.cs`**: Lootbag aggregation and cleanup with ownership validation

---

*Last Updated: 2025-01-XX*
*Total Safeguard Locations: 60+*

