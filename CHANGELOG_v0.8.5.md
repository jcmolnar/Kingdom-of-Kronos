# Kingdom of Kronos v0.8.5 Changelog

## 🎰 NEW FEATURE: Las Vegas Gambling District

### Four New Gambling NPCs with 11 Minigames

#### Honest Jim (Gambler) - Pack Pick Game
- **TWO PACKS**: 50% win chance, 1.4x payout (30% house edge)
- **FOUR PACKS**: 25% win chance, 2.0x payout (50% house edge)
- 10-second time limit with "No refunds! Hurry up." warning
- 30-second cooldown between games
- Bet deducted upfront

#### Card Shark Charlie (Card Dealer)
- **Blackjack**: Classic 21 with hit/stand, 2.5x on natural blackjack
- **High-Low**: Guess if next number (1-11) is higher or lower than current (2-10), 1.8x payout
- **Dice Duel**: Roll 2d6 vs house, 1.8x payout
- **Streak Challenge**: Double or nothing chain, 45% win chance per round, cash out anytime

#### Wheel Master Wendy (Wheelmaster)
- **Wheel of Fortune**: 5 wedges with dramatic commentary
  - LOSE (50%), 1.5x (25%), 2x (15%), 5x (5%), JACKPOT 20x (5%)
- **Roulette**: Full roulette with live ball announcements
  - RED/BLACK/HIGH/LOW/EVEN/ODD (1.9x payout)
  - Specific NUMBER bet (35x payout)
- **Slots**: 3x3 grid with 5 pay lines (3 horizontal + 2 diagonal)
  - Crown (50x), Diamond (20x), Sword (10x), Shield (5x), Skull (3x)
  - Multiple winning lines stack!

#### Quick Quinn (Quick Games)
- **Coin Flip**: Heads/Tails (1.9x payout)
- **Rock Paper Scissors**: Classic RPS (1.8x payout, ties refunded)
- **Number Guess**: Guess 1-100 in 5 tries (3x payout)

### Gambling System Features
- All games display updated balance after win/loss
- Consistent messaging: "You win X coins! (Balance: Y)" / "House Wins! Lost X coins. (Balance: Y)"
- Games are COINS only (no EXP betting)
- All bets deducted upfront
- Technical: New `LasVegas.cs` with `LasVegas_HandleBotDialogue()` function integrated into `comchat.cs`

---

## ⚔️ DUAL WIELDING SYSTEM - COMPLETE OVERHAUL

### Phase 1-4: Full Implementation
- **Off-hand weapons now deal damage** - dual wielding is fully functional
- **Fixed animation system** - off-hand weapon animations work correctly
- **Better weapon positioning** - adjusted `mountOffset` values (-0.85 instead of -1.0) for all dual wield weapons
- Weapons affected: Short Sword, Sword, Long Sword, Elfin Blade, Dagger, Spear, Trident, Mace, Hammer, Morning Star, Pick, Battle Axe, Hatchet, Axe

---

## 🐛 CRITICAL BUG FIXES

### Server Stability
- **Fixed shutdown freeze** affecting server restarts
- **Fixed black screen bug** caused by deferred spawn argument mismatch
  - Was passing (armor, spawnPos, spawnRot, name) instead of (spawnPos, name, skipPostSpawn, bypassRaceCheck)
  - Prevented corrupted bot spawns during player connection
- **Fixed server crash in UnregisterBot** orphan cleanup
  - Added `%excludeObject` parameter to prevent deleting dying player object
  - Schedule orphan deletions with 0.5s delay for safety

### Bot Spawn System Overhaul
- **Fixed ghost bot creation** when spawn aborted due to empty zone
- **Fixed phantom `$numAI` increments** causing eventual spawn block
  - `$numAI` now properly tracks live bot count
  - Centralized increment/decrement logic
- **Fixed spawn telemetry discrepancies** - all spawn paths now tracked
- **Fixed `$numAI` leak** - counter now decrements on bot death
- **Fixed shell bot root cause** - replaced `deleteObject` with `AI::delete` in 6 locations
- **Fixed town bot conflict detection** - verifies bot is actually alive before blocking enemy spawn

### AI System
- **Fixed `AI::getId()` 'False' return value** not being treated as invalid
  - Added checks for 'False' and 'false' strings in spawn logic
- **Removed `AI::getId` fallback** to eliminate 'Could not find drone' spam
- **Fixed `AI::getClientIdFromName`** - now uses silent BaseRep first, `AI::getId` last
- **Fixed `AI::delete()` killing live bots** on different clientIds
  - Prevents cleanup of stale data from killing live bots elsewhere

### Bot Data Management
- **Fixed critical race condition** - `$ClientDataType` now set before `storeData` calls
  - Was causing SpawnBotInfo to be stored in wrong array
  - Fixed EXP distribution failures and lootbag pickup logic
- **Fixed EXP loss bug** - Use O(1) BotType check in `GetClientOrBotName`
  - Replaced slow `isRPGAI()` with fast `$BotType[]` lookup
- **Fixed EXP consistency** - `DistributeExpForKilling` uses same name lookup as damage tracking

### Zone & Spawn Mechanics
- **Added 10-second zone spawn delay** to prevent pass-through spawns
  - Players passing through zones quickly won't trigger bot spawns
  - New functions: `ScheduleZoneSpawn()`, `VerifyAndSpawnZoneBots()`, `CancelPendingZoneSpawn()`
  - Configurable via `$MinZoneOccupancyTime` (default 10s)
- **Fixed zone despawn system**
- **Fixed deferred spawn handling** and `IsRealPlayer` false positives
- **Fixed enemy bot despawn** - missing SpawnPointID issue resolved

### Loot & Gameplay
- **Fixed lootbag aggregation limit** and radius (reduced for performance)
- **Fixed `String::trim` error** in lootbag logic by adding `String_Trim` helper
- **Fixed AFK multi-word zone detection**
- **Fixed turret targeting system**
- **Fixed #verify command** argument parsing

---

## 🛠️ CODE REFACTORING & OPTIMIZATION

### Major Refactors (5000+ lines reduced)
- **Refactored `SpawnAIGetClientId`** - Added 5 helpers, reduced ~200 lines
- **Refactored `AI::onDroneKilled`** - Extracted 4 helper functions, reduced 700 lines to 260
- **Refactored `createAI`** - Added helpers `Bot_ParseGuardType`, `Bot_CleanupStaleIds`
- **Refactored `AI::getClientIdFromName`** - 200 lines → 55 lines using `AI::getId` engine function
- **Created unified `ClearAllBotData()`** function
  - Eliminated ~140 lines of duplicate code across 4 locations
  - Clears 30+ fields across all data arrays consistently

### Spawn System Helpers
- **Phase 1**: `Spawn_CleanupStaleClientId` helper (saved 166 lines)
- **Phase 2**: `Spawn_AbortEmptyZone` helper (saved ~48 lines)
- **Phase 3**: More cleanup refactors (saved 192 lines total from original 13,497)
- Final result: `Ai.cs` reduced from 13,497 to 13,306 lines

### Performance Improvements
- **Implemented `$BotType[]` cache** for O(1) bot type detection
  - Set `$BotType='enemy'` in `RegisterBot()`, `$BotType='town'` in town bot spawns
  - Eliminates 300+ array lookups per spawn
- **Added pre-indexed bot lookup tables** (`BotIndex_*`) for O(1) access
- **Removed dual-write for bots** in `SetDataInArray()`
  - Bots only write to `$EnemyBotData`/`$TownBotData`, not `$ClientData`

---

## 📊 MONITORING & DIAGNOSTICS

### New Admin Commands
- **#fullbotscan** - Compare live bots vs registries, detect discrepancies
- **#spawntelemetry** - Track spawn success/failures with detailed counters
  - Shows `$numAI` counter vs live bot count discrepancy
  - `#spawntelemetry fix` - reset `$numAI` to match live bots
  - `#spawntelemetry reset` - clear all counters
- **Enhanced #deletebot** - Fixed bug where AI number 0 was incorrectly skipped

### Telemetry System
- **Full spawn telemetry tracking** with console + in-game output
- **New counters**: `$Telemetry_SpawnAttempts`, `$Telemetry_SpawnSuccess`, `$Telemetry_SpawnFailed_*`
- **`$numAI` tracking**: `$Telemetry_NumAI_Inc`, `$Telemetry_NumAI_Dec`
- **Periodic AI number reconciliation** - runs every 5 minutes for auto-cleanup

### Watchdog System
- **Heartbeat every 5 seconds** logs to console and exports state to `config/watchdog_state.cs`
- **`Watchdog_LoopCheck`** with 500 iteration limit detects infinite loops
- **Instrumented**: `PeriodicShellBotCheck`, `PeriodicGhostClientIdCleanup`

### Ghost Bot Detection
- **`PeriodicGhostBotScan()`** runs every 30 seconds
  - Detects bots with missing `$BotInfoAiName`
  - Double-scan approach prevents false positives on spawning bots (3s + buffer)
- **Auto-cleanup** confirmed ghosts after verification period

---

## 🔧 TECHNICAL IMPROVEMENTS

### Player Safeguards
- **Added player safeguard** in `GetClientDataType()` - checks save file FIRST
  - Prevents black screen by ensuring players never misidentified as bots
  - Defaults to 'player' instead of 'enemybot' when type unknown
- **Refactored player safeguards** - unified safeguard functions

### Bot Iteration Fixes
- **Fixed `Client::getFirst` bot iteration** + BaseRep refactor
  - `CleanupOldGraveyardEntries` now uses `BaseRep::getFirst`
  - Prevents iteration issues with mixed player/bot lists

### Debug System Overhaul
- **Wrapped all debug output** with appropriate flags across entire codebase
  - Added toggleable flags for loot bag, AI, spawn, zone, and other systems
  - Massive reduction in console spam
- **Removed redundant debug logs** in helper functions
- **Final cleanup**: `rpgfunk.cs`, `playerspawn.cs`, `Ai.cs`, `Spawn.cs`

### Engine Integration
- **Added `PlayerManager::getFreeId` script support**
- **Improved `AI::delete` fixes** for town bot support with TownBotData lookup
- **Fixed `BaseRep::getFirst`** for bot iteration

### Code Quality
- **Replaced Unicode characters** with ASCII equivalents
  - Fixed → to -> in Ai.cs for Tribes 1 console compatibility
  - Added Rule #11 to `torquescript-rules.md`: ASCII-ONLY CHARACTERS
- **Fixed syntax errors** and brace mismatches across multiple files
- **Added missing return values** and safety checks throughout

---

## 📚 DOCUMENTATION

### New Documentation
- **Git workflow guide** added
- **SPAWN_DESPAWN_FUNCTION_LIST.md** updated with new helpers
  - `Spawn_AbortEmptyZone`, `Spawn_CleanupStaleClientId`, `Bot_ClearStaleData`, `Bot_CleanupStaleIds`
- **SCRIPT_OVERVIEW.md** updated with new helper functions
- **`torquescript-rules.md`** - Added ASCII-only character rule

### Updated Documentation
- **CONTEXT_PROMPT.md** updated with current architecture
- **SCRIPT_OVERVIEW.md** updated with refactored functions

---

## 🎮 GAMEPLAY IMPROVEMENTS

### Quality of Life
- **Reduced lootbag aggregate radius** for better performance
- **Added AI number cooldown** to prevent spawn spam
- **Improved zone detection** for multi-word zone names
- **Better AFK detection** with admin override support

### Balance Changes
- **Damage calculator** added for balancing
- **Seal Battle improvements** and bug fixes

---

## 🏗️ INFRASTRUCTURE

### Git & Version Control
- Added `.gitignore` entries for `TEMP/` and large installer files
- Removed 110MB JDK installer from git history
- Added git workflow documentation

### Server Configuration
- **Periodic systems** startup consolidated in `Server.cs`:
  - `StartAINumberReconciliation()`
  - `StartGhostBotCleanup()`
  - `StartSpawnCounterReconciliation()`
  - `StartLootbagAggregation()`

---

## 📈 STATISTICS

- **Total commits**: 72
- **Lines of code reduced**: ~5000+ through refactoring
- **New files**: `LasVegas.cs`, multiple documentation files
- **Files refactored**: `Ai.cs`, `rpgfunk.cs`, `comchat.cs`, `connectivity.cs`, `DualWielding.cs`
- **Critical bugs fixed**: 15+
- **Performance optimizations**: 10+
- **New features**: Gambling system (11 games), Dual wielding overhaul

---

**Full version**: Kingdom of Kronos v0.8.5  
**Previous version**: Kingdom of Kronos v0.8.1  
**Total development time**: December 2025
