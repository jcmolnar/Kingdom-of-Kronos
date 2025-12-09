# Enemy Bot Spawn Flow - Complete Analysis

## Problem Statement
Enemy bots are being set to team -1 or having their teams reset incorrectly during spawn. This document traces the complete spawning flow to identify where teams are being set/reset.

## Complete Spawn Flow Timeline

### T+0.0s: SpawnAI() is called
**Location:** `Ai.cs` line 1336
**Function:** `SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout)`
**What it does:**
- Checks if bot already exists
- Determines if this is first call (schedules with 2s delay) or internal call (proceeds immediately)
- Calls `createAI()` to spawn the bot

### T+0.0s: createAI() is called
**Location:** `Ai.cs` line 49
**Function:** `createAI(%aiName, %markerGroup, %name)`
**What it does:**
- Determines armor type from `$BotInfo` or `$NameForRace`
- Calls `AI::spawn()` engine function
- **CRITICAL:** `AI::spawn()` creates a Player object and the engine may trigger callbacks

### T+0.0s: AI::spawn() engine call
**Location:** Engine (C++)
**What it does:**
- Creates a Player object
- Registers it in the client list
- **CRITICAL:** The engine may call `Game::playerSpawned()` callback automatically
- Returns immediately (synchronous call)

### T+0.0s: Engine callback - Game::playerSpawned() (IF TRIGGERED)
**Location:** `playerspawn.cs` line 239
**Function:** `Game::playerSpawned(%pl, %clientId, %armor)`
**What it does:**
- Sets `HasLoadedAndSpawned` flag
- Calls `GiveThisStuff()` which may call `RefreshAll()`
- **CRITICAL:** Has bot check at line 297-300 to skip `RefreshAll()` for bots
- **BUT:** This check relies on `BotInfoAiName` or `SpawnBotInfo` being set, which might not be set yet!

### T+0.0s: SpawnAI() tries immediate team set (IF client ID available)
**Location:** `Ai.cs` line 1410-1504
**What it does:**
- Tries to get client ID immediately using `NEWgetClientByName(%displayName)`
- **PROBLEM:** This often fails because the Player object hasn't registered in the client list yet
- If successful, sets `SpawnBotInfo` and `botTeam` and calls `GameBase::setTeam()`
- **BUT:** This is a race condition - `Game::playerSpawned()` may have already run!

### T+0.0s: Game::playerSpawn() (IF TRIGGERED BY ENGINE)
**Location:** `playerspawn.cs` line 75
**Function:** `Game::playerSpawn(%clientId, %respawn)`
**What it does:**
- **CRITICAL:** Calls `UpdateTeam(%clientId)` at line 168
- **CRITICAL:** `UpdateTeam()` checks if it's a bot, but `SpawnBotInfo` might not be set yet!
- If `SpawnBotInfo` is not set, `UpdateTeam()` treats it as a player and sets team based on race
- If race is not set, defaults to team 1

### T+0.0s: UpdateTeam() is called
**Location:** `rpgfunk.cs` line 2852
**Function:** `UpdateTeam(%clientId)`
**What it does:**
- Checks if it's a bot using `isRPGAI()` and `SpawnBotInfo`/`BotInfoAiName`
- **PROBLEM:** If `SpawnBotInfo` is not set yet, it treats it as a player
- Sets team based on `$TeamForRace[%race]`
- If race is not set or invalid, defaults to team 1

### T+0.5s: SpawnAIGetClientId() is scheduled
**Location:** `Ai.cs` line 1514
**Function:** `SpawnAIGetClientId(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout)`
**What it does:**
- Brute-force searches for client ID by display name
- Sets `SpawnBotInfo` and `BotInfoAiName`
- Determines `botTeam` from `$BotInfo`, race, or display name pattern
- Stores `botTeam` in `storeData(%aiId, "botTeam", %botTeam)`
- Calls `GameBase::setTeam()` multiple times
- **PROBLEM:** This happens AFTER `UpdateTeam()` may have already run!

### T+0.7s: AI::setWeapons() is scheduled
**Location:** `Ai.cs` line 2682
**Function:** `AI::setWeapons(%aiName, %loadout)`
**What it does:**
- Gets client ID from AI name
- **CRITICAL:** Sets team again before `GiveThisStuff()` (line 492)
- Calls `GiveThisStuff()` which calls `RefreshAll()`

### T+0.7s: GiveThisStuff() is called
**Location:** `rpgfunk.cs`
**Function:** `GiveThisStuff(%clientId, %stuff, %isBelt)`
**What it does:**
- Gives items to the bot
- Calls `RefreshAll()` at the end

### T+0.7s: RefreshAll() is called
**Location:** `rpgfunk.cs` line 3900
**Function:** `RefreshAll(%clientId)`
**What it does:**
- **CRITICAL:** For enemy bots, checks if team matches `botTeam` (line 3939-3977)
- If team doesn't match, restores it from `botTeam`
- **BUT:** If `botTeam` is not set or is -1, it can't restore it!

### T+1.0s: VerifyEnemyBotTeam() is scheduled
**Location:** `Ai.cs` line 2648
**Function:** `VerifyEnemyBotTeam(%clientId, %botName, %expectedTeam)`
**What it does:**
- Verifies team matches expected team
- If not, tries to restore it
- **BUT:** This is too late - team may have already been set incorrectly

## Root Cause Analysis

### Primary Issue: Race Condition with UpdateTeam()

**The Problem:**
1. `AI::spawn()` creates a Player object
2. Engine may call `Game::playerSpawn()` or `Game::playerSpawned()` immediately
3. `Game::playerSpawn()` calls `UpdateTeam()` at line 168
4. `UpdateTeam()` checks if it's a bot, but `SpawnBotInfo` is not set yet
5. `UpdateTeam()` treats it as a player and sets team based on race (or defaults to team 1)
6. Our code tries to set team later, but `UpdateTeam()` has already run

**Why SpawnBotInfo isn't set:**
- `SpawnBotInfo` is set in `SpawnAIGetClientId()` which runs at T+0.5s
- But `UpdateTeam()` may run at T+0.0s (synchronously when `AI::spawn()` is called)
- The immediate team set in `SpawnAI()` (line 1410-1504) often fails because client ID isn't available yet

### Secondary Issue: GameBase::setTeam() is Not Synchronous

**The Problem:**
- `GameBase::setTeam()` is not synchronous - it may not take effect immediately
- Even if we set team before `RefreshAll()`, `RefreshAll()` might still see team -1
- Multiple calls to `GameBase::setTeam()` don't guarantee it's processed before the next function

### Tertiary Issue: botTeam Not Always Stored

**The Problem:**
- `botTeam` is stored in `SpawnAIGetClientId()` at T+0.5s
- But `UpdateTeam()` may run at T+0.0s before `botTeam` is stored
- `RefreshAll()` can't restore the team if `botTeam` isn't stored yet

## Solution Strategy

### Option 1: Prevent UpdateTeam() from running for bots
- Modify `Game::playerSpawn()` to check if it's a bot BEFORE calling `UpdateTeam()`
- Use display name pattern matching or check if `BotInfoAiName` is set
- **PROBLEM:** `BotInfoAiName` might not be set yet either!

### Option 2: Set SpawnBotInfo BEFORE AI::spawn()
- Store bot information in a global array before spawning
- `UpdateTeam()` can check this array to identify bots
- **PROBLEM:** We don't have the client ID yet, so we can't use it as a key

### Option 3: Set team in UpdateTeam() for bots
- Modify `UpdateTeam()` to check display name patterns for bots
- If it matches a bot pattern, set team based on pattern instead of race
- **PROBLEM:** This is a workaround, not a fix

### Option 4: Use a pre-spawn flag
- Set a flag in a global array keyed by display name before spawning
- `UpdateTeam()` checks this flag to identify bots
- After spawn, move the flag to client ID-based storage
- **BEST SOLUTION:** This prevents `UpdateTeam()` from running for bots

## Recommended Fix

Implement Option 4: Use a pre-spawn flag system.

1. Before calling `AI::spawn()`, store bot information in a global array keyed by display name:
   ```tribescript
   $PreSpawnBotInfo[%displayName] = %spawnBotInfo;
   $PreSpawnBotTeam[%displayName] = %botTeam;
   ```

2. Modify `UpdateTeam()` to check this array:
   ```tribescript
   %preSpawnBotInfo = $PreSpawnBotInfo[Client::getName(%clientId)];
   if(%preSpawnBotInfo != "")
   {
       // This is a bot being spawned - use pre-spawn team
       %botTeam = $PreSpawnBotTeam[Client::getName(%clientId)];
       GameBase::setTeam(%clientId, %botTeam);
       return; // Skip player team logic
   }
   ```

3. After `SpawnAIGetClientId()` sets `SpawnBotInfo`, clear the pre-spawn flag:
   ```tribescript
   $PreSpawnBotInfo[%displayName] = "";
   $PreSpawnBotTeam[%displayName] = "";
   ```

This ensures `UpdateTeam()` knows it's a bot even before `SpawnBotInfo` is set.

