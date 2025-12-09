# Bot Data Refactoring - Implementation Notes

## Overview
This refactoring separates data storage for players, town bots, and enemy bots into separate arrays to prevent data overlap when client IDs are reused.

## Changes Made

### 1. New Data Arrays
- `$ClientData[%clientId, %type]` - Player data (unchanged)
- `$TownBotData[%clientId, %type]` - Town bot data (NEW)
- `$EnemyBotData[%clientId, %type]` - Enemy bot data (NEW)

### 2. Helper Functions Added (rpgstats.cs)

#### `GetClientDataType(%clientId)`
- Determines if client is a player, town bot, or enemy bot
- Checks `SpawnBotInfo` directly from arrays to avoid recursion
- Returns: "player", "townbot", or "enemybot"

#### `GetDataFromArray(%clientId, %type)`
- Routes reads to appropriate array based on client type
- During migration: checks new array first, falls back to old array
- After migration: only uses new arrays

#### `SetDataInArray(%clientId, %type, %value)`
- Routes writes to appropriate array based on client type
- During migration: writes to both new and old arrays for backwards compatibility
- After migration: only writes to new arrays

### 3. Modified Functions (rpgstats.cs)

#### `fetchData(%clientId, %type)`
- Now uses `GetDataFromArray()` instead of directly accessing `$ClientData`
- All data reads are routed through the helper function

#### `storeData(%clientId, %type, %amt, %special)`
- Now uses `SetDataInArray()` instead of directly accessing `$ClientData`
- All data writes are routed through the helper function

## Migration Strategy

### Phase 1: Dual-Write (Current Implementation)
- Reads: Check new array first, fallback to old array
- Writes: Write to both new and old arrays
- This ensures backwards compatibility during transition

### Phase 2: Full Migration (Future)
- Migrate all existing data from `$ClientData` to appropriate new arrays
- Remove dual-write logic
- Remove fallback reads

### Phase 3: Cleanup (Future)
- Remove old `$ClientData` entries for bots
- Update all code to use new arrays directly (optional optimization)

## Testing Checklist

- [x] Players can log in and play normally
- [x] Town bots spawn and function correctly
- [x] Enemy bots spawn and function correctly
- [x] Bot data doesn't persist when client IDs are reused
- [x] Player data is unaffected
- [x] Shared fields (LCK, HP, MANA, etc.) work for all client types
- [x] Bot-specific fields (QuestItems, SpawnBotInfo, etc.) are isolated

## Known Issues / Considerations

1. **Empty String Handling**: TorqueScript returns "" for non-existent array keys, making it difficult to distinguish between "field doesn't exist" and "field is empty". Current implementation assumes empty strings are valid values.

2. **SpawnBotInfo Check**: The `GetClientDataType()` function must check `SpawnBotInfo` directly from arrays to avoid infinite recursion. This is handled correctly.

3. **Migration Tracking**: Currently no explicit tracking of which fields have been migrated. The fallback logic handles this implicitly.

4. **Performance**: Additional function calls for routing may have minor performance impact. Consider optimizing after migration is complete.

## Refactoring Status: COMPLETE ✅

### Phase 1: Dual-Write (COMPLETE)
- ✅ `fetchData()` now uses `GetDataFromArray()` for all data reads
- ✅ `storeData()` uses `GetDataFromArray()` and `SetDataInArray()` for all data writes
- ✅ All data access routes through helper functions
- ✅ Dual-write to old array for backwards compatibility during migration

### Implementation Details
- **Data Routing**: All `fetchData()` and `storeData()` calls now route through `GetDataFromArray()` and `SetDataInArray()`
- **Type Detection**: `GetClientDataType()` uses `$TownBotSpawned` as primary source of truth, then checks data arrays
- **Migration Support**: Reads check new arrays first, fallback to old array. Writes go to both arrays.

### Direct Array Access
Some direct writes to `$TownBotData` and `$EnemyBotData` in `Ai.cs` are intentional for:
- Critical initialization (setting `BotInfoAiName` before type detection)
- Stale data cleanup (clearing arrays when bots die/reuse client IDs)
- These are necessary to ensure proper type detection before `storeData()` can route correctly

### Next Steps (Future Optimization)

1. ✅ **COMPLETE**: All data access routes through helper functions
2. Monitor for any data persistence issues
3. Once stable, implement full data migration (Phase 2)
4. Remove dual-write logic after migration is complete (Phase 3)
5. Consider optimizing direct array access for performance-critical paths (optional)

