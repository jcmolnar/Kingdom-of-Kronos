---
description: Critical TorqueScript/Tribes 1 scripting rules and constraints
---

# TorqueScript Rules for Tribes 1

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
- Client IDs 2048 and below are reserved for players
- Bot IDs start at 2049
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

