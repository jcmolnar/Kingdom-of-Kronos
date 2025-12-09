# Complete List of AI Modifications from Original og KoK to Current Version

## Summary
This document lists ALL AI-related modifications made from the original og KoK version to the current version that could affect enemy bot spawning/respawning.

---

## 1. Ai.cs - Core AI Functions

### 1.1 `createAI()` Function
**Original og KoK:** Directly returns after `AI::spawn()`
**Current Version:** 
- Added `createAIPostSpawn()` scheduled call (0.1s delay)
- Added error logging for missing armor types
- **IMPACT:** Post-spawn initialization is now asynchronous

### 1.2 `createAIPostSpawn()` Function
**Original og KoK:** DOES NOT EXIST
**Current Version:** 
- NEW FUNCTION (lines 100-129)
- Called via schedule from `createAI()`
- Sets `BotInfoAiName`, `RACE`, and other bot data
- **IMPACT:** This is only for bots created via `createAI()`, not `SpawnAI()`

### 1.3 `AI::helper()` Function
**Original og KoK (line 680-726):**
```tribescript
function AI::helper(%aiName, %displayName, %commandIssuer, %loadout)
{
    // No loadout initialization
    $numAI++;
    SpawnAI(%newName, %displayName, %spawnPos, %commandIssuer, %loadout);
    setAInumber(%newName, %n);
    return %newName;
}
```

**Current Version (line 884-935):**
```tribescript
function AI::helper(%aiName, %displayName, %commandIssuer, %loadout)
{
    // Initialize %loadout to empty string if not provided
    if(%loadout == "")
        %loadout = "";
    // ... rest identical ...
    $numAI++;
    SpawnAI(%newName, %displayName, %spawnPos, %commandIssuer, %loadout);
    setAInumber(%newName, %n);
    return %newName;
}
```
**DIFFERENCE:** Added loadout initialization check
**IMPACT:** Minor - shouldn't affect spawning

### 1.4 `SpawnAI()` Function
**Original og KoK (line 727-780):**
```tribescript
function SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout)
{
    %retval = createAI(%newName, %aiSpawnPos, %displayName);
    if(%retval != -1)
    {
        %aiId = AI::getId( %newName );
        // ... set vars ...
        if(GetWord(%commandIssuer, 0) == "SpawnPoint")
        {
            storeData(%aiId, "SpawnBotInfo", %commandIssuer);
            $numAIperSpawnPoint[GetWord(%commandIssuer, 1)]++;
            UpdateTeam(%aiId);
            AI::SetVar(%newName, spotDist, $AIspotDist);
        }
        return ( %newName );
    }
    else
        return -1;
}
```

**Current Version (line 936-1011):**
```tribescript
function SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout)
{
    // Initialize %loadout to empty string if not provided
    if(%loadout == "")
        %loadout = "";
    
    %retval = createAI(%newName, %aiSpawnPos, %displayName);
    if(%retval != -1)
    {
        %aiId = AI::getId( %newName );
        // ... set vars ...
        if(GetWord(%commandIssuer, 0) == "SpawnPoint")
        {
            storeData(%aiId, "SpawnBotInfo", %commandIssuer);
            $numAIperSpawnPoint[GetWord(%commandIssuer, 1)]++;
            
            // Set RACE synchronously so UpdateTeam() works correctly
            %guardtype = clipTrailingNumbers(%newName);
            if($BotInfo[%newName, RACE] != "")
                %armor = $RaceToArmorType[$BotInfo[%newName, RACE]];
            else
                %armor = $RaceToArmorType[$NameForRace[%guardtype]];
            
            if(%armor != "" && %armor != -1 && $ArmorTypeToRace[%armor] != "")
                storeData(%aiId, "RACE", $ArmorTypeToRace[%armor]);
            
            UpdateTeam(%aiId);
            AI::SetVar(%newName, spotDist, $AIspotDist);
        }
        return %newName;
    }
    else
        return -1;
}
```
**DIFFERENCES:**
1. Added loadout initialization
2. Added RACE setting before `UpdateTeam()` (to prevent warnings)
**IMPACT:** RACE setting is additional but shouldn't affect counter logic

### 1.5 `AI::onDroneKilled()` Function
**Original og KoK (line 910-955):**
```tribescript
function AI::onDroneKilled(%aiName)
{
    if(!$SinglePlayer)
    {
        %aiId = AI::getId(%aiName);
        %team = fetchData(%aiId, "botTeam");
        storeData(%aiId, "botTeam", "");
        $aiNumTable[$tmpbotn[%aiName]] = "";
        $tmpbotn[%aiName] = "";
        
        if(fetchData(%aiId, "SpawnBotInfo") != "")
        {
            if(GetWord(fetchData(%aiId, "SpawnBotInfo"), 0) == "SpawnPoint")
            {
                $numAIperSpawnPoint[GetWord(fetchData(%aiId, "SpawnBotInfo"), 1)]--;
            }
            storeData(%aiId, "SpawnBotInfo", "");
            storeData(%aiId, "AIattackMarker", "");
            // ... pet/botgroup stuff ...
        }
        else
            schedule("AI::setupAI(" @ %aiName @ ", " @ %team @ ");", 60);
    }
}
```

**Current Version (line 1159-1216):**
```tribescript
function AI::onDroneKilled(%aiName)
{
    if(!$SinglePlayer)
    {
        %aiId = AI::getId(%aiName);
        %team = fetchData(%aiId, "botTeam");
        storeData(%aiId, "botTeam", "");
        $aiNumTable[$tmpbotn[%aiName]] = "";
        $tmpbotn[%aiName] = "";
        
        if(fetchData(%aiId, "SpawnBotInfo") != "")
        {
            if(GetWord(fetchData(%aiId, "SpawnBotInfo"), 0) == "SpawnPoint")
            {
                $numAIperSpawnPoint[GetWord(fetchData(%aiId, "SpawnBotInfo"), 1)]--;
            }
            storeData(%aiId, "SpawnBotInfo", "");
            storeData(%aiId, "AIattackMarker", "");
            // ... pet/botgroup stuff ...
            // ClearEvents(%aiId); - REMOVED (was added, then removed)
        }
        else
            schedule("AI::setupAI(" @ %aiName @ ", " @ %team @ ");", 60);
    }
}
```
**DIFFERENCES:**
1. Removed early return check for invalid `AI::getId()` (was added, then removed to match original)
2. Removed `ClearEvents()` call (was added, then removed)
3. Pet owner message handling matches original (unconditional)
**IMPACT:** Now matches original exactly

### 1.6 NEW FUNCTIONS (Not in Original)
**Functions that DON'T exist in original og KoK:**
1. `createAIPostSpawn()` - Post-spawn initialization (only for `createAI()`, not `SpawnAI()`)
2. `SpawnZoneBots()` - Zone-based town bot spawning
3. `DespawnZoneBots()` - Zone-based bot despawning
4. `ScheduleZoneBotDespawn()` - Scheduled zone bot despawn
5. `CheckAndDespawnZoneBots()` - Check before despawning
6. `InitTownBotPostSpawn()` - Town bot post-spawn setup
7. `InitTownBotItemsForBot()` - Town bot item mounting
8. `verifyWeaponMount()` - Verify weapon mounting
9. `verifyArmorMount()` - Verify armor mounting

**IMPACT:** These are for town bots, not enemy bots from spawn points

---

## 2. Spawn.cs - Spawning Logic

### 2.1 `InitSpawnPoints()` Function
**Original og KoK:** Simple initialization, calls `SpawnLoop()` directly
**Current Version:** Identical - no changes
**STATUS:** ✅ MATCHES ORIGINAL

### 2.2 `SpawnLoop()` Function
**Original og KoK (line 41-74):**
```tribescript
function SpawnLoop(%this)
{
    // ... calculate delay and index ...
    %flag = "";
    if($SelectiveZoneBotSpawning)
    {
        if(Zone::getNumPlayers($MarkerZone[%this]) > 0 || $MarkerZone[%this] == "")
            %flag = True;
    }
    else
        %flag = True;
    
    %maxs = Cap(round(GetWord(%info, 0) * $spawnMultiplier), 0, "inf");
    if(%flag && $numAIperSpawnPoint[%this] < %maxs)
        %AIname = AI::helper($spawnIndex[%index], $spawnIndex[%index], "SpawnPoint " @ %this);
    
    schedule("SpawnLoop(" @ %this @ ");", %delay);
}
```

**Current Version (line 41-78):**
```tribescript
function SpawnLoop(%this)
{
    // ... calculate delay and index ... (IDENTICAL)
    %flag = "";
    if($SelectiveZoneBotSpawning)
    {
        if(Zone::getNumPlayers($MarkerZone[%this]) > 0 || $MarkerZone[%this] == "")
            %flag = True;
    }
    else
        %flag = True;
    
    %maxs = Cap(round(GetWord(%info, 0) * $spawnMultiplier), 0, "inf");
    if(%flag && $numAIperSpawnPoint[%this] < %maxs)
    {
        %AIname = AI::helper($spawnIndex[%index], $spawnIndex[%index], "SpawnPoint " @ %this);
        // Spawn failure handling - counters are managed synchronously, no validation needed
    }
    
    schedule("SpawnLoop(" @ %this @ ");", %delay);
}
```
**DIFFERENCE:** Added braces around spawn call (cosmetic only)
**STATUS:** ✅ MATCHES ORIGINAL FUNCTIONALLY

---

## 3. Server.cs - Initialization

### 3.1 Spawn Point Initialization
**Original og KoK:** Calls `InitSpawnPoints()` directly
**Current Version:** 
- Line 267: Calls `InitSpawnPoints()` 
- Line 270: Comment says "ValidateSpawnPointCounters removed - not needed with synchronous spawning"
**STATUS:** ✅ NO VALIDATION FUNCTIONS ACTIVE

---

## 4. comchat.cs - Admin Commands

### 4.1 `#deletebot` Command
**Original og KoK:** DOES NOT EXIST
**Current Version (line 5551-5595):**
- NEW ADMIN COMMAND
- Manually decrements counter if bot has `SpawnBotInfo` with "SpawnPoint"
- Has validation check: `if($numAIperSpawnPoint[%spawnPointId] > 0)`
- **IMPACT:** Only used for admin manual deletion, not normal bot death

---

## 5. zone.cs - Zone Management

### 5.1 `WipeFromZone()` Function
**Original og KoK (line 682-707):**
```tribescript
function WipeFromZone(%z)
{
    %run = 1;
    for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
        if(Zone::getDesc(fetchData(%cl, "zone")) == $Zone::Desc[%z])
            %run = 0;
    
    if(%run == 1)
    {
        %list = GetEveryoneIdList();
        for(%cl = 0; GetWord(%list, %cl) != -1; %cl += 1)
        {
            %id = GetWord(%list,%cl);
            if (Player::isAiControlled(%id) && Zone::getDesc(fetchData(%id, "zone")) == $Zone::Desc[%z])
            {
                storeData(%id, "noDropLootbagFlag", True);
                Player::Kill(%id);
            }
        }
    }
}
```

**Current Version (line 682-708):**
```tribescript
function WipeFromZone(%z)
{
    // ... same check logic ...
    if(%run == 1)
    {
        %list = GetEveryoneIdList();
        for(%cl = 0; GetWord(%list, %cl) != -1; %cl += 1)
        {
            %id = GetWord(%list,%cl);
            // Skip town bots - they should never be wiped from zones
            if(fetchData(%id, "BotInfoAiName") != "")
                continue;
            
            if (Player::isAiControlled(%id) && Zone::getDesc(fetchData(%id, "zone")) == $Zone::Desc[%z])
            {
                storeData(%id, "noDropLootbagFlag", True);
                Player::Kill(%id);
            }
        }
    }
}
```
**DIFFERENCE:** Added town bot skip check
**IMPACT:** Shouldn't affect enemy bots (they don't have `BotInfoAiName`)

### 5.2 NEW FUNCTIONS (Not in Original)
**Functions that DON'T exist in original og KoK:**
1. `SpawnZoneBots()` - Spawns town bots when players enter zone
2. `DespawnZoneBots()` - Despawns bots when players leave zone (includes enemy bots)
3. `ScheduleZoneBotDespawn()` - Schedules zone bot despawn
4. `CheckAndDespawnZoneBots()` - Checks before despawning

**`DespawnZoneBots()` IMPACT (line 1961-1990):**
- Kills enemy bots when players leave zones
- Uses `Player::Kill()` which should trigger `AI::onDroneKilled()`
- **POTENTIAL ISSUE:** This is NEW functionality - original doesn't despawn enemy bots when zones empty

---

## 6. rpgfunk.cs - Utility Functions

### 6.1 `UpdateTeam()` Function
**Original og KoK (line 928-935):**
```tribescript
function UpdateTeam(%clientId)
{
    %t = $TeamForRace[fetchData(%clientId, "RACE")];
    GameBase::setTeam(%clientId, %t);
}
```

**Current Version (line 1937-1952):**
```tribescript
function UpdateTeam(%clientId)
{
    %race = fetchData(%clientId, "RACE");
    %t = $TeamForRace[%race];
    
    // Validate team value - default to team 1 (enemy) if invalid
    if(%t == "" || %t == -1)
    {
        echo("WARNING: UpdateTeam - Invalid team for race '" @ %race @ "', defaulting to team 1");
        %t = 1;
    }
    
    GameBase::setTeam(%clientId, %t);
}
```
**DIFFERENCE:** Added validation with default to team 1
**IMPACT:** Shouldn't affect spawning, just prevents errors

### 6.2 `FellOffMap()` Function
**Original og KoK:** Calls `Player::Kill()` for AI bots
**Current Version:** Identical
**STATUS:** ✅ MATCHES ORIGINAL

### 6.3 `ClearVariables()` Function
**Original og KoK:** Clears various bot variables
**Current Version:** 
- Added safety check for `BotInfoAiName` before accessing `$BotFollowDirective`
- Does NOT clear `SpawnBotInfo` (uses different storage system)
**STATUS:** ✅ DOESN'T AFFECT SPAWN COUNTERS

---

## 7. playerdamage.cs - Death Handling

### 7.1 `Player::onKilled()` Function
**Original og KoK:** Handles player death, calls `Client::onKilled()`
**Current Version:** 
- No AI-specific changes that would affect `AI::onDroneKilled()` being called
- `Player::Kill()` should still trigger `AI::onDroneKilled()` callback
**STATUS:** ✅ SHOULD WORK CORRECTLY

---

## 8. Functions REMOVED from Current Version

### 8.1 Validation Functions (Never in Original)
- `ValidateSpawnPointCounters()` - REMOVED (was added, then removed)
- `CountBotsForSpawnPoint()` - REMOVED (was added, then removed)
- `ValidateSpawnPointCountersLoop()` - REMOVED (was added, then removed)
- `SpawnAIPostCreate()` - REMOVED (was added, then removed)

**STATUS:** ✅ NO VALIDATION FUNCTIONS ACTIVE

---

## 9. Critical Differences Summary

### 9.1 Counter Increment/Decrement Logic
**STATUS:** ✅ NOW MATCHES ORIGINAL EXACTLY
- Increment: `$numAIperSpawnPoint[GetWord(%commandIssuer, 1)]++;` (line 975)
- Decrement: `$numAIperSpawnPoint[GetWord(fetchData(%aiId, "SpawnBotInfo"), 1)]--;` (line 1177)

### 9.2 NEW Functionality That Could Interfere
1. **`DespawnZoneBots()` in Ai.cs (line 1961-1990)**
   - Kills enemy bots when players leave zones
   - This is NEW - original doesn't do this
   - Uses `Player::Kill()` which should trigger `AI::onDroneKilled()`
   - **POTENTIAL ISSUE:** If this runs before `AI::onDroneKilled()` can decrement counter, or if there's a timing issue

2. **Zone-based bot management**
   - Original: Bots stay in zones even when empty
   - Current: Bots are despawned when zones empty (10 second delay)
   - **POTENTIAL ISSUE:** This could interfere with normal respawning if zones are being loaded/unloaded

### 9.3 RACE Setting in SpawnAI
**NEW:** RACE is now set synchronously before `UpdateTeam()` (line 977-996)
**IMPACT:** Prevents warnings, shouldn't affect counter logic

---

## 10. Conclusion

### Functions That Match Original:
- ✅ `AI::helper()` - Minor loadout init only
- ✅ `SpawnAI()` - Counter increment matches (RACE setting added but harmless)
- ✅ `AI::onDroneKilled()` - Counter decrement matches exactly
- ✅ `SpawnLoop()` - Logic matches exactly
- ✅ `InitSpawnPoints()` - Matches exactly

### NEW Functionality That Could Cause Issues:
1. **`DespawnZoneBots()`** - Kills enemy bots when zones empty (NEW)
2. **Zone-based spawning/despawning** - Not in original

### Recommendation:
The core spawning logic matches the original. The issue is likely:
1. **`DespawnZoneBots()` interfering with normal bot death/respawn cycle** ⚠️ **CRITICAL**
   - This function kills enemy bots when players leave zones (10 second delay)
   - Original og KoK does NOT despawn enemy bots when zones empty
   - This is NEW functionality that could be preventing respawning
   - Uses `Player::Kill()` which should trigger `AI::onDroneKilled()`, but timing could be an issue

2. Zone loading/unloading causing bots to be killed before they can respawn
3. Timing issue where `SpawnBotInfo` is cleared before counter is decremented

**Next Step:** 
- **PRIMARY SUSPECT:** `DespawnZoneBots()` in `Ai.cs` line 1961-1990
  - This kills enemy bots when zones empty (NEW - not in original)
  - Should be disabled or modified to NOT kill enemy bots from spawn points
  - OR ensure it properly triggers `AI::onDroneKilled()` before killing

**Files to Check:**
- `Ai.cs` line 1961-1990: `DespawnZoneBots()` - kills enemy bots when zones empty
- `zone.cs` line 254, 287, 446: Calls `ScheduleZoneBotDespawn()` when players leave zones
- `connectivity.cs` line 152: Calls `ScheduleZoneBotDespawn()` on disconnect

