# Empty Zone Despawn Locations

This document lists all locations where empty zone despawning is triggered and executed for both **enemy dungeon zones** and **town zones**.

---

## Trigger Points (Where Empty Zone Detection Occurs)

### 1. Player Disconnect Handler
**File:** `rpg/scripts/connectivity.cs`  
**Function:** `Server::onClientDisconnect(%clientId)`  
**Lines:** 140-162

**Trigger Logic:**
- Decrements `$ZonePlayerCount[%zoneIndex]` when a player disconnects
- Checks if `$ZonePlayerCount[%zoneIndex] <= 0`
- Schedules `DespawnZoneBots(%zoneIndex)` with 30 second delay

**Code:**
```cs
if(!Player::isAiControlled(%clientId))
{
    %zone = fetchData(%clientId, "zone");
    if(%zone != "")
    {
        %zoneIndex = Zone::getIndex(%zone);
        if(%zoneIndex > 0)
        {
            %count = $ZonePlayerCount[%zoneIndex];
            if(%count > 0)
                $ZonePlayerCount[%zoneIndex] = %count - 1;
            
            // If no players left in zone, despawn bots after 30 seconds
            if($ZonePlayerCount[%zoneIndex] <= 0)
            {
                schedule("DespawnZoneBots(" @ %zoneIndex @ ");", 30);
            }
        }
    }
}
```

---

### 2. Zone Exit (Player Leaves Zone)
**File:** `rpg/scripts/zone.cs`  
**Function:** `Zone::Update(%clientId)`  
**Lines:** 487-497

**Trigger Logic:**
- Called when player exits a zone but is still in another zone
- Decrements `$ZonePlayerCount[%oldZoneIndex]`
- Schedules despawn if count <= 0

**Code:**
```cs
%oldZoneIndex = Zone::getIndex(%currentZone);
if(%oldZoneIndex > 0)
{
    Zone::DoExit(%oldZoneIndex, %clientId);
    
    %oldCount = $ZonePlayerCount[%oldZoneIndex];
    if(%oldCount > 0)
        $ZonePlayerCount[%oldZoneIndex] = %oldCount - 1;
    
    // If no players left in old zone, despawn bots after 30 seconds
    if($ZonePlayerCount[%oldZoneIndex] <= 0)
    {
        schedule("DespawnZoneBots(" @ %oldZoneIndex @ ");", 30);
    }
}
```

---

### 3. Zone Change (Player Moves Between Zones)
**File:** `rpg/scripts/zone.cs`  
**Function:** `Zone::Update(%clientId)`  
**Lines:** 651-668

**Trigger Logic:**
- Called when player moves from one zone to another
- Decrements old zone player count
- Validates zone index > 0 before scheduling despawn

**Code:**
```cs
if(!Player::isAiControlled(%clientId))
{
    %oldCount = $ZonePlayerCount[%oldZoneIndex];
    if(%oldCount > 0)
        $ZonePlayerCount[%oldZoneIndex] = %oldCount - 1;
    
    // If no players left in old zone, despawn bots after 30 seconds
    if($ZonePlayerCount[%oldZoneIndex] <= 0 && %oldZoneIndex > 0)
    {
        schedule("DespawnZoneBots(" @ %oldZoneIndex @ ");", 30);
    }
}
```

---

### 4. Zone Exit (Player Exits to No Zone)
**File:** `rpg/scripts/zone.cs`  
**Function:** `Zone::Update(%clientId)`  
**Lines:** 855-868

**Trigger Logic:**
- Called when player exits a zone and is not in any zone
- Validates zone index > 0
- Schedules despawn if zone is empty

**Code:**
```cs
%oldZoneIndex = Zone::getIndex(%currentZone);
if(%oldZoneIndex > 0)
{
    Zone::DoExit(%oldZoneIndex, %clientId);
    
    if(!Player::isAiControlled(%clientId))
    {
        %oldCount = $ZonePlayerCount[%oldZoneIndex];
        if(%oldCount > 0)
            $ZonePlayerCount[%oldZoneIndex] = %oldCount - 1;
        
        // If no players left in old zone, despawn bots after 30 seconds
        if($ZonePlayerCount[%oldZoneIndex] <= 0)
        {
            schedule("DespawnZoneBots(" @ %oldZoneIndex @ ");", 30);
        }
    }
}
```

---

### 5. Periodic Empty Zone Check
**File:** `rpg/scripts/Ai.cs`  
**Function:** `PeriodicEmptyZoneCheck()`  
**Lines:** 10622-10658

**Trigger Logic:**
- Runs every 30 seconds (scheduled at server startup)
- Checks ALL zones (1 to $Zone::Count)
- Uses `Zone::getPlayerList()` to verify actual player count (type 2 = real players only)
- Despawns zones that are actually empty
- Fixes `$ZonePlayerCount` if it's out of sync

**Code:**
```cs
function PeriodicEmptyZoneCheck()
{
    // Check all zones (not just zones with town bots)
    for(%zoneIndex = 1; %zoneIndex <= $Zone::Count; %zoneIndex++)
    {
        %zoneFolderID = $Zone::FolderID[%zoneIndex];
        if(%zoneFolderID == "" || %zoneFolderID == -1)
            continue; // Invalid zone index, skip
        
        %playerList = Zone::getPlayerList(%zoneFolderID, 2); // Type 2 = real players only (not bots)
        %hasPlayers = (%playerList != "" && %playerList != -1);
        
        if(!%hasPlayers)
        {
            // Zone is empty - despawn bots and fix player count
            $ZonePlayerCount[%zoneIndex] = 0;
            DespawnZoneBots(%zoneIndex);
        }
        else
        {
            // Zone has players - fix the count if it's wrong
            %actualPlayerCount = GetWordCount(%playerList);
            %storedCount = $ZonePlayerCount[%zoneIndex];
            if(%storedCount == "")
                %storedCount = 0;
            
            if(%actualPlayerCount != %storedCount)
            {
                $ZonePlayerCount[%zoneIndex] = %actualPlayerCount;
                echo("WARNING: PeriodicEmptyZoneCheck - Zone " @ %zoneIndex @ " player count mismatch. Fixed: " @ %storedCount @ " -> " @ %actualPlayerCount);
            }
        }
    }
    
    // Schedule next check in 30 seconds
    schedule("PeriodicEmptyZoneCheck();", 30);
}
```

**Initialization:** Called at server startup in `InitTownBots()` (Ai.cs:8695-8697)

---

### 6. Admin Command (Manual Trigger)
**File:** `rpg/scripts/comchat.cs`  
**Function:** `processServerCmd(%clientId, %msg)`  
**Lines:** 3315-3404  
**Command:** `#despawnemptyzones` or `#cleanupemptyzones`

**Trigger Logic:**
- Admin-only command (requires admin level >= 1)
- Manually triggers despawn for town bots in empty zones
- Only checks zones that have town bots
- Uses `Zone::getPlayerList()` to verify zone is empty
- Calls `DespawnZoneBots()` directly (no delay)

**Note:** This command is primarily for town bots, but `DespawnZoneBots()` also handles enemy bots if they're in the same zone.

---

## Execution Points (Where Despawning Actually Happens)

### 7. Main Despawn Function
**File:** `rpg/scripts/Ai.cs`  
**Function:** `DespawnZoneBots(%zoneIndex)`  
**Lines:** 10119-10584

**Purpose:** Despawns both **town bots** AND **enemy bots** in the specified zone.

**Execution Flow:**

1. **Validation** (lines 10121-10141):
   - Rejects invalid zone indices (0, empty, -1)
   - Verifies zone is actually empty using `Zone::getPlayerList()`
   - Fixes player count if out of sync
   - Returns early if zone has players

2. **Town Bot Despawn** (lines 10155-10322):
   - Iterates through `$TownBotRegistry`
   - Checks if bot is in target zone
   - Validates bot is safe to modify
   - Calls `AI::delete()` to despawn town bot
   - Cleans up town bot data and registries

3. **Enemy Bot Despawn** (lines 10368-10583):
   - Iterates through `$BotRegistryList`
   - Checks if bot has `SpawnBotInfo` (enemy bot identifier)
   - Verifies bot is in target zone using:
     - Primary: `fetchData(%botId, "zone")`
     - Fallback: `fetchData(%botId, "tmpzone")`
     - Fallback: `fetchData(%botId, "SpawnOriginZoneID")`
   - Validates bot is safe to modify
   - Sets `noExperienceFlag` and `noDropLootbagFlag`
   - Calls `Player::Kill(%botId)` to despawn enemy bot

**Key Safeguards:**
- `IsSafeToModify()` checks prevent affecting real players
- Zone verification prevents despawning bots in wrong zones
- Validation tokens and save file checks

---

### 8. Schedule Wrapper Function
**File:** `rpg/scripts/Ai.cs`  
**Function:** `ScheduleZoneBotDespawn(%zoneIndex)`  
**Lines:** 10587-10594

**Purpose:** Schedules despawn with 30 second delay to prevent crashes from too many operations at once.

**Code:**
```cs
function ScheduleZoneBotDespawn(%zoneIndex)
{
    if(%zoneIndex == 0 || %zoneIndex == "")
        return;
    
    // Schedule despawn after a delay (prevents crash from too many operations happening at once during zone changes)
    schedule("DespawnZoneBots(" @ %zoneIndex @ ");", 30);
}
```

**Note:** Most trigger points call `schedule("DespawnZoneBots(...)", 30)` directly instead of using this wrapper.

---

### 9. Conditional Despawn Check
**File:** `rpg/scripts/Ai.cs`  
**Function:** `CheckAndDespawnZoneBots(%zoneIndex)`  
**Lines:** 10597-10618

**Purpose:** Verifies zone is still empty before executing despawn (prevents duplicate despawns).

**Code:**
```cs
function CheckAndDespawnZoneBots(%zoneIndex)
{
    // Only despawn if the flag is still set to "pending" (not cleared by player re-entry)
    // AND verify that the zone is actually empty (player count <= 0)
    if($ZoneBotDespawnSchedule[%zoneIndex] == "pending")
    {
        %playerCount = $ZonePlayerCount[%zoneIndex];
        if(%playerCount == "")
            %playerCount = 0;
        
        // Only despawn if zone is actually empty
        if(%playerCount <= 0)
        {
            DespawnZoneBots(%zoneIndex);
        }
        else
        {
            // Player entered zone before despawn executed - clear the flag
            $ZoneBotDespawnSchedule[%zoneIndex] = "";
        }
    }
}
```

**Note:** This function exists but may not be used by all trigger points (some directly schedule `DespawnZoneBots()`).

---

## Summary

### Enemy Dungeon Zone Despawn Locations:
1. **Trigger Points:** 
   - Player disconnect (connectivity.cs:154-158)
   - Zone exit (zone.cs:492-496, 663-667, 863-867)
   - Periodic check (Ai.cs:10638)
   - Admin command (comchat.cs:3356-3404)

2. **Execution Point:**
   - `DespawnZoneBots()` enemy bot loop (Ai.cs:10368-10583)

### Town Zone Despawn Locations:
1. **Trigger Points:** 
   - Same as enemy zones (all trigger points handle both types)
   - Admin command explicitly targets town bots (comchat.cs:3315-3404)

2. **Execution Point:**
   - `DespawnZoneBots()` town bot loop (Ai.cs:10155-10322)

### Common Characteristics:
- **30 second delay** between detection and execution (prevents crashes)
- **Zone verification** using `Zone::getPlayerList()` before despawning
- **Player count tracking** via `$ZonePlayerCount[%zoneIndex]`
- **Safety checks** via `IsSafeToModify()` to prevent affecting real players
- **Both bot types** despawned in the same function call

---

*Last Updated: 2025-01-XX*

