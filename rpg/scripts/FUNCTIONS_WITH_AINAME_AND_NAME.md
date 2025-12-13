# Functions Using Both %aiName and %name (or %displayName)

This document lists all functions in `AllScripts.txt` that use both `%aiName` (internal name) and `%name` or `%displayName` (display name shown in game).

## Naming Convention

- **`%aiName`**: Internal name stored in AIManager (e.g., "Incarnate8", "Liquifier16")
- **`%name`** or **`%displayName`**: Display name shown in game (e.g., "DemonIncarnate8", "AdminLiquifier16")

---

## Functions with Both Parameters

### 1. `createAI(%aiName, %markerGroup, %name, %skipPostSpawn, %bypassRaceCheck)`
**Location:** `Ai.cs:1217`

**Usage:**
- `%aiName`: Internal AI name (e.g., "Incarnate8")
- `%name`: Display name shown in game (e.g., "DemonIncarnate8")

**How it's used:**
- Line 1519: Calls `AI::spawn(%aiName, %armor, %spawnPos, %spawnRot, %name, "male2")`
  - `%aiName` is passed as the first parameter (internal name for AIManager)
  - `%name` is passed as the 5th parameter (display name shown in game)
- Line 1547: Uses `%name` to look up client ID: `NEWgetClientByName(%name)`
- Line 1565: Sets spawn invulnerability by display name: `$SpawnInvulnByName[%name]`

**Example:**
```torquescript
createAI("Incarnate8", %markerGroup, "DemonIncarnate8", false, false);
// %aiName = "Incarnate8" (internal name - stored in AIManager)
// %name = "DemonIncarnate8" (display name - shown in game)
```

---

### 2. `InitTownBotPostSpawn(%aiName, %name)`
**Location:** `Ai.cs:10395`

**Usage:**
- `%aiName`: Full AI name with "TownBot_" prefix (e.g., "TownBot_merchant1")
- `%name`: Bot name from registry (e.g., "merchant1")

**How it's used:**
- Line 10342: Error message uses both: `"Could not get client ID for " @ %name @ " (aiName: " @ %aiName @ ")"`
- Line 10389: Constructs full AI name: `%aiNameFull = "TownBot_" @ %name;`
- Line 10425-10428: Uses `%aiName` to set AI variables: `AI::setVar(%aiName, "pathType", "none")`
- Line 10469: Uses `%name` to get bot info: `$BotInfo[%name, ITEMS]`

**Example:**
```torquescript
InitTownBotPostSpawn("TownBot_merchant1", "merchant1");
// %aiName = "TownBot_merchant1" (full AI name with prefix)
// %name = "merchant1" (bot name from registry)
```

---

### 3. `RotateTownBotPostSpawn(%aiName, %name)`
**Location:** `Ai.cs:11352` (approximately, based on grep results)

**Usage:**
- `%aiName`: Full AI name with "TownBot_" prefix (e.g., "TownBot_merchant1")
- `%name`: Bot name from registry (e.g., "merchant1")

**How it's used:**
- Similar to `InitTownBotPostSpawn()`, used for rotating town bots
- Line 11294: Error message uses both: `"Could not get client ID for " @ %name @ " (aiName: " @ %aiName @ ")"`

**Example:**
```torquescript
RotateTownBotPostSpawn("TownBot_merchant1", "merchant1");
// %aiName = "TownBot_merchant1" (full AI name with prefix)
// %name = "merchant1" (bot name from registry)
```

---

## Functions with %aiName and %displayName

### 4. `AI::helper(%aiName, %displayName, %commandIssuer, %loadout, %spawnPointId)`
**Location:** `Ai.cs:3232`

**Usage:**
- `%aiName`: Base AI name without number (e.g., "Incarnate")
- `%displayName`: Display name without number (e.g., "DemonIncarnate")

**How it's used:**
- Line 3323: Checks if they're equal: `if(%aiName == %displayName)`
- Line 3339: Constructs display name with race prefix: `%displayName = $NameForRace[%aiName] @ %newName;`
- Line 3347: Passes both to `SpawnAI()`: `SpawnAI(%newName, %displayName, %spawnPos, %commandIssuer, %loadout, %spawnPointId)`

**Example:**
```torquescript
AI::helper("Incarnate", "DemonIncarnate", "SpawnPoint 8678", "", 8678);
// %aiName = "Incarnate" (base name without number)
// %displayName = "DemonIncarnate" (base display name without number)
// Function constructs: newName = "Incarnate8", displayName = "DemonIncarnate8"
```

---

### 5. `SpawnZoneBotPostSpawn(%aiName, %botName, %displayName, %zoneIndex)`
**Location:** `Ai.cs:8405`

**Usage:**
- `%aiName`: Full AI name with "TownBot_" prefix (e.g., "TownBot_merchant1")
- `%botName`: Bot name from registry (e.g., "merchant1")
- `%displayName`: Display name shown in game (e.g., "Merchant1")

**How it's used:**
- Line 8439: Schedules retry with all three: `RetryGetAIId(%aiName, %botName, %displayName, %zoneIndex)`
- Line 8472: Another retry call with all three parameters
- Uses `%displayName` to look up client ID: `NEWgetClientByName(%displayName)`

**Example:**
```torquescript
SpawnZoneBotPostSpawn("TownBot_merchant1", "merchant1", "Merchant1", 8511);
// %aiName = "TownBot_merchant1" (full AI name with prefix)
// %botName = "merchant1" (bot name from registry)
// %displayName = "Merchant1" (display name shown in game)
```

---

### 6. `RetryGetAIId(%aiName, %botName, %displayName, %zoneIndex)`
**Location:** `Ai.cs:8631`

**Usage:**
- `%aiName`: Full AI name with "TownBot_" prefix (e.g., "TownBot_merchant1")
- `%botName`: Bot name from registry (e.g., "merchant1")
- `%displayName`: Display name shown in game (e.g., "Merchant1")

**How it's used:**
- Line 8669: Error message uses all three: `"Could not get client ID for bot " @ %botName @ " (displayName: " @ %displayName @ ", aiName: " @ %aiName @ ")"`
- Uses `%displayName` to look up client ID: `NEWgetClientByName(%displayName)`

**Example:**
```torquescript
RetryGetAIId("TownBot_merchant1", "merchant1", "Merchant1", 8511);
// %aiName = "TownBot_merchant1" (full AI name with prefix)
// %botName = "merchant1" (bot name from registry)
// %displayName = "Merchant1" (display name shown in game)
```

---

## Summary

### Enemy Bot Functions
1. **`createAI(%aiName, %markerGroup, %name, ...)`**
   - `%aiName` = internal name (e.g., "Incarnate8")
   - `%name` = display name (e.g., "DemonIncarnate8")

2. **`AI::helper(%aiName, %displayName, ...)`**
   - `%aiName` = base name without number (e.g., "Incarnate")
   - `%displayName` = base display name without number (e.g., "DemonIncarnate")

### Town Bot Functions
3. **`InitTownBotPostSpawn(%aiName, %name)`**
   - `%aiName` = full AI name with "TownBot_" prefix (e.g., "TownBot_merchant1")
   - `%name` = bot name from registry (e.g., "merchant1")

4. **`RotateTownBotPostSpawn(%aiName, %name)`**
   - `%aiName` = full AI name with "TownBot_" prefix (e.g., "TownBot_merchant1")
   - `%name` = bot name from registry (e.g., "merchant1")

5. **`SpawnZoneBotPostSpawn(%aiName, %botName, %displayName, %zoneIndex)`**
   - `%aiName` = full AI name with "TownBot_" prefix (e.g., "TownBot_merchant1")
   - `%botName` = bot name from registry (e.g., "merchant1")
   - `%displayName` = display name shown in game (e.g., "Merchant1")

6. **`RetryGetAIId(%aiName, %botName, %displayName, %zoneIndex)`**
   - `%aiName` = full AI name with "TownBot_" prefix (e.g., "TownBot_merchant1")
   - `%botName` = bot name from registry (e.g., "merchant1")
   - `%displayName` = display name shown in game (e.g., "Merchant1")

---

## Notes

- **Enemy bots** typically use `%aiName` for the internal name and `%name` or `%displayName` for the display name
- **Town bots** use `%aiName` with "TownBot_" prefix and `%name` for the registry name
- The relationship between `%aiName` and `%name`/`%displayName` varies:
  - For enemy bots: `%displayName` = race prefix + `%aiName` (e.g., "Demon" + "Incarnate8" = "DemonIncarnate8")
  - For town bots: `%aiName` = "TownBot_" + `%name` (e.g., "TownBot_" + "merchant1" = "TownBot_merchant1")


