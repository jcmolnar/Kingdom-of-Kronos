
In our Starsiege Tribes RPG Mod - Kingdom of Kronos, located at C:\Users\Joe\Desktop\CURRENT HOSTING BACKUPS - PUT DATE\30-11-2025 PRE NEW AI BOTS Kingdom of Kronos V0.8 BOV Plugins - Copy, we have changed town bots and enemy bots to be more like how players work - Using ClientIDs and player objects, instead of Drones for enemy bots and simple static world objects for town bots, so that we can make town bots and enemy bots equip armor, weapons, and add custom pathing as well as other fine tune control and modifications.


# Current Enemy Bot Issues

## Overview
This document describes the known issues and problems with enemy bots (spawn point bots, seal battle bots, Colloseum bots) in the Kingdom of Kronos server. These issues affect bot spawning, despawning, behavior, and data management.

---

## 1. Data Persistence & Cleanup Issues

### 1.1 Bot Number Leakage (PARTIALLY FIXED)
**Status:** Recently addressed with fallback system
**Description:** Bot numbers (e.g., "20" in "AdminLiquifier20") were not being properly freed from `$aiNumTable` and `$tmpbotn` when bots died, especially when `BotInfoAiName` was missing or corrupted.

**Impact:**
- Bot numbers accumulate instead of recycling (0-20 range)
- Numbers can reach 100+ instead of staying in the 0-20 range
- Prevents proper number recycling for new spawns

**Current Fix:**
- Three-tier fallback system in `Player::onKilled()`:
  1. Primary: Uses `BotInfoAiName` if available
  2. Fallback: Extracts number from display name
  3. Last resort: Exhaustive search through common bot name patterns

**Remaining Risk:**
- If all three methods fail, number may still leak (warning logged)
- Shell bots with corrupted data may still cause issues
- Issue still persists on occassion

---

### 1.2 Incomplete Data Cleanup on Death (FIXED)
**Status:** Recently fixed
**Description:** Various flags, variables, and arrays were not being cleared when enemy bots died, causing data persistence issues when client IDs were reused.

**Variables Now Being Cleared:**
- `ShovedByPlayer`, `botAttackMode`, `tmpbotdata`, `SealBattleBot`, `DeathProcessed`, `ExpDistributed`
- `OriginalLootString`, `LoadedProjectile` entries, `EventCommand` entries
- `$EnemyBotClientIdRetry`, `$EnemyBotSpawnRetry`, `$Directive99RemovalAttempted`
- All array entries in `$EnemyBotData`, `$ClientData`, `$TownBotData`

**Impact:**
- New bots inheriting stale data from previous bots using the same client ID
- Incorrect bot behavior due to persistent flags
- Memory leaks from uncleared arrays
 - Issue can still happen

---

### 1.3 BotInfoAiName Cleared Too Early in #deletebot (FIXED)
**Status:** Recently fixed
**Description:** The `#deletebot` command was clearing `BotInfoAiName` before `AI::onDroneKilled()` could use it to find the bot, causing "Could not get AI ID" errors.

**Impact:**
- `AI::onDroneKilled()` unable to find bots deleted via `#deletebot`
- Incomplete cleanup when using `#deletebot`
- Error messages: `"[BOT SHELL DEBUG] AI::onDroneKilled(): ERROR - Could not get AI ID for [botName] (may be shell bot)"`

**Current Fix:**
- `BotInfoAiName` is preserved until after `AI::delete()` is scheduled
- Delayed cleanup (2 seconds) ensures `AI::onDroneKilled()` can find the bot
- Issue still persists on occassion

---

### 1.4 Lootbag Drop Inconsistency (FIXED)
**Status:** Recently fixed
**Description:** Enemy bots would occasionally stop dropping lootbags, even when they should.

**Root Cause:**
- Stale `noDropLootbagFlag` persisting from previous bots or incomplete cleanup
- Flag not being cleared at the start of `Player::onKilled()`

**Current Fix:**
- `noDropLootbagFlag` is explicitly cleared at the very beginning of `Player::onKilled()` for enemy bots
- Cleared from all data storage locations (`storeData`, `$EnemyBotData`, `$ClientData`)

---

## 2. Bot Spawning & Despawning Issues

### 2.1 Shell Bot Formation
**Status:** Partially addressed
**Description:** Bots can become "shells" - corrupted entities with data from previous bots or incorrect bot names.

**Causes:**
- Client IDs being reused too quickly before cleanup completes
- `BotInfoAiName` being corrupted during spawn/despawn
- Race conditions in spawn flow

**Current Mitigations:**
- `$ClientIdRecentlyFreed` flag prevents immediate client ID reuse (5 second delay)
- Enhanced cleanup in `#deletebot` for shell bots
- Fallback bot name extraction from display names

**Remaining Risk:**
- Shell bots can still form if cleanup timing is off
- Corrupted `BotInfoAiName` can cause lookup failures
- Mostly fixed but still possible

---

### 2.2 Team Reset to -1
**Status:** Partially addressed
**Description:** Enemy bots' teams can reset to -1 (invalid team) instead of their assigned team (typically 11, but varies by bot type).

**Causes:**
- `GameBase::setTeam()` is asynchronous and may not complete before `RefreshAll()` runs
- `UpdateTeam()` being called before bot data is fully initialized
- Race conditions between spawn flow and team assignment

**Current Mitigations:**
- `RefreshAllEnemyBot()` function that explicitly avoids touching team
- `VerifyEnemyBotTeam()` function to aggressively restore team
- `PeriodicBotTeamCheck()` runs every 60 seconds to fix team -1
- Early bot detection in `UpdateTeam()` to skip bots

**Remaining Risk:**
- Team can still temporarily be -1 during spawn
- `PeriodicBotTeamCheck` may still need to fix teams periodically
- Fixed via multiple team changes but still not working as intended

---

### 2.3 Spawn Counter Desynchronization
**Status:** Partially addressed
**Description:** Spawn point counters (`$numAIperSpawnPoint`) can become desynchronized, preventing new bots from spawning or allowing too many bots.

**Causes:**
- Bots dying without decrementing counters
- Spawn failures not properly decrementing counters
- Race conditions in spawn flow

**Current Mitigations:**
- Counters decremented in `Player::onKilled()` for enemy bots
- Counters decremented on spawn failure
- `#resetspawns` resets all spawn point counters

**Remaining Risk:**
- Counters can still become desynchronized if cleanup fails
- Manual intervention (`#resetspawns`) may be needed periodically
- ISSUE STILL PERSISTS - MAIN ISSUE!!!

---

## 3. Bot Behavior Issues

### 3.1 Shove Spells/Commands Not Moving Bots
**Status:** Partially addressed
**Description:** Shove commands (`#shove`) and shove spells (`airfist`, `advshove`, `Airblast`, `Airwarp`) do not effectively move enemy bots or interrupt their spell casting.

**Causes:**
- AI movement directives immediately overriding shove impulse
- `Player::applyImpulse()` not working correctly for Player objects (enemy bots)
- AI not respecting shove state

**Current Mitigations:**
- Temporary AI directive removal when bot is shoved
- `ShovedByPlayer` flag prevents `AI::Periodic()` from adding new directives for 0.5 seconds
- Using Player object directly for `applyImpulse()` on enemy bots
- Spell interruption logic for casting bots

**Remaining Risk:**
- Shove may still not be effective if AI re-adds directives too quickly
- Timing-dependent behavior may be inconsistent
- ISSUE STILL PERSISTS

---

### 3.2 Collision Damage for Bots
**Status:** Fixed
**Description:** Enemy bots and town bots were taking collision/fall damage from being shoved or falling.

**Current Fix:**
- `Player::onDamage()` checks for `$LandingDamageType` and prevents it for bots
- Bots no longer take damage from shoves or falls

---

## 5. Performance & Stability Issues

### 5.1 Console Error Spam
**Status:** Partially addressed
**Description:** Various console errors and warnings can spam the console, making debugging difficult.

**Known Errors:**
- "Could not find drone" errors (reduced but may still occur)
- "Player::getItemCount: could not find player" (fixed with validation)
- "CANCEL: Unknown command" (fixed with stub function)

**Remaining Risk:**
- Some errors may still occur during edge cases
- Error handling could be improved further
- Issues still persist
---

### 5.2 Memory Leaks
**Status:** Partially addressed
**Description:** Various arrays and data structures may not be properly cleared, causing memory leaks over time.

**Areas of Concern:**
- `$EventCommand` entries
- `$aidirectiveTable` entries
- `$Belt::CachedList` entries
- Bot number tracking arrays

**Current Mitigations:**
- Comprehensive cleanup in `Player::onKilled()`
- Cleanup in `#deletebot` and `#resetspawns`
- Array clearing for all bot data

**Remaining Risk:**
- Some edge cases may still leak data
- Long-running servers may accumulate stale data
- Likely issue still persists

---

## 6. Known Limitations

### 6.1 AI Engine Compatibility
**Description:** The Tribes AI engine was designed for "Drone" objects, but enemy bots are "Player" objects. This causes compatibility issues.

**Impact:**
- Many native AI engine functions don't work for Player objects - Cursor's Auto thinks this - may be wrong
- Custom movement system required (`AI::EnemyBotMovementLoop()`) - Maybe not necessary?
- Engine functions like `AI::directiveRemove()` must be bypassed - Maybe not necessary?

**Workarounds:**
- Custom implementations for Player objects
- Fallback systems for bot lookup
- Manual directive tracking

---

### 6.2 Asynchronous Operations
**Description:** Many engine operations (like `GameBase::setTeam()`) are asynchronous, causing timing issues.

**Impact:**
- Team may not be set immediately
- Data may not be available when expected
- Race conditions in spawn/despawn flow

**Workarounds:**
- Multiple team setting attempts
- Verification functions (`VerifyEnemyBotTeam()`)
- Periodic checks (`PeriodicBotTeamCheck()`)
- Still need permanent fix

---

## 7. Testing & Verification Needs

### 7.1 Comprehensive Cleanup Testing
**Needs:**
- Test bot death cleanup with all variable combinations
- Verify no data leaks between bot deaths
- Test client ID reuse scenarios

### 7.2 Spawn Flow Testing
**Needs:**
- Test spawn flow under high load
- Test spawn failures and retries
- Test shell bot formation and cleanup

### 7.3 Behavior Testing
**Needs:**
- Test shove effectiveness
- Test movement and targeting

---

## 8. Priority Fixes Needed

### High Priority:
1. **Shell Bot Formation** - Prevent bots from becoming corrupted shells
2. **Team Reset to -1** - Ensure teams are consistently set and maintained - Find why teams are being reset after initial setting, having to be changed to proper team multiple times
3. **Spawn Counter Desynchronization** - Ensure counters stay accurate - WE NEED THIS FIXED

### Medium Priority:
4. **Shove Effectiveness** - Ensure shove commands/spells work reliably
5. **Bot Number Leakage** - Ensure all numbers are freed (fallback system may need refinement)
6. **Memory Leaks** - Comprehensive cleanup verification

### Low Priority:
7. **Console Error Reduction** - Further reduce error spam
8. **Performance Optimization** - Optimize spawn/despawn flow

---

## Summary

Most critical data cleanup issues have been addressed, but some behavioral and timing issues remain. The primary challenges are:

1. **Timing Issues**: Asynchronous operations and race conditions
2. **Data Persistence**: Ensuring complete cleanup on death
3. **Engine Compatibility**: Working around AI engine limitations for Player objects
4. **Shell Bot Prevention**: Preventing corrupted bot states

The codebase has extensive safeguards and fallback systems, but edge cases and timing issues can still cause problems. Continuous monitoring and refinement of cleanup systems is recommended.

