# Tribes Engine Fundamentals - Comprehensive Reference

## Repository Structure
Based on [TribesRebirth](https://github.com/AlexHuck/TribesRebirth/tree/master):
- **darkstar/**: Core engine code (C++)
- **program/**: Game-specific logic
- **inc/**: Include files and headers

## Core Architecture Concepts

### 1. Object Hierarchy and Inheritance

```
SimObject (base)
  └── NetObject (networked objects)
      └── GameBase (game entities)
          ├── Player (player characters)
          ├── Item (items, weapons, deployables)
          ├── Projectile (bullets, grenades)
          └── StaticShape (static world objects)
```

**Key Concepts:**
- **SimObject**: Base class for all simulation objects
- **NetObject**: Objects that exist on network (have clientId/repId)
- **GameBase**: Game-specific entities with position, rotation, etc.
- **Player**: Represents both human players and AI bots

### 2. Client ID System

**Critical Understanding:**
- **Client ID**: Unique identifier for networked entities
- **Rep ID**: Representation ID (used internally by engine)
- **Player Object**: The actual Player instance (can be used as clientId for bots)
- **Client Object**: Network client connection (for human players)

**For AI Bots:**
- `Player::getClient(%playerObj)` returns `-1` for bots
- Bots use their `Player` object directly as their "client ID"
- `AI::getId(%aiName)` returns the rep ID (can be used like clientId)
- `AI::getClientIdFromName(%aiName)` returns clientId if available

### 3. Bot/AI System Architecture

#### Bot Types:
1. **Drones** (Legacy): Old bot system, uses `AI::onDroneKilled()` callback
2. **Player Objects** (Modern): Bots created as Player objects with AI control

#### Key Functions:
- `AI::spawn(%name, %displayName, %pos, %team)`: Spawns a bot
- `AI::delete(%name)`: Removes bot from engine registry
- `AI::onDroneKilled(%aiName)`: Callback when bot dies (for Drones)
- `Player::Kill(%clientId)`: Kills a player/bot, triggers death handlers
- `Player::onKilled(%this)`: Death handler (called for both players and bots)

#### Bot Lifecycle:
1. **Spawn**: `AI::spawn()` → Creates Player object → `createAI()` → `createAIPostSpawn()`
2. **Active**: Bot exists in world, has clientId, tracked in arrays
3. **Death**: `Player::Kill()` → `Player::onKilled()` → `AI::onDroneKilled()` (if applicable)
4. **Cleanup**: Data cleared, object deleted, clientId freed

### 4. Data Storage Systems

#### TorqueScript Arrays:
- `$GlobalArray[key]`: Global associative array
- `$Array[%id, "field"]`: Multi-dimensional array
- `storeData(%id, "field", %value)`: Stores data (may use multiple arrays)
- `fetchData(%id, "field")`: Retrieves data (checks multiple sources)

#### Data Storage Patterns:
```cpp
// Multiple storage locations for same data:
$ClientData[%id, "BotInfoAiName"]     // Backwards compatibility
$TownBotData[%id, "BotInfoAiName"]    // Town bot specific
$EnemyBotData[%id, "BotInfoAiName"]  // Enemy bot specific
$BotInfoAiName[%id]                   // Direct array (fast lookup)
```

**Why Multiple Arrays?**
- Performance: Direct array access is faster
- Compatibility: Legacy code may use different arrays
- Organization: Separate arrays for different bot types

### 5. Network Synchronization (Torque Engine Architecture)

#### Ghost Manager:
- **Purpose**: Tracks which objects are "ghosted" (replicated) to clients
- **Area of Interest**: Only sends updates for objects in client's visible area
- **Object Lifecycle**: Handles object creation/destruction on network
- **Efficiency**: Reduces bandwidth by only sending relevant updates
- **Implementation**: C++ engine code manages ghosting automatically

#### Move Manager:
- **Purpose**: Processes player input and movement synchronization
- **Client Prediction**: Clients predict movement locally for responsiveness
- **Server Authority**: Server validates and corrects client predictions
- **Lag Compensation**: Handles network latency gracefully
- **Input Processing**: Processes keyboard/mouse input, applies to player movement

#### Networking Model (Tribes-Specific):
- **Supports**: Up to 128 players in multiplayer
- **Data Delivery**: Multiple delivery methods (reliable, fast, etc.)
- **Partial Updates**: Only sends changed object state, not full state
- **Packet Notification**: Protocol for confirming packet delivery
- **Eventual Consistency**: Client state eventually matches server state

**Key Insight**: Server is authoritative. Clients receive updates and predict locally, with server corrections.

### 6. Scripting System (TorqueScript)

#### Language Features:
- **Dynamic Typing**: Variables don't need type declarations (`%var = 5;` or `%var = "string";`)
- **String Concatenation**: `@` operator (`%result = %str1 @ %str2;`)
- **Function Calls**: `Function::name(%param1, %param2)`
- **Namespace**: `::` separator (e.g., `AI::spawn`, `Player::Kill`)
- **Arrays**: Associative arrays with string keys (`$Array[key] = value;`)
- **Variable Prefixes**: `%` for local, `$` for global
- **Case Sensitivity**: Variable names are case-sensitive
- **No Type Checking**: Can assign any type to any variable

#### Common Patterns:
```cpp
// Function definition
function MyFunction(%param1, %param2)
{
    // Function body
    return %result;
}

// Namespace function
function Namespace::Function(%param)
{
    // Namespace function body
}

// Scheduling (delayed execution in seconds)
schedule("FunctionName(" @ %param @ ");", 0.05);

// String manipulation
%str = "Hello";
%len = String::len(%str);
%sub = String::getSubStr(%str, 0, 5);
%pos = String::findSubStr(%str, "ell");

// Array operations
$Array["key"] = "value";
%val = $Array["key"];
$Array["key"] = "";  // Clear/delete

// Multi-dimensional arrays
$Array[%id, "field"] = "value";
%val = $Array[%id, "field"];

// Word operations (space-separated strings)
%word = GetWord(%string, 0);  // Get first word
%count = GetWordCount(%string);  // Count words
```

#### Torque Engine Integration:
- **Engine Callbacks**: C++ engine calls script functions (e.g., `Player::onKilled`)
- **Script to Engine**: Script calls engine functions (e.g., `AI::spawn`, `GameBase::setTeam`)
- **Console Commands**: Script functions can be called from console
- **Hot Reloading**: Scripts can be reloaded without restarting server

### 7. Event System

#### Callbacks:
- `Player::onKilled(%this)`: Called when player/bot dies
- `AI::onDroneKilled(%aiName)`: Called when drone dies
- `Item::onCollision(%this, %object)`: Called when item collides
- `Zone::DoEnter(%object)`: Called when object enters zone
- `Zone::DoExit(%object)`: Called when object exits zone

#### Event Flow:
1. Engine event occurs (death, collision, etc.)
2. Engine calls script callback
3. Script processes event
4. Script may trigger additional events

### 8. Zone System

#### Zone Types:
- **PROTECTED**: Safe zones (towns)
- **DUNGEON**: Dangerous zones (enemy spawns)
- **COLLECT**: Collection zones
- **DISPLAY**: Display zones
- **CONTROL**: Control zones

#### Zone Functions:
- `Zone::getDesc(%zoneId)`: Get zone description
- `Zone::getType(%zoneId)`: Get zone type
- `Zone::DoEnter(%object)`: Handle zone entry
- `Zone::DoExit(%object)`: Handle zone exit
- `UpdateZone(%object)`: Periodic zone update check

### 9. Team System

#### Team Assignment:
- **Team 0**: Citizen (friendly, town bots)
- **Team 1-8**: Enemy teams (Ogres, Pigmen, Undead, Demons, Minotaur, Aliens, Seals)
- `GameBase::setTeam(%object, %team)`: Sets team
- `fetchData(%id, "botTeam")`: Gets stored team

#### Team Behavior:
- Same team: Don't attack each other
- Different teams: Can attack
- Team affects name colors, targeting, etc.

### 10. Bot Spawning Patterns

#### Town Bot Spawning:
1. Player enters protected zone
2. `SpawnZoneBots(%zoneIndex)` called
3. For each bot in zone:
   - `AI::spawn("TownBot_" @ %botName, %displayName, %pos, 0)`
   - Wait for clientId
   - `createAIPostSpawn(%aiName, %name)`
   - Set team to 0 (Citizen)
   - Track in `$TownBotSpawned[%botName] = %clientId`

#### Enemy Bot Spawning:
1. Spawn point activated
2. `SpawnAI(%newName, %displayName, %pos, %commandIssuer)` called
3. `createAI(%newName, %armor, %group)` creates Player object
4. `createAIPostSpawn()` processes (skips town bot logic for enemies)
5. Set team based on zone/enemy type
6. Track in `$EnemyBotData[%clientId, "SpawnBotInfo"]`

### 11. Bot Cleanup Patterns

#### Critical Cleanup Points:
1. **Player::onKilled()**: First cleanup point (for both enemy and town bots)
2. **AI::onDroneKilled()**: Second cleanup point (for Drones, may be called after Player::onKilled)
3. **DespawnZoneBots()**: Manual cleanup when zone becomes empty
4. **#deletebot command**: Admin cleanup

#### Cleanup Checklist:
- [ ] Clear `storeData()` fields
- [ ] Clear `$TownBotData[]` arrays (town bots)
- [ ] Clear `$EnemyBotData[]` arrays (enemy bots)
- [ ] Clear `$ClientData[]` arrays (backwards compatibility)
- [ ] Clear `$BotInfoAiName[]` direct array
- [ ] Clear `$TownBotSpawned[]` tracking
- [ ] Remove from `$TownBotList`
- [ ] Clear belt cached lists
- [ ] Clear AI movement/behavior data
- [ ] Remove from bot groups
- [ ] Clear aiNumTable entries
- [ ] Clear pet data
- [ ] Clear zone data
- [ ] Call `AI::delete(%aiName)` to free engine registry
- [ ] Decrement spawn counters
- [ ] Decrement bot tracking counters

### 12. Timing and Scheduling

#### Critical Timing Issues:
- **Bot Spawn**: `AI::spawn()` may succeed but clientId not immediately available
- **Bot Death**: `Player::Kill()` triggers callbacks, but object may not be deleted immediately
- **AI::delete()**: Must be called AFTER bot is fully dead (scheduled with delay)
- **Data Clearing**: `Player::onKilled()` may clear data before `AI::onDroneKilled()` reads it

#### Scheduling Patterns:
```cpp
// Immediate execution
Function(%param);

// Delayed execution (seconds)
schedule("Function(" @ %param @ ");", 0.05);

// Conditional delayed execution
if(%condition)
    schedule("Function(" @ %param @ ");", %delay);
```

### 13. Error Handling Patterns

#### Common Errors:
- `"Could not find drone X"`: Bot doesn't exist in engine registry
- `"An AI named X already exists!"`: Bot name not freed from registry
- `"Could not get client ID for bot"`: Timing issue, bot not registered yet
- `"Directive #X not found"`: AI directive doesn't exist

#### Error Prevention:
- Check if bot exists before operations
- Use scheduled calls for cleanup
- Validate clientId before use
- Check arrays before accessing
- Use fallback checks when data might be cleared

### 14. Performance Considerations

#### Array Access:
- Direct array: `$Array[key]` (fastest)
- Multi-dimensional: `$Array[id, "field"]` (fast)
- `fetchData()`: Checks multiple sources (slower, but safer)

#### Optimization Patterns:
- Cache frequently accessed data
- Use direct arrays for hot paths
- Minimize `fetchData()` calls in loops
- Batch operations when possible

### 15. Common Pitfalls

1. **Timing Issues**: Bot data cleared before all handlers run
2. **Double Cleanup**: Multiple handlers cleaning same data
3. **Stale Data**: Old bot data persisting after death
4. **Team Overwrites**: Team set correctly but then overwritten
5. **Name Conflicts**: Bot name not freed, causing spawn failures
6. **Array Inconsistency**: Data in one array but not others
7. **Missing Cleanup**: Some data not cleared, causing memory leaks

## Key Takeaways for Kingdom of Kronos Development

1. **Always check if bot exists** before operations
2. **Use scheduled calls** for cleanup to avoid timing issues
3. **Clear all data arrays** consistently (not just one)
4. **Extract bot names correctly** (remove "TownBot_" prefix when needed)
5. **Track bot types explicitly** (town vs enemy) using BotInfoAiName prefix
6. **Decrement counters** before clearing data
7. **Call AI::delete()** with delay after bot death
8. **Validate clientId** before array access
9. **Use fallback checks** when data might be cleared prematurely
10. **Separate town and enemy bot logic** completely

### 16. Torque Game Engine Foundation

#### Engine History:
- **Original**: Developed by Dynamix for Tribes 2
- **Base**: Built on Starsiege: Tribes engine
- **Language**: C++ core with TorqueScript scripting layer
- **Architecture**: Client-server model with authoritative server

#### Core Engine Components:
1. **Rendering Engine**: Handles 3D graphics, terrain, lighting
2. **Physics Engine**: Collision detection, movement, gravity
3. **Networking Layer**: Ghost manager, move manager, packet handling
4. **Scripting System**: TorqueScript interpreter and integration
5. **Resource Management**: Texture, model, sound loading
6. **Input System**: Keyboard, mouse, joystick handling

#### Engine-Script Interface:
- **Engine → Script**: Callbacks (e.g., `Player::onKilled`, `AI::onDroneKilled`)
- **Script → Engine**: Function calls (e.g., `AI::spawn`, `GameBase::setTeam`)
- **Data Sharing**: Global arrays accessible from both C++ and script
- **Object Access**: Script can access engine objects via IDs

### 17. Advanced Bot Management Patterns

#### Bot State Machine:
```
IDLE → SPAWNING → ACTIVE → DYING → DEAD → CLEANED
```

#### State Transitions:
- **IDLE → SPAWNING**: `AI::spawn()` called
- **SPAWNING → ACTIVE**: ClientId obtained, post-spawn complete
- **ACTIVE → DYING**: `Player::Kill()` called
- **DYING → DEAD**: `Player::onKilled()` completes
- **DEAD → CLEANED**: All cleanup handlers finish

#### Bot Data Lifecycle:
1. **Creation**: Data initialized in `createAIPostSpawn()`
2. **Active**: Data updated during gameplay
3. **Death**: Data read in `Player::onKilled()` and `AI::onDroneKilled()`
4. **Cleanup**: Data cleared in cleanup handlers
5. **Reuse**: ClientId may be reused for new bot

### 18. Memory Management Patterns

#### Array Management:
- **Global Arrays**: Persist across script reloads
- **Local Arrays**: Cleared when function exits
- **Multi-dimensional**: Can grow dynamically
- **Clearing**: Set to `""` or `-1` to clear

#### Object Lifecycle:
- **Creation**: `new ObjectType()` or engine function
- **Registration**: Objects registered in engine
- **Active**: Object exists in world
- **Deletion**: `deleteObject(%obj)` or engine cleanup
- **Cleanup**: All references must be cleared

#### Best Practices:
- Clear arrays when objects are deleted
- Use consistent clearing patterns
- Check for existence before access
- Avoid circular references
- Clear data in reverse order of creation

### 19. Debugging and Troubleshooting

#### Common Debug Techniques:
```cpp
// Echo debugging
echo("DEBUG: Variable = " @ %var);

// Conditional debugging
if($DebugMode)
    echo("DEBUG: " @ %message);

// Error logging
echo("ERROR: " @ %errorMessage);

// Warning logging
echo("WARNING: " @ %warningMessage);
```

#### Debugging Bot Issues:
1. **Check if bot exists**: `Client::getOwnedObject(%clientId)`
2. **Verify clientId**: `Client::getName(%clientId)`
3. **Check arrays**: `$Array[%id, "field"]`
4. **Trace function calls**: Add echo statements
5. **Check timing**: Use scheduled delays for debugging

#### Error Patterns:
- **"Could not find"**: Object doesn't exist or wrong ID
- **"already exists"**: Object not cleaned up properly
- **"not found"**: Array key doesn't exist
- **Timing errors**: Need scheduled delays

## References

- [TribesRebirth Repository](https://github.com/AlexHuck/TribesRebirth/tree/master)
- [Tribes Networking Architecture](https://snapnet.dev/blog/netcode-architectures-part-4-tribes/)
- [Torque Game Engine (Wikipedia)](https://en.wikipedia.org/wiki/Torque_%28game_engine%29)
- [Tribes Networking Model PDF](https://www.gamedevs.org/uploads/tribes-networking-model.pdf)
- Original Tribes 1.11 Documentation
- Kingdom of Kronos Codebase Patterns

---

**Last Updated**: Based on comprehensive review of TribesRebirth repository, Torque engine architecture, and Kingdom of Kronos codebase patterns.

