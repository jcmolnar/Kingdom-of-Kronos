# AI System Comparison: Old vs Current Version

## Comparison Date: November 26, 2025
**Old Version:** `C:\Users\Joe\Desktop\Old Versions KoK\2025-14-11 - Kingdom of Kronos HOSTING - Copy\RPG\Scripts`
**Current Version:** `C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts`

---

## 1. SpawnAIPostCreate() Function

### OLD VERSION (Nov 14, 2025):
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
        // ... rest of code ...
    }
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
        // ... rest of code ...
    }
}
```

**DIFFERENCE:** Identical - both versions increment counter the same way. Current version just has a comment.

---

## 2. AI::onDroneKilled() Function

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
    // ... rest of code ...
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
    // ... rest of code ...
}
```

**DIFFERENCE:** Identical - both versions decrement counter the same way.

---

## 3. ValidateSpawnPointCounters() Function

### OLD VERSION:
```tribescript
function ValidateSpawnPointCounters()
{
    %group = nameToID("MissionGroup\\SpawnPoints");
    if(%group == -1)
        return;
    
    for(%i = 0; %i <= Group::objectCount(%group) - 1; %i++)
    {
        %spawnPoint = Group::getObject(%group, %i);
        if(%spawnPoint != -1)
        {
            %actualCount = 0;
            %botList = GetBotIdList();
            
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
                echo("WARNING: Spawn point " @ %spawnPoint @ " counter mismatch: recorded=" @ $numAIperSpawnPoint[%spawnPoint] @ ", actual=" @ %actualCount @ " - correcting...");
                $numAIperSpawnPoint[%spawnPoint] = %actualCount;
            }
        }
    }
}
```

### CURRENT VERSION:
```tribescript
function ValidateSpawnPointCounters()
{
    %group = nameToID("MissionGroup\\SpawnPoints");
    if(%group == -1)
        return;
    
    for(%i = 0; %i <= Group::objectCount(%group) - 1; %i++)
    {
        %spawnPoint = Group::getObject(%group, %i);
        if(%spawnPoint != -1)
        {
            %actualCount = 0;
            %botList = GetBotIdList();
            
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
                echo("WARNING: Spawn point " @ %spawnPoint @ " counter mismatch: recorded=" @ $numAIperSpawnPoint[%spawnPoint] @ ", actual=" @ %actualCount @ " - correcting...");
                $numAIperSpawnPoint[%spawnPoint] = %actualCount;
            }
        }
    }
}
```

**DIFFERENCE:** 
- **CURRENT VERSION ADDS:** Town bot filtering - skips bots with `BotInfoAiName` flag
- **REASON:** Town bots are now Player objects and shouldn't be counted in spawn point counters

---

## 4. SpawnLoop() Function (Spawn.cs)

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
            // Spawning failed - validate counters after a short delay to catch issues
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
            // Spawning failed - validate counters after a short delay to catch issues
            schedule("ValidateSpawnPointCounters();", 1);
        }
    }
    
    schedule("SpawnLoop(" @ %this @ ");", %delay);
}
```

**DIFFERENCE:** Identical - both versions have the same spawn failure validation.

---

## 5. InitSpawnPoints() Function (Spawn.cs)

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
        // This prevents duplicate spawn loops if InitSpawnPoints() is called multiple times
        // (This was added to prevent issues with town bot system, but is safe to keep)
        if($SpawnLoopActive[%this] == "")
        {
            $numAIperSpawnPoint[%this] = 0;
            %indexes = "";
            
            // ... echo statements ...
            
            // Mark that a spawn loop is active for this spawn point
            $SpawnLoopActive[%this] = true;
            
            SpawnLoop(%this);
        }
        else
        {
            echo("WARNING: InitSpawnPoints - Spawn loop already active for spawn point " @ %this @ ", skipping initialization");
        }
    }
}
```

**DIFFERENCE:**
- **CURRENT VERSION ADDS:** `$SpawnLoopActive` check to prevent duplicate spawn loops
- **REASON:** Prevents issues if `InitSpawnPoints()` is called multiple times (e.g., during town bot system initialization)

---

## 6. InitTownBots() Function - MAJOR CHANGE

### OLD VERSION:
```tribescript
function InitTownBots()
{
    dbecho($dbechoMode, "InitTownBots()");
    
    $TownBotList = "";
    
    %group = nameToId("MissionGroup/TownBots");
    
    if(%group != -1)
    {
        %cnt = Group::objectCount(%group);
        for(%i = 0; %i <= %cnt - 1; %i++)
        {
            %object = Group::getObject(%group, %i);
            %name = Object::getName(%object);
            if(getObjectType(%object) == "SimGroup")
            {
                %marker = GatherBotInfo(%object);
            }
            
            // IMMEDIATELY SPAWN AS ITEM OBJECT
            %townbot = newObject("", "Item", $BotInfo[%name, RACE] @ "TownBot", 1, false);
            
            addToSet("MissionCleanup", %townbot);
            GameBase::setMapName(%townbot, $BotInfo[%name, NAME]);
            GameBase::setPosition(%townbot, GameBase::getPosition(%marker));
            GameBase::setRotation(%townbot, GameBase::getRotation(%marker));
            GameBase::setTeam(%townbot, $BotInfo[%name, TEAM]);
            GameBase::playSequence(%townbot, 0, "root");
            %townbot.name = %name;
            
            $TownBotList = $TownBotList @ %townbot @ " ";
        }
    }
}
```

### CURRENT VERSION:
```tribescript
// Global variables for dynamic bot loading
$TownBotRegistry = "";  // List of registered bot names (not spawned yet)
$TownBotZone[0] = "";  // Maps bot name to zone index (0 = no zone/unloaded area)
$TownBotSpawned[0] = "";  // Maps bot name to clientId if spawned, "" if not spawned
$ZonePlayerCount[0] = 0;  // Tracks number of players in each zone
$ZoneBotDespawnSchedule[0] = "";  // Tracks scheduled despawn for each zone

function InitTownBots()
{
    dbecho($dbechoMode, "InitTownBots() - Registering bots for dynamic loading");
    echo("===== Calling InitTownBots() - Registering bots for dynamic loading =====");
    
    $TownBotList = "";  // Clear the list (now stores clientIds, not Item object IDs)
    $TownBotRegistry = "";  // Clear bot registry
    
    %group = nameToId("MissionGroup/TownBots");
    
    if(%group != -1)
    {
        %cnt = Group::objectCount(%group);
        for(%i = 0; %i <= %cnt - 1; %i++)
        {
            %object = Group::getObject(%group, %i);
            if(%object == -1 || %object == "")
                continue;
                
            %name = Object::getName(%object);
            %marker = "";  // Initialize marker
            
            if(getObjectType(%object) == "SimGroup")
            {
                %marker = GatherBotInfo(%object);
                if(%marker == "" || %marker == -1)
                {
                    echo("WARNING: InitTownBots - GatherBotInfo returned invalid marker for " @ %name);
                    continue;
                }
            }
            else if(getObjectType(%object) == "Marker")
            {
                %marker = %object;
            }
            
            if(%marker == "" || %marker == -1)
            {
                echo("WARNING: InitTownBots - No valid marker found for " @ %name);
                continue;
            }
            
            // REGISTER FOR DYNAMIC LOADING (DON'T SPAWN YET)
            %spawnPos = GameBase::getPosition(%marker);
            %spawnRot = GameBase::getRotation(%marker);
            
            // Store bot position and rotation for later spawning
            $BotInfo[%name, SPAWN_POS] = %spawnPos;
            $BotInfo[%name, SPAWN_ROT] = %spawnRot;
            $BotInfo[%name, SPAWN_MARKER] = %marker;
            
            // Determine which zone this bot belongs to
            %botZone = GetBotZone(%spawnPos);
            $TownBotZone[%name] = %botZone;
            $TownBotSpawned[%name] = "";  // Not spawned yet
            
            // Add to registry
            $TownBotRegistry = $TownBotRegistry @ %name @ " ";
            
            echo("Registered bot: " @ %name @ " for zone: " @ %botZone @ " (" @ $Zone::Desc[%botZone] @ ")");
        }
    }
    
    echo("===== InitTownBots() completed - Registered " @ GetWordCount($TownBotRegistry) @ " bots for dynamic loading =====");
}
```

**MAJOR DIFFERENCES:**
1. **OLD:** Immediately spawns all town bots as Item objects
2. **CURRENT:** Only registers bots for dynamic loading (doesn't spawn them yet)
3. **OLD:** `$TownBotList` stores Item object IDs
4. **CURRENT:** `$TownBotList` stores clientIds (Player objects)
5. **CURRENT ADDS:** Zone-based dynamic loading system
6. **CURRENT ADDS:** `GetBotZone()` function to determine bot zones
7. **CURRENT ADDS:** `SpawnZoneBots()` and `DespawnZoneBots()` functions

---

## 7. AI::lookAtPlayer() Function

### OLD VERSION:
```tribescript
function AI::lookAtPlayer(%clientId, %guardId)
{
    // ... validation ...
    
    %clpos = GameBase::getPosition(%clientId);
    %gupos = GameBase::getPosition(%guardId);
    
    %v1 = Vector::sub(%clpos, %gupos);
    %norm = Vector::normalize(%v1);
    %rot = Vector::getRotation(%norm);
    
    GameBase::setRotation(%guardId, %rot);
    
    %gurot = GameBase::getRotation(%guardId);
    %temp = Vector::sub(%rot, %gurot);
    %temp2 = GetWord(%temp, 2);
    
    if(floor(%temp2) != 0)
        %rot = GetWord(%rot, 0) @ " " @ GetWord(%rot, 1) @ " " @ (GetWord(%rot, 2) + 3.141592654);
    
    RotateTownBot(%guardId, %rot);
}
```

### CURRENT VERSION:
```tribescript
function AI::lookAtPlayer(%clientId, %guardId)
{
    // ... validation ...
    
    // Use player objects for position calculations
    %clpos = GameBase::getPosition(%clientPlayer);
    %gupos = GameBase::getPosition(%guardPlayer);
    
    // Calculate direction vector from bot to player
    %v1 = Vector::sub(%clpos, %gupos);
    
    // For horizontal rotation, use only X and Y components (ignore Z/height)
    %dx = GetWord(%v1, 0);
    %dy = GetWord(%v1, 1);
    %horizontalVec = %dx @ " " @ %dy @ " 0";
    
    %norm = Vector::normalize(%horizontalVec);
    %rot = Vector::getRotation(%norm);
    
    %gurot = GameBase::getRotation(%guardPlayer);
    %temp = Vector::sub(%rot, %gurot);
    %temp2 = GetWord(%temp, 2);
    
    // For town bots (Player objects), we want them to face TOWARDS the player, not away
    // Check if this is a town bot by checking BotInfoAiName flag
    %botName = fetchData(%guardId, "BotInfoAiName");
    %isTownBot = (%botName != "" && %botName != -1);
    
    if(%isTownBot)
    {
        // Town bot - face towards player (don't add 180 degrees)
        %currentPitch = GetWord(%gurot, 0);
        %currentRoll = GetWord(%gurot, 1);
        %newYaw = GetWord(%rot, 2);
        %rot = %currentPitch @ " " @ %currentRoll @ " " @ %newYaw;
    }
    else
    {
        // Regular guard/enemy bot - use original logic
        if(floor(%temp2) != 0)
            %rot = GetWord(%rot, 0) @ " " @ GetWord(%rot, 1) @ " " @ (GetWord(%rot, 2) + 3.141592654);
    }
    
    RotateTownBot(%guardId, %rot);
}
```

**DIFFERENCES:**
1. **CURRENT:** Uses player objects for position calculations (not client IDs directly)
2. **CURRENT:** Horizontal rotation only (ignores Z/height)
3. **CURRENT:** Town bots face TOWARDS player (no 180-degree flip)
4. **CURRENT:** Enemy bots still use original logic (may face away)

---

## 8. New Functions in Current Version (Not in Old)

### SpawnZoneBots()
- Spawns town bots when players enter a zone
- Converts town bots from Item objects to Player objects (client-based)

### DespawnZoneBots()
- Despawns town bots when zone is empty
- Cleans up bot data

### InitTownBotPostSpawn()
- Post-spawn initialization for town bots (Player objects)
- Sets race/armor validation
- Sets BotInfoAiName flag

### RotateTownBotPostSpawn()
- Post-spawn rotation for town bots

### GetBotZone()
- Determines which zone a bot position belongs to

### ScheduleZoneBotDespawn()
- Schedules zone bot despawn with delay

### CheckAndDespawnZoneBots()
- Checks if despawn is still needed before executing

---

## Summary of Key Changes

### ✅ PRESERVED (Same as Old):
1. `SpawnAIPostCreate()` - Counter increment logic
2. `AI::onDroneKilled()` - Counter decrement logic
3. `SpawnLoop()` - Spawn failure validation

### 🔄 MODIFIED (Added Town Bot Support):
1. `ValidateSpawnPointCounters()` - Added town bot filtering
2. `InitSpawnPoints()` - Added duplicate loop prevention
3. `AI::lookAtPlayer()` - Added town bot face-towards logic

### 🆕 NEW (Town Bot System):
1. `InitTownBots()` - Complete rewrite for dynamic loading
2. `SpawnZoneBots()` - Zone-based spawning
3. `DespawnZoneBots()` - Zone-based despawning
4. `InitTownBotPostSpawn()` - Post-spawn initialization
5. `GetBotZone()` - Zone detection
6. `ScheduleZoneBotDespawn()` - Despawn scheduling
7. `CheckAndDespawnZoneBots()` - Despawn validation

### 📊 Impact:
- **Old Version:** All town bots spawned immediately as Item objects
- **Current Version:** Town bots dynamically loaded as Player objects only when players are in their zones
- **Performance:** Significant improvement - reduces server load by not loading all bots at once
- **Compatibility:** All enemy bot spawning logic preserved, only town bot system changed










