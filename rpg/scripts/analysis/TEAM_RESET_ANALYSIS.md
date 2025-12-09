# Team Reset Analysis - Enemy Bot Teams Being Set to -1

## Critical Issues Found

### 1. HardcodeAIskills() - HARDCODED TEAM 11 (Lines 3949-3958)
**Location:** `Ai.cs` line 3949-3958
**Problem:** Hardcodes team 11 for ALL SpawnPoint bots if botTeam is not stored
```torquescript
// For enemy bots from spawn points, ensure botTeam is 11 if not stored
%spawnBotInfo = fetchData(%aiId, "SpawnBotInfo");
if(%spawnBotInfo != "" && GetWord(%spawnBotInfo, 0) == "SpawnPoint")
{
    if(%storedBotTeam == "" || %storedBotTeam == -1 || %storedBotTeam == "0" || %storedBotTeam == 0)
    {
        %storedBotTeam = 11; // WRONG! Different bot types have different teams!
        storeData(%aiId, "botTeam", 11);
    }
}
```
**Impact:** This overwrites the correct team that was set in `SpawnAIGetClientId()` if `botTeam` is somehow empty or -1 at this point.

### 2. RefreshAll() - Team Restoration Logic (Lines 3980-4017)
**Location:** `rpgfunk.cs` line 3980-4017
**Problem:** If `botTeam` is not stored, it defaults to team 1, but if `botTeam` IS stored but is -1, it will set team to -1
```torquescript
%storedBotTeam = fetchData(%clientId, "botTeam");
if(%storedBotTeam == "" || %storedBotTeam == -1 || %storedBotTeam == "0")
{
    // botTeam not stored - defaults to 1
    %storedBotTeam = 1;
}
%currentTeam = GameBase::getTeam(%clientId);
if(%currentTeam != %storedBotTeam)
{
    GameBase::setTeam(%clientId, %storedBotTeam);
}
```
**Impact:** If `botTeam` is stored as -1 (which shouldn't happen, but might), RefreshAll will set team to -1.

### 3. UpdateTeam() - Still Being Called (Line 2931)
**Location:** `rpgfunk.cs` line 2931
**Problem:** Even though we skip UpdateTeam() for bots in `Game::playerSpawn()`, UpdateTeam() might still be called from other places
**Impact:** If UpdateTeam() runs and botTeam isn't stored yet, it sets team based on race (defaults to 1), but if race isn't set, it might default to -1.

### 4. GameBase::setTeam() - Not Synchronous
**Problem:** `GameBase::setTeam()` is not synchronous - multiple calls might not take effect immediately
**Impact:** Even if we set team correctly, a subsequent check might still see -1 if the engine hasn't processed it yet.

## Root Cause Analysis

The issue is likely a **race condition** where:
1. `AI::spawn()` creates Player object with team -1 (engine default)
2. `Game::playerSpawn()` is called immediately (synchronously)
3. Our bot detection in `Game::playerSpawn()` might fail if name isn't set yet
4. `UpdateTeam()` runs and sets team based on race (or defaults to 1)
5. `SpawnAIGetClientId()` runs 0.5s later and sets correct team
6. But `RefreshAll()` might run BEFORE `SpawnAIGetClientId()` completes
7. If `botTeam` isn't stored yet, `RefreshAll()` can't restore the team

## All Functions That Set/Reset Teams

### Direct Team Setting:
1. **SpawnAI()** (line 1546-1551) - Sets team immediately if client ID available
2. **SpawnAIGetClientId()** (line 2150-2180) - Sets team after getting client ID
3. **AI::setWeapons()** (line 447-450) - Sets team before GiveThisStuff()
4. **HardcodeAIskills()** (line 3966-3968) - Sets team before RefreshAll()
5. **RefreshAll()** (line 4005) - Restores team from botTeam
6. **UpdateTeam()** (line 2931) - Sets team based on race (for players only)
7. **VerifyEnemyBotTeam()** (line 7883-7891) - Verifies and restores team

### Functions That Call RefreshAll (which might reset team):
1. **GiveThisStuff()** - Calls RefreshAll() at the end
2. **HardcodeAIskills()** - Calls RefreshAll() at the end (line 3976)
3. **Game::playerSpawned()** - Calls RefreshAll() for players (line 353)

### Functions That Call UpdateTeam:
1. **Game::playerSpawn()** - Calls UpdateTeam() for players (line 208)
2. **AI::initDrones()** - Calls UpdateTeam() for drones (line 1206)

## The Real Problem

The issue is that `botTeam` might not be stored when `RefreshAll()` runs. The sequence is:
1. `AI::setWeapons()` is called (scheduled 0.2s after spawn)
2. `AI::setWeapons()` calls `GiveThisStuff()`
3. `GiveThisStuff()` calls `RefreshAll()`
4. `RefreshAll()` checks `botTeam` - but it might not be stored yet!
5. `RefreshAll()` defaults to team 1, but if something else set it to -1, it stays -1

But wait - `SpawnAIGetClientId()` stores `botTeam` at line 2148, and it's scheduled 0.5s after spawn. But `AI::setWeapons()` is scheduled 0.2s after spawn. So `AI::setWeapons()` might run BEFORE `SpawnAIGetClientId()` completes!

## Solution

We need to ensure `botTeam` is stored BEFORE any function that calls `RefreshAll()` runs.

