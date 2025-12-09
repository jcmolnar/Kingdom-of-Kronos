# SEAL BATTLE COMPLETE FLOW MAP

## INITIALIZATION PHASE

### 1. SealBattle::Begin(%clientId, %pos, %seal)
**Called when:** Player initiates seal battle
**Actions:**
- Sets `$SealBattleActive = true`
- Sets `$SealFighterDied = false`
- **Sets `noDropLootbagFlag = false` for initiator** (line 155) - Ensures players can drop lootbags if they die
- Stores zone as "Colloseum"
- Stores house from player
- Initializes `$SealBattleParticipants = ""`
- Adds initiator to participants list
- Announces "battle begins in 30 seconds"
- Schedules countdown messages (20s, 10s)
- **SCHEDULES:** `SealBattle::StartBattle()` after 30 seconds
- Stores parameters in globals: `$SealBattleScheduledClientId`, `$SealBattleScheduledPos`, `$SealBattleScheduledSeal`

---

## BATTLE START PHASE

### 2. SealBattle::StartBattle(%clientId, %pos, %seal)
**Called when:** 30 seconds after Begin()
**Actions:**
- Captures all players currently in "Colloseum" zone as participants
- Sets `lastzone` for all participants to Colloseum zone ID
- Teleports all participants to Colloseum entrance: "-3588 -2364 354"
- Builds participant names list
- Sends "wish good luck" message
- **CALLS:** `SealBattle::Loop(%clientId, %pos, %seal, 1)` - Starts with Round 1

---

## MAIN BATTLE LOOP

### 3. SealBattle::Loop(%clientId, %pos, %seal, %round)
**Called when:** 
- From StartBattle() with round=1
- Scheduled from previous loop iteration
- Scheduled after spawning a round

**Flow:**

#### A. IMMEDIATE DEATH CHECK
- **IF** `$SealFighterDied == true`:
  - Play random taunt sound
  - Kill ALL seal bots (rounds 1-3)
  - Send "all players died" message
  - **CALL:** `SealBattle::Conclude(%clientId, false)` - FAILURE
  - **RETURN** (exit loop)

#### B. ROUND-SPECIFIC LOGIC

##### ROUND 1 (if %round == 1):
1. Call `SealBattle::LazyDeathCheck(%clientId)`
   - **IF** `$SealFighterDied == true`: **RETURN** (exit loop)
2. Check if Round 1 bots exist:
   - `SealFighter1`, `SealMage1`, `SealGuardian1`
3. **IF** all 3 bots DON'T exist (all return -1):
   - Display seal value message
   - **CALL:** `SealBattle::SpawnRound(%clientId, %pos, %seal, 1)`
   - **SCHEDULE:** `SealBattle::Loop(..., 1)` after 5 seconds
   - **RETURN** (exit this iteration)
4. **IF** bots exist: Continue to loop scheduling

##### ROUND 2 (if %round == 2):
1. Call `SealBattle::LazyDeathCheck(%clientId)`
   - **IF** `$SealFighterDied == true`: **RETURN** (exit loop)
2. Check if Round 2 bots exist:
   - `SealFighter2`, `SealMage2`, `SealGuardian2`
3. **IF** Round 2 bots exist (any of them != -1):
   - Continue to loop scheduling (just check, don't spawn)
4. **ELSE** (Round 2 bots don't exist):
   - Check if Round 1 bots are dead:
     - `SealFighter1`, `SealMage1`, `SealGuardian1`
   - **IF** all Round 1 bots are dead (all return -1):
     - **CALL:** `SealBattle::SpawnRound(%clientId, %pos, %seal, 2)`
     - **SCHEDULE:** `SealBattle::Loop(..., 2)` after 5 seconds
     - **RETURN** (exit this iteration)
   - **ELSE** (Round 1 bots still alive):
     - Continue to loop scheduling (wait for Round 1 to finish)

##### ROUND 3 (if %round == 3):
1. Call `SealBattle::LazyDeathCheck(%clientId)`
   - **IF** `$SealFighterDied == true`: **RETURN** (exit loop)
2. Check if Round 3 bots exist:
   - `SealFighter3`, `SealMage3`, `SealGuardian3`
3. **IF** Round 3 bots exist (any of them != -1):
   - Continue to loop scheduling (just check, don't spawn)
4. **ELSE** (Round 3 bots don't exist):
   - Check if Round 2 bots are dead:
     - `SealFighter2`, `SealMage2`, `SealGuardian2`
   - **IF** all Round 2 bots are dead (all return -1):
     - **CALL:** `SealBattle::SpawnRound(%clientId, %pos, %seal, 3)`
     - **SCHEDULE:** `SealBattle::Loop(..., 3)` after 5 seconds
     - **RETURN** (exit this iteration)
   - **ELSE** (Round 2 bots still alive):
     - Continue to loop scheduling (wait for Round 2 to finish)

##### ROUND 4 (if %round == 4):
1. Call `SealBattle::LazyDeathCheck(%clientId)`
   - **IF** `$SealFighterDied == true`: **RETURN** (exit loop)
2. Check if Round 3 bots are dead:
   - `SealFighter3`, `SealMage3`, `SealGuardian3`
3. **IF** all Round 3 bots are dead (all return -1):
   - Send success message
   - **CALL:** `IncrementTotalSealValue()`
   - **CALL:** `Saveworld()`
   - **CALL:** `SealBattle::Conclude(%clientId, true)` - SUCCESS
   - **RETURN** (exit loop)
4. **ELSE** (Round 3 bots still alive):
   - Continue to loop scheduling (wait for Round 3 to finish)

#### C. LOOP CONTINUATION SCHEDULING
**IF** `$SealFighterDied != true`:
- Calculate loop delay:
  - Round 1: 10 seconds
  - Round 2+: 5 seconds
- Schedule 5 death checks: `SealBattle::LazyDeathCheck()` at intervals
- **SCHEDULE:** `SealBattle::Loop(..., %round)` after delay
  - **CRITICAL:** Uses SAME round number, not next round

---

## BOT SPAWNING

### 4. SealBattle::SpawnRound(%clientId, %pos, %seal, %round)
**Called when:** 
- From Loop() when a round needs to spawn
- Only called if bots for that round don't exist

**Actions:**
1. **CHECK:** Verify bots for this round don't already exist
   - `SealFighter%round`, `SealMage%round`, `SealGuardian%round`
2. **IF** all 3 bots DON'T exist:
   - Send "Wave X starting now!" message
   - Send participant announcement
   - Determine bot types based on round:
     - **Round 1:** FighterOne (spawnIndex[66]), MageOne (spawnIndex[67]), GuardianOne (spawnIndex[68])
     - **Round 2:** FighterTwo (spawnIndex[69]), MageTwo (spawnIndex[70]), GuardianTwo (spawnIndex[71])
     - **Round 3:** FighterThree (spawnIndex[72]), MageThree (spawnIndex[73]), GuardianThree (spawnIndex[74])
   - **SPAWN 3 bots:**
     - `AI::helper(..., "SealFighter" @ %round, "TempSpawn -3608 -2365 354 1", default)`
     - `AI::helper(..., "SealMage" @ %round, "TempSpawn -3608 -2365 354 1", default)`
     - `AI::helper(..., "SealGuardian" @ %round, "TempSpawn -3608 -2365 354 1", default)`
   - **SCHEDULE:** `SealBattle::SetupBot()` for each bot after 3 seconds
     - `SealBattle::SetupBot("SealFighter" @ %round, %pos, %round)`
     - `SealBattle::SetupBot("SealMage" @ %round, %pos, %round)`
     - `SealBattle::SetupBot("SealGuardian" @ %round, %pos, %round)`
   - **DOES NOT** schedule loop (loop handles its own scheduling)
3. **ELSE** (bots already exist):
   - **RETURN** (do nothing, let loop handle checking)

---

## BOT SETUP

### 5. SealBattle::SetupBot(%botName, %pos, %round)
**Called when:** 3 seconds after bot spawn
**Actions:**
1. Get client ID: `NEWgetClientByName(%botName)`
   - **IF** not found: ERROR and return
2. Get AI name from `BotInfoAiName` (e.g., "RoundOne3")
3. Determine guardtype (RoundOne, RoundTwo, RoundThree)
4. Set bot flags:
   - `botAttackMode = 1`
   - `noDropLootbagFlag = True`
   - `SealBattleBot = true`
   - `RACE = "Seals"`
   - Set armor to "SealsArmor"
5. Get equipment string from `$BotInfo[%aiName, ITEMS]` or `$BotEquipment[%guardtype]`
6. **IF** bot doesn't have weapon:
   - Set auto-equip flags
   - Store `OriginalLootString`
   - **CALL:** `GiveThisStuff(%aiId, %equipString, False)`
7. **IF** bot doesn't have skills:
   - **CALL:** `HardcodeAIskills(%aiId)`
8. **CALL:** `RefreshAll(%aiId)` - Calculate base stats
9. Get base stats: MaxHP, MaxMANA, DEF, MDEF, ATK, DMG, LCK
10. Calculate round multiplier: `SealBattle::GetRoundMultiplier(%round)`
11. **SCALE UNDERLYING VALUES:**
    - LVL = baseLVL * multiplier
    - RemortStep = baseRemortStep * multiplier
    - Endurance skill = baseEndurance * multiplier
    - Energy skill = baseEnergy * multiplier
    - WeightCapacity skill = baseWeightCapacity * multiplier
12. **CALL:** `RefreshAll(%aiId)` - Recalculate MaxHP, MaxMANA, MaxWeight from scaled values
13. Set HP = MaxHP, MANA = MaxMANA
14. **SCALE DIRECT STATS:**
    - DEF = baseDEF * multiplier
    - MDEF = baseMDEF * multiplier
    - ATK = baseATK * multiplier
    - DMG = baseDMG * multiplier
    - LCK = baseLCK * multiplier
15. Set HP = MaxHP, MANA = MaxMANA again (final)
16. Set `GameBase::setDamageLevel(%playerObj, 0.0)` - Full health
17. **IF** bot is Fighter or Guardian:
    - Get mounted weapon
    - Get base weapon damage from `GetAccessoryVar()`
    - Scale weapon damage by multiplier
    - Store in `$SealBattleWeaponDamage[%aiId, %weaponName]`
    - Store in `$SealBattleWeaponOwner[%weaponName] = %aiId`
18. Display comprehensive stats

---

## DEATH CHECKING

### 6. SealBattle::LazyDeathCheck(%clientId)
**Called when:** 
- Scheduled periodically from Loop()
- Called at start of each round check

**Actions:**
1. **IF** `$SealBattleParticipants == ""`:
   - Check if initiator is dead
   - Check if initiator left Colloseum zone
   - **IF** dead or left: Set `$SealFighterDied = true`
   - **RETURN**
2. Parse participant list (comma-separated)
3. For each participant:
   - Check if client exists
   - Count total participants
   - **IF** participant is alive AND in Colloseum zone:
     - Increment `%aliveParticipantCount`
4. **IF** `%totalParticipantCount > 0` AND `%aliveParticipantCount == 0`:
   - Set `$SealFighterDied = true`

---

## BATTLE CONCLUSION

### 7. SealBattle::Conclude(%clientId, %success)
**Called when:**
- All players dead/left (`$SealFighterDied == true`)
- All 3 rounds complete (Round 4 check)

**Actions:**
1. **KILL ALL SEAL BOTS** (rounds 1-3):
   - Loop through rounds 1-3
   - Kill `SealFighter%r`, `SealMage%r`, `SealGuardian%r` if they exist
2. Get zone description and house
3. **FOR EACH PLAYER IN COLLOSEUM:**
   - Clear `noDropLootbagFlag` (set to `false`)
     - **NOTE:** For players, this flag was set to `false` in `SealBattle::Begin()` (line 155) and is never set to `true` during the battle
     - Only seal battle **bots** have this flag set to `True` (in `SealBattle::SetupBot()` line 931) to prevent them from dropping lootbags
     - Clearing it here ensures players can pick up their packs after the battle ends
   - **IF** player is dead: Spawn them
   - **IF** `%success == false` (FAILURE):
     - Teleport to Colloseum entrance
     - Send "1 minute to retrieve pack" message
     - **SCHEDULE:** `SealBattle::TeleportOut()` after 60 seconds
   - **ELSE** (SUCCESS):
     - Teleport to exit position immediately
     - Send "battle ended" message
4. **FOR EACH PARTICIPANT NOT IN COLLOSEUM:**
   - Clear `noDropLootbagFlag`
   - **IF** dead: Spawn them
   - Teleport to Colloseum entrance
   - **IF** failure: Schedule teleport out after 60 seconds
5. Reset globals:
   - `$SealFighterDied = false`
   - `$SealBattleActive = false`
   - `$SealBattleZone = ""`
   - `$SealBattleHouse = ""`
   - `$SealBattleParticipants = ""`

---

## CRITICAL ISSUES IDENTIFIED

### ISSUE 1: Round Progression Logic
**Problem:** Loop continues with same round number, but round progression happens when checking if previous round bots are dead.

**Current Flow:**
- Round 1 loop checks if Round 1 bots exist, spawns if not
- Round 2 loop checks if Round 2 bots exist, if not checks if Round 1 bots are dead
- **BUT:** Round 1 loop is still running and might spawn Round 1 again

**Issue:** Multiple loop instances running simultaneously for different rounds

### ISSUE 2: Loop Scheduling
**Problem:** Loop schedules itself with the SAME round number, not advancing to next round.

**Current Flow:**
- Round 1 loop schedules Round 1 loop again (10s delay)
- Round 2 loop schedules Round 2 loop again (5s delay)
- Round progression only happens when checking if previous round bots are dead

**Issue:** Round 1 loop might continue running even after Round 2 starts

### ISSUE 3: Spawn Timing
**Problem:** `SetupBot()` is called 3 seconds after spawn, but loop might check before bots are ready.

**Current Flow:**
- Bot spawns via `AI::helper()`
- `SetupBot()` scheduled after 3 seconds
- Loop might check for bots before `SetupBot()` completes

**Issue:** Bots might not be fully initialized when loop checks

### ISSUE 4: Duplicate Spawn Prevention
**Problem:** `SpawnRound()` checks if bots exist, but timing issues might allow duplicate spawns.

**Current Flow:**
- `SpawnRound()` checks if bots exist
- If not, spawns bots
- But loop might call `SpawnRound()` multiple times before bots are registered

**Issue:** Multiple spawn attempts for same round

---

## EXPECTED CORRECT FLOW

### Round 1:
1. `StartBattle()` calls `Loop(..., 1)`
2. `Loop(..., 1)` checks if Round 1 bots exist
3. If not, calls `SpawnRound(..., 1)`
4. `SpawnRound()` spawns 3 bots, schedules `SetupBot()` after 3s
5. `Loop(..., 1)` schedules itself after 10s
6. After 10s, `Loop(..., 1)` runs again
7. Round 1 bots exist, so loop just continues checking
8. When Round 1 bots die, next `Loop(..., 1)` iteration should detect this
9. **ADVANCE TO ROUND 2:** Schedule `Loop(..., 2)` instead of `Loop(..., 1)`

### Round 2:
1. `Loop(..., 2)` checks if Round 2 bots exist
2. If not, checks if Round 1 bots are dead
3. If Round 1 bots are dead, calls `SpawnRound(..., 2)`
4. `SpawnRound()` spawns 3 bots, schedules `SetupBot()` after 3s
5. `Loop(..., 2)` schedules itself after 5s
6. After 5s, `Loop(..., 2)` runs again
7. Round 2 bots exist, so loop just continues checking
8. When Round 2 bots die, next `Loop(..., 2)` iteration should detect this
9. **ADVANCE TO ROUND 3:** Schedule `Loop(..., 3)` instead of `Loop(..., 2)`

### Round 3:
1. `Loop(..., 3)` checks if Round 3 bots exist
2. If not, checks if Round 2 bots are dead
3. If Round 2 bots are dead, calls `SpawnRound(..., 3)`
4. `SpawnRound()` spawns 3 bots, schedules `SetupBot()` after 3s
5. `Loop(..., 3)` schedules itself after 5s
6. After 5s, `Loop(..., 3)` runs again
7. Round 3 bots exist, so loop just continues checking
8. When Round 3 bots die, next `Loop(..., 3)` iteration should detect this
9. **ADVANCE TO ROUND 4:** Schedule `Loop(..., 4)` instead of `Loop(..., 3)`

### Round 4 (Victory Check):
1. `Loop(..., 4)` checks if Round 3 bots are dead
2. If all Round 3 bots are dead, calls `Conclude(..., true)`
3. Battle ends successfully

---

## KEY PROBLEMS TO FIX

1. **Loop should advance to next round when current round bots die**
2. **Only ONE loop instance should run at a time**
3. **Loop should wait for bots to be fully initialized before checking**
4. **Round progression should be explicit, not implicit**

