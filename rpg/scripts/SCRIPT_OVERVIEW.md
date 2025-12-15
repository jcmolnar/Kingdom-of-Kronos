# Kingdom of Kronos Script Codebase Overview

This document provides a "helicopter view" of the TorqueScript codebase located in `rpg/scripts/`. Use this as a map to find relevant logic.

## 📂 Core Systems & Engine

### `Server.cs`
Main server initialization and configuration logic.
*   **Key Global Variables**:
    *   `$Server::Port`: Server port.
    *   `$Server::HostName`: Server name.
    *   `$Server::MaxPlayers`: Max player count.
*   **Core Functions**:
    *   `createServer(%mission, %dedicated)`: Initializes the server.
    *   `Server::storeData()`: Saves server state.
    *   `Server::refreshData()`: Reloads server state.
    *   `KickDaJackal(%clientId)`: Anti-cheat kick function.

### `Client.cs`
*   **Purpose**: Client-side connection logic.
*   **Functions**: `Client::connect(...)`, `Client::disconnect(...)`.

### `Game.cs`
*   **Purpose**: Game loop and mission cycling.
*   **Functions**: `Game::startMission()`, `Game::endMission()`, `Game::cycleMission()`.

### `globals.cs`
*   **Purpose**: Global constants and variables.
*   **Content**: Color codes, constant definitions, global flags.

### `connectivity.cs`
*   **Purpose**: Network connection handling.
*   **Functions**: `onConnect(...)`, `onDrop(...)`.

## ⚔️ RPG Core Logic

### `rpgfunk.cs` (CRITICAL)
Character data management and core utilities.
*   **Key Functions**:
    *   `LoadCharacter(%clientId)`: Loads player data from `.cs` file.
    *   `SaveCharacter(%clientId)`: Saves player data to `.cs` file.
    *   `fetchData(%id, %field)`: **MOST USED**. safeGet for player variables.
    *   `storeData(%id, %field, %val)`: **MOST USED**. safeSet for player variables.
    *   `IsDead(%clientId)`: Checks if player is dead.
    *   `SafeGetItemCount(...)`: Wrapper for `Player::getItemCount` with object validation.
    *   `DoCamp(...)`: Handles camping/saving logic.
    *   `GiveThisStuff(...)`: **CORE**. Massive handler for giving items/weapons to players/bots. Handles belt logic.
    *   `AggregateLootbags()`: Periodic system to merge nearby lootbags for performance.
    *   `SetStuffString(...)` / `GetStuffStringCount(...)`: String manipulation for item lists (space-delimited).
    *   `DeployPlatform(...)`: Logic for deployable objects.

### `rpgstats.cs`
*   **Purpose**: Stat calculations.
*   **Functions**: `refreshHP()`, `refreshMANA()`, `msgStat()` (updates client HUD).

### `playerdamage.cs` (CRITICAL)
*   **Purpose**: Damage pipeline.
*   **Functions**:
    *   `Player::onDamage(...)`: **ENTRY POINT** for all damage.
    *   `Player::onCollision(...)`: Weapon hit logic.
    *   `Player::Kill(...)`: Handles player/bot death.

## 🤖 AI & Bots

### `Ai.cs` (CRITICAL)
The central "Brain" of the AI system.

*   **Key Global Variables**:
    *   `$numAI`: Total number of active bots.
    *   `$ActiveEnemyBots`, `$ActiveTownBots`: Specific counters for enemy/town bots.
    *   `$BotRegistry`: Master registry mapping clientIDs to bot data.
    *   `$aiNumTable`: Lookup table for AI number allocation.

*   **Enemy Bot Spawn Flow**:
    1.  `InitSpawnPoints()`: Server startup - initializes spawn points.
    2.  `SpawnLoop(...)`: Main loop managing spawn slots and cooldowns.
    3.  `AI::helper(...)`: Prepares bot name and calls spawn.
    4.  `SpawnAI(...)`: **CORE**. Creates the AI object (`createAI`) and schedules setups.
    5.  `SpawnAIGetClientId(...)`: **CRITICAL**. Retrieve clientID, sets team, and registers bot.
    6.  `AI::setWeapons(...)`: Equips weapons and initializes skills.

*   **Town Bot Spawn Flow**:
    1.  `InitTownBots()`: Server startup - triggers zone spawns.
    2.  `SpawnZoneBots(...)`: Spawns all town bots for a specific zone.
    3.  `SpawnSingleZoneBot(...)`: Spawns individual town bot (bypasses `createAI`).
    4.  `SpawnZoneBotPostSpawn(...)`: **CRITICAL**. Sets "TownBot_" prefix and Citizen team (0).

*   **Despawn & Cleanup Logic**:
    *   `Player::onKilled(...)`: **ENTRY POINT**. Triggered on death. Decrements counters.
    *   `AI::onDroneKilled(...)`: Post-death processing.
    *   `CleanupBot(...)`: **CRITICAL**. Centralized cleanup. Frees AI numbers, clears data.
    *   `DespawnZoneBots(...)`: Despawns all bots in a zone (e.g., when empty).
    *   `AddToGraveyard(...)` / `RemoveFromGraveyard(...)`: Prevents ID reuse race conditions.
    *   `Bot_CleanupStaleIds(...)`: Periodic safety check for "shell bots".

*   **Other Key Functions**:
    *   `AI::onDroneAI(...)`: Main thinking loop for AI.
    *   `BotIndex_Add/Remove(...)`: Manages O(1) lookup tables.
    *   `RegisterBot(...)` / `UnregisterBot(...)`: Manages the `$BotRegistry`.

### `Spawn.cs`
*   **Purpose**: Handles physical spawning logic (locations, markers).
*   **Functions**: `Game::pickRandomSpawn()`, `pickSpawnPoint()`.

## 🎒 Items, Inventory & Economy

### `comchat.cs` (CRITICAL)
Command processor and game logic hub.
*   **Key Functions**:
    *   `remoteSay(...)`: **ENTRY POINT** for all chat. Handles player chat, commands (`#`), and **NPC Dialogue injection**. Avoids looping output for bots via `%senderName` checks.
    *   `#command` handlers: All functions starting with `#` are chat commands.
    *   `#index`: Lists commands.
    *   `#myinfo`: Displays player stats.
*   **NPC Dialogue**:
    *   Bots use `#say`, `#shout`, and `#whisper` commands via `remoteSay`.
    *   Code includes specific logic to block *enemy* bots from chatting while allowing *town* bots (based on skills) or specific exceptions (e.g., `#cast` for Casting Blade).

### `item.cs`
*   **Purpose**: Item usage logic.
*   **Functions**: `Item::onUse(...)`, `Item::onCollision(...)`.

### `Accessory.cs`
*   **Purpose**: Ring/Amulet logic.
*   **Functions**: `GetAccessoryVar(...)`, `AddPoints(...)` (calculates stat bonuses).

### `shopping.cs`
*   **Purpose**: Shop system.
*   **Functions**: `buyItem(...)`, `sellItem(...)`, `checkMoney(...)`.

## 🌍 World & Zones

### `KingdomKronos.mis` (Mission File)
*   **Purpose**: The physical map layout and object definitions.
*   **Structure**:
    *   `SimGroup "Teams"`: Contains team-specific objects.
    *   `TeamGroup "team0"` (Citizens): Contains `SimGroup "DropPoints"` (Spawn Markers) and `SimGroup "base"` (inventory/vehicle stations).
    *   `TeamGroup "team1"` (Enemies): Contains `SimGroup "Turrets"` and AI spawn definitions.
    *   `SimGroup "TowerX"`: Contains `TowerSwitch` objects for House Bases.
    *   `Item "flag1"`: Artifact definitions (Gratigud, Belaguard, etc.).

### `zone.cs` (CRITICAL)
Zone management and logic.
*   **Key Global Variables**:
    *   `$numZones`: Total number of zones.
    *   `$Zone::Type[...]`: Type of each zone (Safe, PvE, etc.).
    *   `$Zone::Desc[...]`: Description/Name of each zone.
*   **Core Functions**:
    *   `InitZones()`: Initializes all zones from mission markers.
    *   `RecursiveZone(...)`: Periodic zone logic loop.
    *   `DoZoneCheck(...)`: Checks player positions against zones.
    *   `UpdateZone(%object)`: Updates a specific object's zone status.

### `marker.cs`
*   **Purpose**: AI Waypoints.
*   **Functions**: `Marker::onAdd(...)`.

### `mission.cs`
*   **Purpose**: Mission parsing.
*   **Functions**: `Mission::load(...)`.

## 🏰 Game Features & Minigames

### `remortseal.cs`
Seal Battle / Boss Fight System.
*   **Key Global Variables**:
    *   `$SealBattleActive`: Is a battle currently active?
    *   `$SealValue[...]`: Tracking seal value for remorts.
    *   `$SealBattleParticipants`: List of players in the battle.
*   **Core Functions**:
    *   `SealBattle::Begin(...)`: Starts the battle sequence.
    *   `SealBattle::SpawnSingleBot(...)`: Spawns wave enemies.
    *   `SealBattle::Loop()`: Main battle logic loop.
    *   `SealBattle::Conclude(...)`: Handles battle end (victory/defeat).

### `rpgarena.cs`
*   **Purpose**: Arena combat.
*   **Functions**: `Arena::Enter(...)`, `Arena::Leave(...)`.
*   **Related in `rpgfunk.cs`**:
    *   `CheckAndBootFromArena(%id)`: Safety check for arena bounds.
    *   `FellOffMap(%id)`: Teleports players back if they fall.

### `house.cs`
*   **Purpose**: Faction/House System (Kronos, Arbal, Curama, Yuliple). **NOT** player housing/building.
*   **Core Functions**:
    *   `JoinHouse(%clientId, %hn)`: Assigns player to a faction.
    *   `BootFromCurrentHouse(%clientId)`: Removes player from faction.
    *   `IsSameHouse(...)`: Checks if two players are in the same faction.
    *   `GetClientHouse(...)`: Returns faction name.
    *   `HouseData::Save/Load()`: Persists faction data.
*   **Related in `rpgfunk.cs`**:
    *   `GetHouseNumber(%n)`: Helper to convert name to ID.
    *   `LoadCharacter`/`SaveCharacter`: handles `MyHouse` variable persistence.
    *   `RecursiveWorld(...)` (in `housebonus-gameevents.cs`): Triggers `HouseEarnings`.
    *   `LoadHouseObjectives()` / `SaveHouseObjectives()`: **CRITICAL**. Persists base control and flag status to `HouseObjectives.cs`.
    *   `UpdateHouseObjectivesDisplay()`: Updates the F2/Objective HUD with house status.
    *   `RecursiveObjectivesRefresh()`: Periodic refresh of objective data.
### `objectives.cs`
*   **Purpose**: Game objectives, flags, and base capture logic. Dual-purpose: standard CTF support and **House System** mechanics.
*   **Core Functions**:
    *   `TowerSwitch::onCollision(...)`: **CRITICAL**. Logic for capturing Bases/Towers for a House (Faction).
    *   `Flag::onCollision(...)` / `Flag::onDrop(...)`: Logic for capturing/stealing House Artifacts.
    *   `TowerSwitch::clientKilled(...)`: Bonus for defending a base.
    *   `ObjectiveMission::missionComplete()`: Handles end-of-mission logic (score limits, time limits).
    *   `Game::checkTimeLimit()`: Main timer loop for matches.

### `housebonus-gameevents.cs`
*   **Purpose**: **CRITICAL**. Main Game Loop & periodic events.
*   **Core Functions**:
    *   `RecursiveWorld(...)`: **CRITICAL**. Main server ticker (1s heartbeat). Handles economy, weather, saves.
    *   `HouseEarnings()`: Periodic EXP rewards for house members.

### `party.cs`
*   **Purpose**: Grouping system.
*   **Functions**: `Party::Invite(...)`, `Party::Join(...)`.
*   **Related in `rpgfunk.cs`**:
    *   `viewGroupList(%clientId)`: Displays party members.

## 🛠️ Admin & Tools
* **`Admin.cs`**: Admin commands and tools.
* **`editor.cs`**: In-game map editor logic.
* **`hackfix.cs`**: Anti-cheat or bug fix scripts.
* **Related in `rpgfunk.cs`**:
    *   `ResetPlayer(%clientId)`: Complete character reset tool.
    *   `OnOrOfflineGive(...)`: Admin tool to give items to offline players.
    *   `KickDaJackal(%clientId)` (via Server.cs): Anti-cheat.

## 📝 Definitions (Data Files)
* **`HumanArmors.cs` / `HumanArmors2.cs`**: Armor visuals and data for humans.
* **`EnemyArmors.cs`**: **CRITICAL**.
    *   **Race Mappings**: `$NameForRace[...]` (e.g., "Militia" -> "Traveller"), `$ArmorTypeToRace[...]`.
    *   **Loadouts**: `$BotEquipment[...]` defines specific gear for each enemy type (e.g., "CLASS Fighter...").
    *   **Spawn Index**: `$spawnIndex[...]` maps numeric IDs to Enemy Types (used in `Ai.cs` spawning).
* **`RaceArmor.cs`**: Visuals/data for different races.
* **`SpecialArmors.cs`**: Unique or boss armor definitions.

---

## 🔧 Key Global Variables Reference

### Player/Character Data (`rpgfunk.cs`, `rpgstats.cs`)
| Variable | Type | Description |
|----------|------|-------------|
| `$PlayerStats[%id, %field]` | Array | Player stat storage (HP, MaxHP, MANA, ATK, DEF, etc.) |
| `$PlayerSkill[%id, %skillId]` | Array | Skill levels per player (see skill constants below) |
| `$ClientData[%id, %field]` | Array | Generic client data storage |
| `$EnemyBotData[%id, %field]` | Array | Enemy bot specific data |
| `$SkillCap` | Number | Maximum skill level (scales with remort) |
| `$SkillRangePerLevel` | Number | Skill points gained per level |

### Skill Constants (`rpgfunk.cs`)
| Constant | Value | Description |
|----------|-------|-------------|
| `$SkillSlashing` | 1 | Melee slashing skill |
| `$SkillPiercing` | 2 | Melee piercing skill |
| `$SkillBludgeoning` | 3 | Melee bludgeon skill |
| `$SkillDodging` | 4 | Dodge/evasion skill |
| `$SkillOffensiveCasting` | 6 | **CRITICAL** - Affects spell damage |
| `$SkillDefensiveCasting` | 7 | Defensive spell skill |
| `$SkillNeutralCasting` | 8 | Utility spell skill |
| `$SkillEnergy` | 9 | Mana pool skill |
| `$SkillEndurance` | 11 | HP pool skill |
| `$SkillWeightCapacity` | 12 | Carry weight skill |
| `$SkillHealing` | 13 | Heal potency skill |

### Bot System (`Ai.cs`)
| Variable | Type | Description |
|----------|------|-------------|
| `$numAI` | Number | Total active AI count |
| `$ActiveEnemyBots` | Number | Enemy bot count |
| `$ActiveTownBots` | Number | Town NPC count |
| `$BotRegistry[%clientId]` | String | Bot registration data (name, team, spawn) |
| `$aiNumTable[%n]` | Number | AI number allocation table |
| `$ClientIdRecentlyFreed[%id]` | Timestamp | Marks ID as recently freed (prevents reuse race) |
| `$SpawnAIScheduled[%name]` | Boolean | Prevents duplicate spawn scheduling |
| `$BotFrozen[%id]` | Boolean | Freezes AI movement (used by seal battles) |

### Seal Battle System (`remortseal.cs`)
| Variable | Type | Description |
|----------|------|-------------|
| `$SealBattleActive` | Boolean | Is battle in progress |
| `$SealBattleCurrentRound` | Number | Current wave (1, 2, or 3) |
| `$TotalSealValue` | Number | Player's accumulated seal value |
| `$RefPlayer["MaxHP"]` | Number | Reference player HP for scaling |
| `$RefPlayer["SkillCap"]` | Number | Reference player skill cap |
| `$SealBotHP[%type]` | Ratio | Bot HP as % of player HP (Fighter=1.0, Mage=0.8, Guardian=3.0) |
| `$SealBotDmg[%type]` | Ratio | Bot damage as % of player HP per hit |
| `$SealBotDEF[%type]` | Ratio | Bot DEF as % of player skill cap |
| `$SealBotMDEF[%type]` | Ratio | Bot MDEF as % of player skill cap |
| `$SealRoundHP[%round]` | Multiplier | HP multiplier per round (1.0, 1.5, 2.0) |
| `$SealRoundDmg[%round]` | Multiplier | Damage multiplier per round |
| `$SealBattleSpellDmgMult[%id]` | Multiplier | Spell damage multiplier for seal mages |
| `$SealBattleBotSpawnDelay` | Seconds | Time for bots to spawn (default 15) |
| `$SealBattleFreezeDuration` | Seconds | Time bots stay frozen after spawn |
| `$SealBattleScaledStats[%id, %stat]` | Number | Stored scaled stats for reapplication |

### Zone System (`zone.cs`)
| Variable | Type | Description |
|----------|------|-------------|
| `$numZones` | Number | Total zone count |
| `$Zone::Type[%idx]` | String | Zone type (Safe, PvE, PvP, etc.) |
| `$Zone::Desc[%idx]` | String | Zone display name |
| `$ZonePlayerCount[%idx]` | Number | Players in zone |
| `$MarkerZone[%spawnPointId]` | FolderID | Maps spawn points to zones |

### House/Faction System (`house.cs`, `objectives.cs`)
| Variable | Type | Description |
|----------|------|-------------|
| `$BaseControl[%house]` | Number | Bases controlled by house |
| `$FlagCommand[%house]` | Number | Artifacts held by house |
| `$HouseMember[%house]` | Number | Online members in house |

---

## 📚 Critical Function Signatures

### Data Access (`rpgfunk.cs`)
```
fetchData(%clientId, %field)
    Returns: Value stored for field, or "" if not found
    Usage: %hp = fetchData(%clientId, "HP");

storeData(%clientId, %field, %value)
    Returns: Nothing
    Usage: storeData(%clientId, "HP", 1000);

SafeGetItemCount(%clientId, %itemName)
    Returns: Item count (0 if invalid)
    Usage: %count = SafeGetItemCount(%clientId, "HealthPotion1");
```

### Bot Identification (`Ai.cs`, `rpgfunk.cs`)
```
IsAIBot(%clientId)
    Returns: True if clientId is any type of bot
    Usage: if(IsAIBot(%targetId)) ...

IsEnemyBot(%clientId)
    Returns: True if clientId is an enemy bot (has SpawnBotInfo)
    Usage: if(IsEnemyBot(%targetId)) ...

isTownBot(%clientId)
    Returns: True if clientId is a town NPC (has BotInfoAiName but NO SpawnBotInfo)
    Usage: if(isTownBot(%targetId)) ...

IsSealBattleBot(%clientId)
    Returns: True if clientId is a seal battle bot
    Check: fetchData(%clientId, "IsSealBattleBot") == "true"

GetClientIdFromPlayerObject(%playerObj)
    Returns: ClientId for player object, or -1
    Usage: %clientId = GetClientIdFromPlayerObject(%hitObject);

NEWgetClientByName(%displayName)
    Returns: ClientId matching display name, or -1
    Usage: %id = NEWgetClientByName("SealFighter1");

AI::getClientIdFromName(%internalName)
    Returns: ClientId for internal AI name (e.g., "RoundOne0")
    Usage: %id = AI::getClientIdFromName("RoundOne0");
```

### Bot Spawning (`Ai.cs`)
```
AI::helper(%displayName, %loadout, %spawnPos, %spawnPointId, %commandIssuer)
    Returns: Internal bot name (e.g., "RoundOne0")
    Note: Main entry point for spawning enemy bots

SpawnAI(%newName, %displayName, %aiSpawnPos, %commandIssuer, %loadout, %spawnPointId)
    Returns: Internal name or "" on failure
    Note: Creates AI object and schedules initialization

SpawnAIGetClientId(%newName, %displayName, %pos, %issuer, %loadout, %spawnPointId)
    Returns: Internal name after clientId is resolved
    Note: Critical - handles race conditions, sets team, registers bot

createAI(%newName, %loadout, %pos, %rot, %displayName)
    Returns: Internal name
    Note: Low-level AI::spawn wrapper

HardcodeAIskills(%aiId, %clientName, %isSealBattleBot, %sealBattleScaledRound)
    Returns: Nothing
    Note: Sets skill values for bots, called by AI::setWeapons
```

### Seal Battle (`remortseal.cs`)
```
SealBattle::Begin(%clientId, %pos, %seal)
    Entry point for starting seal battle

SealBattle::SpawnRound(%clientId, %pos, %seal, %round)
    Spawns all bots for a round (1, 2, or 3)

SealBattle::SpawnSingleBot(%typeIndex, %displayName, %spawnLoc, %pos, %round)
    Spawns individual seal bot

SealBattle::SetupBot(%aiId, %botType, %round, %botName, %guardtype)
    CRITICAL - Sets all scaled stats for seal bots

SealBattle::GetReferencePlayerStats()
    Calculates reference player stats from TotalSealValue

SealBattle::Loop(%clientId, %pos, %seal, %round)
    Main battle logic loop - checks for round completion

SealBattle::UnfreezeRound(%round)
    Unfreezes and activates bots for specified round

SealBattle::Conclude(%success)
    Handles battle end (rewards/penalties)
```

### Stats & Combat (`rpgstats.cs`, `playerdamage.cs`)
```
RefreshAll(%clientId)
    Recalculates all stats (MaxHP, MaxMANA, ATK, DEF, etc.)
    CRITICAL: Must be called after modifying underlying skills

setHP(%clientId, %value)
    Sets current HP

refreshHP(%clientId, %amount)
    Heals by amount (capped at MaxHP)

Player::onDamage(%this, %damageType, %damage, %pos, %vec, %mom, %part, %side, %sourceId, %keyword)
    Entry point for ALL damage processing

SpellDamage(%clientId, %targetId, %damageValue, %index)
    Applies spell damage to target
```

---

## 🗄️ Data Storage Patterns

### Player Data Fields (used with fetchData/storeData)
| Field | Type | Description |
|-------|------|-------------|
| `HP` | Number | Current health |
| `MaxHP` | Number | Maximum health |
| `MANA` | Number | Current mana |
| `MaxMANA` | Number | Maximum mana |
| `ATK` | Number | Attack power |
| `DEF` | Number | Physical defense |
| `MDEF` | Number | Magic defense |
| `DMG` | Number | Damage modifier |
| `LVL` | Number | Character level |
| `RemortStep` | Number | Remort level (0-160) |
| `LCK` | Number | Luck stat |
| `MyHouse` | String | Faction name |
| `BotInfoAiName` | String | Internal AI name for bots |
| `SpawnBotInfo` | String | Spawn info (e.g., "SpawnPoint 8123") |
| `IsSealBattleBot` | Boolean | Is seal battle bot |
| `SealBattleScaledRound` | Number | Round this bot was scaled for |
| `HasLoadedAndSpawned` | Boolean | Bot fully initialized |

### Bot Type Detection Pattern
```javascript
// Recommended pattern for identifying bot types:
%isTownBot = (fetchData(%id, "BotInfoAiName") != "" && fetchData(%id, "SpawnBotInfo") == "");
%isEnemyBot = (fetchData(%id, "SpawnBotInfo") != "");
%isSealBot = (fetchData(%id, "IsSealBattleBot") == "true");
%isPlayer = (!%isTownBot && !%isEnemyBot);
```

---

## ⚠️ Common Pitfalls & Gotchas

### 1. ClientId Reuse Race Condition
**Problem**: When a bot dies, the engine may immediately reuse its clientId for a new bot. Old data can persist.
**Solution**: `SpawnAIGetClientId` now checks for valid player object before clearing stale data.

### 2. RefreshAll Overwrites Scaled Stats
**Problem**: Calling `RefreshAll()` recalculates ATK/DEF from base values, overwriting manually set values.
**Solution**: For seal bots, use `ReapplyScaledStats()` after any `RefreshAll()` call.

### 3. HardcodeAIskills Resets Skills
**Problem**: `HardcodeAIskills()` sets skills to default values, overwriting scaled values.
**Solution**: Check `%isSealBattleBot` flag and skip resetting specific skills (Endurance, Energy, OffensiveCasting).

### 4. Bot Teams After Spawn
**Problem**: `UpdateTeam()` may reset bot team to 0 after spawn.
**Solution**: Use `ScheduleTeamEnforcement()` to re-set team after a delay.

### 5. Empty String vs -1 Checks
**Problem**: TorqueScript returns "" or -1 inconsistently for "not found".
**Solution**: Always check both: `if(%val == "" || %val == -1)`

### 6. AI::getId vs getClientIdFromName
**Problem**: `AI::getId()` uses internal name, `getClientIdFromName()` uses display name.
**Solution**: 
- Internal name (e.g., "RoundOne0"): `AI::getId(%internalName)`
- Display name (e.g., "SealFighter1"): `NEWgetClientByName(%displayName)`

### 7. Player Object vs ClientId
**Problem**: Some functions expect Player objects, others expect clientIds.
**Solution**: Use `Client::getOwnedObject(%clientId)` to get Player from clientId, `GetClientIdFromPlayerObject(%playerObj)` for reverse.

---

## 🔗 Cross-File Dependencies

### Seal Battle Flow
```
remortseal.cs::SealBattle::Begin()
    └─> SealBattle::SpawnRound()
        └─> SealBattle::SpawnSingleBot()
            └─> Ai.cs::AI::helper()
                └─> SpawnAI()
                    └─> createAI()
                    └─> SpawnAIGetClientId() [3s delay]
                        └─> AI::setWeapons()
                            └─> HardcodeAIskills()
        └─> SealBattle::SetupBot() [4.5s delay]
            └─> SealBattle::GetReferencePlayerStats()
            └─> RefreshAll()
            └─> ReapplyScaledStats() [3s delay]
```

### Damage Flow
```
playerdamage.cs::Player::onDamage()
    └─> Calculate damage reduction (DEF, MDEF, armor)
    └─> Apply damage to HP
    └─> Check for death
        └─> Player::onKilled()
            └─> Ai.cs logic for bots
            └─> respawn logic for players
```

### Zone Transition Flow
```
zone.cs::DoZoneCheck()
    └─> UpdateZone(%object)
        └─> Check $ZonePlayerCount
        └─> Trigger SpawnZoneBots() or DespawnZoneBots()
```

---

## 🏷️ Naming Conventions

| Pattern | Example | Meaning |
|---------|---------|---------|
| `RoundOneX` | RoundOne0 | Seal Fighter internal name |
| `roundTwoX` | roundTwo1 | Seal Mage internal name |
| `roundThreeX` | roundThree2 | Seal Guardian internal name |
| `SealFighterN` | SealFighter1 | Seal Fighter display name |
| `SealMageN` | SealMage2 | Seal Mage display name |
| `SealGuardianN` | SealGuardian3 | Seal Guardian display name |
| `TownBot_%name` | TownBot_merchant1 | Town NPC (friendly) |
| `$Seal*` | $SealBotHP | Seal battle configuration |
| `$RefPlayer[*]` | $RefPlayer["MaxHP"] | Reference player stats |
| `$Bot*` | $BotRegistry | Bot system globals |

---

**Version**: 0.8.1 (Updated December 2024)
**Note**: This file should be kept up-to-date as major systems are refactored.
