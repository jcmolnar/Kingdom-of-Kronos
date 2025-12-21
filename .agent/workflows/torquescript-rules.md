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
- `getSimTime()` returns **SECONDS** (with decimals), NOT milliseconds

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

