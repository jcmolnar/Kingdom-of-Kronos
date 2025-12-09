# Enemy Spawning Comparison: Tribes/KoK vs Current Version

## Comparison Date: November 26, 2025
**Tribes/KoK Version:** `C:\Users\Joe\Desktop\Tribes\KoK\scripts`
**Current Version:** `C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts`

---

## 1. SpawnAI() Function - MAJOR DIFFERENCE

### TRIBES/KOK VERSION:
```tribescript
function SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout)
{
    dbecho($dbechoMode, "SpawnAI(" @ %newName @ ", " @ %displayName @ ", " @ %aiSpawnPos @ ", " @ %commandIssuer @ ")");
    
    %retval = createAI(%newName, %aiSpawnPos, %displayName);
    
    if(%retval != -1)
    {
        %aiId = AI::getId( %newName );
        AI::setVar( %newName,  iq,  100 );
        AI::setVar( %newName,  attackMode, $AIattackMode);
        AI::setVar( %newName,  pathType, $AI::defaultPathType);
        AI::SetVar( %newName,  seekOff, 1);
        AI::setAutomaticTargets( %newName );
        
        if(GetWord(%commandIssuer, 0) == "SpawnPoint")
        {
            //the %commandIssuer is a spawn crystal
            storeData(%aiId, "SpawnBotInfo", %commandIssuer);
            
            // COUNTER INCREMENTED IMMEDIATELY (SYNCHRONOUS)
            $numAIperSpawnPoint[GetWord(%commandIssuer, 1)]++;
            UpdateTeam(%aiId);
            AI::SetVar(%newName, spotDist, $AIspotDist);
        }
        
        AI::setWeapons(%newName, %loadout);
        
        return ( %newName );
    }
    else
    {
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
        // Add small delay to ensure bot is fully initialized before getting ID
        schedule("SpawnAIPostCreate(\"" @ %newName @ "\", \"" @ %displayName @ "\", \"" @ %commandIssuer @ "\", \"" @ %loadout @ "\");", 0.15);
        return %newName;
    }
    else
    {
        echo("ERROR: SpawnAI - createAI failed for " @ %newName);
        return -1;
    }
}

// Separate function called 0.15 seconds later
function SpawnAIPostCreate(%newName, %displayName, %commandIssuer, %loadout)
{
    %aiId = AI::getId(%newName);
    
    // ... validation ...
    
    if(GetWord(%commandIssuer, 0) == "SpawnPoint")
    {
        storeData(%aiId, "SpawnBotInfo", %commandIssuer);
        %spawnPointId = GetWord(%commandIssuer, 1);
        if(%spawnPointId != "" && %spawnPointId != -1)
        {
            // COUNTER INCREMENTED AFTER DELAY (ASYNCHRONOUS)
            $numAIperSpawnPoint[%spawnPointId]++;
        }
        UpdateTeam(%aiId);
        AI::SetVar(%newName, spotDist, $AIspotDist);
    }
    
    AI::setWeapons(%newName, %loadout);
}
```

**CRITICAL DIFFERENCE:**
- **TRIBES/KOK:** Counter incremented **IMMEDIATELY** (synchronously) in `SpawnAI()`
- **CURRENT:** Counter incremented **0.15 SECONDS LATER** (asynchronously) in `SpawnAIPostCreate()`
- **IMPACT:** This 0.15 second delay creates a race condition window where:
  - `SpawnLoop()` can check counter before it's incremented
  - Multiple bots can spawn before counters are updated
  - `ValidateSpawnPointCounters()` can run before counter is set

---

## 2. AI::helper() Function

### TRIBES/KOK VERSION:
```tribescript
function AI::helper(%aiName, %displayName, %commandIssuer, %loadout)
{
    dbecho($dbechoMode, "AI::helper(" @ %aiName @ ", " @ %displayName @ ", " @ %commandIssuer @ ")");
    
    // ... spawn position logic ...
    
    %newName = %aiName @ $numAI;
    if(%aiName == %displayName)
        %displayName = %newName;
    $numAI++;
    SpawnAI(%newName, %displayName, %spawnPos, %commandIssuer, %loadout);
    
    setAInumber(%newName, ($numAi - 1));
    
    return %newName;
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
    
    // ... spawn position logic (with validation) ...
    
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

**DIFFERENCES:**
1. **TRIBES/KOK:** Uses `$numAI` directly, increments before spawn
2. **CURRENT:** Uses `getAInumber()` function, increments only if spawn succeeds
3. **TRIBES/KOK:** No validation of spawn position/marker
4. **CURRENT:** Has validation checks for markers/spawnpoints

**IMPACT:** Current version is safer but more complex

---

## 3. AI::onDroneKilled() Function

### TRIBES/KOK VERSION:
```tribescript
function AI::onDroneKilled(%aiName)
{
    dbecho($dbechoMode, "AI::onDroneKilled(" @ %aiName @ ")");
    
    if(!$SinglePlayer )
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
                //this bot originally spawned from a crystal
                // NO VALIDATION - JUST DECREMENTS
                $numAIperSpawnPoint[GetWord(fetchData(%aiId, "SpawnBotInfo"), 1)]--;
            }
            storeData(%aiId, "SpawnBotInfo", "");
            storeData(%aiId, "AIattackMarker", "");
            
            //botgroup stuff
            %b = AI::IsInWhichBotGroup(%aiId);
            if(%b != -1)
                AI::RemoveBotFromBotGroup(%aiId, %b);
        }
        else
        {
            schedule("AI::setupAI(" @ %aiName @ ", " @ %team @ ");", 60);
        }
    }
}
```

### CURRENT VERSION:
```tribescript
function AI::onDroneKilled(%aiName)
{
    dbecho($dbechoMode, "AI::onDroneKilled(" @ %aiName @ ")");
    
    if(!$SinglePlayer )
    {
        %aiId = AI::getId(%aiName);
        if(%aiId == -1 || %aiId == "")
        {
            // AI object already deleted or never existed - skip cleanup
            echo("ERROR: AI::onDroneKilled - AI " @ %aiName @ " already deleted or invalid. Skipping cleanup.");
            return;
        }
        
        %team = fetchData(%aiId, "botTeam");
        storeData(%aiId, "botTeam", "");
        $aiNumTable[$tmpbotn[%aiName]] = "";
        $tmpbotn[%aiName] = "";
        
        %spawnBotInfo = fetchData(%aiId, "SpawnBotInfo");
        if(%spawnBotInfo != "")
        {
            if(GetWord(%spawnBotInfo, 0) == "SpawnPoint")
            {
                %spawnPointId = GetWord(%spawnBotInfo, 1);
                if(%spawnPointId != "" && %spawnPointId != -1)
                {
                    // HAS VALIDATION - ONLY DECREMENTS IF > 0
                    if($numAIperSpawnPoint[%spawnPointId] > 0)
                        $numAIperSpawnPoint[%spawnPointId]--;
                }
            }
            storeData(%aiId, "SpawnBotInfo", "");
            storeData(%aiId, "AIattackMarker", "");
            
            //pet stuff
            $PetList = RemoveFromCommaList($PetList, %aiId);
            %petowner = fetchData(%aiId, "petowner");
            if(%petowner != "" && %petowner != -1)
            {
                storeData(%petowner, "PersonalPetList", RemoveFromCommaList(fetchData(%petowner, "PersonalPetList"), %aiId));
                Client::sendMessage(%petowner, $MsgRed, Client::getName(%aiId) @ " was slain!");
            }
            storeData(%aiId, "petowner", "");
            
            //botgroup stuff
            %b = AI::IsInWhichBotGroup(%aiId);
            if(%b != -1)
                AI::RemoveBotFromBotGroup(%aiId, %b);
            
            // Clear event commands attached to this bot
            ClearEvents(%aiId);
        }
        else
        {
            schedule("AI::setupAI(" @ %aiName @ ", " @ %team @ ");", 60);
        }
    }
}
```

**DIFFERENCES:**
1. **TRIBES/KOK:** No validation - just decrements counter
2. **CURRENT:** Validates `%aiId` exists, validates `%spawnPointId`, only decrements if counter > 0
3. **CURRENT:** Has pet cleanup code
4. **CURRENT:** Has `ClearEvents()` call

**IMPACT:** Current version is safer but the validation is good

---

## 4. SpawnLoop() Function

### TRIBES/KOK VERSION:
```tribescript
function SpawnLoop(%this)
{
    dbecho($dbechoMode, "SpawnLoop(" @ %this @ ")");
    
    %info = Object::getName(%this);
    
    %mindelay = GetWord(%info, 3);
    %maxdelay = GetWord(%info, 4);
    %diff = %maxdelay - %mindelay;
    %delay = floor(getRandom() * %diff) + %mindelay;
    
    %indexes = "";
    for(%i = 5; GetWord(%info, %i) != -1; %i++)
        %indexes = %indexes @ GetWord(%info, %i) @ " ";
    
    %r = floor(getRandom() * (%i-5));
    %index = GetWord(%indexes, %r);
    
    %flag = "";
    if($SelectiveZoneBotSpawning)
    {
        if(Zone::getNumPlayers($MarkerZone[%this]) > 0 || $MarkerZone[%this] == "")
            %flag = true;
    }
    else
        %flag = true;
    
    %maxs = Cap(round(GetWord(%info, 0) * $spawnMultiplier), 0, "inf");
    if(%flag && $numAIperSpawnPoint[%this] < %maxs)
        %AIname = AI::helper($spawnIndex[%index], $spawnIndex[%index], "SpawnPoint " @ %this);
    
    //always call back the spawn loop, in case a spot is freed up for a helper to spawn
    schedule("SpawnLoop(" @ %this @ ");", %delay);
}
```

### CURRENT VERSION:
```tribescript
function SpawnLoop(%this)
{
    dbecho($dbechoMode, "SpawnLoop(" @ %this @ ")");
    
    %info = Object::getName(%this);
    
    %mindelay = GetWord(%info, 3);
    %maxdelay = GetWord(%info, 4);
    %diff = %maxdelay - %mindelay;
    %delay = floor(getRandom() * %diff) + %mindelay;
    
    %indexes = "";
    for(%i = 5; GetWord(%info, %i) != -1; %i++)
        %indexes = %indexes @ GetWord(%info, %i) @ " ";
    
    %r = floor(getRandom() * (%i-5));
    %index = GetWord(%indexes, %r);
    
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
        
        // If spawning failed, validate counters immediately to fix potential desync
        if(%AIname == -1 || %AIname == "")
        {
            // Spawning failed - validate counters after a short delay to catch issues
            schedule("ValidateSpawnPointCounters();", 1);
        }
    }
    
    //always call back the spawn loop, in case a spot is freed up for a helper to spawn
    schedule("SpawnLoop(" @ %this @ ");", %delay);
}
```

**DIFFERENCES:**
1. **CURRENT ADDS:** Spawn failure validation - calls `ValidateSpawnPointCounters()` if spawn fails
2. **TRIBES/KOK:** No validation on spawn failure

**IMPACT:** Current version handles spawn failures better

---

## 5. InitSpawnPoints() Function

### TRIBES/KOK VERSION:
```tribescript
function InitSpawnPoints()
{
    dbecho($dbechoMode, "InitSpawnPoints()");
    
    %group = nameToID("MissionGroup\\SpawnPoints");
    
    if(%group != -1)
    {
        for(%i = 0; %i <= Group::objectCount(%group)-1; %i++)
        {
            %this = Group::getObject(%group, %i);
            %info = Object::getName(%this);
            
            $MarkerZone[%this] = ObjectInWhichZone(%this);
            
            if(%info != "")
            {
                $numAIperSpawnPoint[%this] = 0;
                %indexes = "";
                
                for(%z = 5; GetWord(%info, %z) != -1; %z++)
                    %indexes = %indexes @ GetWord(%info, %z) @ " ";
                
                if($console::logmode)
                {
                    echo("===================================================");
                    echo("Spawn Point was initialized, %this = " @ %this);
                    echo("Max spawn per: " @ GetWord(%info, 0));
                    echo("Min radius: " @ GetWord(%info, 1));
                    echo("Max radius: " @ GetWord(%info, 2));
                    echo("Min delay: " @ GetWord(%info, 3));
                    echo("Max delay: " @ GetWord(%info, 4));
                    echo("Spawn indexes: " @ %indexes);
                    echo("Marker Zone ID: " @ $MarkerZone[%this]);
                    echo("===================================================");
                }
                
                SpawnLoop(%this);
            }
        }
    }
}
```

### CURRENT VERSION:
```tribescript
function InitSpawnPoints()
{
    dbecho($dbechoMode, "InitSpawnPoints()");
    
    %group = nameToID("MissionGroup\\SpawnPoints");
    
    if(%group != -1)
    {
        for(%i = 0; %i <= Group::objectCount(%group)-1; %i++)
        {
            %this = Group::getObject(%group, %i);
            %info = Object::getName(%this);
            
            $MarkerZone[%this] = ObjectInWhichZone(%this);
            
            if(%info != "")
            {
                // CRITICAL: Check if a spawn loop is already running for this spawn point
                // This prevents duplicate spawn loops if InitSpawnPoints() is called multiple times
                if($SpawnLoopActive[%this] == "")
                {
                    $numAIperSpawnPoint[%this] = 0;
                    %indexes = "";
                    
                    for(%z = 5; GetWord(%info, %z) != -1; %z++)
                        %indexes = %indexes @ GetWord(%info, %z) @ " ";
                    
                    echo("===================================================");
                    echo("Spawn Point was initialized, %this = " @ %this);
                    echo("Max spawn per: " @ GetWord(%info, 0));
                    echo("Min radius: " @ GetWord(%info, 1));
                    echo("Max radius: " @ GetWord(%info, 2));
                    echo("Min delay: " @ GetWord(%info, 3));
                    echo("Max delay: " @ GetWord(%info, 4));
                    echo("Spawn indexes: " @ %indexes);
                    echo("Marker Zone ID: " @ $MarkerZone[%this]);
                    echo("===================================================");
                    
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
    }
}
```

**DIFFERENCES:**
1. **CURRENT ADDS:** `$SpawnLoopActive` check to prevent duplicate loops
2. **TRIBES/KOK:** No duplicate prevention

**IMPACT:** Current version prevents duplicate spawn loops

---

## 6. ValidateSpawnPointCounters() Function

### TRIBES/KOK VERSION:
**DOES NOT EXIST** - This function doesn't exist in the Tribes/KoK version

### CURRENT VERSION:
```tribescript
function ValidateSpawnPointCounters()
{
    // ... counts bots and corrects counters ...
}
```

**DIFFERENCE:**
- **NEW FUNCTION:** Completely new in current version
- **PURPOSE:** Validates and corrects spawn point counters periodically

**IMPACT:** This is a new addition that didn't exist in Tribes/KoK

---

## Summary: Key Differences Affecting Enemy Spawning

### 🔴 CRITICAL DIFFERENCE - Race Condition:

**TRIBES/KOK:**
- Counter incremented **IMMEDIATELY** in `SpawnAI()` (synchronous)
- No delay between spawn and counter increment
- `SpawnLoop()` always sees updated counter

**CURRENT:**
- Counter incremented **0.15 SECONDS LATER** in `SpawnAIPostCreate()` (asynchronous)
- 0.15 second delay creates race condition window
- `SpawnLoop()` can check counter before it's incremented
- Multiple bots can spawn before counters are updated

### ⚠️ ROOT CAUSE OF OVER-SPAWNING:

The **0.15 second delay** in the current version is likely causing the over-spawning issue:

1. `SpawnLoop()` checks counter: `$numAIperSpawnPoint[%this] = 0` (counter not incremented yet)
2. `SpawnLoop()` spawns bot: `AI::helper()` called
3. `SpawnLoop()` reschedules immediately (doesn't wait for counter)
4. **0.15 seconds later:** `SpawnAIPostCreate()` increments counter to 1
5. **But:** Another `SpawnLoop()` call already checked when counter was 0 and spawned another bot
6. **Result:** Multiple bots spawned before counter is updated

### ✅ RECOMMENDATION:

**Option 1:** Revert to Tribes/KoK synchronous approach - increment counter immediately in `SpawnAI()`

**Option 2:** Increment counter early in `SpawnLoop()` before calling `AI::helper()` (like we tried before)

**Option 3:** Add a spawn-in-progress flag to prevent multiple spawns during the 0.15 second window

The Tribes/KoK version is simpler and doesn't have this race condition because the counter is incremented synchronously.










