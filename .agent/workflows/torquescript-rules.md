---
description: Critical TorqueScript/Tribes 1 scripting rules and constraints
---

# TorqueScript Rules for Tribes 1

## CODEBASE DOCUMENTATION

### Critical Reference Files
For understanding the codebase architecture and systems, see these key files:

| File | Description |
|------|-------------|
| [CONTEXT_PROMPT.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/CONTEXT_PROMPT.md) | **Quick start guide** - current architecture summary for new sessions |
| [SCRIPT_OVERVIEW.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/scripts/SCRIPT_OVERVIEW.md) | "Helicopter view" of entire scripts directory and file architecture |
| [TOWN_BOT_CODE_LOCATIONS.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/scripts/TOWN_BOT_CODE_LOCATIONS.md) | Complete town bot function locations and code flow |
| [SPAWN_DESPAWN_FUNCTION_LIST.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/scripts/SPAWN_DESPAWN_FUNCTION_LIST.md) | Enemy bot spawn/despawn function list |
| [analysis/TRIBES_ENGINE_FUNDAMENTALS.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/scripts/analysis/TRIBES_ENGINE_FUNDAMENTALS.md) | Low-level Tribes engine behavior |
| [analysis/SPAWN_DESPAWN_TIMING_ANALYSIS.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/scripts/analysis/SPAWN_DESPAWN_TIMING_ANALYSIS.md) | Spawn timing and schedule delays |
| [REAL_PLAYER_SAFEGUARDS.md](file:///C:/Users/Joe/Desktop/Kingdom%20of%20Kronos%20V0.8.1%20Development/rpg/scripts/REAL_PLAYER_SAFEGUARDS.md) | Player protection safeguards |


### Engine Source Code
**Location**: `C:\Users\Joe\Desktop\Kingdom of Kronos V0.8.1 Development\TribesSource`

> [!TIP]
> **Verifying Engine Commands**
> To verify if a C++ function is exposed to TorqueScript, search `simGame.cpp` (or other plugin files) for `addCommand` calls within the `init()` function.
> Seeing a function in `consoleCallback` logic (C++) is necessary but not sufficient; it must be explicitly registered with `addCommand` to be callable from script.

## CRITICAL SYNTAX RULES

### 1. NO TERNARY OPERATORS
**NEVER use ternary operators (`?:`) in TorqueScript. They cause syntax errors.**

❌ WRONG: `%result = %condition ? "yes" : "no";`
✅ CORRECT: Use if/else blocks

### 2. NO INCREMENT/DECREMENT OPERATORS IN EXPRESSIONS
**Don't use `++` or `--` inside other expressions.**

❌ WRONG: `%array[%i++]` or `if(%count++ > 5)`
✅ CORRECT: Increment on separate line: `%i++; %val = %array[%i];`

### 3. STRING COMPARISON
**Use `==` for string comparison, it works for both strings and numbers.**

### 4. NO BREAK/CONTINUE IN SWITCH
**TorqueScript switch/case falls through by default. Use `return` to exit or structure logic carefully.**

### 5. FUNCTION RETURNS
**Functions return the last evaluated expression or use explicit `return %value;`**
**Empty return uses: `return;` (no value)**

### 6. ARRAY SYNTAX
**Use `$GlobalArray[%key]` or `$GlobalArray[%key, %key2]` for multi-dimensional.**
**Local arrays: `%localArray[%key]` are NOT persistent between function calls.**

## CLIENT ID RULES

### 7. BOT VS PLAYER PROTECTION
- Client ID 2048 is reserved for the server
- ALL client IDs (both players AND bots) start at 2049 and go UP
- Players and bots share the same ID pool - you CANNOT distinguish them by ID range alone
- Use `Player::isAiControlled()` to check if a clientId belongs to a bot
- Always use `IsSafeToModify()` before modifying/deleting bot data
- Use `IsSafeToModifyForEnemyBot()` in enemy bot spawn paths to protect town bots

### 8. TOWN BOT VS ENEMY BOT IDENTIFICATION
- Town bots: Have `BotInfoAiName` but NO `SpawnBotInfo`
- Enemy bots: Have BOTH `BotInfoAiName` AND `SpawnBotInfo`
- Use `isTownBot(%clientId)` to check

## DATA STORAGE

### 9. storeData/fetchData
- `storeData(%clientId, "key", %value)` - routes to correct array based on bot type
- `fetchData(%clientId, "key")` - retrieves data
- For bots, data goes to `$EnemyBotData` or `$ClientData` depending on `isRPGAI()`

### 10. COMMON GOTCHAS
- `GetWord(%string, %index)` returns `-1` when no more words (use as loop terminator)
- `""` (empty string) and `-1` and `0` can all represent "not set" - check all three
- `schedule()` requires string argument: `schedule("FunctionCall();", 1.0);`
- String concatenation uses `@` not `+`
## TIME FUNCTIONS

### 14. getSimTime() vs getIntegerTime()

**`getSimTime()`** - Returns time in **SECONDS** (with decimal precision)
- Use for scheduling, delays, cooldowns, timeouts
- Compare against values in seconds: `< 30`, `< 10`, `+ 3.0`

**`getIntegerTime(true)`** - Returns time in **MILLISECONDS** (as integer)
- Often used with `>> 5` to convert to ~32ms units for anti-spam checks
- When used with `>> 5`, compare against smaller values

### Common Usage Patterns:
```javascript
// getSimTime() - seconds
%timestamp = getSimTime();
if((getSimTime() - %timestamp) < 30)  // 30 seconds ago
%cooldownEnd = getSimTime() + 60;      // 60 seconds from now

// getIntegerTime() - milliseconds, often bit-shifted
%time = getIntegerTime(true) >> 5;    // ~32ms units for anti-spam
if(%time - %clientId.lastFireTime < $fireTimeDelay)  // Fire rate check
```

### ⚠️ Common Mistakes:
- ❌ `if((getSimTime() - %time) < 5000)` - 5000 SECONDS is wrong!
- ✅ `if((getSimTime() - %time) < 5)` - 5 seconds
- The `schedule()` function also uses seconds: `schedule("...", 30.0)` = 30 seconds


### 11. ASCII-ONLY CHARACTERS
**Only use ASCII characters in TorqueScript code and string literals.**
The Tribes 1 console uses legacy encoding (Windows-1252) and cannot display UTF-8/Unicode characters correctly.

❌ WRONG: `echo("Loading → Complete");` (Unicode arrow)
✅ CORRECT: `echo("Loading -> Complete");` (ASCII arrow)

Common replacements:
- `→` becomes `->`
- `←` becomes `<-`
- `•` becomes `*`
- `—` becomes `--`

### 12. STRING CASE HANDLING
**`String::toLower`, `String::toUpper`, `String::getAscii`, `String::getChar` DO NOT EXIST in TorqueScript.**

For case-insensitive string comparison, use the built-in `String::ICompare`:
```cs
// ❌ WRONG - These functions don't exist:
%lower = String::toLower(%str);
%ascii = String::getAscii(%char);

// ✅ CORRECT - Use String::ICompare for case-insensitive comparison:
if(String::ICompare(%str1, %str2) == 0)  // Returns 0 if equal (ignoring case)

// Example: checking command arguments
%subCmd = GetWord(%cropped, 0);
if(String::ICompare(%subCmd, "on") == 0)
    // handle "on", "ON", "On", etc.
```

### 13. ADDING NEW SCRIPT FILES
**All RPG scripts are loaded from `rpg/scripts/Server.cs` in the `createServer()` function (lines 254-322).**

To add a new script file:
1. Create your `.cs` file in `rpg/scripts/`
2. Add `exec(YourFileName);` in Server.cs (without the `.cs` extension)
3. Scripts load in order - place your exec where dependencies are satisfied
4. Example: `exec(Ascension);` loads `rpg/scripts/Ascension.cs`

**REQUIRED: HOOKS/INTEGRATIONS HEADER**
Every new `.cs` file MUST include a header documenting which files it hooks into:
```cs
//====================================================================================================
// MyFeature.cs - Description of the feature
//====================================================================================================
// Brief description of what this script does.
//
//----------------------------------------------------------------------------------------------------
// HOOKS / INTEGRATIONS:
//----------------------------------------------------------------------------------------------------
// Server.cs        - exec(MyFeature); added to load this script
// rpgfunk.cs       - SaveCharacter() saves MyData to funkvar slot XX
// rpgfunk.cs       - LoadCharacter() loads MyData from funkvar slot XX
// comchat_clean.cs - #mycommand handler added
// otherfile.cs     - Description of integration
//====================================================================================================
```
This makes it easy to find all touch points when debugging or modifying the feature.

### 13. ADDING TOWN BOTS (NPCs)
**Town bots are defined in the mission file and their dialogue handlers are in comchat.cs.**

**Step 1: Add to Mission File** (`rpg/MISSIONS/KingdomKronos.mis`)
Inside the `TownBots` SimGroup (line ~4020), add:
```
instant SimGroup "yourbotname" {
    instant Marker "yourbotname" {
        dataBlock = "PathMarker";
        name = "";
        position = "X Y Z";
        rotation = "0 -0 1.56991";
    };
    instant SimGroup "NAME Display Name";
    instant SimGroup "RACE MaleHuman";
    instant SimGroup "ITEMS CLASS Ranger LVL 999 LCK 999 WeaponName 1 ArmorName 1";
};
```

**Step 2: Add Dialogue Handler** (`rpg/scripts/comchat.cs`)
For special NPCs (not merchants/bankers), add a handler after line ~10911:
```cs
else if(%botType == "yourbotname")
{
    if(%initTalk || $state[%closestId, %TrueClientId] != "")
    {
        // Your dialogue logic using $state and AI::sayLater()
    }
}
```

**Bot Types**: The bot name prefix determines type (e.g., "merchant1", "banker2", "sealnpc", "ascensionnpc")

### 15. FUNKVAR SLOTS FOR CHARACTER SAVE DATA
**Before using a new funkvar slot, ALWAYS check if it's already in use.**

Character data is saved to `$funk::var["[\"playername\", TYPE, SLOT]"]` in `rpgfunk.cs`.
- TYPE 0 = regular player variables
- TYPE 1-6 = other data (skills, quest counters, bonus states, etc.)

**Currently Used Slots (TYPE 0):**
| Slot | Data |
|------|------|
| 1 | RACE |
| 2 | EXP |
| 3 | campPos |
| 4 | COINS |
| 5 | isMimic |
| 6 | BANK |
| 7 | PlayerName |
| 8 | grouplist |
| 9 | defaultTalk |
| 10 | password |
| 11 | bounty |
| 12 | inArena |
| 13 | PlayerInfo |
| 14 | deathmsg |
| 15 | Inventory (spawnStuff) |
| 16 | BankStorage |
| 17 | campRot |
| 18 | HP |
| 19 | MANA |
| 20 | LCKconsequence |
| 21 | RemortStep |
| 22 | LCK |
| 23 | RPG Version |
| 26 | GROUP |
| 27 | CLASS |
| 28 | SPcredits |
| 30 | MyHouse |
| 31 | RankPoints |
| 32 | TournyRank |
| 35 | QuestItems |
| 36 | KeyItems |
| 38 | StoredQuestItems |
| 39 | StoredKeyItems |
| 44 | Stance |
| 50 | Other belt items |
| 51 | Equipped Belt Armor |
| 52 | Equipped Belt Accessories |
| 53 | Off-Hand Weapon |
| 54 | Ascension Talents |
| 55 | AutoSkill Priority |
| 56 | AutoParty Enabled (NEW) |

**Before adding a new slot:** `grep -r ", 0, XX]" rpgfunk.cs` to verify it's not in use.
