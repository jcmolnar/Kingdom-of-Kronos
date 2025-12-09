# Tribes Scripting Comprehensive Reference

## Overview
Based on [TribesRebirth repository](https://github.com/AlexHuck/TribesRebirth/tree/master) and Tribes 1.11 scripting patterns, this document covers all aspects of Tribes/TorqueScript scripting beyond AI.

## Table of Contents
1. [Script File Organization](#script-file-organization)
2. [Core Scripting Patterns](#core-scripting-patterns)
3. [Mission Scripts](#mission-scripts)
4. [Server Scripts](#server-scripts)
5. [Client Scripts](#client-scripts)
6. [UI/HUD Scripts](#ui-hud-scripts)
7. [Weapon & Item Scripts](#weapon--item-scripts)
8. [Zone & Trigger Scripts](#zone--trigger-scripts)
9. [Game Mode Scripts](#game-mode-scripts)
10. [Console Commands](#console-commands)
11. [Event Handlers](#event-handlers)
12. [Network Scripting](#network-scripting)
13. [String Manipulation](#string-manipulation)
14. [Math & Vector Operations](#math--vector-operations)
15. [Object Management](#object-management)
16. [File I/O](#file-io)
17. [Scheduling & Timing](#scheduling--timing)
18. [Advanced Patterns](#advanced-patterns)

---

## Script File Organization

### Typical Directory Structure
```
program/
  ├── common/          # Shared utility functions
  ├── server/          # Server-side scripts
  ├── client/          # Client-side scripts
  ├── mission/         # Mission-specific scripts
  ├── ui/              # User interface scripts
  └── weapons/         # Weapon/item scripts
```

### Common File Types
- **.cs**: TorqueScript source files
- **.mis**: Mission files (contain script references)
- **.gui**: GUI definition files
- **.dts**: 3D model files
- **.dml**: Material files

### Script Loading Order
1. **common.cs**: Base utilities loaded first
2. **server.cs**: Server initialization
3. **client.cs**: Client initialization
4. **mission.cs**: Mission-specific setup
5. **Specialized scripts**: Loaded as needed

---

## Core Scripting Patterns

### Function Definitions

#### Basic Function
```cpp
function MyFunction(%param1, %param2)
{
    // Function body
    return %result;
}
```

#### Namespace Function
```cpp
function Namespace::Function(%param)
{
    // Namespace function
}
```

#### Method (Object Function)
```cpp
function ObjectType::Method(%this, %param)
{
    // %this is the object instance
}
```

### Variable Types

#### Local Variables
```cpp
%localVar = 10;
%stringVar = "Hello";
%boolVar = true;
```

#### Global Variables
```cpp
$GlobalVar = 100;
$GlobalString = "World";
```

#### Arrays
```cpp
$Array["key"] = "value";
$MultiArray[%id, "field"] = "data";
```

### Control Flow

#### Conditionals
```cpp
if(%condition)
{
    // Code
}
else if(%otherCondition)
{
    // Code
}
else
{
    // Code
}
```

#### Loops
```cpp
// For loop
for(%i = 0; %i < 10; %i++)
{
    // Code
}

// While loop
%i = 0;
while(%i < 10)
{
    // Code
    %i++;
}

// Word loop (iterate through space-separated string)
for(%i = 0; (%word = GetWord(%string, %i)) != -1; %i++)
{
    // Process %word
}
```

#### Switch Statement
```cpp
switch(%value)
{
    case 1:
        // Code
    case 2:
        // Code
    default:
        // Code
}
```

---

## Mission Scripts

### Mission Initialization

#### onMissionLoaded()
```cpp
function onMissionLoaded(%missionName)
{
    // Called when mission loads
    echo("Mission loaded: " @ %missionName);
    
    // Initialize mission-specific variables
    $MissionActive = true;
    $MissionStartTime = getSimTime();
}
```

#### onMissionStart()
```cpp
function onMissionStart(%missionName)
{
    // Called when mission starts
    // Spawn players, initialize objectives, etc.
}
```

#### onMissionEnded()
```cpp
function onMissionEnded(%missionName)
{
    // Called when mission ends
    // Cleanup, save scores, etc.
    $MissionActive = false;
}
```

### Mission Objectives

#### Objective Tracking
```cpp
function Mission::AddObjective(%objectiveName, %description)
{
    $Objective[%objectiveName, "description"] = %description;
    $Objective[%objectiveName, "completed"] = false;
}

function Mission::CompleteObjective(%objectiveName)
{
    $Objective[%objectiveName, "completed"] = true;
    Mission::UpdateObjectiveDisplay();
}
```

---

## Server Scripts

### Server Initialization

#### onServerCreated()
```cpp
function onServerCreated()
{
    // Called when server is created
    echo("Server created");
    
    // Initialize server variables
    $Server::MaxPlayers = 32;
    $Server::GameMode = "CTF";
}
```

#### onServerDestroyed()
```cpp
function onServerDestroyed()
{
    // Called when server shuts down
    // Cleanup, save data, etc.
}
```

### Player Management

#### onClientConnect()
```cpp
function onClientConnect(%clientId, %name)
{
    // Called when client connects
    echo("Client connected: " @ %name @ " (" @ %clientId @ ")");
    
    // Initialize player data
    storeData(%clientId, "name", %name);
    storeData(%clientId, "joinTime", getSimTime());
}
```

#### onClientDisconnect()
```cpp
function onClientDisconnect(%clientId)
{
    // Called when client disconnects
    %name = Client::getName(%clientId);
    echo("Client disconnected: " @ %name);
    
    // Cleanup player data
    SaveCharacter(%clientId);
    ClearVariables(%clientId);
}
```

#### onClientEnterGame()
```cpp
function onClientEnterGame(%clientId)
{
    // Called when client enters game
    // Spawn player, load character, etc.
    LoadCharacter(%clientId);
    SpawnPlayer(%clientId);
}
```

### Server Commands

#### Server Console Commands
```cpp
function ServerCmdMyCommand(%clientId, %param1, %param2)
{
    // Called from client via remoteEval
    // Process command
    echo("Command received from " @ Client::getName(%clientId));
}
```

---

## Client Scripts

### Client Initialization

#### onClientConnect()
```cpp
function onClientConnect(%clientId)
{
    // Called when client connects to server
    // Initialize client-side variables
    $Client::Connected = true;
}
```

#### onClientDisconnect()
```cpp
function onClientDisconnect(%clientId)
{
    // Called when client disconnects
    // Cleanup client-side data
    $Client::Connected = false;
}
```

### Client Commands

#### Client Console Commands
```cpp
function MyClientCommand(%param)
{
    // Client-side command
    // Can call server via remoteEval
    remoteEval(2048, "ServerCmdMyCommand", %param);
}
```

#### Remote Evaluation
```cpp
// Client to Server
remoteEval(%clientId, "ServerCmdFunction", %param1, %param2);

// Server to Client
remoteEval(%clientId, "ClientCmdFunction", %param1, %param2);
```

---

## UI/HUD Scripts

### GUI Creation

#### Creating GUI Elements
```cpp
function CreateMyGUI()
{
    // Create GUI control
    %gui = new GuiControl(MyGUI)
    {
        profile = "GuiDefaultProfile";
        horizSizing = "width";
        vertSizing = "height";
        position = "0 0";
        extent = "640 480";
    };
    
    // Add to canvas
    Canvas.add(%gui);
}
```

### HUD Elements

#### HUD Updates
```cpp
function updateHUD()
{
    // Update HUD elements
    %health = fetchData(%clientId, "HP");
    %mana = fetchData(%clientId, "MANA");
    
    // Update GUI controls
    HealthBar.setValue(%health);
    ManaBar.setValue(%mana);
}

// Schedule periodic updates
schedule("updateHUD();", 0.1);
```

### Menu Systems

#### Menu Creation
```cpp
function ShowMenu(%menuType)
{
    // Show menu based on type
    switch(%menuType)
    {
        case "inventory":
            Canvas.pushDialog(InventoryMenu);
        case "character":
            Canvas.pushDialog(CharacterMenu);
    }
}
```

---

## Weapon & Item Scripts

### Weapon Functions

#### Weapon Firing
```cpp
function Weapon::onFire(%this, %clientId)
{
    // Called when weapon fires
    %ammo = fetchData(%clientId, "ammo");
    if(%ammo > 0)
    {
        // Fire weapon
        storeData(%clientId, "ammo", %ammo - 1);
        Weapon::CreateProjectile(%this, %clientId);
    }
}
```

#### Weapon Reload
```cpp
function Weapon::onReload(%this, %clientId)
{
    // Called when weapon reloads
    %ammo = fetchData(%clientId, "ammo");
    %maxAmmo = fetchData(%clientId, "maxAmmo");
    
    if(%ammo < %maxAmmo)
    {
        storeData(%clientId, "ammo", %maxAmmo);
    }
}
```

### Item Functions

#### Item Pickup
```cpp
function Item::onPickup(%this, %clientId)
{
    // Called when item is picked up
    %itemType = %this.itemType;
    
    // Add to inventory
    GiveThisStuff(%clientId, %itemType @ " 1");
    
    // Delete item object
    deleteObject(%this);
}
```

#### Item Use
```cpp
function Item::onUse(%this, %clientId)
{
    // Called when item is used
    %itemType = %this.itemType;
    
    // Apply item effects
    if(%itemType == "HealthPotion")
    {
        %health = fetchData(%clientId, "HP");
        %maxHealth = fetchData(%clientId, "MaxHP");
        storeData(%clientId, "HP", %health + 50);
    }
}
```

---

## Zone & Trigger Scripts

### Zone Entry/Exit

#### Zone Entry
```cpp
function Zone::DoEnter(%object)
{
    // Called when object enters zone
    %zoneId = fetchData(%object, "zone");
    %zoneType = Zone::getType(%zoneId);
    
    if(%zoneType == "PROTECTED")
    {
        // Protected zone logic
        SpawnZoneBots(%zoneId);
    }
}
```

#### Zone Exit
```cpp
function Zone::DoExit(%object)
{
    // Called when object exits zone
    %zoneId = fetchData(%object, "zone");
    
    // Cleanup zone-specific data
    DespawnZoneBots(%zoneId);
}
```

### Trigger Scripts

#### Trigger Activation
```cpp
function Trigger::onEnter(%this, %object)
{
    // Called when object enters trigger
    %triggerType = %this.triggerType;
    
    switch(%triggerType)
    {
        case "teleport":
            TeleportPlayer(%object, %this.destination);
        case "spawn":
            SpawnEnemy(%this.spawnPoint);
    }
}
```

---

## Game Mode Scripts

### Capture the Flag (CTF)

#### Flag Capture
```cpp
function CTF::CaptureFlag(%clientId, %flagId)
{
    // Called when flag is captured
    %team = fetchData(%clientId, "team");
    
    // Award points
    $TeamScore[%team]++;
    
    // Return flag
    CTF::ReturnFlag(%flagId);
}
```

### Team Deathmatch

#### Score Tracking
```cpp
function TDM::OnKill(%killerId, %victimId)
{
    // Called when player kills another
    %killerTeam = fetchData(%killerId, "team");
    %victimTeam = fetchData(%victimId, "team");
    
    if(%killerTeam != %victimTeam)
    {
        $TeamScore[%killerTeam]++;
    }
}
```

---

## Console Commands

### Command Registration

#### Registering Commands
```cpp
// Command is automatically registered when function is defined
function MyCommand(%param1, %param2)
{
    // Command implementation
    echo("Command executed with params: " @ %param1 @ ", " @ %param2);
}
```

### Admin Commands

#### Admin Level Check
```cpp
function AdminCommand(%clientId, %param)
{
    %adminLevel = floor(%clientId.adminLevel);
    if(%adminLevel >= 2)
    {
        // Execute admin command
        echo("Admin command executed by " @ Client::getName(%clientId));
    }
    else
    {
        Client::sendMessage(%clientId, 0, "Insufficient permissions");
    }
}
```

---

## Event Handlers

### Player Events

#### Player Damage
```cpp
function Player::onDamage(%this, %type, %value, %pos, %vec, %mom, %vertPos, %rweapon, %object, %weapon, %preCalcMiss)
{
    // Called when player takes damage
    %clientId = Player::getClient(%this);
    
    // Apply damage
    %health = fetchData(%clientId, "HP");
    storeData(%clientId, "HP", %health - %value);
}
```

#### Player Killed
```cpp
function Player::onKilled(%this)
{
    // Called when player dies
    %clientId = Player::getClient(%this);
    
    // Handle death
    // Drop items, respawn, etc.
}
```

### Object Events

#### Object Collision
```cpp
function Object::onCollision(%this, %object)
{
    // Called when objects collide
    // Handle collision logic
}
```

---

## Network Scripting

### Remote Function Calls

#### Client to Server
```cpp
// Client side
remoteEval(2048, "ServerCmdFunction", %param1, %param2);

// Server side
function ServerCmdFunction(%clientId, %param1, %param2)
{
    // Process on server
}
```

#### Server to Client
```cpp
// Server side
remoteEval(%clientId, "ClientCmdFunction", %param1, %param2);

// Client side
function ClientCmdFunction(%param1, %param2)
{
    // Process on client
}
```

### Data Synchronization

#### Syncing Variables
```cpp
// Server updates client
remoteEval(%clientId, "ClientCmdUpdateHealth", %health);

// Client receives update
function ClientCmdUpdateHealth(%health)
{
    $ClientHealth = %health;
    updateHUD();
}
```

---

## String Manipulation

### String Functions

#### Basic Operations
```cpp
// Concatenation
%str = "Hello" @ " " @ "World";

// Length
%len = String::len(%str);

// Substring
%sub = String::getSubStr(%str, 0, 5);  // "Hello"

// Find substring
%pos = String::findSubStr(%str, "World");  // Returns position or -1

// Replace
%new = String::replace(%str, "World", "Universe");
```

#### Word Operations
```cpp
// Get word
%word = GetWord(%string, 0);  // First word

// Get word count
%count = GetWordCount(%string);

// Iterate words
for(%i = 0; (%word = GetWord(%string, %i)) != -1; %i++)
{
    // Process %word
}
```

---

## Math & Vector Operations

### Math Functions

#### Basic Math
```cpp
// Absolute value
%abs = mAbs(%value);

// Minimum/Maximum
%min = getMin(%a, %b);
%max = getMax(%a, %b);

// Clamp
%clamped = Cap(%value, %min, %max);

// Random
%random = getRandom(%min, %max);
```

### Vector Operations

#### Vector Creation
```cpp
// Create vector
%pos = "100 200 300";  // X Y Z

// Get components
%x = getWord(%pos, 0);
%y = getWord(%pos, 1);
%z = getWord(%pos, 2);
```

#### Vector Math
```cpp
// Distance
%distance = Vector::getDistance(%pos1, %pos2);

// Length
%length = Vector::len(%vec);

// Normalize
%normalized = Vector::normalize(%vec);

// Dot product
%dot = Vector::dot(%vec1, %vec2);

// Cross product
%cross = Vector::cross(%vec1, %vec2);
```

---

## Object Management

### Object Creation

#### Creating Objects
```cpp
// Create new object
%obj = new SimObject(MyObject)
{
    data = "value";
};

// Create with parameters
%obj = new Player(MyPlayer)
{
    position = "0 0 0";
    rotation = "0 0 0 1";
};
```

### Object Access

#### Getting Objects
```cpp
// Get object by name
%obj = nameToId("MyObject");

// Get client's player object
%playerObj = Client::getOwnedObject(%clientId);

// Get control object
%controlObj = Client::getControlObject(%clientId);
```

### Object Deletion

#### Deleting Objects
```cpp
// Delete object
deleteObject(%obj);

// Check if object exists
if(%obj != -1 && %obj != "")
{
    // Object exists
}
```

---

## File I/O

### File Operations

#### Reading Files
```cpp
// Execute script file
exec("path/to/file.cs");

// Read file line by line
%file = openFile("path/to/file.txt", "read");
while(!isEOF(%file))
{
    %line = readLine(%file);
    // Process line
}
closeFile(%file);
```

#### Writing Files
```cpp
// Write to file
%file = openFile("path/to/file.txt", "write");
writeLine(%file, "Line 1");
writeLine(%file, "Line 2");
closeFile(%file);
```

### Export/Import

#### Exporting Data
```cpp
function ExportData(%filename)
{
    %file = openFile(%filename, "write");
    
    // Export variables
    writeLine(%file, "$GlobalVar = \"" @ $GlobalVar @ "\";");
    
    closeFile(%file);
}
```

#### Importing Data
```cpp
function ImportData(%filename)
{
    // Execute exported file
    exec(%filename);
}
```

---

## Scheduling & Timing

### Scheduling Functions

#### Basic Scheduling
```cpp
// Schedule function call (seconds)
schedule("MyFunction(" @ %param @ ");", 1.0);

// Cancel schedule
cancel(%scheduleId);
```

#### Conditional Scheduling
```cpp
if(%condition)
{
    schedule("MyFunction();", 0.5);
}
```

### Time Functions

#### Getting Time
```cpp
// Simulation time
%time = getSimTime();

// Real time
%realTime = getRealTime();
```

---

## Advanced Patterns

### State Machines

#### State Management
```cpp
function StateMachine::SetState(%object, %newState)
{
    %oldState = fetchData(%object, "state");
    storeData(%object, "state", %newState);
    
    // Handle state transition
    StateMachine::OnStateChange(%object, %oldState, %newState);
}
```

### Event Systems

#### Event Registration
```cpp
function Event::Register(%eventName, %handler)
{
    if($EventHandlers[%eventName] == "")
        $EventHandlers[%eventName] = %handler;
    else
        $EventHandlers[%eventName] = $EventHandlers[%eventName] @ " " @ %handler;
}

function Event::Trigger(%eventName, %param1, %param2)
{
    for(%i = 0; (%handler = GetWord($EventHandlers[%eventName], %i)) != -1; %i++)
    {
        call(%handler, %param1, %param2);
    }
}
```

### Module System

#### Module Loading
```cpp
function Module::Load(%moduleName)
{
    if($ModuleLoaded[%moduleName])
        return;
    
    exec("modules/" @ %moduleName @ ".cs");
    $ModuleLoaded[%moduleName] = true;
}
```

---

## Best Practices

### Code Organization
1. **Use namespaces** for related functions
2. **Group related functions** in same file
3. **Comment complex logic** thoroughly
4. **Use descriptive names** for variables and functions
5. **Validate inputs** before processing

### Performance
1. **Cache frequently accessed data**
2. **Minimize string operations** in loops
3. **Use direct array access** for hot paths
4. **Batch operations** when possible
5. **Avoid unnecessary function calls**

### Error Handling
1. **Check if objects exist** before use
2. **Validate parameters** in functions
3. **Handle edge cases** explicitly
4. **Log errors** for debugging
5. **Use fallback values** when appropriate

---

## Common Engine Functions

### GameBase Functions

#### Position & Rotation
```cpp
// Get position
%pos = GameBase::getPosition(%object);

// Set position
GameBase::setPosition(%object, "100 200 300");

// Get rotation
%rot = GameBase::getRotation(%object);

// Set rotation
GameBase::setRotation(%object, "0 0 0 1");

// Get transform
%transform = GameBase::getTransform(%object);
```

#### Team Functions
```cpp
// Get team
%team = GameBase::getTeam(%object);

// Set team
GameBase::setTeam(%object, %team);

// Get team name
%teamName = $Server::teamName[%team];
```

### Player Functions

#### Player Control
```cpp
// Get player object from client
%playerObj = Client::getOwnedObject(%clientId);

// Get client from player
%clientId = Player::getClient(%playerObj);

// Check if AI controlled
%isAI = Player::isAiControlled(%clientId);

// Kill player
Player::Kill(%clientId);

// Check if dead
%isDead = Player::IsDead(%playerObj);
```

#### Player Stats
```cpp
// Get health
%health = Player::getHealth(%playerObj);

// Set health
Player::setHealth(%playerObj, %health);

// Get energy
%energy = Player::getEnergy(%playerObj);

// Set energy
Player::setEnergy(%playerObj, %energy);
```

### Client Functions

#### Client Information
```cpp
// Get client name
%name = Client::getName(%clientId);

// Get client by name
%clientId = NEWgetClientByName(%name);

// Get first client
%clientId = Client::getFirst();

// Get next client
%clientId = Client::getNext(%clientId);

// Iterate all clients
for(%clientId = Client::getFirst(); %clientId != -1; %clientId = Client::getNext(%clientId))
{
    // Process client
}
```

#### Client Communication
```cpp
// Send message to client
Client::sendMessage(%clientId, %color, %message);

// Broadcast message
Client::sendMessage(2048, %color, %message);  // 2048 = all clients

// Message colors
$MsgWhite = 0;
$MsgRed = 1;
$MsgBlue = 2;
$MsgGreen = 3;
$MsgYellow = 4;
$MsgBeige = 5;
```

#### Client Objects
```cpp
// Get owned object (player)
%playerObj = Client::getOwnedObject(%clientId);

// Set owned object
Client::setOwnedObject(%clientId, %playerObj);

// Get control object
%controlObj = Client::getControlObject(%clientId);

// Set control object
Client::setControlObject(%clientId, %controlObj);

// Get observer camera
%camera = Client::getObserverCamera(%clientId);
```

### Item Functions

#### Item Management
```cpp
// Get mounted item
%item = Player::getMountedItem(%clientId, %slot);

// Mount item
Player::mountItem(%clientId, %item, %slot);

// Unmount item
Player::unmountItem(%clientId, %slot);

// Item slots
$WeaponSlot = 0;
$PackSlot = 1;
```

### Projectile Functions

#### Projectile Creation
```cpp
// Create projectile
%proj = new Projectile(MyProjectile)
{
    position = %startPos;
    velocity = %velocity;
    dataBlock = "ProjectileData";
};
```

### Network Functions

#### Remote Evaluation
```cpp
// Client to server (2048 = server)
remoteEval(2048, "ServerCmdFunction", %param1, %param2);

// Server to client
remoteEval(%clientId, "ClientCmdFunction", %param1, %param2);

// Server to all clients
remoteEval(2048, "ClientCmdFunction", %param1, %param2);
```

#### Remote Say
```cpp
// Remote say (chat)
remoteSay(%clientId, %team, %message, %initTalk);
```

### Zone Functions

#### Zone Operations
```cpp
// Get zone description
%desc = Zone::getDesc(%zoneId);

// Get zone type
%type = Zone::getType(%zoneId);

// Check if in zone
%inZone = Zone::isInZone(%object, %zoneId);
```

### AI Functions

#### AI Control
```cpp
// Spawn AI
AI::spawn(%name, %displayName, %pos, %team);

// Delete AI
AI::delete(%name);

// Get AI ID
%aiId = AI::getId(%name);

// Get client ID from name
%clientId = AI::getClientIdFromName(%name);

// Set AI variable
AI::setVar(%aiId, %key, %value);

// Get AI variable
%value = AI::getVar(%aiId, %key);
```

### Observer Functions

#### Observer Mode
```cpp
// Set orbit object
Observer::setOrbitObject(%clientId, %object, %distance, %height, %angle);

// Set fly mode
Observer::setFlyMode(%clientId, %speed);
```

### Utility Functions

#### Object Lookup
```cpp
// Name to ID
%obj = nameToId("ObjectName");

// ID to name
%name = idToName(%obj);
```

#### Line of Sight
```cpp
// Get LOS info
GameBase::getLOSinfo(%object, %range);
%hitObject = $los::object;
%hitPosition = $los::position;
%hitNormal = $los::normal;
```

## Advanced Scripting Patterns

### Observer Pattern

#### Event Observer
```cpp
function Observer::Register(%event, %observer)
{
    if($ObserverList[%event] == "")
        $ObserverList[%event] = %observer;
    else
        $ObserverList[%event] = $ObserverList[%event] @ " " @ %observer;
}

function Observer::Notify(%event, %param1, %param2)
{
    for(%i = 0; (%obs = GetWord($ObserverList[%event], %i)) != -1; %i++)
    {
        call(%obs, %param1, %param2);
    }
}
```

### Factory Pattern

#### Object Factory
```cpp
function Factory::Create(%type, %params)
{
    switch(%type)
    {
        case "Player":
            return Factory::CreatePlayer(%params);
        case "Item":
            return Factory::CreateItem(%params);
        default:
            return -1;
    }
}
```

### Singleton Pattern

#### Global Manager
```cpp
function Manager::GetInstance()
{
    if($ManagerInstance == "")
    {
        $ManagerInstance = new SimObject(ManagerInstance);
    }
    return $ManagerInstance;
}
```

## Debugging Techniques

### Console Output
```cpp
// Echo to console
echo("Debug message: " @ %variable);

// Conditional debug
if($DebugMode)
    echo("Debug: " @ %message);

// Error logging
echo("ERROR: " @ %errorMessage);

// Warning logging
echo("WARNING: " @ %warningMessage);
```

### Variable Inspection
```cpp
// Dump variable
echo("Variable = " @ %var);

// Dump array
for(%i = 0; (%key = GetWord($ArrayKeys, %i)) != -1; %i++)
{
    echo("Array[" @ %key @ "] = " @ $Array[%key]);
}
```

### Performance Profiling
```cpp
// Start timer
%startTime = getSimTime();

// Code to profile
// ...

// End timer
%endTime = getSimTime();
%elapsed = %endTime - %startTime;
echo("Execution time: " @ %elapsed @ " seconds");
```

## References

- [TribesRebirth Repository](https://github.com/AlexHuck/TribesRebirth/tree/master)
- [Tribes Script Programming Guide](https://www.scribd.com/document/391089608/TRIBES-Script-Programming-pdf)
- [Tribes Scripting Commands](https://ephemeron.org/~bigby/script3/tribesc.html)
- [TorqueScript Documentation](https://torque-3d.readthedocs.io/en/latest/script/intro.html)
- Original Tribes 1.11 Documentation
- Kingdom of Kronos Codebase Patterns

---

**Last Updated**: Based on comprehensive review of TribesRebirth repository, TorqueScript documentation, and Tribes scripting patterns.

