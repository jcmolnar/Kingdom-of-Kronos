# Lootbag Aggregation Code Locations

This document lists all locations where lootbag aggregation code is located in the codebase.

---

## Primary File: `rpg/scripts/rpgfunk.cs`

All main lootbag aggregation functions are located in `rpgfunk.cs`, starting at line **2807**.

### Main Functions

#### 1. `AggregateLootbags()` 
**Location:** `rpg/scripts/rpgfunk.cs:2807-3060`

**Purpose:** Main aggregation function that:
- Scans `LootbagGroup` and `MissionCleanup` for lootbags
- Filters out player-owned lootbags (only aggregates bot/neutral lootbags)
- Finds nearby lootbags within `$LootbagAggregateRadius`
- Calls `MergeLootbags()` to merge them
- Schedules next run and world save

**Key Features:**
- Always scans both `LootbagGroup` AND `MissionCleanup` (not just as fallback)
- Uses `%processedObjects` list to prevent duplicate processing
- Auto-populates `LootbagGroup` with lootbags found in `MissionCleanup`
- Detailed debug output when `$dbechoMode` is enabled

---

#### 2. `MergeLootbags(%bag1, %bag2)`
**Location:** `rpg/scripts/rpgfunk.cs:3062-3163`

**Purpose:** Merges the contents of `%bag2` into `%bag1`, then deletes `%bag2` if fully merged.

**Key Features:**
- **CRITICAL SAFEGUARD:** Never deletes Player objects (validates object type first)
- Parses loot data format: `"OwnerName NameList COINS X Item1 Y Item2 Z ..."`
- Calls `MergeLootContents()` to merge item lists (supports partial merges)
- Calls `MergeNamelists()` to combine pickup permissions
- Uses `SafeDeleteLootbag()` to safely delete merged bags
- Handles both full merges (delete bag2) and partial merges (update bag2 with leftovers)

---

#### 3. `MergeLootContents(%contents1, %contents2)`
**Location:** `rpg/scripts/rpgfunk.cs:3165-3288`

**Purpose:** Merges two loot content strings with partial merge support.

**Returns:** `"MergedString|RemainingString"`

**Key Features:**
- Parses item lists into associative arrays
- Handles string length limit (255 characters, keeps safe buffer ~240)
- If merged string would exceed limit, items are added to remaining string
- Supports partial merges when bag1 is full

---

#### 4. `MergeNamelists(%namelist1, %namelist2)`
**Location:** `rpg/scripts/rpgfunk.cs:3290-3304`

**Purpose:** Merges two namelists (who can pick up the lootbag).

**Key Features:**
- If either is `"*"` (anyone can pick up), result is `"*"`
- If same, returns as-is
- Otherwise combines with comma separator

---

#### 5. `StartLootbagAggregation(%initialDelay)`
**Location:** `rpg/scripts/rpgfunk.cs:3308-3317`

**Purpose:** Starts the lootbag aggregation system after server initialization.

**Key Features:**
- Uses guard `$LootbagAggregateStarted` to avoid duplicate schedules
- Default delay: 30 seconds after server start
- Schedules first `AggregateLootbags()` call

---

### Helper Functions

#### 6. `IsLootOwnerBot(%ownerName)`
**Location:** `rpg/scripts/rpgfunk.cs:2771` (approximately)

**Purpose:** Determines if a lootbag owner is a bot (not a player).

**Used by:** `AggregateLootbags()` to filter out player-owned lootbags

---

#### 7. `SafeDeleteLootbag(%obj)`
**Location:** `rpg/scripts/rpgfunk.cs:7950` (approximately)

**Purpose:** Safely deletes a lootbag object with validation.

**Key Features:**
- **CRITICAL SAFEGUARD:** Never deletes Player objects
- Validates object type before deletion
- Removes from `LootbagGroup` if present
- Clears loot data arrays

---

## Initialization: `rpg/scripts/Server.cs`

### `Server::onServerCreated()`
**Location:** `rpg/scripts/Server.cs:321`

**Code:**
```torquescript
// Start periodic lootbag aggregation (merges nearby lootbags to reduce clutter)
// Uses guard inside StartLootbagAggregation to avoid duplicate schedules
StartLootbagAggregation(30);
```

**Purpose:** Starts the lootbag aggregation system 30 seconds after server startup.

---

## Configuration Variables

These variables control the aggregation behavior (likely defined in `rpgfunk.cs` or a config file):

- **`$LootbagAggregateInterval`**: How often to run aggregation (in seconds)
- **`$LootbagAggregateRadius`**: Maximum distance between lootbags to merge (in game units)
- **`$LootbagAggregateStarted`**: Guard flag to prevent duplicate initialization
- **`$LootbagAggregateSaveScheduled`**: Guard flag to prevent duplicate world saves
- **`$dbechoMode`**: Debug mode flag (enables detailed debug output)

---

## Related Code

### Lootbag Creation
- **`TossLootbag()`**: Creates lootbags when bots die (likely in `rpgfunk.cs` or `playerdamage.cs`)
- **`Lootbag::onAdd()`**: Should add lootbags to `LootbagGroup` when created (may be in `itemevents.cs`)

### Lootbag Groups
- **`LootbagGroup`**: SimGroup that tracks lootbags (created if it doesn't exist)
- **`MissionCleanup`**: SimGroup that contains all deployable objects including lootbags

---

## Code Flow

1. **Server Startup** (`Server.cs:321`)
   - Calls `StartLootbagAggregation(30)`

2. **Initialization** (`rpgfunk.cs:3308`)
   - Sets `$LootbagAggregateStarted = true`
   - Schedules first `AggregateLootbags()` call after 30 seconds

3. **Aggregation Loop** (`rpgfunk.cs:2807`)
   - Scans `LootbagGroup` for lootbags
   - Scans `MissionCleanup` for additional lootbags
   - Filters out player-owned lootbags
   - Finds nearby lootbags within radius
   - Calls `MergeLootbags()` for each pair

4. **Merging** (`rpgfunk.cs:3062`)
   - Validates objects (never merge Player objects)
   - Parses loot data
   - Calls `MergeLootContents()` to merge items
   - Calls `MergeNamelists()` to merge pickup permissions
   - Updates or deletes lootbags as needed

5. **Scheduling** (`rpgfunk.cs:3059`)
   - Schedules next `AggregateLootbags()` call
   - Schedules `SaveWorldDeployables()` after 30 seconds (if not already scheduled)

---

## Summary

**All lootbag aggregation code is located in:**
- **Primary:** `rpg/scripts/rpgfunk.cs` (lines 2807-3317)
- **Initialization:** `rpg/scripts/Server.cs` (line 321)

**Main Functions:**
1. `AggregateLootbags()` - Main aggregation loop
2. `MergeLootbags()` - Merges two lootbags
3. `MergeLootContents()` - Merges item lists with partial support
4. `MergeNamelists()` - Merges pickup permissions
5. `StartLootbagAggregation()` - Initializes the system

**Helper Functions:**
- `IsLootOwnerBot()` - Checks if owner is a bot
- `SafeDeleteLootbag()` - Safely deletes lootbags


