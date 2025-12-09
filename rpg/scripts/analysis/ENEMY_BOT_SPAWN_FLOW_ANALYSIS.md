# Enemy Bot Spawn Flow - Complete Analysis

## Spawn Flow Timeline

### T+0.0s: SpawnAI() → createAI()
- `AI::spawn()` called
- `createAIPostSpawn()` scheduled for **0.5s** (line 147)
- `SpawnAIGetClientId()` scheduled for **0.5s** (line 1461)

### T+0.5s: SpawnAIGetClientId() runs
- Gets client ID via brute-force search
- **Sets team IMMEDIATELY** (line 1884-1886):
  - `GameBase::setTeam(%playerObj, %botTeam)`
  - `GameBase::setTeam(%aiId, %botTeam)`
  - Stores `botTeam` in `storeData(%aiId, "botTeam", %botTeam)`
- Sets `BotInfoAiName`
- **Schedules `AI::setWeapons()` for 0.2s** (line 2443)

### T+0.7s: AI::setWeapons() runs (0.2s after SpawnAIGetClientId)
- Calls `GiveThisStuff()` to give items
- **Calls `HardcodeAIskills()`** (line 553)

### T+0.7s: HardcodeAIskills() runs (immediately from AI::setWeapons)
- Initializes skills
- **BEFORE calling RefreshAll()** (line 3599-3624):
  - Checks team: `GameBase::getTeam(%aiId)`
  - If -1 or mismatched, restores from `botTeam`
  - Sets team: `GameBase::setTeam(%aiId, %storedBotTeam)`
  - Sets team: `GameBase::setTeam(%playerObjFinal, %storedBotTeam)`
- **Calls `RefreshAll(%aiId)`** (line 3624)

### T+0.7s: RefreshAll() runs (immediately from HardcodeAIskills)
- Checks if enemy bot (line 3738)
- Gets stored `botTeam` (line 3765)
- **Checks team: `GameBase::getTeam(%clientId)`** (line 3786)
- **If team != storedBotTeam, restores it** (line 3787-3791)
- **WARNING if team was -1** (line 3795 - but we suppressed this)

## Problem Analysis

### Issue: Team Still -1 When RefreshAll() Runs

**Root Cause**: `GameBase::setTeam()` is NOT synchronous. Even though we:
1. Set team at T+0.5s in `SpawnAIGetClientId()`
2. Set team again at T+0.7s in `HardcodeAIskills()` before `RefreshAll()`

`RefreshAll()` still sees team -1 when it checks at T+0.7s.

**Why**: The engine needs time to process `GameBase::setTeam()`. Even setting it multiple times doesn't guarantee it's processed before the next function call.

## All RefreshAll() Calls in Spawn Flow

1. **HardcodeAIskills() → RefreshAll()** (line 3624)
   - Called immediately after skills initialization
   - Team should be set before this call
   - **THIS IS THE ONE CAUSING THE WARNING**

2. **GiveThisStuff() → RefreshAll()** (rpgfunk.cs line 2907)
   - Called when giving items to bot
   - Happens BEFORE `HardcodeAIskills()`
   - **POTENTIAL ISSUE**: This might run before team is set!

3. **createAIPostSpawn()** (line 168-367)
   - Scheduled for 0.5s
   - Skips enemy bots (checks SpawnBotInfo, line 242)
   - **NOT A PROBLEM** - doesn't process enemy bots

## Functions That Can Affect Team

1. **GameBase::setTeam()** - Sets team (but not synchronous)
2. **UpdateTeam()** - NOT called for enemy bots (line 2418)
3. **RefreshAll()** - Restores team from `botTeam` if mismatched
4. **VerifyEnemyBotTeam()** - Scheduled for 0.5s (line 2415)
   - Checks team and restores if needed
   - **POTENTIAL ISSUE**: Runs at T+1.0s, but RefreshAll() runs at T+0.7s

## Solution

The team MUST be set RIGHT BEFORE `RefreshAll()` is called. Since `GameBase::setTeam()` isn't synchronous, we need to:

1. **Set team IMMEDIATELY before RefreshAll() call** (already done in HardcodeAIskills)
2. **Add a small delay before RefreshAll()** to give engine time to process
3. **OR: Suppress the warning when team is -1** (already done in RefreshAll)

But the warning is still showing, which means the suppression isn't working. Let me check the RefreshAll code again.


