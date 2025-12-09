# AI Bot System - Senior Engine Developer Audit Report
## Critical Shell Bot & Player Safety Analysis

**Date:** 2025-01-XX  
**Auditor:** Senior Engine Developer Analysis  
**Scope:** `Ai.cs` - Shell Bot Creation & Real Player Safety

---

## EXECUTIVE SUMMARY

This audit identified **7 critical vulnerabilities** that can create shell bots and **3 high-risk scenarios** where real players could be deleted. The primary issues stem from:

1. **Race conditions** in the spawn flow where scheduled cleanup can execute on wrong objects
2. **Insufficient validation** before `deleteObject()` calls
3. **Scheduled cleanup** that doesn't verify object identity before execution
4. **Ghost cleanup** that can target real players during connection

---

## 1. VULNERABILITIES CREATING SHELL BOTS

### VULN-1: Scheduled Deletion Without Identity Verification
**Location:** `DespawnZoneBots()` line 8082  
**Severity:** CRITICAL  
**Issue:** 
```torquescript
schedule("if(isObject(" @ %playerObj @ ")) deleteObject(" @ %playerObj @ ");", 1.0);
```
This schedules deletion by **object ID only**. If a real player connects and gets assigned the same client ID within 1 second, the scheduled deletion will delete the **new player's object**, leaving a shell bot with the old client ID.

**Race Condition:**
1. Bot dies at clientId 2050, `DespawnZoneBots` schedules deletion in 1.0s
2. Real player connects, gets clientId 2050 (engine reuses IDs)
3. Scheduled deletion fires, deletes **player's** object
4. Result: Shell bot (clientId 2050 with bot data) + deleted player object

**Fix Required:** Store bot name/validation token, verify before deletion.

---

### VULN-2: `$ClientIdRecentlyFreed` Scheduled Clear Without Validation
**Location:** Multiple locations (lines 775, 3350, 3652, 3711, 3801, 3821, 4611)  
**Severity:** HIGH  
**Issue:**
```torquescript
$ClientIdRecentlyFreed[%clientId] = getSimTime();
schedule("$ClientIdRecentlyFreed[" @ %clientId @ "] = \"\";", 10.0);
```
If a real player connects and gets the same clientId before the 10s schedule fires, the scheduled clear will execute on the **player's clientId**, potentially allowing bot cleanup to target them.

**Fix Required:** Store validation token, verify before clearing.

---

### VULN-3: `SpawnAIGetClientId` Ghost Bot Cleanup Missing Save File Check
**Location:** `SpawnAIGetClientId()` lines 3256-3358  
**Severity:** CRITICAL  
**Issue:** The ghost bot cleanup checks `HasLoadedAndSpawned` but **does NOT check for character save file** before deleting. If a real player is loading (name not set yet, no save file created yet), this could delete their object.

**Current Code:**
```torquescript
%hasLoadedAndSpawned = fetchData(%ghostBotId, "HasLoadedAndSpawned");
if(%hasLoadedAndSpawned != "True" && %hasLoadedAndSpawned != "true" && %hasLoadedAndSpawned != "1")
{
    deleteObject(%ghostPlayerObj);
}
```

**Missing Check:**
```torquescript
// MISSING: Check for character save file
%ghostName = Client::getName(%ghostBotId);
if(%ghostName != "" && %ghostName != -1)
{
    %characterFile = "temp\\" @ %ghostName @ ".cs";
    if(isFile(%characterFile))
        return; // Real player - abort
}
```

---

### VULN-4: `PeriodicGhostClientIdCleanup` Can Target Loading Players
**Location:** `PeriodicGhostClientIdCleanup()` lines 6187-6231  
**Severity:** HIGH  
**Issue:** This function scans for empty-name Player objects and deletes them if they have bot data. However, **real players can have empty names during initial connection** (before `Client::getName()` returns their name). The function only checks `BotInfoAiName`/`SpawnBotInfo`, which could be stale from a previous bot.

**Current Logic:**
```torquescript
%name = Client::getName(%checkId);
if(%name == "" || %name == -1)
{
    %botInfoAiName = fetchData(%checkId, "BotInfoAiName");
    if(%botInfoAiName != "" || %spawnBotInfo != "")
    {
        CleanupGhostClientId(%checkId, %aiName); // DANGEROUS
    }
}
```

**Missing Safeguard:** Check if client is connected but name not set yet (real player loading).

---

### VULN-5: `PreSpawnCleanup` Deletes Player Object Without Final Verification
**Location:** `PreSpawnCleanup()` lines 468-474  
**Severity:** MEDIUM  
**Issue:** While `PreSpawnCleanup` checks for save file at the start, it doesn't re-check **after** getting the player object. If a real player connects between the check and deletion, their object could be deleted.

**Current Code:**
```torquescript
%pobj = Client::getOwnedObject(%clientId);
if(%pobj != -1 && %pobj != "" && isObject(%pobj))
{
    deleteObject(%pobj); // No final verification
}
```

**Fix Required:** Re-check save file immediately before deletion.

---

### VULN-6: `ReconcileSpawnCounters` Deletes Player Objects Without Save File Check
**Location:** `ReconcileSpawnCounters()` lines 390-396  
**Severity:** HIGH  
**Issue:** When removing dead bots from registry, it deletes player objects without checking for save files.

**Current Code:**
```torquescript
if(%playerObj != -1 && %playerObj != "" && isObject(%playerObj))
{
    deleteObject(%playerObj); // Missing save file check
}
```

---

### VULN-7: `createAI` Stale Player Object Cleanup Missing Final Verification
**Location:** `createAI()` lines 933-940  
**Severity:** MEDIUM  
**Issue:** While it checks save file before the loop, it doesn't re-check **immediately before** deleting the stale object. A real player could connect between checks.

---

## 2. REAL PLAYER SAFETY REPORT

### RISK-1: Scheduled Cleanup Can Target Real Players
**Functions Affected:**
- `DespawnZoneBots()` line 8082
- All `$ClientIdRecentlyFreed` scheduled clears (7 locations)

**Scenario:**
1. Bot dies, cleanup schedules deletion/clear in 1-10 seconds
2. Real player connects, gets same clientId
3. Scheduled cleanup fires, targets player

**Current Safeguards:** ❌ NONE for scheduled operations

**Required Fix:** Store validation token (bot name + timestamp), verify before execution.

---

### RISK-2: Ghost Cleanup During Player Connection
**Functions Affected:**
- `SpawnAIGetClientId()` ghost cleanup (line 3351)
- `PeriodicGhostClientIdCleanup()` (line 6220)
- `CheckGhostClientIdDelayed()` (line 6024)

**Scenario:**
1. Real player connects, name not set yet (empty)
2. Ghost cleanup runs, sees empty name + stale bot data
3. Deletes player object

**Current Safeguards:** ⚠️ Partial (checks `HasLoadedAndSpawned` but not save file)

**Required Fix:** Check for connected client + save file before deletion.

---

### RISK-3: `PreSpawnCleanup` Race Condition
**Functions Affected:**
- `PreSpawnCleanup()` line 472
- `createAI()` line 938
- `SpawnAIGetClientId()` line 3102

**Scenario:**
1. Check save file → not found (bot)
2. Real player connects, gets same clientId, save file created
3. Delete player object → **player data lost**

**Current Safeguards:** ⚠️ Check at start, but not immediately before deletion

**Required Fix:** Re-check save file immediately before `deleteObject()`.

---

## 3. REFACTORED CODE BLOCKS

### FIX-1: `SpawnAIGetClientId` - Atomic Validation Before Deletion

```torquescript
function SpawnAIGetClientId(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout)
{
    // ... existing code ...
    
    // CRITICAL: Before reusing a client ID, ensure any old player object is deleted
    // This prevents shell bots from forming when a new bot spawns before the old one's player object is deleted
    // SAFEGUARD: Triple-check this is not a real player before ANY deletion
    if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
    {
        // SAFEGUARD 1: Check save file FIRST (most reliable)
        %playerNameCheck = Client::getName(%aiId);
        if(%playerNameCheck != "" && %playerNameCheck != -1)
        {
            %characterFile = "temp\\" @ %playerNameCheck @ ".cs";
            if(isFile(%characterFile))
            {
                echo("[SPAWN FLOW] SpawnAIGetClientId(): CRITICAL SAFEGUARD - clientId " @ %aiId @ " is a real player (" @ %playerNameCheck @ ") with save file. Aborting to prevent data loss.");
                %aiId = -1; // Reset to force retry with different client ID
            }
        }
        
        // SAFEGUARD 2: Only proceed if we confirmed it's not a real player
        if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
        {
            %existingPlayerObj = Client::getOwnedObject(%aiId);
            if(%existingPlayerObj != -1 && %existingPlayerObj != "" && isObject(%existingPlayerObj))
            {
                // SAFEGUARD 3: Re-check save file IMMEDIATELY before deletion (prevents race condition)
                %existingName = Client::getName(%aiId);
                if(%existingName != "" && %existingName != -1)
                {
                    %characterFileCheck = "temp\\" @ %existingName @ ".cs";
                    if(isFile(%characterFileCheck))
                    {
                        echo("[SPAWN FLOW] SpawnAIGetClientId(): CRITICAL SAFEGUARD - Found real player " @ %existingName @ " (clientId=" @ %aiId @ ") with save file. Aborting to prevent data loss.");
                        %aiId = -1; // Reset to force retry
                        return -1; // Exit immediately
                    }
                }
                
                // SAFEGUARD 4: Check if this is actually our newly-spawned bot or an old one
                %existingBotInfoAiName = fetchData(%aiId, "BotInfoAiName");
                
                // Only proceed with deletion if we confirmed it's not a real player AND it's an old bot
                if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
                {
                    // If the name doesn't match OR BotInfoAiName is set to a different bot, this is an old player object
                    if((%existingName != "" && %existingName != -1 && String::ICompare(%displayName, %existingName) != 0) || 
                       (%existingBotInfoAiName != "" && %existingBotInfoAiName != -1 && %existingBotInfoAiName != "0" && %existingBotInfoAiName != %newName))
                    {
                        // SAFEGUARD 5: Final check - verify this is NOT a connected real player
                        // Check if client is connected but name matches a player pattern (not bot pattern)
                        %isPlayerPattern = false;
                        if(%existingName != "" && %existingName != -1)
                        {
                            // Real player names typically don't match bot patterns
                            // But check anyway to be safe
                            if(String::findSubStr(%existingName, "Alien") == -1 && 
                               String::findSubStr(%existingName, "Admin") == -1 &&
                               String::findSubStr(%existingName, "Demon") == -1 &&
                               String::findSubStr(%existingName, "Ogre") == -1 &&
                               String::findSubStr(%existingName, "Pigman") == -1 &&
                               String::findSubStr(%existingName, "Undead") == -1 &&
                               String::findSubStr(%existingName, "Minotaur") == -1 &&
                               String::findSubStr(%existingName, "Seal") == -1 &&
                               String::findSubStr(%existingName, "God") == -1)
                            {
                                // Name doesn't match bot patterns - could be a real player
                                // Double-check with save file
                                %finalCharacterFile = "temp\\" @ %existingName @ ".cs";
                                if(isFile(%finalCharacterFile))
                                {
                                    echo("[SPAWN FLOW] SpawnAIGetClientId(): FINAL SAFEGUARD - Name '" @ %existingName @ "' has save file. This is a REAL PLAYER. Aborting deletion.");
                                    %aiId = -1;
                                    return -1;
                                }
                            }
                        }
                        
                        // All safeguards passed - safe to delete old bot object
                        if(%aiId != -1 && %aiId != "" && %aiId != "False" && %aiId != "false")
                        {
                            echo("[SPAWN FLOW] SpawnAIGetClientId(): WARNING - Found old player object " @ %existingPlayerObj @ " for clientId " @ %aiId @ " (name='" @ %existingName @ "', BotInfoAiName='" @ %existingBotInfoAiName @ "'). Deleting to prevent shell bot.");
                            deleteObject(%existingPlayerObj);
                            Client::setOwnedObject(%aiId, -1);
                            // Run cleanup to clear any stale data
                            PreSpawnCleanup(%aiId);
                        }
                    }
                }
            }
        }
    }
    
    // ... rest of function ...
}
```

---

### FIX-2: `AI::onDroneKilled` - Atomic Cleanup with Validation

```torquescript
function AI::onDroneKilled(%aiName)
{
    dbecho($dbechoMode, "AI::onDroneKilled(" @ %aiName @ ")");
    
    if(!$SinglePlayer)
    {
        // ... existing ID lookup code ...
        
        if(%aiId == -1 || %aiId == "" || %aiId == "False" || %aiId == "false")
        {
            echo("[BOT SHELL DEBUG] AI::onDroneKilled(): ERROR - Could not get AI ID for " @ %aiName @ " (may be shell bot)");
            return;
        }
        
        // CRITICAL SAFEGUARD: Verify this is actually a bot, not a real player
        %playerNameCheck = Client::getName(%aiId);
        if(%playerNameCheck != "" && %playerNameCheck != -1)
        {
            %characterFile = "temp\\" @ %playerNameCheck @ ".cs";
            if(isFile(%characterFile))
            {
                echo("[BOT SHELL DEBUG] AI::onDroneKilled(): CRITICAL SAFEGUARD - Detected real player " @ %playerNameCheck @ " (clientId=" @ %aiId @ ") with save file. Skipping bot cleanup to prevent data loss.");
                // Clear any stale bot data that might have caused this false positive
                $EnemyBotData[%aiId, "BotInfoAiName"] = "";
                $EnemyBotData[%aiId, "SpawnBotInfo"] = "";
                $TownBotData[%aiId, "BotInfoAiName"] = "";
                $ClientData[%aiId, "BotInfoAiName"] = "";
                $BotInfoAiName[%aiId] = "";
                $BotRegistry[%aiId] = "";
                return; // CRITICAL: Exit immediately to prevent player data loss
            }
        }
        
        // ... rest of existing cleanup code ...
        
        // CRITICAL: When scheduling $ClientIdRecentlyFreed clear, store validation token
        // This prevents scheduled clear from executing on a real player who got the same clientId
        %validationToken = %aiName @ "_" @ getSimTime();
        $ClientIdRecentlyFreedToken[%aiId] = %validationToken;
        schedule("if($ClientIdRecentlyFreedToken[" @ %aiId @ "] == \"" @ %validationToken @ "\") $ClientIdRecentlyFreed[" @ %aiId @ "] = \"\"; $ClientIdRecentlyFreedToken[" @ %aiId @ "] = \"\";", 1.5);
    }
}
```

---

### FIX-3: `DespawnZoneBots` - Scheduled Deletion with Validation Token

```torquescript
function DespawnZoneBots(%zoneIndex)
{
    // ... existing code ...
    
    // When scheduling deletion, store validation token
    %validationToken = %botName @ "_" @ getSimTime();
    $DespawnValidationToken[%clientId] = %validationToken;
    
    // CRITICAL: Schedule deletion with validation check
    schedule("if($DespawnValidationToken[" @ %clientId @ "] == \"" @ %validationToken @ "\" && !isFile(\"temp\\\\" @ %playerName @ ".cs\")) { if(isObject(" @ %playerObj @ ")) deleteObject(" @ %playerObj @ "); $DespawnValidationToken[" @ %clientId @ "] = \"\"; }", 1.0);
    
    // ... rest of function ...
}
```

---

### FIX-4: `PeriodicGhostClientIdCleanup` - Player Connection Check

```torquescript
function PeriodicGhostClientIdCleanup()
{
    %startId = 2000;
    %endId = %startId + 300;
    
    %ghostCount = 0;
    for(%checkId = %startId; %checkId <= %endId; %checkId++)
    {
        %playerObj = Client::getOwnedObject(%checkId);
        if(%playerObj != -1 && %playerObj != "")
        {
            %name = Client::getName(%checkId);
            
            // CRITICAL: Check if this is a real player loading (connected but name not set yet)
            // Real players can have empty names during initial connection
            %isConnected = false;
            for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
            {
                if(%cl == %checkId)
                {
                    %isConnected = true;
                    break;
                }
            }
            
            // If connected but name is empty, this might be a real player loading - skip it
            if(%isConnected && (%name == "" || %name == -1))
            {
                // Check if save file exists (player might have name set but getName() not ready)
                // If no save file and no bot data, skip (could be loading player)
                %botInfoAiName = fetchData(%checkId, "BotInfoAiName");
                %spawnBotInfo = fetchData(%checkId, "SpawnBotInfo");
                if((%botInfoAiName == "" || %botInfoAiName == -1) && (%spawnBotInfo == "" || %spawnBotInfo == -1))
                {
                    // No bot data and connected - likely a real player loading, skip
                    continue;
                }
            }
            
            // CRITICAL: Check for save file before cleanup
            if(%name != "" && %name != -1)
            {
                %characterFile = "temp\\" @ %name @ ".cs";
                if(isFile(%characterFile))
                {
                    // Real player with save file - skip
                    continue;
                }
            }
            
            if(%name == "" || %name == -1)
            {
                %botInfoAiName = fetchData(%checkId, "BotInfoAiName");
                %spawnBotInfo = fetchData(%checkId, "SpawnBotInfo");
                
                // Only cleanup if we have confirmed bot data AND no save file
                if((%botInfoAiName != "" || %spawnBotInfo != "") && !%isConnected)
                {
                    %aiName = %botInfoAiName;
                    if(%aiName == "" || %aiName == -1 || %aiName == "0")
                        %aiName = "GhostBot_" @ %checkId;
                    
                    echo("WARNING: PeriodicGhostClientIdCleanup - Found ghost client ID " @ %checkId @ " (name: '" @ %name @ "', BotInfoAiName: '" @ %botInfoAiName @ "'). Cleaning up...");
                    CleanupGhostClientId(%checkId, %aiName);
                    %ghostCount++;
                }
            }
        }
    }
    
    if(%ghostCount > 0)
        echo("PeriodicGhostClientIdCleanup: Cleaned up " @ %ghostCount @ " ghost client ID(s)");
    
    schedule("PeriodicGhostClientIdCleanup();", 30);
}
```

---

### FIX-5: `PreSpawnCleanup` - Final Verification Before Deletion

```torquescript
function PreSpawnCleanup(%clientId)
{
    // Hard guard: never touch real players (non-AI or players with a save file)
    if(%clientId == "" || %clientId == -1)
        return;
    if(!Player::isAiControlled(%clientId))
    {
        %playerNameCheck = Client::getName(%clientId);
        if(%playerNameCheck != "" && %playerNameCheck != -1)
        {
            %characterFile = "temp\\" @ %playerNameCheck @ ".cs";
            if(isFile(%characterFile))
                return; // real player with save file
        }
    }
    
    if(%clientId == "" || %clientId == -1)
        return;
    
    echo("[PRE-SPAWN CLEANUP] Cleaning up stale data for clientId " @ %clientId);
    
    // If a player object still exists, delete it to prevent shells before reuse
    %pobj = Client::getOwnedObject(%clientId);
    if(%pobj != -1 && %pobj != "" && isObject(%pobj))
    {
        // CRITICAL: Final verification immediately before deletion (prevents race condition)
        %finalNameCheck = Client::getName(%clientId);
        if(%finalNameCheck != "" && %finalNameCheck != -1)
        {
            %finalCharacterFile = "temp\\" @ %finalNameCheck @ ".cs";
            if(isFile(%finalCharacterFile))
            {
                echo("[PRE-SPAWN CLEANUP] CRITICAL SAFEGUARD - Real player " @ %finalNameCheck @ " (clientId=" @ %clientId @ ") detected immediately before deletion. Aborting to prevent data loss.");
                return; // Abort immediately
            }
        }
        
        // Additional check: Verify this is not a connected real player
        %isConnected = false;
        for(%cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl))
        {
            if(%cl == %clientId)
            {
                %isConnected = true;
                break;
            }
        }
        
        // If connected but no save file yet, check if it's a real player by checking if it's NOT AI-controlled
        if(%isConnected && !Player::isAiControlled(%clientId))
        {
            echo("[PRE-SPAWN CLEANUP] CRITICAL SAFEGUARD - Connected client " @ %clientId @ " is NOT AI-controlled. This is a real player. Aborting deletion.");
            return; // Abort immediately
        }
        
        // All checks passed - safe to delete
        echo("[PRE-SPAWN CLEANUP] Deleting existing player object " @ %pobj @ " for clientId " @ %clientId @ " to prevent shells.");
        deleteObject(%pobj);
        Client::setOwnedObject(%clientId, -1);
    }
    
    // ... rest of cleanup code ...
}
```

---

## 4. SUMMARY OF REQUIRED CHANGES

### Immediate Actions:
1. ✅ Add save file check to ALL `deleteObject()` calls
2. ✅ Add validation tokens to ALL scheduled cleanup operations
3. ✅ Add final verification immediately before deletion (prevents race conditions)
4. ✅ Add player connection check to ghost cleanup functions
5. ✅ Store validation tokens for `$ClientIdRecentlyFreed` scheduled clears

### Code Locations Requiring Fixes:
- `SpawnAIGetClientId()` - Lines 3054-3107 (add final verification)
- `AI::onDroneKilled()` - Line 4611 (add validation token)
- `DespawnZoneBots()` - Line 8082 (add validation token)
- `PreSpawnCleanup()` - Line 472 (add final verification)
- `createAI()` - Line 938 (add final verification)
- `ReconcileSpawnCounters()` - Line 394 (add save file check)
- `PeriodicGhostClientIdCleanup()` - Line 6220 (add connection check)
- `CleanupGhostClientId()` - Add save file check at start
- `CheckGhostClientIdDelayed()` - Add save file check

---

## 5. TESTING RECOMMENDATIONS

1. **Race Condition Test:** Spawn bot, kill it, immediately connect real player with same clientId, verify player is not deleted
2. **Ghost Cleanup Test:** Connect real player, verify ghost cleanup doesn't target them during name initialization
3. **Scheduled Cleanup Test:** Kill bot, schedule cleanup, connect real player before schedule fires, verify player is not affected
4. **Shell Bot Test:** Kill bot, verify no shell bot remains (player object deleted, data cleared)

---

**END OF AUDIT REPORT**

