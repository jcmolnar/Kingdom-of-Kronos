# Complete Spawning System Changes Comparison

## Comparison Date: November 26, 2025
**Old Version:** `C:\Users\Joe\Desktop\Old Versions KoK\2025-14-11 - Kingdom of Kronos HOSTING - Copy\RPG\Scripts`
**Current Version:** `C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts`

---

## 1. AI::helper() Function

### OLD VERSION:
```tribescript
function AI::helper(%aiName, %displayName, %commandIssuer, %loadout)
{
    dbecho($dbechoMode, "AI::helper(" @ %aiName @ ", " @ %displayName @ ", " @ %commandIssuer @ ")");
    
    // ... spawn position logic ...
    
    %n = getAInumber();
    %newName = %aiName @ %n;
    if(%aiName == %displayName)
        %displayName = $NameForRace[%aiName] @ %newName;
    
    %result = SpawnAI(%newName, %displayName, %spawnPos, %commandIssuer, %loadout);
    
    // Only increment $numAI if spawning succeeded
    if(%result != -1)
    {
        $numAI++;
        setAInumber(%newName, %n);
    }
    
    return %result;
}
```

### CURRENT VERSION:
```tribescript
function AI::helper(%aiName, %displayName, %commandIssuer, %loadout)
{
    // Initialize %loadout to empty string if not provided
    if(%loadout == "")
        %loadout = "";
    
    dbecho($dbechoMode, "AI::helper(" @ %aiName @ ", " @ %displayName @ ", " @ %commandIssuer @ ")");
    
    // ... spawn position logic (IDENTICAL) ...
    
    %n = getAInumber();
    %newName = %aiName @ %n;
    if(%aiName == %displayName)
        %displayName = $NameForRace[%aiName] @ %newName;
    
    %result = SpawnAI(%newName, %displayName, %spawnPos, %commandIssuer, %loadout);
    
    // Only increment $numAI if spawning succeeded
    if(%result != -1)
    {
        $numAI++;
        setAInumber(%newName, %n);
    }
    
    return %result;
}
```

**DIFFERENCE:** 
- **CURRENT ADDS:** Loadout initialization check (minor improvement, doesn't affect spawning)

**IMPACT ON ENEMY SPAWNING:** None - identical logic

---

## 2. SpawnAI() Function

### OLD VERSION:
```tribescript
function SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout)
{
    dbecho($dbechoMode, "SpawnAI(" @ %newName @ ", " @ %displayName @ ", " @ %aiSpawnPos @ ", " @ %commandIssuer @ ")");
    
    %retval = createAI(%newName, %aiSpawnPos, %displayName);
    
    if(%retval != -1 && %retval != "")
    {
        schedule("SpawnAIPostCreate(\"" @ %newName @ "\", \"" @ %displayName @ "\", \"" @ %commandIssuer @ "\", \"" @ %loadout @ "\");", 0.15);
        return %newName;
    }
    else
    {
        echo("ERROR: SpawnAI - createAI failed for " @ %newName);
        return -1;
    }
}
```

### CURRENT VERSION:
```tribescript
function SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout)
{
    // Initialize %loadout to empty string if not provided
    if(%loadout == "")
        %loadout = "";
    
    dbecho($dbechoMode, "SpawnAI(" @ %newName @ ", " @ %displayName @ ", " @ %aiSpawnPos @ ", " @ %commandIssuer @ ")");
    
    %retval = createAI(%newName, %aiSpawnPos, %displayName);
    
    if(%retval != -1 && %retval != "")
    {
        schedule("SpawnAIPostCreate(\"" @ %newName @ "\", \"" @ %displayName @ "\", \"" @ %commandIssuer @ "\", \"" @ %loadout @ "\");", 0.15);
        return %newName;
    }
    else
    {
        echo("ERROR: SpawnAI - createAI failed for " @ %newName);
        return -1;
    }
}
```

**DIFFERENCE:**
- **CURRENT ADDS:** Loadout initialization check (minor improvement)

**IMPACT ON ENEMY SPAWNING:** None - identical logic

---

## 3. SpawnAIPostCreate() Function

### OLD VERSION:
```tribescript
function SpawnAIPostCreate(%newName, %displayName, %commandIssuer, %loadout)
{
    // ... validation ...
    
    else if(GetWord(%commandIssuer, 0) == "SpawnPoint")
    {
        storeData(%aiId, "SpawnBotInfo", %commandIssuer);
        %spawnPointId = GetWord(%commandIssuer, 1);
        if(%spawnPointId != "" && %spawnPointId != -1)
        {
            $numAIperSpawnPoint[%spawnPointId]++;
        }
        UpdateTeam(%aiId);
        AI::SetVar(%newName, spotDist, $AIspotDist);
    }
    
    AI::setWeapons(%newName, %loadout);
}
```

### CURRENT VERSION:
```tribescript
function SpawnAIPostCreate(%newName, %displayName, %commandIssuer, %loadout)
{
    // ... validation ...
    
    else if(GetWord(%commandIssuer, 0) == "SpawnPoint")
    {
        storeData(%aiId, "SpawnBotInfo", %commandIssuer);
        %spawnPointId = GetWord(%commandIssuer, 1);
        if(%spawnPointId != "" && %spawnPointId != -1)
        {
            // Increment spawn point counter (original simple behavior)
            $numAIperSpawnPoint[%spawnPointId]++;
        }
        UpdateTeam(%aiId);
        AI::SetVar(%newName, spotDist, $AIspotDist);
    }
    
    AI::setWeapons(%newName, %loadout);
}
```

**DIFFERENCE:** 
- **CURRENT ADDS:** Comment only

**IMPACT ON ENEMY SPAWNING:** None - identical logic

---

## 4. AI::onDroneKilled() Function

### OLD VERSION:
```tribescript
function AI::onDroneKilled(%aiName)
{
    // ... validation ...
    
    if(GetWord(%spawnBotInfo, 0) == "SpawnPoint")
    {
        %spawnPointId = GetWord(%spawnBotInfo, 1);
        if(%spawnPointId != "" && %spawnPointId != -1)
        {
            if($numAIperSpawnPoint[%spawnPointId] > 0)
                $numAIperSpawnPoint[%spawnPointId]--;
        }
    }
    // ... rest of cleanup ...
}
```

### CURRENT VERSION:
```tribescript
function AI::onDroneKilled(%aiName)
{
    // ... validation ...
    
    if(GetWord(%spawnBotInfo, 0) == "SpawnPoint")
    {
        %spawnPointId = GetWord(%spawnBotInfo, 1);
        if(%spawnPointId != "" && %spawnPointId != -1)
        {
            if($numAIperSpawnPoint[%spawnPointId] > 0)
                $numAIperSpawnPoint[%spawnPointId]--;
        }
    }
    // ... rest of cleanup ...
}
```

**DIFFERENCE:** None - identical

**IMPACT ON ENEMY SPAWNING:** None

---

## 5. ValidateSpawnPointCounters() Function

### OLD VERSION:
```tribescript
function ValidateSpawnPointCounters()
{
    // ... loop through spawn points ...
    
    // Count bots that belong to this spawn point
    for(%j = 0; (%botId = GetWord(%botList, %j)) != -1; %j++)
    {
        %spawnInfo = fetchData(%botId, "SpawnBotInfo");
        if(%spawnInfo != "")
        {
            if(GetWord(%spawnInfo, 0) == "SpawnPoint")
            {
                %spawnPointId = GetWord(%spawnInfo, 1);
                if(%spawnPointId == %spawnPoint)
                {
                    %actualCount++;
                }
            }
        }
    }
    
    // Correct the counter if it's wrong
    if($numAIperSpawnPoint[%spawnPoint] != %actualCount)
    {
        echo("WARNING: Spawn point " @ %spawnPoint @ " counter mismatch...");
        $numAIperSpawnPoint[%spawnPoint] = %actualCount;
    }
}
```

### CURRENT VERSION:
```tribescript
function ValidateSpawnPointCounters()
{
    // ... loop through spawn points ...
    
    // Count bots that belong to this spawn point
    // IMPORTANT: Skip town bots - they don't belong to spawn points
    for(%j = 0; (%botId = GetWord(%botList, %j)) != -1; %j++)
    {
        // Skip town bots immediately
        if(fetchData(%botId, "BotInfoAiName") != "")
            continue;
        
        %spawnInfo = fetchData(%botId, "SpawnBotInfo");
        if(%spawnInfo != "")
        {
            if(GetWord(%spawnInfo, 0) == "SpawnPoint")
            {
                %spawnPointId = GetWord(%spawnInfo, 1);
                if(%spawnPointId == %spawnPoint)
                {
                    %actualCount++;
                }
            }
        }
    }
    
    // Correct the counter if it's wrong
    if($numAIperSpawnPoint[%spawnPoint] != %actualCount)
    {
        echo("WARNING: Spawn point " @ %spawnPoint @ " counter mismatch...");
        $numAIperSpawnPoint[%spawnPoint] = %actualCount;
    }
}
```

**DIFFERENCE:**
- **CURRENT ADDS:** Town bot filtering - skips bots with `BotInfoAiName` flag

**IMPACT ON ENEMY SPAWNING:** 
- **POSITIVE:** Prevents town bots from being counted in spawn point counters
- **RESULT:** More accurate counter validation

---

## 6. SpawnLoop() Function (Spawn.cs)

### OLD VERSION:
```tribescript
function SpawnLoop(%this)
{
    // ... setup code ...
    
    %maxs = Cap(round(GetWord(%info, 0) * $spawnMultiplier), 0, "inf");
    if(%flag && $numAIperSpawnPoint[%this] < %maxs)
    {
        %AIname = AI::helper($spawnIndex[%index], $spawnIndex[%index], "SpawnPoint " @ %this);
        
        // If spawning failed, validate counters immediately to fix potential desync
        if(%AIname == -1 || %AIname == "")
        {
            schedule("ValidateSpawnPointCounters();", 1);
        }
    }
    
    schedule("SpawnLoop(" @ %this @ ");", %delay);
}
```

### CURRENT VERSION:
```tribescript
function SpawnLoop(%this)
{
    // ... setup code ...
    
    %maxs = Cap(round(GetWord(%info, 0) * $spawnMultiplier), 0, "inf");
    if(%flag && $numAIperSpawnPoint[%this] < %maxs)
    {
        %AIname = AI::helper($spawnIndex[%index], $spawnIndex[%index], "SpawnPoint " @ %this);
        
        // If spawning failed, validate counters immediately to fix potential desync
        if(%AIname == -1 || %AIname == "")
        {
            schedule("ValidateSpawnPointCounters();", 1);
        }
    }
    
    schedule("SpawnLoop(" @ %this @ ");", %delay);
}
```

**DIFFERENCE:** None - identical

**IMPACT ON ENEMY SPAWNING:** None

---

## 7. InitSpawnPoints() Function (Spawn.cs)

### OLD VERSION:
```tribescript
function InitSpawnPoints()
{
    // ... setup code ...
    
    if(%info != "")
    {
        $numAIperSpawnPoint[%this] = 0;
        %indexes = "";
        
        // ... echo statements ...
        
        SpawnLoop(%this);
    }
}
```

### CURRENT VERSION:
```tribescript
function InitSpawnPoints()
{
    // ... setup code ...
    
    if(%info != "")
    {
        // CRITICAL: Check if a spawn loop is already running for this spawn point
        if($SpawnLoopActive[%this] == "")
        {
            $numAIperSpawnPoint[%this] = 0;
            %indexes = "";
            
            // ... echo statements ...
            
            $SpawnLoopActive[%this] = true;
            SpawnLoop(%this);
        }
        else
        {
            echo("WARNING: InitSpawnPoints - Spawn loop already active...");
        }
    }
}
```

**DIFFERENCE:**
- **CURRENT ADDS:** `$SpawnLoopActive` check to prevent duplicate spawn loops

**IMPACT ON ENEMY SPAWNING:**
- **POSITIVE:** Prevents multiple spawn loops from running for the same spawn point
- **RESULT:** Prevents race conditions that could cause over-spawning

---

## 8. WipeFromZone() Function (zone.cs) - CRITICAL CHANGE

### OLD VERSION:
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

### CURRENT VERSION:
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
            // Skip town bots - they should never be wiped from zones
            // Town bots have BotInfoAiName set, and they're managed by dynamic loading system
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

**DIFFERENCE:**
- **CURRENT ADDS:** Town bot skip check

**IMPACT ON ENEMY SPAWNING:**
- **POSITIVE:** Prevents town bots from being killed by zone wipe
- **NEUTRAL:** Enemy bots are still killed as before (no change to enemy bot behavior)

---

## 9. DespawnZoneBots() Function (Ai.cs) - NEW FUNCTION

### OLD VERSION:
**DOES NOT EXIST** - This function is completely new

### CURRENT VERSION:
```tribescript
function DespawnZoneBots(%zoneIndex)
{
    // ... town bot despawning code ...
    
    // CRITICAL: Also despawn enemy bots spawned from spawn points in this zone
    // This handles the case where multiple bots were incorrectly spawned per spawn point
    // (due to race conditions). We need to kill them and properly decrement spawn counters.
    %botList = GetBotIdList();
    for(%j = 0; (%botId = GetWord(%botList, %j)) != -1; %j++)
    {
        // Skip town bots (they're already handled above)
        if(fetchData(%botId, "BotInfoAiName") != "")
            continue;
        
        // Check if this bot is in the zone we're despawning
        if(Zone::getDesc(fetchData(%botId, "zone")) == %zoneDesc)
        {
            // Check if this bot came from a spawn point
            %spawnBotInfo = fetchData(%botId, "SpawnBotInfo");
            if(%spawnBotInfo != "" && GetWord(%spawnBotInfo, 0) == "SpawnPoint")
            {
                %spawnPointId = GetWord(%spawnBotInfo, 1);
                
                // Kill the bot - this will trigger the death handler
                // which automatically decrements $numAIperSpawnPoint[%spawnPointId]
                storeData(%botId, "noDropLootbagFlag", True);
                Player::Kill(%botId);
                echo("Despawned enemy bot from spawn point " @ %spawnPointId @ " (zone " @ %zoneIndex @ ")");
            }
        }
    }
}
```

**DIFFERENCE:**
- **NEW FUNCTION:** Completely new function that didn't exist in old version

**IMPACT ON ENEMY SPAWNING:**
- **POTENTIAL ISSUE:** This function kills enemy bots when zones are empty
- **WHEN CALLED:** Called by `ScheduleZoneBotDespawn()` when all players leave a zone
- **PURPOSE:** Intended to clean up incorrectly spawned bots, but may be killing valid enemy bots
- **CRITICAL:** This could be causing enemy bots to be killed when they shouldn't be

---

## 10. #deletebot Command (comchat.cs)

### OLD VERSION:
```tribescript
// In #deletebot command handler
%spawnBotInfo = fetchData(%id, "SpawnBotInfo");
if(%spawnBotInfo != "" && GetWord(%spawnBotInfo, 0) == "SpawnPoint")
{
    %spawnPointId = GetWord(%spawnBotInfo, 1);
    if(%spawnPointId != "" && %spawnPointId != -1 && $numAIperSpawnPoint[%spawnPointId] > 0)
        $numAIperSpawnPoint[%spawnPointId]--;
}
```

### CURRENT VERSION:
```tribescript
// In #deletebot command handler
%spawnBotInfo = fetchData(%id, "SpawnBotInfo");
if(%spawnBotInfo != "" && GetWord(%spawnBotInfo, 0) == "SpawnPoint")
{
    %spawnPointId = GetWord(%spawnBotInfo, 1);
    if(%spawnPointId != "" && %spawnPointId != -1 && $numAIperSpawnPoint[%spawnPointId] > 0)
        $numAIperSpawnPoint[%spawnPointId]--;
}
```

**DIFFERENCE:** None - identical

**IMPACT ON ENEMY SPAWNING:** None

---

## 11. aiNumTable Cleanup in DespawnZoneBots()

### OLD VERSION:
**DOES NOT EXIST** - No aiNumTable cleanup for town bots

### CURRENT VERSION:
```tribescript
// In DespawnZoneBots() function
// Clear aiNumTable entry if town bot has one (prevents number slot from being permanently filled)
// This ensures enemy bots don't start at 50+ because town bot slots are still marked as used
if($tmpbotn[%aiName] != "" && $tmpbotn[%aiName] != -1)
{
    $aiNumTable[$tmpbotn[%aiName]] = "";
    $tmpbotn[%aiName] = "";
}
```

**DIFFERENCE:**
- **NEW:** Clears aiNumTable entries when town bots despawn

**IMPACT ON ENEMY SPAWNING:**
- **POSITIVE:** Prevents enemy bots from starting with high numbers (50+) because town bot slots are still marked as used
- **RESULT:** Enemy bots get proper sequential numbering

---

## Summary of Changes Affecting Enemy Spawning

### ✅ NO IMPACT (Identical Logic):
1. `AI::helper()` - Only adds loadout initialization
2. `SpawnAI()` - Only adds loadout initialization
3. `SpawnAIPostCreate()` - Only adds comment
4. `AI::onDroneKilled()` - Identical
5. `SpawnLoop()` - Identical
6. `#deletebot` command - Identical

### 🔄 POSITIVE IMPACT (Improvements):
1. **ValidateSpawnPointCounters()** - Skips town bots, more accurate counting
2. **InitSpawnPoints()** - Prevents duplicate spawn loops
3. **WipeFromZone()** - Skips town bots (doesn't affect enemy bots)
4. **aiNumTable cleanup** - Prevents enemy bots from starting at 50+

### ⚠️ POTENTIAL ISSUE:
1. **DespawnZoneBots()** - NEW FUNCTION
   - **PROBLEM:** Kills enemy bots when zones are empty
   - **WHEN:** Called 10 seconds after all players leave a zone
   - **INTENTION:** Clean up incorrectly spawned bots
   - **RISK:** May be killing valid enemy bots that should remain
   - **RECOMMENDATION:** Review if this should only kill town bots, not enemy bots

---

## Critical Finding: DespawnZoneBots() May Be Killing Enemy Bots

The `DespawnZoneBots()` function (lines 2020-2048 in Ai.cs) is a **NEW FUNCTION** that:
1. Despawns town bots when zones are empty (expected)
2. **ALSO kills enemy bots** spawned from spawn points in empty zones

This could be causing enemy bots to be killed when they shouldn't be, especially if:
- Players leave a zone temporarily
- The function is called before players return
- Enemy bots are killed even though they should persist

**Question:** Should `DespawnZoneBots()` only handle town bots, or should it also kill enemy bots? This may be the root cause of enemy bot spawning issues.










