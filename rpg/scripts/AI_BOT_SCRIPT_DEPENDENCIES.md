# Exhaustive List of Script Files Used by AI Bots

## Overview
This document lists **every script file** that enemy bots and town bots pull from, including direct dependencies, indirect dependencies, and data files.

---

## 1. Core Bot System Files

### Primary Bot Files
- **`Ai.cs`** - Main AI bot system, spawning, despawning, movement, targeting, directives
- **`Spawn.cs`** - Spawn point management, spawn loops, spawn counters
- **`rpgfunk.cs`** - Core utility functions: `RefreshAll()`, `RefreshAllEnemyBot()`, `GiveThisStuff()`, `ClearVariables()`, `ClearPlayerVariables()`, `storeData()`, `fetchData()`, `UpdateTeam()`, `SaveCharacter()`, `LoadCharacter()`, `GetBotIdList()`, `DisplayGetInfo()`, `ChangeRace()`, `GetLCKcost()`, `ActivateAllGenerators()`

### Bot Spawning & Lifecycle
- **`playerspawn.cs`** - Player/bot spawning, reconnection, `Game::playerSpawn()`, `Game::playerSpawned()`
- **`connectivity.cs`** - Client connection/disconnection, `Server::onClientDisconnect()`, zone player tracking
- **`playerdamage.cs`** - Death handling, loot generation, `Player::onKilled()`, `Player::onDamage()`, `TossLootbag()`, `DeployLootbag()`, LCK protection, drop rates

### Bot Behavior & Combat
- **`spells.cs`** - Spell casting, `BeginCastSpell()`, `DoCastSpell()`, spell definitions, shove spells (`airfist`, `advshove`, `Airblast`, `Airwarp`)
- **`weapons.cs`** - Weapon properties, `CastingBladeImage::onFire()`, `GetDelay()`, `MeleeAttack()`, `ProjectileAttack()`, `GenerateItemCost()`
- **`weaponHandling.cs`** - Weapon handling, attack speed, weapon switching

### Bot Data & Stats
- **`rpgstats.cs`** - Stat calculations, DEF/MDEF calculations, stat modifiers
- **`hp.cs`** - Health management, `setHP()`, `MaxHP` calculations
- **`mana.cs`** - Mana management, `setMANA()`, `refreshMANA()`, `refreshMANAREGEN()`, `MaxMANA` calculations
- **`weight.cs`** - Weight calculations, overweight penalties
- **`skills.cs`** - Skill definitions, skill IDs, skill caps, `$SkillHaggling`, `$SkillBashing`, `SetAllSkills()`, `GetNumSkills()`
- **`classes.cs`** - Class definitions, `getFinalCLASS()`

---

## 2. Bot Equipment & Armor Files

### Armor & Equipment Definitions
- **`EnemyArmors.cs`** - Enemy bot equipment loadouts, drop rates, `AdminBoots` definitions, `CastingBlade` assignments
- **`HumanArmors.cs`** - Human armor data, `$speed[Race, ArmorTier]` values for movement speed calculations
- **`HumanArmors2.cs`** - Additional human armor data
- **`SpecialArmors.cs`** - Special armor definitions
- **`RaceArmor.cs`** - Race-to-armor mappings
- **`armordata.cs`** - Base armor data definitions
- **`armors.cs`** - Armor system, armor mounting, armor properties

### Item & Accessory Systems
- **`item.cs`** - Item definitions, item properties, item mounting
- **`Accessory.cs`** - Accessory definitions, accessory properties
- **`Belt.cs`** - Belt system, belt items, `$Belt::CachedList`

---

## 3. Bot Economy & Shopping

- **`economy.cs`** - Merchant buying/selling, `getBuyCost()`, `getSellCost()`, `GetItemCost()`, price calculations, haggling, TournyRank discounts
- **`shopping.cs`** - Shop setup functions, shop indices, `$AccessoryVar[Item, $ShopIndex]` definitions

---

## 4. Bot Special Systems

### Seal Battle Bots
- **`remortseal.cs`** - Seal battle bot setup, `SealBattle::SetupBot()`, stat scaling, `SealBattleMultiplier`

### Arena Bots
- **`rpgarena.cs`** - Arena system, arena bots

---

## 5. Bot Zone & Spawn Management

- **`zone.cs`** - Zone management, `Zone::getPlayerList()`, `Zone::getIndex()`, `UpdateZone()`, zone entry/exit tracking
- **`marker.cs`** - Marker system, spawn markers, bot markers
- **`trigger.cs`** - Trigger system, bot triggers
- **`GenericTriggers.cs`** - Generic trigger definitions (stub file)

---

## 6. Bot Communication & Commands

- **`comchat.cs`** - Chat commands, `remoteSay()`, `#resetspawns`, `#deletebot`, `#listbots`, `#despawnbots`, `#despawnzonebots`, `#getinfo`, `#shove`, `#bash`, `#dropcoins`, `#killallbots`
- **`remote.cs`** - Remote function calls, `remoteCANCEL()`, `remoteEval()`, `remoteSetRPGdata()`, `RPGfetchData()`

---

## 7. Bot Game Events & Systems

- **`gameevents.cs`** - Game event system, bot events
- **`itemevents.cs`** - Item event handling, bot item interactions
- **`game.cs`** - Game functions, match management
- **`Player.cs`** - Player object functions, `Player::Kill()`, `Player::applyImpulse()`, `Player::getItemCount()`, `Player::mountItem()`, `Player::incItemCount()`, `Player::isAiControlled()`

---

## 8. Bot Admin & Debug

- **`Admin.cs`** - Admin functions, `AdminBoots` system, `AdminBoots::onMount`, `AdminBoots::startRaceChangeSequence`, `AdminBoots::onUnmount`
- **`DebugInit.cs`** - Debug initialization, debug functions

---

## 9. Bot Data & Configuration Files

### Global Variables & Configuration
- **`globals.cs`** - Global variables, bot configuration: `$AImoveChance`, `$AIspotDist`, `$AIFOVPan`, `$AImaxRange`, `$AIminrad`, `$AImaxrad`, `$AIattackMode`, `$AI::defaultPathType`, `$NameForRace[]`, `$RaceToArmorType[]`, `$BotInfo[]`

### Base Data Files
- **`BaseExpData.cs`** - Experience data, EXP calculations
- **`BaseDebrisData.cs`** - Debris data
- **`BaseProjData.cs`** - Projectile data, weapon projectiles
- **`ArmorData.cs`** - Armor data definitions

### Mission & World Data
- **`Mission.cs`** - Mission functions, mission data
- **`mission.cs`** - Mission system
- **`newMission.cs`** - New mission system
- **`Crystal.cs`** - Crystal system, spawn crystals

---

## 10. Bot Utility & Helper Files

- **`BonusState.cs`** - Bonus state system, temporary stat bonuses, `AddBonusStatePoints()`
- **`sleep.cs`** - Sleep system, bot sleep states
- **`objectives.cs`** - Objectives system
- **`party.cs`** - Party system (may affect bot targeting)
- **`jail.cs`** - Jail system (may affect bot behavior)
- **`NSound.cs`** - Sound system, bot sounds
- **`Help.cs`** - Help system
- **`ferry.cs`** - Ferry system, bot transport

---

## 11. Bot House & Social Systems

- **`house.cs`** - House system, house objectives
- **`housebonus-gameevents.cs`** - House bonus game events

---

## 12. Server Core Files (Loaded for All Systems)

- **`Server.cs`** - Server initialization, script loading, `InitTownBots()`, `InitSpawnPoints()`, `createServer()`, `Server::loadMission()`, `Server::finishMissionLoad()`
- **`version.cs`** - Version checking
- **`hackfix.cs`** - Hack fixes
- **`newstuff.cs`** - New features
- **`advertisements.cs`** - Advertisements
- **`TaurikAdmins.cs`** - Admin system
- **`plugs.cs`** - Plugin system
- **`dtsviewer.cs`** - DTS viewer

---

## 13. Engine & Base Files (Required by Engine)

### Core Engine Files
- **`Player.cs`** - Player object class (engine)
- **`Vehicle.cs`** - Vehicle system (may affect bot movement)
- **`Turret.cs`** - Turret system
- **`Beacon.cs`** - Beacon system
- **`StaticShape.cs`** - Static shape system
- **`Station.cs`** - Station system
- **`Moveable.cs`** - Moveable objects
- **`Sensor.cs`** - Sensor system
- **`Mine.cs`** - Mine system
- **`InteriorLight.cs`** - Interior lighting

### Mission Files
- **`Mission.cs`** - Mission class (engine)
- **`Item.cs`** - Item class (engine)
- **`Accessory.cs`** - Accessory class (engine)
- **`weapons.cs`** - Weapon class (engine)
- **`armors.cs`** - Armor class (engine)
- **`Crystal.cs`** - Crystal class (engine)
- **`Spawn.cs`** - Spawn class (engine)
- **`Marker.cs`** - Marker class (engine)
- **`Trigger.cs`** - Trigger class (engine)
- **`zone.cs`** - Zone class (engine)

---

## 14. Client-Side Files (For Bot Display/UI)

- **`rpghud.cs`** - Client-side HUD (wrapped for dedicated servers)
- **`Client.cs`** - Client-side functions, `RPGfetchData()`, `remoteSetRPGdata()`

---

## 15. Test & Debug Files

- **`test_droprates.cs`** - Drop rate testing functions, `dropTest()`

---

## 16. Mission Files (.mis) - CRITICAL FOR BOTS

### Mission File Structure
Mission files (`.mis`) are **executed directly** via `exec($missionFile)` in `Server::finishMissionLoad()`. They contain the actual bot definitions and world structure.

### Town Bot Definitions
- **`MissionGroup/TownBots`** - Contains all town bot definitions
  - Each town bot is defined as a `SimGroup` or `Marker` object
  - Contains bot name, position, rotation, race, team, and other properties
  - Read by `InitTownBots()` via `nameToId("MissionGroup/TownBots")`
  - Parsed by `GatherBotInfo()` to extract bot information

### Enemy Bot Spawn Points
- **`MissionGroup/SpawnPoints`** - Contains all spawn point definitions
  - Each spawn point is a `Marker` object with spawn configuration
  - Contains: max spawns, min/max radius, min/max delay, spawn bot type indices
  - Read by `InitSpawnPoints()` via `nameToID("MissionGroup\\SpawnPoints")`
  - Used by `SpawnLoop()` to determine when and where to spawn enemy bots

### Bot Paths & Markers
- **`MissionGroup/Teams/teamX/AI`** - Contains AI bot paths and markers
  - Used for pre-placed bots in the map
  - Contains marker groups for bot patrol paths
  - Read by `createAI()` and `AI::setupAI()`

### Bot Attack Markers
- **`MissionGroup/Teams/teamX/AIattackMarkers`** - Contains attack markers
  - Used to define where bots should attack or defend
  - Read by various AI functions for bot behavior

### Example Mission File Structure
```
MissionGroup
  ├── TownBots
  │   ├── Rufus (SimGroup)
  │   │   ├── Marker (position, rotation)
  │   │   └── BotInfo (race, team, name)
  │   └── Troy (SimGroup)
  │       └── ...
  ├── SpawnPoints
  │   ├── SpawnPoint_8650 (Marker)
  │   │   └── Name: "5 10 20 5 10 0 1 2 3" (max, minRad, maxRad, minDelay, maxDelay, botTypes...)
  │   └── SpawnPoint_8651 (Marker)
  │       └── ...
  └── Teams
      ├── team0
      │   └── AI
      │       └── guardPath (SimGroup)
      │           └── Marker1, Marker2, ... (patrol path)
      └── team1
          └── AIattackMarkers
              └── AttackMarker1, AttackMarker2, ...
```

### Mission File Loading Process
1. **`Server::loadMission()`** - Called to load a mission
2. **`Server::finishMissionLoad()`** - Executes `exec($missionFile)` where `$missionFile = "missions\\" $+ %missionName $+ ".mis"`
3. **Mission file executed** - Creates all MissionGroup objects, markers, spawn points, etc.
4. **`InitTownBots()`** - Reads `MissionGroup/TownBots` from the executed mission file
5. **`InitSpawnPoints()`** - Reads `MissionGroup/SpawnPoints` from the executed mission file

### Key Points
- **Mission files are NOT script files** - They are `.mis` files, not `.cs` files
- **Mission files are executed** - They use `exec()` to create game objects
- **Mission files define bot locations** - All bot spawn positions come from mission files
- **Mission files define bot types** - Spawn point bot types are defined in mission files
- **Mission files are required** - Bots cannot spawn without a properly configured mission file

---

## 17. Files Executed Dynamically

### Character Save Files (Prevented for Bots)
- **`temp\*.cs`** - Character save files (bots should NOT save, but system checks for them)

### Server Data Files
- **`temp\serverTempData*.cs`** - Server temporary data
- **`temp\SealValue.cs`** - Seal value data
- **`ServerTime.cs`** - Server time (executed from `rpgfunk.cs`)
- **`HouseObjectives.cs`** - House objectives (executed from `rpgfunk.cs`)

---

## 18. Files Referenced But Not Directly Executed

### Data Arrays (Defined in Multiple Files)
- **`$BotInfo[%name, RACE]`** - Bot race information
- **`$BotInfo[%name, TEAM]`** - Bot team information
- **`$BotInfo[%name, NAME]`** - Bot display name
- **`$BotInfo[%name, SPAWN_POS]`** - Bot spawn position
- **`$BotInfo[%name, SPAWN_ROT]`** - Bot spawn rotation
- **`$BotInfo[%name, SPAWN_MARKER]`** - Bot spawn marker
- **`$NameForRace[%race]`** - Race name mappings
- **`$RaceToArmorType[%race]`** - Race to armor type mappings
- **`$spawnIndex[]`** - Spawn point bot type indices

---

## Summary by Category

### Critical Bot Files (Must Have)
1. `Ai.cs` - Core bot system
2. `Spawn.cs` - Spawn management
3. `rpgfunk.cs` - Core utilities
4. `playerdamage.cs` - Death/loot
5. `playerspawn.cs` - Spawning
6. `spells.cs` - Spell casting
7. `weapons.cs` - Weapon system
8. `EnemyArmors.cs` - Equipment
9. `globals.cs` - Configuration
10. `Server.cs` - Server init

### Important Bot Files (Heavily Used)
11. `connectivity.cs` - Connections
12. `comchat.cs` - Commands
13. `remote.cs` - Remote calls
14. `zone.cs` - Zone management
15. `rpgstats.cs` - Stats
16. `hp.cs` - Health
17. `mana.cs` - Mana
18. `skills.cs` - Skills
19. `classes.cs` - Classes
20. `economy.cs` - Economy
21. `shopping.cs` - Shopping
22. `remortseal.cs` - Seal bots
23. `Player.cs` - Player functions
24. `itemevents.cs` - Item events
25. `gameevents.cs` - Game events

### Supporting Bot Files (Used Indirectly)
26. `Belt.cs` - Belt system
27. `weight.cs` - Weight
28. `BonusState.cs` - Bonus states
29. `Admin.cs` - Admin functions
30. `HumanArmors.cs` - Armor data
31. `armors.cs` - Armor system
32. `item.cs` - Item system
33. `Accessory.cs` - Accessories
34. `weaponHandling.cs` - Weapon handling
35. `marker.cs` - Markers
36. `trigger.cs` - Triggers
37. `Crystal.cs` - Crystals
38. `Colloseum.cs` - Colloseum
39. `rpgarena.cs` - Arena
40. `newseal.cs` - Seal system

### Data & Configuration Files
41. `BaseExpData.cs` - EXP data
42. `BaseDebrisData.cs` - Debris data
43. `BaseProjData.cs` - Projectile data
44. `ArmorData.cs` - Armor data
45. `armordata.cs` - Armor data
46. `Mission.cs` - Mission data
47. `mission.cs` - Mission system
48. `newMission.cs` - New missions

### Engine & Base Files (Required)
49. All engine base files (Player.cs, Item.cs, etc.)
50. All mission files (Marker.cs, Trigger.cs, etc.)

---

## Total Count

**Approximately 50+ script files** are directly or indirectly used by AI bots, with many more engine base files that are required for the system to function.

**Plus mission file (`KingdomKronos.mis`)** which is executed and contain the actual bot definitions, spawn points, and world structure.

---

## Notes

1. **Mission files (`.mis`) are CRITICAL** - They define all bot locations, spawn points, and bot types. Without a properly configured mission file, bots cannot spawn.
2. **Some files are loaded conditionally** (e.g., `rpghud.cs` only on non-dedicated servers)
3. **Some files are executed dynamically** (e.g., character save files, server data files, mission files)
4. **Some files are referenced but not executed** (e.g., data arrays defined in multiple files)
5. **Engine base files** are required but may not contain bot-specific code
6. **Client-side files** may affect bot display but not bot behavior
7. **Mission files are executed** - They use `exec()` to create game objects, so they're treated like script files during execution

---

## File Dependencies Graph

```
Server.cs
  ├── Ai.cs (core bot system)
  │   ├── Spawn.cs (spawn management)
  │   ├── rpgfunk.cs (utilities)
  │   ├── playerdamage.cs (death/loot)
  │   ├── playerspawn.cs (spawning)
  │   ├── spells.cs (spell casting)
  │   ├── weapons.cs (weapons)
  │   ├── EnemyArmors.cs (equipment)
  │   ├── remortseal.cs (seal bots)
  │   ├── zone.cs (zones)
  │   ├── connectivity.cs (connections)
  │   ├── comchat.cs (commands)
  │   ├── remote.cs (remote calls)
  │   ├── rpgstats.cs (stats)
  │   ├── hp.cs (health)
  │   ├── mana.cs (mana)
  │   ├── skills.cs (skills)
  │   ├── classes.cs (classes)
  │   ├── economy.cs (economy)
  │   ├── shopping.cs (shopping)
  │   ├── Player.cs (player functions)
  │   ├── itemevents.cs (item events)
  │   ├── gameevents.cs (game events)
  │   └── globals.cs (configuration)
  └── [All other server files]
```

---

This list represents all script files that bots interact with, directly or indirectly, during their lifecycle from spawn to death.

