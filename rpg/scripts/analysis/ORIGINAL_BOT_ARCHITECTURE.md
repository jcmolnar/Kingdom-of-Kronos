# Original Kingdom of Kronos Bot Architecture

## Overview
This document describes how enemy and town bots worked in the **original** Kingdom of Kronos before the recent changes to use Player objects with ClientIDs.

---

## 1. Enemy Bots (Spawn Point Bots)

### Original Architecture
**Enemy bots were "Drone" objects**, not Player objects. They were created using the Tribes AI engine's native `AI::spawn()` function, which created **Drone** objects that the engine managed internally.
Because we made town bots also Player Objects, we needed a system to separate the two so that we could manage them individually - We are not sure if our approach is the most efficient or needs a rework.

### Key Characteristics:
- **Object Type**: Drone (engine-managed AI object)
- **Client ID**: Retrieved via `AI::getId(%aiName)` - returned a rep ID that could be used like a clientId
- **AI Engine Functions**: All native AI engine functions worked (`AI::directiveRemove()`, `AI::getId()`, etc.)
- **Movement**: Handled entirely by the AI engine
- **Targeting**: Handled entirely by the AI engine
- **No Player Functions**: Could NOT use `Player::mountItem()`, `Player::incItemCount()`, etc.

### Original Spawn Flow (Synchronous):
```torquescript
function SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout)
{
    %retval = createAI(%newName, %aiSpawnPos, %displayName);
    
    if(%retval != -1)
    {
        %aiId = AI::getId(%newName);  // IMMEDIATE - synchronous
        AI::setVar(%newName, iq, 100);
        AI::setVar(%newName, attackMode, $AIattackMode);
        
        if(GetWord(%commandIssuer, 0) == "SpawnPoint")
        {
            storeData(%aiId, "SpawnBotInfo", %commandIssuer);
            
            // COUNTER INCREMENTED IMMEDIATELY (SYNCHRONOUS)
            $numAIperSpawnPoint[GetWord(%commandIssuer, 1)]++;
            UpdateTeam(%aiId);
        }
        
        AI::setWeapons(%newName, %loadout);
        return %newName;
    }
    return -1;
}
```

### Key Differences from Current Version:

| Aspect | Original (Drone) | Current (Player Object) |
|--------|------------------|------------------------|
| **Object Type** | Drone | Player |
| **AI::getId()** | ✅ Always worked | ❌ Returns "Could not find drone" |
| **AI::directiveRemove()** | ✅ Always worked | ❌ Must be bypassed |
| **Counter Increment** | ✅ Synchronous (immediate) | ❌ Asynchronous (0.15s delay) |
| **Player Functions** | ❌ Not available | ✅ Available |
| **Visual Items** | ❌ Not possible | ✅ Possible |
| **Movement** | ✅ Engine-managed | ❌ Custom system required |
| **Team Setting** | ✅ Synchronous | ❌ Asynchronous (timing issues) |

### Why Original System Worked Better:
1. **Synchronous Operations**: Counter was incremented immediately, no race conditions
2. **Engine Support**: All AI engine functions worked natively
3. **No Timing Issues**: `AI::getId()` worked immediately after spawn
4. **Simpler Code**: No need for custom movement, targeting, or workarounds

### Original Death/Cleanup Flow:
```torquescript
function AI::onDroneKilled(%aiName)
{
    %aiId = AI::getId(%aiName);  // Always worked for Drones
    
    if(fetchData(%aiId, "SpawnBotInfo") != "")
    {
        if(GetWord(fetchData(%aiId, "SpawnBotInfo"), 0) == "SpawnPoint")
        {
            // NO VALIDATION - JUST DECREMENTS
            $numAIperSpawnPoint[GetWord(fetchData(%aiId, "SpawnBotInfo"), 1)]--;
        }
    }
}
```

**Key Point**: No validation needed - `AI::getId()` always worked, so cleanup was straightforward.

---

## 2. Town Bots

### Original Architecture
**Town bots were "Item" objects**, not Player objects or Drones. They were created using `newObject("", "Item", ...)` and were essentially static objects in the world.

### Key Characteristics:
- **Object Type**: Item (static world object)
- **Client ID**: Item object ID itself (not a clientId)
- **No AI Control**: No AI behavior, no movement, no targeting
- **No Player Functions**: Could NOT use `Player::mountItem()`, `Player::incItemCount()`, etc.
- **No Visual Items**: Items could not be visually displayed on Item objects
- **Static**: Bots remained in fixed positions, could not move

### Original Spawn Flow:
```torquescript
function InitTownBots()
{
    $TownBotList = "";
    %group = nameToId("MissionGroup/TownBots");
    
    if(%group != -1)
    {
        for(%i = 0; %i <= Group::objectCount(%group) - 1; %i++)
        {
            %object = Group::getObject(%group, %i);
            %name = Object::getName(%object);
            %marker = GatherBotInfo(%object);
            
            // IMMEDIATELY SPAWN AS ITEM OBJECT
            %townbot = newObject("", "Item", $BotInfo[%name, RACE] @ "TownBot", 1, false);
            
            addToSet("MissionCleanup", %townbot);
            GameBase::setMapName(%townbot, $BotInfo[%name, NAME]);
            GameBase::setPosition(%townbot, GameBase::getPosition(%marker));
            GameBase::setRotation(%townbot, GameBase::getRotation(%marker));
            GameBase::setTeam(%townbot, $BotInfo[%name, TEAM]);
            GameBase::playSequence(%townbot, 0, "root");
            %townbot.name = %name;
            
            // Store Item object ID (not clientId)
            $TownBotList = $TownBotList @ %townbot @ " ";
        }
    }
}
```

### Key Differences from Current Version:

| Aspect | Original (Item) | Current (Player Object) |
|--------|----------------|------------------------|
| **Object Type** | Item | Player |
| **Spawn Timing** | ✅ Immediate (all at once) | ❌ Dynamic (zone-based) |
| **AI Behavior** | ❌ None (static) | ⚠️ Must be disabled |
| **Visual Items** | ❌ Not possible | ✅ Possible |
| **Player Functions** | ❌ Not available | ✅ Available |
| **Memory Usage** | ✅ Low (static objects) | ❌ Higher (Player objects) |
| **Performance** | ✅ All loaded at once | ❌ Dynamic loading/unloading |

### Why Original System Worked:
1. **Simple**: No AI behavior to manage
2. **Static**: Bots never moved, no pathfinding needed
3. **Low Overhead**: Item objects are lightweight
4. **No Cleanup Issues**: Item objects don't have complex cleanup needs

### Original Limitations:
1. **No Visual Items**: Items could not be displayed on bots
2. **No Item Functions**: Could not use standard item management
3. **No Movement**: Bots were completely static
4. **No AI Features**: No targeting, no pathfinding, no behavior

---

## 3. Why Changes Were Made

### Motivation for Converting to Player Objects:

#### Enemy Bots:
- **Goal**: Enable visual item display, use Player functions, better integration with RPG systems
- **Reality**: Created compatibility issues because AI engine was designed for Drones, not Player objects

#### Town Bots:
- **Goal**: Enable visual item display, use Player functions, equip armor/weapons
- **Reality**: Required disabling AI behavior, dynamic loading system, more complex cleanup

### Trade-offs:

| Benefit | Cost |
|---------|------|
| ✅ Visual item display | ❌ Compatibility issues with AI engine |
| ✅ Player functions available | ❌ Custom movement/targeting required |
| ✅ Better RPG integration | ❌ Timing/race condition issues |
| ✅ Dynamic town bot loading | ❌ More complex spawn/despawn flow |
| ✅ Consistent architecture | ❌ More cleanup complexity |

---

## 4. Original System Advantages

### What Worked Better in Original:

1. **Synchronous Operations**
   - Counters incremented immediately
   - No race conditions
   - No timing issues

2. **Engine Compatibility**
   - All AI engine functions worked
   - No workarounds needed
   - No "Could not find drone" errors

3. **Simpler Code**
   - Less complexity
   - Fewer edge cases
   - Easier to debug

4. **Reliable Cleanup**
   - `AI::onDroneKilled()` always worked
   - No shell bot issues
   - No client ID reuse problems

5. **Performance**
   - Item objects are lightweight
   - No dynamic loading overhead
   - Simpler memory management

---

## 5. Current System Challenges

### Issues Introduced by Player Object Conversion:

1. **Asynchronous Operations**
   - `GameBase::setTeam()` is asynchronous
   - Counters incremented with delay (race conditions)
   - Timing-dependent behavior

2. **AI Engine Incompatibility**
   - `AI::getId()` doesn't work for Player objects
   - `AI::directiveRemove()` doesn't work
   - Custom implementations required

3. **Shell Bot Formation**
   - Client IDs reused too quickly
   - Data persistence between bots
   - Corrupted bot states

4. **Team Reset Issues**
   - Teams reset to -1
   - Asynchronous team setting
   - Multiple team setting attempts needed

5. **Spawn Counter Desynchronization**
   - Counters incremented asynchronously
   - Race conditions in spawn flow
   - Counters can become incorrect

6. **Complex Cleanup**
   - More data to clear
   - Timing-dependent cleanup
   - Multiple cleanup points needed

---

## 6. Recommendations

### Option 1: Revert to Original Architecture
- **Pros**: Simpler, more reliable, no compatibility issues
- **Cons**: Lose visual item display, Player functions, RPG integration

### Option 2: Hybrid Approach
- **Enemy Bots**: Keep as Player objects, but fix timing issues
- **Town Bots**: Consider reverting to Item objects (they don't need AI behavior)

### Option 3: Fix Current System
- Make counter increments synchronous
- Fix team setting timing
- Improve cleanup reliability
- Add more validation

### Option 4: Custom AI System
- Build custom AI system for Player objects
- Don't rely on engine AI functions
- Full control over behavior

---

## 7. Key Takeaways

1. **Original enemy bots were Drones** - Engine-managed, all functions worked
2. **Original town bots were Items** - Static objects, no AI behavior
3. **Original system was simpler** - Synchronous operations, no timing issues
4. **Current system has benefits** - Visual items, Player functions, RPG integration
5. **Current system has costs** - Compatibility issues, timing problems, complexity

The fundamental question is: **Are the benefits of Player objects worth the complexity and issues they introduce?**

For enemy bots, the answer may be "yes" if we can fix the timing issues.
For town bots, the answer may be "no" - Item objects might be sufficient if visual items aren't critical.

---

## 8. Historical Context

Based on the comparison documents:
- **Tribes/KoK Version**: Used synchronous counter increments, Drones for enemy bots, Items for town bots
- **Current Version**: Uses asynchronous operations, Player objects for both
- **Key Change**: The 0.15 second delay in `SpawnAIPostCreate()` introduced race conditions that didn't exist in the original

The original system worked because it was designed for the engine's native capabilities. The current system requires working around engine limitations, which introduces complexity and timing issues.

